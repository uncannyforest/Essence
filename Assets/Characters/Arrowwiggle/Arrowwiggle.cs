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
    
    override public bool CanTame(Transform player) =>
        player.GetComponentStrict<Inventory>().CanRetrieve(Material.Type.Arrow, arrowwiggle.tamingCost);

    override public bool ExtractTamingCost(Transform player) {
        return player.GetComponentStrict<Inventory>().Retrieve(Material.Type.Arrow, arrowwiggle.tamingCost);
    }

    override public WhyNot IsValidFocus(Transform characterFocus) => ShouldRestockPlayer();

    override public Optional<Transform> FindFocus() =>
        (bool)ShouldRestockPlayer() ? Optional.Of(GameManager.I.AnyPlayer.transform)
            : Optional<Transform>.Empty();

    private WhyNot ShouldRestockPlayer() {
        PlayerCharacter player = GameManager.I.AnyPlayer;
        return
            !team.SameTeam(player) ? "different_team" :
            player.GetComponentStrict<Inventory>().materials[Material.Type.Arrow].IsFull ? "player_inv_full" :
            Vector2.Distance(transform.position, player.transform.position) > Creature.neighborhood ? "too_far" :
            (WhyNot)true;
    }

    override public IEnumerator FocusedBehavior() {
        while (true) {
            yield return pathfinding.ApproachIfFar(state.characterFocus.Value.position, arrowwiggle.restockDistance).Else(() => {
                state.characterFocus.Value.GetComponentStrict<Inventory>().Add(Material.Type.Arrow, arrowwiggle.restockQuantity);
                return new WaitForSeconds(arrowwiggle.restockTime);
            });
        }
    }
    
}