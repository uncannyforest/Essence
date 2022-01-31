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
        return State == CreatureState.Faint;
    }

    // Returns true if successful.
    public override bool ExtractTamingCost(Transform player) {
        return State == CreatureState.Faint; // changed in CommandFollow
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.WithObject(archer.attackAction,
                new CoroutineWrapper(AttackBehaviorE, species),
                new TeleFilter(TeleFilter.Terrain.NONE, (c) => { Debug.Log(c.GetComponent<Health>() + " " + c.GetComponentStrict<Team>().TeamId); return
                    c.GetComponent<Health>() != null &&
                    c.GetComponentStrict<Team>().TeamId != team ;}
                ))
        };
    }

    override protected IEnumerator ScanningBehaviorE() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);
            if (Focused && IsThreat(Focus)) continue;
            
            if (State == CreatureState.Roam || State == CreatureState.Station)
                Focus = NearestThreat((threat) => threat.GetComponent<Archer>() == null);
            else if (State == CreatureState.FollowOffensive) UpdateFollowOffensive();
            else Focus = null;
        }
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            yield return WatchForMovement(Focus, out Vector2 pos0, out float time0);
            yield return Attack(Focus, pos0, time0);
        }
        Debug.Log("Focus just ended, exiting FocusedBehavior");
    }
    
    private IEnumerator AttackBehaviorE() {
        while (((SpriteSorter)executeDirective) != null) {
            yield return WatchForMovement(((SpriteSorter)executeDirective).Character, out Vector2 pos0, out float time0);
            yield return Attack(((SpriteSorter)executeDirective).Character, pos0, time0);
        }
        Debug.Log("Attack just ended, exiting AttackBehavior");
    }

    private WaitForSeconds WatchForMovement(Transform target, out Vector2 pos0, out float time0) {
        pos0 = target.position;
        time0 = Time.time;
        return new WaitForSeconds(general.reconsiderRatePursuit * .1f);
    }

    private WaitForSeconds Attack(Transform target, Vector2 pos0, float time0) {
        Vector2 pos1 = target.position;
        float time1 = Time.time;
        Vector2 expectedFuturePosition = ExpectedFuturePosition(pos0, pos1, time0, time1);
        Debug.DrawLine(pos1, expectedFuturePosition, Color.red, 1f);
        if (Vector2.Distance(expectedFuturePosition, transform.position) < expectedArrowReach) {
            movement.IdleFacing(expectedFuturePosition);
            Arrow.Instantiate(archer.arrowPrefab, grid, transform, expectedFuturePosition);
        } else if (State != CreatureState.Station) {
            movement.Toward(IndexedVelocity(target.position - transform.position));
        }
        return new WaitForSeconds(general.reconsiderRatePursuit * .9f);
    }

    private Vector2 ExpectedFuturePosition(Vector2 pos0, Vector2 pos1, float time0, float time1) {
        Vector2 velocity = (pos1 - pos0) / (time1 - time0);
        float approxTimeToHit = Vector2.Distance(pos1, transform.position) / archer.arrowPrefab.speed;
        Debug.Log(velocity + " " + approxTimeToHit + " " + (pos1 + velocity * approxTimeToHit));
        return pos1 + velocity * (approxTimeToHit / 2f);
    }

    override protected void OnHealthReachedZero() {
        State = CreatureState.Faint;
        movement.SetBool("Fainted", true);
    }
}