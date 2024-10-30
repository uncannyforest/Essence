using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Feature))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody2D))]
public class Boat : MonoBehaviour {
    public float acceleration = 4;
    public float minSpeed = 1/30f;
    public float shorePush = .5f;
    public float shorePushNoZone = .2f;
    public float creatureDeboardDelay = .5f;
    public Transform seats;

    private Terrain terrain;
    private Transform worldBag;
    private Feature feature;
    [NonSerialized] public CharacterController movement;

    private bool inUse;
    public PlayerCharacter player { get; private set; }
    private CharacterController[] passengers = new CharacterController[4];
    private Cardboard[] seatedCardboards = new Cardboard[0];
    private Vector2 inputVelocity = Vector2.zero;
    private Vector2 currentVelocity = Vector2.zero;
    private Vector2Int currentTile;
    private Vector2 currentShoreCorrection;
    private TaskRunner CreatureExits;
    private Vector2? exitLocation;

    void Start() {
        terrain = Terrain.I;
        worldBag = FindObjectOfType<Fauna>().transform;
        movement = GetComponent<CharacterController>();
        movement.CrossingTile += HandleCrossingTile;
        feature = GetComponent<Feature>();
        feature.PlayerEntered += HandlePlayerEntered;
        CreatureExits = new TaskRunner(CreatureExitE, this);
    }

    bool HandlePlayerEntered(PlayerCharacter player) {
        this.player = player;
        inUse = true;
        movement.rigidbody.bodyType = RigidbodyType2D.Dynamic;
        feature.Uninstall();
        CharacterEnter(0, player.movement);
        player.EnteredVehicle(transform, SetInputVelocity);
        CreatureExits.Stop();
        return false;
    }

    private void HandlePlayerExited(Vector2 location) {
        inUse = false;
        movement.rigidbody.bodyType = RigidbodyType2D.Static;
        exitLocation = location * 2 - terrain.CellCenterAt(transform.position);
        terrain.Feature[currentTile] = feature;
        movement.InDirection((Displacement)currentShoreCorrection).Idle();
        FaceDirection((Displacement)currentShoreCorrection);
        Debug.DrawLine(location, (Vector2)exitLocation, Color.magenta, 5);
        CharacterExit(0);
        player.ExitedVehicle();
        CreatureExits.Start();
        player = null;
    }

    public bool RequestCreatureEnter(Creature creature) {
        int seat;
        if (passengers[1] == null) seat = 1;
        else if (passengers[2] == null) seat = 2;
        else if (passengers[3] == null) seat = 3;
        else return false;
        CharacterController creatureMovement = creature.OverrideControl(this);
        Physics2D.IgnoreCollision(movement.collider, creatureMovement.collider);
        CharacterEnter(seat, creatureMovement);
        return true;
    }

    private void BootCreaturesImmediately() {
        for (int i = 1; i < 4; i++) {
            CharacterController creature = passengers[i];
            if (creature == null) continue;
            CharacterExit(i);
            creature.transform.GetComponentStrict<Creature>().ReleaseControl();
        }
    }

    private IEnumerator CreatureExitE() {
        for (int i = 1; i < 4; i++) {
            CharacterController creature = passengers[i];
            if (creature == null) continue;
            yield return new WaitForSeconds(creatureDeboardDelay);
            Physics2D.IgnoreCollision(movement.collider, creature.collider, false);
            CharacterExit(i);
            creature.transform.GetComponentStrict<Creature>().ReleaseControl();
        }
    }

    private void CharacterEnter(int seat, CharacterController movement) {
        movement.rigidbody.bodyType = RigidbodyType2D.Kinematic;
        movement.transform.parent = seats.GetChild(seat);
        movement.transform.localPosition = Vector2.zero;
        movement.Idle().Sitting(true);
        if (movement.HasComponent(out Health h)) {
            if (movement.HasComponent(out PlayerCharacter pc)) {
                h.ReachedZero += HandlePlayerDied;
            } else {
                h.Changing += HandleNonPlayerHit;
            }
        }
        seatedCardboards = seats.GetComponentsInChildren<Cardboard>();
        passengers[seat] = movement;
    }

    private void CharacterExit(int seat) {
        CharacterController movement = passengers[seat];
        movement.transform.parent = worldBag;
        if (exitLocation is Vector2 actualExitLocation) {
            Debug.DrawLine(movement.transform.position, actualExitLocation, Color.red, 5);
            movement.transform.position = actualExitLocation;
        }
        movement.rigidbody.bodyType = RigidbodyType2D.Dynamic;
        movement.Sitting(false);
        if (movement.HasComponent(out Health h)) {
            if (movement.HasComponent(out PlayerCharacter pc)) {
                h.ReachedZero -= HandlePlayerDied;
            } else {
                h.Changing -= HandleNonPlayerHit;
            }
        }
        seatedCardboards = seats.GetComponentsInChildren<Cardboard>();
        passengers[seat] = null;
    }

    void SetInputVelocity(Vector2Int inputVelocity) {
        this.inputVelocity = ((Vector2)inputVelocity).normalized;
    }

    void FixedUpdate() {
        if (!inUse) return;

        Vector2 shoreCorrection = terrain.mapRenderer.ShoreSlope(transform.position, shorePushNoZone) * shorePush;
        Vector2 expectedVelocity = inputVelocity + shoreCorrection;

        if (currentVelocity != expectedVelocity) {
            currentVelocity = Vector2.MoveTowards(currentVelocity, expectedVelocity, acceleration * Time.fixedDeltaTime);
            if (currentVelocity.magnitude < minSpeed) {
                currentVelocity = Vector2.MoveTowards(currentVelocity, expectedVelocity, minSpeed);
            }
            movement.SetRelativeVelocity((Displacement)currentVelocity);
            if (currentVelocity != Vector2.zero) FaceDirection((Displacement)currentVelocity);
        }

        currentShoreCorrection = shoreCorrection;
    }

    public IEnumerable<CharacterController> GetPassengers() {
        for (int i = 0; i < 4; i++)
            if (passengers[i] != null)
                yield return passengers[i];
    }

    public bool HandleCrossingTile(Vector2Int tile) {
        if ((terrain.GetLand(tile) ?? terrain.Depths) != Land.Water) {
            HandlePlayerExited(transform.position);
            inputVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
            return false;
        } else {
            currentTile = tile;
            foreach (CharacterController passenger in GetPassengers())
                passenger.CrossingTile?.Invoke(tile);
            return true;
        }
    }

    private void FaceDirection(Displacement direction) {
        // Update passenger cardboards
        foreach (Cardboard cardboard in seatedCardboards)
            cardboard.Orient();
        // Update passenger controllers
        foreach (CharacterController passenger in GetPassengers())
            passenger.InDirection(direction).Idle();
    }

    private bool HandleNonPlayerHit(int decrease) {
        if (player != null) player.GetComponentStrict<Health>().DecreaseWithoutBlame(-decrease);
        return false;
    }

    private void HandlePlayerDied() {
        exitLocation = null;
        CharacterExit(0);
        player.ExitedVehicle();
        BootCreaturesImmediately();
        Destroy(gameObject);
    }
}
