using System;
using System.Collections.Generic;
using UnityEngine;

public enum CommandType {
    Roam,
    Follow,
    Station,
    Execute,
}

public struct Command {
    private static Command OfType(CommandType type) {
        Command command = new Command();
        command.type = type;
        return command;
    }

    public CommandType type { get; private set; }
    public Optional<Transform> followDirective;
    public Vector3? stationDirective { get; private set; }
    public BehaviorNode executeDirective { get; private set; }

    public static Command Roam() => Command.OfType(CommandType.Roam);
    public static Command Follow(Transform followDirective) {
        Command command = Command.OfType(CommandType.Follow);
        command.followDirective = Optional.Of(followDirective);
        return command;
    }
    public static Command RequestFollow() {
        return Command.OfType(CommandType.Follow);
    }
    public static Command Station(Vector3 stationDirective) {
        Command command = Command.OfType(CommandType.Station);
        command.stationDirective = stationDirective;
        return command;
    }
    public static Command Execute(BehaviorNode executeDirective) {
        Command command = Command.OfType(CommandType.Execute);
        command.executeDirective = executeDirective;
        return command;
    }
    public Command UpdateExecute(Command executeCommand) {
        if (executeCommand.type != CommandType.Execute) throw new ArgumentException("Not execute command");
        Command command = executeCommand;
        command.executeDirective = this.executeDirective.UpdateWithNewBehavior(executeCommand.executeDirective);
        return command;
    }

    public override string ToString() {
        string result = type.ToString();
        if (followDirective.HasValue) result += " | follow: " + followDirective.Value.gameObject.name;
        if (stationDirective is Vector3 directive) result += " | station: " + directive;
        if (executeDirective != null) result += " | execute: " + executeDirective;
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

public struct Hint {
    public bool generallyOffensive;
    public Optional<Transform> target;

    override public string ToString() {
        string result = "";
        if (generallyOffensive) result += " generally offensive";
        if (target.HasValue) result += " target: " + target.Value.gameObject.name;
        return result.Substring(1);
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
        if (creature.brain == null) throw new InvalidOperationException(creature + " has no brain");
        return ForCreature(creature).TryUpdateState(creature.brain, logLevel);
    }

    public bool endState;

    public Delta<MonoBehaviour> controlOverride;

    public bool faint;

    public Command? command;

    public Hint? hint;

    public CreatureMessage? message;

    public DesireMessage? desireMessage;

    public Environment? environment;
    public struct Environment {
        public Delta<Transform> characterFocus;
        public Optional<Creature> focusIsPair;
        public bool removeInvestigation;

        override  public string ToString(){
            string result = "";
            if (characterFocus.IsAdd) result += " add character focus: " + characterFocus.Value.gameObject.name;
            if (characterFocus.IsRemove) result += " remove character focus";
            if (removeInvestigation) result += " remove investigation";
            return result.Substring(1);
        }
    }

    public PersistentProperties knowledge;
    public struct PersistentProperties {
        public BrainConfig config;
        public Vector3 position;
        public int team;

        public PersistentProperties(BrainConfig config, Vector3 position, int team) {
            this.config = config;
            this.position = position;
            this.team = team;
        }
        public static PersistentProperties ForCreature(Creature creature) {
            return new PersistentProperties(
                creature.brainConfig,
                creature.transform.position,
                creature.GetComponentStrict<Team>().TeamId);
        }
    }

    override public string ToString() {
        string result = "";
        if (endState) result += " end state";
        if (controlOverride.IsAdd) result += " add control override: " + controlOverride.Value;
        if (controlOverride.IsRemove) result += " remove control override";
        if (faint) result += " faint";
        if (command is Command actualCommand) result += " command: " + actualCommand;
        if (hint is Hint actualHint) result += " hint: " + hint;
        if (message is CreatureMessage actualMessage) result += " creature message: " + actualMessage;
        if (desireMessage is DesireMessage actualDesireMessage) result += " desire message: " + actualDesireMessage;
        if (environment is Environment actualEnvironment) result += " environment: " + actualEnvironment;
        return result.Substring(1);
    }
}
