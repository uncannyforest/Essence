using System;
using System.Collections.Generic;
using UnityEngine;

public class Health : StatusQuantity {
    public Collectible itemDrop;
    public int itemDropSize = 0;
    public float damageVisual1Time = .1f;
    public float damageVisual2Time = .4f;

    private Transform grid;

    private float damageVisualStartTime;
    private List<SpriteRenderer> damageVisualSprites = new List<SpriteRenderer>();
    
    override protected void Awake() {
        base.Awake();
        ReachedZero += HandleDeath;
        grid = Terrain.I.transform;
    }

    public void Decrease(int quantity, Transform blame) {
        if (!Decrease(quantity)) return;
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
    public static List<SpriteRenderer> GetColorableSprites(MonoBehaviour mb) {
        Health health = mb.GetComponent<Health>();
        if (health != null && health.damageVisualSprites.Count > 0) {
            return health.damageVisualSprites;
        }
        List<SpriteRenderer> colorableSprites = new List<SpriteRenderer>();
        SpriteRenderer[] allSprites = mb.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allSprites)
            if (sprite.color == Color.white || sprite.color == Color.grey)
                colorableSprites.Add(sprite);
        return colorableSprites;
    }

    public void ResetDamageVisual() {
        if (level <= 0) {
            damageVisualSprites.Clear();
            return;
        }
        if (damageVisualSprites.Count == 0) {
            damageVisualSprites = GetColorableSprites(this);
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
                    if (sprite != null) {
                        sprite.material = GeneralAssetLibrary.P.spriteDefault;
                        sprite.color = new Color(1, fractionElapsed, fractionElapsed);
                    }
                }
                if (fractionElapsed >= 1f)
                    damageVisualSprites.Clear();
            }
        }
    }

    override protected int? GetMaxFromStats(Stats stats) => stats.Def;
}
