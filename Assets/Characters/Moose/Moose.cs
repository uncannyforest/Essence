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
    }

    override public bool CanTame(Transform player) => true;

    public override bool ExtractTamingCost(Transform player) => true;

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.WithTerrain(moose.attackAction,
                new PathTracingBehavior.Targeted(transform, IsDestroyable, ApproachAndAttack),
                TeleFilter.Terrain.TILES)
        };
    }

    private bool IsDestroyable(Terrain.Position location) {
        if (location.grid != Terrain.Grid.Roof) {
            Construction? wall = terrain.GetConstruction(location);
            return wall != null && wall != Construction.None;
        }
        else if (terrain.GetLand(location.Coord) == Land.Dirtpile) return true;
        else if (terrain.Feature[location.Coord] != null)
            return terrain.Feature[location.Coord].GetComponent<Fountain>() == null;
        else return false;
    }

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
