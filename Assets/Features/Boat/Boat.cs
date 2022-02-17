using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private bool inUse;
    public PlayerCharacter player { get; private set; }
    public CharacterController[] charactersInBoat = new CharacterController[4];
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
        if (charactersInBoat[1] == null) seat = 1;
        else if (charactersInBoat[2] == null) seat = 2;
        else if (charactersInBoat[3] == null) seat = 3;
        else return false;
        CharacterController movement = creature.OverrideControl(this);
        CharacterEnter(seat, movement);
        return true;
    }

    private IEnumerator CreatureExitE() {
        for (int i = 1; i < 4; i++) {
            CharacterController creature = charactersInBoat[i];
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
        charactersInBoat[seat] = movement;
    }

    private void CharacterExit(int seat) {
        CharacterController movement = charactersInBoat[seat];
        movement.rigidbody.simulated = true;
        movement.spriteSorter.Enable();
        movement.transform.parent = terrain.transform;
        movement.rigidbody.position = exitLocation;
        movement.Sitting(false);
        charactersInBoat[seat] = null;
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
        }

        currentShoreCorrection = shoreCorrection;
    }

    private void HandleCrossedTile(Vector2Int tile) {
        if ((terrain.GetLand(tile) ?? terrain.Depths) != Land.Water) {
            HandlePlayerExited(transform.position);
            movement.InDirection(currentShoreCorrection).Idle();
            inputVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
        } else {
            currentTile = tile;
        }
    }

}
