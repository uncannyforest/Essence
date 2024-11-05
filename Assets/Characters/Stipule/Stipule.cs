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

        Actions = new List<CreatureAction>() {
            CreatureAction.WithCharacter(stipule.attackAction,
                AttackBehavior,
                (c) => 
                    c.GetComponent<Health>() != null &&
                    c.GetComponentStrict<Team>().TeamId != teamId
                )
        };

        Habitat = Habitat.Feature(this, FeatureLibrary.P.jasmine);
    }

    override public Optional<Transform> FindFocus() => Will.NearestThreat(this);

    override public IEnumerator FocusedBehavior() => AttackBehavior.enumeratorWithParam(state.characterFocus.Value);
    
    private CharacterTargetedBehavior AttackBehavior {
        get => new CharacterTargetedBehavior((Transform focus) =>
            pathfinding.Approach(focus.position, stipule.meleeReach).Else(
                pathfinding.FaceAnd("Attack", focus.position, () => Attack(focus))
            )
        );
    }

    private void Attack(Transform target) {
        Health health = target.GetComponentStrict<Health>();
        if (target.GetComponent<Team>()?.TeamId == teamId) {
            Debug.LogError("Unexpected state, target is same team");
            return;
        }
        health.Decrease(creature.stats.Str, transform);
    }

    // TODO move this to Brain
    override protected void OnHealthReachedZero() {
        new Senses() {
            faint = true
        }.TryUpdateCreature(creature);
    }
}