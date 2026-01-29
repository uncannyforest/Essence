using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Team))]
public class Anthopoid : MonoBehaviour {
    public float respawnDelay = 3;
    public GameObject respawnDelayHiddenObject;

    private Health health;
    [NonSerialized] public CharacterController movement;

    void Start() {        
        movement = GetComponent<CharacterController>();
        movement.CrossingTile += HandleCrossingTile;
        health = GetComponent<Health>();
        health.ReachedZero += Respawn;
    }

    public void Respawn() => Teleportation.RespawnToFountain(movement, respawnDelayHiddenObject, respawnDelay);

    public void FirstSpawn() => Teleportation.Respawn(movement, respawnDelayHiddenObject);

    public bool HandleCrossingTile(Vector2Int newTile) {
        if (Terrain.I.Feature[newTile] is Feature feature
                && feature.hooks != null
                && feature.hooks.PlayerEntered != null) {
            return feature.hooks.PlayerEntered(this);
        }
        return true;
    }
}
