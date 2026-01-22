using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PathfindingCost {
    public float water;

    public float SpeedAt(Vector2Int currentTile) {
        Land land = Terrain.I.GetLand(currentTile) ?? Terrain.I.Depths;
        return (water != 0 && land == Land.Water) ? water : 1;
    }

    public bool IsPassable(Vector2Int tile) {
        // TODO feature speeds
        if (Terrain.I.IsFeature(tile, out Feature feature)) {
            if (feature.config.impassable) return false;
        }

        Land land = Terrain.I.GetLand(tile) ?? Terrain.I.Depths;
        bool notWaterProhibited = water != 0f || land != Land.Water;
        return notWaterProhibited && land.IsPassable();
    }

    public float CostAt(Vector2Int tile) {
        if (!IsPassable(tile)) return FlowFieldRegistry.I.barrierCost;
        float speed = SpeedAt(tile);
        if (speed == 0) return FlowFieldRegistry.I.barrierCost;
        else return 1 / speed;
    }

    public override int GetHashCode() => water.GetHashCode();
    public override bool Equals(object obj)
        => obj is PathfindingCost cost && cost != null
        && this.water == cost.water;
}
