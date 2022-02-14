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

    private Transform cameraTransform;

    private Health health;
    private Vector2Int inputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    [NonSerialized] public CharacterController movement;
    private Action<Vector2Int> VehicleInput = null;

    public Action<Vector2Int> CrossedTile;

    private float SQRT_2 = Mathf.Sqrt(2);
    public const int neighborhood = 8;

    void Start() {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        CrossedTile += HandleCrossedTile;
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
            if (VehicleInput != null) {
                VehicleInput(value);
                return;
            }
            if (value == Vector2Int.zero) movement.Idle();
            else movement.InDirection((Vector2)value * defaultSpeed / value.magnitude);
        }
    }

    public void FixedUpdate() {
        if (VehicleInput == null &&
                movement.FixedUpdateReturnTileWhenEntered() is Vector2Int tile &&
                CrossedTile != null)
            CrossedTile(tile);
    }

    public void HandleCrossedTile(Vector2Int newTile) {
        if (terrain.Feature[newTile] is Feature feature && feature.PlayerEntered != null)
            feature.PlayerEntered(this);
    }

    public void HandleDeath() {
        health.Reset();
        Fountain[] allSpawnPoints = GameObject.FindObjectsOfType<Fountain>();
        Fountain[] teamSpawnPoints =
            (from point in allSpawnPoints
            where point.Team == GetComponent<Team>().TeamId
            select point).ToArray<Fountain>();
        int randomIndex = Random.Range(0, teamSpawnPoints.Length);
        transform.position = (Vector2)(teamSpawnPoints[randomIndex].transform.position);
    }

    public void EnteredVehicle(Action<Vector2Int> ReceiveInput) {
        movement.Idle();
        VehicleInput = ReceiveInput;
        GetComponent<Rigidbody2D>().simulated = false;
        cameraTransform.parent = transform.parent.parent.parent;
        GetComponentInChildren<SpriteSorter>().Disable();
    }

    public void ExitedVehicle() {
        VehicleInput = null;
        GetComponent<Rigidbody2D>().simulated = true;
        cameraTransform.parent = transform;
        GetComponentInChildren<SpriteSorter>().Enable();
    }
}
