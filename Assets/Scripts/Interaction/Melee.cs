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

    private Vector3 direction = Vector3.right;

    public Vector2 InputVelocity {
        get => direction;
        set {
            if (value != Vector2.zero) direction = value.normalized;
        }
    }

    public Melee(Config config, Transform transform) {
        this.config = config;
        this.transform = transform;
    }

    public List<Health> Damage(int safeTeam) {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position + direction * config.damageCenterDistance,
            config.damageRadius, LayerMask.GetMask("Player", "HealthCreature"));
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
}
