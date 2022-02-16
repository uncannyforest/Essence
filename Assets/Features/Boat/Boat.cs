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

    private Terrain terrain;
    private Feature feature;
    public CharacterController movement;

    private bool inUse;
    private PlayerCharacter player;
    public CharacterController[] charactersInBoat = new CharacterController[4];
    private Vector2 inputVelocity = Vector2.zero;
    private Vector2 currentVelocity = Vector2.zero;
    private Vector2Int currentTile;
    private Vector2 currentShoreCorrection;

    void Start() {
        terrain = Terrain.I;
        movement = new CharacterController(this)
            .SettingAnimatorDirectionDirectly()
            .WithCrossedTileHandler(HandleCrossedTile);
        feature = GetComponent<Feature>();
        feature.PlayerEntered += HandlePlayerEntered;
    }

    void HandlePlayerEntered(PlayerCharacter player) {
        this.player = player;
        inUse = true;
        player.transform.parent = transform.Find("Seats").GetChild(0);
        player.transform.localPosition = Vector2.zero;
        player.EnteredVehicle(SetInputVelocity);
        charactersInBoat[0] = player.movement;
        player.movement.Sitting(true);
        feature.Uninstall();
    }

    void HandlePlayerExited(Vector2 location) {
        inUse = false;
        player.transform.parent = terrain.transform;
        player.transform.localPosition = location;
        player.ExitedVehicle();
        player.movement.Sitting(false);
        charactersInBoat[0] = null;
        terrain.Feature[currentTile] = feature;
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
        // float expectedX = (shoreCorrection.x * inputVelocity.x) > 0 && Mathf.Abs(shoreCorrection.x) < Mathf.Abs(inputVelocity.x)
        //     ? shoreCorrection.x : inputVelocity.x;
        // float expectedY = (shoreCorrection.y * inputVelocity.y) > 0 && Mathf.Abs(shoreCorrection.y) < Mathf.Abs(inputVelocity.y)
        //     ? shoreCorrection.y : inputVelocity.y;
        Vector2 expectedVelocity = inputVelocity + shoreCorrection;
        Debug.Log("Input velocity: " + inputVelocity + " / shore max velocity: " + shoreCorrection + " / actual max velocity: " + expectedVelocity);

        if (currentVelocity != expectedVelocity) {
            Debug.Log(currentVelocity + " towards " + expectedVelocity + " at " + acceleration * Time.fixedDeltaTime);
            currentVelocity = Vector2.MoveTowards(currentVelocity, expectedVelocity, acceleration * Time.fixedDeltaTime);
            if (currentVelocity.magnitude < minSpeed) {
                currentVelocity = Vector2.MoveTowards(currentVelocity, expectedVelocity, minSpeed);
            }
            Debug.Log("is " + currentVelocity);
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
