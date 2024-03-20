using System;
using System.Collections.Generic;
using UnityEngine;

public class TileLibrary3D : MonoBehaviour {
    private static TileLibrary3D instance;
    public static TileLibrary3D E {
        get => instance;
    }

    public Biome temperate;

    void Awake() {
        if (instance == null) instance = this;
    }
}
