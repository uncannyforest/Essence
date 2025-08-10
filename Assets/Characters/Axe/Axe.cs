using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AxeConfig {
    public Sprite chopWoodAction;
    public Sprite attackAction;
}

[RequireComponent(typeof(Health))]
public class Axe : Species<AxeConfig> {
    public override Brain Brain(BrainConfig generalConfig) {
        return new AxeBrain(this, generalConfig, speciesConfig);
    }

    public static Feature ChopWood(Vector2Int coord) {
        Land? land = Terrain.I.GetLand(coord);
        if (land?.IsPlanty() != true) throw new NotSupportedException("Do not call ChopWood unless you are confident tile is planty");
        int woodQuantity = land == Land.Meadow ? 1 : land == Land.Shrub ? 3 : 5;
        return Terrain.I.SetUpFeature(coord, Land.Grass, FeatureLibrary.C.woodPile, woodQuantity);
    }

    public static Feature ChopWood(Terrain.Position pos) {
        if (pos.grid != Terrain.Grid.Roof) throw new NotSupportedException("Do not call ChopWood on a wall");
        return ChopWood(pos.Coord);
    }
}

public class AxeBrain : Brain {
    private AxeConfig axe;

    public AxeBrain(Species species, BrainConfig general, AxeConfig axe) : base(species, general) {
        this.axe = axe;

        MainBehavior = new FlexTargetedBehavior(this,
            characterBehavior: AttackBehavior,
            terrainAction: (pos) => Axe.ChopWood(pos),
            silentFilter: new TeleFilter(
                TeleFilter.Terrain.TILES,
                (c) => Will.IsThreat(teamId, c)),
            errorFilter: (target) => SufficientResource()
                && target.IfCharacter((c) => Will.CanSee(transform.position, c.transform))
                && IsValidIfTerrain(target, LandCats.PLANTY));

        Lark = MainBehavior.Lark(() => Habitat?.IsPresent(Radius.Nearby) != true,  Radius.Nearby);

        Actions = new List<CreatureAction>() {
            ((FlexTargetedBehavior)MainBehavior).CreatureActionTerrain(axe.chopWoodAction),
            MainBehavior.CreatureActionCharacter(axe.attackAction)
        };

        Habitat = new Habitat(this, Radius.Inside) {
            IsShelter = (coord) =>
                terrain.Feature[coord + new Vector2Int(-1, -1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(-1, 0)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(-1, 1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(0, -1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(0, 1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(1, -1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(1, 0)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(1, 1)]?.config == FeatureLibrary.C.woodPile};
        Habitat.RestBehavior = Habitat.RestBehaviorSleep;
    }

    override public Optional<Transform> FindFocus() => resource.Has() ? Will.NearestThreat(this) : Optional<Transform>.Empty();

    private IEnumerator<YieldInstruction> AttackBehavior(Transform character) 
        => from focus in Continually.For(character)
            where IsValidFocus(focus)                                   .NegLog(legalName + " focus " + focus + " no longer valid")
            select pathfinding.Approach(focus, GlobalConfig.I.defaultMeleeReach)
                .Then(() => pathfinding.FaceAnd("Attack", focus, Attack));

    private void Attack(Transform target) {
        Melee(target);
        Vector2Int location = terrain.CellAt(target.position);
        if (terrain.GetLand(location)?.IsPlanty() == true) Axe.ChopWood(location);
    }
}
