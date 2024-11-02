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

        Habitat = Habitat.Feature(this, FeatureLibrary.P.carrot);
    }

    override public bool IsValidFocus(Transform characterFocus) =>
        healing.CanHeal(characterFocus, Creature.neighborhood);

    override public Optional<Transform> FindFocus() {
        Transform player = GameManager.I.AnyPlayer.transform;
        if (healing.CanHeal(player, Creature.neighborhood)) return Optional.Of(player);
        else return RequestPair(healing.FindOneCreatureToHeal()); 
    }

    private bool ShouldHealPlayer() {
        PlayerCharacter player = GameManager.I.AnyPlayer;
        return player.GetComponentStrict<Team>().TeamId == teamId &&
                !player.GetComponentStrict<Health>().IsFull() &&
                Vector2.Distance(transform.position, player.transform.position) <= Creature.neighborhood;
    }

    override public IEnumerator FocusedBehavior() {
        while (true) {
            yield return pathfinding.Approach(state.characterFocus.Value.position, healing.healDistance).Else(() => {
                state.characterFocus.Value.GetComponentStrict<Health>().Increase(creature.stats.Str);
                creature.GenericExeSucceeded();
                return new WaitForSeconds(creature.stats.ExeTime);
            });
        }
    }
    
}