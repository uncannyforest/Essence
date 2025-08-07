using System;
using System.Collections.Generic;
using UnityEngine;

// Flexibly handle player-assigned targets and creature-found focuses using the same code.
// Implementation classes use Target class
// - see comment at the top of Target.cs for type discrepancy pitfalls
// which may need to be cleaned up eventually.
// 
// Note: if this were designed from scratch, CreatureActions (CreatureState.Execute),
// Focuses (CreatureState.Focus), and Larks (CreatureState.PassiveCommand)
// would all use the exact same code everywhere and just have different priority levels (see CreatureState).
// This file is a step towards simplifying and universalizing that code,
// but today is not the day for such an extensive refactor.
public interface FlexSourceBehavior {
    public CreatureAction CreatureAction(Sprite sprite);

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
    public CreatureAction CreatureAction(Sprite sprite) => throw new NotImplementedException("NullTargetedBehavior: unspecified Brain behavior");

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

    public CreatureAction CreatureAction(Sprite sprite) => global::CreatureAction.WithCharacter(sprite, this, silentFilter);

    public WhyNot IsValidFocus(Transform characterFocus) => silentFilter(characterFocus) && errorFilter(characterFocus);

    public IEnumerator<YieldInstruction> FocusedBehavior() => enumeratorWithParam(brain.state.characterFocus.Value);

    public Lark Lark(Func<bool> precondition, Radius radius) => global::Lark.None();
}

public class FlexTargetedBehavior : TargetedBehavior<Target>, FlexSourceBehavior {
    private Brain brain;
    public Func<Character, IEnumerator<YieldInstruction>> characterBehavior;
    public Action<Terrain.Position> terrainAction;
    public TeleFilter silentFilter;

    public FlexTargetedBehavior(
            Brain brain,
            Func<Character, IEnumerator<YieldInstruction>> characterBehavior,
            Action<Terrain.Position> terrainAction,
            TeleFilter silentFilter,
            Func<Target, WhyNot> errorFilter)
            : base(null, errorFilter) {
        this.characterBehavior = characterBehavior;
        this.terrainAction = terrainAction;
        this.brain = brain;
        this.silentFilter = silentFilter;
        this.enumeratorWithParam = MuxBehavior;
    }

    public CreatureAction CreatureAction(Sprite sprite) => global::CreatureAction.WithObject(sprite, this, silentFilter);

    public WhyNot IsValidFocus(Transform characterFocus) =>
        silentFilter.characterFilter(characterFocus) && errorFilter(Target.Character(characterFocus));

    public IEnumerator<YieldInstruction> FocusedBehavior() => MuxFocus(brain.state);

    public Lark Lark(Func<bool> precondition, Radius radius) =>
        new Lark(brain, precondition, errorFilter, radius, 1.5f, terrainAction);

    private IEnumerator<YieldInstruction> MuxBehavior(Target f) {
        if (f.Is(out Character c))
            return characterBehavior(c);
        if (f.Is(out Terrain.Position pos))
            return brain.pathfinding.ApproachThenTerraform(pos, 1.5f, terrainAction);
        throw new ArgumentException("Empty target");
    }

    private IEnumerator<YieldInstruction> MuxFocus(CreatureState state) {
        if (state.characterFocus.IsValue(out Transform character)) 
            return enumeratorWithParam(new Target(character.GetComponentStrict<Character>()));
        if (state.terrainFocus is DesireMessage.Obstacle obstacle)
            return enumeratorWithParam(new Target(obstacle.location));
        throw new InvalidOperationException("Tried to build focused behavior without a focus");
    }
}