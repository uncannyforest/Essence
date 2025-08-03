using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class AxeConfig {
    public Sprite attackAction;
    public float meleeReach;
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
}

public class AxeBrain : Brain {
    private AxeConfig axe;

    public AxeBrain(Species species, BrainConfig general, AxeConfig axe) : base(species, general) {
        this.axe = axe;

        MainBehavior = new FlexTargetedBehavior(AttackBehavior,
            new TeleFilter(
                TeleFilter.Terrain.TILES,
                (c) => Will.IsThreat(teamId, transform.position, c).NegLog(legalName + " cannot select " + c)),
            (c) => IsValidIfTerrain(c, LandCats.PLANTY) && SufficientResource());

        Actions = new List<CreatureAction>() {
            MainBehavior.CreatureAction(axe.attackAction)
        };

        Habitat = new SleepHabitat(this, Habitat.InteractionMode.Inside) {
            IsShelter = (coord) =>
                terrain.Feature[coord + new Vector2Int(-1, -1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(-1, 0)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(-1, 1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(0, -1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(0, 1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(1, -1)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(1, 0)]?.config == FeatureLibrary.C.woodPile &&
                terrain.Feature[coord + new Vector2Int(1, 1)]?.config == FeatureLibrary.C.woodPile
        };
    }

    override public Optional<Transform> FindFocus() => resource.Has() ? Will.NearestThreat(this) : Optional<Transform>.Empty();

    private IEnumerator<YieldInstruction> AttackBehavior(Target f) {
        if (f.Is(out Character c)) {
            return
                from focus in Continually.For(c.transform)
                where IsValidFocus(focus)                                   .NegLog(legalName + " focus " + focus + " no longer valid")
                select pathfinding.Approach(focus, axe.meleeReach)
                    .Then(() => pathfinding.FaceAnd("Attack", focus, Attack));
        } else if (f.Is(out Terrain.Position pos)) {
            return
                pathfinding.ApproachThenInteract(1.5f, () => creature.stats.ExeTime,
                    (loc) => {
                        resource.Use(1);
                        Axe.ChopWood(loc.Coord);
                    }).E(pos);
        } else {
            throw new ArgumentException("Called AttackBehavior with empty target");
        }
    }

    private void Attack(Transform target) {
        Melee(target);
        Vector2Int location = terrain.CellAt(target.position);
        if (terrain.GetLand(location)?.IsPlanty() == true) Axe.ChopWood(location);
    }
}
