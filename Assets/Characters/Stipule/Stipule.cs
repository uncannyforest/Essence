using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class StipuleConfig {
    public Sprite attackAction;
    public int scaleCost;
    public float meleeReach;
    public int attack;
}

[RequireComponent(typeof(Health))]
public class Stipule : Species<StipuleConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new StipuleBrain(this, generalConfig, speciesConfig);
    }
}

public class StipuleBrain : Brain {
    private StipuleConfig stipule;

    private float expectedArrowReach;

    public StipuleBrain(Stipule species, BrainConfig general, StipuleConfig stipule) : base(species, general) {
        this.stipule = stipule;
    }

    override public bool CanTame(Transform player) =>
        player.GetComponentStrict<Inventory>().CanRetrieve(Material.Type.Scale, stipule.scaleCost);

    // Returns true if successful.
    public override bool ExtractTamingCost(Transform player) {
        return player.GetComponentStrict<Inventory>().Retrieve(Material.Type.Scale, stipule.scaleCost);
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.WithObject(stipule.attackAction,
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
                Focus = NearestThreat();
            else if (State == CreatureState.FollowOffensive) UpdateFollowOffensive();
            else Focus = null;
        }
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            yield return Attack(Focus);
        }
        Debug.Log("Focus just ended, exiting FocusedBehavior");
    }
    
    private IEnumerator AttackBehaviorE() {
        while (((SpriteSorter)executeDirective) != null) {
            yield return Attack(((SpriteSorter)executeDirective).Character);
        }
        Debug.Log("Attack just ended, exiting AttackBehavior");
    }

    private WaitForSeconds Attack(Transform target) {
        if (Vector2.Distance(target.position, transform.position) < stipule.meleeReach) {
            Health health = target.GetComponentStrict<Health>();
            if (target.GetComponent<Team>()?.TeamId == team) {
                Debug.LogError("Unexpected state, target is same team");
                return new WaitForSeconds(general.reconsiderRatePursuit);
            }
            health.Decrease(stipule.attack, transform);
            movement.IdleFacing(target.position).Trigger("Attack");
        } else movement.InDirection(IndexedVelocity(target.position - transform.position));
        return new WaitForSeconds(general.reconsiderRatePursuit);
    }
}