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
                new PathTracingBehavior.Targeted(transform, IsDestroyable, ApproachAndAttack),
                new TeleFilter(TeleFilter.Terrain.TILES, null)
                    .WithLine(GetDestinationsForDisplay))
        };

        Habitat = new Habitat(this, Habitat.InteractionMode.Inside) {
            IsShelter = (loc) => terrain.Land[loc] == Land.Ditch
        };
    }

    override public bool ExtractTamingCost(Transform player) => CanTame(player);
    override public bool CanTame(Transform player) => Habitat.CanTame();

    private List<Vector3> GetDestinationsForDisplay() => 
        (state.command?.executeDirective as PathTracingBehavior)?.DestinationsForDisplay;

    private bool IsDestroyable(Terrain.Position location) =>
        Will.CanClearObstacleAt(general, location);

    override public IEnumerator FocusedBehavior() {
        while (true) {
            yield return ApproachAndAttack((Terrain.Position)state.terrainFocus?.location);
        }
    }

    override public YieldInstruction UnblockSelf(Terrain.Position location) => ApproachAndAttack(location);

    private YieldInstruction ApproachAndAttack(Terrain.Position location)
        => pathfinding.Approach(terrain.CellCenter(location), moose.destroyDistance)
            .Else(pathfinding.FaceAnd("Attack", terrain.CellCenter(location), () => Attack(location)));
    
    private YieldInstruction Attack(Terrain.Position location) {
        if (location.grid != Terrain.Grid.Roof) {
            terrain[location] = Construction.None;
        } else if (terrain.GetLand(location.Coord) == Land.Dirtpile) {
            terrain.Land[location.Coord] = Land.Grass;
        } else if (terrain.Feature[location.Coord] != null) {
            terrain.Feature[location.Coord].Attack(transform);
        }
        return new WaitForSeconds(moose.destroyTime);
    }
}
