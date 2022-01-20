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