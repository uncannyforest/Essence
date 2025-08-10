using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PositionProvider {
    private Transform t;
    private Vector2? p;
    public Vector2 position {
        get {
            if (p is Vector2 realP) return realP;
            else return t.position;
        }
    }

    public static implicit operator PositionProvider(Vector2 position) =>
        new PositionProvider() { p = position };

    public static implicit operator PositionProvider(Transform transform) => 
        new PositionProvider() { t = transform };
}

public class Pathfinding {
    public readonly Brain brain;
    public Pathfinding(Brain brain) {
        this.brain = brain;
    }
    private BrainConfig general { get => brain.general; }
    private CharacterController movement { get => brain.movement; }
    private Transform transform { get => brain.transform; }

    private Displacement RandomVelocity() => Randoms.Direction();
    
    private Displacement IndexedVelocity(Displacement targetDirection) => targetDirection.normalized;

    public void MoveTowardWithoutClearingObstacles(Vector3 target) {
        CheckTargetForObstacles(target, 0).MoveNext();
        movement.InDirection(IndexedVelocity(Disp.FT(transform.position, target)));
    }

    public IEnumerator<YieldInstruction> Roam() {
        while (true) {
            IEnumerator<YieldInstruction> larkStep;
            while (true) {
                if (brain.Lark.ScanForLark().IsValue(out IEnumerator<YieldInstruction> step)) {
                    larkStep = step;
                    break;
                }
                if (Random.value < general.roamRestingFraction) movement.Idle();
                else movement.InDirection(RandomVelocity());
                yield return new WaitForSeconds(Random.value * general.reconsiderMaxRateRoam);
            }
            while (larkStep.MoveNext()) {
                yield return larkStep.Current;
            }
        }
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
        if (CheckTargetForObstacles(followDirective.position, 0).MoveNext(out YieldInstruction unblockSelf))
            return unblockSelf;
        Displacement targetDirection = FollowTargetDirection(followDirective.position);
        movement.InDirection(IndexedVelocity(targetDirection));
        return new WaitForSeconds(Random.value * general.reconsiderRateFollow);
    }

    public YieldInstruction TypicalWait { get => new WaitForSeconds(brain.creature.stats.ExeTime); }

    public YieldInstruction ApproachThenIdle(Vector2 target, float proximityToStop = 1f / CharacterController.subGridUnit) =>
        Approach(target, proximityToStop).NextOr(() => { movement.Idle(); return TypicalWait; });

    public IEnumerator<YieldInstruction> Approach(PositionProvider target, float proximityToStop = 1f / CharacterController.subGridUnit) {
        while (true) {
            if (CheckTargetForObstacles(target.position, proximityToStop).MoveNext(out YieldInstruction unblockSelf))
                yield return unblockSelf;
            float distance = Vector2.Distance(target.position, transform.position);
            if (distance <= proximityToStop) {
                movement.Idle();
                yield break;
            } else {
                movement.InDirection(IndexedVelocity(Disp.FT(transform.position, target.position)));
                if (distance < movement.Speed * brain.creature.stats.ExeTime) yield return null; // adjust faster when we're close
                else yield return TypicalWait;
            }
        }
    }

    // if no rewardExp is supplied, it defaults to true
    public ApproachThenInteract ApproachThenInteract(float interactionDistance, Func<float> interactionTime, Action<Terrain.Position> interaction, bool rewardExp = true)
        => new ApproachThenInteract(brain, interactionDistance, interactionTime, interaction, rewardExp);

    public ApproachThenInteract ApproachThenInteract(Action<Terrain.Position> interaction, bool rewardExp = true)
        => new ApproachThenInteract(brain, GlobalConfig.I.defaultTerraformingReach, () => brain.creature.stats.ExeTime, interaction, rewardExp);

    public ApproachThenInteract Terraform(Action<Terrain.Position> action)
        => ApproachThenInteract((loc) => {
            brain.resource.Use(1);
            action(loc);
        });

    public QueueOperator.Targeted<Vector2Int> BuildFeature(FeatureConfig feature, int cost = 1)
        => ApproachThenInteract((loc) => {
            brain.resource.Use(cost);
            Terrain.I.BuildFeature(loc.Coord, feature);
        }).PendingVector2Int((p) => brain.resource.Has(cost)).Queued();

    public Func<YieldInstruction> FaceAnd(string animationTrigger,
            Vector2 location,
            Func<YieldInstruction> finalAction) => () => {
        movement.IdleFacing(location);
        if (animationTrigger != null) movement.Trigger(animationTrigger);
        return finalAction();
    };

