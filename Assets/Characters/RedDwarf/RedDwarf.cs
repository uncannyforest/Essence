using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RedDwarfConfig {
    public Sprite attackAction;
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
        MainBehavior = new CharacterTargetedBehavior(this,
            AttackCharacterBehavior,
            (c) => Will.IsThreat(teamId, c),
            (c) => SufficientResource() && Will.CanSee(transform.position, c));

        Actions = new List<CreatureAction>() {
            MainBehavior.CreatureActionCharacter(redDwarf.attackAction),
            CreatureAction.WithTerrain(redDwarf.woodBuildAction,
                pathfinding.ApproachThenInteract(
                    redDwarf.buildDistance, (p) => SufficientResource(Cost(p)), () => creature.stats.ExeTime,
                    (loc) => { resource.Use(Cost(loc)); terrain[loc] = Construction.Wood; }).PendingPosition().Queued(),
                TeleFilter.Terrain.WOODBUILDING),
            CreatureAction.WithFeature(FeatureLibrary.C.boat,
                pathfinding.BuildFeature(FeatureLibrary.C.boat, 60))
        };

        Habitat = new ConsumableFeatureHabitat(this, FeatureLibrary.C.woodPile, () => creature.stats.ExeTime * 5) {
            IsAphrodisiac = (pos) => Terrain.I.IsFeature(pos, out Feature feature)
                && feature.config == FeatureLibrary.C.woodPile
                && feature.ResourceQuantity == FeatureLibrary.C.woodPile.prefab.GetComponentStrict<Woodpile>().maxPile
        };
    }

    // TODO move this to Brain, every creature will have it
    override public Optional<Transform> FindFocus() => resource.Has() ? Will.NearestThreat(this) : Optional<Transform>.Empty();

    public int Cost(Terrain.Position pos) => pos.grid == Terrain.Grid.Roof ? 4 : 1;
}