using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public class Bucket {
    [SerializeField] public int max = 60;

    [SerializeField] private List<Material> materials = new List<Material>();

    public Bucket(int max) {
        this.max = max;
    }

    public int Size {
        get => (from material in materials select material.Quantity).Sum();
    }
    public int SpaceAvailable {
        get => max - Size;
    }

    public void AddMaterial(Material material) {
        materials.Add(material);
    }
}

public class Material {
    public enum Type {
        Scale,
        PondApple,
        ForestBlossom,
        Glass,
        Huckleberry,
        Gemstone,
        Arrow,
        Wood,
        Stone,
        Soil
    };

    private Type type;
    private Bucket bucket;
    [SerializeField] private int quantity;
    public Action<int> Changed;

    private Material(Type type, Bucket bucket) {
        this.type = type;
        this.bucket = bucket;
    }

    public static Material InBucket(Type type, Bucket bucket) {
        Material result = new Material(type, bucket);
        bucket.AddMaterial(result);
        return result;
    }

    public Type MaterialType { get => type; }

    public int Quantity { get => quantity; }

    public bool TryDecrease(int delta) {
        bool result = quantity >= delta;
        if (result) quantity -= delta;
        if (Changed != null) Changed(quantity);
        return result;
    }

    public int TryAdd(int delta) {
        int space = bucket.SpaceAvailable;
        if (space == 0) return 0;
        int actualDelta = Math.Min(delta, space);
        quantity += actualDelta;
        if (Changed != null) Changed(quantity);
        return actualDelta;
    }

    public void Clear() {
        quantity = 0;
        if (Changed != null) Changed(quantity);
    }

    public bool IsFull {
        get => bucket.SpaceAvailable == 0;
    }
}