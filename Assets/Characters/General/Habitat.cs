using System;
using System.Collections;
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

    public readonly InteractionMode restRadius;
    private readonly Brain brain;

    public Habitat(Brain brain, InteractionMode restRadius) {
        this.brain = brain;
        this.restRadius = restRadius;
    }

    public Func<Vector2Int, bool> IsShelter;

    public List<Action<Vector2Int>> MakeShelter; // return list of steps for IEnumerable

    public IEnumerable<Vector2Int> ValidShelterLocations(InteractionMode mode) {
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
    
    virtual public bool CanTame() {
        foreach (Vector2Int validShelterLocation in ValidShelterLocations(restRadius))
            if (IsShelter(validShelterLocation)) return true;
        return false;
    }

    public Optional<Vector2Int> FindShelter() {
        recentlyVisited.RemoveWhere((visited) =>
            Disp.FT(brain.transform.position, Terrain.I.CellCenter(visited)) > Creature.neighborhood);
        foreach (Vector2Int validShelterLocation in ValidShelterLocations(InteractionMode.Nearby))
            if (!recentlyVisited.Contains(validShelterLocation) && IsShelter(validShelterLocation))
                return Optional.Of(validShelterLocation);
        return Optional<Vector2Int>.Empty();
    }

    public void RestInteraction(Vector2Int shelter) {
        recentlyVisited.Add(shelter);
        brain.resource?.Increase(1);
    }

    virtual public IEnumerator RestBehavior(Vector2Int shelter) {
        switch (restRadius) {
            case InteractionMode.Inside:
                return brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), 1f / CharacterController.subGridUnit)
                        .ThenEvery(brain.creature.stats.ExeTime,
                        () => RestInteraction(shelter));
            case InteractionMode.Beside:
            case InteractionMode.Nearby:
            default:
                return brain.pathfinding.Approach(Terrain.I.CellCenter(shelter), besideDistance)
                        .ThenEvery(brain.creature.stats.ExeTime,
                        () => RestInteraction(shelter));
        }
    }

    static public Habitat Feature(Brain brain, Feature feature) {
        return new Habitat(brain, InteractionMode.Nearby) {
            IsShelter = (loc) => Terrain.I.Feature[loc]?.type == feature.type,
            MakeShelter = new List<Action<Vector2Int>>() { (loc) => Terrain.I.ForceBuildFeature(loc, feature) }
        };
    }
}
