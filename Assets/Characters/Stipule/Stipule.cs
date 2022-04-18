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
                AttackBehaviorE,
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
            
            if (state.command?.type == CommandType.Roam || state.command?.type == CommandType.Station)
                Focus = NearestThreat();
            else Focus = null;
        }
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            yield return pathfinding.Approach(Focus, stipule.meleeReach).Then("Attack", Attack);
        }
        Debug.Log("Focus just ended, exiting FocusedBehavior");
    }
    
    private IEnumerator AttackBehaviorE() {
        while (((SpriteSorter)executeDirective) != null) {
            yield return pathfinding.Approach(((SpriteSorter)executeDirective).Character, stipule.meleeReach).Then("Attack", Attack);
        }
        Debug.Log("Attack just ended, exiting AttackBehavior");
    }

    private void Attack(Transform target) {
        Health health = target.GetComponentStrict<Health>();
        if (target.GetComponent<Team>()?.TeamId == team) {
            Debug.LogError("Unexpected state, target is same team");
            return;
        }
        health.Decrease(stipule.attack, transform);
    }
}