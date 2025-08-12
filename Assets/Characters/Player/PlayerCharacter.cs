using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Anthopoid))]
public class PlayerCharacter : MonoBehaviour {
    private Transform pointOfView;

    private Vector2Int inputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    [NonSerialized] public CharacterController movement;
    private Action<Vector2Int> VehicleInput = null;

    public const int neighborhood = 8;

    void Start() {
        movement = GetComponent<CharacterController>();
        pointOfView = GetComponentInChildren<PointOfView>().transform;
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
