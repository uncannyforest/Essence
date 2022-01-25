using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum CreatureState {
    Roam,
    Follow,
    FollowOffensive,
    Station,
    Execute,
    Pair
}

[Serializable]
public class BrainConfig {
    public AIDirections numMovementDirections;
    public float movementSpeed;
    public float roamRestingFraction = .5f;
    public float reconsiderRateRoam = 5;
    public float reconsiderRateTarget = 2.5f;
    public float reconsiderRatePursuit = 1f;
    public float scanningRate = 1f;
    public bool scanForFocusWhenFollowing = true;
    public bool hasAttack = false;
    public float timidity = .75f;   

    public enum AIDirections {
        Infinite,
        Four,
        Eight,
        Twelve
    }
    public static Dictionary<AIDirections, Vector2[]> AIDirectionVectors = new Dictionary<AIDirections, Vector2[]>() {
        [AIDirections.Four] = new Vector2[] {
            Vct.F(0.7071067812f, 0.7071067812f),
        },
        [AIDirections.Twelve] = new Vector2[] {
            Vct.F(1f, 0.2679491924f), // tan(15)
            Vct.F(0.7071067812f, 0.7071067812f),
            Vct.F(0.2679491924f, 1f)
        }
    };
}

//                 | Scanning | Investigating | Focused | attackDir. | executeDir. | pairDir.
// ----------------+----------+---------------+---------+------------+-------------+----------
// Roam            | ALWAYS   | defensively   | defens. |            |             |
// Follow          | sffwf.  | defensively   | defens. |            |             |
// FollowOffensive | ALWAYS   | ifnt dir/vis  | ifnt dv | if directd |             |
// Station         | ALWAYS   | defensively   | defens. |            |             |
// Execute         |          |               |         |            | ALWAYS      |
// Pair            |          |               |         |            |             | ALWAYS

public class Brain {
    public BrainConfig general;
    protected Species species;
    protected Creature creature;
    protected Terrain terrain;
    protected Transform grid { get => terrain.transform; }
    protected int team { get => GetComponentStrict<Team>().TeamId; }
    private GoodTaste taste;

    protected Vector2[] aiDirections { get => BrainConfig.AIDirectionVectors[general.numMovementDirections]; }
    protected Transform transform { get => species.transform; }

    public Vector2 velocity {
        get => creature.InputVelocity;
        set => creature.InputVelocity = value;
    }

    ///////////////////
    // STATE PROPERTIES

