using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Terrain))]
public class MapRenderer2D : MonoBehaviour {

    public static readonly int RoofLevel = 6;
    public GlobalConfig.Elevation elevation;
    public TileLibrary tiles;

    private Terrain terrain;
    private UnityEngine.Grid gameGrid;
    private Tilemap[] mainTiles;
    private Tilemap xWallTiles;
    private Tilemap yWallTiles;
    private Tilemap transparentTiles;
    private Dictionary<Land, TileBase[]> landTiles;
    private Dictionary<Construction, TileBase> buildingTiles;

    public Transform WorldParent { get => gameGrid.transform; }

    public void OnTerrainStart() {
        terrain = GetComponent<Terrain>();
        gameGrid = GetComponent<UnityEngine.Grid>();
        mainTiles = transform.Find("Main Tiles").transform.Cast<Transform>()
            .Select(c => c.GetComponent<Tilemap>()).ToArray();
        xWallTiles = transform.Find("X Walls").GetComponent<Tilemap>();
        yWallTiles = transform.Find("Y Walls").GetComponent<Tilemap>();
        transparentTiles = transform.Find("Transparent").GetComponent<Tilemap>();

        landTiles = new Dictionary<Land, TileBase[]>() {
            [global::Land.Grass] = new TileBase[] { tiles.grass, null, null, null, null, null},
            [global::Land.Meadow] = new TileBase[] { tiles.grass, null, tiles.meadow, null, null, null},
            [global::Land.Shrub] = new TileBase[] { tiles.grass, null, null, tiles.shrub, null, null},
            [global::Land.Forest] = new TileBase[] { tiles.grass, null, null, null, tiles.forest, null},
            [global::Land.Water] = new TileBase[] { tiles.ditch, tiles.water, null, null, null, null},
            [global::Land.Ditch] = new TileBase[] { tiles.ditch, null, null, null, null, null},
            [global::Land.Dirtpile] = new TileBase[] { tiles.grass, null, null, null, null, tiles.dirtpile},
            [global::Land.Woodpile] = new TileBase[] { tiles.grass, tiles.woodpile, null, null, null, null},
            [global::Land.Hill] = new TileBase[] { tiles.grass, null, null, null, null, tiles.hill}
        };

        buildingTiles = new Dictionary<Construction, TileBase> {
            [Construction.None] = null,
            [Construction.Wood] = tiles.woodBldg
        };

        AddDepths();
    }

    // Convert from Terrain Grid Position to Vector2 of cell on the tilemap
    public Vector2 CellCenter(Terrain.Position position) {
        switch (position.grid) {
            case Terrain.Grid.XWalls:
                return xWallTiles.GetCellCenterWorld((Vector3Int) position.Coord);
            case Terrain.Grid.YWalls:
                return yWallTiles.GetCellCenterWorld((Vector3Int) position.Coord);
            default:
                return CellCenter(position.Coord);
        }
    }
    public Vector2 CellCenter(Vector2Int cellPosition) => gameGrid.GetCellCenterWorld((Vector3Int) cellPosition);
    public Vector2Int CellAt(Vector3 worldPosition) => (Vector2Int)gameGrid.WorldToCell(worldPosition);
    public Vector2 CellCenterAt(Vector3 screenPosition) => CellCenter(CellAt(screenPosition));
    private Vector2 PositionInCell(Vector2 position) /* -1 <= x + y <= 1 */ => 2 * (position - CellCenterAt(position));

    public void SetLand(Vector2Int pos, Land terrain) {
        for (int i = 0; i < RoofLevel; i++) {
            mainTiles[i].SetTile((Vector3Int)pos, landTiles[terrain][i]);
        }
    }
    public void SetXWall(int x, int y, Construction construction, bool force = false) {
        switch (construction) {
            case Construction.None:
                xWallTiles.SetTile(new Vector3Int(x, y, 0), null);
            break;
            case Construction.Wood:
                xWallTiles.SetTile(new Vector3Int(x, y, 0), tiles.xFence);
            break;
        }
    }
    public void SetYWall(int x, int y, Construction construction) {
        switch (construction) {
            case Construction.None:
                yWallTiles.SetTile(new Vector3Int(x, y, 0), null);
            break;
            case Construction.Wood:
                yWallTiles.SetTile(new Vector3Int(x, y, 0), tiles.yFence);
            break;
        }
    }
    public void SetRoof(Vector2Int pos, Construction construction, bool force = false) {
        mainTiles[RoofLevel].SetTile((Vector3Int)pos, buildingTiles[construction]);
    }

    // Adds depth tiles for aesthetic purposes.
    public void AddDepths() {
        for (int x = -16; x < terrain.Bounds.x + 16; ) {
            for (int y = -16; y < terrain.Bounds.y + 16; y++) mainTiles[1].SetTile(new Vector3Int(x, y, 0), tiles.deepWater);
            x++;
            if (x == 0) x = terrain.Bounds.x;
        }
        for (int x = 0; x < terrain.Bounds.x; x++) for (int y = -16; y < terrain.Bounds.y + 16; ) {
            mainTiles[1].SetTile(new Vector3Int(x, y, 0), tiles.deepWater);
            y++;
            if (y == 0) y = terrain.Bounds.y;
        }
    }

    // Move tile between mainTiles and transparentTiles.
    // transparentTiles have a special shader that only renders one out of four pixels.
    // hide indicates whether to make the tile transparent.
    public void HideTile(Vector2Int pos, bool hide) {
        if (hide) {
            mainTiles[RoofLevel].SetTile((Vector3Int)pos, null);
            transparentTiles.SetTile((Vector3Int)pos, buildingTiles[terrain.Roof.Get(pos) ?? Construction.None]);
        } else {
            mainTiles[RoofLevel].SetTile((Vector3Int)pos, buildingTiles[terrain.Roof.Get(pos) ?? Construction.None]);
            transparentTiles.SetTile((Vector3Int)pos, null);
        }
    }

    // Returns a Vector2 indicating nearby shore.
    // Direction of the Vector2 indicates direction of shore.
    // Magnitude of the Vector2 indicates the *closeness* of the shore (closer = greater magnitude).
    public Vector2 ShoreEdgeFactor(Vector3 position, float shorePushNoZone) {
        Vector2 shoreCorrection = Vector2.zero;
        Land?[] nearTiles = terrain.GetFourLandTilesAround(position);
        Vector2 sub = PositionInCell(position);
        if (sub.magnitude < shorePushNoZone) sub = Vector2.zero;
        if ((nearTiles[0] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(0, Mathf.Abs(sub.x) - sub.y);
        if ((nearTiles[1] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(- sub.x - Mathf.Abs(sub.y), 0);
        if ((nearTiles[2] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(- sub.x + Mathf.Abs(sub.y), 0);
        if ((nearTiles[3] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(0, -Mathf.Abs(sub.x) - sub.y);
        return shoreCorrection;
    }

}
