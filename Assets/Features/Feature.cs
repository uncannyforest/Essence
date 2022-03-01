using System;
using UnityEngine;

public class Feature : MonoBehaviour {
    [SerializeField] public LandFlags validLand = 0;
    [SerializeField] public bool roofValid;

    public Vector2Int? tile;
    public Func<PlayerCharacter, bool> PlayerEntered;

    public bool IsValidTerrain(Land land) {
        return ((int)validLand & 1 << (int)land) != 0;
    }
    public bool IsValidTerrain(Construction construction) {
        return construction == Construction.None || roofValid;
    }
    public void Uninstall() {
        if (tile is Vector2Int realTile) {
            Terrain.I.UninstallFeature(realTile);
        } else Debug.LogError(this + " not installed");
    }
    public void Destroy() {
        if (tile is Vector2Int realTile) {
            Terrain.I.Feature[realTile] = null;
        } else {
            Debug.Log("Destroying loose feature " + this);
            GameObject.Destroy(this.gameObject);
        }
    }
}
