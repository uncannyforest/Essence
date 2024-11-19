using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ArcherConfig {
    public Sprite attackAction;
    public int scaleCost;
    public Arrow arrowPrefab;
}

[RequireComponent(typeof(Health))]
public class Archer : Species<ArcherConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new ArcherBrain(this, generalConfig, speciesConfig);
    }
}

public class ArcherBrain : Brain {
    private ArcherConfig archer;

    private float expectedArrowReach;

    public ArcherBrain(Archer species, BrainConfig general, ArcherConfig archer) : base(species, general) {
        this.archer = archer;
        expectedArrowReach = archer.arrowPrefab.reach + archer.arrowPrefab.GetComponentStrict<CircleCollider2D>().radius;

        MainBehavior = new CharacterTargetedBehavior(ExecuteBehavior,
            (c) => c.GetComponent<Health>() != null &&
                   c.GetComponentStrict<Team>().TeamId != teamId,
            (c) => SufficientResource());

        Actions = new List<CreatureAction>() {
            MainBehavior.CreatureAction(archer.attackAction)
        };

        Habitat = new ConsumableFeatureHabitat(this, FeatureLibrary.P.arrowPile, () => creature.stats.ExeTime * 2, 20);
    }

    override public Optional<Transform> FindFocus() => resource.Has()
        ? Will.NearestThreat(this, (threat) => threat.GetComponent<Archer>() == null)
        : Optional<Transform>.Empty();

    private IEnumerator<YieldInstruction> ExecuteBehavior(Transform focus) {
        while (IsValidFocus(focus)) {
            yield return WatchForMovement(focus, out Vector2 pos0, out float time0);
            yield return Attack(focus, pos0, time0);
        }
    }

    private WaitForSeconds WatchForMovement(Transform target, out Vector2 pos0, out float time0) {
        pos0 = target.position;
        time0 = Time.time;
        return new WaitForSeconds(creature.stats.ExeTime * .1f);
    }

    private WaitForSeconds Attack(Transform target, Vector2 pos0, float time0) {
        Vector2 pos1 = target.position;
        float time1 = Time.time;
        Vector2 expectedFuturePosition = ExpectedFuturePosition(pos0, pos1, time0, time1);
        Debug.DrawLine(pos1, expectedFuturePosition, Color.red, 1f);
        if (Vector2.Distance(expectedFuturePosition, transform.position) < expectedArrowReach) {
            movement.IdleFacing(expectedFuturePosition);
            resource.Use();
            Arrow.Instantiate(archer.arrowPrefab, grid, transform, expectedFuturePosition, creature.stats.Str);
        } else if (state.command?.type != CommandType.Station) {
            pathfinding.MoveTowardWithoutClearingObstacles(target.position);
        }
        return new WaitForSeconds(creature.stats.ExeTime * .9f);
    }

    private Vector2 ExpectedFuturePosition(Vector2 pos0, Vector2 pos1, float time0, float time1) {
        Vector2 velocity = (pos1 - pos0) / (time1 - time0);
        float approxTimeToHit = Vector2.Distance(pos1, transform.position) / archer.arrowPrefab.speed;
        Debug.Log(velocity + " " + approxTimeToHit + " " + (pos1 + velocity * approxTimeToHit));
        return pos1 + velocity * (approxTimeToHit / 2f);
    }
}