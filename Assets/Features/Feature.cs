using System;
using UnityEngine;

public struct Feature {
    public FeatureConfig config;
    // FeatureHooks is the bridge to Unity features.  It's a component.  Basic Features have this set to null.
    public FeatureHooks hooks;

    // PROPERTIES

    public string ResourceName => hooks?.GetResourceName() ?? config.resourceName;
    public int ResourceQuantity => hooks?.GetResourceQuantity() ?? config.resourceQuantity;

    // SERIALIZATION

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