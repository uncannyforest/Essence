using System;
using System.Collections.Generic;
using UnityEngine;

public static class Vct {
    public static readonly Vector2Int[] Cardinals = new Vector2Int[]
        { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    public static Vector2Int I(int x, int y) {
        return new Vector2Int(x, y);
    }

    public static Vector2 F(float x, float y) {
        return new Vector2(x, y);
    }

    public static Vector2 DirectionToVelocity(float? direction) {
        if (direction is float realDirection) return Quaternion.Euler(0, 0, realDirection) * Vector2.right;
        else return Vector2.zero;
    }
}

public static class GameObjectExtensions {
    public static T GetComponentStrict<T>(this Transform t) {
        return t.GetComponent<T>() ?? throw new ArgumentException
            ("Transform " + t + " in layer " + t.gameObject.layer + " has no " + typeof(T));
    }

    public static T GetComponentStrict<T>(this Collider2D c) {
        return c.GetComponent<T>() ?? throw new ArgumentException
            ("Collider " + c + " in layer " + c.gameObject.layer + " has no " + typeof(T));
    }

    public static T GetComponentStrict<T>(this MonoBehaviour mb) {
        return mb.GetComponent<T>() ?? throw new ArgumentException
            ("MonoBehaviour " + mb + " in layer " + mb.gameObject.layer + " has no " + typeof(T));
    }

    public static T GetComponentStrict<T>(this GameObject go) {
        return go.GetComponent<T>() ?? throw new ArgumentException
            ("GameObject " + go + " in layer " + go.layer + " has no " + typeof(T));
    }

    public static bool Contains(this LayerMask mask, int layer) {
        return mask == (mask | (1 << layer));
    }

    public static bool LayerIsIn(this GameObject go, params string[] layerNames) {
        return ((LayerMask)LayerMask.GetMask(layerNames)).Contains(go.layer);
    }
}

public static class VectorExtensions {
    public static float? VelocityToDirection(this Vector2 value) {
        if (value == Vector2.zero) return (float?)null;
        return Vector3.SignedAngle(Vector3.right, (Vector2)value, Vector3.forward);
    }

    public static Vector2Int Transform(this Vector2Int vec, Func<int, int> x, Func<int, int> y) {
        return new Vector2Int(x(vec.x), y(vec.y));
    }

    public static Vector2Int Map(this Vector2Int vec, Func<int, int> func) {
        return new Vector2Int(func(vec.x), func(vec.y));
    }
    public static Vector2 Map(this Vector2 vec, Func<float, float> func) {
        return new Vector2(func(vec.x), func(vec.y));
    }

    public static Vector2Int FloorToInt(this Vector2 vec) {
        return new Vector2Int(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
    }

    public static int ChebyshevMagnitude(this Vector2Int vec) {
        return Mathf.Max(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
    }

    public static float ChebyshevMagnitude(this Vector2 vec) {
        return Mathf.Max(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
    }

    public static Vector3 WithZ(this Vector2 vector, float z) {
        return new Vector3(vector.x, vector.y, z);
    }

    public static Vector2Int ToRight(this Vector2Int vec) {
        return vec + Vector2Int.right;
    }

    public static Vector2Int Ahead(this Vector2Int vec) {
        return vec + Vector2Int.up;
    }

    public static Vector2Int ToLeft(this Vector2Int vec) {
        return vec + Vector2Int.left;
    }

    public static Vector2Int Behind(this Vector2Int vec) {
        return vec + Vector2Int.down;
    }

    public static bool IsCardinal(this Vector2Int vec) {
        return vec.x * vec.y == 0;
    }

    // Nonsense output if IsCardinal == false
    public static int GetCardinal(this Vector2Int vec) {
        int result = 0;
        if (vec.x == 0) result += 1;
        if (vec.x + vec.y < 0) result += 2;
        return result;
    }

    // Nonsense output if IsCardinal == false
    public static int CardinalMagnitude(this Vector2Int vec) {
        return Mathf.Abs(vec.x + vec.y);
    }

    public static bool AllFourSides(this Vector2Int vec, Func<Vector2Int, bool> func) {
        return func(vec.ToRight()) &&
            func(vec.Ahead()) &&
            func(vec.ToLeft()) &&
            func(vec.Behind());
    }

    public static Action AllFourSides(this Vector2Int vec, Action<Vector2Int> func) {
        return () => {
            func(vec.ToRight());
            func(vec.Ahead());
            func(vec.ToLeft());
            func(vec.Behind());
        };
    }

    public static bool AnyFourSides(this Vector2Int vec, Func<Vector2Int, bool> func) {
        return func(vec.ToRight()) ||
            func(vec.Ahead()) ||
            func(vec.ToLeft()) ||
            func(vec.Behind());
    }

    // Rotate index * 90 degrees, but there's no reason to multiply by 90 here
    public static Vector2 RotateRightAngles(this Vector2 input, int numRightAngles) {
        switch(numRightAngles) {
            case 1: return new Vector2(-input.y, input.x);
            case 2: return new Vector2(-input.x, -input.y);
            case 3: return new Vector2(input.y, -input.x);
            default: return input;
        }
    }
}