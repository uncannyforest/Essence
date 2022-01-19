using System;
using UnityEngine;

public class InputManager : MonoBehaviour {
    public PlayerCharacter playerScript;
    public WorldInteraction world;
    public TextDisplay textDisplay;
    public bool useWASD;
     [TextArea(6, 12)] public string keysTip = "Number keys (<color=#c06000>1</color>-<color=#c06000>0</color>) - select action\n" +
        "<color=#c06000>space</color> or left click - use action\n" +
        "<color=#c06000>,</color> (<color=#c06000><</color>) and <color=#c06000>.</color> (<color=#c06000>></color>) - rotate view\n" +
        "<color=#c06000>Shift</color> - aim with directional keys\n" +
        "<color=#c06000>P</color> or <color=#c06000>Esc</color> - pause\n" +
        "<color=#c06000>/</color> - this menu";

    void Start() {
        SelectAction2();
    }

    public static Vector2 PointerPosition {
        get {
            Vector3 mouse = Input.mousePosition;
            return Camera.main.ScreenToWorldPoint(mouse);
        }
    }
    public static bool Clicking {
        get => SimpleInput.GetButton("Fire");
    }

    private Collider2D CheckForObject(Vector2 mousePos2D) {
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            return hit.collider;
    }

    private Vector2Int GetLeftHandDirection() {
        int x = 0;
        int y = 0;
        if (useWASD) {
            if (Input.GetKey("w")) y += 1;
            if (Input.GetKey("a")) x -= 1;
            if (Input.GetKey("s")) y -= 1;
            if (Input.GetKey("d")) x += 1;
        } else {
            if (Input.GetKey("a")) { x -= 1; y -= 1; }
            if (Input.GetKey("w")) { x -= 1; y += 1; }
            if (Input.GetKey("e")) { x += 1; y += 1; }
            if (Input.GetKey("f")) { x += 1; y -= 1; }
        }
        return new Vector2Int(x, y);
    }

    public void SelectAction1() => world.PlayerAction = WorldInteraction.Mode.Sword;
    public void SelectAction2() =>  world.PlayerAction = WorldInteraction.Mode.Arrow;
    public void SelectAction3() =>  world.PlayerAction = WorldInteraction.Mode.Praxel;
    public void SelectAction4() =>  world.PlayerAction = WorldInteraction.Mode.WoodBuilding;
    public void SelectAction5() =>  world.PlayerAction = WorldInteraction.Mode.Sod;
    public void SelectAction6() =>  world.PlayerAction = WorldInteraction.Mode.Taming;
    public void SelectAction7() =>  world.MaybeUseCreatureAction(0);
    public void SelectAction8() =>  world.MaybeUseCreatureAction(1);
    public void SelectAction9() =>  world.MaybeUseCreatureAction(2);
    public void SelectAction0() =>  world.MaybeUseCreatureAction(3);

    public void Update() {
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) {
            world.PointerMove(PointerPosition, Clicking);
        }

        if (SimpleInput.GetButtonDown("Pause")) {
            textDisplay.ToggleFullText();
        }
        if (SimpleInput.GetButtonDown("Help")) {
            textDisplay.ShowFullText(keysTip);
        }
		if (SimpleInput.GetButtonDown("Fire")) {
            if (textDisplay.IsFullTextUp) {
                textDisplay.HideFullText();
            } else {
			    world.Confirm(PointerPosition);
            }
		}
        if (SimpleInput.GetButtonUp("Fire")) {
            world.ConfirmComplete(PointerPosition);
        }

        if (Input.GetKeyDown("1")) SelectAction1();
        if (Input.GetKeyDown("2")) SelectAction2();
        if (Input.GetKeyDown("3")) SelectAction3();
        if (Input.GetKeyDown("4")) SelectAction4();
        if (Input.GetKeyDown("5")) SelectAction5();
        if (Input.GetKeyDown("6")) SelectAction6();
        if (Input.GetKeyDown("7")) SelectAction7();
        if (Input.GetKeyDown("8")) SelectAction8();
        if (Input.GetKeyDown("9")) SelectAction9();
        if (Input.GetKeyDown("0")) SelectAction0();

        float h = Math.Sign(SimpleInput.GetAxis("Horizontal"));
        float v = Math.Sign(SimpleInput.GetAxis("Vertical"));

        Vector2 move = Orientor.WorldFromScreen(
            new ScreenVector(new Vector2(h, v) + GetLeftHandDirection()));
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
