using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class FarmerConfig {
    public Sprite farmAction;
    public Sprite clearAction;
    public int averageSeedsHarvested = 5;
}

public class Farmer : Species<FarmerConfig> {
    public override Brain Brain(BrainConfig generalConfig) {
        return new FarmerBrain(this, generalConfig, speciesConfig);
    }
}

public class FarmerBrain : Brain {
    private FarmerConfig farmer;
    private int sproutsRemainingInLark = 0;

    public FarmerBrain(Species species, BrainConfig general, FarmerConfig farmer) : base(species, general) {
        this.farmer = farmer;

        Actions = new List<CreatureAction>() {
            CreatureAction.WithTerrain(farmer.farmAction, 
                pathfinding.ApproachThenInteract(FarmTile, rewardExp: false).PendingPosition(CanFarm).Queued(), TeleFilter.Terrain.TILES),
            CreatureAction.Instant(farmer.clearAction, (creature) => resource.Reset(), keepFollowing: true)
        };

        Habitat = new Habitat(this, Radius.Beside) {
            IsShelter = CanHarvest,

            RestBehavior = (shelter) =>
                Habitat.RestBehaviorConsume(shelter, () => creature.stats.ExeTime, () => Harvest(shelter))
        };

        Lark = new Lark(this, CanLark, (v) => (bool)FeatureLibrary.C.sprout.IsValidTerrain(v), Radius.Beside, DoLark);
    }

    override protected void Initialize() {
        // Spawn with seeds in inventory
        CollectSeedQuantity();
        resource.type = FeatureToResource(Sprout.RandomPlant());
    }

    private WhyNot CanFarm(Terrain.Position pos) {
        if (pos.grid != Terrain.Grid.Roof) throw new InvalidOperationException("Cannot call CanFarm with wall");
        FeatureConfig feature = Terrain.I.Feature[pos.Coord]?.config;
        if (Sprout.IsPlant(feature)) return CanHarvest(feature) ? (WhyNot)true : "different_seed";
        else return SufficientResource() && FeatureLibrary.C.sprout.IsValidTerrain(pos.Coord);
    }

    // Farms a plant for seeds if possible, OR plants sprout if possible.
    private void FarmTile(Terrain.Position pos) {
        if (CanHarvest(pos.Coord))
            Harvest(pos.Coord);
        else if (SufficientResource() && FeatureLibrary.C.sprout.IsValidTerrain(pos.Coord))
            Sow(pos.Coord);
    }

    private void Sow(Vector2Int loc) {
        resource.Use();
        Feature sprout = Terrain.I.BuildFeature(loc, FeatureLibrary.C.sprout);
        sprout.hooks.GetComponentStrict<Sprout>().adultFeature = ResourceToFeature(resource);
        creature.GenericExeSucceeded();
    }

    private bool CanHarvest (Vector2Int loc) {
        FeatureConfig feature = Terrain.I.Feature[loc]?.config;
        return feature == null ? false : CanHarvest(feature);
    }
    private bool CanHarvest(FeatureConfig feature) =>
        Sprout.IsPlant(feature) && (resource.IsOut || FeatureToResource(feature.type) == resource.type);
    private static string FeatureToResource(string feature) => Capitalize(feature) + " Seeds";
    private static string Capitalize(string word) => word.Substring(0, 1).ToUpper() + word.Substring(1);
    private static string ResourceToFeature(Resource resource) => resource.type.Substring(0, resource.type.Length - 6).ToLower();

    // Precondition: loc must point to a Feature
    private void Harvest(Vector2Int loc) {
        FeatureConfig feature = Terrain.I.Feature[loc]?.config;
        CollectSeedQuantity();
        resource.type = FeatureToResource(feature.type);
        Terrain.I.DestroyFeature(loc);
    }

    // increase resource by number between 2 and averageSeedsHarvested - 2
    // with middle values more likely
    private void CollectSeedQuantity() => resource.Increase(Random.Range(1, farmer.averageSeedsHarvested) + Random.Range(1, farmer.averageSeedsHarvested));

    // Sometimes, a Lark can involve sowing all remaning seeds;
    // sometimes, sowing just one seed;
    // sometimes, a portion of remaining seeds.
    // This resets when the previous lark is out of view.
    private bool CanLark() {
        if (resource.IsOut) {
            sproutsRemainingInLark = 0;
            return false;
        }

        if (sproutsRemainingInLark > 0) return true;
        else if (Radius.Nearby.Center(this).Where((v) => Terrain.I.Feature[v]?.config?.type == ResourceToFeature(resource)).Any()) return false;
        else {
            int randomSproutCase = Random.Range(0, 3);
            if (randomSproutCase == 0)
                sproutsRemainingInLark = resource.Level;
            else if (randomSproutCase == 1)
                sproutsRemainingInLark = 1;
            else
                sproutsRemainingInLark = Random.Range(1, resource.Level + 1);
            return true;
        }
    }

    private void DoLark(Terrain.Position pos) {
        Sow(pos.Coord);
        sproutsRemainingInLark--;
    }
}
