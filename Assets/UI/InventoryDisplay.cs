using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryDisplay : MonoBehaviour {
    public Inventory inventory;
    public TMP_Text text;
    public RectTransform bar;
    
    void Start() {
        inventory = GameManager.I.YourPlayer.GetComponentStrict<Inventory>();
        inventory.itemsClearedEventHandler += RemoveAllItems;
        inventory.itemsCanBeReplacedEventHandler += AlertItemsToReplace;
        inventory.itemsUpdatedEventHandler += NewItem;
    }

    public void ChangeQuantity(float percent) => bar.anchorMax = new Vector3(percent, 1);

    public void NewItem(string resource, float percent) {
        text.text = resource;
        ChangeQuantity(percent);
    }

    public void RemoveAllItems() => NewItem("", 0);

    public void AlertItemsToReplace(string resource, int quantity) {
        TextDisplay.I.ShowMiniText(quantity + " " + resource + "available to replace");
    }
}
