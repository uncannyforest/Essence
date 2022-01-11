using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(menuName = "Wall Tile")]
public class WallTile : SimpleTile {
    public bool isX;
    public Sprite xWallTile;
    public Sprite yWallTile;
    public Tilemap roofs;

    public Tilemap Roofs {
        get {
            if (roofs == null) {
                    Transform mainTiles = GameObject.Find("Main Tiles").transform;
                    Debug.Log(mainTiles + " " + mainTiles.GetChild(mainTiles.childCount - 1) + " " + mainTiles.GetChild(mainTiles.childCount - 1).GetComponent<Tilemap>());
                    roofs = mainTiles.GetChild(mainTiles.childCount - 1).GetComponent<Tilemap>();
            }
            return roofs;
        }
    }

    // public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject instantiatedGameObject) {
    //     return base.StartUp(position, tilemap, instantiatedGameObject);
    // }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.sprite = (ViewOrientation.IsLeftOrRight() ^ isX) ? xWallTile : yWallTile;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap) {
        base.RefreshTile(position, tilemap);

        List<Vector3Int> offsets = isX ?
            new List<Vector3Int> {new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0)} :
            new List<Vector3Int> {new Vector3Int(0, 0, 0), new Vector3Int(-1, 0, 0)};
        
        foreach (Vector3Int offset in offsets) {
            TileBase tile = Roofs.GetTile(position + offset);
            RuleTile ruleTile = null;

            if (tile is RuleTile)
                ruleTile = tile as RuleTile;
            else if (tile is RuleOverrideTile)
                ruleTile = (tile as RuleOverrideTile).m_Tile;

            if (ruleTile != null) Roofs.RefreshTile(position + offset);
        }
    }
}