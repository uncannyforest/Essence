using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiveMind : MonoBehaviour {
    private static HiveMind instance;
    public static HiveMind I { get => instance; }
    void Awake() {
        instance = this;
    }

    public void RegisterObstacle(Creature creature, DesireMessage.Obstacle obstacle) { }
}
