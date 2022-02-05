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

    override public bool CanTame(Transform player) => true;
    override public bool ExtractTamingCost(Transform player) => true;

    override protected IEnumerator ScanningBehaviorE() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);

            if (Focused) {
                if (CanHeal(Focus)) continue;
                else Focus = null;
            }
            
            Transform player = GameObject.FindObjectOfType<PlayerCharacter>().transform;
            if (CanHeal(player)) Focus = player;
            else Focus = RequestPair(FindCreatureToHeal()); 
        }
    }

    private bool CanHeal(Transform character) {
        return character.GetComponentStrict<Team>().TeamId == team &&
                !character.GetComponentStrict<Health>().IsFull() &&
                Vector2.Distance(transform.position, character.position) <= Creature.neighborhood;
    }

    private bool ShouldHealPlayer() {
        PlayerCharacter player = GameObject.FindObjectOfType<PlayerCharacter>();
        return player.GetComponentStrict<Team>().TeamId == team &&
                !player.GetComponentStrict<Health>().IsFull() &&
                Vector2.Distance(transform.position, player.transform.position) <= Creature.neighborhood;
    }

    private Creature FindCreatureToHeal() {
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(transform.position, Creature.neighborhood, LayerMask.GetMask("HealthCreature"));
        Creature result = null;
        float resultPriority = 1;
        foreach (Collider2D character in charactersNearby) {
            if (character.GetComponentStrict<Team>().TeamId == team && character.GetComponentStrict<Creature>().CanPair()) {
                float priority = character.GetComponentStrict<Health>().LevelPercent
                    + Vector2.Distance(character.transform.position, transform.position) / Creature.neighborhood;
                Debug.Log("To heal? " + character.gameObject + " priority " + priority + (priority < resultPriority ? ": updating" : null));
                if (priority < resultPriority) {
                    result = character.GetComponentStrict<Creature>();
                    resultPriority = priority;
                }
            }
        }
        return result;
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            if (Vector2.Distance(Focus.position, transform.position) < bunny.healDistance) {
                movement.IdleFacing(Focus.position);
                Focus.GetComponentStrict<Health>().Increase(bunny.healQuantity);
                yield return new WaitForSeconds(bunny.healTime);
            } else {
                movement.InDirection(IndexedVelocity(Focus.position - transform.position));
                yield return new WaitForSeconds(general.reconsiderRatePursuit);
            }
        }
    }
}