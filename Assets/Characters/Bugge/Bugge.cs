using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class BuggeConfig {
}

[RequireComponent(typeof(Anthopoid))]
public class Bugge : Species<BuggeConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new BuggeBrain(this, generalConfig, speciesConfig);
    }
}

public class BuggeBrain : Brain {
    private BuggeConfig bugge;

    public BuggeBrain(Bugge species, BrainConfig general, BuggeConfig bugge) : base(species, general) {
        this.bugge = bugge;

        pathfinding.Roam = Roam;
    }

    override public bool CanTame(Transform player) => false;

    private IEnumerator<YieldInstruction> Roam() {
        while (true) {
            Fountain nearestFountain = Fountain.FindAllByTeam(teamId, invert: true)
                .MinBy((f) => (f.transform.position - transform.position).sqrMagnitude);

            IEnumerator<YieldInstruction> approach = pathfinding.Approach((Vector2)nearestFountain.transform.position, proximityToStop: -1);
            
            while (nearestFountain.Team != teamId) {
                approach.MoveNext();
                yield return approach.Current;
            }
        }
    }

    override public Optional<Transform> FindFocus() {
        // any enemy Creature non-Bugge
        return default;
    }

    override public IEnumerator<YieldInstruction> FocusedBehavior() {
        // if fountain not in view
        // get adjacent creature & initiate taming
        yield break;
    }
}
