using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class BrainConfig {
    public Pathfinding.AIDirections numMovementDirections;
    public bool canFaint = false;
    public float roamRestingFraction = .5f;
    public float reconsiderRateRoam = 5;
    public float reconsiderRateFollow = 2.5f;
    public float reconsiderRateTarget = 1f;
    public float scanningRate = 1f;
    public bool scanForFocusWhenFollowing = true;
    public bool hasAttack = false;
    public float timidity = .75f;   

}

// READY States : Roam | Follow | FollowOffsensive | Station

//                 | Scanning | Investigating | Focused | Busy | Trekking | followDir. | attackDir. | executeDir.
// ----------------+----------+---------------+---------+------+----------+------------+------------+------------
// READY: not I/F  | USUALLY* |               |         |      | ALWAYS   | if F/FO    | FO if dir. |            
// READY: investg. | ALWAYS   | ALWAYS        |         |      | ALWAYS   | if F/FO    | FO if dir. |            
// READY: focused  |          |               | ALWAYS  |      |          | if F/FO    | FO if dir. |            
// Execute         |          |               |         | ALWS |          | ALWAYS     |            | ALWAYS     
// Pair            |          |               |         | ALWS | ALWAYS   | sometimes  |            |            
// Faint           |          |               |         | ALWS |          |            |            |            
// controlOverride | states and directives: any row above, but ignored - no coroutines running

// * always except iff State is Following and scanForFocusWhileFollowing is off

// Note: always exactly one in each row is true:
// * Focused | Busy | TrekkingFree
// * Focused | Trekking | Execute | Faint
// (note Busy == Execute || Pair || Faint, and TrekkingFree == Trekking && !Pair)
// These rules are encoded in StateAssumptions() at the bottom, which runs every Update().

// Notes from the last refactor.
// The decision tree of the AI can be modeled by https://app.diagrams.net/#G12U12TJ4aRo3wOl9gePm-mq7hj-KayC94 .
//
// Separate different functionality:
// 1. Inputs: five types
//    - Player commands
//    - Player hints
//    - Same-team creature requests and messages
//    - Environmental input (e.g. enemies)
//    - Internal feedback ("am I succeeding at X")
//    Most calls to Brain will update an Input.
//    State change computation can be accomplished without committing a state modification.
//    So can immediately compute the state change even without its consequences running until the next frame.
// 2. State change
//    The decision tree of the AI can be modeled by https://app.diagrams.net/#G12U12TJ4aRo3wOl9gePm-mq7hj-KayC94 .
//    Note that there are only nine different kinds of output behaviors, even though the decision tree is complex.
//    We want legal transitions to be relatively stateless, i.e., most states can transition to most states with few special cases.
// 3. Consequences of state change
//    Macro logic:
//      Although most states can transition to most states, we still want to borrow a good State Machine principle:
//      Every state has OnEnter and OnExit logic that runs when entering and exiting the state.
//    Micro logic:
//      We will introduce a concept of BehaviorNodes which are units of behavior,
//      and some BehaviorNodes can modify other BehaviorNodes for more complex behavior.

public class Brain {
    public BrainConfig general;
    public Species species;
    protected Creature creature;
    public Terrain terrain;
    protected Transform grid { get => terrain.transform; }
    public int team { get => GetComponentStrict<Team>().TeamId; }
    public CharacterController movement { get => creature.controller; }
    public Pathfinding pathfinding;
    private GoodTaste taste;

    public Transform transform { get => species.transform; }

    ///////////////////
    // STATE PROPERTIES

