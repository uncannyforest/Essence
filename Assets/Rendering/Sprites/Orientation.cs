using UnityEngine;

public enum Orientation {    
    Default = 0,
    Right = 90,
    Opposite = 180,
    Left = 270
}

public static class OrientationExtensions {
    public static bool IsLeftOrRight(this Orientation orientation) {
        return orientation == Orientation.Left || orientation == Orientation.Right;
    }
}