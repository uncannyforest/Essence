using System;
using System.Collections;
using UnityEngine;

public class CoroutineWrapper {
    protected Func<IEnumerator> enumeratorGenerator;
    protected Coroutine coroutine;
    protected MonoBehaviour attachedScript;
    protected bool isRunning;

    protected CoroutineWrapper() {}

    public CoroutineWrapper(Func<IEnumerator> enumeratorGenerator, MonoBehaviour attachedScript) {
        this.enumeratorGenerator = enumeratorGenerator;
        this.attachedScript = attachedScript;
    }

    public void RunIf(bool on) {
        if (on) Start();
        else Stop();
    }

    public void Start() {
        Stop();
        isRunning = true;
        coroutine = attachedScript.StartCoroutine(enumeratorGenerator.Invoke());
    }

    public void Stop() {
        isRunning = false;
        if (coroutine != null) attachedScript.StopCoroutine(coroutine);
    }

    public bool IsRunning {
        get => isRunning;
    }

    public static CoroutineWrapper<T> WithParam<T>(Func<T, IEnumerator> enumeratorGenerator) {
        return new CoroutineWrapper<T>(enumeratorGenerator);
    }
}

public class CoroutineWrapper<T> {
    protected Func<T, IEnumerator> enumeratorGenerator;

    public CoroutineWrapper(Func<T, IEnumerator> enumeratorGenerator) {
        this.enumeratorGenerator = enumeratorGenerator;
    }

    public CoroutineWrapper Of(T t, MonoBehaviour attachedScript) {
        return new CoroutineWrapper(() => enumeratorGenerator(t), attachedScript);
    }
}

public class RunOnce : CoroutineWrapper {
    private Action action;
    private float seconds;

    public RunOnce(MonoBehaviour attachedScript, float seconds, Action action) {
        this.action = action;
        this.seconds = seconds;
        this.attachedScript = attachedScript;
        this.enumeratorGenerator = RunOnceE;
    }

    public static RunOnce Run(MonoBehaviour attachedScript, float seconds, Action action) {
        RunOnce runOnce = new RunOnce(attachedScript, seconds, action);
        runOnce.Start();
        return runOnce;
    }

    private IEnumerator RunOnceE() {
        yield return new WaitForSeconds(seconds);
        action();
        yield break;
    }
}