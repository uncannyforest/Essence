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

    public Func<Vector2Int, IEnumerable> MakeShelter;

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
                for (int x = -8; x <= 8; x++) for (int y = -8; y <= 8; y++) {
                    Vector2Int possShelterLocation = center + Vct.I(x, y);
                    if (Terrain.I.InBounds(possShelterLocation) &&
                            Disp.FT(brain.transform.position, Terrain.I.CellCenter(possShelterLocation)) <= Creature.neighborhood)
                        yield return possShelterLocation;
                }
                yield break;
        }
    }

    public bool MinLevelUp() {
        foreach (Vector2Int validShelterLocation in ValidShelterLocations(InteractionMode.Nearby))
            if (IsShelter(validShelterLocation)) return true;
        return false;
    }

    virtual public bool MaxLevelUp() {
        foreach (Vector2Int validShelterLocation in ValidShelterLocations(restRadius))
            if (IsShelter(validShelterLocation)) return true;
        return false;
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

    virtual public BehaviorNode RestBehavior(Vector2Int shelter) {
        switch (restRadius) {
            case InteractionMode.Inside:
                return brain.pathfinding.ApproachThenInteract(1f / CharacterController.subGridUnit, Random.value * brain.general.lureMaxTime, (_) => recentlyVisited.Add(shelter))
                    .WithTarget(new Terrain.Position(Terrain.Grid.Roof, shelter));
            case InteractionMode.Beside:
            case InteractionMode.Nearby:
            default:
                return brain.pathfinding.ApproachThenInteract(besideDistance, brain.general.lureMaxTime, (_) => recentlyVisited.Add(shelter))
                    .WithTarget(new Terrain.Position(Terrain.Grid.Roof, shelter));
        }
    }

}
