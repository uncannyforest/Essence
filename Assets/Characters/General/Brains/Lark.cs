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
    protected Brain brain;
    protected Func<bool> precondition;
    protected Func<Vector2Int, WhyNot> criterion;
    protected Radius scanRadius;
    protected Action<Terrain.Position> action;

    public Lark(Brain brain, Func<bool> precondition, Func<Vector2Int, WhyNot> criterion, Radius scanRadius, Action<Terrain.Position> action) {
        this.brain = brain;
        this.precondition = precondition;
        this.criterion = criterion;
        this.scanRadius = scanRadius;
        this.action = action;
    }

    // In subclasses, for a more sophisticated lark action,
    // create a method: private IEnumerator<YieldInstruction> DoLark(Terrain.Position target) 
    // and thus here: return from p in target select DoLark(p)
    virtual public Optional<IEnumerator<YieldInstruction>> ScanForLark() {
        if (!precondition()) return Optional.Empty<IEnumerator<YieldInstruction>>();
        Optional<Terrain.Position> target = from v in scanRadius.ClosestTo(brain.transform.position, criterion.Silence())
                                            select new Terrain.Position(Terrain.Grid.Roof, v);
        return from p in target select brain.pathfinding.Terraform(criterion.Coord(), action).Enumerator(p);
    }

    public static Lark None() => new Lark(null, () => false, null, Radius.Inside, null);
}