using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Feature))]
[RequireComponent(typeof(Rigidbody2D))]
public class Boat : MonoBehaviour {
    public float speed = 2;
    public float acceleration = 4;
    public float minSpeed = 1/30f;
    public float shorePush = .5f;
    public float shorePushNoZone = .2f;
    public float creatureDeboardDelay = .5f;

    private Terrain terrain;
    private Feature feature;
    public CharacterController movement;
    private SortingGroup[] seatSorters = new SortingGroup[4];
    private int[][] seatSortings = new int[][] {
        new int[] {2, 3, 1, 0},
        new int[] {3, 2, 0, 1},
        new int[] {3, 1, 0, 2},
        new int[] {2, 0, 1, 3},
        new int[] {1, 0, 2, 3},
        new int[] {0, 1, 3, 2},
        new int[] {0, 2, 3, 1},
        new int[] {1, 3, 2, 0},
    };

    private bool inUse;
    public PlayerCharacter player { get; private set; }
    public CharacterController[] passengers = new CharacterController[4];
    private Vector2 inputVelocity = Vector2.zero;
    private Vector2 currentVelocity = Vector2.zero;
    private Vector2Int currentTile;
    private Vector2 currentShoreCorrection;
    private CoroutineWrapper CreatureExits;
    private Vector2 exitLocation;

    void Start() {
        terrain = Terrain.I;
        movement = new CharacterController(this)
            .SettingAnimatorDirectionDirectly()
            .WithCrossedTileHandler(HandleCrossedTile);
        feature = GetComponent<Feature>();
        feature.PlayerEntered += HandlePlayerEntered;
        CreatureExits = new CoroutineWrapper(CreatureExitE, this);
        Transform seats = transform.Find("Seats");
        for (int i = 0; i < 4; i++) 
            seatSorters[i] = seats.GetChild(i).GetComponentStrict<SortingGroup>();

    }

    void HandlePlayerEntered(PlayerCharacter player) {
        this.player = player;
        inUse = true;
        CharacterEnter(0, player.movement);
        player.EnteredVehicle(SetInputVelocity);
        feature.Uninstall();
        CreatureExits.Stop();
    }

    private void HandlePlayerExited(Vector2 location) {
        inUse = false;
        exitLocation = location;
        CharacterExit(0);
        player.ExitedVehicle();
        terrain.Feature[currentTile] = feature;
        CreatureExits.Start();
    }

    public bool RequestCreatureEnter(Creature creature) {
        int seat;
        if (passengers[1] == null) seat = 1;
        else if (passengers[2] == null) seat = 2;
        else if (passengers[3] == null) seat = 3;
        else return false;
        CharacterController movement = creature.OverrideControl(this);
        CharacterEnter(seat, movement);
        return true;
    }

    private IEnumerator CreatureExitE() {
        for (int i = 1; i < 4; i++) {
            CharacterController creature = passengers[i];
            if (creature == null) continue;
            yield return new WaitForSeconds(creatureDeboardDelay);
            CharacterExit(i);
            creature.transform.GetComponentStrict<Creature>().ReleaseControl();
        }
    }

    private void CharacterEnter(int seat, CharacterController movement) {
        movement.rigidbody.simulated = false;
        movement.spriteSorter.Disable();
        movement.transform.parent = transform.Find("Seats").GetChild(seat);
        movement.transform.localPosition = Vector2.zero;
        movement.Idle().Sitting(true);
        passengers[seat] = movement;
    }

    private void CharacterExit(int seat) {
        CharacterController movement = passengers[seat];
        movement.rigidbody.simulated = true;
        movement.spriteSorter.Enable();
        movement.transform.parent = terrain.transform;
        movement.rigidbody.position = exitLocation;
        movement.Sitting(false);
        passengers[seat] = null;
    }

    void SetInputVelocity(Vector2Int inputVelocity) {
        this.inputVelocity = speed * ((Vector2)inputVelocity).normalized;
    }

    void FixedUpdate() {
        if (!inUse) return;

        Vector2 shoreCorrection = Vector2.zero;
        Land?[] nearTiles = terrain.GetFourLandTilesAround(transform.position);
        Vector2 sub = terrain.PositionInCell(transform.position);
        if (sub.magnitude < shorePushNoZone) sub = Vector2.zero;
        if ((nearTiles[0] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(0, shorePush * (Mathf.Abs(sub.x) - sub.y));
        if ((nearTiles[1] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(shorePush * (- sub.x - Mathf.Abs(sub.y)), 0);
        if ((nearTiles[2] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(shorePush * (- sub.x + Mathf.Abs(sub.y)), 0);
        if ((nearTiles[3] ?? terrain.Depths) != Land.Water) shoreCorrection += new Vector2(0, shorePush * (-Mathf.Abs(sub.x) - sub.y));
        Vector2 expectedVelocity = inputVelocity + shoreCorrection;

        if (currentVelocity != expectedVelocity) {
            currentVelocity = Vector2.MoveTowards(currentVelocity, expectedVelocity, acceleration * Time.fixedDeltaTime);
            if (currentVelocity.magnitude < minSpeed) {
                currentVelocity = Vector2.MoveTowards(currentVelocity, expectedVelocity, minSpeed);
            }
            movement.SetVelocity(currentVelocity);
            if (currentVelocity != Vector2.zero) FaceDirection(currentVelocity);
        }

        currentShoreCorrection = shoreCorrection;
    }

    private void HandleCrossedTile(Vector2Int tile) {
        if ((terrain.GetLand(tile) ?? terrain.Depths) != Land.Water) {
            HandlePlayerExited(transform.position);
            movement.InDirection(currentShoreCorrection).Idle();
            FaceDirection(currentShoreCorrection);
            inputVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
        } else {
            currentTile = tile;
        }
    }

    private void FaceDirection(Vector2 direction) {
        // Update passenger controllers
        for (int i = 0; i < 4; i++) if (passengers[i] != null)
            passengers[i].InDirection(direction).Idle();

        // Sort seat sprites: these properties are not available to the Animator
        int eightDirectionIndex = Mathf.FloorToInt((Vector2.SignedAngle(Vector3.right, direction) + 360) % 360 / 45);
        for (int i = 0; i < 4; i++)
            seatSorters[i].sortingOrder = seatSortings[eightDirectionIndex][i];
    }
}
