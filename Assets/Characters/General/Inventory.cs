using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour {
    public string resource = "";
    public int quantity = 0;

    public string resourceToReplace = "";
    public int quantityToReplace = 0;

    public int Max => GetMax(resource);
    public float Percent => (float)quantity / Max;
    public int GetMax(string resource) => 100;

    public Action<string, int> itemsCanBeReplacedEventHandler;
    public Action itemsClearedEventHandler;
    public Action<string, float> itemsUpdatedEventHandler;

    public int Add(string resource, int delta) {
        if (this.resource == "") {
            this.resource = resource;
            TryAddOneQuantity(resource, ref quantity, delta);
        } if (this.resource == resource) {
            return TryAddOneQuantity(resource, ref quantity, delta);
        } else {
            resourceToReplace = resource;
            quantityToReplace = Mathf.Min(delta, GetMax(resource));
            itemsCanBeReplacedEventHandler(resource, delta);
            return 0;
        }
    }

    private int TryAddOneQuantity(string resource, ref int quantity, int delta) {
        int space = GetMax(resource) - quantity;
        if (space == 0) return 0;
        int actualDelta = Math.Min(delta, space);
        quantity += actualDelta;
        itemsUpdatedEventHandler(resource, Percent);
        ClearReplacement();
        return actualDelta;
    }

    public bool CanRetrieve(string resource, int delta) => this.resource == resource && quantity >= delta;

    public bool Retrieve(string resource, int delta) {
        if (!CanRetrieve(resource, delta)) return false;
        quantity -= delta;
        if (quantity == 0) Clear();
        else itemsUpdatedEventHandler(resource, Percent);
        ClearReplacement();
        return true;
    }

    // Call this on any modification to avoid using replacement as second inventory
    public void ClearReplacement() {
        resourceToReplace = "";
        quantityToReplace = 0;
    }

    public void Clear() {
        resource = "";
        quantity = 0;
        ClearReplacement();
        if (itemsClearedEventHandler != null) itemsClearedEventHandler();
    }

    public void ReplaceOrClearItems() {
        resource = resourceToReplace;
        quantity = quantityToReplace;
        ClearReplacement();
        if (itemsUpdatedEventHandler != null) itemsUpdatedEventHandler(resource, Percent);
    }

}
