using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Linq;

public class Terrain : MonoBehaviour {
    public enum Grid {
        YWalls,
        XWalls,
        Roof
    }
    public enum Biome {
        Temperate
    }

    public class LandIndex {
        Terrain terrain;
        public LandIndex(Terrain terrain) => this.terrain = terrain;
        public Land this[Vector2Int key] {
            get => terrain.land[key.x, key.y];
            set {
                if (terrain.SetLand(key, value)) {} else
                    throw new InvalidOperationException("Cannot set land at (" + key.x + ", " + key.y + ") to " + value);
            }
        }
        public Land this[int x, int y] {
            get => terrain.land[x, y];
            set => this[Vct.I(x, y)] = value;
        }
    }
    public class ConstructionIndex {
        Terrain terrain;
        Grid grid;
        public ConstructionIndex(Terrain terrain, Grid grid) {
            this.terrain = terrain;
            this.grid = grid;
        }
        public Construction this[Vector2Int key] {
            get => terrain.GetConstructionStrict(new Position(grid, key));
            set => terrain.SetConstruction(new Position(grid, key), value);
        }
        public Construction this[int x, int y] {
            get => terrain.GetConstructionStrict(new Position(grid, x, y));
            set => terrain.SetConstruction(new Position(grid, x, y), value);
        }
        public Construction? Get(Vector2Int key) => terrain.GetConstruction(new Position(grid, key));
        public Construction? Get(int x, int y) => terrain.GetConstruction(new Position(grid, x, y));
    }
    public class FeatureIndex {
        Terrain terrain;
        public FeatureIndex(Terrain terrain) => this.terrain = terrain;
        public Feature this[Vector2Int key] {
            get => terrain.features[key.x, key.y];
            set => terrain.PlaceFeature(key, value);
        }
        public Feature this[int x, int y] {
            get => terrain.features[x, y];
            set => terrain.PlaceFeature(Vct.I(x, y), value);
        }
    }

    public static readonly int RoofLevel = 6;

    public TileLibrary tiles;
    public float validationUpdateTime = 0.5f;
    public GameObject collapsePrefab;
    public Fountain spawnPrefab;

    public readonly TerrainValidator validator;
    public readonly Concealment concealment;

    private const int Dim = 128;
    private Land[,] land = new Land[Dim, Dim];
    private Construction[,] xWalls = new Construction[Dim, Dim];
    private Construction[,] yWalls = new Construction[Dim, Dim];
    private Construction[,] roofs = new Construction[Dim, Dim];
    private Feature[,] features = new Feature[Dim, Dim];

    public LandIndex Land;
    public ConstructionIndex XWall;
    public ConstructionIndex YWall;
    public ConstructionIndex Roof;
    public FeatureIndex Feature;

    private UnityEngine.Grid gameGrid;
    private Tilemap[] mainTiles;
    private Tilemap xWallTiles;
    private Tilemap yWallTiles;
    private Dictionary<Land, TileBase[]> landTiles;

    Terrain(): base() {
        validator = new TerrainValidator(this);
        concealment = new Concealment(this);
    }

    void Start() {
        Land = new LandIndex(this);
        XWall = new ConstructionIndex(this, Grid.XWalls);
        YWall = new ConstructionIndex(this, Grid.YWalls);
        Roof = new ConstructionIndex(this, Grid.Roof);
        Feature = new FeatureIndex(this);

        validator.Initialize();

        gameGrid = GetComponent<UnityEngine.Grid>();
        mainTiles = transform.Find("Main Tiles").transform.Cast<Transform>()
            .Select(c => c.GetComponent<Tilemap>()).ToArray();
        xWallTiles = transform.Find("X Walls").GetComponent<Tilemap>();
        yWallTiles = transform.Find("Y Walls").GetComponent<Tilemap>();
        concealment.Initialize(mainTiles);

        landTiles = new Dictionary<Land, TileBase[]>() {
            [global::Land.Grass] = new TileBase[] { tiles.grass, null, null, null, null, null},
            [global::Land.Meadow] = new TileBase[] { tiles.grass, null, tiles.meadow, null, null, null},
            [global::Land.Shrub] = new TileBase[] { tiles.grass, null, null, tiles.shrub, null, null},
            [global::Land.Forest] = new TileBase[] { tiles.grass, null, null, null, tiles.forest, null},
            [global::Land.Water] = new TileBase[] { tiles.ditch, tiles.water, null, null, null, null},
            [global::Land.Ditch] = new TileBase[] { tiles.ditch, null, null, null, null, null},
            [global::Land.Woodpile] = new TileBase[] { tiles.grass, tiles.woodpile, null, null, null, null},
            [global::Land.Hill] = new TileBase[] { tiles.grass, null, null, null, null, tiles.hill}
        };

        TerrainGenerator.GenerateTerrain(this);
        AddDepths();
        Vector2Int startLocation = TerrainGenerator.PlaceFountains(this);
        TerrainGenerator.FinalDecor(this, startLocation);
    }

