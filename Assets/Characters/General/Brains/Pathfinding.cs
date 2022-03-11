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

    public void MoveToward(Vector3 target) =>
        movement.InDirection(IndexedVelocity(Disp.FT(transform.position, target)));

    public YieldInstruction Roam() {
        if (Random.value < general.roamRestingFraction) movement.Idle();
        else movement.InDirection(RandomVelocity());
        return new WaitForSeconds(Random.value * general.reconsiderRateRoam);
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
        Displacement targetDirection = FollowTargetDirection(followDirective.position);
        movement.InDirection(IndexedVelocity(targetDirection));
        return new WaitForSeconds(Random.value * general.reconsiderRateFollow);
    }

    public YieldInstruction TypicalWait { get => new WaitForSeconds(general.reconsiderRateTarget); }

    public YieldInstruction ApproachThenIdle(Vector2 target, float proximityToStop) {
        return Approach(target, proximityToStop).Else(() => { movement.Idle(); return TypicalWait; });
    }

    public Optional<YieldInstruction> Approach(Vector2 target, float proximityToStop) {
        float distance = Vector2.Distance(target, transform.position);
        if (distance <= proximityToStop) {
            return Optional<YieldInstruction>.Empty();
        } else {
            movement.InDirection(IndexedVelocity(Disp.FT(transform.position, target)));
            if (distance < movement.Speed * general.reconsiderRateTarget) return Optional<YieldInstruction>.Of(null); // adjust faster when we're close
            else return Optional.Of(TypicalWait);
        }
    }

    public ApproachThenBuild ApproachThenBuild(float buildDistance, float buildTime, Action<Terrain.Position> buildAction)
        => new ApproachThenBuild(brain, buildDistance, buildTime, buildAction);

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
}

public class ApproachThenBuild : TargetedBehavior<Terrain.Position> {
    private readonly Brain brain;
    private readonly float buildDistance;
    private readonly float buildTime;
    private readonly Action<Terrain.Position> buildAction;

    public ApproachThenBuild (
            Brain brain,
            float buildDistance,
            float buildTime,
            Action<Terrain.Position> buildAction) {
        this.brain = brain;
        this.buildDistance = buildDistance;
        this.buildTime = buildTime;
        this.buildAction = buildAction;
        this.enumeratorWithParam = E;
    }

    public IEnumerator E(Terrain.Position buildLocation) {
        for (int i = 0; i < 100_000; i++) {
            Optional<YieldInstruction> approaching = brain.pathfinding.Approach(Terrain.I.CellCenter(buildLocation), buildDistance);
            if (approaching.HasValue) yield return approaching.Value;
            else {
                buildAction(buildLocation);
                yield return new WaitForSeconds(buildTime);
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