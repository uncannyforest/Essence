using System;
using System.Collections.Generic;
using UnityEngine;

public enum CommandType {
    Roam,
    Follow,
    FollowOffensive, // TODO deprecated
    Station,
    Execute,
    Pair, // TODO deprecated
    Faint, // TODO deprecated
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
}

public struct CreatureMessage {
    public enum Type {
        PairToSubject,
        EndPairToSubject,
        EndPairToMaster,
    }

    public Type type;
    public Transform master; // required except for EndPairToMaster
}

public struct Hint {
    public bool offensive;
    public Optional<Transform> target;
}

public struct Senses {
    public static Senses ForCreature(Brain brain) {
        Senses senses = new Senses();
        senses.knowledge = new PersistentProperties(
            brain.general,
            brain.transform.position,
            brain.team
        );
        return senses;
    }

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
    }
}
