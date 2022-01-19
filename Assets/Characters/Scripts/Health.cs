using System;
using UnityEngine;

[RequireComponent(typeof(Team))]
public class Health : StatusQuantity {
    public Collectible itemDrop;
    public int itemDropSize = 0;
    public Collectible collectiblePrefab;

    private Transform grid;
    
    override protected void Awake() {
        base.Awake();
        ReachedZero += HandleDeath;
        grid = GameObject.FindObjectOfType<Grid>().transform;
    }

    public void Decrease(int quantity, Transform blame) {
        GetComponent<Team>()?.OnAttack(blame);
        Decrease(quantity);
    }

    public void HandleDeath() {
        if (itemDrop != null) Collectible.Instantiate(itemDrop, grid, transform.position, itemDropSize);
        Inventory inventory = GetComponent<Inventory>();
        if (inventory != null) {
            foreach (Material.Type type in Enum.GetValues(typeof(Material.Type))) {
                if (inventory.materials[type].Quantity > 0) {
                    Collectible item = Collectible.Instantiate(itemDrop, inventory.materials[type], grid, transform.position);
                    switch (type) {
                        case (Material.Type.Scale):
                            item.GetComponentInChildren<SpriteRenderer>().sprite = inventory.scale;
                            break;
                        case (Material.Type.Wood):
                            item.GetComponentInChildren<SpriteRenderer>().sprite = inventory.wood;
                            break;
                        case (Material.Type.Soil):
                            item.GetComponentInChildren<SpriteRenderer>().sprite = inventory.soil;
                            break;
                    }
                }
            }
            inventory.Clear();
        }
    }
}
