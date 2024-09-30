
using UnityEngine;

public class EcologicalSuccession : MonoBehaviour {
    public float frequency = 1f;
    public int radius = 24;
    public Terrain terrain;
    public Transform player;

    private Vector2Int[] adjacentSquares = {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1)
    };

    private Vector2Int?[] adjacentEdges = {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 0),
        new Vector2Int(0, 0),
        null, null, null, null
    };

    void Start() {
        InvokeRepeating("Grow", frequency, frequency);
    }

    public void Grow() {
        Vector2Int center = terrain.CellAt(player.position);

        Vector2Int location = center;
        Land? attemptedGrowth = null;
        int seed = Random.Range(0, 8192);
        while (seed >= 0) {
            location = GetCloseLocation(center, radius);
            attemptedGrowth = MaybeGrow(location, out int probability);
            seed -= 8192 / probability;
        }

        if (attemptedGrowth is Land growth) {
            if (growth == Land.Woodpile || growth == Land.Rockpile) {
                terrain.Roof[location] = Construction.None;
            }
            terrain.SetLand(location, growth);
        }
    }

    private Land? MaybeGrow(Vector2Int location, out int prob) {
        Construction currentRoof = terrain.Roof[location];
        if (currentRoof == Construction.Wood) {
            prob = 512;
            return Land.Woodpile;
        }
        if (currentRoof == Construction.Stone) {
            prob = 512;
            return Land.Rockpile;
        }

        Land currentLand = terrain.Land[location];
        if ((currentLand == Land.Grass || currentLand == Land.Meadow || currentLand == Land.Shrub || currentLand == Land.Ditch) &&
                IsSurrounded(currentLand, location, (Random.value < 0.5f), out bool includingWater, out int count)
                is Land surroundingLand) {
            if (currentLand != Land.Ditch) {
                Land newLand = (Land)(Random.Range((int)ToGrassOrFlora(currentLand), (int)surroundingLand) + 1);
                if (!includingWater) {
                    if (count >= 3) prob = 1;
                    else if (count == 2) prob = newLand == Land.Forest ? 32 : newLand == Land.Shrub ? 4 : 8;
                    else prob = newLand == Land.Shrub ? 128 : 256;
                } else {
                    if (count >= 3) prob = newLand == Land.Forest ? 8 : newLand == Land.Shrub ? 2 : 4;
                    else prob = newLand == Land.Forest ? 64 : 32;
                }
                return newLand;
             } else {
                if (count >= 3) prob = 2;
                else if (count == 2) prob = 32;
                else prob = 512;
                return (Land)(Random.Range((int)ToGrassOrFlora(currentLand), (int)surroundingLand) + 1);
             }
        }

        int random;
        switch (currentLand) {
            case Land.Shrub:
                prob = 512;
                return Land.Forest;
            case Land.Meadow:
                prob = 512;
                return Random.Range(0, 17) == 0 ? Land.Forest : Land.Shrub;
            case Land.Grass:
                prob = 1024;
                random = Random.Range(0, 18);
                return random == 0 ? Land.Forest : random == 1 ? Land.Shrub : Land.Meadow;
            case Land.Ditch:
                prob = 2048;
                random = Random.Range(0, 18);
                return random == 0 ? Land.Forest : random == 1 ? Land.Shrub : Land.Meadow;
            case Land.Woodpile:
            case Land.Rockpile:
                prob = 4096;
                return Land.Meadow;
            case Land.Road:
                prob = 8192;
                return Land.Meadow;
        }

        prob = 8192;
        return null;
    }

    private Vector2Int GetCloseLocation(Vector2Int center, int radius) {
        return new Vector2Int(
            Random.Range(System.Math.Max(0, center.x - radius), System.Math.Min(terrain.Bounds.x, center.x + radius + 1)),
            Random.Range(System.Math.Max(0, center.y - radius), System.Math.Min(terrain.Bounds.y, center.y + radius + 1)));
    }

    private bool IsLogicalGrowth(Land start, Land end) {
        switch(end) {
            case Land.Forest:
                switch(start) { 
                    case Land.Forest: return false;
                    case Land.Shrub: return true;
                    case Land.Meadow: return true;
                    default: return true;
                }
            case Land.Shrub:
                switch(start) { 
                    case Land.Forest: return false;
                    case Land.Shrub: return false;
                    case Land.Meadow: return true;
                    default: return true;
                }
            case Land.Meadow:
                switch(start) { 
                    case Land.Forest: return false;
                    case Land.Shrub: return false;
                    case Land.Meadow: return false;
                    default: return true;
                }
            default: return false;
        }
    }

    private Land ToGrassOrFlora(Land land) {
        if (land <= Land.Forest) return land;
        return Land.Grass;
    }

    private Land? IsSurrounded(Land currentLand, Vector2Int center, bool maximize, out bool includingWater, out int count) {
        int waterCount = 0;
        int meadowCount = 0;
        int shrubCount = 0;
        int forestCount = 0;
        for (int i = 0; i < 8; i++) {
            if (adjacentEdges[i] is Vector2Int adjacentEdge) {
                Vector2Int edge = center + adjacentEdge;
                if ((i % 2 == 0 ? terrain.YWall[edge] : terrain.YWall[edge]) != Construction.None)
                    continue;
            }
            Vector2Int square = center + adjacentSquares[i];
            Land land = terrain.GetLand(square) ?? terrain.Depths;
            switch(land) {
                case Land.Water:
                    waterCount++;
                break;
                case Land.Meadow:
                    meadowCount++;
                break;
                case Land.Shrub:
                    shrubCount++;
                break;
                case Land.Forest:
                    forestCount++;
                break;
            }
        }

        int minimum;
        if (maximize) minimum = 1;
        else minimum = Mathf.Min(3, meadowCount + waterCount + shrubCount + forestCount);
        
        if (forestCount >= minimum) {
            count = forestCount;
            includingWater = false;
            return Land.Forest;
        } else if (IsLogicalGrowth(currentLand, Land.Shrub) && forestCount + shrubCount >= minimum) {
            count = shrubCount + forestCount;
            includingWater = false;
            return Land.Shrub;
        } else if (IsLogicalGrowth(currentLand, Land.Meadow) && forestCount + shrubCount + meadowCount >= 1) {
            count = meadowCount + shrubCount + forestCount;
            includingWater = false;
            return Land.Meadow;
        } else if (waterCount + forestCount >= minimum && (forestCount >= 1 || currentLand == Land.Shrub)) {
            count = forestCount + waterCount;
            includingWater = true;
            return Land.Forest;
        } else if (IsLogicalGrowth(currentLand, Land.Shrub) && waterCount + forestCount + shrubCount >= minimum && (shrubCount >= 1 || currentLand == Land.Meadow)) {
            count = shrubCount + forestCount + waterCount;
            includingWater = true;
            return Land.Shrub;
        } else if (IsLogicalGrowth(currentLand, Land.Meadow) && waterCount + forestCount + shrubCount + meadowCount >= 1) {
            count = meadowCount + shrubCount + forestCount + waterCount;
            includingWater = true;
            return Land.Meadow;
        }
        count = 0;
        includingWater = false;
        return null;
    }

}
