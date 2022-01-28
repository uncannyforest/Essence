using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Health))]
public class PlayerCharacter : MonoBehaviour {
    public Terrain terrain;
    public float defaultSpeed = 3f;

    private Health health;
    private Vector2Int inputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    private Vector2Int currentTile = Vector2Int.zero;
    private CharacterController movement;

    private float SQRT_2 = Mathf.Sqrt(2);

    public Action<Vector2Int> CrossedTile;

    public const int neighborhood = 8;

    void Start() {
        movement = new CharacterController(this).WithSnap();
        health = GetComponent<Health>();
        health.ReachedZero += HandleDeath;
    }

    public WorldInteraction Interaction { // Implmentation will change when I add multiplayer
        get => GameObject.FindObjectOfType<WorldInteraction>();
    }

    // input x and y are from {-1, 0, 1}: 9 possibilities
    public Vector2Int InputVelocity {
        get => inputVelocity;
        set {
            inputVelocity = value;
            if (value == Vector2Int.zero) movement.Idle();
            else movement.Toward((Vector2)value * defaultSpeed / value.magnitude);
        }
    }

    public void FixedUpdate() {
        if (movement.FixedUpdate() is Vector2 newPos) {
            Vector2Int oldTile = currentTile;
            currentTile = terrain.CellAt(newPos);
            if (oldTile != currentTile && CrossedTile != null) CrossedTile(currentTile);
        }
    }

    public void HandleDeath() {
        health.Reset();
        Fountain[] allSpawnPoints = GameObject.FindObjectsOfType<Fountain>();
        Fountain[] teamSpawnPoints =
            (from point in allSpawnPoints
            where point.Team == GetComponent<Team>().TeamId
            select point).ToArray<Fountain>();
        int randomIndex = Random.Range(0, teamSpawnPoints.Length);
        Debug.Log(allSpawnPoints.Length + " " + teamSpawnPoints.Length + " " + randomIndex);
        transform.position = teamSpawnPoints[randomIndex].transform.position;
    }
}
