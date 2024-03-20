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

    public Transform WorldParent { get => transform; }

    public void OnTerrainLoaded() {
        terrain = GetComponent<Terrain>();
        CreateAllCellsInRenderWindow();
        PopulateAllCellsInRenderWindow();
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
    public Vector2Int CellAt(Vector3 worldPosition) =>
        new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
    public Vector2 CellCenterAt(Vector3 screenPosition) => CellCenter(CellAt(screenPosition));
    private Vector2 PositionInCell(Vector2 position) /* -1 <= x, y <= 1 */ => 2 * (position - CellCenterAt(position));

    public bool IsInRenderWindow(Vector2Int pos) {
        Vector2Int playerPos = CellAt(GameManager.I.YourPlayer.transform.position);
        return pos.x >= playerPos.x - renderWindow / 2
            && pos.x <= playerPos.x + renderWindow / 2
            && pos.y >= playerPos.y - renderWindow / 2
            && pos.y <= playerPos.y + renderWindow / 2;
    }
    public void UpdateLand(Vector2Int pos) {
        if (!TerrainIsLoaded) return;
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) {
            Vector2Int localPos = pos + new Vector2Int(x, y);
            if (IsInRenderWindow(localPos)) {
                UpdateLandCell(localPos.x, localPos.y, 0);
                UpdateLandCell(localPos.x, localPos.y, 1);
                UpdateLandCell(localPos.x, localPos.y, 2);
                UpdateLandCell(localPos.x, localPos.y, 3);
            }
        }
    }
    public void UpdateXWall(int x, int y) {
        if (!TerrainIsLoaded) return;
        if (IsInRenderWindow(new Vector2Int(x, y))) {
            UpdateXWallCell(x, y);
        }
    }
    public void UpdateYWall(int x, int y) {
        if (!TerrainIsLoaded) return;
        if (IsInRenderWindow(new Vector2Int(x, y))) {
            UpdateYWallCell(x, y);
        }
    }
    public void UpdateRoof(Vector2Int pos) {
        if (!TerrainIsLoaded) return;
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) {
            Vector2Int localPos = pos + new Vector2Int(x, y);
            if (IsInRenderWindow(localPos)) {
                // UpdateRoofCell(localPos.x, localPos.y, 0);
                // UpdateRoofCell(localPos.x, localPos.y, 1);
                // UpdateRoofCell(localPos.x, localPos.y, 2);
                // UpdateRoofCell(localPos.x, localPos.y, 3);
            }
        }
    }
    public void CreateAllCellsInRenderWindow() {
        Vector2Int playerPos = CellAt(GameManager.I.YourPlayer.transform.position);
        for (int x = playerPos.x - renderWindow / 2; x <= playerPos.x + renderWindow / 2; x++) {
            for (int y = playerPos.y - renderWindow / 2; y <= playerPos.y + renderWindow / 2; y++) {
                Vector2Int pos = new Vector2Int(x, y); // position: corner NOT cell center
                GameObject child = new GameObject(pos.ToString());
                child.transform.parent = transform;
                GameObject.Instantiate(gridCell, (Vector2)pos, Quaternion.identity,
                    child.transform).name = "0";
                GameObject.Instantiate(gridCell, pos + Vct.F(1, 0), Quaternion.Euler(0, 0, 90),
                    child.transform).name = "1";
                GameObject.Instantiate(gridCell, pos +  Vct.F(1, 1), Quaternion.Euler(0, 0, 180),
                    child.transform).name = "2";
                GameObject.Instantiate(gridCell, pos + Vct.F(0, 1), Quaternion.Euler(0, 0, 270),
                    child.transform).name = "3";
            }
        }
    }
    public void PopulateAllCellsInRenderWindow() {
        Vector2Int playerPos = CellAt(GameManager.I.YourPlayer.transform.position);
        for (int x = playerPos.x - renderWindow / 2; x <= playerPos.x + renderWindow / 2; x++) {
            for (int y = playerPos.y - renderWindow / 2; y <= playerPos.y + renderWindow / 2; y++) {
                Debug.Log(x + ", " + y + ": " + Terrain.I.GetLand(Vct.I(x, y)));
                UpdateLandCell(x, y, 0);
                UpdateLandCell(x, y, 1);
                UpdateLandCell(x, y, 2);
                UpdateLandCell(x, y, 3);
            }
        }
    }
    public Land GetLand(int x, int y) => Terrain.I.GetLand(Vct.I(x, y)) ?? Terrain.I.Depths;
    public void UpdateLandCell(int x, int y, int rot) {
        Transform child = transform.Find(new Vector2Int(x, y).ToString());
        switch (rot) {
            case 0: child.Find("0").GetComponentStrict<GridCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x - 1, y),
                    GetLand(x - 1, y - 1),
                    GetLand(x, y - 1),
                    GetLand(x + 1, y),
                    GetLand(x, y + 1));
                break;
            case 1: child.Find("1").GetComponentStrict<GridCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x, y - 1),
                    GetLand(x + 1, y - 1),
                    GetLand(x + 1, y),
                    GetLand(x, y + 1),
                    GetLand(x - 1, y));
                break;
            case 2: child.Find("2").GetComponentStrict<GridCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x + 1, y),
                    GetLand(x + 1, y + 1),
                    GetLand(x, y + 1),
                    GetLand(x - 1, y),
                    GetLand(x, y - 1));
                break;
            case 3: child.Find("3").GetComponentStrict<GridCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x, y + 1),
                    GetLand(x - 1, y + 1),
                    GetLand(x - 1, y),
                    GetLand(x, y - 1),
                    GetLand(x + 1, y));
                break;
        }
    }
    public void UpdateXWallCell(int x, int y) {
        
    }
    public void UpdateYWallCell(int x, int y) {
        
    }
    public void UpdateRoofCell(Vector2Int pos, int rot) {
        
    }

    public void HideTile(Vector2Int pos, bool hide) {

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
    // Direction of the Vector2 indicates direction of shore.
    // Magnitude of the Vector2 indicates the *closeness* of the shore (closer = greater magnitude).
    public Vector2 ShoreEdgeFactor(Vector3 position, float shorePushNoZone) {
        Vector2 shoreCorrection = Vector2.zero;
        Land?[] nearTiles = GetFourLandTilesAround(position);
        Vector2 sub = PositionInCell(position);
        if (sub.magnitude < shorePushNoZone) sub = Vector2.zero;
        if ((nearTiles[0] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(0, Mathf.Abs(sub.x) - sub.y);
        if ((nearTiles[1] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(- sub.x - Mathf.Abs(sub.y), 0);
        if ((nearTiles[2] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(- sub.x + Mathf.Abs(sub.y), 0);
        if ((nearTiles[3] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(0, -Mathf.Abs(sub.x) - sub.y);
        return shoreCorrection;
    }
}
