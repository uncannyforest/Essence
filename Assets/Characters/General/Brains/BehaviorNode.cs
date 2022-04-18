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

public class BehaviorNodeShell<T> {
    private Func<T, IEnumerator> enumeratorWithParam;

    public BehaviorNodeShell(Func<T, IEnumerator> enumeratorWithParam) {
        this.enumeratorWithParam = enumeratorWithParam;
    }

    virtual public BehaviorNode ToNode(T target) {
        return new BehaviorNode(() => enumeratorWithParam(target));
    }
}

public class LegacyBehaviorNode : BehaviorNode {
    public Target target { get; protected set; }

    public LegacyBehaviorNode(Func<IEnumerator> enumerator, Target target) : base(enumerator) {
        this.target = target;
    }
}

public class QueueOperator : BehaviorNode {
    private Queue<BehaviorNode> queue = new Queue<BehaviorNode>();

    override public Func<IEnumerator> enumerator {
        get => (queue.Peek()).enumerator;
        protected set => throw new NotSupportedException();
    }

    public QueueOperator() : base() {}
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

    public class Shell<T> : BehaviorNodeShell<T> {
        public Shell(Func<T, IEnumerator> enumeratorWithParam) : base(enumeratorWithParam) {}

        override public BehaviorNode ToNode(T target) =>
            QueueOperator.Of(base.ToNode(target));
    }

    public Target DeprecatedTargetAccessor { get => ((LegacyBehaviorNode)queue.Peek()).target; }
}

public class BehaviorNodeTest {
    public static void Test() {
        BehaviorNodeShell<int> testFactory = new BehaviorNodeShell<int>(TestE);
        BehaviorNode actualNode = testFactory.ToNode(6);
    }

    private static IEnumerator TestE(int i) {
        yield return null;
    }
}