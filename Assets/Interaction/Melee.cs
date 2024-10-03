using System;
using System.Collections.Generic;
using UnityEngine;

public class Melee {
    [Serializable] public class Config {
        [SerializeField] public float damageCenterDistance;
        [SerializeField] public float damageRadius;
        [SerializeField] public int damage;
        public Config(float damageCenterDistance, float damageRadius, int damage) {
            this.damageCenterDistance = damageCenterDistance;
            this.damageRadius = damageRadius;
            this.damage = damage;
        }
    }
    private Config config;
    private Transform transform;

    private Vector3 direction = Vector3.right; // one of eight options
    private bool keysArePressed = false; // muxes between keys and pointer

    public Vector2 InputVelocity {
        get => direction;
        set {
            keysArePressed = value != Vector2.zero;
            if (value != Vector2.zero) direction = value.normalized;
        }
    }

    public Melee(Config config, Transform transform) {
        this.config = config;
        this.transform = transform;
    }

    public Vector2 DamageCenter {
        get => transform.position + direction * config.damageCenterDistance;
    }
    public float DamageRadius {
        get => config.damageRadius;
    }

    public List<Health> Damage(int safeTeam) {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(DamageCenter,
            DamageRadius, LayerMask.GetMask("Player", "HealthCreature"));
        List<Health> healths = new List<Health>();
        foreach (Collider2D collider in colliders) {
            if (collider.GetComponentStrict<Team>().TeamId == safeTeam) continue;
            healths.Add(collider.GetComponentStrict<Health>());
        }
        Debug.Log("Damage to " + direction * config.damageCenterDistance + " at " + (transform.position + direction * config.damageCenterDistance) + "! All " + colliders.Length + " except team " + safeTeam + ". Number to damage: " + healths.Count);
        foreach (Health health in healths) {
            health.Decrease(config.damage, transform);
        }
        return healths;
    }

    public void PointerToKeys(Vector2 pointer) {
        if (keysArePressed) return;
        float? inputAngle = pointer.VelocityToDirection();
        if (inputAngle is float realAngle) {
            float nearest45 = Mathf.Round((realAngle + 360) % 360 / 45) * 45;
            direction = Vct.DirectionToVelocity(nearest45);
        }
    }
}
