using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ArrowwiggleConfig {
    public int tamingCost = 1;
    public float restockDistance = 1f;
    public int restockQuantity = 10;
    public float restockTime = .1f;
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

    override public bool CanTame(Transform player) =>
        player.GetComponentStrict<Inventory>().CanRetrieve(Material.Type.Arrow, arrowwiggle.tamingCost);

    override public bool ExtractTamingCost(Transform player) {
        return player.GetComponentStrict<Inventory>().Retrieve(Material.Type.Arrow, arrowwiggle.tamingCost);
    }

    override public bool IsValidFocus(Transform characterFocus) =>
        ShouldRestockPlayer();

    override public Optional<Transform> FindFocus() =>
        ShouldRestockPlayer() ? Optional.Of(GameObject.FindObjectOfType<PlayerCharacter>().transform)
            : Optional<Transform>.Empty();

    private bool ShouldRestockPlayer() {
        PlayerCharacter player = GameObject.FindObjectOfType<PlayerCharacter>();
        return player.GetComponentStrict<Team>().TeamId == team &&
                !player.GetComponentStrict<Inventory>().materials[Material.Type.Arrow].IsFull &&
                Vector2.Distance(transform.position, player.transform.position) <= Creature.neighborhood;
    }

    override public IEnumerator FocusedBehavior(Transform focus) {
        while (true) {
            yield return pathfinding.Approach(focus.position, arrowwiggle.restockDistance).Else(() => {
                focus.GetComponentStrict<Inventory>().Add(Material.Type.Arrow, arrowwiggle.restockQuantity);
                return new WaitForSeconds(arrowwiggle.restockTime);
            });
        }
    }
    
}