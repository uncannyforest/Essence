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
    public static Dictionary<AIDirections, Vector2[]> AIDirectionVectors = new Dictionary<AIDirections, Vector2[]>() {
        [AIDirections.Four] = new Vector2[] {
            Vct.F(0.7071067812f, 0.7071067812f),
        },
        [AIDirections.Twelve] = new Vector2[] {
            Vct.F(1f, 0.2679491924f), // tan(15)
            Vct.F(0.7071067812f, 0.7071067812f),
            Vct.F(0.2679491924f, 1f)
        }
    };

    private Brain brain;
    public Pathfinding(Brain brain) {
        this.brain = brain;
    }
    private BrainConfig general { get => brain.general; }
    private CharacterController movement { get => brain.movement; }
    private Transform transform { get => brain.transform; }

    private Vector2[] aiDirections { get => AIDirectionVectors[general.numMovementDirections]; }

    private Vector2 RandomVelocity() {
        Vector2 randomFromList = aiDirections[Random.Range(0, aiDirections.Length)];
        return Randoms.RightAngleRotation(randomFromList) * general.movementSpeed;
    }
    
    private Vector2 IndexedVelocity(Vector2 targetDirection) {
        // round instead of floor if aiDirections.Length were even.
        int index = Mathf.FloorToInt((Vector2.SignedAngle(Vector3.right, targetDirection) + 360) % 360 / (90 / aiDirections.Length));
        int rotation = index / aiDirections.Length;
        int subIndex = index % aiDirections.Length;
        return aiDirections[subIndex].RotateRightAngles(rotation) * general.movementSpeed;
    }

    public void MoveToward(Vector3 target) =>
        movement.InDirection(IndexedVelocity(target - transform.position));

    public WaitForSeconds Roam() {
        if (Random.value < general.roamRestingFraction) movement.Idle();
        else movement.InDirection(RandomVelocity());
        return new WaitForSeconds(Random.value * general.reconsiderRateRoam);
    }

    private Vector3 FollowTargetDirection(Vector3 targetPosition) {
        Vector3 toTarget = targetPosition - transform.position;

        Transform nearestThreat = brain.NearestThreat();
        if (nearestThreat == null) return toTarget;
        Vector3 toThreat = nearestThreat.position - transform.position;
        Vector3 toThreatCorrected = toThreat * toTarget.sqrMagnitude / toThreat.sqrMagnitude * general.timidity;
        return toTarget - toThreatCorrected;
    }
    public WaitForSeconds Follow(Transform followDirective) {
        Vector3 targetDirection = FollowTargetDirection(followDirective.position);
        movement.InDirection(IndexedVelocity(targetDirection));
        return new WaitForSeconds(Random.value * general.reconsiderRateFollow);
    }

    public PathfindingOperation<Transform> Approach(Transform target, float proximityToStop) {
        bool result = ReachTarget(target.position, proximityToStop, out WaitForSeconds persist);
        return new PathfindingOperation<Transform>(movement, target, result, persist, t => t.position);
    }
    public PathfindingOperation<Terrain.Position> Approach(Terrain.Position target, float proximityToStop) {
        bool result = ReachTarget(brain.terrain.CellCenter(target), proximityToStop, out WaitForSeconds persist);
        return new PathfindingOperation<Terrain.Position>(movement, target, result, persist, brain.terrain.CellCenter);
    }
    public WaitForSeconds ApproachThenIdle(Vector2 target, float proximityToStop) {
        bool result = ReachTarget(target, proximityToStop, out WaitForSeconds persist);
        if (result) movement.Idle();
        return persist;
    }
    public bool ReachTarget(Vector3 target, float proximityToStop, out WaitForSeconds persist) {
        persist = new WaitForSeconds(general.reconsiderRateTarget);
        float distance = Vector2.Distance(target, transform.position);
        if (distance <= proximityToStop) {
            return true;
        } else {
            if (distance < general.movementSpeed * general.reconsiderRateTarget) persist = null; // adjust faster when we're close
            movement.InDirection(IndexedVelocity(target - transform.position));
            return false;
        }
    }
}

public class PathfindingOperation<T> {
    private CharacterController movement;
    private T target;
    private bool result;
    private WaitForSeconds persist;
    private Func<T, Vector2> LocationOf;
    public PathfindingOperation(CharacterController movement, T target, bool result,
            WaitForSeconds persist, Func<T, Vector2> locationOperator) {
        this.movement = movement;
        this.target = target;
        this.result = result;
        this.persist = persist;
        this.LocationOf = locationOperator;
    }

    public WaitForSeconds Then(string animationTrigger, Action<T> action) {
        if (result) {
            movement.IdleFacing(LocationOf(target));
            if (animationTrigger != null) movement.Trigger(animationTrigger);
            action(target);
        }
        return persist;
    }

    public WaitForSeconds Then(string animationTrigger, float repeatTime, Action<T> action) {
        if (result) {
            movement.IdleFacing(LocationOf(target));
            if (animationTrigger != null) movement.Trigger(animationTrigger);
            action(target);
            return new WaitForSeconds(repeatTime);
        } else return persist;
    }

    public bool ThenCheckIfReached(string animationTrigger, out WaitForSeconds approachWait) {
        approachWait = persist;
        if (result) {
            movement.IdleFacing(LocationOf(target));
            if (animationTrigger != null) movement.Trigger(animationTrigger);
            return true;
        } else return false;
    }
}