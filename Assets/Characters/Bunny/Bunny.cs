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

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
        };
    }

    override public bool CanTame(Transform player) => true;
    override public bool ExtractTamingCost(Transform player) => true;

    override protected IEnumerator ScanningBehaviorE() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);

            if (Focused) {
                if (healing.CanHeal(Focus, Creature.neighborhood)) continue;
                else Focus = null;
            }
            
            Transform player = GameObject.FindObjectOfType<PlayerCharacter>().transform;
            if (healing.CanHeal(player, Creature.neighborhood)) Focus = player;
            else Focus = RequestPair(healing.FindOneCreatureToHeal()); 
        }
    }

    private bool ShouldHealPlayer() {
        PlayerCharacter player = GameObject.FindObjectOfType<PlayerCharacter>();
        return player.GetComponentStrict<Team>().TeamId == team &&
                !player.GetComponentStrict<Health>().IsFull() &&
                Vector2.Distance(transform.position, player.transform.position) <= Creature.neighborhood;
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            yield return pathfinding.Approach(Focus, healing.healDistance).Then(null, healing.healTime, (target) => {
                target.GetComponentStrict<Health>().Increase(healing.healQuantity);
            });
        }
    }
}