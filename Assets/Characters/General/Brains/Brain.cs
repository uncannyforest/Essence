using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class BrainConfig {
    public bool canFaint = false;
    public float lureMaxTime = 10f;
    public float roamRestingFraction = .5f;
    public float reconsiderMaxRateRoam = 5;
    public float reconsiderRateFollow = 2f;
    public float reconsiderRateTarget = 1f;
    public float scanningRate = 1f;
    [Obsolete("No creatures should scan for focus while following")]
    public bool scanForFocusWhenFollowing = true;
    [Obsolete("All creatures should have an attack")]
    public bool hasAttack = false;
    public Land[] canClearObstacles = new Land[0];
    public Construction[] canClearConstruction = new Construction[0];
    public bool canClearFeatures = false;
    public float timidity = .75f;   

}

// Notes from the last refactor.
//
// Separate different functionality:
// 1. Inputs (Senses class): five types
//    - Player commands
//    - Player hints
//    - Same-team creature requests and messages
//    - Environmental input (e.g. enemies)
//    - Internal feedback ("am I succeeding at X")
//    Most calls to Brain will update an Input.
//    State change computation can be accomplished without committing a state modification.
//    So can immediately compute the state change even without its consequences running until the next frame.
// 2. State change (Will class):
//    The decision tree of the AI can be modeled by https://app.diagrams.net/#G12U12TJ4aRo3wOl9gePm-mq7hj-KayC94 .
//    Note that there are only nine different kinds of output behaviors, even though the decision tree is complex.
//    We want legal transitions to be relatively stateless, i.e., most states can transition to most states with few special cases.
// 3. Consequences of state change
//    Macro logic (Habit class):
//      Although most states can transition to most states, we still want to borrow a good State Machine principle:
//      Every state has OnEnter and OnExit logic that runs when entering and exiting the state.
//    Micro logic (BehaviorNode class):
//      We will introduce a concept of BehaviorNodes which are units of behavior,
//      and some BehaviorNodes can modify other BehaviorNodes for more complex behavior.

public class Brain {
    public BrainConfig general;
    public Species species;
    public Creature creature;
    public string legalName { get => creature.gameObject.name; }
    public Terrain terrain;
    protected Transform grid { get => terrain.transform; }
    public int teamId { get => GetComponentStrict<Team>().TeamId; }
    public Team team { get => GetComponentStrict<Team>(); }
    public CharacterController movement { get => creature.controller; }
    public Pathfinding pathfinding;
    public Resource resource;
    public Transform transform { get => species.transform; }

    protected T GetComponent<T>() => species.GetComponent<T>();
    protected T GetComponentStrict<T>() => species.GetComponentStrict<T>();
    virtual protected void OnHealthReachedZero() => new Senses() { faint = true }.TryUpdateCreature(creature);
    public RunOnce investigationCancel = null;

    ///////////////////
    // STATE PROPERTIES

    public CreatureState state { get; private set; } = CreatureState.PassiveCommand(PassiveCommand.Roam());
    private CreatureState oldState = CreatureState.WithControlOverride(CreatureState.PassiveCommand(PassiveCommand.Roam()), null);
    private bool stateIsDirty = true;
    protected TaskRunner RunningBehaviorTask;
    protected TaskRunner ScanningBehaviorTask;

    /////////////////////////////////////////////
    // INITIALIZATION, VIRTUAL, AND UTILITY METHODS FOR DERIVED CLASSES

    public Brain(Species species, BrainConfig general) {
        this.species = species;
        this.general = general;
        pathfinding = new Pathfinding(this);
        reproduction = new Reproduction(this);
    }
    public Brain InitializeAll() {
        creature = GetComponentStrict<Creature>();
        terrain = GameObject.FindObjectOfType<Terrain>();
        resource = GetComponentStrict<Resource>();
        Health health = GetComponent<Health>();
        if (health != null) {
            health.ReachedZero += OnHealthReachedZero;
        }
        ScanningBehaviorTask = new TaskRunner(ScanningBehavior, species);
        OnStateChange();
        Initialize();
        return this;
    }
    virtual protected void Initialize() {}
    public List<CreatureAction> Actions { get; protected set; } = new List<CreatureAction>();
    virtual public bool CanTame(Transform player) {
        if (GetComponent<Health>() == null) return Habitat?.CanTame() ?? false;
        else return state.type == CreatureStateType.Faint && (Habitat?.IsAphrodisiacPresent(Radius.Nearby) ?? false);
    }

    public WhyNot SufficientResource(int quantityNeeded = 1) =>
        resource.Has(quantityNeeded) ? (WhyNot) true : "insufficient_resource(" + quantityNeeded + ")";
    public WhyNot IsValidIfTerrain(Target t, LandFlags? land = null, ConstructionFlags? construction = null) =>
        t.WhichType == typeof(Character) ? true : terrain.IsValid((Terrain.Position)t, land, construction);

    public FlexSourceBehavior MainBehavior { get; protected set; } = new NullSourceBehavior();
    virtual public IEnumerator<YieldInstruction> FocusedBehavior() => MainBehavior.FocusedBehavior();
    virtual public WhyNot IsValidFocus(Transform characterFocus) => 
        characterFocus == null ? "null_focus" : MainBehavior.IsValidFocus(characterFocus);
    virtual public Optional<Transform> FindFocus() => Optional<Transform>.Empty();
    virtual public IEnumerator<YieldInstruction> UnblockSelf(Terrain.Position location) =>
        throw new NotImplementedException("Must implement if one can clear obstacles one cannot pass");
    public Habitat Habitat { get; protected set; } = null;
    public Lark Lark { get; protected set; } = Lark.None();
    public readonly Reproduction reproduction;

