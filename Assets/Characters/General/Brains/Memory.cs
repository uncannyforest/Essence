using System;
using System.Collections.Generic;
using UnityEngine;

public enum CreatureStateType {
    Override = 1000,
    Faint = 800,
    Execute = 600,
    Scan = 0,
}

public enum ScanActivityType {
    Focus = 500,
    FollowPair = 400,
    Rest = 300,
    Investigate = 200,
    PassiveCommand = 100,
}

public enum CreatureStateDetailedType {
    Override = 1000,
    Faint = 800,
    Execute = 600,
    Focus = 500,
    FollowPair = 400,
    Rest = 300,
    Investigate = 200,
    PassiveCommand = 100,
}

public struct CreatureState {
    private static CreatureState OfType(CreatureStateType type) {
        CreatureState result = new CreatureState();
        result.type = type;
        return result;
    }
    
    public CreatureStateType type { get; private set; }
    public CreatureStateDetailedType detailedType {
        get => type == CreatureStateType.Scan ?
            (CreatureStateDetailedType)(int)((ScanActivity)scanActivity).type :
            (CreatureStateDetailedType)(int)type;
    }

    private Optional<MonoBehaviour> controlOverride;
    private CreatureStateType? controlOverridePrevState;
    public MonoBehaviour ControlOverride { get => controlOverride.Value; }
    public static CreatureState WithControlOverride(CreatureState copiedState, MonoBehaviour source) {
        if (copiedState.type == CreatureStateType.Override) throw new ArgumentException("Nested control override not supported");
        copiedState.controlOverridePrevState = copiedState.type;
        copiedState.controlOverride = Optional.Of(source);
        copiedState.type = CreatureStateType.Override;
        return copiedState;
    }
    public static CreatureState WithoutControlOverride(CreatureState copiedState) {
        if (copiedState.type != CreatureStateType.Override) throw new ArgumentException("Not control override");
        copiedState.type = copiedState.controlOverridePrevState.Value;
        copiedState.controlOverride = Optional<MonoBehaviour>.Empty();
        copiedState.controlOverridePrevState = null;
        return copiedState;
    }

    public static CreatureState Fainted() {
        return CreatureState.OfType(CreatureStateType.Faint);
    }

    public ExecuteCommand? executeCommand;
    public static CreatureState Execute(ExecuteCommand executeCommand) {
        CreatureState result = CreatureState.OfType(CreatureStateType.Execute);
        result.executeCommand = executeCommand;
        return result;
    }

    public ScanActivity? scanActivity;
    public bool IsScanning => scanActivity != null;
    public static CreatureState PassiveCommand(PassiveCommand command) => ScanActivity(global::ScanActivity.ForCommand(command));
    public static CreatureState ScanActivity(ScanActivity? scanActivity) {
        CreatureState result = CreatureState.OfType(CreatureStateType.Scan);
        result.scanActivity = scanActivity;
        return result;
    }
    public CreatureState EndScanState() {
        CreatureState result = this;
        result.scanActivity = this.scanActivity?.EndCurrentState();
        return result;
    }
    public WhyNot CanBecomeScanActivity(Senses input) => Will.CanBecomeScanActivity(this, input);

    public override string ToString() {
        string result = type.ToString();
        if (type == CreatureStateType.Override) result +=
            " | control override: " + controlOverride.Value
            + " | previous state: " + controlOverridePrevState;
        if (executeCommand is ExecuteCommand actualExecuteCommand) result += " | execute command: " + actualExecuteCommand;
        if (scanActivity is ScanActivity actualScanActivity) result += " | scan activity: " + actualScanActivity;
        return result;
    }
}

public struct ScanActivity : Positioned {
    public static ScanActivity ForCommand(PassiveCommand command) {
        ScanActivity result = new ScanActivity();
        result.command = command;
        result.type = ScanActivityType.PassiveCommand;
        return result;
    }
    
    public ScanActivityType type { get; private set; }
    public PassiveCommand command { get; private set; }
    public Optional<Creature> followerToLead;

