using System;
using UnityEngine;

// Code relating to the construction of Features

[Serializable]
public class FeatureConfig {
    [NonSerialized] public string type;

    public LandFlags validLand = LandFlags.Grass & LandFlags.Dirtpile;
    public bool roofValid = true;
    public Sprite sprite;
    public bool impassable = true;
    public int strength = 5;  // min strength necessary to damage/destroy this. Player has 10
    public int maxHealth = 10; // Player has ATK 10
    public FeatureHooks prefab = null; // (optional) GameObject with further logic to spawn here
    public string resourceName = "";  // resource name when destroyed
    public int resourceQuantity = 0; // resource quantity when destroyed, may be overridden in FeatureHooks
    public bool isResourcePile = false; // if not, replace with resource pile when destroyed by player
    public Color resourceColor = Color.white; // used in status bars and default pile render

    private Feature Instantiate() {
        if (prefab == null) return new Feature() { config = this, hooks = null };
        FeatureHooks hooks = GameObject.Instantiate(prefab);
        hooks.feature = new Feature() { config = this, hooks = hooks };
        return hooks.feature;
    }
    public Feature? MaybeInstantiate(Vector2Int pos) {
        if (!IsValidTerrain(pos)) return null;
        Feature feature = Instantiate();
        if (feature.hooks != null) feature.hooks.Place(pos);
        else Terrain.I.ForceSetFeature(pos, feature);
        return feature;
    }

    public bool IsValidTerrain(Land land) => ((int)validLand & 1 << (int)land) != 0;
    public bool IsValidTerrain(Construction construction) => construction == Construction.None || roofValid;
    public WhyNot IsValidTerrain(Vector2Int pos) {
        if (!Terrain.I.InBounds(pos)) return "out of bounds";
        if (Terrain.I.Feature[pos] != null) return "feature already present: " + Terrain.I.Feature[pos]?.config?.type;
        if (!IsValidTerrain(Terrain.I.Land[pos]))
            return "land: " + Terrain.I.Land[pos] + " / allowed: " + validLand;
        if (!IsValidTerrain(Terrain.I.Roof[pos]))
            return "roof: " + Terrain.I.Roof[pos] + " / allowed: " + validLand;
        return true;
    }

    public static bool operator==(FeatureConfig a, FeatureConfig b) => a is null ? b is null : a.Equals(b);
    public static bool operator!=(FeatureConfig a, FeatureConfig b) => a is null ? !(b is null) : !a.Equals(b);
    public override bool Equals(object obj) {
        if (obj is FeatureConfig f) return this.type.Equals(f.type);
        else return false;
    }
    public override int GetHashCode() => type.GetHashCode();

    public bool IsTypeOf(Feature? feature) => Equals(feature?.config);
}