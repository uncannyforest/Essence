using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ephemeral : MonoBehaviour {
    public float duration;

    void Start() {
        GameObject.Destroy(gameObject, duration);
    }
}
