using System;
using UnityEngine;

[RequireComponent(typeof(Team))]
public class Health : MonoBehaviour {
    public int maxHealth;
    public Collectible itemDrop;
    public int itemDropSize = 0;
    public GameObject statBarPrefab;
    public Collectible collectiblePrefab;

    private int level;
    private StatBar healthBar;
    private Transform grid;

    public Action Died;

    public int Level {
        get => level;
    }
    
    void Awake() {
        level = maxHealth;
        healthBar = StatBar.Instantiate(statBarPrefab, this, new Color(.8f, 0, .2f));
        Died += HandleDeath;
        grid = GameObject.FindObjectOfType<Grid>().transform;
    }

    public void Reset() {
        level = maxHealth;
        healthBar.SetPercentWithoutVisibility((float) level / maxHealth);
    }

    public bool IsFull() {
        return level == maxHealth;
    }

    public void Decrease(int quantity, Transform blame) {
        level -= quantity;
        if (level > 0) {
            healthBar.SetPercent((float) level / maxHealth);
            GetComponent<Team>()?.OnAttack(blame);
        }
        else Died();
    }

    public void Increase(int quantity) {
        level = Math.Min(maxHealth, level + quantity);
        healthBar.SetPercent((float) level / maxHealth);
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

    void HideStatBar() {
        healthBar.Hide();
    }
}