    private Dictionary<Land, int> voluminousTiles = new Dictionary<Land, int> {
        [global::Land.Meadow] = 2,
        [global::Land.Shrub] = 3,
        [global::Land.Forest] = 4
    };

    void StabilizeNow() => validator.StabilizeNow();

    public Bounds Bounds {
        get => new Bounds(Dim, Dim);
    }
    public Land Depths {
        get => global::Land.Water;
    }
    public bool InBounds(Vector2 coord) => Bounds.Contains(coord.FloorToInt());
    public bool InBounds(Position pos) {
        switch (pos.grid) {
            case Grid.XWalls:
                return (pos.x >= 0 && pos.y >= 0 && pos.x < Bounds.x && pos.y <= Bounds.y);
            case Grid.YWalls:
                return (pos.x >= 0 && pos.y >= 0 && pos.x <= Bounds.x && pos.y < Bounds.y);
            default:
                return (pos.x >= 0 && pos.y >= 0 && pos.x < Bounds.x && pos.y < Bounds.y);
        }
    }
    public Vector2 CellCenter(Position position) {
        switch (position.grid) {
            case Grid.XWalls:
                return xWallTiles.GetCellCenterWorld((Vector3Int) position.Coord);
            case Grid.YWalls:
                return yWallTiles.GetCellCenterWorld((Vector3Int) position.Coord);
            default:
                return CellCenter(position.Coord);
        }
    }
    public Vector2 CellCenter(Vector2Int cellPosition) => gameGrid.GetCellCenterWorld((Vector3Int) cellPosition);
    public Vector2Int CellAt(Vector3 worldPosition) => (Vector2Int)gameGrid.WorldToCell(worldPosition);
    public Vector2 CellCenterAt(Vector3 screenPosition) => CellCenter(CellAt(screenPosition));

    public Land? GetLand(Vector2Int coord) {
        return InBounds(coord) ? (Land?)Land[coord] : null;
    }

    public bool SetLand(Vector2Int pos, Land terrain) {
        if (!validator.IsValidLand(pos, terrain)) return false;
        land[pos.x, pos.y] = terrain;
        for (int i = 0; i < RoofLevel; i++) {
            mainTiles[i].SetTile((Vector3Int)pos, landTiles[terrain][i]);
        }
        validator.StabilizeNext(() => validator.StabilizeLand(pos));
        validator.StabilizeAdjacentLandNext(pos);
        return true;
    }

    public Construction this[Position key] {
        get => GetConstructionStrict(key);
        set => SetConstruction(key, value);
    }

    public bool PlaceFeature(Vector2Int pos, Feature feature) {
        if (!feature.IsValidTerrain(Land[pos]) || !feature.IsValidTerrain(Roof[pos])) return false;
        feature.transform.position = CellCenter(pos).WithZ(GlobalConfig.I.elevation.features);
        feature.transform.rotation = Quaternion.identity;
        features[pos.x, pos.y] = feature;
        return true;
    }
    public Feature BuildFeature(Vector2Int pos, Feature featurePrefab) {
        if (!featurePrefab.IsValidTerrain(Land[pos]) || !featurePrefab.IsValidTerrain(Roof[pos])) return null;
        Feature feature = GameObject.Instantiate(featurePrefab, gameGrid.transform);
        PlaceFeature(pos, feature);
        return feature;
    }

    private void SetXWall(int x, int y, Construction construction) {
        xWalls[x, y] = construction;
        switch (construction) {
            case Construction.None:
                xWallTiles.SetTile(new Vector3Int(x, y, 0), null);
                validator.StabilizeAdjacentConstructionNext(new Position(Grid.XWalls, x, y));
            break;
            case Construction.Wood:
                xWallTiles.SetTile(new Vector3Int(x, y, 0), tiles.xFence);
            break;
        }
    }

    private void SetYWall(int x, int y, Construction construction) {
        yWalls[x, y] = construction;
        switch (construction) {
            case Construction.None:
                yWallTiles.SetTile(new Vector3Int(x, y, 0), null);
                validator.StabilizeAdjacentConstructionNext(new Position(Grid.YWalls, x, y));
            break;
            case Construction.Wood:
                yWallTiles.SetTile(new Vector3Int(x, y, 0), tiles.yFence);
            break;
        }
    }

    private void SetRoof(Vector2Int pos, Construction construction) {
        Vector3Int coord = (Vector3Int) pos;
        Construction oldRoof = roofs[coord.x, coord.y];
        roofs[coord.x, coord.y] = construction;
        switch (construction) {
            case Construction.None:
                mainTiles[RoofLevel].SetTile(coord, null);
                validator.StabilizeAdjacentConstructionNext(new Position(Grid.Roof, pos));
                if (oldRoof == Construction.Wood) {
                    GameObject collapse = GameObject.Instantiate(collapsePrefab, gameGrid.transform);
                    collapse.transform.position = gameGrid.GetCellCenterWorld((Vector3Int)pos);
                }
            break;
            case Construction.Wood:
                mainTiles[RoofLevel].SetTile(coord, tiles.woodBldg);
            break;
        }
    }

