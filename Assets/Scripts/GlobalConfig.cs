using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalConfig : MonoBehaviour {

    private static GlobalConfig instance;
    void Awake() {
        instance = this;
        mapRenderer = GetComponent<MapRenderer3D>();
    }
    public static GlobalConfig I {
        get => instance;
    }
    
    private MapRenderer3D mapRenderer;
    public Elevation elevation { get => mapRenderer.elevation; }
    [Serializable] public class Elevation {
        public float groundLevelHighlight;
        public float collectibles;
        public float features;
    }
}
