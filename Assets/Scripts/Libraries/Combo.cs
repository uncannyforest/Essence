using System;
using UnityEngine;

public static class Combo {
    public static readonly Combo<Vector2Int, int> Vector2Int =
        new Combo<Vector2Int, int>((a, b, f) => new Vector2Int(f(a.x, b.x), f(a.y, b.y)));
    public static readonly Combo<Vector2, float> Vector2 =
        new Combo<Vector2, float>((a, b, f) => new Vector2(f(a.x, b.x), f(a.y, b.y)));

    public static Combo<Vector2Int, int>.Intermediary Of(Vector2Int a, Vector2Int b) {
        return Vector2Int.Of(a, b);
    }

    public static Combo<Vector2, float>.Intermediary Of(Vector2 a, Vector2 b) {
        return Vector2.Of(a, b);
    }
}

public class Combo<T, U> {
    private Func<T, T, Func<U, U, U>, T> translator;

    public Combo(Func<T, T, Func<U, U, U>, T> translator) {
        this.translator = translator;
    }

    public class Intermediary {
        private T a;
        private T b;
        private Func<T, T, Func<U, U, U>, T> translator;

        public Intermediary(T a, T b, Func<T, T, Func<U, U, U>, T> translator) {
            this.a = a;
            this.b = b;
            this.translator = translator;
        }

        public T With(Func<U, U, U> combiner) {
            return translator(a, b, combiner);
        }
    }

    public Intermediary Of(T a, T b) {
        return new Intermediary(a, b, translator);
    }
}