using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteSorter))]
public class Collectible : MonoBehaviour {
    public Material.Type material;
    public int quantity;

    private SpriteSorter spriteSorter;
    public void Awake() { spriteSorter = GetComponent<SpriteSorter>(); }

    public static Collectible Instantiate(Material material, Vector2 location) {
        Collectible collectible = Instantiate<Collectible>(CollectibleLibrary.P[material.MaterialType],
            location.WithZ(GlobalConfig.I.elevation.collectibles), Quaternion.identity, Terrain.I.transform);
        collectible.quantity = material.Quantity;
        return collectible;
    }

    public static Collectible Instantiate(Collectible prefab, Transform parent, Vector2 location, int quantity) {
        Collectible collectible = Instantiate<Collectible>(prefab, location.WithZ(GlobalConfig.I.elevation.collectibles), Quaternion.identity, parent);
        collectible.quantity = quantity;
        return collectible;
    }

    void OnTriggerEnter2D(Collider2D other) {
        Inventory inventory = other.GetComponent<Inventory>();
        if (inventory != null) TryCollect(inventory);
    }

    private void TryCollect(Inventory inventory) {
        if (!inventory.materials[material].IsFull) {
            inventory.Add(material, quantity);
            StartCoroutine(CollectAnimation());
        }
    }

    private IEnumerator CollectAnimation() {
        float startTime = Time.time;
        float endTime = startTime + CollectibleLibrary.C.collectAnimationTime;
        float speed = CollectibleLibrary.C.collectAnimationDistance / CollectibleLibrary.C.collectAnimationTime;
        float startY = spriteSorter.VerticalDisplacement;
        while (Time.time < endTime) {
            spriteSorter.VerticalDisplacement = startY + speed * (Time.time - startTime);
            yield return null;
        }
        Destroy(gameObject);
    }


    public static void InstantiateAndCollect(Collectible prefab,
            Transform parent, Vector2 location, float startY, int quantity, Inventory inventory) {
        Collectible collectible = Instantiate(prefab, parent, location, quantity);
        collectible.GetComponentStrict<SpriteSorter>().VerticalDisplacement = startY;
        collectible.TryCollect(inventory);
    }
}
