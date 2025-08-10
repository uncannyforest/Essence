using System;
using System.Collections.Generic;
using UnityEngine;

// This is a interface for factory classes
// to flexibly handle player-assigned targets and creature-found focuses using the same code.
//
// Implementing classes store behavior configuration
// to be exported to CreatureActions, FocusedBehavior, and Larks.
// 
// Note: if this were designed from scratch, perhaps CreatureActions (CreatureState.Execute),
// Focuses (CreatureState.Focus), and Larks (CreatureState.PassiveCommand)
// would all use the exact same code everywhere and just have different priority levels (see CreatureState).
// This file is a step towards simplifying and universalizing that code,
// but today is not the day for such an extensive refactor.
public interface FlexSourceBehavior {
    public CreatureAction CreatureActionCharacter(Sprite sprite);

    // Used to give up on character focus/target.
    // This could be expanded to terrain focus/target using DesireMessage.Obstacle::IsStillPresent
    // but that will require a refactor to use DesireMessage.Obstacle throughout if/when it is done.
    public WhyNot IsValidFocus(Transform characterFocus);

    public IEnumerator<YieldInstruction> FocusedBehavior();

    public Lark Lark(Func<bool> precondition, Radius radius);
}

// behavior generator with unspecified target
// 1. At construction:
//        targetedBehavior = new TargetedBehavior(targetToEnumeratorFunction)
// 2. When target is known:
//        behaviorNode = targetedBehavior.WithTarget(target)
public class TargetedBehavior<T> {
    virtual public bool canQueue { get; protected set; } = false;

    public Func<T, IEnumerator<YieldInstruction>> enumeratorWithParam;
    public Func<T, WhyNot> errorFilter;

    protected TargetedBehavior() {}
    public TargetedBehavior(Func<T, IEnumerator<YieldInstruction>> enumeratorWithParam, Func<T, WhyNot> errorFilter) {
        this.enumeratorWithParam = enumeratorWithParam;
        this.errorFilter = errorFilter;
    }

    virtual public OneOf<BehaviorNode, string> WithTarget(T target) {
        WhyNot canTarget = errorFilter(target);
        if (canTarget) return new BehaviorNode(() => CheckNonNullTargetEnumerator(target));
        else return (string)canTarget;
    }

    private IEnumerator<YieldInstruction> CheckNonNullTargetEnumerator(T target) {
        IEnumerator<YieldInstruction> task = enumeratorWithParam(target);
        while (target != null && task.MoveNext()) {
            yield return task.Current;
        }
    }

    public QueueOperator.Targeted<T> Queued() =>
        new QueueOperator.Targeted<T>(enumeratorWithParam, errorFilter);
}

public class NullSourceBehavior : FlexSourceBehavior {
    public CreatureAction CreatureActionCharacter(Sprite sprite) => throw new NotImplementedException("NullTargetedBehavior: unspecified Brain behavior");

    public WhyNot IsValidFocus(Transform characterFocus) => "none_allowed";

    public IEnumerator<YieldInstruction> FocusedBehavior() { yield break; }

    public Lark Lark(Func<bool> precondition, Radius radius) => global::Lark.None();
}

public class CharacterTargetedBehavior : TargetedBehavior<Transform>, FlexSourceBehavior {
    private Brain brain;
    public Func<Transform, WhyNot> silentFilter;

    public CharacterTargetedBehavior(
            Brain brain,
            Func<Transform, IEnumerator<YieldInstruction>> enumeratorWithParam,
            Func<Transform, WhyNot> silentFilter,
            Func<Transform, WhyNot> errorFilter)
            : base(enumeratorWithParam, errorFilter) {
        this.brain = brain;
        this.silentFilter = silentFilter;
    }

    public CreatureAction CreatureActionCharacter(Sprite sprite) => CreatureAction.WithCharacter(sprite, this, silentFilter);

    public WhyNot IsValidFocus(Transform characterFocus) => silentFilter(characterFocus) && errorFilter(characterFocus);

    public IEnumerator<YieldInstruction> FocusedBehavior() => enumeratorWithParam(brain.state.characterFocus.Value);

