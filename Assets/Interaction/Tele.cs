using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Target : OneOf<Terrain.Position, Character> {
    private Target() : base() {}
    public Target(Terrain.Position t) : base(t) {}
    public Target(Character u) : base(u) {}
    new public static Target Neither { get => new Target(); }

    public Vector3 Position {
        get {
            if (this.Is(out Terrain.Position position))
                return Terrain.I.CellCenter(position);
            else if (this.Is(out Character character))
                return character.transform.position;
            else throw new InvalidOperationException("Neither");
        }
    }
}

public class TeleFilter {
    public enum Terrain {
        NONE,
        TILES,
        WOODBUILDING
    }

    public Terrain terrainSelection;
    public Func<Transform, bool> characterFilter;
    public Func<List<Vector3>> line { get; private set; } = null;

    public TeleFilter(Terrain terrainSelection,
            Func<Transform, bool> characterFilter) {
        this.terrainSelection = terrainSelection;
        this.characterFilter = characterFilter;
    }

    public TeleFilter WithLine(Func<List<Vector3>> line) {
        this.line = line;
        return this;
    }
}

public class Tele {
    private Terrain terrain;
    private TeleFilter dynamicFilter;

    public TeleFilter DynamicFilter {
        set => dynamicFilter = value;
    }

    public Tele(Terrain terrain) {
        this.terrain = terrain;
    }

    public Func<List<Vector3>> Line { get => dynamicFilter.line; }

    public Target SelectDynamic(Vector2 point) {
        if (dynamicFilter == null) throw new InvalidOperationException("No dynamic filter set");
        if (dynamicFilter.characterFilter != null) {
            Character character = SelectCharacterWithFilter(point, dynamicFilter.characterFilter);
            if (character != null) return new Target(character);
        }
        switch (dynamicFilter.terrainSelection) {
            case (TeleFilter.Terrain.TILES):
                Vector2Int tile = SelectSquareOnly(point);
                return new Target(new Terrain.Position(Terrain.Grid.Roof, tile));
            case (TeleFilter.Terrain.WOODBUILDING):
                Terrain.Position? maybePosition = SelectBuildLoc(point);
                if (maybePosition is Terrain.Position position)
                    return new Target(position);
                break;
        }
        return Target.Neither;
    }

    public Vector2Int SelectSquareOnly(Vector2 point) {
        return terrain.CellAt(point);
    }

    public Character SelectCharacterOnly(Vector2 point) {
        Collider2D[] colliders = Physics2D.OverlapPointAll(point, LayerMask.GetMask("Clickable"));
        if (colliders.Length > 0) return colliders
                .Select(collider => collider.transform.parent)
                .MinBy(transform => Vector2.Distance((Vector2)transform.position, point))
                .GetComponentStrict<Character>();
        return null;
    }

    public Character SelectCharacterWithFilter(Vector2 point, Func<Transform, bool> characterFilter) {
        Collider2D[] colliders = Physics2D.OverlapPointAll(point, LayerMask.GetMask("Clickable"));
        if (colliders.Length == 0) return null;
        IEnumerable<Transform> charactersFiltered = colliders
                .Select(collider => collider.transform.parent)
                .Where(characterFilter);
        if (charactersFiltered.Count() == 0) return null;
        return charactersFiltered
                .MinBy(transform => Vector2.Distance((Vector2)transform.position, point))
                .GetComponentStrict<Character>();
    }

    public Terrain.Position? SelectBuildLoc(Vector2 point) {
        Terrain.Position pos = Terrain.Position.zero;
        Vector2Int tile = Vector2Int.zero;
        if (IsCenter(point, ref tile)) {
            if (CanBuildRoof(tile)) {
                return new Terrain.Position(Terrain.Grid.Roof, tile);
            } else if (CanBuildWall(point, ref pos)) {
                return pos;
            }
        } else {
            if (CanBuildWall(point, ref pos)) {
                return pos;
            } else if (CanBuildRoof(tile)) {
                return new Terrain.Position(Terrain.Grid.Roof, tile);
            }
        }
        return null;
    }

    private bool IsCenter(Vector2 worldPoint, ref Vector2Int tile) {
        tile = terrain.CellAt(worldPoint);
        Vector2 center = terrain.CellCenter(tile);
        Vector2 diff = worldPoint - center;
        return Mathf.Abs(diff.x) < 0.25f && Mathf.Abs(diff.y) < 0.25f;
    }

    private Terrain.Position GetEdgeAt(Vector2 worldPoint) {
        Vector2Int pos = terrain.CellAt(worldPoint);
        Vector2 center = terrain.CellCenter(pos);
        Vector2 diff = worldPoint - center;
        bool upperLeftQuad = diff.y > diff.x;
        bool upperRightQuad = diff.y + diff.x > 0;
        if (upperLeftQuad ^ upperRightQuad) {
            return new Terrain.Position(Terrain.Grid.YWalls, upperRightQuad ? pos + Vector2Int.right : pos);
        } else {
            return new Terrain.Position(Terrain.Grid.XWalls, upperRightQuad ? pos + Vector2Int.up : pos);
        }
    }

    private bool CanBuildWall(Vector2 worldPoint, ref Terrain.Position pos) {
        pos = GetEdgeAt(worldPoint);
        return terrain.InBounds(pos) &&
                terrain[pos] == Construction.None &&
                terrain.validator.IsStableConstruction(pos, Construction.Wood);
    }

    private bool CanBuildRoof(Vector2Int tile) {
        return terrain.InBounds(tile) &&
                terrain.Roof[tile] == Construction.None &&
                terrain.validator.IsStableConstruction(new Terrain.Position(Terrain.Grid.Roof, tile), Construction.Wood);
    }

}
