using System;
using UnityEngine;

public struct CreatureAction {
    public readonly Sprite icon;
    public readonly Action<Creature> instantDirective;
    public readonly Action<Creature, Target> pendingDirective;
    public readonly TeleFilter dynamicFilter;
    public readonly bool canQueue;
    public readonly bool isRoam;
    public readonly bool isStation;
    public readonly Feature feature;
    private CreatureAction(Sprite icon, Action<Creature> instantDirective, 
            Action<Creature, Target> pendingDirective,
            TeleFilter filter, Feature feature, bool canQueue, bool isRoam, bool isStation) {
        this.icon = icon;
        this.instantDirective = instantDirective;
        this.pendingDirective = pendingDirective;
        this.dynamicFilter = filter;
        this.canQueue = canQueue;
        this.isRoam = isRoam;
        this.isStation = isStation;
        this.feature = feature;
    }

    public static CreatureAction Instant(Sprite icon, Action<Creature> instantDirective) =>
        new CreatureAction(icon, instantDirective, null, null, null, false, false, false);
    public static CreatureAction WithObject(Sprite icon,
            TargetedBehavior<Target> executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.Execute(executingBehavior.WithTarget(target)),
            filter, null, executingBehavior.canQueue, false, false);
    public static CreatureAction WithCharacter(Sprite icon,
            TargetedBehavior<Transform> executingBehavior,
            Func<Transform, bool> characterFilter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.Execute(executingBehavior.WithTarget(((Character)target).transform)),
            new TeleFilter(TeleFilter.Terrain.NONE, characterFilter),
            null, executingBehavior.canQueue, false, false);
    public static CreatureAction WithTerrain(Sprite icon,
            TargetedBehavior<Terrain.Position> executingBehavior,
            TeleFilter.Terrain terrainFilter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.Execute(executingBehavior.WithTarget((Terrain.Position)target)),
            new TeleFilter(terrainFilter, null),
            null, executingBehavior.canQueue, false, false);
    public static CreatureAction WithFeature(Feature feature,
            TargetedBehavior<Vector2Int> executingBehavior) =>
        new CreatureAction(null, null,
            (creature, target) => creature.Execute(executingBehavior.WithTarget(((Terrain.Position)target).Coord)),
            new TeleFilter(TeleFilter.Terrain.TILES, null),
            feature, executingBehavior.canQueue, false, false);
    public static CreatureAction Roam =
        new CreatureAction(null, (creature) => creature.CommandRoam(), null, null, null, false, true, false);
    public static CreatureAction Station =
        new CreatureAction(null, null,
            (creature, location) => creature.Station(((Terrain.Position)location).Coord),
            new TeleFilter(TeleFilter.Terrain.TILES, null),
            null, false, false, true);

    public bool IsInstant {
        get => instantDirective != null;
    }
    public bool UsesFeature {
        get => feature != null;
    }
}
