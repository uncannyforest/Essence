public enum Land {
    Grass = 0,
    Meadow,
    Shrub,
    Forest,
    Quirk,
    Road,
    Water,
    Ditch,
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
    Quirk = 1 << 4,
    Road = 1 << 5,
    Water = 1 << 6,
    Ditch = 1 << 7,
    Woodpile = 1 << 8,
    Rockpile = 1 << 9,
    Hill = 1 << 10,
    PavedTunnel = 1 << 11,
    WaterTunnel = 1 << 12,
    DirtTunnel = 1 << 13,
}

public enum Construction {
    None = 0,
    Wood,
    Stone
}

public static class LandExtensions {
    public static bool IsPassable(this Land land) {
        return land != Land.Quirk && land != Land.Rockpile && land != Land.Hill;
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