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

		if (SimpleInput.GetButtonDown("Fire")) {
			interactionScript.Confirm(PointerPosition);
		}
	}

    public Vector2 PointerPosition {
        get {
            Vector3 mouse = Input.mousePosition;
            // RectTransformUtility.ScreenPointToLocalPointInRectangle(
            //     worldView, mouse, null, out Vector2 screenPoint
            // );
            // Vector2 normalizedPoint = Rect.PointToNormalized(worldView.rect, screenPoint);
            // Vector3 world = interactionScript.camera.ViewportToWorldPoint(normalizedPoint);
            return Camera.main.ScreenToWorldPoint(mouse);
        }
    } 

    private Collider2D CheckForObject(Vector2 mousePos2D) {
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            return hit.collider;
    }
}