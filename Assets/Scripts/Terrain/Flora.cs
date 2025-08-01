using System.Collections.Generic;
using UnityEngine;

public class Flora : MonoBehaviour{
    public float possibilityRate = 1/256f;
    public Terrain terrain;
    public Transform parent;
    public Transform player;

    private Vector2 beyondPlayer;

    private int floraCount = 0;

    void Start() {
        StartCoroutine(Repeat());
        beyondPlayer = terrain.Bounds.Vector2.Map(c => c / 2f - PlayerCharacter.neighborhood);
    }

    private IEnumerator<YieldInstruction> Repeat() {
        yield return null;
        while (true) {
            Populate();
            yield return new WaitForSeconds(GetRepeatRate());
        }
    }

    private float GetRepeatRate() {
        int numFlora = floraCount;
        return numFlora * numFlora * possibilityRate;
    }

    private FeatureConfig IdentifyFeature() {
        return Randoms.CoinFlip ? FeatureLibrary.C.jasmine : FeatureLibrary.C.carrot;
    }

    private Vector2Int RandomLocation() {
        Vector2 direction = Randoms.ChebyshevUnit();
        float rawMagnitude = Random.value;
        Vector2 magnitude = new Vector2(PlayerCharacter.neighborhood + beyondPlayer.x * rawMagnitude,
                                        PlayerCharacter.neighborhood + beyondPlayer.y * rawMagnitude);
        Vector2 location = direction * magnitude;
        return terrain.Bounds.Wrap(terrain.CellAt(player.position) + location.FloorToInt());
    }

    private Vector2Int? IdentifyLocation(FeatureConfig feature) {
        for (int i = 0; i < 100; i++) {
            Vector2Int possLocation = RandomLocation();
            if (terrain.Land[possLocation] != Land.Meadow) continue;
            if (!feature.IsValidTerrain(Terrain.I[new Terrain.Position(Terrain.Grid.Roof, possLocation)]) || Terrain.I.Feature[possLocation] != null) continue;
            if (Physics2D.OverlapCircleAll(terrain.CellCenter(possLocation), PlayerCharacter.neighborhood, LayerMask.GetMask("Player")).Length != 0) continue;
            Debug.Log("FAR ENOUGH FROM PLAYER!");
            return possLocation;
        }
        return null;
    }

    private void Populate() {
        FeatureConfig prefab = IdentifyFeature();
        Vector2Int? possRandomTile = IdentifyLocation(prefab);
        if (possRandomTile is Vector2Int randomTile) {
            terrain.Land[randomTile] = Land.Grass;
            terrain.BuildFeature(randomTile, prefab);
            floraCount++;
        }
    }

}
