using System;
using System.Collections.Generic;
using UnityEngine;

public class Will {

    public static OneOf<CreatureState, string> Decide(CreatureState old, Senses input) {
        int? transitionPriority = null;
        OneOf<CreatureState, string> maybeResult = Decide(old, input, ref transitionPriority);
        if (maybeResult.Is(out CreatureState result)) {
            if (Habits.ForcedTransitionAllowed(old, result, transitionPriority)) return result;
            else return "Lower priority (" + transitionPriority + ") " + result.type + " not allowed to replace " + old.type;
        }
        else return maybeResult;
    }
    // States can naturally transition up
    // To transition horizontally or down, must have with relinquishedPriority set at least as high as the starting state.
    // When intentionally transitioning from a higher-priority state to a lower-priority state,
    // relinquishedPriority must be set to the higher priority,
    // otherwise this would fail.
    // relinquishedPriority should generally only be set when the input includes
    // a direct request to end that higher-priority state specifically.
    // All other requests are considered requests to escalate priority (or keep it the same level).
    public static OneOf<CreatureState, string> Decide(CreatureState state, Senses input, ref int? relinquishedPriority) {
        if (input.controlOverride.IsAdd) {
            return CreatureState.WithControlOverride(state, input.controlOverride.Value);
        } else if (input.controlOverride.IsRemove) {
            relinquishedPriority = (int)CreatureStateType.Override; // end override
            return CreatureState.WithoutControlOverride(state);
        } else if (input.faint) {
            return CreatureState.Fainted();
        } else if (input.command is Command command) {
            if (state.command?.type == CommandType.Execute || command.type == CommandType.Execute) {
                relinquishedPriority = (int)CreatureStateType.Execute; // may transition out of Execute
                if (state.command?.type == CommandType.Execute && command.type == CommandType.Execute)
                    command = ((Command)state.command).UpdateExecute(command);
                if (!command.followDirective.HasValue) {
                    if (state.command?.followDirective.HasValue == true)
                        command.followDirective = ((Command)state.command).followDirective;
                    else if (command.type == CommandType.Follow)
                        command = Command.Roam(); // follow request failed
                }
                return CreatureState.Command(command);
            } else if (state.type == CreatureStateType.Faint && command.type == CommandType.Follow) {
                relinquishedPriority = (int)CreatureStateType.Faint; // end faint
                return CreatureState.Command(command);
            } else if (command.type == CommandType.Roam) {
                relinquishedPriority = (int)CreatureStateType.PassiveCommand; // to another command
                return state.UpdatePassiveCommand(command);
            } else {
                relinquishedPriority = (int)CreatureStateType.CharacterFocus; // clear focus
                return CreatureState.Command(command);
            }
        } else if (input.hint is Hint hint) {
            if (!input.knowledge.config.hasAttack)
                return "hasAttack false";
            if (state.command?.type != CommandType.Follow)
                return "No use for player hints under command " + state.command?.type;

            if (!hint.offensive)
                return state.DisableFollowOffensive();
            else if (!hint.target.HasValue)
                return state.FollowOffensiveNoTarget(); // TODO identify immediately
            else {
                if (state.hint.target.HasValue)
                    return "already attacking" + state.hint.target.Value;
                return state.FollowOffensiveWithTarget(hint.target.Value); // TODO identify immediately
            }
        } else if (input.message is CreatureMessage message) {
            switch (message.type) {
                case CreatureMessage.Type.PairToSubject:
                    return CreatureState.Pair(state, message.master);
                case CreatureMessage.Type.EndPairToSubject:
                    if (!state.pairDirective.HasValue) return "Not paired already";
                    if (state.pairDirective.Value != message.master)
                        return "Paired to " + state.pairDirective.Value  + " not " + message.master;
                    relinquishedPriority = (int)CreatureStateType.Pair; // end pair
                    return CreatureState.Unpair(state);
                case CreatureMessage.Type.EndPairToMaster:
                    if (state.type != CreatureStateType.CharacterFocus) {
                        Debug.LogError("Why was there a pair when state " + state);
                        return "Can't remove pair focus when no focus";
                    } 
                    return state.ClearFocus();
                default:
                    return "No such CreatureMessage type";
            }
        } else if (input.desireMessage is DesireMessage desireMessage) {
            if (desireMessage.target.Is(out SpriteSorter target)){
                relinquishedPriority = (int)CreatureStateType.Investigate; // to another investigation
                return DesireAttack(input.knowledge.config, input.knowledge.position, state, target.transform);
            } else throw new NotImplementedException("Desire is Position");
        } else if (input.environment is Senses.Environment environment) {
            if (environment.characterFocus.IsAdd) {
                if (state.characterFocus.HasValue)
                    return "Trying to add characterFocus " + environment.characterFocus.Value +
                            " when already focused on " + state.characterFocus.Value;
                CreatureState result = state.ClearFocus().WithCharacterFocus(environment.characterFocus.Value);
                if (environment.focusIsPair.HasValue) result = result.WhereFocusIsPair(environment.focusIsPair.Value);
                return result;
            } else if (environment.characterFocus.IsRemove) {
                if (state.characterFocus.HasValue) {
                    relinquishedPriority = (int)CreatureStateType.CharacterFocus; // end focus
                    return state.ClearFocus();
                } else return "Trying to remove characterFocus when none present";
            } else if (environment.removeInvestigation) {
                if (state.characterFocus.HasValue)
                    return "Trying to remove investigation when focused on " + state.characterFocus.Value;
                if (state.investigation == null)
                    return "Trying to remove investigation when none present";
                relinquishedPriority = (int)CreatureStateType.Investigate; // end investigate
                return state.ClearFocus();
            } else return "environment present, but no change";
        } else return "No change";
    }

