using System;
using UnityEngine;

// Consider https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Vector2.cs
[Serializable]
public struct ScreenVector {
    [SerializeField] public float x;
    [SerializeField] public float y;

    // To make comparison operators easier
    private Vector2 Vector2 { get => new Vector2(x, y); }

    public ScreenVector(Vector3 vector) {
        this.x = vector.x;
        this.y = vector.y;
    }
    public ScreenVector(float x, float y) {
        this.x = x;
        this.y = y;
    }
    
    private static ScreenVector SV(float x, float y) => new ScreenVector(x, y);

    public static ScreenVector operator+(ScreenVector a, ScreenVector b) => SV(a.x + b.x, a.y + b.y);
    public static ScreenVector operator-(ScreenVector a, ScreenVector b) => SV(a.x - b.x, a.y - b.y);
    public static ScreenVector operator*(ScreenVector v, float f) => SV(f * v.x, f * v.y);
    public static ScreenVector operator*(float f, ScreenVector v) => SV(f * v.x, f * v.y);
    public static bool operator==(ScreenVector lhs, ScreenVector rhs) => lhs.Vector2 == rhs.Vector2;
    public static bool operator!=(ScreenVector lhs, ScreenVector rhs) => lhs.Vector2 != rhs.Vector2;
    public override bool Equals(object other) => (other is ScreenVector) && this == (ScreenVector)other;
    public bool Equals(ScreenVector other) => this == other;
    public override int GetHashCode() => this.Vector2.GetHashCode();

}
