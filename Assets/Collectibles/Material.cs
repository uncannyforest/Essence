using System;
using UnityEngine;

public class Material {
    public enum Type {
        Scale,
        PondApple,
        ForestBlossom,
        Glass,
        Huckleberry,
        Gemstone,
        Sword,
        Arrow,
        Praxel,
        Wood,
        Stone,
        Soil
    };

    private Type type;
    private int max;
    private int quantity;
    public Action<int> Changed;

    public Material(Type type, int max) {
        this.type = type;
        this.max = max;
    }

    public Type MaterialType { get => type; }

    public int Quantity { get => quantity; }

    public bool TryDecrease(int delta) {
        bool result = quantity >= delta;
        if (result) quantity -= delta;
        if (Changed != null) Changed(quantity);
        return result;
    }

    public int ChangeQuantity(int delta) {
        int oldQuantity = quantity;
        quantity = Mathf.Clamp(quantity + delta, 0, max);
        if (Changed != null) Changed(quantity);
        return quantity - oldQuantity;
    }

    public void Clear() {
        quantity = 0;
        if (Changed != null) Changed(quantity);
    }

    public bool IsFull {
        get => max == quantity;
    }
}