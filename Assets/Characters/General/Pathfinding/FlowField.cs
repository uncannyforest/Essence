using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

public class FlowField {
    protected Vector3 location;
    protected float expiry;
    public Vector2Int boundsMin;
    public Vector2Int boundsMax;
    protected PathfindingCost detectBarriers;
    protected float barrierCost;

    public float expiryTime;
    protected float[,] direction;
    protected float[,] totalCost;
    // The only way I've figured out to properly account for diagonals,
    // orthogonal permutations is approximately 1 + number of "good" diagonals.
    // This avoids factoring in "bad" diagonals that cut a tight corner.
    protected int[,] orthogonalPermutations;

    protected FlowField() {}

    public FlowField(Vector3 location, BoundsInt bounds, float expiry, PathfindingCost detectBarriers, float barrierCost) {
        this.detectBarriers = detectBarriers;
        this.expiry = expiry;
        this.boundsMin = new Vector2Int(bounds.min.x, bounds.min.y);
        this.boundsMax = new Vector2Int(bounds.max.x, bounds.max.y);
        this.detectBarriers = detectBarriers;
        this.barrierCost = barrierCost;
        this.direction = new float[bounds.max.x - bounds.min.x + 1, bounds.max.y - bounds.min.y + 1];
        this.totalCost = new float[bounds.max.x - bounds.min.x + 1, bounds.max.y - bounds.min.y + 1];
        this.orthogonalPermutations = new int[bounds.max.x - bounds.min.x + 1, bounds.max.y - bounds.min.y + 1];
        Update(location);
    }

    // Returns null if we're already in the right cell
    // Throws error if out of bounds
    virtual public float? GetDirection(Vector2Int cell, Vector3 location) {
        if (cell == Terrain.I.CellAt(location)) return null;
        if (!InBounds(cell.x, cell.y))
            throw new IndexOutOfRangeException("Out of bounds: " + cell.x + ", " + cell.y);
        if (Time.time > expiryTime) Update(location);
        return Get(direction, cell);
    }

    protected bool InBounds(int x, int y) {
        return x >= boundsMin.x && y >= boundsMin.y && x <= boundsMax.x && y <= boundsMax.y;
    }

    virtual public void Update(Vector3 location) {
        this.location = location;
        Update();
    }

    protected void Update() {
        expiryTime = Time.time + expiry;

        Array.Clear(direction, 0, direction.Length);
        Array.Clear(totalCost, 0, totalCost.Length);
        Array.Clear(orthogonalPermutations, 0, orthogonalPermutations.Length);

        Vector2Int start = Terrain.I.CellAt(location);
        SimplePriorityQueue<Vector2Int> q = new SimplePriorityQueue<Vector2Int>();
        q.Enqueue(start, 1);
        float initialAngle = Disp.FT(Terrain.I.CellCenter(start), location).angle;
        if (initialAngle < 0) initialAngle += 360; // this class's range is [0, 360) but Unity's is [-180, 180]
        Set(direction, q.First, initialAngle);
        Set(orthogonalPermutations, q.First, 1);
        
        while (q.Count > 0) {
            Vector2Int current = q.First;
            float currentValue = q.GetPriority(current);
            q.Dequeue();
            Set(totalCost, current, currentValue);
            float currentAngle = Get(direction, current);
            int currentOp = Get(orthogonalPermutations, current);

            ProcessAdjacent(current, (adj, dir) => {
                if (!InBounds(adj.x, adj.y)) return;
                if (IsClosed(adj)) return;

                float possPriority = detectBarriers.CostAt(adj) + currentValue;
                if (!q.Contains(adj)) {
                    q.Enqueue(adj, possPriority);
                    Set(direction, adj, dir);
                    Set(orthogonalPermutations, adj, currentOp);
                } else if (q.GetPriority(adj) > possPriority) {
                    q.UpdatePriority(adj, possPriority);
                    Set(direction, adj, dir);
                    Set(orthogonalPermutations, adj, currentOp);
                } else if (q.GetPriority(adj) == possPriority) {
                    // handle diagonals
                    float oldAngle = Get(direction, adj);
                    float newAngle = dir;
                    int oldOp = Get(orthogonalPermutations, adj);
                    int newOp = currentOp;
                    float lerpFactor = (float)newOp / (oldOp + newOp);
                    Set(direction, adj, AverageAngles(oldAngle, newAngle, lerpFactor));
                    Set(orthogonalPermutations, adj, oldOp + newOp);
                }
            });
        }

        DebugLines();
    }

    protected T Get<T>(T[,] array, Vector2Int location) {
        Vector2Int index = location - boundsMin;
        return array[index.x, index.y];
    }
    protected void Set<T>(T[,] array, Vector2Int location, T value) {
        Vector2Int index = location - boundsMin;
        array[index.x, index.y] = value;
    }

    protected bool IsClosed(Vector2Int location) => Get(totalCost, location) != 0;

    protected void ProcessAdjacent(Vector2Int current, Action<Vector2Int, float> process) {
        process(current + new Vector2Int(1, 0), 180);
        process(current + new Vector2Int(0, 1), 270);
        process(current + new Vector2Int(-1, 0), 0);
        process(current + new Vector2Int(0, -1), 90);
    }

    // input and output range [0, 360)
    protected float AverageAngles(float a, float b, float lerp) {
        float max = Mathf.Max(a, b);
        float min = Mathf.Min(a, b);
        if (max - min < 180) return Mathf.Lerp(a, b, lerp);
        if (a < b) a += 360;
        else b += 360;
        // now max - min < 180
        float result = Mathf.Lerp(a, b, lerp);
        if (result >= 360) return result - 360;
        else return result;
    }

    protected void DebugLines() {
        for (int x = boundsMin.x; x <= boundsMax.x; x++) {
            for (int y = boundsMin.y; y <= boundsMax.y; y++) {
                if (expiry < 2)
                    Debug.DrawLine(Terrain.I.CellCenter(Vct.I(x, y)),
                        Terrain.I.CellCenter(Vct.I(x, y)) + Displacement.FromAngle(Get(direction, new Vector2Int(x, y))) / 2, Color.white, expiry);
            }
        }
    }
}