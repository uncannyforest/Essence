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
        return (int)type < 400;
    }
    public static bool IsTrekking(this CreatureStateType type) {
        return (int)type <= 300;
    }
    public static bool CanTransition(this CreatureStateType from, int priority) {
        return priority >= (int)from;
    }
    public static bool CanTransitionTo(this CreatureStateType from, CreatureStateType type) {
        return from.CanTransition((int)type - 50);
    }
}

public struct Habit {
    private Node node;
    private CreatureState creatureState;
    private Brain brain;
    public Habit(Node node, CreatureState creatureState, Brain brain) {
        this.node = node;
        this.creatureState = creatureState;
        this.brain = brain;
    }
    public bool HasRunBehavior { get => node.onRun != null || node.onRunStep != null; }
    public IEnumerator RunBehavior(Brain brain, Action doneRunning) =>
        node.RunBehavior(brain, doneRunning);
    public void OnEnter() => node.onEnter(creatureState, brain);
    public void OnExit() => node.onExit(creatureState, brain);
    public bool OnUpdate() => node.onUpdate(creatureState, brain);

    public static bool CanTransitionTo(CreatureStateType from, CreatureStateType type) => from.CanTransitionTo(type);
    public static bool CanTransition(CreatureStateType from, int priority) => from.CanTransition(priority);
    public static bool ForcedTransitionAllowed(CreatureState oldState, CreatureState newState, int? priority = null) {
        if (priority is int actualPriority) return oldState.CanTransition(actualPriority);
        else return oldState.CanTransitionTo(newState.type);
    }

    public static Habit Get(CreatureState state, Brain brain) => new Habit(Nodes[state.type], state, brain);

    public class Node {
        public CreatureStateType type;
        public Action<CreatureState, Brain> onEnter = (s, b) => {};
        public Action<CreatureState, Brain> onExit = (s, b) => {};
        public Func<CreatureState, Brain, bool> onUpdate = (s, b) => false;
        public Func<CreatureState, bool> onRunCheck = (s) => true;
        public Func<Brain, Func<IEnumerator>> onRun; // retrieved on the second frame of RunBehavior()
        public Func<CreatureState, Brain, WaitForSeconds> onRunStep;

        public Node (CreatureStateType type) => this.type = type;

        public Node ExitAndEnterOnUpdate() {
            onUpdate = (state, brain) => {
                onExit(state, brain);
                onEnter(state, brain);
                return true;
            };
            return this;
        }

        public Func<Brain, Action, IEnumerator> RunBehavior {
            get {
                if (onRun != null) return RunBehaviorEnumerator;
                else if (onRunStep != null) return RunBehaviorStep;
                else throw new InvalidOperationException("No behaviors");
            }
        }

        private IEnumerator RunBehaviorEnumerator(Brain brain, Action doneRunning) {
            yield return null;
            IEnumerator subBehavior = onRun(brain)(); // saved here, not reset unless RunBehavior() is called again
            while (onRunCheck(brain.state) && subBehavior.MoveNext()) {
                yield return subBehavior.Current;
            }
            doneRunning();
        }

        private IEnumerator RunBehaviorStep(Brain brain, Action doneRunning) {
            yield return null;
            while (onRunCheck(brain.state)) {
                yield return onRunStep(brain.state, brain);
            }
            doneRunning();
        }

    }

    private static Dictionary<CreatureStateType, Node> Nodes = new Dictionary<CreatureStateType, Node>() {
        [CreatureStateType.Override] = new Node(CreatureStateType.Override) {
            onRunCheck = (state) => state.ControlOverride != null,
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
            onRun = (brain) => brain.state.command?.executeDirective?.enumerator
        },

        [CreatureStateType.CharacterFocus] = new Node(CreatureStateType.CharacterFocus) {
            onExit = (state, brain) => {
                if (state.focusIsPair.HasValue) {
                    state.focusIsPair.Value?.EndPairCommand(brain.transform);
                }
            },
            onRunCheck = (state) => !state.characterFocus.IsDestroyed,
            onRun = (brain) => brain.FocusedBehavior,
        },

        [CreatureStateType.Pair] = new Node(CreatureStateType.Pair) {
            onExit = (state, _) => {
                Creature master = state.pairDirective.Value.GetComponent<Creature>();
                if (master != null) master.EndPairRequest();
            },
            onRunCheck = (state) => !state.pairDirective.IsDestroyed,
            onRunStep = (state, brain) => brain.pathfinding.ApproachThenIdle(state.pairDirective.Value.transform.position, brain.movement.personalBubble)
        },

        [CreatureStateType.Investigate] = new Node(CreatureStateType.Investigate) {
            onEnter = (_, brain) => brain.investigationCancel =
                RunOnce.Run(brain.species, Creature.neighborhood * brain.movement.Speed, brain.RemoveInvestigation),
            onExit = (_, brain) => brain.investigationCancel.Stop(),
            onRunStep = (_, brain) => {
                if (((Vector3)brain.Investigation - brain.transform.position).magnitude <
                        brain.general.reconsiderRateTarget * brain.movement.Speed) brain.RemoveInvestigation();
                brain.pathfinding.MoveToward((Vector3)brain.Investigation);
                return new WaitForSeconds(brain.general.reconsiderRateTarget);  
            },
        }.ExitAndEnterOnUpdate(),

        [CreatureStateType.PassiveCommand] = new Node(CreatureStateType.PassiveCommand) {
            onEnter = (state, brain) => {
                if (state.followOffensive) brain.UpdateFollowOffensive();
            },
            onUpdate = (_, __) => true,
            onRunStep = (state, brain) => {
                if (state.command?.type == CommandType.Roam) return brain.pathfinding.Roam();
                if (state.command?.type == CommandType.Follow) return brain.pathfinding.Follow(state.command?.followDirective.Value);
                if (state.command?.type == CommandType.Station) return brain.pathfinding
                    .ApproachThenIdle((Vector3)state.command?.stationDirective, 1f / CharacterController.subGridUnit);
                Debug.LogError("Weird state: " + state);
                return null;
            }
        },
    };
}