    public CommandType State {
        get => state;
        set {
            creature.stateForEditorDebugging = value;
            state = value;
            TriggerStateChange();
        }
    }
    // focus is (C#) Really Null iff Focused == false.
    // Here we only care if we set it to null, not whether Unity did.
    // To avoid Unity Null, you must check Focused first.
    public Transform Focus {
        get {
            return focus;
        }
        set {
            if (ReferenceEquals(focus, value)) return;
            ClearFocus();
            focus = value;
            if (ReferenceEquals(value, null)) {
                if (focusOrExecuteDirectiveIsPair != null) {
                    focusOrExecuteDirectiveIsPair.EndPairCommand(transform);
                    focusOrExecuteDirectiveIsPair = null;
                }
            } else FocusedBehavior.Start();
            TriggerStateChange();
        }
    }
    public bool Focused {
        get {
            if (focus == null && !ReferenceEquals(focus, null)) {
                Debug.Log("Focus died");
                badState = true;
            }
            return focus != null;
        }
    }
    public Vector3? Investigation { // trying to focus but cannot see
        get => investigation;
        set {
            investigationCancel?.Stop();
            investigation = value;
            if (investigation != null) investigationCancel =
                RunOnce.Run(species, Creature.neighborhood * movement.Speed,
                    () => Investigation = null);
            TriggerStateChange();
        }
    }
    public bool Investigating { // trying to focus but cannot see
        get => investigation != null;
    }
    public bool Busy { // not available to focus
        get => state == CommandType.Execute || state == CommandType.Pair || state == CommandType.Faint;
    }
    public bool TrekkingFree { // available for pairing
        get => !Focused && !Busy;
    }
    protected bool Scanning {
        get =>
            state == CommandType.Roam ||
            state == CommandType.Station ||
            state == CommandType.FollowOffensive ||
            (state == CommandType.Follow && general.scanForFocusWhenFollowing);
    }
    private CommandType state = CommandType.Roam;
    private bool badState = false; // wait one frame for CleanUpState()
    private bool stateIsDirty = false;
    private MonoBehaviour controlOverride = null; // character controller fully controlled by another script
    private Transform focus = null; // when Focused. Only written to inside Focus.set and ClearFocus()
    private Vector2? investigation = null; // focus character not identified, only location
    private RunOnce investigationCancel = null;
    protected Transform threat = null; // for Follow
    protected Transform followDirective = null; // for Follow
    protected Vector3 stationDirective = Vector3.zero; // for Station
    protected Transform attackDirective = null; // for FollowOffensive, can be null
    public OneOf<Terrain.Position, SpriteSorter> executeDirective { get; protected set; } // for Execute
    protected Queue<Tuple<CoroutineWrapper, OneOf<Terrain.Position, SpriteSorter>>> executeCommandQueue = null; // null when not used
    protected Transform pairDirective = null;
    protected Creature focusOrExecuteDirectiveIsPair = null;

    /////////////////////////////////////////////
    // INITIALIZATION, VIRTUAL, & UTILITY METHODS

    public Brain(Species species, BrainConfig general) {
        this.species = species;
        this.general = general;
    }
    public Brain InitializeAll() {
        pathfinding = new Pathfinding(this);
        creature = GetComponentStrict<Creature>();
        terrain = GameObject.FindObjectOfType<Terrain>();
        taste = GetComponent<GoodTaste>();
        Health health = GetComponent<Health>();
        if (health != null) health.ReachedZero += OnHealthReachedZero;
        TrekkingBehavior = new CoroutineWrapper(TrekkingBehaviorE, species);
        ScanningBehavior = new CoroutineWrapper(ScanningBehaviorE, species);
        FocusedBehavior = new CoroutineWrapper(FocusedBehaviorE, species);
        OnStateChange();
        Initialize();
        return this;
    }
    virtual protected void Initialize() {}
    virtual public List<CreatureAction> Actions() { return new List<CreatureAction>(); }
    virtual protected void Attack() {}
    virtual public bool CanTame(Transform player) { return false; }
    virtual public bool ExtractTamingCost(Transform player) { return false; }

    protected CoroutineWrapper ScanningBehavior;
    virtual protected IEnumerator ScanningBehaviorE() { yield break; }
    protected CoroutineWrapper FocusedBehavior;
    virtual protected IEnumerator FocusedBehaviorE() { yield break; }
    protected CoroutineWrapper ExecutingBehavior;

