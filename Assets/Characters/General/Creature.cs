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
    public readonly Action<Creature, Target> pendingDirective;
    public readonly TeleFilter dynamicFilter;
    public readonly bool canQueue;
    public readonly bool isRoam;
    public readonly bool isStation;
    public readonly Feature feature;
    private CreatureAction(Sprite icon, Action<Creature> instantDirective, 
            Action<Creature, Target> pendingDirective,
            TeleFilter filter, Feature feature, bool canQueue, bool isRoam, bool isStation) {
        this.icon = icon;
        this.instantDirective = instantDirective;
        this.pendingDirective = pendingDirective;
        this.dynamicFilter = filter;
        this.canQueue = canQueue;
        this.isRoam = isRoam;
        this.isStation = isStation;
        this.feature = feature;
    }

    public static CreatureAction Instant(Sprite icon, Action<Creature> instantDirective) =>
        new CreatureAction(icon, instantDirective, null, null, null, false, false, false);
    public static CreatureAction WithObject(Sprite icon,
            Func<IEnumerator> executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.Execute(executingBehavior, target),
            filter, null, false, false, false);
    public static CreatureAction QueueableWithObject(Sprite icon,
            Func<IEnumerator> executingBehavior,
            TeleFilter filter) =>
        new CreatureAction(icon, null,
            (creature, target) => creature.ExecuteEnqueue(executingBehavior, target),
            filter, null, true, false, false);
    public static CreatureAction QueueableFeature(Feature feature,
            Func<IEnumerator> executingBehavior) =>
        new CreatureAction(null, null,
            (creature, target) => creature.ExecuteEnqueue(executingBehavior, target),
            new TeleFilter(TeleFilter.Terrain.TILES, null), feature, true, false, false);
    public static CreatureAction Roam =
        new CreatureAction(null, (creature) => creature.CommandRoam(), null, null, null, false, true, false);
    public static CreatureAction Station =
        new CreatureAction(null, null,
            (creature, location) => creature.Station(((Terrain.Position)location).Coord),
            new TeleFilter(TeleFilter.Terrain.TILES, null),
            null, false, false, true);

    public bool IsInstant {
        get => instantDirective != null;
    }
    public bool UsesFeature {
        get => feature != null;
    }
}

[RequireComponent(typeof(Team))]
[RequireComponent(typeof(CharacterController))]
public class Creature : MonoBehaviour {
    public BrainConfig brainConfig;
    public Species species;
    public string creatureName;
    public Sprite icon;
    public Sprite breastplate;
    public string tamingInfoShort = "You cannot tame any";
    [TextArea(2, 12)] public string tamingInfoLong = "That creature cannot be tamed.";

    public Brain brain;
    public CreatureState State {
        get => brain.state;
    }
    public List<CreatureAction> action = new List<CreatureAction>();

    private const float despawnTime = 128f;
    public const float neighborhood = 6.5f;

    [NonSerialized] public CharacterController controller;
    private SpriteSorter spriteManager;
    public SpriteSorter SpriteManager { get => spriteManager; }

    [NonSerialized] public Creature.Data? serializedData;
    void Start() {
        controller = GetComponent<CharacterController>();
        brain = species.Brain(brainConfig).InitializeAll();
        InitializeActionList(brain);
        spriteManager = GetComponentInChildren<SpriteSorter>();
        GetComponent<Team>().changed += TeamChangedEventHandler;

        if (serializedData is Data data) DeserializeNow(data);
        else cMaybeDespawn = StartCoroutine(MaybeDespawn());
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

    public CharacterController OverrideControl(MonoBehaviour source) {
        new Senses() { controlOverride = Delta<MonoBehaviour>.Add(source) }
            .TryUpdateCreature(this, 2);
        return brain.movement;
    }
    public void ReleaseControl() => new Senses() {
        controlOverride = Delta<MonoBehaviour>.Remove()
    }.TryUpdateCreature(this, 2);
    
    public void Follow(Transform player) => new Senses() {
        command = Command.Follow(player)
    }.ForCreature(this).TryUpdateState(brain, 2);
    
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
        if (cMaybeDespawn != null) StopCoroutine(cMaybeDespawn);
        GetComponent<Team>().TeamId = player.GetComponentStrict<Team>().TeamId;
        Follow(player);
    }
    public void ForceTame(int team) {
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

    public bool CanPair() => State.type.CanTransitionTo(CreatureStateType.Pair);
    
    public bool TryPair(Transform initiator) => new Senses() {
        message = CreatureMessage.PairToSubject(initiator)
    }.TryUpdateCreature(this);
    
    // call this method on recipient
    public bool EndPairCommand(Transform initiator) => new Senses() {
        message = CreatureMessage.EndPairToSubject(initiator)
    }.TryUpdateCreature(this);

    // call this method on initiator
    public void EndPairRequest() => new Senses() {
        message = CreatureMessage.EndPairToMaster()
    }.TryUpdateCreature(this, 1);

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
        new Senses() {
            hint = (target != null)
                ? new Hint() { target = Optional.Of(target) }
                : new Hint() { generallyOffensive = true }
        }.TryUpdateCreature(this);
    }

    // Defensive
    public void WitnessAttack(Transform assailant) => new Senses() {
        desireMessage = new DesireMessage() {
            target = new Target(assailant.GetComponentInChildren<SpriteSorter>())
        }
    }.TryUpdateCreature(this);

    public void CommandRoam() => new Senses() {
        command = Command.Roam()
    }.TryUpdateCreature(this);

    public void Station(Vector2Int location) => new Senses() {
        command = Command.Station(Terrain.I.CellCenter(location))
    }.TryUpdateCreature(this);

    public void Execute(Func<IEnumerator> executingBehavior, Target target) => new Senses() {
        command = Command.Execute(new LegacyBehaviorNode(executingBehavior, target))
    }.TryUpdateCreature(this);

    public void ExecuteEnqueue(Func<IEnumerator> executingBehavior, Target target) => new Senses() {
        command = Command.Execute(QueueOperator.Of(new LegacyBehaviorNode(executingBehavior, target)))
    }.TryUpdateCreature(this);

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

    void OnTriggerEnter2D(Collider2D collider) {
        Boat boat = collider.transform.parent.GetComponent<Boat>();
        if (boat != null && boat.player == brain.followDirective?.GetComponent<PlayerCharacter>())
            boat.RequestCreatureEnter(this);
    }

    [Serializable] public struct Data {
        public int x;
        public int y;
        public string species;
        public int team;
        public bool stationed;
        public string name;

        public Vector2Int tile { get => Vct.I(x, y); }

        public Data(Vector2Int tile, string species, int team, bool stationed, string name) {
            this.x = tile.x;
            this.y = tile.y;
            this.species = species;
            this.team = team;
            this.stationed = stationed;
            this.name = name;
        }
    }
    public Data Serialize() {
        return new Data(Terrain.I.CellAt(transform.position),
            creatureName,
            GetComponent<Team>().TeamId,
            brain.state.command?.type == CommandType.Station,
            gameObject.name);
    }
    public void DeserializeUponStart(Data data) {
        serializedData = data;
    }
    private void DeserializeNow(Data data) {
        gameObject.name = data.name;
        GetComponent<Team>().TeamId = data.team;
        if (data.stationed) Station(data.tile);
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