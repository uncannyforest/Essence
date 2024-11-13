using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class StipuleConfig {
    public Sprite attackAction;
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
                new CharacterTargetedBehavior(AttackBehavior),
                (c) => Will.IsThreat(teamId, transform.position, c)     .NegLog(legalName + " cannot select " + c)
            )
        };

        Habitat = Habitat.Feature(this, FeatureLibrary.P.jasmine);
    }

    override public Optional<Transform> FindFocus() => resource.Has() ? Will.NearestThreat(this) : Optional<Transform>.Empty();

    override public IEnumerator<YieldInstruction> FocusedBehavior() => AttackBehavior(state.characterFocus.Value);
    
    private IEnumerator<YieldInstruction> AttackBehavior(Transform f) =>
        from focus in Continually.For(f)
        where IsValidFocus(focus)                                   .NegLog(legalName + " focus " + focus + " no longer valid")
        select pathfinding.Approach(focus, stipule.meleeReach)
            .Then(() => pathfinding.FaceAnd("Attack", focus, Attack));

    private void Attack(Transform target) {
        Health health = target.GetComponentStrict<Health>();
        if (target.GetComponent<Team>()?.TeamId == teamId) {
            Debug.LogError("Unexpected state, target is same team");
            return;
        }
        health.Decrease(creature.stats.Str, transform);
        resource.Use();
    }

    // TODO move this to Brain
    override protected void OnHealthReachedZero() {
        new Senses() {
            faint = true
        }.TryUpdateCreature(creature);
    }
}