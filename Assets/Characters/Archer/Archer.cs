using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ArcherConfig {
    public Arrow arrowPrefab;
}

[RequireComponent(typeof(Health))]
public class Archer : Species<ArcherConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new ArcherBrain(this, generalConfig, speciesConfig);
    }
}

public class ArcherBrain : Brain {
    private ArcherConfig archer;

    public ArcherBrain(Archer species, BrainConfig general, ArcherConfig archer) : base(species, general) {
        this.archer = archer;
    }

    override protected IEnumerator ScanningBehaviorE() {
        while (true) {
            yield return new WaitForSeconds(general.scanningRate);
            if (Focused && IsThreat(Focus)) continue;
            
            if (State == CreatureState.Roam || State == CreatureState.Station)
                Focus = NearestThreat((threat) => threat.GetComponent<Archer>() == null);
            else if (State == CreatureState.FollowOffensive) UpdateFollowOffensive();
            else Focus = null;
        }
    }

    override protected IEnumerator FocusedBehaviorE() {
        while (Focused) {
            if (Vector2.Distance(Focus.position, transform.position) < archer.arrowPrefab.reach) {
                velocity = Vector2.zero;
                Arrow.Instantiate(archer.arrowPrefab, grid, transform, Focus.position);
            } else if (State != CreatureState.Station) {
                velocity = IndexedVelocity(Focus.position - transform.position);
            }
            yield return new WaitForSeconds(general.reconsiderRatePursuit);
        }
        Debug.Log("Focus just ended, exiting FocusedBehavior");
    }
    
}