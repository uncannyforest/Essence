using System;
using System.Collections.Generic;
using UnityEngine;

public class Health : StatusQuantity {
    public Collectible itemDrop;
    public int itemDropSize = 0;
    public float damageVisual1Time = .1f;
    public float damageVisual2Time = .4f;

    private Transform grid;

    private bool damageVisual = false;
    private float damageVisualStartTime;
    public List<SpriteRenderer> ColorableSprites = new List<SpriteRenderer>();
    
    override protected void Awake() {
        base.Awake();
        ReachedZero += HandleDeath;
        grid = Terrain.I.transform;
        ColorableSprites = GetColorableSprites(this);
    }

    public void Decrease(int quantity, Transform blame) {
        float fraction = (float) quantity / max;
        float actualFraction = 1 - 1 / (fraction + 1f); // Approaches fraction as x approaches 0, but never surpasses 1
        int actualQuantity = Mathf.FloorToInt(actualFraction * max);
        if (!Decrease(actualQuantity)) return;
        GetComponent<Team>()?.OnAttack(blame);
        if (blame != null) blame.GetComponent<Creature>()?.AttackSucceeded(IsZero() ? max : (int?)null);
        ResetDamageVisual();
    }

    public void DecreaseWithoutBlame(int quantity) {
        if (!Decrease(quantity)) return;
        ResetDamageVisual();
    }

    public void HandleDeath() {
        if (itemDrop != null) Collectible.Instantiate(itemDrop, grid, transform.position, itemDropSize);
        Inventory inventory = GetComponent<Inventory>();
        if (inventory != null) {
            foreach (Material.Type type in Enum.GetValues(typeof(Material.Type)))
                if (inventory.materials[type].Quantity > 0)
                    Collectible.Instantiate(inventory.materials[type], transform.position);
            inventory.Clear();
        }
    }

    // This code is getting increasingly spaghettified because it is on its way out with the transition to 3D.
    private static List<SpriteRenderer> GetColorableSprites(MonoBehaviour mb) {
        List<SpriteRenderer> colorableSprites = new List<SpriteRenderer>();
        SpriteRenderer[] allSprites = mb.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allSprites)
            if (sprite.color == Color.white)
                colorableSprites.Add(sprite);
        return colorableSprites;
    }

    public void ResetDamageVisual() {
        if (level <= 0) {
            damageVisual = false;
            return;
        }
        damageVisual = true;
        foreach (SpriteRenderer sprite in ColorableSprites) {
            sprite.material = GeneralAssetLibrary.P.spriteSolidColor;
            sprite.color = Color.red;
        }
        damageVisualStartTime = Time.time;
    }

    void Update() {
        if (damageVisual) {
            float timeSinceDamage = Time.time - damageVisualStartTime;
            if (timeSinceDamage >= damageVisual1Time) {
                timeSinceDamage -= damageVisual1Time;
                float fractionElapsed = Mathf.Clamp01(timeSinceDamage / damageVisual2Time);
                foreach (SpriteRenderer sprite in ColorableSprites) {
                    if (sprite != null) {
                        sprite.material = GeneralAssetLibrary.P.spriteDefault;
                        sprite.color = new Color(1, fractionElapsed, fractionElapsed);
                    }
                }
                if (fractionElapsed >= 1f)
                    damageVisual = false;
            }
        }
    }

    override protected int? GetMaxFromStats(Stats stats) => stats.Def;
}
