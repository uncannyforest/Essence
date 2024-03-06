using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Concealment {
    private Terrain terrain;
    private Tilemap[] mainTiles;
    private Tilemap transparentTiles;
    private Dictionary<Construction, TileBase> buildingTiles;

    private Vector2Int currentCenter = Vector2Int.zero;
    private int[] currentCardinalExtents = new int[] {0, 0, 0, 0};

    public Concealment(Terrain terrain) {
        this.terrain = terrain;
    }

    public void Initialize(Tilemap[] mainTiles) {
        this.mainTiles = mainTiles;

        buildingTiles = new Dictionary<Construction, TileBase> {
            [Construction.None] = null,
            [Construction.Wood] = terrain.tiles.woodBldg
        };
        transparentTiles = terrain.transform.Find("Transparent").GetComponent<Tilemap>();
        GameObject.FindObjectOfType<PointOfView>().CrossedTile += HandleCrossedTile;
    }

    public bool CanSee(Vector3 seerPosition, Character seen) {
        Construction? expectedConstruction = terrain.Roof.Get(terrain.CellAt(seen.transform.position));
        if (expectedConstruction != null && expectedConstruction != Construction.None) {
            Vector2Int cellDistance =
                terrain.CellAt(seerPosition) - terrain.CellAt(seen.transform.position);
            for (int i = 0; i < 4; i++) {
                Terrain.Position wall = Terrain.Position.Edge(currentCenter, i);
                Vector2Int roof = currentCenter + Vct.Cardinals[i];
                if (terrain.GetConstruction(wall) != expectedConstruction &&
                    terrain.Roof.Get(roof) != expectedConstruction) return true;
            }
            if (cellDistance.IsCardinal()) {
                // At this point, we know we have an opening to an adjacent roof
                // in the direction of the seer — OR a wall.
                // Now we have to check for nonexistence of n walls, and existence of
                // n - 2 roofs (already knowing there's one above the object and one adjacent,
                // if not a wall), where n is the distance to seer.
                int i = cellDistance.GetCardinal();
                int distanceToSeer = cellDistance.CardinalMagnitude();
                for (int j = 0; j < distanceToSeer; j++) {
                    Terrain.Position wall = Terrain.Position.Edge(currentCenter + j * Vct.Cardinals[i], i);
                    if (terrain.GetConstruction(wall) == expectedConstruction) return false;
                    if (j < 2) continue; // per reasoning above
                    Vector2Int roof = currentCenter + j * Vct.Cardinals[i];
                    if (terrain.Roof.Get(roof) != expectedConstruction) return false;
                }
                // There is an unbroken hallway containing the seen object and which the seer
                // is either inside or just outside of.
                return true;
            }
            return false;
        }
        Land? land = terrain.GetLand(terrain.CellAt(seen.transform.position));
        if (land?.PlantLevel() >= seen.height) {
            if (Disp.FT(seerPosition, seen.transform.position) < 2.5f)
                return true;
            foreach (Land? adjLand in terrain.GetFourLandTilesAround(seen.transform.position))
                if (adjLand == null || adjLand?.PlantLevel() < seen.height)
                    return true;
            return false;
        }
        return true;
    }

    private void HandleCrossedTile(Vector2Int curr) {
        Vector2Int oldCenter = currentCenter;
        int[] oldCardinalExtents = new int[4];
        currentCardinalExtents.CopyTo(oldCardinalExtents, 0);

        currentCenter = curr;
        UpdateExtents();
        UpdateTile(oldCenter);
        UpdateTile(currentCenter, true);
        for(int i = 0; i < 4; i++) {
            for (int j = 1; j <= oldCardinalExtents[i]; j++)
                UpdateTile(oldCenter + j * Vct.Cardinals[i]);
            for (int j = 1; j <= currentCardinalExtents[i]; j++)
                UpdateTile(currentCenter + j * Vct.Cardinals[i], true);
        }
    }

    private void UpdateExtents() {
        for (int i = 0; i < 4; i++) {
            Construction? expectedConstruction = terrain.Roof.Get(currentCenter + Vct.Cardinals[i]);
            if (expectedConstruction == null || expectedConstruction == Construction.None) {
                currentCardinalExtents[i] = 0;
                continue;
            }
            int j = 0;
            for ( ; ; j++) {
                Terrain.Position wall = Terrain.Position.Edge(currentCenter + j * Vct.Cardinals[i], i);
                if (terrain.GetConstruction(wall) != Construction.None) break;
                Vector2Int roof = currentCenter + (j + 1) * Vct.Cardinals[i];
                if (terrain.Roof.Get(roof) != expectedConstruction) break;
            }
            currentCardinalExtents[i] = j;
        }
    }

    private void UpdateTile(Vector2Int pos) {
        if (pos == currentCenter) UpdateTile(pos, true);
        else {
            Vector2Int relativePos = pos - currentCenter;
            if (!relativePos.IsCardinal()) UpdateTile(pos, false);
            else {
                int cardinal = relativePos.GetCardinal();
                int magnitude = relativePos.CardinalMagnitude();
                UpdateTile(pos, magnitude <= currentCardinalExtents[cardinal]);
            }
        }
    }

    private void UpdateTile(Vector2Int pos, bool hide) {
        if (hide) {
            mainTiles[Terrain.RoofLevel].SetTile((Vector3Int)pos, null);
            transparentTiles.SetTile((Vector3Int)pos, buildingTiles[terrain.Roof.Get(pos) ?? Construction.None]);
        } else {
            mainTiles[Terrain.RoofLevel].SetTile((Vector3Int)pos, buildingTiles[terrain.Roof.Get(pos) ?? Construction.None]);
            transparentTiles.SetTile((Vector3Int)pos, null);
        }
    }

    private Vector2Int[] FourTilesAround(Vector2 pos) {
        Vector2Int firstCell = terrain.CellAt(pos + Vct.F(0, -.5f));
        return new Vector2Int[] {
            firstCell,
            firstCell + Vct.I(1, 0),
            firstCell + Vct.I(0, 1),
            firstCell + Vct.I(1, 1)
        };
    }
}
