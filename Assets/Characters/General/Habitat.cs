using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Habitat {

    public const float besideDistance = 1; // close enough to 1.5 * sqrt(0.5)

    public enum InteractionMode {
        Inside,
        Beside,
        Nearby
    }

    private HashSet<Vector2Int> recentlyVisited = new HashSet<Vector2Int>();
    private float restAgainTime = 0;
    private float restDuration = 10;
    public void ClearRecentlyVisited() {
        recentlyVisited.Clear();
        restAgainTime = 0;
        restDuration = 10;
    }

    public readonly InteractionMode restRadius;
    protected readonly Brain brain;

    public Habitat(Brain brain, InteractionMode restRadius) {
        this.brain = brain;
        this.restRadius = restRadius;
    }

    static public Habitat Feature(Brain brain, Feature feature) => new Habitat(brain, feature);
    public Habitat(Brain brain, Feature feature)
        : this(brain, InteractionMode.Nearby) {
        IsShelter = (loc) => Terrain.I.Feature[loc]?.type == feature.type;
        MakeShelter = new List<Action<Vector2Int>>() { (loc) => Terrain.I.ForceBuildFeature(loc, feature) };
    }

    static public Habitat Land(Brain brain, Land land, InteractionMode mode) => new Habitat(brain, land, mode);
    public Habitat(Brain brain, Land land, InteractionMode mode)
        : this(brain, mode) {
        IsShelter = (loc) => Terrain.I.GetLand(loc) == land;
        MakeShelter = new List<Action<Vector2Int>>() { (loc) => Terrain.I.SetLand(loc, land, true) };
    }

    public Func<Vector2Int, bool> IsShelter;

    public List<Action<Vector2Int>> MakeShelter; // return list of steps for IEnumerable

    private IEnumerable<Vector2Int> ValidShelterLocations(InteractionMode mode) {
        Vector2Int center;
        switch (mode) {
            case InteractionMode.Inside:
                center = Terrain.I.CellAt(brain.transform.position);
                if (Terrain.I.InBounds(center)) yield return center;
                yield break;
            case InteractionMode.Beside:
                center = Terrain.I.CellAt(brain.transform.position);
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                    if (Terrain.I.InBounds(center + Vct.I(x, y))) yield return center + Vct.I(x, y);
                yield break;
            case InteractionMode.Nearby:
                center = Terrain.I.CellAt(brain.transform.position);
                for (int x = -7; x <= 7; x++) for (int y = -7; y <= 7; y++) {
                    Vector2Int possShelterLocation = center + Vct.I(x, y);
                    if (Terrain.I.InBounds(possShelterLocation) &&
                            Disp.FT(brain.transform.position, Terrain.I.CellCenter(possShelterLocation)) <= Creature.neighborhood)
                        yield return possShelterLocation;
                }
                yield break;
        }
    }

    private float SqrDistance(Vector2Int loc) => Disp.FT(brain.transform.position, Terrain.I.CellCenter(loc)).sqrMagnitude;

    virtual public bool CanTame() {
        foreach (Vector2Int validShelterLocation in ValidShelterLocations(restRadius))
            if (IsShelter(validShelterLocation)) return true;
        return false;
    }

    public Optional<Vector2Int> FindShelter() {
        if (Time.time < restAgainTime) return Optional<Vector2Int>.Empty();
        recentlyVisited.RemoveWhere((visited) =>
            Disp.FT(brain.transform.position, Terrain.I.CellCenter(visited)) > Creature.neighborhood);
        return (from location in ValidShelterLocations(InteractionMode.Nearby)
            where !recentlyVisited.Contains(location) && IsShelter(location)
            orderby SqrDistance(location)
            select Optional.Of(location)).FirstOrDefault();
    }

    virtual public IEnumerator<YieldInstruction> RestBehavior(Vector2Int shelter) {
        IEnumerator<YieldInstruction> approach;
        switch (restRadius) {
            case InteractionMode.Inside:
                approach = brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), 1f / CharacterController.subGridUnit);
                break;
            case InteractionMode.Beside:
            case InteractionMode.Nearby:
            default:
                approach = brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), besideDistance);
                break;
        }

        while (approach.MoveNext()) yield return approach.Current;
        recentlyVisited.Add(shelter);
        float transitionTime = Time.time + restDuration;
        restDuration += 5;
        while (Time.time < transitionTime) {
            brain.resource?.Increase(1);
            yield return new WaitForSeconds(brain.creature.stats.ExeTime);
        }
        restAgainTime = Time.time + restDuration;
        restDuration += 5;
    }

    public IEnumerator<YieldInstruction> RestBehaviorConsume(Vector2Int shelter, Func<float> consumeTime, Action consume) {
        switch (restRadius) {
            case InteractionMode.Inside:
                return brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), 1f / CharacterController.subGridUnit)
                        .ThenOnce(consumeTime, consume);
            case InteractionMode.Beside:
            case InteractionMode.Nearby:
            default:
                return brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), besideDistance)
                        .ThenOnce(consumeTime, consume);
        }
    }
}

public class WoodpileHabitat : Habitat {
    private Func<float> consumeTime;

    public WoodpileHabitat(Brain brain, Func<float> consumeTime) : base(brain, global::Land.Woodpile, Habitat.InteractionMode.Inside) {
        this.consumeTime = consumeTime;
    }

    override public IEnumerator<YieldInstruction> RestBehavior(Vector2Int shelter) =>
        RestBehaviorConsume(shelter, consumeTime, () => {
            Terrain.I.SetLand(shelter, global::Land.Grass, true);
            brain.resource?.Increase(5);
    });
}

public class ConsumableFeatureHabitat : Habitat {
    private Func<float> consumeTime;
    private int consumeQuantity;

    public ConsumableFeatureHabitat(Brain brain, Feature feature, Func<float> consumeTime, int consumeQuantity) : base(brain, feature) {
        this.consumeTime = consumeTime;
        this.consumeQuantity = consumeQuantity;
    }

    override public IEnumerator<YieldInstruction> RestBehavior(Vector2Int shelter) =>
        RestBehaviorConsume(shelter, consumeTime, () => {
            Terrain.I.DestroyFeature(shelter);
            brain.resource?.Increase(consumeQuantity);
    });

}