using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RedDwarfConfig {
    public Sprite woodBuildAction;
    public float buildTime;
    public float buildDistance;
    public int tamingCost = 1;
}

public class RedDwarf : Species<RedDwarfConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new RedDwarfBrain(this, generalConfig, speciesConfig);
    }
}

public class RedDwarfBrain : Brain {
    private RedDwarfConfig redDwarf;

    public RedDwarfBrain(RedDwarf species, BrainConfig general, RedDwarfConfig redDwarf) : base(species, general) {
        this.redDwarf = redDwarf;
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.WithTerrain(redDwarf.woodBuildAction,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => terrain[loc] = Construction.Wood).Queued(),
                TeleFilter.Terrain.WOODBUILDING),
            CreatureAction.WithFeature(FeatureLibrary.P.boat,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => terrain.BuildFeature(loc.Coord, FeatureLibrary.P.boat)).ForVector2Int().Queued())
        };
    }

    override public bool CanTame(Transform player) =>
        player.GetComponentStrict<Inventory>().CanRetrieve(Material.Type.Gemstone, redDwarf.tamingCost);

    public override bool ExtractTamingCost(Transform player) =>
        player.GetComponentStrict<Inventory>().Retrieve(Material.Type.Gemstone, redDwarf.tamingCost);

}