using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// usage: behaviorNode.enumerator()
public class BehaviorNode {
    virtual public Func<IEnumerator<YieldInstruction>> enumerator { get; protected set; }

    protected BehaviorNode() {}
    public BehaviorNode(Func<IEnumerator<YieldInstruction>> enumerator) {
        this.enumerator = enumerator;
    }
    public BehaviorNode(Func<YieldInstruction> singleLine) {
        this.enumerator = SingleLine(singleLine);
    }
    
    // By default, replace old behavior with new one.
    // Can be overriden to chain commands
    virtual public BehaviorNode UpdateWithNewBehavior(BehaviorNode newNode) {
        return newNode;
    }

    public static Func<IEnumerator<YieldInstruction>> SingleLine(Func<YieldInstruction> line) => () => SingleLineEnumerator(line);
    public static IEnumerator<YieldInstruction> SingleLineEnumerator(Func<YieldInstruction> line) {
        while (true) yield return line();
    }
}

// places condition on subBehavior to give up when target exceeds distance
public class RestrictNearbyBehavior : BehaviorNode {
    private Func<IEnumerator<YieldInstruction>> subBehavior;
    private Transform ai;
    private Func<Vector2> targetLocation;
    private float distance;

    public RestrictNearbyBehavior(Func<IEnumerator<YieldInstruction>> subBehavior, Transform ai, Func<Vector2> targetLocation, float distance) {
        this.subBehavior = subBehavior;
        this.ai = ai;
        this.targetLocation = targetLocation;
        this.distance = distance;
        this.enumerator = E;
    }
    
    public IEnumerator<YieldInstruction> E() => 
        from _ in Provisionally.Run(subBehavior())
        where Vector3.Distance(ai.position, targetLocation()) < distance
        select _;
}

// queue multiple sub-behaviors
public class QueueOperator : BehaviorNode {
    private Queue<BehaviorNode> queue = new Queue<BehaviorNode>();

    public QueueOperator() : base() {
        this.enumerator = QueueEnumerator;
    }

    public static OneOf<BehaviorNode, string> Of(OneOf<BehaviorNode, string> node) {
        if (node.Is(out string error)) return error;

        QueueOperator queueNode = new QueueOperator();
        queueNode.queue.Enqueue((BehaviorNode)node);
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

    private IEnumerator<YieldInstruction> QueueEnumerator() {
        while (queue.Count > 0) {
            IEnumerator<YieldInstruction> subBehavior = queue.Peek().enumerator(); // saved here, not reset unless RunBehavior() is called again
            while (subBehavior.MoveNext()) {
                yield return subBehavior.Current;
            }
            Pop();
        }
    }
    
    public class Targeted<T> : TargetedBehavior<T> {
        public Targeted(Func<T, IEnumerator<YieldInstruction>> enumeratorWithParam, Func<T, WhyNot> errorFilter) : base(enumeratorWithParam, errorFilter) {
            this.canQueue = true;
        }

        override public OneOf<BehaviorNode, string> WithTarget(T target) =>
            QueueOperator.Of(base.WithTarget(target));
    }
}