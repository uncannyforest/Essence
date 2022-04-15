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

    private CreatureState oldState;
    public CreatureState state { get; private set; } = CreatureState.Command(Command.Roam());
    public bool TryUpdateState(Senses input, int logLevel = 0) {
        OneOf<CreatureState, string> result = Will.Decide(state, input);
        if (result.Is(out CreatureState newState)) {
            CreatureState oldState = state;
            state = newState;
            TriggerStateChange(oldState);
            return true;
        } else if (result.Is(out string error)) {
            if (logLevel == 0) Debug.Log(species.name + ": " + error);
            else if (logLevel == 1) Debug.LogWarning(species.name + ": " + error);
            else if (logLevel == 2) Debug.LogError(species.name + ": " + error);
            return false;
        }
        return false;
    }

    public Transform Focus {
        get => state.characterFocus.Or(null);
        set {
            if (Focus == null && value != null) SetFocus(value);
            if (Focus != null && value == null) RemoveFocus();
        }
    }
    public bool Focused {
        get => state.characterFocus.HasValue;
    }
    public Vector3? Investigation { // trying to focus but cannot see
        get => state.investigation;
    }
    public bool Investigating { // trying to focus but cannot see
        get => Investigation != null;
    }
    public bool Busy { // not available to focus
        get => state.type == CreatureStateType.Execute || state.type == CreatureStateType.Pair || state.type == CreatureStateType.Faint;
    }
    public bool TrekkingFree { // available for pairing
        get => !Focused && !Busy;
    }
    protected bool Scanning {
        get => state.type.IsScanning();
    }
    private bool stateIsDirty = false;
    private MonoBehaviour controlOverride { get => state.ControlOverride; } // character controller fully controlled by another script
    private RunOnce investigationCancel = null;
    public Transform followDirective { get => state.command?.followDirective.Or(null); }
    protected Vector3 stationDirective { get => state.command?.stationDirective ?? Vector3.zero; }
    public OneOf<Terrain.Position, SpriteSorter> executeDirective { 
        get {
            if (state.command?.executeDirective is LegacyBehaviorNode executeDirective) {
                return executeDirective.target;
            } else if (state.command?.executeDirective is QueueOperator executeCommandQueue) {
                return executeCommandQueue.DeprecatedTargetAccessor;
            } else if (state.command?.executeDirective == null) {
                return null;
            } else {
                throw new NotImplementedException();
            }
        }
    }
    protected QueueOperator executeCommandQueue {
        get {
            if (state.command?.executeDirective is QueueOperator executeCommandQueue)
                return executeCommandQueue;
            else return null;
        }
    }
    protected Transform pairDirective { get => state.pairDirective.Or(null); }
    protected Creature focusOrExecuteDirectiveIsPair { get => state.focusIsPair.Or(null); }

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
    protected CoroutineWrapper ExecutingBehavior { get => state.command?.executeDirective?.coroutine; }

    protected T GetComponent<T>() => species.GetComponent<T>();
    protected T GetComponentStrict<T>() => species.GetComponentStrict<T>();
    virtual protected void OnHealthReachedZero() => GameObject.Destroy(creature.gameObject);

    /////////////////////////
    // STATE UPDATE FUNCTIONS

    public void DebugLogStateChange(bool triggerOnly) {
        string stateChangeText = triggerOnly ? ") changed state to " : ") processed state change to ";
        string result = species + " (team " + GetComponentStrict<Team>().TeamId + stateChangeText + state;
        Debug.Log(result);
    }
    public void TriggerStateChange(CreatureState oldState) {
        DebugLogStateChange(true);
        if (!stateIsDirty) this.oldState = oldState;
        stateIsDirty = true;
    }
    protected void OnStateChange() {
        DebugLogStateChange(false);
        if (state.type == CreatureStateType.Override) {
            ScanningBehavior.Stop();
            FocusedBehavior.Stop();
            TrekkingBehavior.Stop();
            ExecutingBehavior?.Stop();
            
            return;
        }

        // resolve unstable states
        if (state.type == CreatureStateType.PassiveCommand && state.followOffensive) {
            UpdateFollowOffensive();
            return;
        }

        // changes resulting from transitions
        if (state.type == CreatureStateType.Faint) movement.Idle();
        if (oldState.investigation != state.investigation) ChangedInvestigation(state.investigation != null);
        if (oldState.type == CreatureStateType.Pair && state.type != CreatureStateType.Pair) {
            Creature master = oldState.pairDirective.Value.GetComponent<Creature>();
            if (master != null) master.EndPairRequest();
        }
        if (oldState.focusIsPair.HasValue && !state.focusIsPair.HasValue) {
            Creature subject = oldState.focusIsPair.Value;
            if (subject != null) subject.EndPairCommand(transform);
        }

        // run coroutines
        FocusedBehavior.RunIf(Focused);
        ScanningBehavior.RunIf(Scanning);
        TrekkingBehavior.RunIf(TrekkingFree || state.type == CreatureStateType.Pair);
        ExecutingBehavior?.RunIf(state.type == CreatureStateType.Execute);
    }

    public void RequestFollow() {
        new Senses() { command = Command.RequestFollow() }.TryUpdateCreature(creature);
        if (state.command?.type == CommandType.Follow) {
            WorldInteraction playerInterface = followDirective.GetComponentStrict<PlayerCharacter>().Interaction;
            playerInterface.EnqueueFollowing(creature);
        }
    }

    // param recipient may be null, which is a no-op
    protected Transform RequestPair(Creature recipient) {
        if (recipient?.TryPair(transform) == true) {
            new Senses {
                environment = new Senses.Environment() {
                    characterFocus = Delta<Transform>.Add(recipient.transform),
                    focusIsPair = Optional.Of(recipient)
                }
            }.TryUpdateCreature(creature, 2);
            return recipient.transform;
        }
        else return null;
    }

    public void CompleteExecution() {
        ExecutingBehavior.Stop();
        bool complete = !executeCommandQueue.Pop();
        if (complete) RequestFollow();
        else ExecutingBehavior.Start();
    }

    private void SetFocus(Transform focus) => new Senses() {
        environment = new Senses.Environment() {
            characterFocus = Delta<Transform>.Add(focus)
        }
    }.TryUpdateCreature(creature);

    private void RemoveFocus() => new Senses() {
        environment = new Senses.Environment() {
            characterFocus = Delta<Transform>.Remove()
        }
    }.TryUpdateCreature(creature);
    
    public void RemoveInvestigation() => new Senses() {
        environment = new Senses.Environment() { removeInvestigation = true }
    }.TryUpdateCreature(creature);

    public void ChangedInvestigation(bool investigating) {
        investigationCancel?.Stop();
        if (investigating) investigationCancel =
            RunOnce.Run(species, Creature.neighborhood * movement.Speed, RemoveInvestigation);
    }

    public void DisableFollowOffensive() => new Senses() {
        hint = new Hint() { generallyOffensive = false }
    }.TryUpdateCreature(creature);

    public void UpdateFollowOffensive() {
        Transform threat = NearestThreat();
        if (threat == null) DisableFollowOffensive();
        Focus = threat;
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
                    RemoveInvestigation();
                }
            } else if (state.type == CreatureStateType.Pair) {
                yield return pathfinding.ApproachThenIdle(pairDirective.transform.position, movement.personalBubble);
            } else switch (state.command?.type) {
                case CommandType.Roam:
                    yield return pathfinding.Roam();
                break;
                case CommandType.Follow:
                    yield return pathfinding.Follow(followDirective);
                break;
                case CommandType.Station:
                    yield return pathfinding.ApproachThenIdle(stationDirective, 1f / CharacterController.subGridUnit);
                break;
                default:
                    Debug.LogError("Weird state: " + state);
                    yield break;
            }
        }
        Debug.LogError("Forgot to add a yield return on some branch :P");
    }

    protected bool IsThreat(Transform threat) => Will.IsThreat(team, transform.position, threat);
    public Transform NearestThreat() => Will.NearestThreat(team, transform.position);
    public Transform NearestThreat(Func<Collider2D, bool> filter) => Will.NearestThreat(team, transform.position, filter);

    ////////////////
    // SANITY CHECKS

    public void Update() {
        CleanUpState();
        if (stateIsDirty) {
            stateIsDirty = false;
            OnStateChange();
        }
        if (state.type != CreatureStateType.Override) StateAssumptions(); // put off sanity checks if overriden
    }

    public void CleanUpState() {
        if (Focus == null && !ReferenceEquals(Focus, null)) {
            Debug.Log("Cleanup pre-check: focus died");
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
        if (state.type == CreatureStateType.Override && controlOverride == null) {
            Debug.LogError("controlOverride died for some reason");
            creature.ReleaseControl();
        }
        if (state.followOffensive && Focus == null) {
            Debug.Log("Cleanup post-check: follow offensive lost target");
            UpdateFollowOffensive();
        }
    }

    // Check for things that should never happen
    public void StateAssumptions() {
        // Coroutines
        if (Focus == null && FocusedBehavior.IsRunning) {
            Debug.LogError("Focus null but FocusedBehavior running");
            FocusedBehavior.Stop();
        }
        if (Focus != null && TrekkingBehavior.IsRunning) {
            Debug.LogError("Focus nonnull but TrekkingBehavior running");
            TrekkingBehavior.Stop();
        }
        if (Scanning == false && ScanningBehavior.IsRunning) {
            Debug.LogError("Scanning false but ScanningBehavior running");
            ScanningBehavior.Stop();
        }
        if (state.type != CreatureStateType.Execute && (ExecutingBehavior != null)) {
            Debug.LogWarning("Should not have ExecutingBehavior while state is " + state.type);
            ExecutingBehavior.Stop();
        }
        if (Busy && (FocusedBehavior.IsRunning || ScanningBehavior.IsRunning ||
                (TrekkingBehavior.IsRunning && state.type != CreatureStateType.Pair))) {
            Debug.LogError("Busy but " + (FocusedBehavior.IsRunning ? "FocusedBehavior " : null)
                + (TrekkingBehavior.IsRunning ? "TrekkingBehavior " : null)
                + (ScanningBehavior.IsRunning ? "ScanningBehavior " : null) + "is running");
            FocusedBehavior.Stop();
            TrekkingBehavior.Stop();
            ScanningBehavior.Stop();
        }
        int runningBehaviors = (FocusedBehavior.IsRunning ? 1 : 0) + (TrekkingBehavior.IsRunning ? 1 : 0)
                + (ExecutingBehavior != null && ExecutingBehavior.IsRunning ? 1 : 0)
                + (state.type == CreatureStateType.Faint ? 1 : 0);
        if (runningBehaviors != 1) {
            Debug.LogError("Exactly one of these must be running, but FocusedBehavior is"
                + (FocusedBehavior.IsRunning ? null : " not") + ", TrekkingBehavior is"
                + (TrekkingBehavior.IsRunning ? null : " not") + ", ExecutingBehavior is"
                + (ExecutingBehavior != null && ExecutingBehavior.IsRunning ? null : " not")
                + ", and State == Faint is " + (state.type == CreatureStateType.Faint));
            RequestFollow(); // reset to most helpful default
        }
        // Directives
        if (state.command?.type == CommandType.Follow && followDirective == null) {
            Debug.LogError("Following without followDirective");
            creature.CommandRoam();
        }
        if (state.command?.type == CommandType.Station && stationDirective == Vector3.zero) {
            Debug.LogError("Stationed without stationDirective");
            creature.CommandRoam();
        }
        if (state.command?.type == CommandType.Execute && executeDirective == null) {
            Debug.LogError("Executing without executeDirective");
            creature.CommandRoam();
        }
        if (state.command?.type == CommandType.Execute && (ExecutingBehavior == null || !ExecutingBehavior.IsRunning)) {
            Debug.LogError("Executing without ExecutingBehavior");
            creature.CommandRoam();
        }
        if (state.command?.type == CommandType.Execute && followDirective == null) {
            Debug.LogError("Executing without subsequent followDirective");
        }
        if (state.type == CreatureStateType.Pair && pairDirective == null) {
            Debug.LogError("Paired without pairDirective");
            creature.CommandRoam();
        }
        // Invalid focus combinations
        if (Focus != null && Investigation != null) {
            Debug.LogError("Focus and investigation at the same time: " + Focus + " " + Investigation);
            RemoveInvestigation();
        }
        // I didn't bother to ensure Pair and Investigation cannot happen simultaneously
        // so Pair is not checked here
        if ((state.type == CreatureStateType.Execute || state.type == CreatureStateType.Faint)
            && (Focus != null || Investigation != null)) {
            Debug.LogError("Should not have focus or investigation while in state "
                + state.type + ": " + Focus + " " + Investigation);
            if (Focus != null) Focus = null;
            if (Investigation != null) RemoveInvestigation();
        }
    }
}