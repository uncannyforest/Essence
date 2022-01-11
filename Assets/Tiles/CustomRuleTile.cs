using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

[CreateAssetMenu(menuName = "Custom Rule Tile")]
public class CustomRuleTile : IsometricRuleTile<CustomRuleTile.Neighbor> {
    public Color color = Color.white;
    public bool canMirrorX = false;
    public bool mirrorX = false;
    public TileBase[] tilesToConnect;
    // public TileBase tileThree;

    // private RotationManager rotationManager;
    public Orientation ViewOrientation {
        get {
        //     if (rotationManager == null) CacheRotationManager();
        //     if (rotationManager == null) {Debug.Log("Aaaauugh!");}
            return Orientor.Rotation;
        }
    }

    public class Neighbor : RuleTile.TilingRule.Neighbor {
        new public const int This = 1;
        new public const int NotThis = 2;
        // public const int Three = 3;
    }

    // public void CacheRotationManager() {
    //     rotationManager = GameObject.FindObjectOfType<RotationManager>();
    // }

    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject instantiatedGameObject) {
        // CacheRotationManager();
        return base.StartUp(position, tilemap, instantiatedGameObject);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.color = color;
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform) {
        bool result = base.RuleMatches(rule, position, tilemap, ref transform);
        bool mirror = canMirrorX && (mirrorX ^ ViewOrientation.IsLeftOrRight());
        transform = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0f, 0f, (int)ViewOrientation),
                mirror ? new Vector3(-1f, 2f, 1f) : new Vector3(1f, 2f, 1f)
            ) * transform;
        return result;
    }

    public override Vector3Int GetOffsetPosition(Vector3Int position, Vector3Int offset) {
        Vector3Int rotatedOffset = GetRotatedPosition(offset, 360-(int)ViewOrientation);
        return base.GetOffsetPosition(position, rotatedOffset);
    }

    public Vector3Int GetOffsetPositionNoRotation(Vector3Int position, Vector3Int offset) {
        return base.GetOffsetPosition(position, offset);
    }

    public override Vector3Int GetOffsetPositionReverse(Vector3Int position, Vector3Int offset) {
        Vector3Int rotatedOffset = GetRotatedPosition(offset, (int)ViewOrientation);
        return base.GetOffsetPositionReverse(position, rotatedOffset);
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.This: return Check_This(tile);
            case Neighbor.NotThis: return Check_NotThis(tile);
            // case Neighbor.Three: return Check_Three(tile);
        }
        return base.RuleMatch(neighbor, tile);
    }

    public virtual bool Check_This(TileBase tile) {
        return tile == this || tilesToConnect.Contains(tile);
    }

    bool Check_NotThis(TileBase tile) {
        return !Check_This(tile);
    }

    // bool Check_Three(TileBase tile) {
    //     return tile == tileThree;
    // }
}