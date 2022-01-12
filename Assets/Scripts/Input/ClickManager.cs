using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ClickManager {
    private WorldInteraction interactionScript;

    public ClickManager(WorldInteraction interactionScript) {
        this.interactionScript = interactionScript;
    }

	public void Update () {
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) {
            interactionScript.PointerMove(PointerPosition);
        }
	}

    public static Vector2 PointerPosition {
        get {
            Vector3 mouse = Input.mousePosition;
            return Camera.main.ScreenToWorldPoint(mouse);
        }
    } 

    private Collider2D CheckForObject(Vector2 mousePos2D) {
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            return hit.collider;
    }
}