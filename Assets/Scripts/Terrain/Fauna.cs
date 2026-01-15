using System.Collections.Generic;
using UnityEngine;

public class Fauna : MonoBehaviour {
    public float possibilityRate = 1/256f;
    public float fixedRate = 1;
    public int maxCreatures = 8;
    public Terrain terrain;
    public Transform parent;
    public Transform player;

    private Vector2 beyondPlayer;

    private int creatureCount = 0;

    void Start() {
        StartCoroutine(Repeat());
        beyondPlayer = terrain.Bounds.Vector2.Map(c => c / 2f - PlayerCharacter.neighborhood);
    }

    private IEnumerator<YieldInstruction> Repeat() {
        yield return null;
        while (true) {
            Collider2D[] nearbyCreatures = Physics2D.OverlapCircleAll(GameManager.I.AnyPlayer.transform.position,
                PlayerCharacter.neighborhood * 2, LayerMask.GetMask("Creature", "HealthCreature"));
            int numCreatures = nearbyCreatures.Length;
            int followingCreatures = 0;
            foreach (Collider2D c in nearbyCreatures) {
                Creature creature = c.GetComponentStrict<Creature>();
                if (GameManager.I.YourTeam.SameTeam(creature.team) &&
                        creature.State.scanActivity?.command.type == PassiveCommandType.Follow)
                    followingCreatures++;
            }
            int spawnNumber = maxCreatures - numCreatures + followingCreatures * 2;  // following creatures *increase* spawn count
            if (spawnNumber > 0) Debug.Log(numCreatures + " creatures nearby, " +
                followingCreatures + " following player, spawning " + spawnNumber);
            for (int i = 0; i < spawnNumber; i++) Populate();
            yield return new WaitForSeconds(fixedRate);
        }
    }
    
    private float GetRepeatRate() {
        int numCreatures = GameObject.FindObjectsOfType<Creature>().Length;
        Debug.Log("Added creature, waiting " + numCreatures * numCreatures * possibilityRate);
        return numCreatures * numCreatures * possibilityRate;
    }

    private Vector2Int RandomLocation() {
        Vector2 direction = Randoms.ChebyshevUnit();
        float rawMagnitude = Random.value;
        Vector2 magnitude = new Vector2(PlayerCharacter.neighborhood * (1 + rawMagnitude * rawMagnitude), // square rawMagniture to bias closer to player
                                        PlayerCharacter.neighborhood * (1 + rawMagnitude * rawMagnitude)); // TODO: change that to include fountains
        Vector2 location = direction * magnitude;
        return terrain.Bounds.Wrap(terrain.CellAt(GameManager.I.AnyPlayer.transform.position) + location.FloorToInt());
    }

    private Vector2Int? IdentifyLocation() {
        for (int i = 0; i < 100; i++) {
            Vector2Int possLocation = RandomLocation();
            if (!terrain.Land[possLocation].IsPassable() ||
                terrain.Land[possLocation].IsWatery()) continue;
            if (Physics2D.OverlapCircleAll(terrain.CellCenter(possLocation), PlayerCharacter.neighborhood, LayerMask.GetMask("Player")).Length != 0) continue;
            if (0 != Random.Range(0, 1 + Physics2D.OverlapCircleAll(terrain.CellCenter(possLocation), PlayerCharacter.neighborhood, LayerMask.GetMask("Creature", "HealthCreature")).Length)) continue;
            return possLocation;
        }
        return null;
    }

    private Creature IdentifyCreature() {
        int justARandomNumberForNow = Random.Range(0, 3);
        switch (justARandomNumberForNow) {
            case 0: return CreatureLibrary.P.farmer;
            case 1: return CreatureLibrary.P.moose;
            // case 0: return CreatureLibrary.P.bunny;
            // case 1: return CreatureLibrary.P.arrowwiggle;
            // case 2: return CreatureLibrary.P.archer;
            // case 4: return CreatureLibrary.P.axe;
            // case 5: return CreatureLibrary.P.stipule;
            default: return CreatureLibrary.P.redDwarf;
        }
    }

    private void Populate() {
        Vector2Int? possRandomTile = IdentifyLocation();
        if (possRandomTile is Vector2Int randomTile) {
            Creature prefab = IdentifyCreature();
            Creature newCreature = Instantiate(prefab, terrain.CellCenter(randomTile), Quaternion.identity, parent);
            newCreature.gameObject.name = prefab.name + " " + creatureCount++;
        }
    }
}
