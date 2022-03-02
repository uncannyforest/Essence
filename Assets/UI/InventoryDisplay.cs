using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDisplay : MonoBehaviour {
    public Inventory inventory;
    public int columns = 5;
    public int rows = 2;
    public float offset = 2f;
    public Vector3 localScale = new Vector3(16, 16, 1);
    public Material.Type material1;
    public Material.Type material2;

    private int count1 = 0;
    private int count2 = 0;

    void Start() {
        inventory.itemsAddedEventHandler += AddItems;
        inventory.itemsRetrievedEventHandler += RemoveItems;
        inventory.itemsClearedEventHandler += RemoveAllItems;
    }

    public void AddItems(Material.Type material, int number) {
        int indexPosition = transform.childCount;
        for (int i = 0; i < number; i++) {
            if (material == material1) {
                indexPosition = count1;
                count1++;
            }
            else if (material == material2) {
                indexPosition = count1 + count2;
                count2++;
            }
            else return;
            GameObject newSprite = GameObject.Instantiate(CollectibleLibrary.P[material].transform.GetComponentInChildren<SpriteRenderer>().gameObject, transform);
            newSprite.layer = LayerMask.NameToLayer("UI");
            Transform newTransform = newSprite.transform;
            newTransform.localScale = localScale;
            newTransform.localPosition = PositionForIndex(indexPosition);
            newTransform.SetSiblingIndex(indexPosition);
        }
        UpdateChildrenFromIndex(indexPosition + 1);
    }

    public void RemoveItems(Material.Type material, int number) {
        int indexPosition = transform.childCount;
        for (int i = 0; i < number; i++) {
            if (material == material1) {
                count1--;
                indexPosition = count1;
            }
            else if (material == material2) {
                count2--;
                indexPosition = count1 + count2;
            }
            else return;
            GameObject.Destroy(transform.GetChild(indexPosition).gameObject);
        }
        UpdateChildrenFromIndex(indexPosition);
    }

    public void RemoveAllItems() {
        foreach (Transform child in transform) {
            GameObject.Destroy(child.gameObject);
        }
        count1 = 0;
        count2 = 0;
    }

    public void UpdateChildrenFromIndex(int indexPosition) {
        for (int i = indexPosition; i < transform.childCount; i++) {
            transform.GetChild(i).localPosition = PositionForIndex(i);
        }
    }

    // Pivot point of collectibles is bottom center, so add (offset / 2f, 0)
    public Vector2 PositionForIndex(int i) =>
        new Vector2(i / rows * offset + offset / 2f, i % rows * offset);
}
