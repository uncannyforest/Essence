using System;
using UnityEngine;

public class MeleeSquare {
    [Serializable] public class Config {
        [SerializeField] public float latency;
        [SerializeField] public float delay;
        public Config(float latency, float delay) {
            this.latency = latency;
            this.delay = delay;
        }
    }
    private Config config;

    private Vector2Int inputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    private float timeDoneMoving = 0;
    private Vector2Int currentTile = Vector2Int.zero;
    private Vector3Int? waitingOnPlayerToLeave = null; // if null and timeDoneMoving == 0, we're waiting on input

    private Transform player;
    private Grid grid;
    public MeleeSquare(Config meleeSquareConfig, Transform player, Grid grid) {
        this.config = meleeSquareConfig;
        this.player = player;
        this.grid = grid;
        currentTile = (Vector2Int)grid.WorldToCell(player.position);
    }

    // input x and y are from {-1, 0, 1}: 9 possibilities
    public Vector2 InputVelocity {
        get => (Vector2)inputVelocity;
        set {
            Vector2Int oldInputVelocity = inputVelocity;
            inputVelocity = new Vector2Int(
                value.y + value.x > 0 ? 1 : value.y + value.x < 0 ? -1 : 0,
                value.y - value.x > 0 ? 1 : value.y - value.x < 0 ? -1 : 0
            );
            if (oldInputVelocity != inputVelocity) {
                ProcessNewInput();
            }
        }
    }

    private void ProcessNewInput() {
        if (inputVelocity == Vector2Int.zero) {
            timeDoneMoving = 0;
        } else {
            timeDoneMoving = config.delay + Time.fixedTime;
        }
        waitingOnPlayerToLeave = null;
    }

    public Vector2Int? TryMove() {
        Vector2Int nextSquare = currentTile + inputVelocity;
        Vector2Int relativeToPlayer = nextSquare - (Vector2Int)grid.WorldToCell(player.position);

        if (Math.Abs(relativeToPlayer.x) + Math.Abs(relativeToPlayer.y) > 1) {
            if (BounceWithinBounds() is Vector2Int bounce) {
                timeDoneMoving = config.latency + Time.fixedTime;
                currentTile = bounce;
                return bounce;
            }
            waitingOnPlayerToLeave = grid.WorldToCell(player.position);
            timeDoneMoving = 0f;
            return null;
        }

        if (waitingOnPlayerToLeave == null) {
            timeDoneMoving = config.latency + Time.fixedTime;
        } else {
            waitingOnPlayerToLeave = grid.WorldToCell(player.position);
        }

        currentTile = nextSquare;
        return nextSquare;
    }

    public Vector2Int? BounceWithinBounds() {
        Vector2Int relativeToPlayer = currentTile - (Vector2Int)grid.WorldToCell(player.position);
        if (Math.Abs(relativeToPlayer.x) + Math.Abs(relativeToPlayer.y) > 1) {
            Vector2Int bounce = (Vector2Int)grid.WorldToCell(player.position) + inputVelocity;
            return bounce;
        } else {
            return null;
        }
    }

    public Vector2Int? GetResultForFixedUpdate() {
        if (timeDoneMoving == 0f) {
            if (waitingOnPlayerToLeave is Vector3Int oldSquare) {
                Vector3Int newSquare = grid.WorldToCell(player.position);
                if (oldSquare != newSquare) {
                    return TryMove();
                } else {
                    return null; // waiting on player to leave square
                }
            } else {
                return null; // no input
            }
        } else {
            if (Time.fixedTime > timeDoneMoving) {
                return TryMove();
            } else {
                return null; // waiting for delay or latency
            }
        }
    }

    public override string ToString() {
        return "MeleeSquare config(" + config.latency + ", " + config.delay
            + ") inputVelocity(" + inputVelocity + ") currentTile(" + currentTile + ")"; 
    }

}
