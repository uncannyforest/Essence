using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Randoms {
    public static bool CoinFlip { get => Random.Range(0, 2) == 0; }
    public static Vector2Int Vector2Int(Vector2Int v0, Vector2Int v1) {
        return new Vector2Int(Random.Range(v0.x, v1.x), Random.Range(v0.y, v1.y));
    }
    public static Vector2Int Vector2Int(int x0, int y0, int x1, int y1) {
        return new Vector2Int(Random.Range(x0, x1), Random.Range(y0, y1));
    }
    public static Vector2Int InBounds(Bounds bounds) {
        return Randoms.Vector2Int(UnityEngine.Vector2Int.zero, (Vector2Int)bounds);
    }

    public static Vector2Int Midpoint(Vector2Int v0, Vector2Int v1) {
        int x = v0.x + v1.x;
        int y = v0.y + v1.y;
        int dx = (x % 2 == 0) ? 0 : Random.Range(0, 2);
        int dy = (y % 2 == 0) ? 0 : Random.Range(0, 2);
        return new Vector2Int(x / 2 + dx, y / 2 + dy);
    }

    public static Displacement RightAngleRotation(Displacement input) {
        int rotation = Random.Range(0, 4);
        switch(rotation) {
            case 1: return new Displacement(-input.y, input.x);
            case 2: return new Displacement(-input.x, -input.y);
            case 3: return new Displacement(input.y, -input.x);
            default: return input;
        }
    }

    public static Vector2 ChebyshevUnit() {
        float mean = Random.Range(-1f, 1f);
        float extreme = CoinFlip ? 1 : -1;
        if (CoinFlip) {
            return new Vector2(mean, extreme);
        } else {
            return new Vector2(extreme, mean);
        }
    }
}
