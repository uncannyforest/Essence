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
                    (loc) => { resource.Use(Cost(loc)); terrain[loc] = Construction.Wood; }).ForPosition((p) => SufficientResource(Cost(p))).Queued(),
                TeleFilter.Terrain.WOODBUILDING),
            CreatureAction.WithFeature(FeatureLibrary.P.boat,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => { resource.Use(60); terrain.BuildFeature(loc.Coord, FeatureLibrary.P.boat); }).ForVector2Int((p) => SufficientResource(60)).Queued())
        };

        Habitat = new WoodpileHabitat(this, () => creature.stats.ExeTime * 5);
    }

    public int Cost(Terrain.Position pos) => pos.grid == Terrain.Grid.Roof ? 4 : 1;
}