    protected T GetComponent<T>() => species.GetComponent<T>();
    protected T GetComponentStrict<T>() => species.GetComponentStrict<T>();
    virtual protected void OnHealthReachedZero() => GameObject.Destroy(creature.gameObject);

    /////////////////////////
    // STATE UPDATE FUNCTIONS

    private void ClearFocus() {
        this.focus = null;
        this.investigation = null;
        FocusedBehavior.Stop();
    }
    public void DebugLogStateChange(bool triggerOnly) {
        string stateChangeText = triggerOnly ? "): triggered state change, state: " : "): state changed! state: ";
        string result = species + " (team " + GetComponentStrict<Team>().TeamId + stateChangeText + state;
        if (Scanning) result += "; scanning";
        if (Focused) result += "; focus: " + focus;
        if (Investigating) result += "; investigation: " + investigation;
        if (controlOverride != null) result += "; controlled by " + controlOverride;
        Debug.Log(result);
    }
    public void TriggerStateChange() {
        DebugLogStateChange(true);
        stateIsDirty = true;
    }
    protected void OnStateChange() {
        DebugLogStateChange(false);
        if (controlOverride != null) {
            ScanningBehavior.Stop();
            FocusedBehavior.Stop();
            TrekkingBehavior.Stop();
            ExecutingBehavior?.Stop();
            return;
        }
        if (Busy) ClearFocus();
        if (State == CommandType.Faint) movement.Idle();

        // run coroutines
        ScanningBehavior.RunIf(Scanning);
        TrekkingBehavior.RunIf(TrekkingFree || State == CommandType.Pair);
        ExecutingBehavior?.RunIf(State == CommandType.Execute);

        // clear directives
        if (!(State == CommandType.Follow || State == CommandType.FollowOffensive || 
            State == CommandType.Execute || State == CommandType.Pair)) followDirective = null;
        if (State != CommandType.FollowOffensive) attackDirective = null;
        if (State != CommandType.Station) stationDirective = Vector3.zero;
        if (State != CommandType.Execute) {
            executeDirective = null;
            ExecutingBehavior = null;
            executeCommandQueue = null;
        }
        if (State != CommandType.Pair && pairDirective != null) {
            Creature pairCreature = pairDirective.GetComponent<Creature>();
            if (pairCreature != null) pairCreature.EndPairRequest(); // this updates another creature's state
            pairDirective = null;
        }
    }

    public CharacterController OverrideControl(MonoBehaviour source) {
        controlOverride = source;
        TriggerStateChange();
        return movement;
    }
    public void ReleaseControl() {
        controlOverride = null;
        TriggerStateChange();
    }

