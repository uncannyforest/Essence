using System.Collections.Generic;
using UnityEngine;
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
            get => terrain.InBounds(key) ? terrain.features[key.x, key.y] : null;
            set => terrain.PlaceFeature(key, value);
        }
        public Feature this[int x, int y] {
            get => terrain.InBounds(Vct.I(x, y)) ? terrain.features[x, y] : null;
            set => terrain.PlaceFeature(Vct.I(x, y), value);
        }
    }

    public float validationUpdateTime = 0.5f;
    public GameObject collapsePrefab;

    public readonly TerrainValidator validator;
    public readonly Concealment concealment;

    public const int Dim = 256;
    private Land[,] land = new Land[Dim, Dim];
    private Construction[,] xWalls = new Construction[Dim, Dim + 1];
    private Construction[,] yWalls = new Construction[Dim + 1, Dim];
    private Construction[,] roofs = new Construction[Dim, Dim];
    private Feature[,] features = new Feature[Dim, Dim];

    public LandIndex Land;
    public ConstructionIndex XWall;
    public ConstructionIndex YWall;
    public ConstructionIndex Roof;
    public FeatureIndex Feature;

    [NonSerialized] public MapRenderer3D mapRenderer;

    private static Terrain instance;
    public static Terrain I { get => instance; }
    Terrain(): base() {
        validator = new TerrainValidator(this);
        concealment = new Concealment(this);
        instance = this;
    }

    public Action Started; // must be set before Start() is run

    void Awake() {
        Land = new LandIndex(this);
        XWall = new ConstructionIndex(this, Grid.XWalls);
        YWall = new ConstructionIndex(this, Grid.YWalls);
        Roof = new ConstructionIndex(this, Grid.Roof);
        Feature = new FeatureIndex(this);

        validator.Initialize();
        mapRenderer = this.GetComponentStrict<MapRenderer3D>();
        concealment.Initialize(mapRenderer.HideTile);
    }

    void Start() {
        if (Started != null) Started();
    }

    void Loaded() {
        mapRenderer.OnTerrainLoaded();
    }

    void StabilizeNow() => validator.StabilizeNow();

    public Bounds Bounds {
        get => new Bounds(Dim, Dim);
    }
    public Land Depths {
        get => global::Land.Hill;
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
    public Vector2 CellCenter(Position position) => mapRenderer.CellCenter(position);
    public Vector2 CellCenter(Vector2Int cellPosition) => mapRenderer.CellCenter(cellPosition);
    public Vector2Int CellAt(Vector3 worldPosition) => mapRenderer.CellAt(worldPosition);
    public Vector2 CellCenterAt(Vector3 screenPosition) => mapRenderer.CellCenterAt(screenPosition);

    public Land? GetLand(int x, int y) => GetLand(new Vector2Int(x, y));
    public Land? GetLand(Vector2Int coord) => InBounds(coord) ? (Land?)Land[coord] : null;

    public bool SetLand(Vector2Int pos, Land terrain, bool force = false) {
        if (!force && !validator.IsValidLand(pos, terrain)) return false;
        land[pos.x, pos.y] = terrain;
        mapRenderer.UpdateLand(pos);
        if (force) return true;
        validator.StabilizeNext(() => validator.StabilizeLand(pos));
        validator.StabilizeAdjacentLandNext(pos);
        return true;
    }

    public Construction this[Position key] {
        get => GetConstructionStrict(key);
        set => SetConstruction(key, value);
    }

    public bool PlaceFeature(Vector2Int pos, Feature feature) {
        if (feature == null) return DestroyFeature(pos);
        if (features[pos.x, pos.y] != null) return false;
        if (!feature.IsValidTerrain(Land[pos]) || !feature.IsValidTerrain(Roof[pos])) return false;
        feature.transform.position = CellCenter(pos).WithZ(GlobalConfig.I.elevation.features);
        features[pos.x, pos.y] = feature;
        feature.tile = pos;
        return true;
    }
    public Feature BuildFeature(Vector2Int pos, Feature featurePrefab) {
        if (!featurePrefab.IsValidTerrain(Land[pos]) || !featurePrefab.IsValidTerrain(Roof[pos])) return null;
        Feature feature = GameObject.Instantiate(featurePrefab, mapRenderer.WorldParent);
        PlaceFeature(pos, feature);
        return feature;
    }
    public Feature ForceBuildFeature(Vector2Int pos, Feature featurePrefab) {
        DestroyFeature(pos);
        if (!featurePrefab.IsValidTerrain(Land[pos])) SetLand(pos, featurePrefab.GetSomeValidLand());
        if (!featurePrefab.IsValidTerrain(Roof[pos])) SetRoof(pos, Construction.None);
        Feature feature = GameObject.Instantiate(featurePrefab, mapRenderer.WorldParent);
        PlaceFeature(pos, feature);
        return feature;
    }
    public Feature UninstallFeature(Vector2Int pos) {
        Feature feature = features[pos.x, pos.y];
        features[pos.x, pos.y] = null;
        if (feature != null) feature.tile = null;
        return feature;
    }
    public bool DestroyFeature(Vector2Int pos) {
        Feature feature = UninstallFeature(pos);
        if (feature != null) {
            GameObject.Destroy(feature.gameObject);
            return true;
        } else return false;
    }

    private void SetXWall(int x, int y, Construction construction, bool force = false) {
        xWalls[x, y] = construction;
        mapRenderer.UpdateXWall(x, y);
        if (construction == Construction.None && !force)
            validator.StabilizeAdjacentConstructionNext(new Position(Grid.XWalls, x, y));
    }

    private void SetYWall(int x, int y, Construction construction, bool force = false) {
        yWalls[x, y] = construction;
        mapRenderer.UpdateYWall(x, y);
        if (construction == Construction.None && !force)
            validator.StabilizeAdjacentConstructionNext(new Position(Grid.XWalls, x, y));
    }

    private void SetRoof(Vector2Int pos, Construction construction, bool force = false) {
        Construction oldRoof = roofs[pos.x, pos.y];
        roofs[pos.x, pos.y] = construction;
        mapRenderer.UpdateRoof(pos);
        if (construction == Construction.None && !force) {
            validator.StabilizeAdjacentConstructionNext(new Position(Grid.Roof, pos));
            if (oldRoof == Construction.Wood) {
                GameObject collapse = GameObject.Instantiate(collapsePrefab, mapRenderer.WorldParent);
                collapse.transform.position = CellCenter(pos);
            }
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

    public Land?[] GetFourLandTilesAround(Vector2 pos) => mapRenderer.GetFourLandTilesAround(pos);

    public Land[,] AllLandTiles => land;
    public Construction[,] AllXWallTiles => xWalls;
    public Construction[,] AllYWallTiles => yWalls;
    public Construction[,] AllRoofTiles => roofs;

    public void PopulateTerrainFromData(MapData mapData) {
        for (int x = 0 ; x < Dim; x++) for (int y = 0; y < Dim; y++)
            SetLand(Vct.I(x, y), mapData.land[x, y], true);
        for (int x = 0 ; x < Dim; x++) for (int y = 0; y <= Dim; y++)
            SetXWall(x, y, mapData.xWalls[x, y], true);
        for (int x = 0 ; x <= Dim; x++) for (int y = 0; y < Dim; y++)
            SetYWall(x, y, mapData.yWalls[x, y], true);
        for (int x = 0 ; x < Dim; x++) for (int y = 0; y < Dim; y++)
            SetRoof(Vct.I(x, y), mapData.roofs[x, y], true);
        foreach (Feature.Data featureData in mapData.features)
            BuildFeature(featureData.tile, FeatureLibrary.P.ByTypeName(featureData.type))
                .DeserializeUponStart(featureData.customFields);
        I.Loaded();
    }

    public static void GenerateNewWorld() {
        TerrainGenerator.GenerateTerrain(I);
        Vector2Int startLocation = TerrainGenerator.PlaceFountains(I);
        TerrainGenerator.FinalDecor(I, startLocation);
        Debug.Log("Loadededed");
        I.Loaded();
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