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
    public RedDwarfBrain(RedDwarf species, BrainConfig general, RedDwarfConfig redDwarf) : base(species, general) {
        Actions = new List<CreatureAction>() {
            CreatureAction.WithTerrain(redDwarf.woodBuildAction,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, (p) => SufficientResource(Cost(p)), () => creature.stats.ExeTime,
                    (loc) => { resource.Use(Cost(loc)); terrain[loc] = Construction.Wood; }).PendingPosition().Queued(),
                TeleFilter.Terrain.WOODBUILDING),
            CreatureAction.WithFeature(FeatureLibrary.C.boat,
                pathfinding.BuildFeature(FeatureLibrary.C.boat, 60))
        };

        Habitat = new ConsumableFeatureHabitat(this, FeatureLibrary.C.woodPile, () => creature.stats.ExeTime * 5);
    }

    public int Cost(Terrain.Position pos) => pos.grid == Terrain.Grid.Roof ? 4 : 1;
}