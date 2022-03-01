using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteSorter))]
public class Collectible : MonoBehaviour {
    public Material.Type material;
    public int quantity;
    public float collectAnimationTime = .5f;
    public float collectAnimationDistance = 1;

    private SpriteSorter spriteSorter;
    void Start() { spriteSorter = GetComponent<SpriteSorter>(); }

    public static Collectible Instantiate(Collectible prefab, Transform parent, Vector2 location, int quantity) {
        Collectible collectible = Instantiate<Collectible>(prefab, location.WithZ(GlobalConfig.I.elevation.collectibles), Quaternion.identity, parent);
        collectible.quantity = quantity;
        return collectible;
    }

    public static Collectible Instantiate(Collectible prefab, Material material, Transform parent, Vector2 location) {
        Collectible collectible = Instantiate<Collectible>(prefab, location.WithZ(GlobalConfig.I.elevation.collectibles), Quaternion.identity, parent);
        collectible.material = material.MaterialType;
        collectible.quantity = material.Quantity;
        return collectible;
    }

    void OnTriggerEnter2D(Collider2D other) {
        Inventory inventory = other.GetComponent<Inventory>();

        if (inventory != null && !inventory.materials[material].IsFull) {
            inventory.Add(material, quantity, GetComponentInChildren<SpriteRenderer>().sprite);
            StartCoroutine(CollectAnimation());
        }
    }

    private IEnumerator CollectAnimation() {
        float startTime = Time.time;
        float endTime = startTime + collectAnimationTime;
        float speed = collectAnimationDistance / collectAnimationTime;
        while (Time.time < endTime) {
            spriteSorter.VerticalDisplacement = speed * (Time.time - startTime);
            yield return null;
        }
        Destroy(gameObject);
    }
}
