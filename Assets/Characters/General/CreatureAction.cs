using System;
using UnityEngine;

public struct CreatureAction {
    public readonly Sprite icon;
    public readonly Action<Creature> instantDirective;
    public readonly Action<Creature, Target> pendingDirective;
    public readonly TeleFilter dynamicFilter;
    public readonly bool keepFollowing;
    public readonly bool canQueue;
    public readonly bool isRoam;
    public readonly bool isStation;
    public readonly FeatureConfig feature;
    private CreatureAction(Sprite icon, Action<Creature> instantDirective = null, 
            Action<Creature, Target> pendingDirective = null,
            TeleFilter filter = null, FeatureConfig feature = null, bool keepFollowing = false, bool canQueue = false, bool isRoam = false, bool isStation = false) {
        this.icon = icon;
        this.instantDirective = instantDirective;
        this.pendingDirective = pendingDirective;
        this.dynamicFilter = filter;
        this.keepFollowing = keepFollowing;
        this.canQueue = canQueue;
        this.isRoam = isRoam;
        this.isStation = isStation;
        this.feature = feature;
    }

    public static CreatureAction Instant(Sprite icon, Action<Creature> instantDirective, bool keepFollowing = false) =>
        new CreatureAction(icon, instantDirective: instantDirective, keepFollowing: keepFollowing);
    public static CreatureAction WithObject(Sprite icon,
            TargetedBehavior<Target> executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon,
            pendingDirective: (creature, target) => creature.ProcessDirective(executingBehavior.WithTarget(target)),
            filter: filter,
            canQueue: executingBehavior.canQueue);
    public static CreatureAction WithCharacter(Sprite icon,
            TargetedBehavior<Transform> executingBehavior,
            Func<Transform, WhyNot> characterFilter) =>
        new CreatureAction(icon,
            pendingDirective: (creature, target) => creature.ProcessDirective(executingBehavior.WithTarget(((Character)target).transform)),
            filter: new TeleFilter(TeleFilter.Terrain.NONE, characterFilter),
            canQueue: executingBehavior.canQueue);
    public static CreatureAction WithTerrain(Sprite icon,
            TargetedBehavior<Terrain.Position> executingBehavior,
            TeleFilter.Terrain terrainFilter) =>
        new CreatureAction(icon,
            pendingDirective: (creature, target) => creature.ProcessDirective(executingBehavior.WithTarget((Terrain.Position)target)),
            filter: new TeleFilter(terrainFilter, null),
            canQueue: executingBehavior.canQueue);
    public static CreatureAction WithFeature(FeatureConfig feature,
            TargetedBehavior<Vector2Int> executingBehavior) =>
        new CreatureAction(null,
            pendingDirective: (creature, target) => creature.ProcessDirective(executingBehavior.WithTarget(((Terrain.Position)target).Coord)),
            filter: new TeleFilter(TeleFilter.Terrain.TILES, null),
            feature: feature, canQueue: executingBehavior.canQueue);
    public static CreatureAction Roam =
        new CreatureAction(null, instantDirective: (creature) => creature.CommandRoam(), isRoam: true);
    public static CreatureAction Station =
        new CreatureAction(null,
            pendingDirective: (creature, location) => creature.Station(((Terrain.Position)location).Coord),
            filter: new TeleFilter(TeleFilter.Terrain.TILES, null),
            isStation: true);

    public bool IsInstant {
        get => instantDirective != null;
    }
    public bool UsesFeature {
        get => feature != null;
    }
}
