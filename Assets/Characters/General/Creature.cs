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
    public readonly bool canQueue;
    public readonly bool isRoam;
    public readonly bool isStation;
    private CreatureAction(Sprite icon, Action<Creature> instantDirective, 
            Action<Creature, OneOf<Terrain.Position, SpriteSorter>> pendingDirective,
            TeleFilter filter, bool canQueue, bool isRoam, bool isStation) {
        this.icon = icon;
        this.instantDirective = instantDirective;
        this.pendingDirective = pendingDirective;
        this.dynamicFilter = filter;
        this.canQueue = canQueue;
        this.isRoam = isRoam;
        this.isStation = isStation;
    }

    public static CreatureAction Instant(Sprite icon, Action<Creature> instantDirective) =>
        new CreatureAction(icon, instantDirective, null, null, false, false, false);
    public static CreatureAction WithObject(Sprite icon,
            CoroutineWrapper executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.Execute(executingBehavior, target),
            filter, false, false, false);
    public static CreatureAction QueueableWithObject(Sprite icon,
            CoroutineWrapper executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.ExecuteEnqueue(executingBehavior, target),
            filter, true, false, false);
    public static CreatureAction Roam =
        new CreatureAction(null, (c) => c.State = CreatureState.Roam, null, null, false, true, false);
    public static CreatureAction Station =
        new CreatureAction(null, null,
            (creature, location) => creature.Station(((Terrain.Position)location).Coord),
            new TeleFilter(TeleFilter.Terrain.TILES, null),
            false, false, true);

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
    public string tamingInfoShort = "You cannot tame any";
    [TextArea(2, 12)] public string tamingInfoLong = "That creature cannot be tamed.";
    public float personalBubble = .25f;

    public CreatureState stateForEditorDebugging;

    public Brain brain;
    public CreatureState State {
        get => brain.State;
        set => brain.State = value;
    }
    public List<CreatureAction> action = new List<CreatureAction>();

    private const float despawnTime = 128f;
    public const float neighborhood = 6.5f;

    public CharacterController controller;
    private SpriteSorter spriteManager;
    public SpriteSorter SpriteManager { get => spriteManager; }

    void Start() {
        controller = new CharacterController(this).WithPersonalBubble(personalBubble, HandleHitCollider);
        brain = species.Brain(brainConfig).InitializeAll();
        InitializeActionList(brain);
        cMaybeDespawn = StartCoroutine(MaybeDespawn());
        spriteManager = GetComponentInChildren<SpriteSorter>();
        GetComponent<Team>().changed += TeamChangedEventHandler;
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

    public CharacterController OverrideControl(MonoBehaviour source) => brain.OverrideControl(source);
    public void ReleaseControl() => brain.ReleaseControl();

    public void Follow(Transform player) {
        brain.CommandFollow(player);
    }
    // Can call without calling CanTame() first; result will indicate whether it succeeded
    // If false, get TamingInfo for error
    public bool TryTame(Transform player) {
        if (brain.ExtractTamingCost(player)) {
            ForceTame(player);
            return true;
        } else return false;
    }
    public bool CanTame(Transform player) {
        return brain.CanTame(player);
    }
    public void ForceTame(Transform player) { // bypasses ExtractTamingCost
        StopCoroutine(cMaybeDespawn);
        GetComponent<Team>().TeamId = player.GetComponentStrict<Team>().TeamId;
        Follow(player);
    }
    public ExpandableInfo TamingInfo {
        get => GenerateTamingInfo(creatureName, tamingInfoShort, tamingInfoLong);
    }
    public static ExpandableInfo GenerateTamingInfo(GameObject creature, string tamingInfoShort, string tamingInfoLong) {
        string creatureName = creature.GetComponentStrict<Creature>().creatureName;
        return GenerateTamingInfo(creatureName, tamingInfoShort, tamingInfoLong);
    }
    private static ExpandableInfo GenerateTamingInfo(string creatureName, string tamingInfoShort, string tamingInfoLong) {
        return new ExpandableInfo(tamingInfoShort + " <color=creature>" + creatureName + "</color>",
            tamingInfoLong.Replace("<creature/>", "<color=creature>" + creatureName + "</color>"));
    }

    public bool CanPair() => brain.TrekkingSolo;
    
    public bool TryPair(Creature initiator) => brain.TryCommandPair(initiator);

    public void EndPairCommand() => brain.EndPairCommand(); // call this method on recipient

    public void EndPairRequest() => brain.EndPairRequest(); // call this method on initiator

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
            OneOf<Terrain.Position, SpriteSorter> target) => brain.CommandExecute(executingBehavior, target);

    public void ExecuteEnqueue(CoroutineWrapper executingBehavior,
            OneOf<Terrain.Position, SpriteSorter> target) => brain.EnqueueExecuteCommand(executingBehavior, target);

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

    void Update() {
        brain.Update();
    }

    public void HandleHitCollider(Collider2D collider) {
        Boat boat = collider.GetComponent<Boat>();
        if (boat != null && boat.player == brain.FollowDirective?.GetComponent<PlayerCharacter>())
            boat.RequestCreatureEnter(this);
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