
using System;
using System.Collections.Generic;
using UnityEngine;

public class FlowFieldDeluxe : FlowField  {
    private int radius;

    public FlowFieldDeluxe(Vector3 location, int radius, float expiry, PathfindingCost detectBarriers, float barrierCost) {
        this.radius = radius;
        this.detectBarriers = detectBarriers;
        this.expiry = expiry;
        this.barrierCost = barrierCost;
        this.direction = new float[2 * radius + 1, 2 * radius + 1];
        this.totalCost = new float[2 * radius + 1, 2 * radius + 1];
        this.orthogonalPermutations = new int[2 * radius + 1, 2 * radius + 1];
        Update(location);
    }

    override public void Update(Vector3 location) {
        this.location = location;
        UpdateBounds();
        Update();
    }

    public void UpdateBounds() {
        Vector2Int center = Terrain.I.CellAt(location);
        this.boundsMin = center - Vector2Int.one * this.radius;
        this.boundsMax = center + Vector2Int.one * this.radius;
    }

     // Returns null if we're already in the right cell
     // Ignores flow field if out of bounds
     override public float? GetDirection(Vector2Int cell, Vector3 location) {
        if (Time.time > expiryTime) Update(location);
        if (!InBounds(cell.x, cell.y)) {
            Debug.LogWarning("Accessing flow field outside bounds - use a different strategy.  Accessing " + cell.x + " " + cell.y + " " + location);
            return Disp.FT(Terrain.I.CellCenter(cell), location).angle;
        }
        return Get(direction, cell);
    }
}
