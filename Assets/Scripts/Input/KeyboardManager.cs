using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class KeyboardManager {
    private PlayerCharacter playerScript;
    private WorldInteraction world;

    public KeyboardManager(PlayerCharacter playerScript, WorldInteraction world) {
        this.playerScript = playerScript;
        this.world = world;
    }

    public void Update() {
        if (Input.GetKeyDown("1")) {
            world.PlayerAction = WorldInteraction.Mode.Sword;
        }
        if (Input.GetKeyDown("2")) {
            world.PlayerAction = WorldInteraction.Mode.Arrow;
        }
        if (Input.GetKeyDown("3")) {
            world.PlayerAction = WorldInteraction.Mode.Praxel;
        }
        if (Input.GetKeyDown("4")) {
            world.PlayerAction = WorldInteraction.Mode.WoodBuilding;
        }
        if (Input.GetKeyDown("5")) {
            world.PlayerAction = WorldInteraction.Mode.Sod;
        }
        if (Input.GetKeyDown("6")) {
            world.PlayerAction = WorldInteraction.Mode.Taming;
        }
        if (Input.GetKeyDown("7")) {
            world.MaybeUseCreatureAction(0);
        }
        if (Input.GetKeyDown("8")) {
            world.MaybeUseCreatureAction(1);
        }
        if (Input.GetKeyDown("9")) {
            world.MaybeUseCreatureAction(2);
        }
        if (Input.GetKeyDown("0")) {
            world.MaybeUseCreatureAction(3);
        }

        float h = Math.Sign(SimpleInput.GetAxis("Horizontal"));
        float v = Math.Sign(SimpleInput.GetAxis("Vertical"));
        float ne = Math.Sign(SimpleInput.GetAxis("Diagonal NE"));
        float nw = Math.Sign(SimpleInput.GetAxis("Diagonal NW"));

        Vector2 move = Orientor.WorldFromScreen(
            new ScreenVector(new Vector2(h, v) + ne * new Vector2(1, 1) + nw * new Vector2(-1, 1)));
        Vector2Int moveUnit = new Vector2Int(move.x > .1 ? 1 : move.x < -.1 ? -1 : 0, move.y > .1 ? 1 : move.y < -.1 ? -1 : 0);
        playerScript.InputVelocity = (Input.GetKey("left shift") || Input.GetKey("right shift"))
            ? Vector2Int.zero : moveUnit;
        world.PlayerMove(moveUnit);

        int rotate = 0;
        if (SimpleInput.GetButtonDown("Camera Right")) {
            rotate = 270;
        } else if (SimpleInput.GetButtonDown("Camera Left")) {
            rotate = 90;
        } else {
            return;
        }
        Orientor.Rotation = (Orientation)(((int)Orientor.Rotation + rotate) % 360);
    }
}
