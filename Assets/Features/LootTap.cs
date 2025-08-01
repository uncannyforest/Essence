using System;
using UnityEngine;

[Serializable] public class PluralCollectible {
    public Collectible prefab;
    public int quantity;
    public string name;
}

[RequireComponent(typeof(FeatureHooks))]
public class LootTap : MonoBehaviour {
    public float reloadTime = 120;
    public float itemStartY = .5f;
    public PluralCollectible[] drops;
    
    private float nextTapTime = 0;

    private Transform grid;

    void Start() {
        GetComponent<FeatureHooks>().Attacked += HandleAttacked;
        grid = Terrain.I.transform;
    }

    bool HandleAttacked() {
        if (Time.time > nextTapTime) {
            Tap(GameManager.I.AnyPlayer);
            nextTapTime = Time.time + reloadTime;
            return false;
        }
        else return true;
    }

    private void Tap(PlayerCharacter player) {
        PluralCollectible drop = drops[UnityEngine.Random.Range(0, drops.Length)];
        Vector3 position = transform.position;
        position.z = -itemStartY;
        bool collected = Collectible.InstantiateAndCollect(drop.prefab,
            grid, position, drop.quantity, player.GetComponentStrict<Inventory>());
        TextDisplay.I.ShowMiniText("Found " + drop.name + (collected ? "!" : " but inventory full"));
    }
}
