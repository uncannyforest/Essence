using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEngine.Events;

public class Orientor : MonoBehaviour {
    private static Orientor instance;
    public static Orientor I { get => instance; }
    void Awake() { if (instance == null) instance = this; }

    new public Camera camera;
    public Vector3 transparencySortAxis;

    public UnityAction onRotation;

    [SerializeField]
    private Orientation rotation;

    public static Orientation Rotation {
        get => instance.rotation;
        set {
            instance.rotation = value;
            instance.UpdateRotation();
        }
    }

    public static Vector2 WorldFromScreen(ScreenVector input) {
        return Quaternion.Euler(0, 0, (int)Rotation) * new Vector2(input.x, input.y);
    }
    public static void SetRotation(Transform gridChild) {
        gridChild.eulerAngles = new Vector3(0, 0, (int)Rotation);
    }

    public void UpdateRotation() {
        camera.transparencySortAxis = Quaternion.Euler(0, 0, (int)rotation) * transparencySortAxis;
        Debug.Log(camera.transparencySortAxis*10);

        UpdateChildRotations(transform);
        if (onRotation != null) onRotation();
    }

    public void UpdateChildRotations(Transform parent) {
        foreach (Transform child in parent) {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            TilemapGroup childTilemapGroup = child.GetComponent<TilemapGroup>();
            DisplacementOrientable displacement = child.GetComponent<DisplacementOrientable>();

            if (tilemap != null) {

                // Update tilemaps: refresh rule tiles
                tilemap.RefreshAllTiles();

            } else if (childTilemapGroup != null) {

                // Update groups of tilemaps
                UpdateChildRotations(childTilemapGroup.transform);

            } else if (displacement != null) {

                // Update objects whose sprites should not be rotated
                displacement.Orient();
            } else {

                // Update object sprites
                SetRotation(child.transform);
            }
        }
    }
}
