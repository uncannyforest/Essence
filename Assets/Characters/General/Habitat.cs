using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Habitat {

    public const float besideDistance = 1; // close enough to 1.5 * sqrt(0.5)

    private HashSet<Vector2Int> recentlyVisited = new HashSet<Vector2Int>();
    private float restAgainTime = 0;
    private float restDuration = 10;
    public void ClearRecentlyVisited() {
        recentlyVisited.Clear();
        restAgainTime = 0;
        restDuration = 10;
    }

    public readonly Radius restRadius;
    protected readonly Brain brain;

    public Habitat(Brain brain, Radius restRadius) {
        this.brain = brain;
        this.restRadius = restRadius;
    }

    static public Habitat Feature(Brain brain, FeatureConfig feature, Radius mode = Radius.Nearby) => new Habitat(brain, feature, mode);
    public Habitat(Brain brain, FeatureConfig feature, Radius mode = Radius.Nearby)
        : this(brain, mode) {
        IsShelter = (loc) => feature == Terrain.I.Feature[loc]?.config; // okay??
    }

    static public Habitat Land(Brain brain, Land land, Radius mode) => new Habitat(brain, land, mode);
    public Habitat(Brain brain, Land land, Radius mode)
        : this(brain, mode) {
        IsShelter = (loc) => Terrain.I.GetLand(loc) == land;
    }

    public Func<Vector2Int, bool> IsShelter;

    private IEnumerable<Vector2Int> ValidShelterLocations(Radius radius) => radius.Center(brain.transform.position);

    public bool IsPresent(Radius radius) {
        foreach (Vector2Int validShelterLocation in ValidShelterLocations(radius))
            if (IsShelter(validShelterLocation)) return true;
        return false;
    }
    public bool IsPresent() => IsPresent(restRadius);
    virtual public bool CanTame() => IsPresent();

    public Optional<Vector2Int> FindShelter() {
        if (Time.time < restAgainTime) return Optional<Vector2Int>.Empty();
        recentlyVisited.RemoveWhere((visited) =>
            Disp.FT(brain.transform.position, Terrain.I.CellCenter(visited)) > Creature.neighborhood);
        Optional<Vector2Int> result = Radius.Nearby.ClosestTo(brain.transform.position, (loc) => !recentlyVisited.Contains(loc) && IsShelter(loc));
        if (brain.teamId != 0) Debug.Log(brain.legalName + " searched for shelter nearby, result: " + result);
        return result;
    }

    public IEnumerator<YieldInstruction> ApproachAndRestBehavior(Vector2Int shelter) {
        IEnumerator<YieldInstruction> approach;
        switch (restRadius) {
            case Radius.Inside:
                approach = brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), 1f / CharacterController.subGridUnit);
                break;
            case Radius.Beside:
            case Radius.Nearby:
            default:
                approach = brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), besideDistance);
                break;
        }

        while (approach.MoveNext()) yield return approach.Current;
        IEnumerator<YieldInstruction> restBehavior = RestBehavior(shelter);
        while (restBehavior.MoveNext()) yield return restBehavior.Current;
    }

    virtual public IEnumerator<YieldInstruction> RestBehavior(Vector2Int shelter) => RestBehaviorDefault(shelter);

    private IEnumerator<YieldInstruction> RestBehaviorDefault(Vector2Int shelter) {
        recentlyVisited.Add(shelter);
        float transitionTime = Time.time + restDuration;
        restDuration += 5;
        while (Time.time < transitionTime || brain.resource?.IsFull() == false) {
            brain.resource?.Increase(1);
            yield return new WaitForSeconds(brain.creature.stats.ExeTime);
        }
        restAgainTime = Time.time + restDuration;
        restDuration += 5;
    }

    public IEnumerator<YieldInstruction> RestBehaviorSleep() {
        while (true) {
            brain.resource?.Increase(1);
            yield return new WaitForSeconds(brain.creature.stats.ExeTime);
        }
    }

    public IEnumerator<YieldInstruction> RestBehaviorConsume(Vector2Int shelter, Func<float> consumeTime, Action consume) =>
        Provisionally.Run(Enumerators.AfterWait(consumeTime, consume))
            .Where(yi => brain.resource?.IsFull() != true)
            .Then(RestBehaviorDefault(shelter));
}

public class ConsumableFeatureHabitat : Habitat {
    private Func<float> consumeTime;

    public ConsumableFeatureHabitat(Brain brain, FeatureConfig feature, Func<float> consumeTime) : base(brain, feature) {
        this.consumeTime = consumeTime;
    }

    override public IEnumerator<YieldInstruction> RestBehavior(Vector2Int shelter) =>
        RestBehaviorConsume(shelter, consumeTime, () => {
            brain.resource?.Increase(Terrain.I.Feature[shelter]?.config?.resourceQuantity ?? 1);
            Terrain.I.DestroyFeature(shelter);
    });
}

public class SleepHabitat : Habitat {
    public SleepHabitat(Brain brain, Radius restRadius) : base(brain, restRadius) {}

    override public IEnumerator<YieldInstruction> RestBehavior(Vector2Int shelter) => RestBehaviorSleep();
}