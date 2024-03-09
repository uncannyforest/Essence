using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class MapRenderer3D : MonoBehaviour {
    public GlobalConfig.Elevation elevation;
    public TileLibrary3D tiles;

    private Terrain terrain;
    private Transform mainTiles;
    private Transform xWallTiles;
    private Transform yWallTiles;

    public Transform WorldParent { get => transform; }

    public void OnTerrainStart() {
        terrain = GetComponent<Terrain>();
    }

    public Vector2 CellCenter(Terrain.Position position) {
        switch (position.grid) {
            case Terrain.Grid.XWalls:
                return new Vector2(position.x, position.y + .5f);
            case Terrain.Grid.YWalls:
                return new Vector2(position.x + .5f, position.y);
            default:
                return CellCenter(position.Coord);
        }
    }
    public Vector2 CellCenter(Vector2Int cellPosition) =>
        new Vector2(cellPosition.x + .5f, cellPosition.y + .5f);
    public Vector2Int CellAt(Vector3 worldPosition) =>
        new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
    public Vector2 CellCenterAt(Vector3 screenPosition) => CellCenter(CellAt(screenPosition));
    private Vector2 PositionInCell(Vector2 position) /* -1 <= x, y <= 1 */ => 2 * (position - CellCenterAt(position));

}
