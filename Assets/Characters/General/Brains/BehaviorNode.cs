using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorNode {
    virtual public Func<IEnumerator> enumerator { get; protected set; }

    protected BehaviorNode() {}
    public BehaviorNode(Func<IEnumerator> enumerator) {
        this.enumerator = enumerator;
    }
    
    virtual public BehaviorNode UpdateWithNewBehavior(BehaviorNode newNode) {
        return newNode;
    }
}

public class RestrictNearbyBehavior : BehaviorNode {
    private BehaviorNode subBehavior;
    private Transform ai;
    private Target target;
    private float distance;

    public RestrictNearbyBehavior(BehaviorNode subBehavior, Transform ai, Target target, float distance) {
        this.subBehavior = subBehavior;
        this.ai = ai;
        this.target = target;
        this.distance = distance;
        this.enumerator = E;
    }
    
    public IEnumerator E() {
        IEnumerator task = subBehavior.enumerator(); // saved here, not reset unless RunBehavior() is called again
        while (Vector3.Distance(ai.position, target.Position) < distance && task.MoveNext()) {
            yield return task.Current;
        }
    }
}

public class TargetedBehavior<T> {
    public Func<T, IEnumerator> enumeratorWithParam;

    protected TargetedBehavior() {}
    public TargetedBehavior(Func<T, IEnumerator> enumeratorWithParam) {
        this.enumeratorWithParam = enumeratorWithParam;
    }
    public TargetedBehavior(Func<T, YieldInstruction> singleLine) {
        this.enumeratorWithParam = (target) => FromSingleLine(singleLine, target);
    }

    virtual public BehaviorNode WithTarget(T target) {
        return new BehaviorNode(() => CheckNonNullTargetEnumerator(target));
    }

    private IEnumerator FromSingleLine(Func<T, YieldInstruction> line, T target) {
        while (true) yield return line(target);
    }

    private IEnumerator CheckNonNullTargetEnumerator(T target) {
        IEnumerator task = enumeratorWithParam(target);
        while (target != null && task.MoveNext()) {
            yield return task.Current;
        }
    }

    public TargetedBehavior<U> For<U>(Func<U, T> func) => new TargetedBehavior<U>(
        (target) =>enumeratorWithParam(func(target))
    );
}

public class CharacterTargetedBehavior : TargetedBehavior<Transform> {
    public CharacterTargetedBehavior(Func<Transform, IEnumerator> enumeratorWithParam) : base(enumeratorWithParam) {}
    public CharacterTargetedBehavior(Func<Transform, YieldInstruction> singleLine) : base(singleLine) {}

    public TargetedBehavior<Target> ForTarget() => For<Target>(t => ((SpriteSorter)t).Character);
}

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

    public class Targeted<T> : TargetedBehavior<T> {
        public Targeted(Func<T, IEnumerator> enumeratorWithParam) : base(enumeratorWithParam) {}

        override public BehaviorNode WithTarget(T target) =>
            QueueOperator.Of(base.WithTarget(target));
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
}