    private Construction GetConstructionStrict(Position position) {
        switch (position.grid) {
            case Grid.XWalls: return xWalls[position.x, position.y];
            case Grid.YWalls: return yWalls[position.x, position.y];
            default: return roofs[position.x, position.y];
        }
    }

    public Construction? GetConstruction(Position position) {
        return InBounds(position) ? GetConstructionStrict(position) : (Construction?)null;
    }

    private void SetConstruction(Position position, Construction construction) {
        if (position.grid == Grid.XWalls) SetXWall(position.x, position.y, construction);
        if (position.grid == Grid.YWalls) SetYWall(position.x, position.y, construction);
        if (position.grid == Grid.Roof) SetRoof(position.Coord, construction);
    }

    public Land?[] GetFourLandTilesAround(Vector2 pos) {
        Vector2Int firstCell = CellAt(pos + Vct.F(0, -.5f));
        return new Land?[] {
            GetLand(firstCell),
            GetLand(firstCell + Vct.I(1, 0)),
            GetLand(firstCell + Vct.I(0, 1)),
            GetLand(firstCell + Vct.I(1, 1))
        };
    }

    private void AddDepths() {
        for (int x = -16; x < Bounds.x + 16; ) {
            for (int y = -16; y < Bounds.y + 16; y++) mainTiles[1].SetTile(new Vector3Int(x, y, 0), tiles.deepWater);
            x++;
            if (x == 0) x = Bounds.x;
        }
        for (int x = 0; x < Bounds.x; x++) for (int y = -16; y < Bounds.y + 16; ) {
            mainTiles[1].SetTile(new Vector3Int(x, y, 0), tiles.deepWater);
            y++;
            if (y == 0) y = Bounds.y;
        }
    }

    private void PopulateTerrainFromUnityEditor() {
        foreach (var tilemap in mainTiles) {
            foreach (var position in tilemap.cellBounds.allPositionsWithin) {
                if (tilemap.HasTile(position)) {
                    TileBase tile = tilemap.GetTile(position);
                    switch (tile.name) {
                        case "Grass":
                            break;
                        case "Meadow":
                        case "Shrub":
                        case "Forest":
                        case "Hill":
                        case "Water":
                            land[position.x, position.y] = (Land) Enum.Parse(typeof(Land), tile.name);
                            break;
                        case "Ditch":
                            if (land[position.x, position.y] == global::Land.Grass) {
                                land[position.x, position.y] = global::Land.Ditch;
                            }
                            break;
                        case "WoodBldg":
                            roofs[position.x, position.y] = Construction.Wood;
                            break;
                    }
                }
            }
        }
        foreach (var position in xWallTiles.cellBounds.allPositionsWithin) {
            if (xWallTiles.HasTile(position)) {
                TileBase tile = xWallTiles.GetTile(position);
                xWalls[position.x, position.y] = Construction.Wood;
            }
        }
        foreach (var position in yWallTiles.cellBounds.allPositionsWithin) {
            if (yWallTiles.HasTile(position)) {
                TileBase tile = yWallTiles.GetTile(position);
                yWalls[position.x, position.y] = Construction.Wood;
            }
        }
    }

    public readonly struct Position {
        public static Position[] Edges = new Position[] {
                new Position(Grid.YWalls, 1, 0),
                new Position(Grid.XWalls, 0, 1),
                new Position(Grid.YWalls, 0, 0),
                new Position(Grid.XWalls, 0, 0)
            };
        public Position (Grid grid, int x, int y) {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }
        public Position (Grid grid, Vector2Int c) {
            this.grid = grid;
            this.x = c.x;
            this.y = c.y;
        }
        public static Position Edge(Vector2Int cell, int index) =>
            new Position(Grid.Roof, cell) + Edges[index];
        public readonly Grid grid;
        public readonly int x;
        public readonly int y;
        public Vector2Int Coord { get => new Vector2Int(x, y); }
        public static Position operator +(Position a, Position b) => new Position(b.grid, a.x + b.x, a.y + b.y);
        public Position ToRight() => new Position(grid, x + 1, y);
        public Position Ahead() => new Position(grid, x, y + 1);
        public Position ToLeft() => new Position(grid, x - 1, y);
        public Position Behind() => new Position(grid, x, y - 1);
        public bool AllFourSides(Func<Position, bool> func) => func(ToRight()) && func(Ahead()) && func(ToLeft()) && func(Behind());
        public bool IsOnOrAdjacentTo(Vector2Int cell) {
            switch (grid) {
                case Grid.XWalls:
                    return cell.x == x && (cell.y == y || cell.y == y - 1);
                case Grid.YWalls:
                    return cell.y == y && (cell.x == x || cell.x == x - 1);
                default:
                    return Math.Abs(cell.x - x) + Math.Abs(cell.y - y) <= 1;
            }
        }
        public override string ToString() => $"({grid}, {x}, {y})";
        public static Position zero = new Position(Grid.Roof, 0, 0);
    }
}