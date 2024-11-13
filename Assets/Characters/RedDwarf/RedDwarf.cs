using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RedDwarfConfig {
    public Sprite woodBuildAction;
    public float buildTime;
    public float buildDistance;
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

        Actions = new List<CreatureAction>() {
            CreatureAction.WithTerrain(redDwarf.woodBuildAction,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => terrain[loc] = Construction.Wood).ForPosition((p) => true).Queued(),
                TeleFilter.Terrain.WOODBUILDING),
            CreatureAction.WithFeature(FeatureLibrary.P.boat,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => terrain.BuildFeature(loc.Coord, FeatureLibrary.P.boat)).ForVector2Int((p) => true).Queued())
        };

        Habitat = Habitat.Land(this, Land.Woodpile, Habitat.InteractionMode.Inside);
    }
}