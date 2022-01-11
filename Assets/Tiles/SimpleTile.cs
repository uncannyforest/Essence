using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

[CreateAssetMenu(menuName = "Simple Tile")]
public class SimpleTile : Tile {
    public bool canMirrorX = false;

    public Orientation ViewOrientation {
        get {
    //         if (rotationManager == null) rotationManager = GameObject.FindObjectOfType<RotationManager>();
            return Orientor.Rotation;
        }
    }
    // private RotationManager rotationManager;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.flags = TileFlags.LockTransform;
        tileData.transform = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0f, 0f, (int)ViewOrientation),
                (canMirrorX && ViewOrientation.IsLeftOrRight()) ? new Vector3(-1f, 2f, 1f) : new Vector3(1f, 2f, 1f)
            );
    }
}