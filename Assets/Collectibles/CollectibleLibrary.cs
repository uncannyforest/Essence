using System;
using UnityEngine;

public class CollectibleLibrary : MonoBehaviour {
    private static CollectibleLibrary instance;
    void Awake() { if (instance == null) instance = this; }

    public static Config C { get => instance.config; }
    [SerializeField] private Config config;
    [Serializable] public class Config {
        public float collectAnimationTime = .125f;
        public float collectAnimationDistance = 2;
    }

    public static Prefabs P { get => instance.prefabs; }
    [SerializeField] private Prefabs prefabs;
    [Serializable] public class Prefabs {
        public Collectible arrow;
        public Collectible wood;
        public Collectible soil;
        public Collectible scale;
        public Collectible gemstone;

        public Collectible this[Material.Type key] {
            get {
                switch (key) {
                    case Material.Type.Arrow: return arrow;
                    case Material.Type.Wood: return wood;
                    case Material.Type.Soil: return soil;
                    case Material.Type.Scale: return scale;
                    case Material.Type.Gemstone: return gemstone;
                    default: throw new ArgumentException("No prefab for " + key);
                }
            }
        }
    }
}
