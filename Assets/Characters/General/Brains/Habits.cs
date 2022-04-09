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

public class Habits {
    public static bool CanTransitionTo(CreatureStateType from, CreatureStateType type) => CanTransition(from, (int)type - 50);
    public static bool CanTransition(CreatureStateType from, int priority) => priority >= (int)from;

    public static bool ForcedTransitionAllowed(CreatureState oldState, CreatureState newState, int? priority = null) {
        if (priority is int actualPriority) return oldState.CanTransition(actualPriority);
        else return oldState.CanTransitionTo(newState.type);
    }

    public struct Node {
        public CreatureStateType type;
        public Action onEnter;
        public BehaviorNodeShell<Target> onRun;
        public Action onExit;

        public Node(CreatureStateType type) {
            this.type = type;
            onEnter = null;
            onRun = null;
            onExit = null;
        }
        public static Node OfType(CreatureStateType type) => new Node(type);
    }

    private static Dictionary<CreatureStateType, Node> nodes = new Dictionary<CreatureStateType, Node>() {
        [CreatureStateType.Override] = Node.OfType(CreatureStateType.Override),
        [CreatureStateType.Faint] = Node.OfType(CreatureStateType.Faint),
        [CreatureStateType.Execute] = Node.OfType(CreatureStateType.Execute),
        [CreatureStateType.CharacterFocus] = Node.OfType(CreatureStateType.CharacterFocus),
        [CreatureStateType.Pair] = Node.OfType(CreatureStateType.Pair),
        [CreatureStateType.Investigate] = Node.OfType(CreatureStateType.Investigate),
        [CreatureStateType.PassiveCommand] = Node.OfType(CreatureStateType.PassiveCommand),
    };
}
