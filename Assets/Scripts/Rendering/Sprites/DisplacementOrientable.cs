using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplacementOrientable : Orientable {
    
    private Vector3 defaultDisplacement;

    override public void Start() {
        defaultDisplacement = transform.GetChild(0).localPosition;
        Orient(Orientor.Rotation);
    }

    public void Orient(Orientation orientation) {
        transform.GetChild(0).localPosition =
                Quaternion.Euler(0, 0, (int)orientation) * defaultDisplacement;
        Debug.Log(defaultDisplacement + " " + transform.GetChild(0).localPosition);
    }
}