    public void CommandFollow(Transform directive) {
        followDirective = directive;
        ClearFocus();
        State = CommandType.Follow;
        movement.SetBool("Fainted", false);
    }
    public void RequestFollow() {
        if (followDirective == null) State = CommandType.Roam;
        else {
            WorldInteraction playerInterface = followDirective.GetComponentStrict<PlayerCharacter>().Interaction;
            State = CommandType.Follow;
            playerInterface.EnqueueFollowing(creature);
        }
    }
    public Transform FollowDirective { get => followDirective; }
    // param recipient may be null, which is a no-op
    protected Transform RequestPair(Creature recipient) {
        if (recipient?.TryPair(transform) == true) {
            focusOrExecuteDirectiveIsPair = recipient;
            return recipient.transform;
        }
        else return null;
    }
    public bool TryCommandPair(Transform initiator) {
        if (TrekkingFree) {
            State = CommandType.Pair;
            pairDirective = initiator;
            return true;
        } else return false;
    }
    public void EndPairRequest() {
        focusOrExecuteDirectiveIsPair = null; // manually lest we issue redundant EndPairCommand below
        if (Focused) Focus = null;
        else if (State == CommandType.Execute) RequestFollow();
        else Debug.LogError(species.name + ": Why was there a pair when state " + State);
    }
    public void EndPairCommand(Transform initiator) {
        if (pairDirective != initiator) return; // third parties are not allowed to do this
        if (followDirective == null) State = CommandType.Roam;
        else State = CommandType.Follow;
    }
    public void CommandExecute(CoroutineWrapper executingBehavior,
            OneOf<Terrain.Position, SpriteSorter> directive) {
        this.ExecutingBehavior = executingBehavior;
        this.executeDirective = directive;
        State = CommandType.Execute;
        executeCommandQueue = null;
    }
    public void EnqueueExecuteCommand(CoroutineWrapper executingBehavior,
            OneOf<Terrain.Position, SpriteSorter> directive) {
        if (executeCommandQueue == null) {
            CommandExecute(executingBehavior, directive);
            executeCommandQueue = new Queue<Tuple<CoroutineWrapper, OneOf<Terrain.Position, SpriteSorter>>>();
        } else {
            executeCommandQueue.Enqueue(new Tuple<CoroutineWrapper, OneOf<Terrain.Position, SpriteSorter>>(executingBehavior, directive));
        }
    }
    public void CompleteExecution() {
        if (executeCommandQueue == null || executeCommandQueue.Count == 0) RequestFollow();
        else {
            ExecutingBehavior.Stop(); // not sure if this does anything, yield break should happen right after
            Tuple<CoroutineWrapper, OneOf<Terrain.Position, SpriteSorter>> nextCommand = executeCommandQueue.Dequeue();
            this.ExecutingBehavior = nextCommand.Item1;
            this.executeDirective = nextCommand.Item2;
            ExecutingBehavior.Start();
        }
    }
    public void CommandStation(Vector2Int directive) {
        stationDirective = terrain.CellCenter(directive);
        ClearFocus();
        State = CommandType.Station;
    }

    public void DisableFollowOffensive() {
        if (State == CommandType.FollowOffensive) State = CommandType.Follow;
    }
    public bool EnableFollowOffensive() {
        if (!general.hasAttack) throw new InvalidOperationException(species + " cannot attack");
        if (State == CommandType.FollowOffensive) {
            attackDirective = null;
            return true;
        } else if (State == CommandType.Follow) {
            State = CommandType.FollowOffensive;
            return true;
        } else return false;
    }
    public void EnableFollowOffensiveNoTarget() {
        Transform threat = NearestThreat();
        if (threat == null || !EnableFollowOffensive()) return;
        Focus = threat;
    }
    public void EnableFollowOffensiveWithTarget(Transform target) {
        if (attackDirective != null) {
            Debug.Log("But " + species.gameObject + " is already attacking " + attackDirective.gameObject);
            return;
        }
        if (!EnableFollowOffensive()) return;
        Debug.Log(species.gameObject + " is following attack directive");
        attackDirective = target;
        TryIndicateAttack(attackDirective, true);
        // If attackDirective already exists, it will be updated, but Focus will not necessarily.
        // If attackDirective cannot be seen and there is no Focus, Investigation will be updated.
    }
    public void TryIndicateAttack(Transform assailant, bool forceUpdateInvestigation) {
        if (Busy) return;
        bool canSee = CanSee(assailant);
        if (canSee) IndicateAttack(assailant);
        else IndicateAttack(assailant.position, forceUpdateInvestigation);
    }
    private void IndicateAttack(Transform assailant) { // initiates Focused
        if (general.hasAttack && !Focused) Focus = assailant;
    }
    private void IndicateAttack(Vector3 source, bool forceUpdateInvestigation) { // initiates Investigating
        if (general.hasAttack && !Focused &&
            (forceUpdateInvestigation || !Investigating || (transform.position - source).sqrMagnitude <
                               (transform.position - Investigation)?.sqrMagnitude))
            Investigation = source;
    }

    ///////////
    // BEHAVIOR

