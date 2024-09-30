using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cardboard : MonoBehaviour {
    private IEnumerable<SpriteRenderer> Sprites {
        get => GetComponentsInChildren<SpriteRenderer>();
    }

    // Set by boats and CharacterController on terrain
    public float VerticalDisplacement {
        get =>  -1.25f * transform.position.z;
        set => transform.position = new Vector3(transform.position.x, transform.position.y, -.8f * value);
    }

    public void Start() {
        Orient(Orientor3D.I.cardboardDirection);
    }

    public void Orient(Transform camera) {
        transform.rotation = camera.rotation;
    }

    public static void OrientAllCardboards(Transform camera) {
        foreach (Cardboard cardboard in GameObject.FindObjectsOfType<Cardboard>()) {
            cardboard.Orient(camera);
        }
    }
}
