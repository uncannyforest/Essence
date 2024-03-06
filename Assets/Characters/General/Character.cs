using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// for character as subject
// for character as object, see CharacterController
[RequireComponent(typeof(Team))]
public class Character : MonoBehaviour {
    public int height;
    public bool broadGirth;

    [NonSerialized] public SpriteSorter spriteSorter;

    public Team Team { get => this.GetComponentStrict<Team>(); }

    void Start() {
        spriteSorter = GetComponentInChildren<SpriteSorter>();
        if (spriteSorter != null) {
            height = spriteSorter.Height;
        }
    }
}
