using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// usage: behaviorNode.enumerator()
public class BehaviorNode {
    virtual public Func<IEnumerator> enumerator { get; protected set; }

    protected BehaviorNode() {}
    public BehaviorNode(Func<IEnumerator> enumerator) {
        this.enumerator = enumerator;
    }
    public BehaviorNode(Func<YieldInstruction> singleLine) {
        this.enumerator = () => FromSingleLine(singleLine);
    }
    
    // By default, replace old behavior with new one.
    // Can be overriden to chain commands
    virtual public BehaviorNode UpdateWithNewBehavior(BehaviorNode newNode) {
        return newNode;
    }

    private IEnumerator FromSingleLine(Func<YieldInstruction> line) {
        while (true) yield return line();
    }
}

// places condition on subBehavior to give up when target exceeds distance
public class RestrictNearbyBehavior : BehaviorNode {
    private BehaviorNode subBehavior;
    private Transform ai;
    private Func<Vector2> targetLocation;
    private float distance;

    public RestrictNearbyBehavior(BehaviorNode subBehavior, Transform ai, Func<Vector2> targetLocation, float distance) {
        this.subBehavior = subBehavior;
        this.ai = ai;
        this.targetLocation = targetLocation;
        this.distance = distance;
        this.enumerator = E;
    }
    
    public IEnumerator E() {
        IEnumerator task = subBehavior.enumerator(); // saved here, not reset unless RunBehavior() is called again
        while (Vector3.Distance(ai.position, targetLocation()) < distance && task.MoveNext()) {
            yield return task.Current;
        }
    }
}

// behavior generator with unspecified target
// 1. At construction:
//        targetedBehavior = new TargetedBehavior(targetToEnumeratorFunction)
// 2. When target is known:
//        behaviorNode = targetedBehavior.WithTarget(target)
public class TargetedBehavior<T> {
    virtual public bool canQueue { get; protected set; } = false;

    public Func<T, IEnumerator> enumeratorWithParam;

    protected TargetedBehavior() {}
    public TargetedBehavior(Func<T, IEnumerator> enumeratorWithParam) {
        this.enumeratorWithParam = enumeratorWithParam;
    }
    public TargetedBehavior(Func<T, YieldInstruction> singleLine) {
        this.enumeratorWithParam = (target) => FromSingleLine(singleLine, target);
    }
    public TargetedBehavior(Func<T, Optional<YieldInstruction>> singleLine) {
        this.enumeratorWithParam = (target) => FromSingleLine(singleLine, target);
    }

    virtual public BehaviorNode WithTarget(T target) {
        return new BehaviorNode(() => CheckNonNullTargetEnumerator(target));
    }

    private IEnumerator FromSingleLine(Func<T, YieldInstruction> line, T target) {
        while (true) yield return line(target);
    }
    private IEnumerator FromSingleLine(Func<T, Optional<YieldInstruction>> line, T target) {
        while (true) {
            Optional<YieldInstruction> next = line(target);
            if (next.HasValue) yield return next.Value;
            else yield break;
        }
    }

    private IEnumerator CheckNonNullTargetEnumerator(T target) {
        IEnumerator task = enumeratorWithParam(target);
        while (target != null && task.MoveNext()) {
            yield return task.Current;
        }
    }

    // not currently used
    public TargetedBehavior<U> For<U>(Func<U, T> func) => new TargetedBehavior<U>(
        (target) =>enumeratorWithParam(func(target))
    );

    public QueueOperator.Targeted<T> Queued() =>
        new QueueOperator.Targeted<T>(enumeratorWithParam);
}

// common TargetedBehavior use case, implementation simply specifies <Transform>
public class CharacterTargetedBehavior : TargetedBehavior<Transform> {
    public CharacterTargetedBehavior(Func<Transform, IEnumerator> enumeratorWithParam) : base(enumeratorWithParam) {}
    public CharacterTargetedBehavior(Func<Transform, YieldInstruction> singleLine) : base(singleLine) {}
    public CharacterTargetedBehavior(Func<Transform, Optional<YieldInstruction>> singleLine) : base(singleLine) {}

    // not currently used. See CreatureAction.WithCharacter()
    public TargetedBehavior<Target> ForTarget() => For<Target>(t => ((Character)t).transform);
}

// queue multiple sub-behaviors
public class QueueOperator : BehaviorNode {
    private Queue<BehaviorNode> queue = new Queue<BehaviorNode>();

    public QueueOperator() : base() {
        this.enumerator = QueueEnumerator;
    }

    public static QueueOperator Of(BehaviorNode node) {
        QueueOperator queueNode = new QueueOperator();
        queueNode.queue.Enqueue(node);
        return queueNode;
    }

    override public BehaviorNode UpdateWithNewBehavior(BehaviorNode newNode) {
        if (newNode is QueueOperator newQueue) {
            foreach (BehaviorNode node in newQueue.queue) queue.Enqueue(node);
            return this;
        }
        else return newNode;
    }

    public bool Pop() {
        queue.Dequeue();
        return (queue.Count > 0);
    }

    private IEnumerator QueueEnumerator() {
        while (queue.Count > 0) {
            IEnumerator subBehavior = queue.Peek().enumerator(); // saved here, not reset unless RunBehavior() is called again
            while (subBehavior.MoveNext()) {
                yield return subBehavior.Current;
            }
            Pop();
        }
    }
    
    public class Targeted<T> : TargetedBehavior<T> {
        public Targeted(Func<T, IEnumerator> enumeratorWithParam) : base(enumeratorWithParam) {
            this.canQueue = true;
        }

        override public BehaviorNode WithTarget(T target) =>
            QueueOperator.Of(base.WithTarget(target));
    }
}