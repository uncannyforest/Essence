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
    public int subGridUnit = 8;

    new private Rigidbody2D rigidbody;
    private Animator animator;
    private Health health;
    private Vector2Int inputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    private float timeDoneMoving = 0;
    private Vector2Int currentTile = Vector2Int.zero;
    private Vector2 animatorDirection; // last non-zero velocity
    private int animatorFrame = -1;
    private int numAnimatorFrames = 2;

    private float SQRT_2 = Mathf.Sqrt(2);
    private float FRAME_OF_FIRST_WALKING = 1; // change to 2 when new sprites

    public Action<Vector2Int> CrossedTile;

    public const int neighborhood = 8;

    void Start() {
        rigidbody = GetComponentInChildren<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        health.Died += HandleDeath;
    }

    public WorldInteraction Interaction { // Implmentation will change when I add multiplayer
        get => GameObject.FindObjectOfType<WorldInteraction>();
    }

    // input x and y are from {-1, 0, 1}: 9 possibilities
    public Vector2Int InputVelocity {
        get => inputVelocity;
        set => inputVelocity = value;
    }

    private float GetSpeed() {
        return defaultSpeed;
    }

    private Vector2 Move() {
        float speed = GetSpeed();
        timeDoneMoving = inputVelocity.magnitude / (speed * subGridUnit) + Time.fixedTime;

        animatorDirection = inputVelocity;
        animatorFrame = (animatorFrame + 1) % numAnimatorFrames;
        float animatorVelocity = (animatorFrame + FRAME_OF_FIRST_WALKING) * inputVelocity.x;
        animator.SetFloat("Blend", animatorVelocity);

        return new Vector2(
            (Mathf.Round(transform.position.x * subGridUnit) + inputVelocity.x) / subGridUnit,
            (Mathf.Round(transform.position.y * subGridUnit) + inputVelocity.y) / subGridUnit);
    }

    private void Stop() {
        animatorFrame = -1;
        animator.SetFloat("Blend", animatorDirection.x);
    }

    public void FixedUpdate() {
        if (Time.fixedTime > timeDoneMoving) {
            if (inputVelocity != Vector2Int.zero) {
                Vector2 newPos = Move();
                rigidbody.MovePosition(newPos);
                Vector2Int oldTile = currentTile;
                currentTile = terrain.CellAt(newPos);
                if (oldTile != currentTile && CrossedTile != null) CrossedTile(currentTile);
            } else if (animatorFrame != -1) {
                Stop();
            }
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
