using System.Linq;
using UnityEngine;

public class Fauna : MonoBehaviour {
    public float possibilityRate = 1/256;
    public CreatureLibrary creatureLibrary;
    public Terrain terrain;
    public Transform parent;
    public Transform player;

    private Vector2 beyondPlayer;

    private int creatureCount = 0;

    void Start() {
        Invoke("Repeat", possibilityRate);
        beyondPlayer = terrain.Bounds.Vector2.Map(c => c / 2f - Creature.neighborhood);
    }

    private void Repeat() {
        Populate();
        Invoke("Repeat", GetRepeatRate());
    }

    private float GetRepeatRate() {
        int numCreatures = GameObject.FindObjectsOfType<Creature>().Length;
        return numCreatures * numCreatures * possibilityRate;
    }

    private Vector2Int RandomLocation() {
        Vector2 direction = Randoms.ChebyshevUnit();
        float rawMagnitude = Random.value;
        Vector2 magnitude = new Vector2(PlayerCharacter.neighborhood + beyondPlayer.x * rawMagnitude,
                                        PlayerCharacter.neighborhood + beyondPlayer.y * rawMagnitude);
        Vector2 location = direction * magnitude;
        return terrain.Bounds.Wrap(terrain.CellAt(player.position) + location.FloorToInt());
    }

    private Vector2Int? IdentifyLocation() {
        for (int i = 0; i < 100; i++) {
            Vector2Int possLocation = RandomLocation();
            if (!terrain.Land[possLocation].IsPassable() ||
                terrain.Land[possLocation].IsWatery()) continue;
            if (Physics2D.OverlapCircleAll(terrain.CellCenter(possLocation), PlayerCharacter.neighborhood).Length != 0) continue;
            if (0 != Random.Range(0, 1 + Physics2D.OverlapCircleAll(terrain.CellCenter(possLocation), PlayerCharacter.neighborhood).Length)) continue;
            return possLocation;
        }
        return null;
    }

    private GameObject IdentifyCreature() {
        if (Randoms.CoinFlip) return creatureLibrary.stipule;
        int justARandomNumberForNow = Random.Range(0, 4);
        switch (justARandomNumberForNow) {
            case 0: return creatureLibrary.bunny;
            case 1: return creatureLibrary.arrowwiggle;
            case 2: return creatureLibrary.archer;
            default: return creatureLibrary.redDwarf;
        }
    }

    private void Populate() {
        Vector2Int? possRandomTile = IdentifyLocation();
        if (possRandomTile is Vector2Int randomTile) {
            GameObject prefab = IdentifyCreature();
            GameObject newCreature = Instantiate(prefab, terrain.CellCenter(randomTile), Quaternion.identity, parent);
            newCreature.name = prefab.name + " " + creatureCount++;
        }
    }
}
