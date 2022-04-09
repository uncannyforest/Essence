using System;
using System.Collections.Generic;
using UnityEngine;

public struct CreatureState {
    private static CreatureState OfType(CreatureStateType type) {
        CreatureState result = new CreatureState();
        result.type = type;
        return result;
    }
    
    public bool CanTransitionTo(CreatureStateType type) => Habits.CanTransitionTo(this.type, type);
    public bool CanTransition(int priority) => Habits.CanTransition(this.type, priority);

    public CreatureStateType type { get; private set; }

    private Optional<MonoBehaviour> controlOverride;
    private CreatureStateType? controlOverridePrevState;
    public MonoBehaviour ControlOverride { get => controlOverride.Value; }
    public static CreatureState WithControlOverride(CreatureState copiedState, MonoBehaviour source) {
        if (copiedState.type == CreatureStateType.Override) throw new ArgumentException("Nested control override not supported");
        copiedState.controlOverridePrevState = copiedState.type;
        copiedState.controlOverride = Optional.Of(source);
        copiedState.type = CreatureStateType.Override;
        return copiedState;
    }
    public static CreatureState WithoutControlOverride(CreatureState copiedState) {
        if (copiedState.type != CreatureStateType.Override) throw new ArgumentException("Not control override");
        copiedState.type = copiedState.controlOverridePrevState.Value;
        copiedState.controlOverride = Optional<MonoBehaviour>.Empty();
        copiedState.controlOverridePrevState = null;
        return copiedState;
    }

    public static CreatureState Fainted() {
        return CreatureState.OfType(CreatureStateType.Faint);
    }

    public Command? command;
    public static CreatureState Command(Command command) {
        CreatureStateType resultType = command.type == CommandType.Execute ? CreatureStateType.Execute : CreatureStateType.PassiveCommand;
        CreatureState result = CreatureState.OfType(resultType);
        result.command = command;
        return result;
    }

    public Optional<Transform> pairDirective;
    public static CreatureState Pair(CreatureState oldState, Transform pairDirective) {
        oldState.pairDirective = Optional.Of(pairDirective);
        oldState.type = CreatureStateType.Pair;
        return oldState;
    }
    public static CreatureState Unpair(CreatureState oldState) {
        oldState.pairDirective = Optional<Transform>.Empty();
        return oldState.ToPassiveCommand();
    }

    public Hint hint;
    public CreatureState DisableFollowOffensive() {
        if (command?.type != CommandType.Follow) throw new InvalidOperationException("Called DisableFollowOffensive on command " + command?.type);
        hint = new Hint();
        return this;
    }
    public CreatureState FollowOffensiveNoTarget() {
        if (command?.type != CommandType.Follow) throw new InvalidOperationException("Called FollowOffensive on command " + command?.type);
        hint = new Hint();
        hint.offensive = true;
        return this;
    }
    public CreatureState FollowOffensiveWithTarget(Transform target) {
        if (command?.type != CommandType.Follow) throw new InvalidOperationException("Called FollowOffensive on command " + command?.type);
        hint = new Hint();
        hint.offensive = true;
        hint.target = Optional.Of(target);
        return this;
    }

    public Optional<Creature> focusIsPair;
    public Optional<Transform> characterFocus;
    public Vector3? investigation;
    public CreatureState WithCharacterFocus(Transform characterFocus) {
        CreatureState state = this;
        state.type = CreatureStateType.CharacterFocus;
        state.characterFocus = Optional.Of(characterFocus);
        return state;
    }
    public CreatureState WithoutCharacterFocus() {
        CreatureState state = this;
        if (state.focusIsPair.HasValue) throw new InvalidOperationException("Must remove pair");
        state.type = CreatureStateType.PassiveCommand;
        state.characterFocus = Optional<Transform>.Empty();
        return state;
    }
    public CreatureState WhereFocusIsPair(Creature focusIsPair) {
        CreatureState state = this;
        if (state.type != CreatureStateType.CharacterFocus) throw new InvalidOperationException("Not a focus");
        state.focusIsPair = Optional.Of(focusIsPair);
        return state;
    }
    public CreatureState WithInvestigation(Vector3 investigation) {
        CreatureState state = this;
        state.type = CreatureStateType.Investigate;
        state.investigation = investigation;
        return state;
    }
    public CreatureState WithoutInvestigation() {
        CreatureState state = this;
        state.type = CreatureStateType.PassiveCommand;
        state.investigation = null;
        return state;
    }
    public CreatureState ClearFocus() {
        CreatureState state = this;
        state.focusIsPair = Optional<Creature>.Empty();
        state.type = CreatureStateType.PassiveCommand;
        state.characterFocus = Optional<Transform>.Empty();
        state.investigation = null;
        return state;
    }

    public CreatureState ToPassiveCommand() {
        CreatureState state = this;
        state.type = CreatureStateType.PassiveCommand;
        return state;
    }
    public CreatureState UpdatePassiveCommand(Command command) {
        CreatureState state = this;
        state.command = command;
        return state;
    }
}