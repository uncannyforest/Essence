using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CreatureStateType {
    Override = 800,
    Faint = 700,
    Execute = 500,
    CharacterFocus = 340,
    Pair = 300,
    Investigate = 260,
    PassiveCommand = 100,
}
public static class CreatureStateTypeExtensions {
    public static bool IsScanning(this CreatureStateType type) {
        return (int)type < 300;
    }
    public static bool CanTransition(this CreatureStateType from, int priority) {
        return priority >= (int)from;
    }
    public static bool CanTransitionTo(this CreatureStateType from, CreatureStateType type) {
        return from.CanTransition((int)type - 50);
    }
}

public struct Habit {

    // class properties and methods

    private Node node;
    private CreatureState creatureState;
    private Brain brain;
    public Habit(Node node, CreatureState creatureState, Brain brain) {
        this.node = node;
        this.creatureState = creatureState;
        this.brain = brain;
    }
    public bool HasRunBehavior { get => node.onRun != null || node.onRunStep != null; }
    public IEnumerator RunBehavior(CreatureState state, Brain brain, Action doneRunning) =>
        node.RunBehavior(state, brain, doneRunning);
    public void OnEnter() => node.onEnter(creatureState, brain);
    public void OnExit() => node.onExit(creatureState, brain);
    public bool OnUpdate() => node.onUpdate(creatureState, brain);

    // static methods

    public static bool CanTransitionTo(CreatureStateType from, CreatureStateType type) => from.CanTransitionTo(type);
    public static bool CanTransition(CreatureStateType from, int priority) => from.CanTransition(priority);
    public static bool ForcedTransitionAllowed(CreatureState oldState, CreatureState newState, int? priority = null) {
        if (priority is int actualPriority) return oldState.CanTransition(actualPriority);
        else return oldState.CanTransitionTo(newState.type);
    }

    public static Habit Get(CreatureState state, Brain brain) => new Habit(Nodes[state.type], state, brain);

    // Node class

    public class Node {
        public CreatureStateType type;
        public Action<CreatureState, Brain> onEnter = (s, b) => {}; // run on transition from another CreatureStateType
        public Action<CreatureState, Brain> onExit = (s, b) => {}; // run on transition to another CreatureStateType
        public Func<CreatureState, Brain, bool> onUpdate = (s, b) => false; // run on transition to same type, return false if illegal
        public Func<CreatureState, Brain, bool> onRunCheck = (s, b) => true; // while condition for run behavior
        public Func<CreatureState, Brain, BehaviorNode> onRun; // retrieved on the second frame of RunBehavior()
        public Func<CreatureState, Brain, YieldInstruction> onRunStep; // single step alternative to onRun

        public Node (CreatureStateType type) => this.type = type;

        public Node ExitAndEnterOnUpdate() {
            onUpdate = (state, brain) => {
                onExit(state, brain);
                onEnter(state, brain);
                return true;
            };
            return this;
        }

        public Func<CreatureState, Brain, Action, IEnumerator> RunBehavior {
            get {
                if (onRun != null) return RunBehaviorEnumerator;
                else if (onRunStep != null) return RunBehaviorStep;
                else throw new InvalidOperationException("No behaviors");
            }
        }

        private IEnumerator RunBehaviorEnumerator(CreatureState state, Brain brain, Action doneRunning) {
            yield return null;
            IEnumerator subBehavior = onRun(state, brain).enumerator(); // saved here, not reset unless RunBehavior() is called again
            while (onRunCheck(state, brain) && subBehavior.MoveNext()) {
                yield return subBehavior.Current;
            }
            doneRunning();
        }

        private IEnumerator RunBehaviorStep(CreatureState state, Brain brain, Action doneRunning) {
            yield return null;
            while (onRunCheck(state, brain)) {
                yield return onRunStep(state, brain);
            }
            doneRunning();
        }

    }

    public class PassiveCommandNode {
        public CommandType type;
        public Func<CreatureState, Brain, YieldInstruction> passiveCommandRunStep;
        public Optional<Func<CreatureState, Vector2>> nearbyTracking;

        public PassiveCommandNode(CommandType type) => this.type = type;

        public BehaviorNode MaybeRestrictNearby(CreatureState state, Brain brain, BehaviorNode subBehavior) {
            if (nearbyTracking.HasValue)
                return new RestrictNearbyBehavior(subBehavior, brain.transform, () => nearbyTracking.Value(state), Creature.neighborhood);
            else return subBehavior;
        }

    }

