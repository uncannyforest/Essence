using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BunnyConfig {
    public Sprite healAction;
}

[RequireComponent(typeof(Healing))]
public class Bunny : Species<BunnyConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new BunnyBrain(this, generalConfig, speciesConfig);
    }
}

public class BunnyBrain : Brain {
    private BunnyConfig bunny;
    private Healing healing;

    public BunnyBrain(Bunny species, BrainConfig general, BunnyConfig bunny) : base(species, general) {
        this.bunny = bunny;
        this.healing = species.GetComponentStrict<Healing>();

        Habitat = Habitat.Feature(this, FeatureLibrary.C.carrot);
    }

    override public WhyNot IsValidFocus(Transform characterFocus) =>
        SufficientResource() &&
        healing.CanHeal(characterFocus, Creature.neighborhood);

    override public Optional<Transform> FindFocus() {
        if (resource.IsOut) return Optional.Empty<Transform>();
        Transform player = GameManager.I.AnyPlayer.transform;
        if ((bool)healing.CanHeal(player, Creature.neighborhood)) return Optional.Of(player);
        else return RequestPair(healing.FindOneCreatureToHeal());
    }
    
    override public IEnumerator<YieldInstruction> FocusedBehavior() =>
        pathfinding.Approach(state.characterFocus.Value, healing.healDistance).Then(() => {
                state.characterFocus.Value.GetComponentStrict<Health>().Increase(creature.stats.Str);
                resource.Use();
                creature.GenericExeSucceeded();
                return new WaitForSeconds(creature.stats.ExeTime);
    });
}