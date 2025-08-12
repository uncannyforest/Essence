using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Team))]
public class Anthopoid : MonoBehaviour {
    private Health health;
    [NonSerialized] public CharacterController movement;

    void Start() {        
        health = GetComponent<Health>();
        health.ReachedZero += HandleDeath;
    }

    public void HandleDeath() {
        movement = GetComponent<CharacterController>();
        movement.CrossingTile += HandleCrossingTile;
        health.Reset();
        MoveViaFountain(null);
    }

    public bool HandleCrossingTile(Vector2Int newTile) {
        if (Terrain.I.Feature[newTile] is Feature feature
                && feature.hooks != null
                && feature.hooks.PlayerEntered != null) {
            return feature.hooks.PlayerEntered(this);
        }
        return true;
    }

    public void MoveViaFountain(Fountain prev) {
        Fountain[] allSpawnPoints = GameObject.FindObjectsOfType<Fountain>();
        Fountain[] teamSpawnPoints = 
            (from point in allSpawnPoints
            where point.Team == GetComponent<Team>().TeamId
            select point).ToArray();
        int index = 0;
        if (prev == null) {
            index = Random.Range(0, teamSpawnPoints.Length);
        } else {
            for ( ; index < teamSpawnPoints.Length; index++) {
                if (teamSpawnPoints[index] == prev) break;
            }
            index--;
            if (index < 0) index = teamSpawnPoints.Length - 1;
            teamSpawnPoints[index].Teleporting();
        }
        transform.position = (Vector2)teamSpawnPoints[index].transform.position;
        Terrain.I.mapRenderer.Reset();
    }
}
