using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GridCell3D : MonoBehaviour {
    public Land land;
    public GridSubCell3D gridSubCellPrefab;
    public Shader unhiddenShader;
    public Shader hiddenShader;

    private Vector2Int pos;
    private BoxCollider2D squareCollider;
    private GridSubCell3D land0;
    private GridSubCell3D land1;
    private GridSubCell3D land2;
    private GridSubCell3D land3;
    private Transform xWall;
    private Transform yWall;
    private GridSubCell3D roof0;
    private GridSubCell3D roof1;
    private GridSubCell3D roof2;
    private GridSubCell3D roof3;

    void Awake() {
        pos = Vector2Int.RoundToInt(transform.position);
        squareCollider = GetComponent<BoxCollider2D>();
        gameObject.name = pos.ToString();
        land0 = GameObject.Instantiate(gridSubCellPrefab, (Vector2)pos, Quaternion.identity, transform);
        land0.name = "0";
        land1 = GameObject.Instantiate(gridSubCellPrefab, pos + Vct.F(1, 0), Quaternion.Euler(0, 0, 90), transform);
        land1.name = "1";
        land2 = GameObject.Instantiate(gridSubCellPrefab, pos + Vct.F(1, 1), Quaternion.Euler(0, 0, 180), transform);
        land2.name = "2";
        land3 = GameObject.Instantiate(gridSubCellPrefab, pos + Vct.F(0, 1), Quaternion.Euler(0, 0, 270), transform);
        land3.name = "3";
        PopulateCell();
    }

    private int x { get => pos.x; }
    private int y { get => pos.y; }
    private Transform tilesParent { get => transform.parent; }

    public void PopulateCell() {
        UpdateLand();
        UpdateXWall();
        UpdateYWall();
        UpdateRoof();
    }

    public void HideRoof(bool hide) {
        if (roof0 == null) return;
        roof0.GetComponentInChildren<MeshRenderer>().material.shader = hide ? hiddenShader : unhiddenShader;
        roof1.GetComponentInChildren<MeshRenderer>().material.shader = hide ? hiddenShader : unhiddenShader;
        roof2.GetComponentInChildren<MeshRenderer>().material.shader = hide ? hiddenShader : unhiddenShader;
        roof3.GetComponentInChildren<MeshRenderer>().material.shader = hide ? hiddenShader : unhiddenShader;
    }

    private Land GetLand(int x, int y) => Terrain.I.GetLand(Vct.I(x, y)) ?? Terrain.I.Depths;
    private Construction GetXWall(int x, int y) => Terrain.I.GetConstruction(new Terrain.Position(Terrain.Grid.XWalls, x, y)) ?? Construction.None;
    private Construction GetYWall(int x, int y) => Terrain.I.GetConstruction(new Terrain.Position(Terrain.Grid.YWalls, x, y)) ?? Construction.None;
    private Construction GetRoof(int x, int y) => Terrain.I.GetConstruction(new Terrain.Position(Terrain.Grid.Roof, x, y)) ?? Construction.None;

    public void UpdateLand() {
        land = GetLand(x, y);
        squareCollider.enabled = !land.IsPassable();
        land0.MaybeRender(
                GetLand(x, y),
                GetLand(x - 1, y),
                GetLand(x - 1, y - 1),
                GetLand(x, y - 1),
                GetLand(x + 1, y),
                GetLand(x, y + 1));
        land1.MaybeRender(
                GetLand(x, y),
                GetLand(x, y - 1),
                GetLand(x + 1, y - 1),
                GetLand(x + 1, y),
                GetLand(x, y + 1),
                GetLand(x - 1, y));
        land2.MaybeRender(
                GetLand(x, y),
                GetLand(x + 1, y),
                GetLand(x + 1, y + 1),
                GetLand(x, y + 1),
                GetLand(x - 1, y),
                GetLand(x, y - 1));
        land3.MaybeRender(
                GetLand(x, y),
                GetLand(x, y + 1),
                GetLand(x - 1, y + 1),
                GetLand(x - 1, y),
                GetLand(x, y - 1),
                GetLand(x + 1, y));
    }
    public void UpdateXWall() {
        if (xWall != null) GameObject.Destroy(xWall.gameObject);
        if (GetXWall(x, y) == Construction.None) return;
        xWall = new GameObject("X").transform;
        xWall.parent = transform;
        xWall.position = new Vector2(x, y);
        TileLibrary3D.E.temperate.woodWall.Render(false)(xWall);
    }
    public void UpdateYWall() {
        if (yWall != null) GameObject.Destroy(yWall.gameObject);
        if (GetYWall(x, y) == Construction.None) return;
        yWall = new GameObject("Y").transform;
        yWall.parent = transform;
        yWall.position = new Vector2(x, y);
        TileLibrary3D.E.temperate.woodWall.Render(true)(yWall);
    }
    public void UpdateRoof() {
        if (GetRoof(x, y) == Construction.None) {
            if (roof0 != null) {
                GameObject.Destroy(roof0.gameObject);
                GameObject.Destroy(roof1.gameObject);
                GameObject.Destroy(roof2.gameObject);
                GameObject.Destroy(roof3.gameObject);
            }
            return;
        }
        if (roof0 == null) {
            roof0 = GameObject.Instantiate(gridSubCellPrefab, new Vector2(x, y), Quaternion.identity, transform);
            roof0.name = "R0";
            roof1 = GameObject.Instantiate(gridSubCellPrefab, new Vector2(x + 1, y), Quaternion.Euler(0, 0, 90), transform);
            roof1.name = "R1";
            roof2 = GameObject.Instantiate(gridSubCellPrefab, new Vector2(x + 1, y + 1), Quaternion.Euler(0, 0, 180), transform);
            roof2.name = "R2";
            roof3 = GameObject.Instantiate(gridSubCellPrefab, new Vector2(x, y + 1), Quaternion.Euler(0, 0, 270), transform);
            roof3.name = "R3";
        }
        // Since adjacency works the other way:
        // Here left and right walls (and oppRight and oppLeft) have been swapped.
        // Corner vs Inner are swapped in the Biome scriptable object .asset file itself.
        roof0.MaybeRender(
                GetRoof(x, y),
                GetXWall(x, y),
                Construction.None,
                GetYWall(x, y),
                GetXWall(x, y + 1),
                GetYWall(x + 1, y));
        roof1.MaybeRender(
                GetRoof(x, y),
                GetYWall(x + 1, y),
                Construction.None,
                GetXWall(x, y),
                GetYWall(x, y),
                GetXWall(x, y + 1));
        roof2.MaybeRender(
                GetRoof(x, y),
                GetXWall(x, y + 1),
                Construction.None,
                GetYWall(x + 1, y),
                GetXWall(x, y),
                GetYWall(x, y));
        roof3.MaybeRender(
                GetRoof(x, y),
                GetYWall(x, y),
                Construction.None,
                GetXWall(x, y + 1),
                GetYWall(x + 1, y),
                GetXWall(x, y));
    }
}
