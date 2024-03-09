using System;
using UnityEngine;

public class TileLibrary3D : MonoBehaviour {
    public MeshRenderer grass;
    public CornerVariation meadow;
    public MeshRenderer shrub;
    public InnerVariation forest;
    public MeshRenderer water;
    public CornerVariation ditch;
    public TripleVariation dirtpile;
    public MeshRenderer woodpile;
    public InnerVariation hill;
    public MeshRenderer fence;
    public TripleVariation woodBldg;

    [Serializable] public struct CornerVariation {
        public MeshRenderer corner;
        public MeshRenderer basic;
    }
    [Serializable] public struct InnerVariation {
        public MeshRenderer basic;
        public MeshRenderer inner;
    }
    [Serializable] public struct TripleVariation {
        public MeshRenderer corner;
        public MeshRenderer basic;
        public MeshRenderer inner;
    }
}

