using System;
using UnityEngine;

[RequireComponent(typeof(FeatureHooks))]
public class Fountain : MonoBehaviour {
    public float timeToCapture = 5f;
    public float timeToTeleport = 2f;
    public float timeToReset = 1f;
    public float ringMaxSize = Mathf.Sqrt(10);
    public Transform ring;

    private FeatureHooks feature;

    private int team = 0;
    private float ringSize = 0;
    new private Collider2D collider;
    private int enemyPresent = 0;
    private Transform enemy;
    private int friendPresent = 0;
    private PlayerCharacter friend;
    private bool lastOutward;

    public int Team {
        get => team;
        set {
            team = value;
            if (value == 0) GetComponentInChildren<SpriteRenderer>().color = Color.gray;
            else GetComponentInChildren<SpriteRenderer>().color = new Color(.99f, .99f, .99f);
        }
    }

    void Start() {
        feature = GetComponent<FeatureHooks>();
        if (feature.serializedFields != null) Deserialize(feature.serializedFields);
        feature.SerializeFields += Serialize;
        feature.PlayerEntered += HandlePlayerEntered;
        collider = GetComponent<Collider2D>();
        // If this were in PlayerController Fountains might not be loaded yet.
        if (team != 0) GameManager.I.YourPlayer.HandleDeath();
    }

    int[] Serialize() => new int[] { team };
    void Deserialize(int[] fields) => Team = fields[0];

    bool HandlePlayerEntered(PlayerCharacter target) {
        int playerTeam = target.GetComponentStrict<Team>().TeamId;
        if (playerTeam == team) {
            friendPresent = 2;
            friend = target;
            lastOutward = true;
        } else {
            enemyPresent = 2; // Rather than using boolean, we need an extra frame for FixedUpdate to run
            enemy = target.transform;
            lastOutward = false;
        }
        return true;
    }

    void FixedUpdate() {
        if (enemyPresent > 0) {
            if (feature.tile == Terrain.I.CellAt(enemy.position)) {
                RingProgress(HandleDeath);
            } else enemyPresent--;
        } else if (friendPresent > 0) {
            if (feature.tile == Terrain.I.CellAt(friend.transform.position)) {
                RingProgress(Teleport);
            } else friendPresent--;
        } else RingRegress();
    }

    private void RingProgress(Action done) {
        if (!ring.gameObject.activeSelf) {
            ring.gameObject.SetActive(true);
            ringSize = lastOutward ? 0 : ringMaxSize;
        } else {
            ringSize += ringMaxSize * Time.deltaTime / (lastOutward ? timeToTeleport : -timeToCapture);
        }
        ring.localScale = Vector3.one * ringSize * ringSize;
        if (lastOutward ? (ringSize >= ringMaxSize) : (ringSize <= 0)) {
            done();
            ring.gameObject.SetActive(false);
        }
    }
        
    private void RingRegress() {
        if (ring.gameObject.activeSelf) {
            ringSize += ringMaxSize * Time.deltaTime / timeToReset * (lastOutward ? -1 : 1);
            ring.localScale = Vector3.one * ringSize * ringSize;
            if (lastOutward ? (ringSize <= 0) : (ringSize >= ringMaxSize)) {
                ring.gameObject.SetActive(false);
            }
        }
    }

    private void HandleDeath() {
        Team = enemy.GetComponentStrict<Team>().TeamId;
        enemyPresent = 0;
    }

    private void Teleport() {
        friend.MoveViaFountain(this);
        friendPresent = 0;
    }

    public void Teleporting() {
        ring.gameObject.SetActive(true);
        ringSize = ringMaxSize;
        lastOutward = true;
    }
}
