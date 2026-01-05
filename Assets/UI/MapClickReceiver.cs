using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapClickReceiver : MonoBehaviour {
    void Update() {
        InputManager.I.pointerIsOverMap = !IsMouseOverUI();
    }
    
    // derived from https://youtu.be/ptmum1FXiLE
    private List<RaycastResult> GetUIUnderMouse() {
        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Input.mousePosition;

        List<RaycastResult> raycasts = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, raycasts);
        return raycasts;
    }

    private bool IsMouseOverUI() {
        return GetUIUnderMouse().Count > 0;
    }

    public void DebugRaycast() {
        foreach (RaycastResult raycast in GetUIUnderMouse()) {
            Debug.Log("Clicked UI object: " + raycast);
        }
    }
}
