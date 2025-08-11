using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enumerators {
    public static IEnumerator<YieldInstruction> Continually(Func<YieldInstruction> action) {
        while (true) {
            yield return action();
        }
    }

    public static IEnumerator<YieldInstruction> AfterWait(Func<float> seconds, Action action) {
        yield return new WaitForSeconds(seconds());
        action();
    }
}

public static class EnumeratorExtensions {
    public static T NextOrDefault<T>(this IEnumerator<T> e) {
        if (e.MoveNext()) return e.Current;
        else return default;
    }
    public static bool MoveNext<T>(this IEnumerator<T> e, out T next) {
        if (e.MoveNext()) {
            next = e.Current;
            return true;
        } else {
            next = default;
            return false;
        }
    }
    public static T NextOr<T>(this IEnumerator<T> e, Func<T> provider) {
        if (e.MoveNext()) return (T)e.Current;
        else return provider();
    }

    // Each step, tries first enumerator before trying second
    public static IEnumerator<T> Then<T>(this IEnumerator<T> first, IEnumerator<T> second) {
        while (true) {
            if (first.MoveNext()) yield return first.Current;
            else if (second.MoveNext()) yield return second.Current;
            else yield break;
        }
    }

    public static IEnumerator<T> Then<T>(this IEnumerator<T> first, Func<T> second) {
        while (true) {
            if (first.MoveNext()) yield return first.Current;
            else yield return second();
        }
    }

    public static IEnumerator<YieldInstruction> ThenEvery(this IEnumerator<YieldInstruction> first, float seconds, Action second) {
        while (true) {
            if (first.MoveNext()) yield return first.Current;
            else {
                yield return new WaitForSeconds(seconds);
                second();
            }
        }
    }

    public static IEnumerator<YieldInstruction> ThenOnce(this IEnumerator<YieldInstruction> first, Func<float> seconds, Action second) {
        while (first.MoveNext()) {
            yield return first.Current;
        }
        yield return new WaitForSeconds(seconds());
        second();
        yield break;
    }
}

// Provisionally ends the Enumerator once Where returns false.
public static class Provisionally {
    public static Provisionally<T> Run<T>(IEnumerator<T> e) {
        return Provisionally<T>.Run(e);
    }
}
public class Provisionally<T> : IEnumerator<T> {
    IEnumerator<T> e;
    Func<object, bool> where;

    private Provisionally(IEnumerator<T> e, Func<object, bool> where) {
        this.e = e;
        this.where = where;
    }

    public static Provisionally<T> Run(IEnumerator<T> e) => new Provisionally<T>(e, null);

    public Provisionally<T> Where(Func<object, bool> where) => new Provisionally<T>(e, where);

    public bool MoveNext() {
        if (where == null) throw new InvalidOperationException("Provisionally must be used with where clause");
        return where(null) ? e.MoveNext() : false;
    }
    public void Reset() => e.Reset();
    public T Current => e.Current;
    object IEnumerator.Current => e.Current;
    public void Dispose() {}
}

// Like Provisionally, Continually ends the Enumerator once Where returns false.
// In takes a single target object as parameter, which is passed along repeatedly.
// 
// Provisionally may be better when you don't want the provided IEnumerator (in Select()) to start over.
public static class Continually {
    public static Continually<T> For<T>(T target) {
        return Continually<T>.For(target);
    }
}
public class Continually<T> {
    T target;
    Func<T, bool> where;

    private Continually(T target, Func<T, bool> where) {
        this.target = target;
        this.where = where;
    }

    public static Continually<T> For(T target) => new Continually<T>(target, null);

    public Continually<T> Where(Func<T, bool> where) => new Continually<T>(target, where);

    public IEnumerator<U> Select<U>(Func<T, IEnumerator<U>> selector) {
        if (where == null) throw new InvalidOperationException("Continually must be used with where clause");
        while (where(target)) {
            IEnumerator<U> enumerator = selector(target);
            if (enumerator.MoveNext()) yield return enumerator.Current;
            else yield break;
        }
    }
    public IEnumerator<U> Select<U>(Func<T, U> selector) {
        if (where == null) throw new InvalidOperationException("Continually must be used with where clause");
        while (where(target)) yield return selector(target);
    }
}
