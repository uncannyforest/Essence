using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ArcherConfig {
    public Sprite attackAction;
    public int scaleCost;
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

    override public bool CanTame(Transform player) =>
        player.GetComponentStrict<Inventory>().CanRetrieve(Material.Type.Scale, archer.scaleCost);

    // Returns true if successful.
    public override bool ExtractTamingCost(Transform player) {
        return player.GetComponentStrict<Inventory>().Retrieve(Material.Type.Scale, archer.scaleCost);
    }

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
            CreatureAction.WithObject(archer.attackAction,
                new CoroutineWrapper(AttackBehaviorE, species),
                new TeleFilter(TeleFilter.Terrain.NONE, (c) => { Debug.Log(c.GetComponent<Health>() + " " + c.GetComponentStrict<Team>().TeamId); return
                    c.GetComponent<Health>() != null &&
                    c.GetComponentStrict<Team>().TeamId != team ;}
                ))
        };
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
            yield return Attack(Focus);
        }
        Debug.Log("Focus just ended, exiting FocusedBehavior");
    }
    
    private IEnumerator AttackBehaviorE() {
        while (((SpriteSorter)executeDirective) != null) {
            yield return Attack(((SpriteSorter)executeDirective).Character);
        }
        Debug.Log("Attack just ended, exiting AttackBehavior");
    }

    private WaitForSeconds Attack(Transform target) {
        if (Vector2.Distance(target.position, transform.position) < archer.arrowPrefab.reach) {
            velocity = Vector2.zero;
            Arrow.Instantiate(archer.arrowPrefab, grid, transform, target.position);
        } else if (State != CreatureState.Station) {
            velocity = IndexedVelocity(target.position - transform.position);
        }
        return new WaitForSeconds(general.reconsiderRatePursuit);
    }
}