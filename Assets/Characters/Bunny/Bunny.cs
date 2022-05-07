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
    }

    override public bool CanTame(Transform player) => true;
    override public bool ExtractTamingCost(Transform player) => true;

    override public bool IsValidFocus(Transform characterFocus) =>
        healing.CanHeal(characterFocus, Creature.neighborhood);

    override public Optional<Transform> FindFocus() {
        Transform player = GameObject.FindObjectOfType<PlayerCharacter>().transform;
        if (healing.CanHeal(player, Creature.neighborhood)) return Optional.Of(player);
        else return RequestPair(healing.FindOneCreatureToHeal()); 
    }

    private bool ShouldHealPlayer() {
        PlayerCharacter player = GameObject.FindObjectOfType<PlayerCharacter>();
        return player.GetComponentStrict<Team>().TeamId == teamId &&
                !player.GetComponentStrict<Health>().IsFull() &&
                Vector2.Distance(transform.position, player.transform.position) <= Creature.neighborhood;
    }

    override public IEnumerator FocusedBehavior() {
        while (true) {
            yield return pathfinding.Approach(state.characterFocus.Value.position, healing.healDistance).Else(() => {
                state.characterFocus.Value.GetComponentStrict<Health>().Increase(healing.healQuantity);
                return new WaitForSeconds(healing.healTime);
            });
        }
    }
    
}