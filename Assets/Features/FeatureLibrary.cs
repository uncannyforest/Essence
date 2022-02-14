using System;
using UnityEngine;

public class FeatureLibrary : MonoBehaviour {
    private static FeatureLibrary instance;
    public static FeatureLibrary P {
        get => instance;
    }
    void Awake() { if (instance == null) instance = this; }

    public Feature fountain;
    public Feature windmill;
    public Feature boat;
}
