using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EnumeratorExtensions {
    public static YieldInstruction NextOrDefault(this IEnumerator e) {
        if (e.MoveNext()) return (YieldInstruction)e.Current;
        else return default;
    }
    public static bool MoveNext<T>(this IEnumerator e, out T next) {
        if (e.MoveNext()) {
            next = (T)e.Current;
            return true;
        } else {
            next = default;
            return false;
        }
    }
    public static T NextOr<T>(this IEnumerator e, Func<T> provider) {
        if (e.MoveNext()) return (T)e.Current;
        else return provider();
    }

    // Each step, tries first enumerator before trying second
    public static IEnumerator Then(this IEnumerator first, IEnumerator second) {
        while (true) {
            if (first.MoveNext()) yield return first.Current;
            else if (second.MoveNext()) yield return second.Current;
            else yield break;
        }
    }

    public static IEnumerator Then(this IEnumerator first, Func<YieldInstruction> second) {
        while (true) {
            if (first.MoveNext()) yield return first.Current;
            else yield return second();
        }
    }

    public static IEnumerator ThenEvery(this IEnumerator first, float seconds, Action second) {
        while (true) {
            if (first.MoveNext()) yield return first.Current;
            else {
                second();
                yield return new WaitForSeconds(seconds);
            }
        }
    }
}

public class Provisionally : IEnumerator {
    IEnumerator e;
    Func<object, bool> where;

    private Provisionally(IEnumerator e, Func<object, bool> where) {
        this.e = e;
        this.where = where;
    }

    public static Provisionally Run(IEnumerator e) => new Provisionally(e, null);

    public Provisionally Where(Func<object, bool> where) => new Provisionally(e, where);

    public bool MoveNext() {
        if (where == null) throw new InvalidOperationException("Provisionally must be used with where clause");
        return where(null) ? e.MoveNext() : false;
    }
    public void Reset() => e.Reset();
    public object Current => e.Current;

    public static Provisionally<T> For<T>(T target) {
        return Provisionally<T>.For(target);
    }
}

public class Provisionally<T> {
    T target;
    Func<T, bool> where;

    private Provisionally(T target, Func<T, bool> where) {
        this.target = target;
        this.where = where;
    }

    public static Provisionally<T> For(T target) => new Provisionally<T>(target, null);

    public Provisionally<T> Where(Func<T, bool> where) => new Provisionally<T>(target, where);

    public IEnumerator<U> Select<U>(Func<T, U> selector) {
        if (where == null) throw new InvalidOperationException("Provisionally must be used with where clause");
        while (where(target)) yield return selector(target);
    }
}