    public Optional<Transform> characterFocus; // or pair leader to follow
    public DesireMessage.Obstacle? terrainFocus;
    public Vector3? investigation;
    public Vector2Int? shelter;

    public bool HasValidPosition => type != ScanActivityType.PassiveCommand && !characterFocus.IsDestroyed;
    public Vector3 GetPosition()
        => characterFocus.HasValue ? characterFocus.Value.position
        : terrainFocus.HasValue ? (Vector3)Terrain.I.CellCenter(terrainFocus.Value.location)
        : type == ScanActivityType.Investigate ? (Vector3)investigation
        : type == ScanActivityType.Rest ? (Vector3)Terrain.I.CellCenter((Vector2Int)shelter)
        : throw new Exception("Can't get position when in state PassiveComand");

    public ScanActivity EndFollow(PassiveCommand command) {
        ScanActivity state = this;
        state.command = command;
        return state;
    }

    public ScanActivity Pair(Transform pairDirective) {
        ScanActivity state = this.ClearExtraneousFocus();
        state.type = ScanActivityType.FollowPair;
        state.characterFocus = Optional.Of(pairDirective);
        return state;
    }
    public ScanActivity Unpair() {
        ScanActivity state = this.ClearExtraneousFocus();
        state.type = ScanActivityType.PassiveCommand;
        return state;
    }

    public ScanActivity WithCharacterFocus(Transform characterFocus) {
        ScanActivity state = this.ClearExtraneousFocus();
        state.type = ScanActivityType.Focus;
        state.characterFocus = Optional.Of(characterFocus);
        return state;
    }
    public ScanActivity WithFollowerToLead(Creature followerToLead) {
        ScanActivity state = this;
        if (state.type != ScanActivityType.Focus) throw new InvalidOperationException("Can only lead pairs in Focus"); // may expand the options later
        state.followerToLead = Optional.Of(followerToLead);
        return state;
    }
    public ScanActivity WithTerrainFocus(DesireMessage.Obstacle terrainFocus) {
        ScanActivity state = this.ClearExtraneousFocus();
        state.type = ScanActivityType.Focus;
        state.terrainFocus = terrainFocus;
        return state;
    }
    public ScanActivity WithInvestigation(Vector3 investigation) {
        ScanActivity state = this.ClearExtraneousFocus();
        state.type = ScanActivityType.Investigate;
        state.investigation = investigation;
        return state;
    }
    public ScanActivity WithShelter(Vector2Int shelter) {
        ScanActivity state = this.ClearExtraneousFocus();
        state.type = ScanActivityType.Rest;
        state.shelter = shelter;
        return state;
    }
    public ScanActivity ClearAllFocus() {
        ScanActivity state = this.ClearExtraneousFocus();
        state.followerToLead = Optional<Creature>.Empty();
        state.type = ScanActivityType.PassiveCommand;
        return state;
    }
    public ScanActivity EndCurrentState() {
        if (type == ScanActivityType.PassiveCommand) return ForCommand(PassiveCommand.Roam());
        else return ClearAllFocus();
    }

    private ScanActivity ClearExtraneousFocus() {
        ScanActivity state = this;
        state.characterFocus = Optional<Transform>.Empty();
        state.terrainFocus = null;
        state.investigation = null;
        state.shelter = null;
        return state;
    }

    public override string ToString() {
        string result = "command " + command + " activity " + type;
        if (shelter is Vector2Int realShelter) result += " | shelter target: " + realShelter + " at " + Terrain.I.CellCenter(realShelter);
        if (characterFocus.HasValue) result += " | character target: " + characterFocus.Value;
        if (terrainFocus is DesireMessage.Obstacle focus) result += " | terrain target: unblocking " + focus.requestor;
        if (investigation is Vector3 realInvestigation) result += " | investigation target: " + realInvestigation;
        if (followerToLead.HasValue) result += " | follower to lead: " + followerToLead.Value.gameObject.name;
        return result;
    }
}