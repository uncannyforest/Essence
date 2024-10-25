using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfView : MonoBehaviour {
    public Action<Vector2Int> CrossedTile;

    private Vector2Int currentTile;

    void LateUpdate() {
        if (transform.hasChanged && CrossedTile != null) {
            Vector2Int oldTile = currentTile;
            currentTile = Terrain.I.CellAt(transform.position);
            if (oldTile != currentTile) CrossedTile(currentTile);
            transform.hasChanged = false;
        }
    }
}
