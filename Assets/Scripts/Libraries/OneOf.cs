
using System;
using UnityEngine;

public class OneOf<T, U> {
    public enum Which { Neither, First, Second }

    public Which which = Which.Neither;
    public Type WhichType {
        get => which == Which.First ? typeof(T) : which == Which.Second ? typeof(U) : typeof(void);
    }

    private T t;
    private U u;

    public OneOf(T t) {
        this.t = t;
        which = Which.First;
    }

    public OneOf(U u) {
        this.u = u;
        which = Which.Second;
    }

    private OneOf() {
        which = Which.Neither;
    }

    public static OneOf<T, U> Neither {
        get => new OneOf<T, U>();
    }

    public static explicit operator T(OneOf<T, U> oneOf) {
        if (oneOf.which == Which.First) return oneOf.t;
        else return default(T);
    }

    public static explicit operator U(OneOf<T, U> oneOf) {
        if (oneOf.which == Which.Second) return oneOf.u;
        else return default(U);
    }

    public bool Is(out T tOut) {
        if (which == Which.First) {
            tOut = t;
            return true;
        }
        tOut = default(T);
        return false;
    }

    public bool Is(out U uOut) {
        if (which == Which.Second) {
            uOut = u;
            return true;
        }
        uOut = default(U);
        return false;
    }

}

public class OneOfTest {
    public static void Main() {
        OneOf<Vector2Int, GameObject> a = new OneOf<Vector2Int, GameObject>(Vector2Int.right);
        OneOf<Vector2Int, GameObject> b = new OneOf<Vector2Int, GameObject>(new GameObject());
        OneOf<Vector2Int, GameObject> c = OneOf<Vector2Int, GameObject>.Neither;

        // First way

        if (b.Is(out Vector2Int vector)) {
            Debug.Log(vector);
        } else if (b.Is(out GameObject go)) {
            Debug.Log(go);
        }
        Debug.Log(vector); // warning: still in scope

        // Second way

        if (b.WhichType == typeof(Vector2Int)) {
            Vector2Int b2 = (Vector2Int)b;
            // do stuff
        } else if (b.WhichType == typeof(GameObject)) {
            GameObject b2 = (GameObject)b;
            // do stuff
        }

        // Third way

        Vector2Int a1 = (Vector2Int)a;
        GameObject b1 = (GameObject)a;

        if (a1 != Vector2Int.zero) { // if you don't like this, use Vector2Int? instead
            // do stuff
        } else if (b1 != null) {
            // do stuff
        }

    }
}