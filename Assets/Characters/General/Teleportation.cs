using System;
using System.Collections.Generic;
using UnityEngine;

public class Teleportation {

    // any GameObject with a Team component is eligible, so the Team component the input to enforce that constraint
    public static void MoveViaFountain(Team teamComponent, Fountain prev) {
        Fountain[] teamSpawnPoints = Fountain.FindAllByTeam(teamComponent.TeamId);
        int index = 0;
        if (prev == null) {
            index = UnityEngine.Random.Range(0, teamSpawnPoints.Length);
        } else {
            for ( ; index < teamSpawnPoints.Length; index++) {
                if (teamSpawnPoints[index] == prev) break;
            }
            index--;
            if (index < 0) index = teamSpawnPoints.Length - 1;
            teamSpawnPoints[index].Teleporting();
        }
        teamComponent.transform.position = (Vector2)teamSpawnPoints[index].transform.position;
        Terrain.I.mapRenderer.Reset();
    }

    public static void RespawnToFountain(CharacterController character, GameObject respawnDelayHiddenObject, float respawnDelay) {
        if (respawnDelayHiddenObject != null) respawnDelayHiddenObject.SetActive(false);
        character.Invoke(() => Respawn(character, respawnDelayHiddenObject), respawnDelay);
    }

    public static void Respawn(CharacterController character, GameObject respawnDelayHiddenObject) {
        if (respawnDelayHiddenObject != null) respawnDelayHiddenObject.SetActive(true);
        character.GetComponentStrict<Health>().Reset();
        MoveViaFountain(character.GetComponentStrict<Team>(), null);
    }
}
