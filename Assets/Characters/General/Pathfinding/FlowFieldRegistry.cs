using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public struct FlowFieldRegistryKey {
    public PositionProvider target;
    public PathfindingCost cost;

    public FlowFieldRegistryKey(PositionProvider target, PathfindingCost cost) {
        this.target = target;
        this.cost = cost;
    }

    public override int GetHashCode() => cost.GetHashCode() + target.GetHashCode();
    public override bool Equals(object obj) 
        => obj is FlowFieldRegistryKey key && target.Equals(key.target) && cost.Equals(key.cost);
}

[Serializable]
public struct FlowFieldRegistryDebugObject {
    public Transform transformTarget;
    public Vector3 positionTarget;
    public PathfindingCost cost;
    // public float expiryTime;

    public FlowFieldRegistryDebugObject(FlowFieldRegistryKey key, float expiryTime) {
        this.transformTarget = null;
        this.positionTarget = Vector3.zero;
        if (key.target.t != null) this.transformTarget = key.target.t;
        else this.positionTarget = key.target.position;
        this.cost = key.cost;
        // this.expiryTime = expiryTime;
    }
}

public class FlowFieldRegistry : MonoBehaviour {
    private static FlowFieldRegistry instance;
    FlowFieldRegistry(): base() {
        instance = this;
    }
    public static FlowFieldRegistry I { get => instance; }

    public float barrierCost = 255;
    public float transformUpdateTime = 1;
    public float stationaryUpdateTime = 3;

    private Dictionary<FlowFieldRegistryKey, FlowField> values = new Dictionary<FlowFieldRegistryKey, FlowField>();
    public List<FlowFieldRegistryDebugObject> valuesDebug = new List<FlowFieldRegistryDebugObject>();

    private static float? GetAngle(PositionProvider target, PathfindingCost cost, Vector3 follower) {
        FlowField ff = instance.GetKey(target, cost);
        return ff.GetDirection(Terrain.I.CellAt(follower), target.position);
    }
    public static Displacement GetDirection(PositionProvider target, PathfindingCost cost, Vector3 follower) {
        if (Terrain.I.CellAt(follower) == Terrain.I.CellAt(target.position))
            return Disp.FT(follower, target.position).normalized;
       else return Displacement.FromAngle((float)GetAngle(target, cost, follower));
    }

    private FlowField GetKey(PositionProvider target, PathfindingCost cost) {
        FlowFieldRegistryKey key = new FlowFieldRegistryKey(target, cost);
        if (!values.ContainsKey(key)) {
            float updateTime = target.t != null ? transformUpdateTime : stationaryUpdateTime;
            values.Add(key, new FlowFieldDeluxe(target.position, Mathf.CeilToInt(Creature.neighborhood), updateTime, cost, barrierCost));
            valuesDebug.Add(new FlowFieldRegistryDebugObject(key, values[key].expiryTime));
        }
        return values[key];
    }
}