    public static OneOf<CreatureState, string> DesireAttack(BrainConfig config, Vector3 creaturePosition, CreatureState state, Transform target) {
        if (!config.hasAttack) return "No attack for desire";
        if (state.type >= CreatureStateType.Pair) return "Busy with " + state.type;
        bool canSee = CanSee(creaturePosition, target);
        if (canSee) return state.ClearFocus().WithCharacterFocus(target);
        else if (state.type != CreatureStateType.Investigate ||
                (creaturePosition - target.position).sqrMagnitude <
                (creaturePosition - state.investigation)?.sqrMagnitude)
            return state.ClearFocus().WithInvestigation(target.position);
        else return "Investigating something more important";
    }

    public static bool CanSee(Vector3 seerPosition, Transform seen) {
        if (Vector2.Distance(seerPosition, seen.position) > Creature.neighborhood)
            return false;
        SpriteSorter seenSprite = seen.GetComponentInChildren<SpriteSorter>();
        if (seenSprite == null)
            return true;
        else {
            bool result = Terrain.I.concealment.CanSee(seerPosition, seenSprite);
            if (!result && !TextDisplay.I.DisplayedYet("hiding")) TextDisplay.I.CheckpointInfo("hiding",
                "Nearby enemies cannot see you.  You are hidden from enemies when deep in trees and buildings, unless they get close.");
            return result;
        }
    }

    // Sanity check for NearestThreat to avoid contradiction
    // OverlapCircleAll may produce colliders with center slightly outside Creature.neighborhood
    public static bool IsThreat(int team, Vector3 creaturePosition, Transform threat) =>
        !Team.SameTeam(team, threat) && CanSee(creaturePosition, threat);

    public static Transform NearestThreat(int team, Vector3 creaturePosition) => NearestThreat(team, creaturePosition, null);
    public static Transform NearestThreat(int team, Vector3 creaturePosition, Func<Collider2D, bool> filter) {
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(creaturePosition, Creature.neighborhood, LayerMask.GetMask("Player", "HealthCreature"));
        List<Transform> threats = new List<Transform>();
        foreach (Collider2D character in charactersNearby) {
            if (IsThreat(team, creaturePosition, character.transform) && (filter?.Invoke(character) != false))
                if (character.GetComponent<Creature>()?.brainConfig?.hasAttack == true ||
                        character.GetComponent<PlayerCharacter>() != null)
                    threats.Add(character.transform);
        }
        if (threats.Count == 0) return null;
        return threats.MinBy(threat => (threat.position - creaturePosition).sqrMagnitude);
    }
}
