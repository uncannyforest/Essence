using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BunnyConfig {
    public Sprite healAction;
    public float healDistance = 1f;
    public int healQuantity = 10;
    public float healTime = .1f;
}

public class Bunny : Species<BunnyConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new BunnyBrain(this, generalConfig, speciesConfig);
    }
}

public class BunnyBrain : Brain {
    private BunnyConfig bunny;

    public BunnyBrain(Bunny species, BrainConfig general, BunnyConfig bunny) : base(species, general) {
        this.bunny = bunny;
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
        };
    }

    override protected IEnumerator ScanningBehaviorE() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);
            if (Focused && ShouldHealPlayer()) continue;
            
            if ((State == CreatureState.Roam || State == CreatureState.Station || State == CreatureState.Follow)
                && ShouldHealPlayer()) Focus = GameObject.FindObjectOfType<PlayerCharacter>().transform;
            else Focus = null;
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
            if (Vector2.Distance(Focus.position, transform.position) < bunny.healDistance) {
                velocity = Vector2.zero;
                Focus.GetComponentStrict<Health>().Increase(bunny.healQuantity);
                yield return new WaitForSeconds(bunny.healTime);
            } else velocity = IndexedVelocity(Focus.position - transform.position);
            yield return new WaitForSeconds(general.reconsiderRatePursuit);
        }
    }
}