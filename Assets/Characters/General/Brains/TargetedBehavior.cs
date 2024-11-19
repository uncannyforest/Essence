using System;
using System.Collections.Generic;
using UnityEngine;

public interface FlexSourceBehavior {
    public CreatureAction CreatureAction(Sprite sprite);

    public IEnumerator<YieldInstruction> FocusedBehavior(Brain brain);
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

    public IEnumerator<YieldInstruction> FocusedBehavior(Brain brain) { yield break; }
}

public class CharacterTargetedBehavior : TargetedBehavior<Transform>, FlexSourceBehavior {
    public Func<Transform, bool> silentFilter;

    public CharacterTargetedBehavior(
            Func<Transform, IEnumerator<YieldInstruction>> enumeratorWithParam,
            Func<Transform, bool> silentFilter,
            Func<Transform, WhyNot> errorFilter)
            : base(enumeratorWithParam, errorFilter) {
        this.silentFilter = silentFilter;
    }

    public CreatureAction CreatureAction(Sprite sprite) => global::CreatureAction.WithCharacter(sprite, this, silentFilter);

    public IEnumerator<YieldInstruction> FocusedBehavior(Brain brain) => enumeratorWithParam(brain.state.characterFocus.Value);
}
