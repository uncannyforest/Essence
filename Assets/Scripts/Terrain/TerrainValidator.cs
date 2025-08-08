using System;
using UnityEngine;

public class TerrainValidator {
    private const float ValidationUpdateTime = 0.5f;
    private static readonly Terrain.Position[] adjacentEdges = new Terrain.Position[]  {
        new Terrain.Position(Terrain.Grid.YWalls, new Vector2Int(1, 0)),
        new Terrain.Position(Terrain.Grid.XWalls, new Vector2Int(0, 1)),
        new Terrain.Position(Terrain.Grid.YWalls, new Vector2Int(0, 0)),
        new Terrain.Position(Terrain.Grid.XWalls, new Vector2Int(0, 0))
    };

    private Terrain terrain;

    private Action thisUpdate;
    private Action nextUpdate;

    public TerrainValidator(Terrain terrain) {
        this.terrain = terrain;
    }

    public void Initialize() {
        terrain.InvokeRepeating("StabilizeNow", ValidationUpdateTime, ValidationUpdateTime);
    }

    public void StabilizeNext(Action nextTile) {
        nextUpdate += nextTile;
    }

    public void StabilizeNow() {
        if (thisUpdate != null) thisUpdate();
        thisUpdate = nextUpdate;
        nextUpdate = null;
    }

    // Accepts positions outside bounds to simplify StabilizeAdjacentLandNext()
    // They will return doing nothing
    public void StabilizeLand(Vector2Int pos) {
        if (terrain.Bounds.Contains(pos) && !IsStableLand(pos, terrain.Land[pos])) {
            terrain.Land[pos] = Land.Water;
        }
    }

    public void StabilizeAdjacentLandNext(Vector2Int pos) {
        StabilizeNext(pos.AllFourSides(p => StabilizeLand(p)));
    }

    public bool IsValidLand(Vector2Int pos, Land tile) {
        if (terrain.Feature[pos]?.config?.IsValidTerrain(tile) == false) return false;
        return true;
    }

    public bool IsStableLand(Vector2Int pos, Land tile) {
        if (tile.IsDitchy()) {
            if (pos.AnyFourSides(p => terrain.Land[p].IsWatery())) return false;
        }
        return true;
    }

    public void StabilizeConstruction(Terrain.Position pos) {
        if (terrain.Bounds.Contains(pos.Coord) && terrain.Roof[pos.Coord] != Construction.None && !IsStableConstruction(pos, terrain[pos])) {
            terrain.SetUpFeature(pos.Coord, Land.Grass, FeatureLibrary.C.woodPile);
        }
    }

    public void StabilizeAdjacentConstructionNext(Terrain.Position pos) {
        switch (pos.grid) {
            case Terrain.Grid.XWalls:
                StabilizeNext(() => StabilizeConstruction(new Terrain.Position(Terrain.Grid.Roof, pos.x, pos.y - 1)));
                StabilizeNext(() => StabilizeConstruction(new Terrain.Position(Terrain.Grid.Roof, pos.x, pos.y)));
            break;
            case Terrain.Grid.YWalls:
                StabilizeNext(() => StabilizeConstruction(new Terrain.Position(Terrain.Grid.Roof, pos.x - 1, pos.y)));
                StabilizeNext(() => StabilizeConstruction(new Terrain.Position(Terrain.Grid.Roof, pos.x, pos.y)));
            break;
            default:
                StabilizeNext(pos.Coord.AllFourSides(p => StabilizeConstruction(new Terrain.Position(pos.grid, p))));
            break;
        }
    }

    public bool IsStableConstruction(Terrain.Position pos, Construction tile) {
        if (!terrain.InBounds(pos)) return false;
        switch (pos.grid) {
            case Terrain.Grid.XWalls:
                return terrain.GetLand(pos.x, pos.y - 1)?.IsHilly() == false && !terrain.Land[pos.Coord].IsHilly();
            case Terrain.Grid.YWalls:
                return terrain.GetLand(pos.x - 1, pos.y)?.IsHilly() == false && !terrain.Land[pos.Coord].IsHilly();
            default:
                if (terrain.Feature[pos.Coord]?.config?.IsValidTerrain(tile) == false) return false;
                Land land = terrain.Land[pos.Coord];
                if (land == Land.Meadow ||
                    land == Land.Shrub ||
                    land == Land.Forest ||
                    land == Land.Dirtpile ||
                    land == Land.Hill ||
                    land == Land.PavedTunnel ||
                    land == Land.WaterTunnel ||
                    land == Land.DirtTunnel) return false;
                int numWalls = GetNumWallsUnderStructure(pos, tile, out int onlyWallLocation);
                if (numWalls >= 2) return true;
                if (pos.AllFourSides(p => IsRoofWithMinWalls(p, tile, 1))) return true;
                if (numWalls == 1 && (
                    (onlyWallLocation == 0 && IsRoofWithMinWalls(pos.ToLeft(), tile, 2)) ||
                    (onlyWallLocation == 1 && IsRoofWithMinWalls(pos.Behind(), tile, 2)) ||
                    (onlyWallLocation == 2 && IsRoofWithMinWalls(pos.ToRight(), tile, 2)) ||
                    (onlyWallLocation == 3 && IsRoofWithMinWalls(pos.Ahead(), tile, 2)) ||
                    (onlyWallLocation % 2 == 0 &&
                        IsRoofWithMinWalls(pos.Ahead(), tile, 2) &&
                        IsRoofWithMinWalls(pos.Behind(), tile, 2) ) ||
                    (onlyWallLocation % 2 == 1 &&
                        IsRoofWithMinWalls(pos.ToRight(), tile, 2) &&
                        IsRoofWithMinWalls(pos.ToLeft(), tile, 2) )
                )) return true;                    
                return false;
        }
    }

    private bool IsRoofWithMinWalls(Terrain.Position pos, Construction type, int minWalls) {
        return terrain.Roof[pos.Coord] == type && GetNumWallsUnderStructure(pos, type, out _) >= minWalls;
    }

    private int GetNumWallsUnderStructure(Terrain.Position pos, Construction type, out int onlyWallLocation) {
        int numExistingWalls = 0;
        onlyWallLocation = -1;
        for (int i = 0; i < 4; i++) {
            Terrain.Position adj = adjacentEdges[i];
            if (terrain[pos + adj] == type) {
                numExistingWalls++;
                onlyWallLocation = i;
            }
        }
        return numExistingWalls;
    }

}