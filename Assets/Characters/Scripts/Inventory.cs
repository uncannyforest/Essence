using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {
    public int maxLures = 20;
    public int maxTools = 60;

    public Sprite scale;
    public Sprite wood;
    public Sprite soil;

    public Dictionary<Material.Type, Material> materials = new Dictionary<Material.Type, Material>();
    public Action<Material.Type, int, Sprite> itemsAddedEventHandler;
    public Action<Material.Type, int> itemsRetrievedEventHandler;
    public Action itemsClearedEventHandler;

    public void Awake() {
        materials[Material.Type.Scale] = new Material(Material.Type.Scale, maxLures);
        materials[Material.Type.PondApple] = new Material(Material.Type.PondApple, maxLures);
        materials[Material.Type.ForestBlossom] = new Material(Material.Type.ForestBlossom, maxLures);
        materials[Material.Type.Glass] = new Material(Material.Type.Glass, maxLures);
        materials[Material.Type.Huckleberry] = new Material(Material.Type.Huckleberry, maxLures);
        materials[Material.Type.Gemstone] = new Material(Material.Type.Gemstone, maxLures);
        materials[Material.Type.Sword] = new Material(Material.Type.Sword, maxTools);
        materials[Material.Type.Arrow] = new Material(Material.Type.Arrow, maxTools);
        materials[Material.Type.Praxel] = new Material(Material.Type.Praxel, maxTools);
        materials[Material.Type.Wood] = new Material(Material.Type.Wood, maxTools);
        materials[Material.Type.Stone] = new Material(Material.Type.Stone, maxTools);
        materials[Material.Type.Soil] = new Material(Material.Type.Soil, maxTools);
    }

    public int Add(Material.Type material, int quantity, Sprite sprite) {
        int added = materials[material].ChangeQuantity(quantity);
        if (itemsAddedEventHandler != null) itemsAddedEventHandler(material, added, sprite);
        return added;
    }

    public bool Retrieve(Material.Type type, int quantity) {
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
