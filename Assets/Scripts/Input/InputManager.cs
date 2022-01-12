using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour {
    public PlayerCharacter playerScript;
    public WorldInteraction world;
    public TextDisplay textDisplay;

    private ClickManager clicks;
    private KeyboardManager keys;

    void Start() {
        clicks = new ClickManager(world);
        keys = new KeyboardManager(playerScript, world, textDisplay);
    }

    void Update() {
        clicks.Update();
        keys.Update();
    }
}