    public CreatureState State {
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
            if (!ReferenceEquals(value, null)) FocusedBehavior.Start();
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
                RunOnce.Run(species, Creature.neighborhood * general.movementSpeed,
                    () => Investigation = null);
            TriggerStateChange();
        }
    }
    public bool Investigating { // trying to focus but cannot see
        get => investigation != null;
    }
    public bool Busy { // not available to focus
        get => state == CreatureState.Execute || state == CreatureState.Pair;
    }
    protected bool Scanning {
        get =>
            state == CreatureState.Roam ||
            state == CreatureState.Station ||
            state == CreatureState.FollowOffensive ||
            (state == CreatureState.Follow && general.scanForFocusWhenFollowing);
    }
    protected bool Listening {
        get => taste?.Listening == true;
    }
    private CreatureState state = CreatureState.Roam;
    private bool badState = false; // wait one frame for CleanUpState()
    private bool stateIsDirty = false;
    private Transform focus = null; // when Focused
    private Vector2? investigation = null; // focus character not identified, only location
    private RunOnce investigationCancel = null;
    protected Transform threat = null; // for Follow
    protected Transform followDirective = null; // for Follow
    protected Vector3 stationDirective = Vector3.zero; // for Station
    protected Transform attackDirective = null; // for FollowOffensive, can be null
    protected OneOf<Terrain.Position, SpriteSorter> executeDirective = null; // for Execute

    /////////////////////////////////////////////
    // INITIALIZATION, VIRTUAL, & UTILITY METHODS

    public Brain(Species species, BrainConfig general) {
        this.species = species;
        this.general = general;
    }
    public Brain InitializeAll() {
        creature = GetComponentStrict<Creature>();
        terrain = GameObject.FindObjectOfType<Terrain>();
        taste = GetComponent<GoodTaste>();
        if (taste != null) taste.TamerChanged += TriggerStateChange;
        Health health = GetComponent<Health>();
        if (health != null) health.ReachedZero += HandleDeath;
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
    private void HandleDeath() => GameObject.Destroy(creature.gameObject);
    protected Vector2 RandomVelocity() {
        Vector2 randomFromList = aiDirections[Random.Range(0, aiDirections.Length)];
        return Randoms.RightAngleRotation(randomFromList) * general.movementSpeed;
    }
    protected Vector2 IndexedVelocity(Vector2 targetDirection) {
        // round instead of floor if aiDirections.Length were even.
        int index = Mathf.FloorToInt((Vector2.SignedAngle(Vector3.right, targetDirection) + 360) % 360 / (90 / aiDirections.Length));
        int rotation = index / aiDirections.Length;
        int subIndex = index % aiDirections.Length;
        return aiDirections[subIndex].RotateRightAngles(rotation) * general.movementSpeed;
    }

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
        if (Listening) result += "; listening";
        Debug.Log(result);
    }
    public void TriggerStateChange() {
        DebugLogStateChange(true);
        stateIsDirty = true;
    }
    protected void OnStateChange() {
        DebugLogStateChange(false);
        if (Listening) {
            velocity = Vector2.zero;
            ScanningBehavior.Stop();
            FocusedBehavior.Stop();
            TrekkingBehavior.Stop();
            ExecutingBehavior?.Stop();
            return;
        }
        if (Busy) ClearFocus();
        ScanningBehavior.RunIf(Scanning);
        TrekkingBehavior.RunIf(!Focused && !Busy);
        ExecutingBehavior?.RunIf(State == CreatureState.Execute);
        if (State != CreatureState.Execute) {
            executeDirective = null;
            ExecutingBehavior = null;
        }
    }
    protected void ClearDirectives() {
        followDirective = null;
        attackDirective = null;
        stationDirective = Vector2.zero;
    }

    public void CommandFollow(Transform directive) {
        ClearDirectives();
        followDirective = directive;
        ClearFocus();
        State = CreatureState.Follow;
    }
    protected void RequestFollow() { // initiated by creature
        if (followDirective == null) throw new InvalidOperationException("No follow directive to return to");
        WorldInteraction playerInterface = followDirective.GetComponentStrict<PlayerCharacter>().Interaction;
        State = CreatureState.Follow;
        playerInterface.EnqueueFollowing(creature);
    }
    public void CommandExecute(CoroutineWrapper executingBehavior,
            OneOf<Terrain.Position, SpriteSorter> directive) {
        this.ExecutingBehavior = executingBehavior;
        this.executeDirective = directive;
        State = CreatureState.Execute;
    }
    public void CommandStation(Vector2Int directive) {
        ClearDirectives();
        stationDirective = terrain.CellCenter(directive);
        ClearFocus();
        State = CreatureState.Station;
    }

    public void DisableFollowOffensive() {
        if (State == CreatureState.FollowOffensive) {
            attackDirective = null;
            State = CreatureState.Follow;
            Debug.Log("DISABLED");
        }
    }
    public bool EnableFollowOffensive() {
        if (!general.hasAttack) throw new InvalidOperationException(species + " cannot attack");
        if (State == CreatureState.FollowOffensive) {
            attackDirective = null;
            return true;
        } else if (State == CreatureState.Follow) {
            attackDirective = null;
            State = CreatureState.FollowOffensive;
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

    // Runs when not Focused
    protected CoroutineWrapper TrekkingBehavior;
    public IEnumerator TrekkingBehaviorE() {
        if (GetComponent<Team>().TeamId == 1) Debug.Log("Starting trek");
        Vector3 targetDirection;

        for (int i = 0; i < 10_000; i++) {
            if (Investigating) {
                velocity = IndexedVelocity((Vector3)Investigation - transform.position);
                yield return new WaitForSeconds(general.reconsiderRatePursuit);
                if (Investigation is Vector3 investigation && (investigation - transform.position).magnitude <
                        general.reconsiderRatePursuit * general.movementSpeed) { // arrived at point, found nothing
                    DisableFollowOffensive();
                    Investigation = null;
                }
            } else switch (state) {
                case CreatureState.Roam:
                    if (Random.value < general.roamRestingFraction) velocity = Vector2.zero;
                    else velocity = RandomVelocity();
                    yield return new WaitForSeconds(Random.value * general.reconsiderRateRoam);
                break;
                case CreatureState.Follow:
                    targetDirection = FollowTargetDirection(followDirective.position);
                    velocity = IndexedVelocity(targetDirection);
                    yield return new WaitForSeconds(Random.value * general.reconsiderRateTarget);
                break;
                case CreatureState.FollowOffensive:
                    if (!badState && !stateIsDirty) Debug.LogError(species + ": FollowOffensive state must have Focus or Investigation. Please call UpdateFollowOffensive()");
                    badState = true;
                    yield return null;
                break;
                case CreatureState.Station:
                    targetDirection = stationDirective - transform.position;
                    if (targetDirection.magnitude < 1f / Creature.subGridUnit) {
                        velocity = Vector2.zero;
                        yield return new WaitForSeconds(general.reconsiderRateTarget);
                        continue;
                    }
                    velocity = IndexedVelocity(targetDirection);
                    if (targetDirection.magnitude > general.reconsiderRateTarget * general.movementSpeed)
                        yield return new WaitForSeconds(Random.value * general.reconsiderRateTarget);
                    else yield return null;
                break;
                default:
                    Debug.LogError("Weird state: " + state);
                    yield break;
            }
        }
        Debug.LogError("Forgot to add a yield return on some branch :P");
    }

    protected Vector3 FollowTargetDirection(Vector3 targetPosition) {
        Vector3 toTarget = targetPosition - transform.position;

        Transform nearestThreat = NearestThreat();
        if (nearestThreat == null) return toTarget;
        Vector3 toThreat = nearestThreat.position - transform.position;
        Vector3 toThreatCorrected = toThreat * toTarget.sqrMagnitude / toThreat.sqrMagnitude * general.timidity;
        return toTarget - toThreatCorrected;
    }

    public virtual bool CanSee(Transform seen) {
        if (Vector2.Distance(transform.position, seen.position) > Creature.neighborhood) {
            if (GetComponentStrict<Team>().TeamId == 1) Debug.DrawLine(transform.position, (seen.position - transform.position).normalized * Creature.neighborhood + transform.position, Color.red, 1f);
            return false;
        } else if (GetComponentStrict<Team>().TeamId == 1) Debug.DrawLine(transform.position, (seen.position - transform.position).normalized * Creature.neighborhood + transform.position, Color.yellow, 1f);
        SpriteSorter seenSprite = seen.GetComponentInChildren<SpriteSorter>();
        if (seenSprite == null)
            return true;
        else {
            bool result = terrain.concealment.CanSee(transform, seenSprite);
            if (!result && !TextDisplay.I.DisplayedYet("hiding")) TextDisplay.I.CheckpointInfo("hiding",
                "The <color=creature>" + creature.creatureName + "</color> nearby cannot see you.  You are hidden from enemies when deep in trees and buildings, unless they get close.");
            return result;
        }
    }

    // Sanity check for NearestThreat to avoid contradiction
    // OverlapCircleAll may produce colliders with center slightly outside Creature.neighborhood
    protected bool IsThreat(Transform threat) =>
        !GetComponentStrict<Team>().SameTeam(threat) && CanSee(threat);

    protected Transform NearestThreat() => NearestThreat(null);
    protected Transform NearestThreat(Func<Collider2D, bool> filter) {
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(transform.position, Creature.neighborhood, LayerMask.GetMask("Player", "HealthCreature"));
        List<Transform> threats = new List<Transform>();
        foreach (Collider2D character in charactersNearby) {
            if (IsThreat(character.transform) && (filter?.Invoke(character) != false))
                if (character.GetComponent<Creature>()?.brainConfig?.hasAttack == true ||
                        character.GetComponent<PlayerCharacter>() != null)
                    threats.Add(character.transform);
        }
        if (threats.Count == 0) return null;
        return threats.MinBy(threat => (threat.position - transform.position).sqrMagnitude);
    }

    // Call when in FollowOffensive but not Focused or Investigating.
    protected void UpdateFollowOffensive() {
        if (!ReferenceEquals(attackDirective, null) && attackDirective == null) { // target died
            Debug.Log("KILLED IT!");
            DisableFollowOffensive();
            Focus = null;
        } else if (attackDirective == null) {
            Focus = NearestThreat(); // no target
            if (Focus == null) DisableFollowOffensive();
        } else if (CanSee(attackDirective)) Focus = attackDirective; // found target
        else {
            if (!Investigating) DisableFollowOffensive(); // unless we have no leads
            Focus = null; // keep looking
        }
    }

    ////////////////
    // SANITY CHECKS

    public void Update() {
        StatePreChecks();
        StatePostChecks();
        if (stateIsDirty) {
            stateIsDirty = false;
            OnStateChange();
        }
        if (!Listening) StateAssumptions(); // Listening overrides everything, so put off sanity checks
    }

    // Clean up state
    public void StatePreChecks() {
        if (focus == null && !ReferenceEquals(focus, null)) {
            Debug.Log("Cleanup pre-check: focus died");
            Focus = null;
            TriggerStateChange();
        }
        if (attackDirective == null && !ReferenceEquals(attackDirective, null)) {
            Debug.Log("Cleanup pre-check: attackDirective died");
            DisableFollowOffensive();
        }
        if (executeDirective != null && executeDirective.WhichType == typeof(SpriteSorter)
                && (SpriteSorter)executeDirective == null) {
            Debug.Log("Cleanup pre-check: executeDirective died");
            RequestFollow();
        }
    }

    // Clean up state
    public void StatePostChecks() {
        if (state == CreatureState.FollowOffensive && focus == null && investigation == null) {
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
        if (state != CreatureState.Execute && (ExecutingBehavior != null)) {
            Debug.LogError("Should not have ExecutingBehavior while state is " + State);
            ExecutingBehavior.Stop();
            ExecutingBehavior = null;
        }
        if (Busy && (FocusedBehavior.IsRunning || TrekkingBehavior.IsRunning || ScanningBehavior.IsRunning)) {
            Debug.LogError("Busy but " + (FocusedBehavior.IsRunning ? "FocusedBehavior " : null)
                + (TrekkingBehavior.IsRunning ? "TrekkingBehavior " : null)
                + (ScanningBehavior.IsRunning ? "ScanningBehavior " : null) + "is running");
            FocusedBehavior.Stop();
            TrekkingBehavior.Stop();
            ScanningBehavior.Stop();
        }
        int runningBehaviors = (FocusedBehavior.IsRunning ? 1 : 0) + (TrekkingBehavior.IsRunning ? 1 : 0)
                + (ExecutingBehavior != null && ExecutingBehavior.IsRunning ? 1 : 0);
        if (runningBehaviors != 1) {
            Debug.LogError("Exactly one of these must be running, but FocusedBehavior is"
                + (FocusedBehavior.IsRunning ? null : " not") + ", TrekkingBehavior is"
                + (TrekkingBehavior.IsRunning ? null : " not") + ", and ExecutingBehavior is"
                + (ExecutingBehavior != null && ExecutingBehavior.IsRunning ? null : " not"));
            // reset to most helpful default
            if (followDirective != null) RequestFollow();
            else State = CreatureState.Roam;
        }
        // Directives
        if (state == CreatureState.Follow && followDirective == null) {
            Debug.LogError("Following without followDirective");
            State = CreatureState.Roam;
        }
        if (state == CreatureState.Station && stationDirective == Vector3.zero) {
            Debug.LogError("Stationed without stationDirective");
            State = CreatureState.Roam;
        }
        if (state == CreatureState.Execute && executeDirective == null) {
            Debug.LogError("Executing without executeDirective");
            State = CreatureState.Roam;
        }
        if (state == CreatureState.Execute && (ExecutingBehavior == null || !ExecutingBehavior.IsRunning)) {
            Debug.LogError("Executing without ExecutingBehavior");
            State = CreatureState.Roam;
        }
        if (state == CreatureState.Execute && followDirective == null) {
            Debug.LogError("Executing without subsequent followDirective");
        }
        // Invalid focus combinations
        if (focus != null && investigation != null) {
            Debug.LogError("Focus and investigation at the same time: " + focus + " " + investigation);
            Investigation = null;
        }
        if (State == CreatureState.Execute && (focus != null || investigation != null)) {
            Debug.LogError("Should not have focus or investigation while Executing: " + focus + " " + investigation);
            if (focus != null) Focus = null;
            if (investigation != null) Investigation = null;
        }
        badState = false;
    }
}