using System;
using System.Collections.Generic;
using UnityEngine;

public class Will {

    public static OneOf<CreatureState, string> Decide(CreatureState old, Senses input) {
        int? transitionPriority = null;
        OneOf<CreatureState, string> maybeResult = Decide(old, input, ref transitionPriority);
        if (maybeResult.Is(out CreatureState result)) {
            if (Habit.ForcedTransitionAllowed(old, result, transitionPriority)) return result;
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
        if (input.endState) {
            relinquishedPriority = (int)CreatureStateType.Override; // end anything
            switch (state.type) {
                case CreatureStateType.Override:
                    return CreatureState.WithoutControlOverride(state);
                case CreatureStateType.Execute:
                    if (state.command?.followDirective.HasValue == true)
                        return CreatureState.Command(Command.Follow(((Command)state.command).followDirective.Value));
                    else return CreatureState.Command(Command.Roam()); // follow request failed
                case CreatureStateType.Pair:
                    return CreatureState.Unpair(state);
                case CreatureStateType.Focus:
                case CreatureStateType.Rest:
                case CreatureStateType.Investigate:
                    return state.ClearFocus();
                case CreatureStateType.PassiveCommand:
                    return CreatureState.Command(Command.Roam());
                default: throw new ArgumentException("End state on wrong state");
            }

        } else if (input.controlOverride.IsAdd) {
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
                // If Follow/Execute -> Execute, copy over followDirective
                // If Execute -> non-Execute, request follow
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
            } else if (command.type == CommandType.Follow) {
                relinquishedPriority = (int)CreatureStateType.Focus; // clear focus
                if (Team.SameTeam(input.knowledge.team, command.followDirective.Value))
                    return CreatureState.Command(command);
                else return "Creature on team " + input.knowledge.team +
                    " not willing to follow " + command.followDirective.Value.gameObject.name;
            } else {
                relinquishedPriority = (int)CreatureStateType.Focus; // clear focus
                return CreatureState.Command(command);
            }

        } else if (input.hint is Hint hint) {
            if (!input.knowledge.config.hasAttack)
                return "hasAttack false";
            if (state.command?.type != CommandType.Follow)
                return "No use for player hints under command " + state.command?.type;

            if (!hint.generallyOffensive && !hint.target.HasValue)
                return state.DisableFollowOffensive();
            else if (hint.generallyOffensive)
                return state.FollowOffensive();
            else {
                relinquishedPriority = (int)CreatureStateType.Investigate; // to another investigation
                return DesireAttack(input.knowledge.config, input.knowledge.position, state, hint.target.Value);
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
                    if (state.type != CreatureStateType.Focus) { // we must have initiated
                        return "Can't remove pair focus when no focus";
                    } 
                    return state.ClearFocus();
                default:
                    return "No such CreatureMessage type";
            }

        } else if (input.desireMessage is DesireMessage desireMessage) {
            relinquishedPriority = (int)CreatureStateType.Rest; // to another investigation, or stop resting
            if (desireMessage.assailant.HasValue)
                return DesireAttack(input.knowledge.config, input.knowledge.position, state, desireMessage.assailant.Value);
            else if (desireMessage.obstacle is DesireMessage.Obstacle obstacle) {
                return DesireClearObstacle(input.knowledge.config, state, obstacle);
            } else throw new ArgumentException("DesireMessage must have contents");

        } else if (input.environment is Senses.Environment environment) {
            if (environment.characterFocus.IsAdd) {
                CreatureState result = state.ClearFocus().WithCharacterFocus(environment.characterFocus.Value);
                if (environment.focusIsPair.HasValue) result = result.WhereFocusIsPair(environment.focusIsPair.Value); // friend
                else relinquishedPriority = (int)CreatureStateType.Rest; // stop resting to attack enemy
                return result;
            } else if (environment.characterFocus.IsRemove) {
                if (!state.characterFocus.HasValue)
                    return "Trying to remove characterFocus when none present";
                relinquishedPriority = (int)CreatureStateType.Focus; // end focus
                return state.ClearFocus();
            } else if (environment.removeInvestigation) {
                if (state.investigation == null)
                    return "Trying to remove investigation when none present";
                relinquishedPriority = (int)CreatureStateType.Investigate; // end investigate
                return state.ClearFocus();
            } else if (environment.shelter.IsAdd) {
                return state.ClearFocus().WithShelter(environment.shelter.Value);
            } else if (environment.shelter.IsRemove) {
                if (state.shelter == null)
                    return "Trying to remove shelter when none present";
                relinquishedPriority = (int)CreatureStateType.Rest; // end rest
                return state.ClearFocus();
            } else return "environment present, but no change";
        } else return "No change";
    }

