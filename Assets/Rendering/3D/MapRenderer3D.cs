using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class MapRenderer3D : MonoBehaviour {
    public GlobalConfig.Elevation elevation;
    public TileLibrary3D tiles;
    public GridCell3D gridCell;
    public int renderWindow;

    private Terrain terrain;
    private Transform mainTiles;
    private Transform xWallTiles;
    private Transform yWallTiles;

    private Transform tilesParent;

    public Transform WorldParent { get => transform; }

    public void OnTerrainLoaded() {
        terrain = GetComponent<Terrain>();
        GameManager.I.YourPlayer.movement.CrossingTile += HandleCrossingTile;
    }

    private bool TerrainIsLoaded { get => terrain != null; }

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
    public static Vector2 ToWorld(Vector2Int vector) => new Vector2(vector.x, vector.y);
    public Vector2Int CellAt(Vector3 worldPosition) =>
        new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
    public Vector2 CellCenterAt(Vector3 screenPosition) => CellCenter(CellAt(screenPosition));
    private Vector2 PositionInCell(Vector2 position) /* -1 <= x, y <= 1 */ => 2 * (position - CellCenterAt(position));

    private bool IsInRenderWindow(Transform tile) => IsInRenderWindow(((Vector2)tile.GetChild(0).position).RoundToInt());
    public bool IsInRenderWindow(Vector2Int pos) {
        Vector2Int playerPos = CellAt(GameManager.I.YourPlayer.transform.position);
        return pos.x >= playerPos.x - renderWindow / 2
            && pos.x <= playerPos.x + renderWindow / 2
            && pos.y >= playerPos.y - renderWindow / 2
            && pos.y <= playerPos.y + renderWindow / 2;
    }
    public bool CellRendered(Vector2Int pos, out GridCell3D cell) {
        cell = null;
        if (!IsInRenderWindow(pos)) {
            return false;
        }
        Transform cellTransform = tilesParent.Find(pos.ToString());
        if (cellTransform == null) {
            Debug.LogWarning(pos + " in render window (playerPos "
                + CellAt(GameManager.I.YourPlayer.transform.position) + ") but not rendered");
            return false;
        }
        cell = cellTransform.GetComponentStrict<GridCell3D>();
        return true;
    }
    public void UpdateLand(Vector2Int pos) {
        if (!TerrainIsLoaded) return;
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) {
            Vector2Int localPos = pos + new Vector2Int(x, y);
            if (CellRendered(localPos, out GridCell3D cell)) {
                cell.UpdateLand();
            }
        }
    }
    public void UpdateXWall(int x, int y) {
        if (!TerrainIsLoaded) return;
        if (CellRendered(new Vector2Int(x, y), out GridCell3D cell)) {
            cell.UpdateXWall();
        }
        for (int i = -1; i <= 0; i++) {
            int localY = y + i;
            if (CellRendered(new Vector2Int(x, localY), out GridCell3D roofCell)) {
                roofCell.UpdateRoof();
            }
        }
    }
    public void UpdateYWall(int x, int y) {
        if (!TerrainIsLoaded) return;
        if (CellRendered(new Vector2Int(x, y), out GridCell3D cell)) {
            cell.UpdateYWall();
        }
        for (int i = -1; i <= 0; i++) {
            int localX = x + i;
            if (CellRendered(new Vector2Int(localX, y), out GridCell3D roofCell)) {
                roofCell.UpdateRoof();
            }
        }
    }
    public void UpdateRoof(Vector2Int pos) {
        if (!TerrainIsLoaded) return;
        if (CellRendered(pos, out GridCell3D cell)) {
            cell.UpdateRoof();
        }
    }
    public void Reset() {
        if (tilesParent != null) GameObject.Destroy(tilesParent.gameObject);
        tilesParent = new GameObject("Tiles").transform;
        tilesParent.parent = WorldParent;
        CreateAllCellsInRenderWindow();
    }
    public void CreateAllCellsInRenderWindow(bool force = true) {
        Vector2Int playerPos = CellAt(GameManager.I.YourPlayer.transform.position);
        for (int x = playerPos.x - renderWindow / 2; x <= playerPos.x + renderWindow / 2; x++) {
            for (int y = playerPos.y - renderWindow / 2; y <= playerPos.y + renderWindow / 2; y++) {
                Vector2Int pos = new Vector2Int(x, y); // position: corner NOT cell center
                if (!force && tilesParent.Find(pos.ToString()) != null) continue;
                GameObject.Instantiate(gridCell, (Vector2)pos, Quaternion.identity, tilesParent);
            }
        }
    }
    public bool HandleCrossingTile(Vector2Int _) { UpdateRenderWindow(); return true; }
    public void UpdateRenderWindow() {
        foreach (Transform child in tilesParent)
            if (!IsInRenderWindow(child))
                GameObject.Destroy(child.gameObject);
        CreateAllCellsInRenderWindow(false);
    }

    public void HideTile(Vector2Int pos, bool hide) {
        if (CellRendered(pos, out GridCell3D cell)) cell.HideRoof(hide);
    }

    public Land?[] GetFourLandTilesAround(Vector2 pos) {
        Vector2Int firstCell = CellAt(pos + Vct.F(-.5f, -.5f));
        return new Land?[] {
            terrain.GetLand(firstCell),
            terrain.GetLand(firstCell + Vct.I(1, 0)),
            terrain.GetLand(firstCell + Vct.I(0, 1)),
            terrain.GetLand(firstCell + Vct.I(1, 1))
        };
    }

    // Returns a Vector2 indicating nearby shore.
    // Direction of the Vector2 is the negative of proximity to shore.
    // Magnitude of the Vector2 indicates the *closeness* of the shore (closer = greater magnitude).
    //
    // When position is near a land corner, the output always points the direction the corner points.
    //
    // This algorithm doesn't treat every point along a flat shore equally,
    // but it does the job.
    public Vector2 ShoreSlope(Vector2 position, float shorePushNoZone) {
        Vector2 shoreCorrection = Vector2.zero;
        Land?[] nearTiles = GetFourLandTilesAround(position);
        Vector2 sub = PositionInCell(position + Vct.F(-.5f, -.5f));
        if ((1 - Mathf.Abs(sub.x)) + (1 - Mathf.Abs(sub.y)) < shorePushNoZone) return Vector2.zero; // if sub is near a corner
        if ((nearTiles[0] ?? terrain.Depths) != Land.Water) shoreCorrection += Vct.F(1, 1) * (1 - Mathf.Max(sub.x, sub.y));
        if ((nearTiles[1] ?? terrain.Depths) != Land.Water) shoreCorrection += Vct.F(-1, 1) * (1 - Mathf.Max(-sub.x, sub.y));
        if ((nearTiles[2] ?? terrain.Depths) != Land.Water) shoreCorrection += Vct.F(1, -1) * (1 - Mathf.Max(sub.x, -sub.y));
        if ((nearTiles[3] ?? terrain.Depths) != Land.Water) shoreCorrection += Vct.F(-1, -1) * (1 - Mathf.Max(-sub.x, -sub.y));
        return shoreCorrection;
    }
}
