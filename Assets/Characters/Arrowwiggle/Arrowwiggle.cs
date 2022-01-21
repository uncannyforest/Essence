using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ArrowwiggleConfig {
    public float restockDistance = 1f;
    public int restockQuantity = 10;
    public float restockTime = .1f;
    public Sprite arrowCollectibleSprite;
}

public class Arrowwiggle : Species<ArrowwiggleConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new ArrowwiggleBrain(this, generalConfig, speciesConfig);
    }
}

public class ArrowwiggleBrain : Brain {
    private ArrowwiggleConfig arrowwiggle;

    public ArrowwiggleBrain(Arrowwiggle species, BrainConfig general, ArrowwiggleConfig arrowwiggle) : base(species, general) {
        this.arrowwiggle = arrowwiggle;
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
            if (Focused && ShouldRestockPlayer()) continue;
            
            if ((State == CreatureState.Roam || State == CreatureState.Station || State == CreatureState.Follow)
                && ShouldRestockPlayer()) Focus = GameObject.FindObjectOfType<PlayerCharacter>().transform;
            else Focus = null;
        }
    }

    private bool ShouldRestockPlayer() {
        PlayerCharacter player = GameObject.FindObjectOfType<PlayerCharacter>();
        return player.GetComponentStrict<Team>().TeamId == team &&
                !player.GetComponentStrict<Inventory>().materials[Material.Type.Arrow].IsFull &&
                Vector2.Distance(transform.position, player.transform.position) <= Creature.neighborhood;
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            if (Vector2.Distance(Focus.position, transform.position) < arrowwiggle.restockDistance) {
                velocity = Vector2.zero;
                Focus.GetComponentStrict<Inventory>().Add(Material.Type.Arrow, arrowwiggle.restockQuantity, arrowwiggle.arrowCollectibleSprite);
                yield return new WaitForSeconds(arrowwiggle.restockTime);
            } else {
                velocity = IndexedVelocity(Focus.position - transform.position);
                yield return new WaitForSeconds(general.reconsiderRatePursuit);
            }
        }
    }
}