    public Lark Lark(Func<bool> precondition, Radius radius) => global::Lark.None();
}

// Class Target is used for field errorFilter to handle either Characters or Terrain.
// 
// See comment at the top of Target.cs for type discrepancy pitfalls
// which may need to be cleaned up eventually.
public class FlexTargetedBehavior : FlexSourceBehavior {
    private Brain brain;
    public Func<Transform, IEnumerator<YieldInstruction>> characterBehavior;
    public Action<Terrain.Position> terrainAction;
    public TeleFilter silentFilter;
    public Func<Target, WhyNot> errorFilter;

    public FlexTargetedBehavior(
            Brain brain,
            Func<Transform, IEnumerator<YieldInstruction>> characterBehavior,
            Action<Terrain.Position> terrainAction,
            TeleFilter silentFilter,
            Func<Target, WhyNot> errorFilter) {
        this.brain = brain;
        this.characterBehavior = characterBehavior;
        this.terrainAction = terrainAction;
        this.silentFilter = silentFilter;
        this.errorFilter = errorFilter;
    }

    public CreatureAction CreatureActionCharacter(Sprite sprite) => CreatureAction.WithCharacter(sprite,
        new TargetedBehavior<Transform>(characterBehavior, errorFilter.Transform()),
        silentFilter.characterFilter);

    public CreatureAction CreatureActionTerrain(Sprite sprite) => CreatureAction.WithTerrain(sprite,
        brain.pathfinding.Terraform(errorFilter.Pos(), terrainAction).PendingPosition().Queued(),
        silentFilter.terrainSelection);

    public WhyNot IsValidFocus(Transform characterFocus) =>
        silentFilter.characterFilter(characterFocus) && errorFilter(Target.Character(characterFocus));

    public IEnumerator<YieldInstruction> FocusedBehavior() => MuxFocus(brain.state);

    public Lark Lark(Func<bool> precondition, Radius radius) =>
        new Lark(brain, precondition, errorFilter.Vct(), radius, terrainAction);

    private IEnumerator<YieldInstruction> MuxFocus(CreatureState state) =>
        MuxFocus(state,
            characterBehavior,
            (pos) => brain.pathfinding.Terraform(errorFilter.Pos(), terrainAction).Enumerator(pos));

    public static IEnumerator<YieldInstruction> MuxFocus(CreatureState state,
            Func<Transform, IEnumerator<YieldInstruction>> characterBehavior,
            Func<Terrain.Position, IEnumerator<YieldInstruction>> terrainBehavior) {
        if (state.characterFocus.IsValue(out Transform character)) return characterBehavior(character);
        if (state.terrainFocus is DesireMessage.Obstacle obstacle) return terrainBehavior(obstacle.location);
        throw new InvalidOperationException("Tried to build focused behavior without a focus");
    }
}

// For filter type conversions
public static class Filter {
    // use Log when processing a single T
    public static Func<T, bool> Log<T>(this Func<T, WhyNot> filter, string message = "Filtered")
        => (t) => filter(t).NegLog(message);
    // use Silence when processing through a list of T's, to reduce logging
    public static Func<T, bool> Silence<T>(this Func<T, WhyNot> filter)
        => (t) => (bool)filter(t);
    
    public static Func<Vector2Int, U> Vct<U>(this Func<Target, U> filter)
        => (v) => filter(new Target(new Terrain.Position(Terrain.Grid.Roof, v)));

    public static Func<Vector2Int, U> Vct<U>(this Func<Terrain.Position, U> filter)
        => (v) => filter(new Terrain.Position(Terrain.Grid.Roof, v));

    public static Func<Terrain.Position, U> Coord<U>(this Func<Vector2Int, U> filter)
        => (pos) => filter(pos.Coord);

    public static Func<Terrain.Position, U> Pos<U>(this Func<Target, U> filter)
        => (pos) => filter(new Target(pos));

    public static Func<Transform, U> Transform<U>(this Func<Target, U> filter)
        => (t) => filter(Target.Character(t));
}