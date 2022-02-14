using System;
using UnityEngine;

public class Feature : MonoBehaviour {
    [SerializeField] public LandFlags validLand = 0;
    [SerializeField] public bool roofValid;

    public Action<PlayerCharacter> PlayerEntered;

    public bool IsValidTerrain(Land land) {
        return ((int)validLand & 1 << (int)land) != 0;
    }
    public bool IsValidTerrain(Construction construction) {
        return construction == Construction.None || roofValid;
    }
}
