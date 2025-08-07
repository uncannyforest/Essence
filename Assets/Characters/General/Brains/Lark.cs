using System;
using System.Collections.Generic;
using UnityEngine;

// A Lark is like a terrain Focus but executed in low-priority Roam state.
// A Creature's Focus is usually either a Character or terrain that is an Obstacle to some Character.
// If a Creature is terraforming terrain not by request of the Player (Execute, priority = 600)
// or another Creature (Focus, priority = 410), that is usually low priority,
// and happens in the Roam state (PassiveCommand, priority = 100) using the Lark class.
// See CreatureStateType in Habit.cs for full list of priority levels.
public class Lark {
    private Brain brain;
    private Func<bool> precondition;
    private Func<Vector2Int, bool> criterion;
    private Radius radius;
    private float interactionDistance;
    private Action<Terrain.Position> action;

    public Lark(Brain brain, Func<bool> precondition, Func<Vector2Int, bool> criterion, Radius radius, float interactionDistance, Action<Terrain.Position> action) {
        this.brain = brain;
        this.precondition = precondition;
        this.criterion = criterion;
        this.radius = radius;
        this.interactionDistance = interactionDistance;
        this.action = action;
    }

    public Lark(Brain brain, Func<bool> precondition, Func<Target, WhyNot> mainErrorFilter, Radius radius, float interactionDistance, Action<Terrain.Position> action) {
        this.brain = brain;
        this.precondition = precondition;
        this.criterion = ConvertFilter(mainErrorFilter);
        this.radius = radius;
        this.interactionDistance = interactionDistance;
        this.action = action;
    }

    virtual public Optional<IEnumerator<YieldInstruction>> ScanForLark() {
        if (!precondition()) return Optional.Empty<IEnumerator<YieldInstruction>>();
        Optional<Terrain.Position> target = from v in radius.ClosestTo(brain.transform.position, criterion)
                                            select new Terrain.Position(Terrain.Grid.Roof, v);
        return from p in target select brain.pathfinding.ApproachThenTerraform(p, interactionDistance, action);
    }

    private static Func<Vector2Int, bool> ConvertFilter(Func<Target, WhyNot> mainErrorFilter) =>
        (v) => (bool)mainErrorFilter(new Target(new Terrain.Position(Terrain.Grid.Roof, v)));

    public static Lark None() => new Lark(null, () => false, null, Radius.Inside, 0, null);
}