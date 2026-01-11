using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public IEnumerator<YieldInstruction> RunBehavior(CreatureState state, Brain brain, Action doneRunning) =>
        node.RunBehavior(state, brain, doneRunning);
    public void OnEnter() => node.onEnter(creatureState, brain);
    public void OnExit(CreatureState newState) => node.onExit(creatureState, newState, brain);
    public bool OnUpdate() => node.onUpdate(creatureState, brain);

    // static methods

    public static Habit Get(CreatureState state, Brain brain) => new Habit(Nodes[state.detailedType], state, brain);

    // Node class

    public class Node {
        public CreatureStateDetailedType type;
        public Action<CreatureState, Brain> onEnter = (s, b) => {}; // run on transition from another CreatureStateType
        public Action<CreatureState, CreatureState, Brain> onExit = (s, n, b) => {}; // run on transition to another CreatureStateType
        public Func<CreatureState, Brain, bool> onUpdate = (s, b) => false; // run on transition to same type, return false if illegal
        public Func<CreatureState, Brain, bool> onRunCheck = (s, b) => true; // while condition for run behavior
        public Func<CreatureState, Brain, BehaviorNode> onRun; // retrieved on the second frame of RunBehavior()
        public Func<CreatureState, Brain, YieldInstruction> onRunStep; // single step alternative to onRun

        public Node (CreatureStateDetailedType type) => this.type = type;

        public Node ExitAndEnterOnUpdate() {
            onUpdate = (state, brain) => {
                onExit(state, state, brain);
                onEnter(state, brain);
                return true;
            };
            return this;
        }

        public Func<CreatureState, Brain, Action, IEnumerator<YieldInstruction>> RunBehavior {
            get {
                if (onRun != null) return RunBehaviorEnumerator;
                else if (onRunStep != null) return RunBehaviorStep;
                else throw new InvalidOperationException("No behaviors");
            }
        }

        private IEnumerator<YieldInstruction> RunBehaviorEnumerator(CreatureState state, Brain brain, Action doneRunning) {
            yield return null;
            IEnumerator<YieldInstruction> subBehavior = onRun(state, brain).enumerator(); // saved here, not reset unless RunBehavior() is called again
            while (onRunCheck(state, brain)) {
                if (!subBehavior.MoveNext()) {
                    Debug.Log(brain.creature.gameObject.name + " stopping Behavior because enumerator ended");
                    break;
                }
                yield return subBehavior.Current;
            }
            doneRunning();
        }

        private IEnumerator<YieldInstruction> RunBehaviorStep(CreatureState state, Brain brain, Action doneRunning) {
            yield return null;
            while (onRunCheck(state, brain)) {
                yield return onRunStep(state, brain);
            }
            doneRunning();
        }

    }

    public class PassiveCommandNode {
        public PassiveCommandType type;
        public Func<CreatureState, Brain, IEnumerator<YieldInstruction>> passiveCommandRun;
        public Optional<Func<CreatureState, Vector2>> nearbyTracking;

        public PassiveCommandNode(PassiveCommandType type) => this.type = type;

        public BehaviorNode MaybeRestrictNearby(CreatureState state, Brain brain, Func<IEnumerator<YieldInstruction>> behavior) {
            if (nearbyTracking.HasValue)
                return new RestrictNearbyBehavior(behavior, brain.transform, () => nearbyTracking.Value(state), Creature.neighborhood);
            else return new BehaviorNode(behavior);
        }

        public BehaviorNode Run(CreatureState state, Brain brain) => new BehaviorNode(() => passiveCommandRun(state, brain));
    }

    // all state specs

    private static Dictionary<CreatureStateDetailedType, Node> Nodes = new Dictionary<CreatureStateDetailedType, Node>() {
        [CreatureStateDetailedType.Override] = new Node(CreatureStateDetailedType.Override) {
            onRunCheck = (state, _) => state.ControlOverride != null,
        },

        [CreatureStateDetailedType.Faint] = new Node(CreatureStateDetailedType.Faint) {
            onEnter = (_, brain) => {
                brain.movement.Idle();
                brain.movement.SetFainted(true);
            },
            onExit = (_, __, brain) => brain.movement.SetFainted(false)
        },

        [CreatureStateDetailedType.Execute] = new Node(CreatureStateDetailedType.Execute) {
            onUpdate = (_, __) => true,
            onRun = (state, _) => state.executeCommand?.executeDirective
        },

        [CreatureStateDetailedType.Focus] = new Node(CreatureStateDetailedType.Focus) {
            onExit = (state, _, brain) => {
                if (state.scanActivity?.followerToLead.HasValue == true) {
                    state.scanActivity?.followerToLead.Value?.EndPairCommand(brain.transform);
                }
            },
            // TODO: once Focus & Execute have been standardized to use the same BehaviorNodes, simplify by removing this:
            // all BehaviorNodes in an executeDirective will already run this step in onRun.
            onRunCheck = (state, brain) => {
                if (state.scanActivity?.characterFocus.HasValue == true)
                    return state.scanActivity?.characterFocus.IsDestroyed == false &&
                        brain.IsValidFocus(state.scanActivity?.characterFocus.Value)
                            .NegLog(brain.legalName + " onRunCheck: focus " + state.scanActivity?.characterFocus.Value + " no longer valid");
                if (state.scanActivity?.terrainFocus.HasValue == true)
                    return state.scanActivity?.terrainFocus.Value.IsStillPresent ?? false;
                else {
                    Debug.LogError("Neither characterFocus nor terrainFocus: " + state);
                    return false;
                }
            },
            onRun = (state, brain) => PassiveCommandNodes[(PassiveCommandType)state.scanActivity?.command.type].MaybeRestrictNearby(state, brain, brain.FocusedBehavior),
        },

        [CreatureStateDetailedType.FollowPair] = new Node(CreatureStateDetailedType.FollowPair) {
            onExit = (state, _, __) => {
                Creature master = state.scanActivity?.characterFocus.Value.GetComponent<Creature>();
                if (master != null) master.EndPairRequest();
            },
            onRunCheck = (state, _) => state.scanActivity?.characterFocus.IsDestroyed == false,
            onRunStep = (state, brain) => brain.pathfinding.ApproachThenIdle((Vector3)state.scanActivity?.characterFocus.Value.transform.position, brain.movement.personalBubble)
        },

        [CreatureStateDetailedType.Rest] = new Node(CreatureStateDetailedType.Rest) {
            onExit = (_, newState, brain) => {
                if (newState.detailedType != CreatureStateDetailedType.PassiveCommand || newState.scanActivity?.command.type == PassiveCommandType.Follow) 
                    brain.Habitat?.ClearRecentlyVisited();
            },
            onRunCheck = (state, brain) => brain.Habitat.IsShelter((Vector2Int)state.scanActivity?.shelter),
            onRun = (state, brain) => PassiveCommandNodes[(PassiveCommandType)state.scanActivity?.command.type].MaybeRestrictNearby(state, brain,
                () => brain.Habitat.ApproachAndRestBehavior((Vector2Int)state.scanActivity?.shelter)),
        },

        [CreatureStateDetailedType.Investigate] = new Node(CreatureStateDetailedType.Investigate) {
            onEnter = (_, brain) => brain.investigationCancel =
                RunOnce.Run(brain.species, Creature.neighborhood * brain.movement.Speed, brain.EndState),
            onExit = (_, __, brain) => brain.investigationCancel.Stop(),
            onRunCheck = (state, brain) => ((Vector3)state.scanActivity?.investigation - brain.transform.position).magnitude >
                                            brain.creature.stats.ExeTime * brain.movement.Speed,
            onRun = (state, brain) => PassiveCommandNodes[(PassiveCommandType)state.scanActivity?.command.type].MaybeRestrictNearby(state, brain,
                BehaviorNode.SingleLine(() => {
                    brain.pathfinding.MoveTowardWithoutClearingObstacles((Vector3)state.scanActivity?.investigation);
                    return new WaitForSeconds(brain.creature.stats.ExeTime);
                })),
        }.ExitAndEnterOnUpdate(),

        [CreatureStateDetailedType.PassiveCommand] = new Node(CreatureStateDetailedType.PassiveCommand) {
            onExit = (_, newState, brain) => {
                if (newState.detailedType != CreatureStateDetailedType.Rest) 
                    brain.Habitat?.ClearRecentlyVisited();
            },
            onUpdate = (_, __) => true,
            onRun = (state, brain) => PassiveCommandNodes[(PassiveCommandType)state.scanActivity?.command.type].Run(state, brain)
        },
    };

    private static Dictionary<PassiveCommandType, PassiveCommandNode> PassiveCommandNodes = new Dictionary<PassiveCommandType, PassiveCommandNode>() {
        [PassiveCommandType.Roam] = new PassiveCommandNode(PassiveCommandType.Roam) {
            nearbyTracking = Optional<Func<CreatureState, Vector2>>.Empty(),
            passiveCommandRun = (state, brain) => brain.pathfinding.Roam()
        },
        [PassiveCommandType.Follow] = new PassiveCommandNode(PassiveCommandType.Follow) {
            nearbyTracking = Optional<Func<CreatureState, Vector2>>.Of((state) => (Vector2)state.scanActivity?.command.followDirective.Value.position),
            passiveCommandRun = (state, brain) => Enumerators.Continually(() => brain.pathfinding.Follow(state.scanActivity?.command.followDirective.Value))
        },
        [PassiveCommandType.Station] = new PassiveCommandNode(PassiveCommandType.Station) {
            nearbyTracking = Optional<Func<CreatureState, Vector2>>.Of((state) => (Vector2)state.scanActivity?.command.stationDirective),
            passiveCommandRun = (state, brain) => Enumerators.Continually(() => brain.pathfinding
                .ApproachThenIdle((Vector3)state.scanActivity?.command.stationDirective))
        },
    };
}
