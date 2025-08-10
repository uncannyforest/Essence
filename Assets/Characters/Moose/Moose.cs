using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MooseConfig {
    public Sprite attackAction;
    public Sprite destroyAction;
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

        MainBehavior = new CharacterTargetedBehavior(this,
            AttackCharacterBehavior,
            (c) => Will.IsThreat(teamId, c),
            (c) => SufficientResource() && Will.CanSee(transform.position, c));

        Actions = new List<CreatureAction>() {
            CreatureAction.WithObject(moose.destroyAction,
                new PathTracingBehavior.Targeted(transform, IsDestroyable, (pos) => ApproachAndDestroy(pos).NextOrDefault()),
                new TeleFilter(TeleFilter.Terrain.TILES, null)
                    .WithLine(GetDestinationsForDisplay)),
            MainBehavior.CreatureActionCharacter(moose.attackAction)
        };

        Habitat = new Habitat(this, Radius.Inside) {
            IsShelter = (loc) => terrain.Land[loc] == Land.Shrub
        };
    }

    private List<Vector3> GetDestinationsForDisplay() => 
        (state.command?.executeDirective as PathTracingBehavior)?.DestinationsForDisplay;

    private bool IsDestroyable(Terrain.Position location) =>
        Will.CanClearObstacleAt(general, location);

    override public Optional<Transform> FindFocus() => resource.Has() ? Will.NearestThreat(this) : Optional<Transform>.Empty();

    override public IEnumerator<YieldInstruction> FocusedBehavior() =>
        FlexTargetedBehavior.MuxFocus(state, AttackCharacterBehavior, ApproachAndDestroy);

    override public IEnumerator<YieldInstruction> UnblockSelf(Terrain.Position location) => ApproachAndDestroy(location);

    private IEnumerator<YieldInstruction> AttackCharacterBehavior(Transform f) =>
        from focus in Continually.For(f)
        where IsValidFocus(focus)                                   .NegLog(legalName + " focus " + focus + " no longer valid")
        select pathfinding.Approach(focus, GlobalConfig.I.defaultMeleeReach)
            .Then(() => pathfinding.FaceAnd("Attack", focus, Melee));

    private IEnumerator<YieldInstruction> ApproachAndDestroy(Terrain.Position l) =>
        from location in Continually.For(l)
        where resource.Has()
        select pathfinding.Approach(terrain.CellCenter(location), moose.destroyDistance)
            .Then(pathfinding.FaceAnd("Attack", terrain.CellCenter(location), () => DestroyTerrain(location)));
    
    private YieldInstruction DestroyTerrain(Terrain.Position location) {
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
