using System;
using System.Collections.Generic;
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
    public int resourceQuantity = 0; // resource quantity when destroyed
    public string replaceWhenDestroyedByPlayer = null; // replace with another feature when destroyed by player - or null

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

public struct Feature {
    public FeatureConfig config;
    public FeatureHooks hooks;

    public static bool operator==(Feature a, Feature b) => a.Equals(b);
    public static bool operator!=(Feature a, Feature b) => !a.Equals(b);
    public override bool Equals(object obj) {
        if (obj is Feature f) return this.config.type.Equals(f.config.type);
        else return false;
    }
    public override int GetHashCode() => config.type.GetHashCode();

    [Serializable] public struct Data {
        public int x;
        public int y;
        public string type;
        public int[] customFields; // serialization hack

        public Vector2Int tile { get => Vct.I(x, y); }

        public Data(int x, int y, string type, int[] customFields) {
            this.x = x;
            this.y = y;
            this.type = type;
            this.customFields = customFields;
        }

        override public string ToString() {
            return type + ": " + tile;
        }
    }
    public Data? Serialize(int x, int y) {
        int[] customFields;
        if (hooks == null || hooks.SerializeFields == null) customFields = new int[0];
        else customFields = hooks.SerializeFields();
        return new Data(x, y, config.type, customFields);
    }
    public void DeserializeUponStart(int[] customFields) {
        if (hooks != null) hooks.serializedFields = customFields;
    }
}