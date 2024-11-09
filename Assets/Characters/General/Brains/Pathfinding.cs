using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Pathfinding {
    public enum AIDirections {
        Infinite,
        Four,
        Eight,
        Twelve
    }
    public static Dictionary<AIDirections, Displacement[]> AIDirectionVectors = new Dictionary<AIDirections, Displacement[]>() {
        [AIDirections.Four] = new Displacement[] {
            new Displacement(0.7071067812f, 0.7071067812f),
        },
        [AIDirections.Eight] = new Displacement[] {
            new Displacement(1f, 0f),
            new Displacement(0.7071067812f, 0.7071067812f),
        },
        [AIDirections.Twelve] = new Displacement[] {
            new Displacement(1f, 0.2679491924f), // tan(15)
            new Displacement(0.7071067812f, 0.7071067812f),
            new Displacement(0.2679491924f, 1f)
        }
    };

    public readonly Brain brain;
    public Pathfinding(Brain brain) {
        this.brain = brain;
    }
    private BrainConfig general { get => brain.general; }
    private CharacterController movement { get => brain.movement; }
    private Transform transform { get => brain.transform; }

    private Displacement[] aiDirections { get => AIDirectionVectors[general.numMovementDirections]; }

    private Displacement RandomVelocity() {
        Displacement randomFromList = aiDirections[Random.Range(0, aiDirections.Length)];
        return Randoms.RightAngleRotation(randomFromList);
    }
    
    private Displacement IndexedVelocity(Displacement targetDirection) {
        // round instead of floor if aiDirections.Length were even.
        int index = Mathf.FloorToInt((targetDirection.angle + 360) % 360 / (90 / aiDirections.Length));
        int rotation = index / aiDirections.Length;
        int subIndex = index % aiDirections.Length;
        return aiDirections[subIndex].RotateRightAngles(rotation);
    }

    public void MoveTowardWithoutClearingObstacles(Vector3 target) {
        CheckTargetForObstacles(target, 0);
        movement.InDirection(IndexedVelocity(Disp.FT(transform.position, target)));
    }

    public YieldInstruction Roam() {
        if (Random.value < general.roamRestingFraction) movement.Idle();
        else movement.InDirection(RandomVelocity());
        return new WaitForSeconds(Random.value * general.reconsiderMaxRateRoam);
    }

    private Displacement FollowTargetDirection(Vector3 targetPosition) {
        Displacement toTarget = Disp.FT(transform.position, targetPosition);

        Optional<Transform> nearestThreat = Will.NearestThreat(brain);
        if (!nearestThreat.HasValue) return toTarget;
        Displacement toThreat = Disp.FT(transform.position, nearestThreat.Value.position);
        Displacement toThreatCorrected = toThreat * toTarget.sqrMagnitude / toThreat.sqrMagnitude * general.timidity;
        return toTarget - toThreatCorrected;
    }
    public YieldInstruction Follow(Transform followDirective) {
        if (CheckTargetForObstacles(followDirective.position, 0).IsValue(out YieldInstruction unblockSelf))
            return unblockSelf;
        Displacement targetDirection = FollowTargetDirection(followDirective.position);
        movement.InDirection(IndexedVelocity(targetDirection));
        return new WaitForSeconds(Random.value * general.reconsiderRateFollow);
    }

    public YieldInstruction TypicalWait { get => new WaitForSeconds(brain.creature.stats.ExeTime); }

    public YieldInstruction ApproachThenIdle(Vector2 target, float proximityToStop = 1f / CharacterController.subGridUnit) {
        return ApproachIfFar(target, proximityToStop).Else(() => { movement.Idle(); return TypicalWait; });
    }

    public Optional<YieldInstruction> ApproachIfFar(Vector2 target, float proximityToStop = 1f / CharacterController.subGridUnit) {
        if (CheckTargetForObstacles(target, proximityToStop).IsValue(out YieldInstruction unblockSelf))
            return Optional.Of(unblockSelf);
        float distance = Vector2.Distance(target, transform.position);
        if (distance <= proximityToStop) {
            return Optional<YieldInstruction>.Empty();
        } else {
            movement.InDirection(IndexedVelocity(Disp.FT(transform.position, target)));
            if (distance < movement.Speed * brain.creature.stats.ExeTime) return Optional<YieldInstruction>.Of(null); // adjust faster when we're close
            else return Optional.Of(TypicalWait);
        }
    }

    public ApproachThenInteract ApproachThenInteract(float interactionDistance, float interactionTime, Action<Terrain.Position> interaction, bool rewardExp = true)
        => new ApproachThenInteract(brain, interactionDistance, interactionTime, interaction, rewardExp);

    public Func<YieldInstruction> FaceAnd(string animationTrigger,
            Vector2 location,
            Func<YieldInstruction> finalAction) => () => {
        movement.IdleFacing(location);
        if (animationTrigger != null) movement.Trigger(animationTrigger);
        return finalAction();
    };

    public Func<YieldInstruction> FaceAnd(string animationTrigger,
            Vector2 location,
            Action finalAction) => () => {
        movement.IdleFacing(location);
        if (animationTrigger != null) movement.Trigger(animationTrigger);
        finalAction();
        return TypicalWait;
    };

    public Optional<YieldInstruction> CheckTargetForObstacles(Vector2 target, float exceptWithinRadius) {
        if (IdentifyObstacles(target) is DesireMessage.Obstacle obstacle &&
                Disp.FT(target, Terrain.I.CellCenter(obstacle.location)) > exceptWithinRadius) {
            if (Will.CanClearObstacleAt(brain.general, obstacle.location)) {
                return Optional.Of(brain.UnblockSelf(obstacle.location));
            } else BroadcastObstacle(obstacle);
        }
        return Optional<YieldInstruction>.Empty();
    }

    private DesireMessage.Obstacle? IdentifyObstacles(Vector2 target) {
        if (!Terrain.I.InBounds(Terrain.I.CellAt(target)))
            throw new InvalidOperationException("Target out of bounds");
        foreach (Terrain.Position position in
                MooseMath.GetPathPositions(brain.transform.position, target)) {
            if (position.grid == Terrain.Grid.XWalls || position.grid == Terrain.Grid.YWalls) {
                if (Terrain.I[position] != Construction.None) {
                    return new DesireMessage.Obstacle() {
                        requestor = brain.creature,
                        location = position,
                        wallObstacle = Terrain.I[position]
                    };
                }
            } else {
                Land? land = Terrain.I.GetLand(position.Coord);
                if (land?.IsPassable() == false || (movement.waterSpeed == 0 && land?.IsWatery() == true)) {
                    return new DesireMessage.Obstacle() {
                        requestor = brain.creature,
                        location = position,
                        landObstacle = land
                    };
                }
            }
        }
        return null;
    }

    private void BroadcastObstacle(DesireMessage.Obstacle obstacle) {
        int count = brain.team.Broadcast(new DesireMessage() { obstacle = obstacle });
        if (count == 0 && brain.teamId == 0)
            HiveMind.I.RegisterObstacle(brain.creature, obstacle);
    }
}

