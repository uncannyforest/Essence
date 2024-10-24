using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class MapRenderer3D : MonoBehaviour {
    public GlobalConfig.Elevation elevation;
    public TileLibrary3D tiles;
    public GridSubCell3D gridSubCell;
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
        for (int i = -1; i <= 0; i++) {
            int localY = y + i;
            if (!IsInRenderWindow(new Vector2Int(x, localY))) continue;
            UpdateRoofCell(x, localY, 0);
            UpdateRoofCell(x, localY, 1);
            UpdateRoofCell(x, localY, 2);
            UpdateRoofCell(x, localY, 3);
        }
    }
    public void UpdateYWall(int x, int y) {
        if (!TerrainIsLoaded) return;
        if (IsInRenderWindow(new Vector2Int(x, y))) {
            UpdateYWallCell(x, y);
        }
        for (int i = -1; i <= 0; i++) {
            int localX = x + i;
            if (!IsInRenderWindow(new Vector2Int(localX, y))) continue;
            UpdateRoofCell(localX, y, 0);
            UpdateRoofCell(localX, y, 1);
            UpdateRoofCell(localX, y, 2);
            UpdateRoofCell(localX, y, 3);
        }
    }
    public void UpdateRoof(Vector2Int pos) {
        if (!TerrainIsLoaded) return;
        if (IsInRenderWindow(pos)) {
            UpdateRoofCell(pos.x, pos.y, 0);
            UpdateRoofCell(pos.x, pos.y, 1);
            UpdateRoofCell(pos.x, pos.y, 2);
            UpdateRoofCell(pos.x, pos.y, 3);
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
                GameObject child = new GameObject(pos.ToString());
                child.transform.parent = tilesParent;
                GameObject.Instantiate(gridSubCell, (Vector2)pos, Quaternion.identity,
                    child.transform).name = "0";
                GameObject.Instantiate(gridSubCell, pos + Vct.F(1, 0), Quaternion.Euler(0, 0, 90),
                    child.transform).name = "1";
                GameObject.Instantiate(gridSubCell, pos + Vct.F(1, 1), Quaternion.Euler(0, 0, 180),
                    child.transform).name = "2";
                GameObject.Instantiate(gridSubCell, pos + Vct.F(0, 1), Quaternion.Euler(0, 0, 270),
                    child.transform).name = "3";
                PopulateCell(x, y);
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
    public void PopulateCell(int x, int y) {
        Debug.Log(x + ", " + y + ": " + Terrain.I.GetLand(Vct.I(x, y)));
        UpdateLandCell(x, y, 0);
        UpdateLandCell(x, y, 1);
        UpdateLandCell(x, y, 2);
        UpdateLandCell(x, y, 3);
        UpdateRoofCell(x, y, 0);
        UpdateRoofCell(x, y, 1);
        UpdateRoofCell(x, y, 2);
        UpdateRoofCell(x, y, 3);
    }
    public Land GetLand(int x, int y) => Terrain.I.GetLand(Vct.I(x, y)) ?? Terrain.I.Depths;
    public void UpdateLandCell(int x, int y, int rot) {
        Transform child = tilesParent.Find(new Vector2Int(x, y).ToString());
        switch (rot) {
            case 0: child.Find("0").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x - 1, y),
                    GetLand(x - 1, y - 1),
                    GetLand(x, y - 1),
                    GetLand(x + 1, y),
                    GetLand(x, y + 1));
                break;
            case 1: child.Find("1").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x, y - 1),
                    GetLand(x + 1, y - 1),
                    GetLand(x + 1, y),
                    GetLand(x, y + 1),
                    GetLand(x - 1, y));
                break;
            case 2: child.Find("2").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x + 1, y),
                    GetLand(x + 1, y + 1),
                    GetLand(x, y + 1),
                    GetLand(x - 1, y),
                    GetLand(x, y - 1));
                break;
            case 3: child.Find("3").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetLand(x, y),
                    GetLand(x, y + 1),
                    GetLand(x - 1, y + 1),
                    GetLand(x - 1, y),
                    GetLand(x, y - 1),
                    GetLand(x + 1, y));
                break;
        }
    }
    public Construction GetXWall(int x, int y) => Terrain.I.GetConstruction(new Terrain.Position(Terrain.Grid.XWalls, x, y)) ?? Construction.None;
    public void UpdateXWallCell(int x, int y) {
        Transform child = tilesParent.Find(new Vector2Int(x, y).ToString());
        Transform xWall = child.Find("X");
        if (xWall != null) GameObject.Destroy(xWall.gameObject);
        if (GetXWall(x, y) == Construction.None) return;
        xWall = new GameObject("X").transform;
        xWall.parent = child;
        xWall.position = new Vector2(x, y);
        TileLibrary3D.E.temperate.woodWall.Render(false)(xWall);
    }
    public Construction GetYWall(int x, int y) => Terrain.I.GetConstruction(new Terrain.Position(Terrain.Grid.YWalls, x, y)) ?? Construction.None;
    public void UpdateYWallCell(int x, int y) {
        Transform child = tilesParent.Find(new Vector2Int(x, y).ToString());
        Transform yWall = child.Find("Y");
        if (yWall != null) GameObject.Destroy(yWall.gameObject);
        if (GetYWall(x, y) == Construction.None) return;
        yWall = new GameObject("Y").transform;
        yWall.parent = child;
        yWall.position = new Vector2(x, y);
        TileLibrary3D.E.temperate.woodWall.Render(true)(yWall);
    }
    public Construction GetRoof(int x, int y) => Terrain.I.GetConstruction(new Terrain.Position(Terrain.Grid.Roof, x, y)) ?? Construction.None;
    public void UpdateRoofCell(int x, int y, int rot) {
        Transform child = tilesParent.Find(new Vector2Int(x, y).ToString());
        if (GetRoof(x, y) == Construction.None) {
            if (child.Find("R0") != null) {
                GameObject.Destroy(child.Find("R0").gameObject);
                GameObject.Destroy(child.Find("R1").gameObject);
                GameObject.Destroy(child.Find("R2").gameObject);
                GameObject.Destroy(child.Find("R3").gameObject);
            }
            return;
        }
        if (child.Find("R0") == null) {
            GameObject.Instantiate(gridSubCell, new Vector2(x, y), Quaternion.identity,
                child.transform).name = "R0";
            GameObject.Instantiate(gridSubCell, new Vector2(x + 1, y), Quaternion.Euler(0, 0, 90),
                child.transform).name = "R1";
            GameObject.Instantiate(gridSubCell, new Vector2(x + 1, y + 1), Quaternion.Euler(0, 0, 180),
                child.transform).name = "R2";
            GameObject.Instantiate(gridSubCell, new Vector2(x, y + 1), Quaternion.Euler(0, 0, 270),
                child.transform).name = "R3";
        }
        switch (rot) {
            // Since adjacency works the other way:
            // Here left and right walls (and oppRight and oppLeft) have been swapped.
            // Corner vs Inner are swapped in the Biome scriptable object .asset file itself.
            case 0: child.Find("R0").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetRoof(x, y),
                    GetXWall(x, y),
                    Construction.None,
                    GetYWall(x, y),
                    GetXWall(x, y + 1),
                    GetYWall(x + 1, y));
                break;
            case 1: child.Find("R1").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetRoof(x, y),
                    GetYWall(x + 1, y),
                    Construction.None,
                    GetXWall(x, y),
                    GetYWall(x, y),
                    GetXWall(x, y + 1));
                break;
            case 2: child.Find("R2").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetRoof(x, y),
                    GetXWall(x, y + 1),
                    Construction.None,
                    GetYWall(x + 1, y),
                    GetXWall(x, y),
                    GetYWall(x, y));
                break;
            case 3: child.Find("R3").GetComponentStrict<GridSubCell3D>().MaybeRender(
                    GetRoof(x, y),
                    GetYWall(x, y),
                    Construction.None,
                    GetXWall(x, y + 1),
                    GetYWall(x + 1, y),
                    GetXWall(x, y));
                break;
        }
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