    // all state specs

    private static Dictionary<CreatureStateType, Node> Nodes = new Dictionary<CreatureStateType, Node>() {
        [CreatureStateType.Override] = new Node(CreatureStateType.Override) {
            onRunCheck = (state, _) => state.ControlOverride != null,
        },

        [CreatureStateType.Faint] = new Node(CreatureStateType.Faint) {
            onEnter = (_, brain) => {
                brain.movement.Idle();
                brain.movement.SetBool("Fainted", true);
            },
            onExit = (_, brain) => brain.movement.SetBool("Fainted", false)
        },

        [CreatureStateType.Execute] = new Node(CreatureStateType.Execute) {
            onUpdate = (_, __) => true,
            onRun = (state, _) => state.command?.executeDirective
        },

        [CreatureStateType.CharacterFocus] = new Node(CreatureStateType.CharacterFocus) {
            onExit = (state, brain) => {
                if (state.focusIsPair.HasValue) {
                    state.focusIsPair.Value?.EndPairCommand(brain.transform);
                }
            },
            onRunCheck = (state, brain) => !state.characterFocus.IsDestroyed &&
                    brain.IsValidFocus(state.characterFocus.Value),
            onRun = (state, brain) => PassiveCommandNodes[(CommandType)state.command?.type].MaybeRestrictNearby(state, brain,
                    new TargetedBehavior<Transform>(brain.FocusedBehavior).WithTarget(state.characterFocus.Value)),
        },

        [CreatureStateType.Pair] = new Node(CreatureStateType.Pair) {
            onExit = (state, _) => {
                Creature master = state.pairDirective.Value.GetComponent<Creature>();
                if (master != null) master.EndPairRequest();
            },
            onRunCheck = (state, _) => !state.pairDirective.IsDestroyed,
            onRunStep = (state, brain) => brain.pathfinding.ApproachThenIdle(state.pairDirective.Value.transform.position, brain.movement.personalBubble)
        },

        [CreatureStateType.Investigate] = new Node(CreatureStateType.Investigate) {
            onEnter = (_, brain) => brain.investigationCancel =
                RunOnce.Run(brain.species, Creature.neighborhood * brain.movement.Speed, brain.RemoveInvestigation),
            onExit = (_, brain) => brain.investigationCancel.Stop(),
            onRunCheck = (state, brain) => ((Vector3)state.investigation - brain.transform.position).magnitude >
                                            brain.general.reconsiderRateTarget * brain.movement.Speed,
            onRun = (state, brain) => PassiveCommandNodes[(CommandType)state.command?.type].MaybeRestrictNearby(state, brain,
                new BehaviorNode(() => {
                    brain.pathfinding.MoveToward((Vector3)state.investigation);
                    return new WaitForSeconds(brain.general.reconsiderRateTarget);  
                })),
        }.ExitAndEnterOnUpdate(),

        [CreatureStateType.PassiveCommand] = new Node(CreatureStateType.PassiveCommand) {
            onEnter = (state, brain) => {
                if (state.followOffensive) brain.UpdateFollowOffensive();
            },
            onUpdate = (_, __) => true,
            onRunStep = (state, brain) => PassiveCommandNodes[(CommandType)state.command?.type].passiveCommandRunStep(state, brain)
        },
    };

    private static Dictionary<CommandType, PassiveCommandNode> PassiveCommandNodes = new Dictionary<CommandType, PassiveCommandNode>() {
        [CommandType.Roam] = new PassiveCommandNode(CommandType.Roam) {
            nearbyTracking = Optional<Func<CreatureState, Vector2>>.Empty(),
            passiveCommandRunStep = (state, brain) => brain.pathfinding.Roam()
        },
        [CommandType.Follow] = new PassiveCommandNode(CommandType.Follow) {
            nearbyTracking = Optional<Func<CreatureState, Vector2>>.Of((state) => (Vector2)state.command?.followDirective.Value.position),
            passiveCommandRunStep = (state, brain) => brain.pathfinding.Follow(state.command?.followDirective.Value)
        },
        [CommandType.Station] = new PassiveCommandNode(CommandType.Station) {
            nearbyTracking = Optional<Func<CreatureState, Vector2>>.Of((state) => (Vector2)state.command?.stationDirective),
            passiveCommandRunStep = (state, brain) => brain.pathfinding
                .ApproachThenIdle((Vector3)state.command?.stationDirective, 1f / CharacterController.subGridUnit)
        },
    };
}
