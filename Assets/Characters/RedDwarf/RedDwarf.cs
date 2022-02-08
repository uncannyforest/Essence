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
                new CoroutineWrapper(WoodBuildBehaviorE, species),
                new TeleFilter(TeleFilter.Terrain.WOODBUILDING, null))
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

    private IEnumerator WoodBuildBehaviorE() {
        for (int i = 0; i < 100_000; i++) {
            Terrain.Position buildLocation = (Terrain.Position)executeDirective;
            bool reached = pathfinding.Approach(buildLocation, redDwarf.buildDistance)
                    .ThenCheckIfReached(null, out WaitForSeconds approachWait);
            if (reached) {
                terrain[buildLocation] = Construction.Wood;
                yield return new WaitForSeconds(redDwarf.buildTime);
                CompleteExecution(); 
                yield break;
            } else yield return approachWait;
        }
    }
}