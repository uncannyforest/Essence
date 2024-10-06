using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cardboard : MonoBehaviour {
    public bool keepPerpendicularToGround = true;

    private IEnumerable<SpriteRenderer> Sprites {
        get => GetComponentsInChildren<SpriteRenderer>();
    }

    // Set by boats and CharacterController on terrain
    public float VerticalDisplacement {
        get =>  -1.25f * transform.position.z;
        set => transform.position = new Vector3(transform.position.x, transform.position.y, -.8f * value);
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
