using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class StipuleConfig {
    public Sprite attackAction;
}

[RequireComponent(typeof(Health))]
public class Stipule : Species<StipuleConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new StipuleBrain(this, generalConfig, speciesConfig);
    }
}

public class StipuleBrain : Brain {
    private StipuleConfig stipule;

    public StipuleBrain(Stipule species, BrainConfig general, StipuleConfig stipule) : base(species, general) {
        this.stipule = stipule;

        MainBehavior = new CharacterTargetedBehavior(this,
            AttackBehavior,
            (c) => Will.IsThreat(teamId, c),
            (c) => SufficientResource() && Will.CanSee(transform.position, c));

        Actions = new List<CreatureAction>() {
            MainBehavior.CreatureAction(stipule.attackAction)
        };

        Habitat = Habitat.Feature(this, FeatureLibrary.C.jasmine);
    }

    override public Optional<Transform> FindFocus() => resource.Has() ? Will.NearestThreat(this) : Optional<Transform>.Empty();

    private IEnumerator<YieldInstruction> AttackBehavior(Transform f) =>
        from focus in Continually.For(f)
        where IsValidFocus(focus)                                   .NegLog(legalName + " focus " + focus + " no longer valid")
        select pathfinding.Approach(focus, GlobalConfig.I.defaultMeleeReach)
            .Then(() => pathfinding.FaceAnd("Attack", focus, Melee));
}