    public static OneOf<CreatureState, string> DesireClearObstacle(BrainConfig config, CreatureState state, DesireMessage.Obstacle obstacle) {
        if (obstacle.wallObstacle is Construction wall) {
            if (config.canClearConstruction.includes(wall)) return state.ClearFocus().WithTerrainFocus(obstacle);
            else return "Cannot clear wall of " + wall;
        } else if (obstacle.featureObstacle != null) {
            if (!config.canClearFeatures) return "Cannot clear obstacles";
            else if (FeatureLibrary.C.fountain.IsTypeOf(obstacle.featureObstacle)) return "Cannot clear fountains";
            else return state.ClearFocus().WithTerrainFocus(obstacle);
        } else if (obstacle.landObstacle is Land land) {
            if (config.canClearObstacles.includes(land)) return state.ClearFocus().WithTerrainFocus(obstacle);
            else return "Cannot clear land " + land;
        } else throw new ArgumentException("Obstacle message must include expected obstacle");
    }

    public static bool CanClearObstacleAt(BrainConfig config, Terrain.Position location) {
        if (!Terrain.I.InBounds(location)) return false;
        if (location.grid != Terrain.Grid.Roof)
            return config.canClearConstruction.includes(Terrain.I[location]);
        else if (Terrain.I.Land[location.Coord] == Land.Dirtpile
            && config.canClearObstacles.includes(Land.Dirtpile)) return true;
        else if (Terrain.I.Feature[location.Coord] != null)
            return config.canClearFeatures && !FeatureLibrary.C.fountain.IsTypeOf(Terrain.I.Feature[location.Coord]);
        else return config.canClearObstacles.includes(Terrain.I.Land[location.Coord]);
    }

    public static OneOf<CreatureState, string> DesireAttack(BrainConfig config, Vector3 creaturePosition, CreatureState state, Transform target) {
        if (!config.hasAttack) return "No attack for desire";
        bool canSee = CanSee(creaturePosition, target).NegLog("Desire: investigating attack");
        if (canSee) return state.ClearFocus().WithCharacterFocus(target);
        else if (state.type != CreatureStateType.Investigate ||
                Disp.FT(creaturePosition, target.position).sqrMagnitude <
                Disp.FT(creaturePosition, (Vector2)state.investigation).sqrMagnitude)
            return state.ClearFocus().WithInvestigation(target.position);
        else return "Investigating something more important";
    }

    public static WhyNot CanSee(Vector3 seerPosition, Transform seen) {
        if (Vector2.Distance(seerPosition, seen.position) > Creature.neighborhood)
            return "view_too_far";
        Character seenSprite = seen.GetComponent<Character>();
        if (seenSprite == null)
            return true;
        else {
            WhyNot result = Terrain.I.concealment.CanSee(seerPosition, seenSprite);
            if (!(bool)result && !TextDisplay.I.DisplayedYet("hiding")) TextDisplay.I.CheckpointInfo("hiding",
                "Nearby enemies cannot see you.  You are hidden from enemies when deep in trees and buildings, unless they get close.");
            return result;
        }
    }

    public static bool Attackable(Transform possibleThreat)
        => (possibleThreat.GetComponent<Health>()?.Level ?? 0) > 0;

    // Sanity check for NearestThreat to avoid contradiction
    // OverlapCircleAll may produce colliders with center slightly outside Creature.neighborhood
    public static WhyNot IsThreat(int team, Vector3 creaturePosition, Transform threat) =>
        Team.SameTeam(team, threat) ? "same_team" :
        !Attackable(threat) ? "not_attackable" :
        CanSee(creaturePosition, threat);

    public static Optional<Transform> NearestThreat(Brain brain) => NearestThreat(brain, null);
    public static Optional<Transform> NearestThreat(Brain brain, Func<Collider2D, bool> filter) {
        int team = brain.teamId;
        Vector3 creaturePosition = brain.transform.position;
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(creaturePosition, Creature.neighborhood, LayerMask.GetMask("Player", "HealthCreature"));
        List<Transform> threats = new List<Transform>();
        foreach (Collider2D character in charactersNearby) {
            if ((bool)IsThreat(team, creaturePosition, character.transform) && (filter?.Invoke(character) != false))
                if (character.GetComponent<Creature>()?.brainConfig?.hasAttack == true ||
                        character.GetComponent<PlayerCharacter>() != null)
                    threats.Add(character.transform);
        }
        if (threats.Count == 0) return Optional<Transform>.Empty();
        return Optional.Of(threats.MinBy(threat => (threat.position - creaturePosition).sqrMagnitude));
    }
}
