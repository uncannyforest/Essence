using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MooseMath {

    public static IEnumerable<Terrain.Position> GetPathPositions(Vector2 worldStart, Vector2 worldEnd) {        
        Vector2Int currentCell = Terrain.I.CellAt(worldStart);

        Vector2 start = GridFromWorld(worldStart);
        Vector2 end = GridFromWorld(worldEnd);
        Vector2 direction = end - start;
        int xDirection = direction.x > 0 ? 1 : -1;
        Vector2Int xTransition = Vct.I(xDirection == 1 ? 1 : 0, 0);
        int yDirection = direction.y > 0 ? 1 : -1;
        Vector2Int yTransition = Vct.I(0, yDirection == 1 ? 1 : 0);
        int xi = direction.x > 0 ? Mathf.CeilToInt(start.x) : Mathf.FloorToInt(start.x); // integer edge
        int yi = direction.y > 0 ? Mathf.CeilToInt(start.y) : Mathf.FloorToInt(start.y); // integer edge
        float xt = (xi - start.x) / direction.x; // lerp [0, 1]
        float yt = (yi - start.y) / direction.y; // lerp [0, 1]
        if (direction.x == 0) xt = 1; // skip
        if (direction.y == 0) yt = 1; // skip
        while (xt < 1 || yt < 1) { // queue up smallest of xt and yt
            if (xt < yt) {
                yield return new Terrain.Position(Terrain.Grid.YWalls, currentCell + xTransition);
                currentCell += Vct.I(xDirection, 0);
                yield return new Terrain.Position(Terrain.Grid.Roof, currentCell);
                xi += xDirection;
                xt = (xi - start.x) / direction.x;
            } else if (yt < xt) {
                yield return new Terrain.Position(Terrain.Grid.XWalls, currentCell + yTransition);
                currentCell += Vct.I(0, yDirection);
                yield return new Terrain.Position(Terrain.Grid.Roof, currentCell);
                yi += yDirection;
                yt = (yi - start.y) / direction.y;
            } else {
                foreach (Terrain.Position wall in Randoms.Order<Terrain.Position>(
                    new Terrain.Position(Terrain.Grid.YWalls, currentCell + xTransition),
                    new Terrain.Position(Terrain.Grid.XWalls, currentCell + yTransition)
                )) yield return wall;
                foreach (Terrain.Position corner in Randoms.Order<Terrain.Position>(
                    new Terrain.Position(Terrain.Grid.Roof, currentCell + Vct.I(xDirection, 0)),
                    new Terrain.Position(Terrain.Grid.Roof, currentCell + Vct.I(0, yDirection))
                )) yield return corner;
                foreach (Terrain.Position wall in Randoms.Order<Terrain.Position>(
                    new Terrain.Position(Terrain.Grid.XWalls, currentCell + Vct.I(xDirection, 0) + yTransition),
                    new Terrain.Position(Terrain.Grid.YWalls, currentCell + Vct.I(0, yDirection) + xTransition)
                )) yield return wall;
                currentCell += Vct.I(xDirection, yDirection);
                yield return new Terrain.Position(Terrain.Grid.Roof, currentCell);
                xi += xDirection;
                xt = (xi - start.x) / direction.x;
                yi += yDirection;
                yt = (yi - start.y) / direction.y;
            }
        }

        yield break;
    }

    public static Vector2 GridFromWorld(Vector2 world) => world;

    public static Vector2 WorldFromGrid(Vector2 grid) => grid;

}

public class PathTracingBehavior : BehaviorNode {
    private Vector2 previousDestination;
    private Queue<Vector2> destinations = new Queue<Vector2>();
    private Transform ai;
    private Func<Terrain.Position, bool> positionFilter; // which tile types we can interact with
    private Func<Terrain.Position, YieldInstruction> positionAction;

    private Terrain.Position positionFocus;

    public PathTracingBehavior() {
        this.enumerator = QueueEnumerator;
    }

    // Create a PathTracingBehavior with one target.
    // These may be combined afterward through UpdateWithNewBehavior.
    public static PathTracingBehavior Of(
            Vector2 target,
            Transform ai,
            Func<Terrain.Position, bool> positionFilter,
            Func<Terrain.Position, YieldInstruction> positionAction) {
        PathTracingBehavior queueNode = new PathTracingBehavior();
        queueNode.destinations.Enqueue(target);
        queueNode.ai = ai;
        queueNode.positionFilter = positionFilter;
        queueNode.positionAction = positionAction;
        return queueNode;
    }

    // Overrides "replace" with "chain" command when player Confirms
    // Receives a PathTracingBehavior (if not - replace, don't chain)
    // Takes all destinations from it and enqueues them into this one.
    override public BehaviorNode UpdateWithNewBehavior(BehaviorNode newNode) {
        if (newNode is PathTracingBehavior newQueue) {
            foreach (Vector2 destination in newQueue.destinations) {
                Debug.Log("PTB destinations.Count " + destinations.Count + ", enqueuing new one");
                destinations.Enqueue(destination);
            }
            return this;
        }
        else return newNode;
    }

    private bool Pop() {
        destinations.Dequeue();
        return (destinations.Count > 0);
    }

    private IEnumerator QueueEnumerator() {
        previousDestination = ai.position;
        int i = 0;
        while (destinations.Count > 0) {
            Vector2 nextDestination = destinations.Peek();
            foreach (Terrain.Position pathPos in MooseMath.GetPathPositions(previousDestination, nextDestination)) {
                Debug.Log("PTB possible position focus: " + pathPos + " near " + Terrain.I.CellCenter(pathPos.Coord));
                while (positionFilter(pathPos)) {
                    positionFocus = pathPos;
                    Debug.Log("PTB FOUND position focus: " + pathPos + " near " + Terrain.I.CellCenter(pathPos.Coord));
                    Debug.DrawLine(nextDestination, Terrain.I.CellCenter(positionFocus), Color.blue, 1);
                    yield return positionAction(positionFocus);
                    if (i++ > 1000) throw new StackOverflowException();
                }
                if (i++ > 1000) throw new StackOverflowException();
            }
            previousDestination = nextDestination;
            Pop();
            Debug.Log("PTB reached destination, popping; " + destinations.Count + " left");
            if (i++ > 1000) throw new StackOverflowException();
        }
    }

    public List<Vector3> DestinationsForDisplay {
        get {
            if (destinations.Count == 0 || previousDestination == null) return null;
            List<Vector3> result = new List<Vector3>();
            result.Add(previousDestination);
            foreach (Vector2 destination in destinations) result.Add(destination);
            return result;
        }
    }

    public class Targeted : TargetedBehavior<Target> {
        private Transform ai;
        private Func<Terrain.Position, bool> positionFilter;
        private Func<Terrain.Position, YieldInstruction> positionAction;

        public Targeted(Transform ai, Func<Terrain.Position, bool> positionFilter, Func<Terrain.Position, YieldInstruction> positionAction) {
            this.ai = ai;
            this.positionFilter = positionFilter;
            this.positionAction = positionAction;
            this.canQueue = true;
        }

        override public BehaviorNode WithTarget(Target target) =>
            PathTracingBehavior.Of(Terrain.I.CellCenter((Terrain.Position)target), ai, positionFilter, positionAction);
    }
}
