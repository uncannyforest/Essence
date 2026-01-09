using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class FeatureLibrary : MonoBehaviour {
    private static FeatureLibrary instance;
    public static FeatureLibrary C {
        get => instance;
    }

    private Dictionary<string, FeatureConfig> resourcePiles = new Dictionary<string, FeatureConfig>();

    void Awake() {
        if (instance == null) instance = this;
        foreach (FieldInfo field in this.GetType().GetFields())
            if (field.GetValue(this) is FeatureConfig feature)
                feature.type = field.Name;
        foreach (FieldInfo field in this.GetType().GetFields())
            if (field.GetValue(this) is FeatureConfig feature && feature.isResourcePile) {
                Debug.Log("Feature " + feature.type + " is a pile of " + feature.resourceName);
                resourcePiles.Add(feature.resourceName, feature);
        }
    }
    public FeatureConfig ByTypeName(string type) {
        return (FeatureConfig)this.GetType().GetField(type).GetValue(this);
    }
    public bool ResourceHasPile(string resource, out FeatureConfig feature)
        => resourcePiles.TryGetValue(resource, out feature);
    public Color? ResourceColor(string resource)
        => ResourceHasPile(resource, out FeatureConfig feature) ? feature.resourceColor : (Color?)null;
    public bool FeatureTransformsAfterAttack(Feature feature, out FeatureConfig newFeature)
        => resourcePiles.TryGetValue(feature.config.resourceName, out newFeature) && newFeature != feature.config;

    public GameObject renderPrefab;
    public GameObject defaultRenderPrefab;
    public FeatureConfig fountain;
    public FeatureConfig windmill;
    public FeatureConfig boat;
    public FeatureConfig jasmine;
    public FeatureConfig carrot;
    public FeatureConfig arrowPile;
    public FeatureConfig woodPile;
    public FeatureConfig sprout;
    public FeatureConfig skeleton;
    public FeatureConfig fertilizer;
}
