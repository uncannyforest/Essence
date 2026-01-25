using System;
using System.Collections.Generic;
using UnityEngine;

// very similar to Lark, but without much variation between species
public class Reproduction  {
    private readonly Brain brain;

    public Reproduction(Brain brain) {
        this.brain = brain;
    }

    virtual public Optional<IEnumerator<YieldInstruction>> ScanForAphrodisiac() {
        Optional<Terrain.Position> target = from v in Radius.Nearby.ClosestTo(brain.transform.position, brain.Habitat.IsAphrodisiac)
                                            select new Terrain.Position(Terrain.Grid.Roof, v);
        if (!target.HasValue) return Optional.Empty<IEnumerator<YieldInstruction>>();
        Vector2Int nest = FindIdealNestLocation();
        return Optional.Of(brain.pathfinding.ApproachThenInteract((_) => true, LayEgg, true) // grant exp for laying eggs
            .Enumerator(new Terrain.Position(Terrain.Grid.Roof, nest)));
    }

    private void LayEgg(Terrain.Position tile) {
        // TODO find valid location first - this is gonna throw errors often
        Feature f = Terrain.I.BuildFeature(tile.Coord, FeatureLibrary.C.egg);
        f.hooks.GetComponentStrict<Egg>().species = brain.creature.creatureName;
        f.hooks.GetComponentStrict<Egg>().Team = 0;
    }

    private Vector2Int FindIdealNestLocation() {
        Optional<Vector2Int> loc = Radius.Nearby.ClosestTo(brain.transform.position, HasWalls(3));
        if (!loc.HasValue) loc = Radius.Nearby.ClosestTo(brain.transform.position, (t) => Terrain.I.GetLand(t) == Land.Forest);
        if (!loc.HasValue) loc = Radius.Nearby.ClosestTo(brain.transform.position, HasWalls(2));
        if (!loc.HasValue) loc = Radius.Nearby.ClosestTo(brain.transform.position, HasWalls(1));
        if (!loc.HasValue) loc = Radius.Nearby.ClosestTo(brain.transform.position, HasWalls(0));
        if (!loc.HasValue) {
            Debug.LogWarning("No nest location??? What about the square we're standing on? " + brain.creature);
            return Terrain.I.CellAt(brain.transform.position);
        }
        return loc.Value;
    }

    private Func<Vector2Int, bool> HasWalls(int numWallTarget) {
        return (tile) => {
            if (!FeatureLibrary.C.egg.IsValidTerrain(tile)) return false;
            int numWalls = 0;
            if (!(Terrain.I.GetLand(tile + Vct.I(1, 0)) ?? Terrain.I.Depths).IsPassable()
                    || Terrain.I.GetConstruction(Terrain.Position.Edge(tile, 0)) != Construction.None)
                numWalls++;
            if (!(Terrain.I.GetLand(tile + Vct.I(0, 1)) ?? Terrain.I.Depths).IsPassable()
                    || Terrain.I.GetConstruction(Terrain.Position.Edge(tile, 1)) != Construction.None)
                numWalls++;
            if (!(Terrain.I.GetLand(tile + Vct.I(-1, 0)) ?? Terrain.I.Depths).IsPassable()
                    || Terrain.I.GetConstruction(Terrain.Position.Edge(tile, 2)) != Construction.None)
                numWalls++;
            if (!(Terrain.I.GetLand(tile + Vct.I(0, -1)) ?? Terrain.I.Depths).IsPassable()
                    || Terrain.I.GetConstruction(Terrain.Position.Edge(tile, 3)) != Construction.None)
                numWalls++;
            return numWalls == numWallTarget;
        };
    }
}
