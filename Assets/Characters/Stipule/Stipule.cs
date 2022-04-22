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
                AttackBehavior.ForTarget(),
                new TeleFilter(TeleFilter.Terrain.NONE, (c) => 
                    c.GetComponent<Health>() != null &&
                    c.GetComponentStrict<Team>().TeamId != team
                ))
        };
    }

    override public Optional<Transform> FindFocus() => Will.NearestThreat(this);

    override public IEnumerator FocusedBehavior(Transform focus) => AttackBehavior.enumeratorWithParam(focus);
    
    private CharacterTargetedBehavior AttackBehavior {
        get => new CharacterTargetedBehavior((Transform focus) =>
            pathfinding.Approach(focus.position, stipule.meleeReach).Else(
                pathfinding.FaceAnd("Attack", focus.position, () => Attack(focus))
            )
        );
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