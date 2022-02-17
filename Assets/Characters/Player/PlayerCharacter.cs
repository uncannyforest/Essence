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

    private float SQRT_2 = Mathf.Sqrt(2);
    public const int neighborhood = 8;

    void Start() {
        cameraTransform = GetComponentInChildren<Camera>().transform;
        movement = new CharacterController(this).WithSnap().WithCrossedTileHandler(HandleCrossedTile);
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
        VehicleInput = ReceiveInput;
        cameraTransform.parent = transform.parent.parent.parent;
    }

    public void ExitedVehicle() {
        VehicleInput = null;
        cameraTransform.parent = transform;
    }
}
