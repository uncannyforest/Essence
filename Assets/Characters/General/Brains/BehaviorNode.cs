using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorNode {
    public CoroutineWrapper coroutine { get; protected set; }

    public BehaviorNode(CoroutineWrapper coroutine) {
        this.coroutine = coroutine;
    }
    
    virtual public BehaviorNode UpdateWithNewBehavior(BehaviorNode newNode) {
        return newNode;
    }
}

public class BehaviorNodeShell<T> {
    private CoroutineWrapper<T> coroutineWithParam;

    public BehaviorNodeShell(Func<T, IEnumerator> enumeratorWithParam) {
        this.coroutineWithParam = CoroutineWrapper.WithParam(enumeratorWithParam);
    }

    virtual public BehaviorNode ToNode(T target, MonoBehaviour attachedScript) {
        return new BehaviorNode(coroutineWithParam.Of(target, attachedScript));
    }
}

public class QueueOperator : BehaviorNode {
    private Queue<BehaviorNode> queue = new Queue<BehaviorNode>();

    public QueueOperator() : base(null) {}
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

    public class Shell<T> : BehaviorNodeShell<T> {
        public Shell(Func<T, IEnumerator> enumeratorWithParam) : base(enumeratorWithParam) {}

        override public BehaviorNode ToNode(T target, MonoBehaviour attachedScript) =>
            QueueOperator.Of(base.ToNode(target, attachedScript));
    }
}

public class BehaviorNodeTest {
    public static void Test() {
        BehaviorNodeShell<int> testFactory = new BehaviorNodeShell<int>(TestE);
        BehaviorNode actualNode = testFactory.ToNode(6, null);
    }

    private static IEnumerator TestE(int i) {
        yield return null;
    }
}