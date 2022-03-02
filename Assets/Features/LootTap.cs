using System;
using UnityEngine;

[Serializable] public class PluralCollectible {
    public Collectible prefab;
    public int quantity;
    public string name;
}

[RequireComponent(typeof(Feature))]
// While not inherently required, all Features with LootTap will have Health
[RequireComponent(typeof(Health))]
public class LootTap : MonoBehaviour {
    public float reloadTime = 120;
    public float itemStartY = .5f;
    public PluralCollectible[] drops;
    
    private float nextTapTime = 0;

    private Transform grid;

    void Start() {
        GetComponent<Feature>().Attacked += HandleAttacked;
        grid = Terrain.I.transform;
    }

    void HandleAttacked(Transform blame) {
        if (Time.time > nextTapTime && blame.GetComponent<PlayerCharacter>() != null) {
            Tap(blame.GetComponent<PlayerCharacter>());
            nextTapTime = Time.time + reloadTime;
        }
        else GetComponent<Health>().Decrease(1, blame);
    }

    private void Tap(PlayerCharacter player) {
        PluralCollectible drop = drops[UnityEngine.Random.Range(0, drops.Length)];
        Collectible.InstantiateAndCollect(drop.prefab,
            grid, transform.position, itemStartY, drop.quantity, player.GetComponentStrict<Inventory>());
        TextDisplay.I.ShowMiniText("Found " + drop.name + "!");
    }
}
