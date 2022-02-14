using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Feature))]
[RequireComponent(typeof(Health))]
public class Fountain : MonoBehaviour {
    public float timeToCapture = 5f;
    public float timeToReset = 1f;

    private Feature feature;
    private Terrain terrain;

    private int team = 0;
    private Health health;
    private bool enemyPresent;
    new private Collider2D collider;
    private Transform enemy;

    public int Team {
        get => team;
        set {
            team = value;
            if (value == 0) GetComponentInChildren<SpriteRenderer>().color = Color.gray;
            else GetComponentInChildren<SpriteRenderer>().color = Color.white;
        }
    }

    void Start() {
        feature = GetComponent<Feature>();
        feature.PlayerEntered += HandlePlayerEntered;
        terrain = GameObject.FindObjectOfType<Terrain>();
        GetComponent<Feature>().PlayerEntered += HandlePlayerEntered;
        collider = GetComponent<Collider2D>();
        health = GetComponent<Health>();
        health.ReachedZero += HandleDeath;
        if (team != 0) GameObject.FindObjectOfType<PlayerCharacter>().HandleDeath();
    }

    void HandlePlayerEntered(PlayerCharacter target) {
        int playerTeam = target.GetComponentStrict<Team>().TeamId;
        if (playerTeam == team) return;
        enemyPresent = true;
        enemy = target.transform;
    }

    void FixedUpdate() {
        if (enemyPresent) {
            Debug.Log(terrain.CellAt(transform.position) + " and " + terrain.CellAt(enemy.position));
            if (feature.tile == terrain.CellAt(enemy.position))
                health.Decrease((int)(health.max * Time.fixedDeltaTime / timeToCapture), enemy);
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
