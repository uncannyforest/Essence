using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell3D : MonoBehaviour {
    public int landHash = -1;

    public void MaybeRender(Land here, Land left, Land cc, Land right, Land oppLeft, Land oppRight) {
        Action<Transform> render = TileLibrary3D.E.temperate[here].Render(left, right, cc, oppLeft, oppRight, out int hashCode);
        int newHash = (int)here * 100 + hashCode;
        Debug.Log(here + " old hash " + landHash + ", new hash " + newHash);
        if (landHash != newHash) {
            foreach (Transform child in transform) GameObject.Destroy(child); 
            render(transform);
            landHash = newHash;
        }
    }
}
