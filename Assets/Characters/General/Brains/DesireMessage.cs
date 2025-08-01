using System;
using UnityEngine;

public struct DesireMessage {
    public struct Obstacle {
        public Creature requestor;
        public Terrain.Position location;
        public Land? landObstacle;
        public Construction? wallObstacle;
        public Feature? featureObstacle;

        public bool IsStillPresent {
            get {
                if (landObstacle is Land land) {
                    return land == Terrain.I.Land[location.Coord];
                } else if (wallObstacle is Construction wall) {
                    return wall == Terrain.I[location];
                } else if (featureObstacle is Feature feature) {
                    return feature.Equals(Terrain.I.Feature[location.Coord]);
                } else throw new InvalidOperationException("Desire message must include obstacle");
            }
        }
    }

    public Optional<Transform> assailant;
    public Obstacle? obstacle;

    override public string ToString() {
        return "DesireMessage assailant: " + assailant;
    }
}
