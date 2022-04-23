using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disp {
    public static Displacement FT(Vector2 from, Vector2 to) => new Displacement(from, to);
}

public struct Displacement {
    private Vector2 displacement;
    private Displacement(Vector2 displacement) {
        this.displacement = displacement;
    }
    public Displacement(float x, float y) {
        displacement = new Vector2(x, y);
    }
    public Displacement(Vector2 from, Vector2 to) {
        displacement = to - from;
    }

    public static Displacement zero { get => new Displacement(0, 0); }

    public static explicit operator Displacement(Vector2 v) => new Displacement(v);

    public static bool operator==(Displacement a, Displacement b) => a.displacement == b.displacement;
    public static bool operator!=(Displacement a, Displacement b) => a.displacement != b.displacement;
    public static Vector2 operator+(Displacement d, Vector2 v) => v + d.displacement;
    public static Vector2 operator+(Vector2 v, Displacement d) => v + d.displacement;
    public static Vector2 operator+(Displacement d, Vector3 v) => (Vector2)v + d.displacement;
    public static Vector2 operator+(Vector3 v, Displacement d) => (Vector2)v + d.displacement;
    public static Displacement operator+(Displacement a, Displacement b)
        => new Displacement(a.displacement + b.displacement);
    public static Displacement operator-(Displacement a, Displacement b)
        => new Displacement(a.displacement - b.displacement);
    public static Displacement operator*(Displacement d, float f)
        => new Displacement(f * d.displacement);
    public static Displacement operator*(float f, Displacement d)
        => new Displacement(f * d.displacement);
    public static Displacement operator/(Displacement d, float f)
        => new Displacement(d.displacement / f);
    public static Displacement operator*(Quaternion q, Displacement d)
        => new Displacement(q * d.displacement);

    public float x { get => displacement.x; }
    public float y { get => displacement.y; }
    public float sqrMagnitude { get => displacement.sqrMagnitude; }

    public float chebyshevMagnitude {
        get => Mathf.Max(Mathf.Abs(this.x), Mathf.Abs(this.y));
    }

    public float angle {
        get => Vector2.SignedAngle(Vector3.right, displacement);
    }

    public Vector2 ToVelocity(float speed) => displacement.normalized * speed;

    // Rotate index * 90 degrees, but there's no reason to multiply by 90 here
    public Displacement RotateRightAngles(int numRightAngles) {
        switch(numRightAngles) {
            case 1: return new Displacement(-this.y, this.x);
            case 2: return new Displacement(-this.x, -this.y);
            case 3: return new Displacement(this.y, -this.x);
            default: return this;
        }
    }

    override public bool Equals(object obj) {
        if (obj is Displacement d) return this == d;
        else return false;
    }
    override public int GetHashCode() {
        return displacement.GetHashCode();
    }
}
