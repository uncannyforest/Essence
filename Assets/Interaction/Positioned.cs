using UnityEngine;

// Interface for classes like Target
// that may contain one of several things,
// whose most important property is their position.
public interface Positioned {
    public Vector3 GetPosition();
}
