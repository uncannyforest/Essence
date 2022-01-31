using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Team))]
public class Health : StatusQuantity {
    public Collectible itemDrop;
    public int itemDropSize = 0;
    public Collectible collectiblePrefab;
    public float damageVisual1Time = .1f;
    public float damageVisual2Time = .4f;

    private Transform grid;

    private float damageVisualStartTime;
    private List<SpriteRenderer> damageVisualSprites = new List<SpriteRenderer>();
    
    override protected void Awake() {
        base.Awake();
        ReachedZero += HandleDeath;
        grid = GameObject.FindObjectOfType<Grid>().transform;
    }

    public void Decrease(int quantity, Transform blame) {
        GetComponent<Team>()?.OnAttack(blame);
        Decrease(quantity);
        ResetDamageVisual();
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

    public void ResetDamageVisual() {
        if (damageVisualSprites.Count == 0) {
            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sprite in allSprites) if (sprite.color == Color.white) {
                damageVisualSprites.Add(sprite);
            }
        }
        foreach (SpriteRenderer sprite in damageVisualSprites) {
            sprite.material = GeneralAssetLibrary.P.spriteSolidColor;
            sprite.color = Color.red;
        }
        damageVisualStartTime = Time.time;
    }

    void Update() {
        if (damageVisualSprites.Count > 0) {
            float timeSinceDamage = Time.time - damageVisualStartTime;
            if (timeSinceDamage >= damageVisual1Time) {
                timeSinceDamage -= damageVisual1Time;
                float fractionElapsed = Mathf.Clamp01(timeSinceDamage / damageVisual2Time);
                foreach (SpriteRenderer sprite in damageVisualSprites) {
                    sprite.material = GeneralAssetLibrary.P.spriteDefault;
                    sprite.color = new Color(1, fractionElapsed, fractionElapsed);
                }
                if (fractionElapsed >= 1f)
                    damageVisualSprites.Clear();
            }
        }
    }
}
