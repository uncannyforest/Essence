using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour {
    private const int z = 1;

    public Material.Type material;
    public int quantity;

    public static Collectible Instantiate(Collectible prefab, Transform parent, Vector2 location, int quantity) {
        Collectible collectible = Instantiate<Collectible>(prefab, location.WithZ(z), Quaternion.identity, parent);
        collectible.quantity = quantity;
        return collectible;
    }

    public static Collectible Instantiate(Collectible prefab, Material material, Transform parent, Vector2 location) {
        Collectible collectible = Instantiate<Collectible>(prefab, location.WithZ(z), Quaternion.identity, parent);
        collectible.material = material.MaterialType;
        collectible.quantity = material.Quantity;
        return collectible;
    }

    void OnTriggerEnter2D(Collider2D other) {
        Inventory inventory = other.GetComponent<Inventory>();

        if (inventory != null && !inventory.materials[material].IsFull) {
            inventory.Add(material, quantity, GetComponentInChildren<SpriteRenderer>().sprite);
            Destroy(gameObject);
        }

    }
}
