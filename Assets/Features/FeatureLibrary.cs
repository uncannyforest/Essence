using System;
using System.Reflection;
using UnityEngine;

public class FeatureLibrary : MonoBehaviour {
    private static FeatureLibrary instance;
    public static FeatureLibrary C {
        get => instance;
    }
    void Awake() {
        if (instance == null) instance = this;

        foreach (FieldInfo field in this.GetType().GetFields())
            if (field.GetValue(this) is FeatureConfig feature)
                feature.type = field.Name;
    }
    public FeatureConfig ByTypeName(string type) {
        return (FeatureConfig)this.GetType().GetField(type).GetValue(this);
    }

    public GameObject renderPrefab;
    public FeatureConfig fountain;
    public FeatureConfig windmill;
    public FeatureConfig boat;
    public FeatureConfig jasmine;
    public FeatureConfig carrot;
    public FeatureConfig arrowPile;
    public FeatureConfig woodPile;
    public FeatureConfig sprout;
}