    virtual public void Melee(Transform target) {
        Health health = target.GetComponentStrict<Health>();
        health.Decrease(creature.stats.Str, transform);
        resource.Use();
        AttackTerraformSideEffect(Terrain.I.CellAt(target.position));
    }
    virtual public void AttackTerraformSideEffect(Vector2Int loc) {}
    virtual public IEnumerator<YieldInstruction> AttackCharacterBehavior(Transform f) =>
        from focus in Continually.For(f)
        where IsValidFocus(focus)                   .NegLog(legalName + " focus " + focus + " no longer valid")
        select pathfinding.Approach(focus, GlobalConfig.I.defaultMeleeReach)
            .Then(() => pathfinding.FaceAnd("Attack", focus, Melee));

    /////////////////////////
    // STATE UPDATE FUNCTIONS

    public bool TryUpdateState(Senses input, int logLevel = 0) {
        if (logLevel >= 0) Debug.Log(legalName + " (team " + GetComponentStrict<Team>().TeamId + ") state change input: " + input);
        OneOf<CreatureState, string> result = Will.Decide(state, input);
        if (result.Is(out CreatureState newState)) {
            CreatureState oldState = state;
            state = newState;
            TriggerStateChange(oldState);
            return true;
        } else if (result.Is(out string error)) {
            if (logLevel == 0) Debug.Log(legalName + ": " + error);
            else if (logLevel == 1) Debug.LogWarning(legalName + ": " + error);
            else if (logLevel == 2) Debug.LogError(legalName + ": " + error);
            return false;
        }
        return false;
    }

    public void DebugLogStateChange(bool triggerOnly) {
        string stateChangeText = triggerOnly ? ") changed state to " : ") processed state change to ";
        string result = legalName + " (team " + GetComponentStrict<Team>().TeamId + stateChangeText + state;
        Debug.Log(result);
    }

    public void TriggerStateChange(CreatureState oldState) {
        DebugLogStateChange(true);
        if (!stateIsDirty) this.oldState = oldState;
        stateIsDirty = true;
    }

    protected void OnStateChange() {
        CreatureState originalState = oldState;

        Habit newHabit = new Habit();
        while (stateIsDirty) {
            stateIsDirty = false;
            Habit oldHabit = Habit.Get(oldState, this);
            newHabit = Habit.Get(state, this);
            if (oldState.detailedType != state.detailedType) {
                oldHabit.OnExit(state);
                newHabit.OnEnter();
            } else {
                newHabit.OnUpdate();
            }
        }
        if (originalState.detailedType == state.detailedType && !newHabit.OnUpdate()) {
            Debug.LogError("Should not transition from state " + state.type + " to same state: " + state);
        }
        RunningBehaviorTask?.Stop();
        if (newHabit.HasRunBehavior) {
            RunningBehaviorTask = new TaskRunner(() => newHabit.RunBehavior(state, this, EndState), species);
            RunningBehaviorTask.Start();
        }

        DebugLogStateChange(false);

        ScanningBehaviorTask.RunIf(state.IsScanning);
    }

    public void EndState() => new Senses() {
        endState = true
    }.TryUpdateCreature(creature);

    private IEnumerator<YieldInstruction> ScanningBehavior() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);
            if (state.scanActivity?.command.type == PassiveCommandType.Follow)
                continue; // do not scan for focus or habitat while following
            Optional<Transform> maybeFocus = FindFocus();
            Optional<Vector2Int> maybeShelter = Optional.Empty<Vector2Int>();
            if (Habitat != null) maybeShelter = Habitat.FindShelter();

            if (maybeFocus.HasValue || maybeShelter.HasValue) {
                new Senses() {
                    environment = new Senses.Environment() {
                        characterFocus = maybeFocus,
                        shelter = maybeShelter
                    }
                }.TryUpdateCreature(creature);
            }
        }
    }

    /////////////////////////
    // AI SELF STATE CHANGES

    public void RequestFollow() {
        new Senses() { passiveCommand = PassiveCommand.RequestFollow() }.TryUpdateCreature(creature);
        if (state.scanActivity?.command.type == PassiveCommandType.Follow) {
            WorldInteraction playerInterface = state.scanActivity?.command.followDirective.Value.GetComponentStrict<PlayerCharacter>().Interaction;
            playerInterface.EnqueueFollowing(creature);
        }
    }

    // param recipient may be null, which is a no-op
    protected Optional<Transform> RequestPair(Creature recipient) {
        if (recipient?.TryPair(transform) == true) {
            new Senses {
                environment = new Senses.Environment() {
                    characterFocus = Optional.Of(recipient.transform),
                    focusIsPair = Optional.Of(recipient)
                }
            }.TryUpdateCreature(creature, 2);
            return Optional.Of(recipient.transform);
        }
        else return Optional<Transform>.Empty();
    }

    ////////////////
    // SANITY CHECKS

    public void Update() {
        if (state.scanActivity?.HasValidPosition == true) {
            Debug.DrawLine(transform.position, ((ScanActivity)state.scanActivity).GetPosition(), Color.white);
        }
        if (stateIsDirty) OnStateChange();
    }
}