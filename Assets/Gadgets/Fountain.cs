using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Fountain : MonoBehaviour {
    public float timeToCapture = 5f;
    public float timeToReset = 1f;

    private int team = 0;
    private Health health;
    private bool enemyPresent;
    new private Collider2D collider;
    private Collider2D enemy;

    public int Team {
        get => team;
        set {
            team = value;
            if (value == 0) GetComponentInChildren<SpriteRenderer>().color = Color.gray;
            else GetComponentInChildren<SpriteRenderer>().color = Color.white;
        }
    }

    void Start() {
        collider = GetComponent<Collider2D>();
        health = GetComponent<Health>();
        health.ReachedZero += HandleDeath;
        if (team != 0) GameObject.FindObjectOfType<PlayerCharacter>().HandleDeath();
    }

    void OnTriggerEnter2D(Collider2D other) {
        PlayerCharacter target = other.GetComponent<PlayerCharacter>();
        if (target == null) return;
        int playerTeam = target.GetComponentStrict<Team>().TeamId;
        if (playerTeam == team) return;
        enemyPresent = true;
        enemy = other;
    }

    void FixedUpdate() {
        if (enemyPresent) {
            if (collider.IsTouching(enemy))
                health.Decrease((int)(health.max * Time.fixedDeltaTime / timeToCapture), enemy.transform);
            else enemyPresent = false;
        } else if (!health.IsFull()) {
            health.Increase((int)(health.max * Time.fixedDeltaTime / timeToReset));
        }
    }

    void HandleDeath() {
        Team = enemy.GetComponentStrict<Team>().TeamId;
        enemyPresent = false;
    }
}