    protected CoroutineWrapper TrekkingBehavior;
    public IEnumerator TrekkingBehaviorE() {
        for (int i = 0; i < 10_000; i++) {
            if (Investigating) {
                pathfinding.MoveToward((Vector3)Investigation);
                yield return new WaitForSeconds(general.reconsiderRateTarget);
                if (Investigation is Vector3 investigation && (investigation - transform.position).magnitude <
                        general.reconsiderRateTarget * movement.Speed) { // arrived at point, found nothing
                    DisableFollowOffensive();
                    Investigation = null;
                }
            } else switch (state) {
                case CommandType.Roam:
                    yield return pathfinding.Roam();
                break;
                case CommandType.Follow:
                    yield return pathfinding.Follow(followDirective);
                break;
                case CommandType.FollowOffensive:
                    if (!badState && !stateIsDirty) Debug.LogError(species + ": FollowOffensive state must have Focus or Investigation. Please call UpdateFollowOffensive()");
                    badState = true;
                    yield return null;
                break;
                case CommandType.Station:
                    yield return pathfinding.ApproachThenIdle(stationDirective, 1f / CharacterController.subGridUnit);
                break;
                case CommandType.Pair:
                    yield return pathfinding.ApproachThenIdle(pairDirective.transform.position, movement.personalBubble);
                break;
                default:
                    Debug.LogError("Weird state: " + state);
                    yield break;
            }
        }
        Debug.LogError("Forgot to add a yield return on some branch :P");
    }

    public bool CanSee(Transform seen) => Will.CanSee(transform.position, seen);
    protected bool IsThreat(Transform threat) => Will.IsThreat(team, transform.position, threat);
    public Transform NearestThreat() => Will.NearestThreat(team, transform.position);
    public Transform NearestThreat(Func<Collider2D, bool> filter) => Will.NearestThreat(team, transform.position, filter);

    // Call when in FollowOffensive but not Focused or Investigating.
    // Assumes already checked attackDirective still alive.
    protected void UpdateFollowOffensive() {
        if (attackDirective == null) {
            Focus = NearestThreat(); // no target
            if (Focus == null) DisableFollowOffensive();
        } else if (GetComponentStrict<Team>().SameTeam(attackDirective)) {
            DisableFollowOffensive();
        } else if (CanSee(attackDirective)) {
            Focus = attackDirective; // found target
        } else {
            if (!Investigating) DisableFollowOffensive(); // unless we have no leads
            Focus = null; // keep looking
        }
    }

    ////////////////
    // SANITY CHECKS

    public void Update() {
        CleanUpState();
        if (stateIsDirty) {
            stateIsDirty = false;
            OnStateChange();
        }
        if (controlOverride == null) StateAssumptions(); // put off sanity checks if overriden
    }

    public void CleanUpState() {
        if (focus == null && !ReferenceEquals(focus, null)) {
            Debug.Log("Cleanup pre-check: focus died");
            Focus = null;
            TriggerStateChange();
        }
        if (attackDirective == null && !ReferenceEquals(attackDirective, null)) {
            Debug.Log("Cleanup pre-check: attackDirective died");
            DisableFollowOffensive();
            Focus = null;
        }
        if (executeDirective != null && executeDirective.WhichType == typeof(SpriteSorter)
                && (SpriteSorter)executeDirective == null) {
            Debug.Log("Cleanup pre-check: executeDirective died");
            RequestFollow();
        }
        if (pairDirective == null && !ReferenceEquals(pairDirective, null)) {
            Debug.Log("Cleanup pre-check: pairDirective died");
            RequestFollow();
        }
        if (controlOverride == null && !ReferenceEquals(controlOverride, null)) {
            Debug.LogError("controlOverride died for some reason");
            ReleaseControl();
        }
        if (state == CommandType.FollowOffensive && focus == null && investigation == null) {
            Debug.Log("Cleanup post-check: follow offensive lost target");
            UpdateFollowOffensive();
        }
    }

