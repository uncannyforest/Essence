using UnityEngine;

public class GameManager : MonoBehaviour {
    private static GameManager instance;
    public static GameManager I { get => instance; }
    GameManager(): base() {
        instance = this;
    }

    private PlayerCharacter singlePlayer;

    void Start() {
        singlePlayer = GameObject.FindObjectOfType<PlayerCharacter>();
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
}
