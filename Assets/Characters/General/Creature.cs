using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[RequireComponent(typeof(Team))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Stats))]
public class Creature : MonoBehaviour {
    public BrainConfig brainConfig;
    public Species species;
    public string creatureName;
    public string creatureShortName;
    public Sprite icon;
    public Sprite breastplate;
    public string tamingInfoShort = "You cannot tame any";
    [TextArea(2, 12)] public string tamingInfoLong = "That creature cannot be tamed.";
    public int terraformingPower = 5;

    public Brain brain;
    public CreatureState State {
        get => brain.state;
    }
    public List<CreatureAction> action = new List<CreatureAction>();

    private const float despawnTime = 128f;
    public const float neighborhood = 7.5f;

    [NonSerialized] public CharacterController controller;
    [NonSerialized] public Stats stats;
    [NonSerialized] public Team team;

    [NonSerialized] public Creature.Data? serializedData;
    void Start() {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<Stats>();
        team = GetComponent<Team>();
        brain = species.Brain(brainConfig).InitializeAll();
        InitializeActionList(brain);
        team.changed += TeamChangedEventHandler;

        if (serializedData is Data data) DeserializeNow(data);
        else cMaybeDespawn = StartCoroutine(MaybeDespawn());
    }

    private void InitializeActionList(Brain brain) {
        action.Add(CreatureAction.Roam);
        action.Add(CreatureAction.Station);
        action.AddRange(brain.Actions);
    }

    private void TeamChangedEventHandler(int newTeam) {
        transform.Find("Cardboard/Heart").GetComponentStrict<SpriteRenderer>().color = team.Color;
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
    }.TryUpdateCreature(this, 1);
    
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
        team.TeamId = player.GetComponentStrict<Team>().TeamId;
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

    public void GenericExeSucceeded() => stats.Exp += 2;

    public void AttackSucceeded(int? completedWithDef = null) {
        if (completedWithDef is int def) stats.Exp += def;
        else stats.Exp++;
    }

    public bool ReceiveDesireMessage(DesireMessage desireMessage) => new Senses() {
        desireMessage = desireMessage
    }.TryUpdateCreature(this, -1);

    public void CommandRoam() => new Senses() {
        command = Command.Roam()
    }.TryUpdateCreature(this);

    public void Station(Vector2Int location) => new Senses() {
        command = Command.Station(Terrain.I.CellCenter(location))
    }.TryUpdateCreature(this);

    public void ProcessDirective(OneOf<BehaviorNode, string> executingBehaviorOrError) {
        if (executingBehaviorOrError.Is(out string error))
            TextDisplay.I.ShowMiniText(UserFriendly(error));

        else new Senses() {
            command = Command.Execute((BehaviorNode)executingBehaviorOrError)
        }.TryUpdateCreature(this);
    }

    private string UserFriendly(string error) {
        if (error.StartsWith("insufficient_resource"))
            return new Regex("(?<=insufficient_resource\\().*(?=\\))").Match(error).Value + " " + stats.resourceName + " needed to do that";
        else
            return "Error: " + error;
    }

    private Coroutine cMaybeDespawn;
    private IEnumerator<YieldInstruction> MaybeDespawn() {
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
        if (boat != null && boat.player != null && boat.player == brain.state.command?.followDirective.Or(null)?.GetComponent<PlayerCharacter>())
            boat.RequestCreatureEnter(this);
    }

    [Serializable] public struct Data {
        public int x;
        public int y;
        public string species;
        public int team;
        public bool stationed;
        public string name;
        public int exp;

        public Vector2Int tile { get => Vct.I(x, y); }

        public Data(Vector2Int tile, string species, int team, bool stationed, string name, int exp) {
            this.x = tile.x;
            this.y = tile.y;
            this.species = species;
            this.team = team;
            this.stationed = stationed;
            this.name = name;
            this.exp = exp;
        }

        override public string ToString() {
            return species + ": " + name + (stationed ? " @ " : " ~ ") + tile + " " + " T" + team + " E" + exp;
        }
    }
    public Data Serialize() {
        return new Data(Terrain.I.CellAt(transform.position),
            creatureName,
            team.TeamId,
            brain.state.command?.type == CommandType.Station,
            gameObject.name,
            stats.Exp);
    }
    public void DeserializeUponStart(Data data) {
        serializedData = data;
    }
    private void DeserializeNow(Data data) {
        gameObject.name = data.name;
        team.TeamId = data.team;
        Debug.Log("Setting currentExp for saved creature " + gameObject.name);
        stats.SetExp(data.exp);
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