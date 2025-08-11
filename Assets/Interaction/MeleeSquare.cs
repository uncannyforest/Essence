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

    private Vector2Int keyInputVelocity = Vector2Int.zero; // not scaled to speed, instant update on key change
    private float timeDoneMoving = 0;
    private Vector2Int currentTile = Vector2Int.zero;
    private Vector2Int? waitingOnPlayerToLeave = null; // if null and timeDoneMoving == 0, we're waiting on input
    private bool updateSetByMouseMove = false;

    private Transform player;
    public MeleeSquare(Config meleeSquareConfig, Transform player) {
        this.config = meleeSquareConfig;
        this.player = player;
        currentTile = Terrain.I.CellAt(player.position);
    }

    // input x and y are from {-1, 0, 1}: 9 possibilities
    public Vector2 KeyInputVelocity {
        get => (Vector2)keyInputVelocity;
        set {
            Vector2Int oldInputVelocity = keyInputVelocity;
            keyInputVelocity = new Vector2Int(
                value.x > 0 ? 1 : value.x < 0 ? -1 : 0,
                value.y > 0 ? 1 : value.y < 0 ? -1 : 0
            );
            if (oldInputVelocity != keyInputVelocity) {
                ProcessNewInput();
            }
        }
    }

    private void ProcessNewInput() {
        if (keyInputVelocity == Vector2Int.zero) {
            timeDoneMoving = 0;
        } else {
            timeDoneMoving = config.delay + Time.fixedTime;
        }
        waitingOnPlayerToLeave = null;
    }

    public Vector2Int? TryMoveFromKeyInput() {
        Vector2Int nextSquare = currentTile + keyInputVelocity;
        Vector2Int relativeToPlayer = nextSquare - Terrain.I.CellAt(player.position);

        if (Math.Abs(relativeToPlayer.x) + Math.Abs(relativeToPlayer.y) > 1) {
            if (BounceWithinBounds() is Vector2Int bounce) {
                timeDoneMoving = config.latency + Time.fixedTime;
                currentTile = bounce;
                return bounce;
            }
            waitingOnPlayerToLeave = Terrain.I.CellAt(player.position);
            timeDoneMoving = 0f;
            return null;
        }

        if (waitingOnPlayerToLeave == null) {
            timeDoneMoving = config.latency + Time.fixedTime;
        } else {
            waitingOnPlayerToLeave = Terrain.I.CellAt(player.position);
        }

        currentTile = nextSquare;
        return nextSquare;
    }

    public Vector2Int? BounceWithinBounds() {
        Vector2Int relativeToPlayer = currentTile - Terrain.I.CellAt(player.position);
        if (Math.Abs(relativeToPlayer.x) + Math.Abs(relativeToPlayer.y) > 1) {
            Vector2Int bounce = Terrain.I.CellAt(player.position) + keyInputVelocity;
            return bounce;
        } else {
            return null;
        }
    }

    public Vector2Int PointerToSquareRelative(Vector2 pointer) {
        float? inputAngle = pointer.VelocityToDirection();
        if (inputAngle is float realAngle) {
            int pointerDirection = Mathf.RoundToInt(realAngle / 90);
            switch (pointerDirection) {
                case 1: return Vector2Int.up;
                case 0: return Vector2Int.right;
                case -1: return Vector2Int.down;
                default: return Vector2Int.left;
            }
        } else return Vector2Int.zero;
    }

    public void PointerToSquare(Vector2 pointer) {
        Vector2Int relative;
        if (Terrain.I.CellAt(player.position) == Terrain.I.CellAt(pointer))
            relative = Vector2Int.zero;
        else relative = PointerToSquareRelative(pointer - (Vector2)player.position);
        Vector2Int nextSquare = Terrain.I.CellAt(player.position) + relative;
        if (nextSquare == currentTile) return;
        updateSetByMouseMove = true;
        currentTile = nextSquare;
    }

    public Vector2Int? GetResultForFixedUpdate() {
        if (updateSetByMouseMove) {
            updateSetByMouseMove = false;
            return currentTile;
        } else if (timeDoneMoving == 0f) {
            if (waitingOnPlayerToLeave is Vector2Int oldSquare) {
                Vector2Int newSquare = Terrain.I.CellAt(player.position);
                if (oldSquare != newSquare) {
                    return TryMoveFromKeyInput();
                } else {
                    return null; // waiting on player to leave square
                }
            } else {
                return null; // no input
            }
        } else {
            if (Time.fixedTime > timeDoneMoving) {
                return TryMoveFromKeyInput();
            } else {
                return null; // waiting for delay or latency
            }
        }
    }

    public override string ToString() {
        return "MeleeSquare config(" + config.latency + ", " + config.delay
            + ") inputVelocity(" + keyInputVelocity + ") currentTile(" + currentTile + ")"; 
    }

}
