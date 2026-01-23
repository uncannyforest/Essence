using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    private static GameManager instance;
    public static GameManager I { get => instance; }
    GameManager(): base() {
        instance = this;
    }

    public Transform worldBag;
    public PlayerCharacter playerPrefab;
    public Bugge buggePrefab;
    public int numberOfBugges = 1;

    private PlayerCharacter singlePlayer;
    private List<Bugge> bugges = new List<Bugge>();

    void Awake() {
        singlePlayer = GameObject.Instantiate(playerPrefab, worldBag);
        Debug.Log("SINGLE PLAYER = " + singlePlayer);
        for (int i = 0; i < numberOfBugges; i++)
            bugges.Add(GameObject.Instantiate(buggePrefab, worldBag));
    }

    public void FountainLoaded(Fountain fountain) {
        if (fountain.Team == 1) {
            GameManager.I.YourPlayer.GetComponentStrict<Anthopoid>().Respawn();
            Origin = (Vector2Int)fountain.GetComponentStrict<FeatureHooks>().tile;
        }
        if (fountain.Team == 0) foreach (Bugge bugge in bugges) {
            bugge.GetComponentStrict<Anthopoid>().Respawn();
        }
    }

    // This is a placeholder for any code
    // that ought to handle multiple players in multiplayer.
    // TODO: Refactor to return a list.
    public PlayerCharacter AnyPlayer {
        get => singlePlayer;
    }

    // the player being controlled on this computer
    public PlayerCharacter YourPlayer {
        get => singlePlayer;
    }

    public Team YourTeam {
        get => YourPlayer.GetComponentStrict<Team>();
    }

    // used for stats
    public Vector2Int Origin;
}
