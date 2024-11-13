using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskRunner {
    protected Func<IEnumerator<YieldInstruction>> task;
    protected Coroutine coroutine;
    protected MonoBehaviour attachedScript;
    public bool isRunning { get; protected set; }

    protected TaskRunner() {}

    public TaskRunner(Func<IEnumerator<YieldInstruction>> task, MonoBehaviour attachedScript) {
        this.task = task;
        this.attachedScript = attachedScript;
    }

    public void RunIf(bool on) {
        if (on) Start();
        else Stop();
    }

    public void Start() {
        Stop();
        isRunning = true;
        coroutine = attachedScript.StartCoroutine(task.Invoke());
    }

    public void Stop() {
        isRunning = false;
        if (coroutine != null) attachedScript.StopCoroutine(coroutine);
    }

    public void SwapOut(Func<IEnumerator<YieldInstruction>> task) {
        Stop();
        this.task = task;
        Start();
    }

    public static CoroutineWrapper<T> WithParam<T>(Func<T, IEnumerator<YieldInstruction>> enumeratorGenerator) {
        return new CoroutineWrapper<T>(enumeratorGenerator);
    }
}

public class CoroutineWrapper<T> {
    protected Func<T, IEnumerator<YieldInstruction>> enumeratorGenerator;

    public CoroutineWrapper(Func<T, IEnumerator<YieldInstruction>> enumeratorGenerator) {
        this.enumeratorGenerator = enumeratorGenerator;
    }

    public TaskRunner Of(T t, MonoBehaviour attachedScript) {
        return new TaskRunner(() => enumeratorGenerator(t), attachedScript);
    }
}

public class RunOnce : TaskRunner {
    private Action action;
    private float seconds;

    public RunOnce(MonoBehaviour attachedScript, float seconds, Action action) {
        this.action = action;
        this.seconds = seconds;
        this.attachedScript = attachedScript;
        this.task = RunOnceE;
    }

    public static RunOnce Run(MonoBehaviour attachedScript, float seconds, Action action) {
        RunOnce runOnce = new RunOnce(attachedScript, seconds, action);
        runOnce.Start();
        return runOnce;
    }

    private IEnumerator<YieldInstruction> RunOnceE() {
        yield return new WaitForSeconds(seconds);
        action();
        yield break;
    }
}