using System;
using UnityEngine;

public enum Land {
    Grass = 0,
    Meadow,
    Shrub,
    Forest,
    Bramble,
    Road,
    Water,
    Ditch,
    Dirtpile,
    Woodpile,
    Rockpile,
    Hill,
    PavedTunnel,
    WaterTunnel,
    DirtTunnel,
}

// Use only for serialized attributes for Unity UI: not internal code
[System.Flags]
public enum LandFlags {
    Grass = 1,
    Meadow = 1 << 1,
    Shrub = 1 << 2,
    Forest = 1 << 3,
    Bramble = 1 << 4,
    Road = 1 << 5,
    Water = 1 << 6,
    Ditch = 1 << 7,
    Dirtpile = 1 << 8,
    Woodpile = 1 << 9,
    Rockpile = 1 << 10,
    Hill = 1 << 11,
    PavedTunnel = 1 << 12,
    WaterTunnel = 1 << 13,
    DirtTunnel = 1 << 14,
}

public enum Construction {
    None = 0,
    Wood,
    Stone
}

[Serializable] public class TileMaterial {
    public enum Level {
        Land,
        Roof
    }
    public Level level;
    [SerializeField] private Land land;
    [SerializeField] private Construction roof;

    public TileMaterial(Level level) {
        this.level = level;
    }
    private TileMaterial(Level level, Land land, Construction roof) {
        this.level = level;
        this.land = land;
        this.roof = roof;
    }

    public static implicit operator TileMaterial(Land land) => new TileMaterial(Level.Land, land, default);
    public static implicit operator TileMaterial(Construction roof) => new TileMaterial(Level.Roof, default, roof);
    public static explicit operator Land(TileMaterial tm) {
        if (tm.level != Level.Land) throw new InvalidCastException("Not Land");
        return tm.land;
    }
    public static explicit operator Construction(TileMaterial tm) {
        if (tm.level != Level.Roof) throw new InvalidCastException("Not Roof");
        return tm.roof;
    }

    public int GetEnumHashCode() => level == Level.Land ? (int)land : (int)roof;

    public override int GetHashCode() => (int)level * 100000 + GetEnumHashCode();

    public static bool operator ==(TileMaterial a, TileMaterial b) {
        if (a is null || b is null) return (a is null) && (b is null);
        if (a.level != b.level) return false;
        if (a.level == Level.Land) return a.land == b.land;
        if (a.level == Level.Roof) return a.roof == b.roof;
        throw new NotImplementedException("Did we add more Levels to TileMaterial?");
    }
    public static bool operator !=(TileMaterial a, TileMaterial b) => !(a == b);

    public override bool Equals(object obj) {
        return obj is TileMaterial && (TileMaterial)obj == this;
    }
}

public static class LandExtensions {
    public static bool IsPassable(this Land land) {
        return land != Land.Dirtpile && land != Land.Rockpile && land != Land.Hill;
    }

    public static bool IsHilly(this Land land) {
        return land >= Land.Hill;
    }

    public static bool IsWatery(this Land land) {
        return land == Land.Water || land == Land.WaterTunnel;
    }

    public static bool IsDitchy(this Land land) {
        return land == Land.Ditch || land == Land.DirtTunnel;
    }

    public static bool IsPlanty(this Land land) {
        return land == Land.Meadow || land == Land.Shrub || land == Land.Forest;
    }

    public static int PlantLevel(this Land land) {
        int number = (int) land;
        return number <= 3 ? number : 0;
    }
}