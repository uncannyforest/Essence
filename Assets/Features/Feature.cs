using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Feature : MonoBehaviour {
    [SerializeField] public LandFlags validLand = 0;
    [SerializeField] public bool roofValid;
    [SerializeField] public bool canHit = true;

    public Vector2Int? tile;
    public Func<PlayerCharacter, bool> PlayerEntered;
    public Action<Transform> Attacked;
    public Action Died; // if set, overrides Destroy() as response

    [HideInInspector] public string type;
    [NonSerialized] public Func<int[]> SerializeFields;
    [NonSerialized] public int[] serializedFields;

    void Start() { GetComponent<Health>().ReachedZero += HandleDied; }

    public bool IsValidTerrain(Land land) {
        return ((int)validLand & 1 << (int)land) != 0;
    }
    public bool IsValidTerrain(Construction construction) {
        return construction == Construction.None || roofValid;
    }
    public Land GetSomeValidLand() {
        int i = 0;
        while (!IsValidTerrain((Land)i)) i++;
        return (Land)i;
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

    public void Attack(Transform blame, int quantity = 1) {
        if (!canHit) return;
        if (Attacked != null) Attacked(blame);
        else GetComponent<Health>().Decrease(quantity, blame);
    }
    void HandleDied() {
        if (Died != null) Died();
        else Destroy();
    }

    [Serializable] public struct Data {
        public int x;
        public int y;
        public string type;
        public int[] customFields; // serialization hack

        public Vector2Int tile { get => Vct.I(x, y); }

        public Data(Vector2Int tile, string type, int[] customFields) {
            this.x = tile.x;
            this.y = tile.y;
            this.type = type;
            this.customFields = customFields;
        }
    }
    public Data? Serialize() {
        int[] customFields;
        if (SerializeFields == null) customFields = new int[0];
        else customFields = SerializeFields();
        if (tile is Vector2Int actualTile) return new Data(actualTile, type, customFields);
        else {
            Debug.LogError("Feature not instantiated! Not writing it to data");
            return null;
        }
    }
    public void DeserializeUponStart(int[] customFields) {
        serializedFields = customFields;
    }
}
