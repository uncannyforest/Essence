using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Bounds {
    public readonly int x;
    public readonly int y;

    public Bounds(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public Vector2Int Vector2Int {
        get => new Vector2Int(x, y);
    }
    public static explicit operator Vector2Int(Bounds b) => b.Vector2Int;
    public Vector2 Vector2 {
        get => new Vector2(x, y);
    }
    public static explicit operator Vector2(Bounds b) => b.Vector2;

    public bool Contains(Vector2Int coord) {
        return (coord.x >= 0 && coord.y >= 0 && coord.x < x && coord.y < y);
    }

    public Vector2Int Wrap(Vector2Int input) {
        int outX = input.x - x * Mathf.FloorToInt((float)input.x / x);
        int outY = input.y - y * Mathf.FloorToInt((float)input.y / y);
        return new Vector2Int(outX, outY);
    }

    public Vector2Int Clamp(Vector2Int input) {
        int outX = Mathf.Clamp(input.x, 0, x - 1);
        int outY = Mathf.Clamp(input.y, 0, y - 1);
        return new Vector2Int(outX, outY);
    }
}
