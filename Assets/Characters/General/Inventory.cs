using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public class Bucket {
    [SerializeField] public int max = 60;

    [SerializeField] private List<Material> materials = new List<Material>();

    public Bucket(int max) {
        this.max = max;
    }

    public int Size {
        get => (from material in materials select material.Quantity).Sum();
    }
    public int SpaceAvailable {
        get => max - Size;
    }

    public void AddMaterial(Material material) {
        materials.Add(material);
    }
}

public class Inventory : MonoBehaviour {
    public Bucket lureBucket = new Bucket(10);
    public Bucket soilBucket = new Bucket(10);
    public Bucket buildBucket = new Bucket(30);
    public Bucket arrowBucket = new Bucket(30);

    public Sprite scale;
    public Sprite wood;
    public Sprite soil;

    public Dictionary<Material.Type, Material> materials = new Dictionary<Material.Type, Material>();
    public Action<Material.Type, int> itemsAddedEventHandler;
    public Action<Material.Type, int> itemsRetrievedEventHandler;
    public Action itemsClearedEventHandler;

    public void Awake() {
        materials[Material.Type.Scale] = Material.InBucket(Material.Type.Scale, lureBucket);
        materials[Material.Type.PondApple] = Material.InBucket(Material.Type.PondApple, lureBucket);
        materials[Material.Type.ForestBlossom] = Material.InBucket(Material.Type.ForestBlossom, lureBucket);
        materials[Material.Type.Glass] = Material.InBucket(Material.Type.Glass, lureBucket);
        materials[Material.Type.Huckleberry] = Material.InBucket(Material.Type.Huckleberry, lureBucket);
        materials[Material.Type.Gemstone] = Material.InBucket(Material.Type.Gemstone, lureBucket);
        materials[Material.Type.Arrow] = Material.InBucket(Material.Type.Arrow, arrowBucket);
        materials[Material.Type.Wood] = Material.InBucket(Material.Type.Wood, buildBucket);
        materials[Material.Type.Stone] = Material.InBucket(Material.Type.Stone, buildBucket);
        materials[Material.Type.Soil] = Material.InBucket(Material.Type.Soil, soilBucket);
    }

    public int Add(Material.Type material, int quantity) {
        int added = materials[material].TryAdd(quantity);
        if (added > 0 && itemsAddedEventHandler != null) itemsAddedEventHandler(material, added);
        return added;
    }

    public bool CanRetrieve(Material.Type type, int quantity) => materials[type].Quantity >= quantity;

    public bool Retrieve(Material.Type type, int quantity) {
        return true;
        if (materials[type].TryDecrease(quantity)) {
            if (itemsRetrievedEventHandler != null) itemsRetrievedEventHandler(type, quantity);
            return true;
        }
        return false;
    }

    public void Clear() {
        foreach (Material.Type type in Enum.GetValues(typeof(Material.Type)))
            materials[type].Clear();
        if (itemsClearedEventHandler != null) itemsClearedEventHandler();
    }

}
