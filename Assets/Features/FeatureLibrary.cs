using System;
using System.Reflection;
using UnityEngine;

public class FeatureLibrary : MonoBehaviour {
    private static FeatureLibrary instance;
    public static FeatureLibrary P {
        get => instance;
    }
    void Awake() {
        if (instance == null) instance = this;

        foreach (FieldInfo field in this.GetType().GetFields())
            if (field.GetValue(this) is Feature feature)
                feature.type = field.Name;
    }
    public Feature ByTypeName(string type) {
        return (Feature)this.GetType().GetField(type).GetValue(this);
    }

    public Feature fountain;
    public Feature windmill;
    public Feature boat;
    public Feature jasmine;
    public Feature carrot;
}
