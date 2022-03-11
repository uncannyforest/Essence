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
    }

    override public bool CanTame(Transform player) {
        return state.type == CreatureStateType.Faint // changed in CommandFollow not ExtractTamingCost
            && HasBunnyNearby(player);
    }

    // Returns true if successful.
    public override bool ExtractTamingCost(Transform player) {
        if (CanTame(player)) {
            GetComponentStrict<Health>().ResetTo(1);
            return true;
        } else return false;
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.WithCharacter(archer.attackAction,
                new CharacterTargetedBehavior(FocusedBehavior),
                (c) => { Debug.Log(c.GetComponent<Health>() + " " + c.GetComponentStrict<Team>().TeamId); return
                    c.GetComponent<Health>() != null &&
                    c.GetComponentStrict<Team>().TeamId != teamId;}
                )
        };
    }

    override public Optional<Transform> FindFocus() => Will.NearestThreat(this,
        (threat) => threat.GetComponent<Archer>() == null);

    override public IEnumerator FocusedBehavior(Transform focus) {
        while (true) {
            yield return WatchForMovement(focus, out Vector2 pos0, out float time0);
            yield return Attack(focus, pos0, time0);
        }
    }

    private WaitForSeconds WatchForMovement(Transform target, out Vector2 pos0, out float time0) {
        pos0 = target.position;
        time0 = Time.time;
        return new WaitForSeconds(general.reconsiderRateTarget * .1f);
    }

    private WaitForSeconds Attack(Transform target, Vector2 pos0, float time0) {
        Vector2 pos1 = target.position;
        float time1 = Time.time;
        Vector2 expectedFuturePosition = ExpectedFuturePosition(pos0, pos1, time0, time1);
        Debug.DrawLine(pos1, expectedFuturePosition, Color.red, 1f);
        if (Vector2.Distance(expectedFuturePosition, transform.position) < expectedArrowReach) {
            movement.IdleFacing(expectedFuturePosition);
            Arrow.Instantiate(archer.arrowPrefab, grid, transform, expectedFuturePosition);
        } else if (state.command?.type != CommandType.Station) {
            pathfinding.MoveToward(target.position);
        }
        return new WaitForSeconds(general.reconsiderRateTarget * .9f);
    }

    private Vector2 ExpectedFuturePosition(Vector2 pos0, Vector2 pos1, float time0, float time1) {
        Vector2 velocity = (pos1 - pos0) / (time1 - time0);
        float approxTimeToHit = Vector2.Distance(pos1, transform.position) / archer.arrowPrefab.speed;
        Debug.Log(velocity + " " + approxTimeToHit + " " + (pos1 + velocity * approxTimeToHit));
        return pos1 + velocity * (approxTimeToHit / 2f);
    }

    override protected void OnHealthReachedZero() {
        new Senses() {
            faint = true
        }.TryUpdateCreature(creature);
    }

    private bool HasBunnyNearby(Transform player) {
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(player.position, Creature.neighborhood, LayerMask.GetMask("Creature"));
        foreach (Collider2D character in charactersNearby)
            if (character.GetComponent<Bunny>() != null &&
                    character.GetComponentStrict<Team>().SameTeam(player))
                return true;
        return false;
    }
}