using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MooseConfig {
    public Sprite attackAction;
    public float destroyDistance;
    public float destroyTime;
    public int attack;
}

[RequireComponent(typeof(Health))]
public class Moose : Species<MooseConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new MooseBrain(this, generalConfig, speciesConfig);
    }
}

public class MooseBrain : Brain {
    private MooseConfig moose;

    public MooseBrain(Moose species, BrainConfig general, MooseConfig moose) : base(species, general) {
        this.moose = moose;

        Actions = new List<CreatureAction>() {
            CreatureAction.WithObject(moose.attackAction,
                new PathTracingBehavior.Targeted(transform, IsDestroyable, (pos) => ApproachAndAttack(pos).NextOrDefault()),
                new TeleFilter(TeleFilter.Terrain.TILES, null)
                    .WithLine(GetDestinationsForDisplay))
        };

        Habitat = new Habitat(this, Radius.Inside) {
            IsShelter = (loc) => terrain.Land[loc] == Land.Shrub
        };
    }

    private List<Vector3> GetDestinationsForDisplay() => 
        (state.command?.executeDirective as PathTracingBehavior)?.DestinationsForDisplay;

    private bool IsDestroyable(Terrain.Position location) =>
        Will.CanClearObstacleAt(general, location);

    override public IEnumerator<YieldInstruction> FocusedBehavior() => ApproachAndAttack((Terrain.Position)state.terrainFocus?.location);

    override public IEnumerator<YieldInstruction> UnblockSelf(Terrain.Position location) => ApproachAndAttack(location);

    private IEnumerator<YieldInstruction> ApproachAndAttack(Terrain.Position l) =>
        from location in Continually.For(l)
        where resource.Has()
        select pathfinding.Approach(terrain.CellCenter(location), moose.destroyDistance)
            .Then(pathfinding.FaceAnd("Attack", terrain.CellCenter(location), () => Attack(location)));
    
    private YieldInstruction Attack(Terrain.Position location) {
        if (location.grid != Terrain.Grid.Roof) {
            terrain[location] = Construction.None;
            creature.GenericExeSucceeded();
            resource.Use();
        } else if (terrain.GetLand(location.Coord) == Land.Dirtpile) {
            terrain.Land[location.Coord] = Land.Grass;
            creature.GenericExeSucceeded();
            resource.Use();
        } else if (terrain.Feature[location.Coord] != null) {
            terrain.AttackFeature(location.Coord, 10, creature.stats.Str);
            resource.Use();
        }
        return new WaitForSeconds(moose.destroyTime);
    }
}
