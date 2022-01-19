using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public struct CreatureAction {
    public readonly Sprite icon;
    public readonly Action<Creature> instantDirective;
    public readonly Action<Creature, OneOf<Terrain.Position, SpriteSorter>> pendingDirective;
    public readonly TeleFilter dynamicFilter;
    public readonly bool isRoam;
    public readonly bool isStation;
    private CreatureAction(Sprite icon, Action<Creature> instantDirective, 
            Action<Creature, OneOf<Terrain.Position, SpriteSorter>> pendingDirective,
            TeleFilter filter, bool isRoam, bool isStation) {
        this.icon = icon;
        this.instantDirective = instantDirective;
        this.pendingDirective = pendingDirective;
        this.dynamicFilter = filter;
        this.isRoam = isRoam;
        this.isStation = isStation;
    }

    public static CreatureAction Instant(Sprite icon, Action<Creature> instantDirective) =>
        new CreatureAction(icon, instantDirective, null,  null, false, false);
    public static CreatureAction WithObject(Sprite icon,
            CoroutineWrapper executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.Execute(executingBehavior, target),
            filter, false, false);
    public static CreatureAction Roam =
        new CreatureAction(null, (c) => c.State = CreatureState.Roam, null, null, true, false);
    public static CreatureAction Station =
        new CreatureAction(null, null,
            (creature, location) => creature.Station(((Terrain.Position)location).Coord),
            new TeleFilter(TeleFilter.Terrain.TILES, null),
            false, true);

    public bool IsInstant {
        get => instantDirective != null;
    }
}

[RequireComponent(typeof(Team))]
public class Creature : MonoBehaviour {
    public BrainConfig brainConfig;
    public Species species;
    public string creatureName;
    public Sprite icon;
    public Sprite breastplate;
    public string tamingInfo = "This creature cannot be tamed.";
    public float personalBubble = .25f;

    public const int subGridUnit = 8;

    public CreatureState stateForEditorDebugging;

    public Brain brain;
    public CreatureState State {
        get => brain.State;
        set => brain.State = value;
    }
    public List<CreatureAction> action = new List<CreatureAction>();

    private const float despawnTime = 128f;
    public const float neighborhood = 6.5f;

    new private Rigidbody2D rigidbody;
    private SpriteSorter spriteManager;
    public SpriteSorter SpriteManager { get => spriteManager; }
    private Animator animator; // may be null

    void Start() {
        brain = species.Brain(brainConfig).InitializeAll();
        InitializeActionList(brain);
        cMaybeDespawn = StartCoroutine(MaybeDespawn());
        rigidbody = GetComponentInChildren<Rigidbody2D>();
        spriteManager = GetComponentInChildren<SpriteSorter>();
        GetComponent<Team>().changed += TeamChangedEventHandler;
        animator = GetComponentInChildren<Animator>(); // may be null
    }

    private void InitializeActionList(Brain brain) {
        action.Add(CreatureAction.Roam);
        action.Add(CreatureAction.Station);
        action.AddRange(brain.Actions());
    }

    private void TeamChangedEventHandler(int team) {
        species.transform.Find("SpriteSort/Torso/Heart").GetComponentStrict<SpriteRenderer>().color =
            GetComponent<Team>().Color;
    }

    public void Follow(Transform player) {
        brain.CommandFollow(player);
    }
    // Can call without calling CanTame() first; result will indicate whether it succeeded
    // If false, get TamingInfo for error
    public bool TryTame(Transform player) {
        if (brain.ExtractTamingCost(player)) {
            StopCoroutine(cMaybeDespawn);
            GetComponent<Team>().TeamId = player.GetComponentStrict<Team>().TeamId;
            Follow(player);
            return true;
        } else return false;
    }
    public bool CanTame(Transform player) {
        return brain.CanTame(player);
    }
    public string TamingInfo {
        get => tamingInfo;
    }

    public bool CanSee(Transform seen) => brain.CanSee(seen);

    public static Transform FindOffensiveTarget(int team, Vector2 playerPosition, Vector2 playerDirection,
            float castRadius, float castStart, float castDistance) {
        Debug.DrawLine(playerPosition + playerDirection.normalized * castStart,
            playerPosition + playerDirection.normalized * (castStart + castDistance), Color.white);
        RaycastHit2D[] possibleResults = Physics2D.CircleCastAll(
            playerPosition + playerDirection.normalized * castStart,
            castRadius, playerDirection, castDistance, LayerMask.GetMask("Player", "HealthCreature"));
        return (from result in possibleResults
            where result.transform.GetComponentStrict<Team>().TeamId != team
            select result.transform).FirstOrDefault();
    }

    public void FollowOffensive(Transform target) {
        if (!brain.general.hasAttack) return;
        Debug.Log(gameObject + " received message to attack " + target);
        // Generally offensive, when no target specified
        if (target == null) brain.EnableFollowOffensiveNoTarget();
        // Specific offense, temporary once target is gone.
        else brain.EnableFollowOffensiveWithTarget(target);
    }

    // Defensive
    public void WitnessAttack(Transform assailant) => brain.TryIndicateAttack(assailant, false);

    public void Station(Vector2Int location) => brain.CommandStation(location);

    public void Execute(CoroutineWrapper executingBehavior,
            OneOf<Terrain.Position, SpriteSorter> target) {
        brain.CommandExecute(executingBehavior, target);
    }

    private Coroutine cMaybeDespawn;
    private IEnumerator MaybeDespawn() {
        while (true) {
            yield return new WaitForSeconds(despawnTime);
            Collider2D[] playersNearby =
                Physics2D.OverlapCircleAll(transform.position, PlayerCharacter.neighborhood, LayerMask.GetMask("Player"));
            if (playersNearby.Length == 0) {
                Debug.Log("Despawning " + gameObject + " at " + transform.position);
                Destroy(gameObject);
                yield break;
            } else {
                Debug.Log("Too close to player to despawn " + gameObject + " at " + transform.position);
            }
        }
    }

    private Vector2 velocity = Vector2.zero;
    public Vector2 InputVelocity {
        get => velocity;
        set {
            velocity = value;
            animator?.SetFloat("Velocity X", value.x);
            animator?.SetFloat("Velocity Y", value.y);
        }
    }
    private float timeDoneMoving = 0;
    public void FixedUpdate() {
        if (Time.fixedTime > timeDoneMoving)
            if (InputVelocity != Vector2Int.zero)
                if (Move() is Vector2 move)
                    rigidbody.MovePosition(move);
    }

    public void Update() {
        brain.Update();
    }
    private Vector2? Move() {
        float timeToMove = 1f / (InputVelocity.ChebyshevMagnitude() * subGridUnit);
        Vector2 newLocation = rigidbody.position + InputVelocity * timeToMove;
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(newLocation, personalBubble, LayerMask.GetMask("Player", "Creature", "HealthCreature"));
        if (overlaps.Length > 1) return null;
        timeDoneMoving = timeToMove + Time.fixedTime;
        return newLocation;
    }
}

[RequireComponent(typeof(Creature))]
public abstract class Species : MonoBehaviour {
    public Creature creatureliness { get => GetComponent<Creature>(); }

    public abstract Brain Brain(BrainConfig general);

}

[RequireComponent(typeof(Creature))]
public abstract class Species<T> : Species {
    public T speciesConfig;
}