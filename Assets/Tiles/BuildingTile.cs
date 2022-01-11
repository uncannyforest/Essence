using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;


[CreateAssetMenu(menuName = "Building Tile")]
public class BuildingTile : CustomRuleTile {
    public TileBase xWallTile;
    public TileBase yWallTile;

    private Tilemap xWallTiles;
    private Tilemap yWallTiles;

    private Tilemap XWallTiles {
        get {
            if (xWallTiles == null) xWallTiles = GameObject.Find("X Walls").GetComponent<Tilemap>();
            return xWallTiles;
        }
    }
    private Tilemap YWallTiles {
        get {
            if (yWallTiles == null) yWallTiles = GameObject.Find("Y Walls").GetComponent<Tilemap>();
            return yWallTiles;
        }
    }

    // public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject instantiatedGameObject) {
    //     return base.StartUp(position, tilemap, instantiatedGameObject);
    // }

    // ignores rotation and reflection
    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform) {
        bool result = RuleMatchesSimple(rule, position, tilemap);
        transform = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0f, 0f, (int)ViewOrientation),
                new Vector3(1f, 2f, 1f)
            );
        return result;
    }

    public bool RuleMatchesSimple(TilingRule rule, Vector3Int position, ITilemap tilemap) {
        var minCount = Math.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
        for (int i = 0; i < minCount ; i++) {
            int neighbor = rule.m_Neighbors[i];
            Vector3Int positionOffset = rule.m_NeighborPositions[i];
            if (positionOffset.sqrMagnitude > 1) continue; // ignore diagonals
            Vector3Int actualOffset = (positionOffset + GetRotatedPosition(new Vector3Int(1, 1, 0), (int)ViewOrientation)) / 2;
            Tilemap whichTilemap = (positionOffset.x != 0 ^ ViewOrientation.IsLeftOrRight()) ? YWallTiles : XWallTiles;
            TileBase other = whichTilemap.GetTile(GetOffsetPosition(position, actualOffset));
            if (!RuleMatch(neighbor, other)) {
                return false;
            }
        }
        return true;
    }

    public override bool Check_This(TileBase tile) {
        return tile != xWallTile && tile != yWallTile;
    }

}