using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cardboard : MonoBehaviour {
    private static float SCALE_Y = 0.8164965809f; // sqrt(2/3)

    public bool keepPerpendicularToGround = true;

    private IEnumerable<SpriteRenderer> Sprites {
        get => GetComponentsInChildren<SpriteRenderer>();
    }

    // Set by boats and CharacterController on terrain
    public float VerticalDisplacement {
        get =>  transform.position.z / -SCALE_Y;
        set => transform.position = new Vector3(transform.position.x, transform.position.y, -SCALE_Y * value);
    }

    public void Start() {
        Orient(keepPerpendicularToGround ? Orientor3D.I.cardboardPerpendicularDirection : Orientor3D.I.cameraDirection);
    }

    public void Orient(Transform camera) {
        transform.rotation = camera.rotation;
    }

    public static void OrientAllCardboards(Transform camera, Transform perpendicular) {
        foreach (Cardboard cardboard in GameObject.FindObjectsOfType<Cardboard>()) {
            cardboard.Orient(cardboard.keepPerpendicularToGround ? perpendicular : camera);
        }
    }
}
