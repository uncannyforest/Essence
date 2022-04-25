using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter : MonoBehaviour {
    public Terrain terrain;

    private Transform pointOfView;

    private Health health;
    private Vector2Int inputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    [NonSerialized] public CharacterController movement;
    private Action<Vector2Int> VehicleInput = null;

    private float SQRT_2 = Mathf.Sqrt(2);
    public const int neighborhood = 8;

    void Start() {
        pointOfView = GetComponentInChildren<PointOfView>().transform;
        movement = GetComponent<CharacterController>();
        movement.CrossingTile += HandleCrossingTile;
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
            else movement.InDirection(Disp.FT(Vector2.zero, ((Vector2)value).normalized));
        }
    }

    public bool HandleCrossingTile(Vector2Int newTile) {
        if (terrain.Feature[newTile] is Feature feature && feature.PlayerEntered != null) {
            return feature.PlayerEntered(this);
        }
        return true;
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

    public void EnteredVehicle(Transform vehicle, Action<Vector2Int> ReceiveInput) {
        VehicleInput = ReceiveInput;
        pointOfView.parent = vehicle;
        pointOfView.localPosition = Vector3.zero;
    }

    public void ExitedVehicle() {
        VehicleInput = null;
        pointOfView.parent = transform;
        pointOfView.localPosition = Vector3.zero;
    }
}
