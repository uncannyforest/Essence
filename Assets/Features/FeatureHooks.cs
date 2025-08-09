using System;
using System.Linq;
using UnityEngine;

public class FeatureHooks : MonoBehaviour {
    [SerializeField] public LandFlags validLand = 0;
    [SerializeField] public bool roofValid;
    [SerializeField] public bool canHit = true;

    public Vector2Int? tile;
    public Feature feature;

    public Func<PlayerCharacter, bool> PlayerEntered;
    public Func<bool> Attacked;

    [NonSerialized] public Func<int[]> SerializeFields;
    [NonSerialized] public int[] serializedFields;

    public bool Place(Vector2Int pos) {
        if (!feature.config.IsValidTerrain(pos)) return false;
        transform.position = Terrain.I.CellCenter(pos).WithZ(GlobalConfig.I.elevation.features);
        Terrain.I.ForceSetFeature(pos, feature);
        tile = pos;
        return true;
    }

    public void Uninstall() {
        if (tile is Vector2Int realTile) {
            Terrain.I.UninstallFeature(realTile);
        } else Debug.LogError(this + " not installed");
    }

    // returns true if attack should continue being processed
    public bool Attack() {
        if (Attacked != null) return Attacked();
        return true;
    }

    public static int[] SerializeString(string input) =>
        input.ToCharArray().Select(c => (int)c).ToArray();

    public static string DeserializeString(int[] serialized) =>
        new string(serialized.Select(c => (char)c).ToArray());
}
