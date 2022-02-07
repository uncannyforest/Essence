using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalConfig : MonoBehaviour {

    private static GlobalConfig instance;
    void Awake() => instance = this;
    public static GlobalConfig I {
        get => instance;
    }

    public Elevation elevation;
    [Serializable] public class Elevation {
        public float groundLevelHighlight;
        public float collectibles;
        public float features;
    }

}
