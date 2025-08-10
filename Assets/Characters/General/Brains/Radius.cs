using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum Radius {
    Inside,
    Beside,
    Nearby
}

public static class RadiusExtensions {
    public static IEnumerable<Vector2Int> Center(this Radius mode, Brain brain) => mode.Center(brain.transform.position);

    public static IEnumerable<Vector2Int> Center(this Radius mode, Vector2 position) {
        Vector2Int center;
        switch (mode) {
            case Radius.Inside:
                center = Terrain.I.CellAt(position);
                if (Terrain.I.InBounds(center)) yield return center;
                yield break;
            case Radius.Beside:
                center = Terrain.I.CellAt(position);
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                    if (Terrain.I.InBounds(center + Vct.I(x, y))) yield return center + Vct.I(x, y);
                yield break;
            case Radius.Nearby:
                center = Terrain.I.CellAt(position);
                for (int x = -7; x <= 7; x++) for (int y = -7; y <= 7; y++) {
                    Vector2Int possShelterLocation = center + Vct.I(x, y);
                    if (Terrain.I.InBounds(possShelterLocation) &&
                            Disp.FT(position, Terrain.I.CellCenter(possShelterLocation)) <= Creature.neighborhood)
                        yield return possShelterLocation;
                }
                yield break;
        }
    }

    public static Optional<Vector2Int> ClosestTo(this Radius mode, Vector2 position, Func<Vector2Int, bool> criterion) {
        return (from location in mode.Center(position)
            where criterion(location)
            orderby SqrDistance(position, location)
            select Optional.Of(location)).FirstOrDefault();
    }

    private static float SqrDistance(Vector2 position, Vector2Int loc) => Disp.FT(position, Terrain.I.CellCenter(loc)).sqrMagnitude;
}
