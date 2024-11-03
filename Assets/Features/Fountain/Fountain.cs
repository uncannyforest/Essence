using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Feature))]
public class Fountain : MonoBehaviour {
    public float timeToCapture = 5f;
    public float timeToReset = 1f;
    public float ringMaxSize = Mathf.Sqrt(10);
    public Transform ring;

    private Feature feature;
    private Terrain terrain;

    private int team = 0;
    private float ringSize = 0;
    private int enemyPresent = 0;
    new private Collider2D collider;
    private Transform enemy;

    public int Team {
        get => team;
        set {
            team = value;
            if (value == 0) GetComponentInChildren<SpriteRenderer>().color = Color.gray;
            else GetComponentInChildren<SpriteRenderer>().color = new Color(.99f, .99f, .99f);
        }
    }

    void Start() {
        feature = GetComponent<Feature>();
        if (feature.serializedFields != null) Deserialize(feature.serializedFields);
        feature.SerializeFields += Serialize;
        feature.PlayerEntered += HandlePlayerEntered;
        terrain = GameObject.FindObjectOfType<Terrain>();
        collider = GetComponent<Collider2D>();
        // If this were in PlayerController Fountains might not be loaded yet.
        if (team != 0) GameManager.I.YourPlayer.HandleDeath();
    }

    int[] Serialize() => new int[] { team };
    void Deserialize(int[] fields) => Team = fields[0];

    bool HandlePlayerEntered(PlayerCharacter target) {
        int playerTeam = target.GetComponentStrict<Team>().TeamId;
        if (playerTeam == team) return true;
        enemyPresent = 2; // Rather than using boolean, we need an extra frame for FixedUpdate to run
        enemy = target.transform;
        return true;
    }

    void FixedUpdate() {
        if (enemyPresent > 0) {
            if (feature.tile == terrain.CellAt(enemy.position)) {
                if (!ring.gameObject.activeSelf) {
                    ring.gameObject.SetActive(true);
                    ringSize = ringMaxSize;
                } else {
                    ringSize -= ringMaxSize * Time.deltaTime / timeToCapture;
                }
                ring.localScale = Vector3.one * ringSize * ringSize;
                if (ringSize <= 0) {
                    HandleDeath();
                    ring.gameObject.SetActive(false);
                }
            } else enemyPresent--;
        } else if (ring.gameObject.activeSelf) {
            ringSize += ringMaxSize * Time.deltaTime / timeToReset;
            ring.localScale = Vector3.one * ringSize * ringSize;
            if (ringSize >= ringMaxSize) {
                ring.gameObject.SetActive(false);
            }
        }
    }

    void HandleDeath() {
        Team = enemy.GetComponentStrict<Team>().TeamId;
        enemyPresent = 0;
    }
}
