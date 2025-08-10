using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(FeatureHooks))]
public class Sprout : MonoBehaviour {
    public string adultFeature;
    private FeatureHooks feature;

    void Start() {
        feature = GetComponent<FeatureHooks>();
        if (feature.serializedFields != null) Deserialize(feature.serializedFields);
        feature.SerializeFields += Serialize;
        if (adultFeature == null) throw new NullReferenceException("adultFeature must be set");
        this.Invoke(Grow, delay.ForFeature(adultFeature));
    }

    int[] Serialize() => FeatureHooks.SerializeString(adultFeature);
    void Deserialize(int[] fields) => adultFeature = FeatureHooks.DeserializeString(fields);

    private void Grow() {
        FeatureConfig adult = FeatureLibrary.C.ByTypeName(adultFeature);
        Terrain.I.SetUpFeature((Vector2Int)feature.tile, Land.Grass, adult);
    }

    public static bool IsPlant(FeatureConfig feature)
        => feature != null && typeof(GrowthTimes).GetField(feature.type) != null;
    public static string RandomPlant() {
        IEnumerable<string> plants =
            from field in typeof(GrowthTimes).GetFields()
            select field.Name;
        return Randoms.InArray(plants.ToArray());
    }

    public GrowthTimes delay;
    [Serializable] public class GrowthTimes {
        public float ForFeature(string type) =>
            (float)this.GetType().GetField(type).GetValue(this);

        public float carrot;
    }
}
