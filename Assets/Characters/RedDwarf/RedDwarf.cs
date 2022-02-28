using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RedDwarfConfig {
    public Sprite woodBuildAction;
    public float buildTime;
    public float buildDistance;
}

public class RedDwarf : Species<RedDwarfConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new RedDwarfBrain(this, generalConfig, speciesConfig);
    }
}

public class RedDwarfBrain : Brain {
    private RedDwarfConfig redDwarf;

    public RedDwarfBrain(RedDwarf species, BrainConfig general, RedDwarfConfig redDwarf) : base(species, general) {
        this.redDwarf = redDwarf;
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.QueueableWithObject(redDwarf.woodBuildAction,
                new PathfindingEnumerator.ApproachThenBuild(this,
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => terrain[loc] = Construction.Wood).C,
                new TeleFilter(TeleFilter.Terrain.WOODBUILDING, null)),
            CreatureAction.QueueableFeature(FeatureLibrary.P.boat,
                new PathfindingEnumerator.ApproachThenBuild(this,
                    redDwarf.buildDistance, redDwarf.buildTime,
                    (loc) => terrain.BuildFeature(loc.Coord, FeatureLibrary.P.boat)).C)
        };
    }

    override public bool CanTame(Transform player) => true;
    override public bool ExtractTamingCost(Transform player) => true;

    override protected IEnumerator ScanningBehaviorE() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);
        }
    }

    override protected IEnumerator FocusedBehaviorE() {
        yield break; // not implemented
    }
}