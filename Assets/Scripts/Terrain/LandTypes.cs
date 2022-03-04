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