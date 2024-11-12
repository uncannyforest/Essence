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
    public float lureMaxTime = 10f;
    public float roamRestingFraction = .5f;
    public float reconsiderMaxRateRoam = 5;
    public float reconsiderRateFollow = 2f;
    public float reconsiderRateTarget = 1f;
    public float scanningRate = 1f;
    public bool scanForFocusWhenFollowing = true;
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
    virtual protected void OnHealthReachedZero() => GameObject.Destroy(creature.gameObject);
    public RunOnce investigationCancel = null;

    ///////////////////
    // STATE PROPERTIES

    public CreatureState state { get; private set; } = CreatureState.Command(Command.Roam());
    private CreatureState oldState = CreatureState.WithControlOverride(CreatureState.Command(Command.Roam()), null);
    private bool stateIsDirty = true;
    protected TaskRunner RunningBehaviorTask;
    protected TaskRunner ScanningBehaviorTask;

    /////////////////////////////////////////////
    // INITIALIZATION AND VIRTUAL METHODS

    public Brain(Species species, BrainConfig general) {
        this.species = species;
        this.general = general;
        pathfinding = new Pathfinding(this);
    }
    public Brain InitializeAll() {
        creature = GetComponentStrict<Creature>();
        terrain = GameObject.FindObjectOfType<Terrain>();
        resource = GetComponent<Resource>();
        Health health = GetComponent<Health>();
        if (health != null) {
            health.ReachedZero += OnHealthReachedZero;
            faintCondition = () => state.type == CreatureStateType.Faint;
        }
        ScanningBehaviorTask = new TaskRunner(ScanningBehavior, species);
        OnStateChange();
        Initialize();
        return this;
    }
    virtual protected void Initialize() {}
    public List<CreatureAction> Actions { get; protected set; } = new List<CreatureAction>();
    virtual public bool CanTame(Transform player) => faintCondition() && (Habitat?.CanTame() ?? false);
    virtual public bool ExtractTamingCost(Transform player) => Habitat?.CanTame() ?? false;
    private Func<bool> faintCondition = () => true;

    virtual public IEnumerator FocusedBehavior() { yield break; }
    virtual public WhyNot IsValidFocus(Transform characterFocus) => 
        characterFocus == null ? "null_focus" :
        resource?.IsOut == true ? "insufficient_resource" :
        general.hasAttack ? Will.IsThreat(teamId, transform.position, characterFocus) :
        true;
    virtual public Optional<Transform> FindFocus() => Optional<Transform>.Empty();
    virtual public IEnumerator UnblockSelf(Terrain.Position location) =>
        throw new NotImplementedException("Must implement if one can clear obstacles one cannot pass");
    public Habitat Habitat { get; protected set; } = null;

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
            if (oldState.type != state.type) {
                oldHabit.OnExit();
                newHabit.OnEnter();
            } else {
                newHabit.OnUpdate();
            }
        }
        if (originalState.type == state.type && !newHabit.OnUpdate()) {
            Debug.LogError("Should not transition from state " + state.type + " to same state: " + state);
        }
        RunningBehaviorTask?.Stop();
        if (newHabit.HasRunBehavior) {
            RunningBehaviorTask = new TaskRunner(() => newHabit.RunBehavior(state, this, EndState), species);
            RunningBehaviorTask.Start();
        }

        DebugLogStateChange(false);

        ScanningBehaviorTask.RunIf(state.type.IsScanning());
    }

    private void EndState() => new Senses() {
        endState = true
    }.TryUpdateCreature(creature);

    private IEnumerator ScanningBehavior() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);
            if (state.type == CreatureStateType.PassiveCommand && 
                    state.command?.type == CommandType.Follow &&
                    (general.hasAttack || !general.scanForFocusWhenFollowing))
                continue;
            Optional<Transform> maybeFocus = FindFocus();
            if (maybeFocus.HasValue) { 
                SetFocus(maybeFocus.Value);
                continue;
            }
            if (Habitat != null) {
                Optional<Vector2Int> maybeShelter = Habitat.FindShelter();
                if (maybeShelter.HasValue) {
                    SetShelter(maybeShelter.Value);
                    continue;
                }
            }
        }
    }

    /////////////////////////
    // AI SELF STATE CHANGES

    public void RequestFollow() {
        new Senses() { command = Command.RequestFollow() }.TryUpdateCreature(creature);
        if (state.command?.type == CommandType.Follow) {
            WorldInteraction playerInterface = state.command?.followDirective.Value.GetComponentStrict<PlayerCharacter>().Interaction;
            playerInterface.EnqueueFollowing(creature);
        }
    }

    // param recipient may be null, which is a no-op
    protected Optional<Transform> RequestPair(Creature recipient) {
        if (recipient?.TryPair(transform) == true) {
            new Senses {
                environment = new Senses.Environment() {
                    characterFocus = Delta<Transform>.Add(recipient.transform),
                    focusIsPair = Optional.Of(recipient)
                }
            }.TryUpdateCreature(creature, 2);
            return Optional.Of(recipient.transform);
        }
        else return Optional<Transform>.Empty();
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

    public void DisableFollowOffensive() => new Senses() {
        hint = new Hint() { generallyOffensive = false }
    }.TryUpdateCreature(creature);

    public void UpdateFollowOffensive() {
        Optional<Transform> threat = Will.NearestThreat(this);
        if (!threat.HasValue) DisableFollowOffensive();
        else SetFocus(threat.Value);
    }

    private void SetShelter(Vector2Int shelter) => new Senses() {
        environment = new Senses.Environment() {
            shelter = Delta<Vector2Int>.Add(shelter)
        }
    }.TryUpdateCreature(creature);

    ////////////////
    // SANITY CHECKS

    public void Update() {
        if (stateIsDirty) OnStateChange();
        StateAssumptions();
    }

    // Check for things that should never happen
    public void StateAssumptions() {
        if (state.type == CreatureStateType.Override) return; // put off sanity checks if overriden

        // Directives
        if (state.command?.type == CommandType.Follow && state.command?.followDirective.HasValue != true) {
            Debug.LogError("Following without followDirective");
            creature.CommandRoam();
        }
        if (state.command?.type == CommandType.Station && state.command?.stationDirective == null) {
            Debug.LogError("Stationed without stationDirective");
            creature.CommandRoam();
        }
        if (state.command?.type == CommandType.Execute && state.command?.followDirective.HasValue != true) {
            Debug.LogError("Executing without subsequent followDirective");
        }
        // Invalid focus combinations
        if (state.characterFocus.HasValue && state.investigation != null) {
            Debug.LogError("Focus and investigation at the same time: " + state.characterFocus.Or(null) + " " + state.investigation);
            RemoveInvestigation();
        }
        // I didn't bother to ensure Pair and Investigation cannot happen simultaneously
        // so Pair is not checked here
        if ((state.type == CreatureStateType.Execute || state.type == CreatureStateType.Faint)
            && (state.characterFocus.HasValue || state.investigation != null)) {
            Debug.LogError("Should not have focus or investigation while in state "
                + state.type + ": " + state.characterFocus.Or(null) + " " + state.investigation);
            if (state.characterFocus.HasValue) RemoveFocus();
            if (state.investigation != null) RemoveInvestigation();
        }
    }
}