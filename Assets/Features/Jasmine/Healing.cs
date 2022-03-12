using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Healing : MonoBehaviour {
    public float healDistance = 1f;
    public int healQuantity = 10;
    public float healTime = .1f;
    public bool healAutomatically = false;

    private Team team;

    void Start() {
        team = GetComponent<Team>();
        if (team == null) team = null;
        if (healAutomatically) StartCoroutine(HealAutomatically());
    }

    private bool SameTeam(Transform character) => team?.SameTeam(character) != false;

    public bool CanHeal(Transform character, float radius) {
        return SameTeam(character) &&
            !character.GetComponentStrict<Health>().IsFull() &&
            Vector2.Distance(transform.position, character.position) <= radius;
    }

    // lower is higher priority
    public float HealPriority(Collider2D character) {
        return character.GetComponentStrict<Health>().LevelPercent
            + Vector2.Distance(character.transform.position, transform.position) / Creature.neighborhood;
    }

    public Creature FindOneCreatureToHeal() {
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(transform.position, Creature.neighborhood, LayerMask.GetMask("HealthCreature"));
        Creature result = null;
        float resultPriority = 1;
        foreach (Collider2D character in charactersNearby) {
            // Creature.CanPair checks the right level of AI availability to override for healing
            if (SameTeam(character.transform) && character.GetComponentStrict<Creature>().CanPair()) {
                float priority = HealPriority(character);
                if (priority < resultPriority) {
                    result = character.GetComponentStrict<Creature>();
                    resultPriority = priority;
                }
            }
        }
        return result;
    }

    public void ForceHeal(Transform target, int healQuantity) {
        target.GetComponentStrict<Health>().Increase(healQuantity);
    }

    private IEnumerator HealAutomatically() {
        while (true) {
            Collider2D[] charactersNearby =
                Physics2D.OverlapCircleAll(transform.position, Creature.neighborhood, LayerMask.GetMask("Player", "HealthCreature"));
            foreach (Collider2D character in charactersNearby) if (SameTeam(character.transform)) {
                if (CanHeal(character.transform, healDistance)) ForceHeal(character.transform, healQuantity);
                Creature creature = character.GetComponent<Creature>();
                if (creature != null) {
                    if (character.GetComponentStrict<Health>().IsFull()) creature.EndPairCommand(transform);    
                    else if (HealPriority(character) < 1 && creature.CanPair()) creature.TryPair(transform);
                }
            }
            yield return new WaitForSeconds(healTime);
        }
    }
}
