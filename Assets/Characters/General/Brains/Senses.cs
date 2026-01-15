using System;
using System.Collections.Generic;
using UnityEngine;

public enum PassiveCommandType {
    Roam,
    Follow,
    Station,
}

public struct ExecuteCommand {
    public BehaviorNode executeDirective { get; private set; }
    public Transform followDirective { get; private set; }

    public static ExecuteCommand New(BehaviorNode executeDirective, Transform followDirective) {
        ExecuteCommand command = new ExecuteCommand();
        command.executeDirective = executeDirective;
        command.followDirective = followDirective;
        return command;
    }
    public ExecuteCommand Update(BehaviorNode executeDirective) {
        ExecuteCommand command = this;
        command.executeDirective = command.executeDirective.UpdateWithNewBehavior(executeDirective);
        return command;
    }
    public PassiveCommand ToFollow() => PassiveCommand.Follow(followDirective);

    public override string ToString() => "follow " + followDirective.gameObject.name + " execute " + executeDirective;
}

public struct PassiveCommand {
    private static PassiveCommand OfType(PassiveCommandType type) {
        PassiveCommand command = new PassiveCommand();
        command.type = type;
        return command;
    }

    public PassiveCommandType type { get; private set; }
    public Optional<Transform> followDirective;
    public Vector3? stationDirective { get; private set; }

    public static PassiveCommand Roam() => PassiveCommand.OfType(PassiveCommandType.Roam);
    public static PassiveCommand Follow(Transform followDirective) {
        PassiveCommand command = PassiveCommand.OfType(PassiveCommandType.Follow);
        command.followDirective = Optional.Of(followDirective);
        return command;
    }
    public static PassiveCommand RequestFollow() {
        return PassiveCommand.OfType(PassiveCommandType.Follow);
    }
    public static PassiveCommand Station(Vector3 stationDirective) {
        PassiveCommand command = PassiveCommand.OfType(PassiveCommandType.Station);
        command.stationDirective = stationDirective;
        return command;
    }

    public override string ToString() {
        string result = type.ToString();
        if (followDirective.HasValue) result += " | follow: " + followDirective.Value.gameObject.name;
        if (stationDirective is Vector3 directive) result += " | station: " + directive;
        return result;
    }
}

public struct CreatureMessage {
    public enum Type {
        PairToSubject,
        EndPairToSubject,
        EndPairToMaster,
    }

    public Type type;
    public Transform master; // required except for EndPairToMaster

    public static CreatureMessage PairToSubject(Transform master) {
        CreatureMessage message = new CreatureMessage();
        message.type = Type.PairToSubject;
        message.master = master;
        return message;
    }
    public static CreatureMessage EndPairToSubject(Transform master) {
        CreatureMessage message = new CreatureMessage();
        message.type = Type.EndPairToSubject;
        message.master = master;
        return message;
    }
    public static CreatureMessage EndPairToMaster() {
        CreatureMessage message = new CreatureMessage();
        message.type = Type.EndPairToMaster;
        return message;
    }

    override public string ToString() {
        string result = type.ToString();
        if (master != null) result += " master: " + master.gameObject.name;
        return result;
    }
}

public struct Senses {
    public static Senses CreateForCreature(Creature creature) {
        Senses senses = new Senses()
            { knowledge = PersistentProperties.ForCreature(creature) };
        return senses;
    }

    public Senses ForCreature(Creature creature) {
        knowledge = PersistentProperties.ForCreature(creature);
        return this;
    }

    public bool TryUpdateState(Brain brain, int logLevel = 0) {
        return brain.TryUpdateState(this, logLevel);
    }

    public bool TryUpdateCreature(Creature creature, int logLevel = 0) {
        if (creature.brain == null) {
            Debug.Log("Tried to update " + creature + " before it was initialized, this happens with broadcasting desire messages on occasion");
            return false;
        }
        return ForCreature(creature).TryUpdateState(creature.brain, logLevel);
    }

    public bool endState;

    public Delta<MonoBehaviour> controlOverride;

    public bool faint;

    public Optional<BehaviorNode> executeDirective;

    public PassiveCommand? passiveCommand;

    public CreatureMessage? message;

    public DesireMessage? desireMessage;

    public Environment? environment;
    public struct Environment {
        public Optional<Transform> characterFocus;
        public Optional<Creature> focusIsPair;
        public Optional<Vector2Int> shelter;

        override  public string ToString(){
            string result = "";
            if (characterFocus.HasValue) result += " character focus: " + characterFocus.Value.gameObject.name;
            if (focusIsPair.HasValue) result += " focus is follower to lead: " + focusIsPair.Value.gameObject.name;
            if (shelter.HasValue) result += " shelter: " + shelter.Value;
            return result.Substring(1);
        }
    }

    public PersistentProperties knowledge;
    public struct PersistentProperties {
        public BrainConfig config;
        public Vector3 position;
        public int team;
        public float healthFraction;
        public float resourceFraction;

        public PersistentProperties(BrainConfig config, Vector3 position, int team, float health, float resource) {
            this.config = config;
            this.position = position;
            this.team = team;
            this.healthFraction = health;
            this.resourceFraction = resource;
        }
        public static PersistentProperties ForCreature(Creature creature) {
            return new PersistentProperties(
                creature.brainConfig,
                creature.transform.position,
                creature.GetComponentStrict<Team>().TeamId,
                creature.GetComponent<Health>()?.LevelPercent ?? 1, // TODO make Strict when I officially force Health
                creature.GetComponentStrict<Resource>().LevelPercent);
        }
    }

    override public string ToString() {
        string result = "";
        if (endState) result += " end state";
        if (controlOverride.IsAdd) result += " add control override: " + controlOverride.Value;
        if (controlOverride.IsRemove) result += " remove control override";
        if (faint) result += " faint";
        if (executeDirective.HasValue) result += " execute directive: " + executeDirective.Value;
        if (passiveCommand is PassiveCommand actualCommand) result += " passive command: " + actualCommand;
        if (message is CreatureMessage actualMessage) result += " creature message: " + actualMessage;
        if (desireMessage is DesireMessage actualDesireMessage) result += " desire message: " + actualDesireMessage;
        if (environment is Environment actualEnvironment) result += " environment: " + actualEnvironment;
        return result.Substring(1);
    }
}