public class ApproachThenInteract : TargetedBehavior<Terrain.Position> {
    private readonly Brain brain;
    private readonly float interactDistance;
    private readonly float interactTime;
    private readonly Action<Terrain.Position> interaction;
    private readonly bool rewardExp;

    public ApproachThenInteract (
            Brain brain,
            float interactDistance,
            float interactTime,
            Action<Terrain.Position> interaction,
            bool rewardExp) {
        this.brain = brain;
        this.interactDistance = interactDistance;
        this.interactTime = interactTime;
        this.interaction = interaction;
        this.rewardExp = rewardExp;
        this.enumeratorWithParam = E;
    }

    public IEnumerator E(Terrain.Position location) {
        for (int i = 0; i < 100_000; i++) {
            if (brain.pathfinding.CheckTargetForObstacles(Terrain.I.CellCenter(location), interactDistance)
                    .IsValue(out YieldInstruction unblockSelf)) {
                yield return unblockSelf;
                continue;
            }
            Optional<YieldInstruction> approaching = brain.pathfinding.ApproachIfFar(Terrain.I.CellCenter(location), interactDistance);
            Debug.Log(approaching);
            if (approaching.HasValue) yield return approaching.Value;
            else {
                brain.movement.IdleFacing(Terrain.I.CellCenter(location));
                interaction(location);
                if (rewardExp) brain.creature.GenericExeSucceeded();
                yield return new WaitForSeconds(interactTime);
                yield break;
            }
        }
    }

    public TargetedBehavior<Vector2Int> ForVector2Int() => new TargetedBehavior<Vector2Int>(
        (target) => enumeratorWithParam(new Terrain.Position(Terrain.Grid.Roof, target))
    );
    public TargetedBehavior<Target> ForTarget() => new TargetedBehavior<Target>(
        (target) => enumeratorWithParam((Terrain.Position)target)
    );
}