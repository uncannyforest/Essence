using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class CustomTile {
    public interface I {
        void UpdateTile();
    }

    public static void UpdateTiles(Tilemap tilemap) {
        TileBase[] usedTiles = new TileBase[tilemap.GetUsedTilesCount()];
        tilemap.GetUsedTilesNonAlloc(usedTiles);
        foreach (TileBase tile in usedTiles) {
            if (tile is I) {
                ((I)tile).UpdateTile();
            }
        }
    }
}