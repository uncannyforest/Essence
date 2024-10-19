using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSubCell3D : MonoBehaviour {
    public int landHash = -1;

    public void MaybeRender(TileMaterial here, TileMaterial left, TileMaterial cc, TileMaterial right, TileMaterial oppLeft, TileMaterial oppRight) {
        Action<Transform> render = TileLibrary3D.E.temperate[here].Render(left, right, cc, oppLeft, oppRight, out int hashCode);
        int newHash = here.GetEnumHashCode() * 10000 + hashCode;
        if (landHash != newHash) {
            foreach (Transform child in transform) GameObject.Destroy(child.gameObject); 
            render(transform);
            landHash = newHash;
        }
    }
}