    public YieldInstruction FaceAnd(string animationTrigger,
            Transform focus,
            Action<Transform> finalAction) {
        movement.IdleFacing(focus.position);
        if (animationTrigger != null) movement.Trigger(animationTrigger);
        finalAction(focus);
        return TypicalWait;
    }

    public IEnumerator<YieldInstruction> CheckTargetForObstacles(Vector2 target, float exceptWithinRadius) {
        while (true) {
            if (IdentifyObstacles(target) is DesireMessage.Obstacle obstacle &&
                    Disp.FT(target, Terrain.I.CellCenter(obstacle.location)) > exceptWithinRadius) {
                if (Will.CanClearObstacleAt(brain.general, obstacle.location)) {
                    IEnumerator<YieldInstruction> nextMove = brain.UnblockSelf(obstacle.location);
                    nextMove.MoveNext();
                    yield return nextMove.Current;
                } else BroadcastObstacle(obstacle);
            }
            yield break;
        }
    }

    private DesireMessage.Obstacle? IdentifyObstacles(Vector2 target) {
        if (!Terrain.I.InBounds(Terrain.I.CellAt(target)))
            throw new InvalidOperationException("Target out of bounds");
        foreach (Terrain.Position position in
                MooseMath.GetPathPositions(brain.transform.position, target)) {
            if (position.grid == Terrain.Grid.XWalls || position.grid == Terrain.Grid.YWalls) {
                if ((Terrain.I.GetConstruction(position) ?? Construction.None) != Construction.None) {
                    return new DesireMessage.Obstacle() {
                        requestor = brain.creature.GetComponentStrict<Character>(),
                        location = position,
                        wallObstacle = Terrain.I[position]
                    };
                }
            } else {
                Land? land = Terrain.I.GetLand(position.Coord);
                if (land?.IsPassable() == false || (movement.waterSpeed == 0 && land?.IsWatery() == true)) {
                    return new DesireMessage.Obstacle() {
                        requestor = brain.creature.GetComponentStrict<Character>(),
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

// This is a factory class storing configuration for approach+interact behavior,
// and can be exported to several different types for usage.
//
// Approach+interact behavior involves:
//      (1) Approaching a target, including prerequisites (including checking for obstacles)
//      (2) Running a single Action on that target, integrating connections to the Animator and EXP
//
// Note: this class only runs a single action once upon reaching.
// For continuous actions upon reaching, use Pathfinding::Approach().Then()
// 
// Exported types available for usage:
// For CreatureActions, a TargetedBehavior is needed, which requires supplying an error filter, for example:
//      approachThenInteract.ForPosition(errorFilter).Queued()
// In other cases, a simple IEnumerator<YieldInstruction> is desired, which requires supplying the Position targeted:
//      approachThenInteract.Enumerator(position)
public class ApproachThenInteract {
    private readonly Brain brain;
    private readonly float interactDistance;
    private readonly Func<float> interactTime;
    private readonly Action<Terrain.Position> interaction;
    private readonly bool rewardExp;
    private readonly Func<Terrain.Position, IEnumerator<YieldInstruction>> enumeratorWithParam;

    public ApproachThenInteract (
            Brain brain,
            float interactDistance,
            Func<float> interactTime,
            Action<Terrain.Position> interaction,
            bool rewardExp) {
        this.brain = brain;
        this.interactDistance = interactDistance;
        this.interactTime = interactTime;
        this.interaction = interaction;
        this.rewardExp = rewardExp;
        this.enumeratorWithParam = Enumerator;
    }

    public IEnumerator<YieldInstruction> Enumerator(Terrain.Position location) =>
        brain.pathfinding.CheckTargetForObstacles(Terrain.I.CellCenter(location), interactDistance)
            .Then(brain.pathfinding.Approach(Terrain.I.CellCenter(location), interactDistance))
            .Then(Finish(location));

    private IEnumerator<YieldInstruction> Finish(Terrain.Position location) {
        brain.movement.IdleFacing(Terrain.I.CellCenter(location));
        yield return new WaitForSeconds(interactTime());
        interaction(location);
        if (rewardExp) brain.creature.GenericExeSucceeded();
    }

    public TargetedBehavior<Vector2Int> PendingVector2Int(Func<Vector2Int, WhyNot> errorFilter) => new TargetedBehavior<Vector2Int>(
        (target) => enumeratorWithParam(new Terrain.Position(Terrain.Grid.Roof, target)),
        errorFilter
    );
    public TargetedBehavior<Terrain.Position> PendingPosition(Func<Terrain.Position, WhyNot> errorFilter) => new TargetedBehavior<Terrain.Position>(
        enumeratorWithParam,
        errorFilter
    );
}
