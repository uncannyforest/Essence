using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour {
    public Material.Type material;
    public int quantity;

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

    private bool TryCollect(Inventory inventory) {
        if (!inventory.materials[material].IsFull) {
            inventory.Add(material, quantity);
            StartCoroutine(CollectAnimation());
            return true;
        } else return false;
    }

    private IEnumerator CollectAnimation() {
        GetComponent<Collider2D>().enabled = false;
        float startTime = Time.time;
        float endTime = startTime + CollectibleLibrary.C.collectAnimationTime;
        float speed = CollectibleLibrary.C.collectAnimationDistance / CollectibleLibrary.C.collectAnimationTime;
        Vector3 startPos = transform.position;
        while (Time.time < endTime) {
            transform.position = startPos + Vector3.back * speed * (Time.time - startTime);
            yield return null;
        }
        Destroy(gameObject);
    }


    public static bool InstantiateAndCollect(Collectible prefab,
            Transform parent, Vector2 location, int quantity, Inventory inventory) {
        Collectible collectible = Instantiate(prefab, parent, location, quantity);
        if (collectible.TryCollect(inventory)) return true;
        else {
            Destroy(collectible.gameObject);
            return false; 
        }
    }
}
