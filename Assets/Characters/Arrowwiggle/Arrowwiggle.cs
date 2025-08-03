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

        Actions = new List<CreatureAction>() {
            CreatureAction.WithFeature(FeatureLibrary.C.arrowPile,
                pathfinding.BuildFeature(FeatureLibrary.C.arrowPile, this, () => creature.stats.ExeTime, 5))
        };

        Habitat = new ConsumableFeatureHabitat(this, FeatureLibrary.C.woodPile, () => creature.stats.ExeTime * 5);
    }
}