    // Check for things that should never happen
    public void StateAssumptions() {
        // Coroutines
        if (focus == null && FocusedBehavior.IsRunning) {
            Debug.LogError("Focus null but FocusedBehavior running");
            FocusedBehavior.Stop();
        }
        if (focus != null && TrekkingBehavior.IsRunning) {
            Debug.LogError("Focus nonnull but TrekkingBehavior running");
            TrekkingBehavior.Stop();
        }
        if (Scanning == false && ScanningBehavior.IsRunning) {
            Debug.LogError("Scanning false but ScanningBehavior running");
            ScanningBehavior.Stop();
        }
        if (state != CommandType.Execute && (ExecutingBehavior != null)) {
            Debug.LogError("Should not have ExecutingBehavior while state is " + State);
            ExecutingBehavior.Stop();
            ExecutingBehavior = null;
        }
        if (Busy && (FocusedBehavior.IsRunning || ScanningBehavior.IsRunning ||
                (TrekkingBehavior.IsRunning && State != CommandType.Pair))) {
            Debug.LogError("Busy but " + (FocusedBehavior.IsRunning ? "FocusedBehavior " : null)
                + (TrekkingBehavior.IsRunning ? "TrekkingBehavior " : null)
                + (ScanningBehavior.IsRunning ? "ScanningBehavior " : null) + "is running");
            FocusedBehavior.Stop();
            TrekkingBehavior.Stop();
            ScanningBehavior.Stop();
        }
        int runningBehaviors = (FocusedBehavior.IsRunning ? 1 : 0) + (TrekkingBehavior.IsRunning ? 1 : 0)
                + (ExecutingBehavior != null && ExecutingBehavior.IsRunning ? 1 : 0)
                + (State == CommandType.Faint ? 1 : 0);
        if (runningBehaviors != 1) {
            Debug.LogError("Exactly one of these must be running, but FocusedBehavior is"
                + (FocusedBehavior.IsRunning ? null : " not") + ", TrekkingBehavior is"
                + (TrekkingBehavior.IsRunning ? null : " not") + ", ExecutingBehavior is"
                + (ExecutingBehavior != null && ExecutingBehavior.IsRunning ? null : " not")
                + ", and State == Faint is " + (State == CommandType.Faint));
            RequestFollow(); // reset to most helpful default
        }
        // Directives
        if (state == CommandType.Follow && followDirective == null) {
            Debug.LogError("Following without followDirective");
            State = CommandType.Roam;
        }
        if (state == CommandType.Station && stationDirective == Vector3.zero) {
            Debug.LogError("Stationed without stationDirective");
            State = CommandType.Roam;
        }
        if (state == CommandType.Execute && executeDirective == null) {
            Debug.LogError("Executing without executeDirective");
            State = CommandType.Roam;
        }
        if (state == CommandType.Execute && (ExecutingBehavior == null || !ExecutingBehavior.IsRunning)) {
            Debug.LogError("Executing without ExecutingBehavior");
            State = CommandType.Roam;
        }
        if (state == CommandType.Execute && followDirective == null) {
            Debug.LogError("Executing without subsequent followDirective");
        }
        if (state == CommandType.Pair && pairDirective == null) {
            Debug.LogError("Paired without pairDirective");
            State = CommandType.Roam;
        }
        // Invalid focus combinations
        if (focus != null && investigation != null) {
            Debug.LogError("Focus and investigation at the same time: " + focus + " " + investigation);
            Investigation = null;
        }
        // I didn't bother to ensure Pair and Investigation cannot happen simultaneously
        // so Pair is not checked here
        if ((State == CommandType.Execute || State == CommandType.Faint)
            && (focus != null || investigation != null)) {
            Debug.LogError("Should not have focus or investigation while in state "
                + State + ": " + focus + " " + investigation);
            if (focus != null) Focus = null;
            if (investigation != null) Investigation = null;
        }
        badState = false;
    }
}