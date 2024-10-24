using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Biome")]
public class Biome : ScriptableObject {
    public UnityEngine.Material material;
    public TileVariation grass = new TileVariation(TileMaterial.Level.Land);
    public TileVariation meadow = new TileVariation(TileMaterial.Level.Land);
    public TileVariation shrub = new TileVariation(TileMaterial.Level.Land);
    public TileVariation forest = new TileVariation(TileMaterial.Level.Land);
    public TileVariation water = new TileVariation(TileMaterial.Level.Land);
    public TileVariation ditch = new TileVariation(TileMaterial.Level.Land);
    public TileVariation dirtpile = new TileVariation(TileMaterial.Level.Land);
    public TileVariation woodpile = new TileVariation(TileMaterial.Level.Land);
    public TileVariation hill = new TileVariation(TileMaterial.Level.Land);
    [NonSerialized] public TileVariation noRoof = new TileVariation(TileMaterial.Level.Roof);
    public TileVariation woodRoof = new TileVariation(TileMaterial.Level.Roof);
    public EdgeVariation woodWall = new EdgeVariation();

    private Dictionary<TileMaterial, TileVariation> byLand;
    public TileVariation this[TileMaterial land] { get => byLand[land]; }

    void OnEnable() {
        byLand = new Dictionary<TileMaterial, TileVariation> {
            [Land.Grass] = grass,
            [Land.Meadow] = meadow,
            [Land.Shrub] = shrub,
            [Land.Forest] = forest,
            [Land.Water] = water,
            [Land.Ditch] = ditch,
            [Land.Dirtpile] = dirtpile,
            [Land.Woodpile] = woodpile,
            [Land.Hill] = hill,
            [Construction.None] = noRoof,
            [Construction.Wood] = woodRoof,
        };

        foreach (TileMaterial land in byLand.Keys) {
            byLand[land].biome = this;
            byLand[land].thisLayer = land;
        }
        woodWall.biome = this;
    }
}