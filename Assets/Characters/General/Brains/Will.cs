using System;
using System.Collections.Generic;
using UnityEngine;

public class Will {

    public static OneOf<CreatureState, string> Decide(CreatureState state, Senses input) {
        if (input.endState) {
            switch (state.type) {
                case CreatureStateType.Override:
                    return CreatureState.WithoutControlOverride(state);
                case CreatureStateType.Execute:
                    return CreatureState.PassiveCommand(((ExecuteCommand)state.executeCommand).ToFollow());
                case CreatureStateType.Scan:
                    return state.EndScanState();
                default: throw new ArgumentException("End state on wrong state");
            }

        } else if (input.controlOverride.IsAdd) {
            if (state.type == CreatureStateType.Override) return "Can't nest overrides";
            return CreatureState.WithControlOverride(state, input.controlOverride.Value);

        } else if (input.controlOverride.IsRemove) {
            return CreatureState.WithoutControlOverride(state);

        } else if (input.faint) {
            if (state.type == CreatureStateType.Override || state.type == CreatureStateType.Faint) return "Cannot Faint from " + state.type;
            else return CreatureState.Fainted();

        } else if (input.executeDirective.HasValue) {
            if (state.type == CreatureStateType.Execute)
                return CreatureState.Execute(((ExecuteCommand)state.executeCommand).Update(input.executeDirective.Value));
            else if (state.scanActivity?.command.followDirective is Optional<Transform> followDirective && followDirective.HasValue)
                return CreatureState.Execute(ExecuteCommand.New(input.executeDirective.Value, followDirective.Value));
            else throw new ArgumentException("Cannot Execute except from Execute or Follow");

        } else return DecideScan(state, input);
    }

    public static OneOf<CreatureState, string> DecideScan(CreatureState state, Senses input) {
        if (state.type == CreatureStateType.Faint && input.passiveCommand?.type == PassiveCommandType.Follow) {
            return CreatureState.PassiveCommand((PassiveCommand)input.passiveCommand);
        } else if (state.type == CreatureStateType.Scan) {
            ScanActivity oldScan = (ScanActivity)state.scanActivity;
            OneOf<ScanActivity, string> possibleNewScan = DraftScanUpdate(oldScan, input);
            if (possibleNewScan.Is(out string error)) return error;
            ScanActivity newScan = (ScanActivity)possibleNewScan;
            if (oldScan.type == ScanActivityType.PassiveCommand || newScan.type == ScanActivityType.PassiveCommand) {
                return CreatureState.ScanActivity(newScan);
            } else {
                WhyNot preferNewScan = WeighOptions(oldScan, newScan, input.knowledge.position);
                if (preferNewScan) return CreatureState.ScanActivity(newScan);
                else return (string)preferNewScan;
            }
        } else {
            return "Cannot Scan from state " + state.type;
        }
    }
            
    public static WhyNot CanBecomeScanActivity(CreatureState state, Senses input) {
        if (DecideScan(state, input).Is(out string reason)) return reason;
        else return true;
    }

    public static OneOf<ScanActivity, string> DraftScanUpdate(ScanActivity state, Senses input) {
        // Player commands
        if (input.passiveCommand is PassiveCommand command) {
            if (command.type == PassiveCommandType.Follow) return ScanActivity.ForCommand(command); // resets focuses
            else return state.EndFollow(command);

        // Creature commands
        } else if (input.message is CreatureMessage message) {
            switch (message.type) {
                case CreatureMessage.Type.PairToSubject:
                    return state.Pair(message.master);
                case CreatureMessage.Type.EndPairToSubject:
                    if (!state.characterFocus.HasValue) return "Not paired already";
                    if (state.characterFocus.Value != message.master)
                        return "Paired to " + state.characterFocus.Value  + " not " + message.master;
                    return state.Unpair();
                case CreatureMessage.Type.EndPairToMaster:
                    if (state.type != ScanActivityType.Focus) // we must have initiated
                        return "Can't remove pair focus when no focus";
                    return state.ClearAllFocus();
                default:
                    return "No such CreatureMessage type";
            }

        // Creature requests
        } else if (input.desireMessage is DesireMessage desireMessage) {
            if (desireMessage.assailant.HasValue)
                return DesireAttack(input.knowledge.config, input.knowledge.position, state, desireMessage.assailant.Value);
            else if (desireMessage.obstacle is DesireMessage.Obstacle obstacle)
                return DesireClearObstacle(input.knowledge.config, state, obstacle);
            else throw new ArgumentException("DesireMessage must have contents");

        // Observations of surroundings
        } else if (input.environment is Senses.Environment environment) {
            ScanActivity? characterFocusActivity = null;
            ScanActivity? shelterActivity = null;
            if (environment.characterFocus.HasValue) {
                characterFocusActivity = state.ClearAllFocus().WithCharacterFocus(environment.characterFocus.Value);
                if (environment.focusIsPair.HasValue) characterFocusActivity =
                    ((ScanActivity)characterFocusActivity).WithFollowerToLead(environment.focusIsPair.Value); // friend
            }
            if (environment.shelter.HasValue) {
                shelterActivity = state.ClearAllFocus().WithShelter(environment.shelter.Value);
            }
            if (characterFocusActivity is ScanActivity characterFocus) {
                if (shelterActivity is ScanActivity shelter)
                    return (bool)WeighOptions(characterFocus, shelter, input.knowledge.position) ? shelter : characterFocus;
                else return characterFocus;
            } else {
                if (shelterActivity is ScanActivity shelter) return shelter;
                else return "environment present, but no change";
            }
        } else return "No change";
    }

    // Returns true if second is better
    public static WhyNot WeighOptions(ScanActivity oldAct, ScanActivity newAct, Vector3 position) {
        if (!oldAct.HasValidPosition) return true;
        if (Disp.FT(position, newAct.GetPosition()).sqrMagnitude < Disp.FT(position, oldAct.GetPosition()).sqrMagnitude) {
            Debug.DrawLine(position, oldAct.GetPosition(), Color.red, 1);
            Debug.DrawLine(position, newAct.GetPosition(), Color.yellow, 1);
            return true;
        }
        Debug.DrawLine(position, oldAct.GetPosition(), Color.magenta, 1);
        Debug.DrawLine(position, newAct.GetPosition(), Color.blue, 1);
        return "Prefer " + oldAct + " to " + newAct + " because it's closer";
    }

    public static OneOf<ScanActivity, string> DesireClearObstacle(BrainConfig config, ScanActivity state, DesireMessage.Obstacle obstacle) {
        if (obstacle.wallObstacle is Construction wall) {
            if (config.canClearConstruction.includes(wall)) return state.ClearAllFocus().WithTerrainFocus(obstacle);
            else return "Cannot clear wall of " + wall;
        } else if (obstacle.featureObstacle != null) {
            if (!config.canClearFeatures) return "Cannot clear obstacles";
            else if (FeatureLibrary.C.fountain.IsTypeOf(obstacle.featureObstacle)) return "Cannot clear fountains";
            else return state.ClearAllFocus().WithTerrainFocus(obstacle);
        } else if (obstacle.landObstacle is Land land) {
            if (config.canClearObstacles.includes(land)) return state.ClearAllFocus().WithTerrainFocus(obstacle);
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

    public static OneOf<ScanActivity, string> DesireAttack(BrainConfig config, Vector3 creaturePosition, ScanActivity state, Transform target) {
        if (!config.hasAttack) return "No attack for desire";
        bool canSee = CanSee(creaturePosition, target).NegLog("Desire: investigating attack");
        if (canSee) return state.ClearAllFocus().WithCharacterFocus(target);
        else if (state.type != ScanActivityType.Investigate ||
                Disp.FT(creaturePosition, target.position).sqrMagnitude <
                Disp.FT(creaturePosition, (Vector2)state.investigation).sqrMagnitude)
            return state.ClearAllFocus().WithInvestigation(target.position);
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
    public static WhyNot IsVisibleThreat(int team, Vector3 creaturePosition, Transform threat) =>
        IsThreat(team, threat) && CanSee(creaturePosition, threat);

    public static WhyNot IsThreat(int team, Transform threat) =>
        Team.SameTeam(team, threat) ? "same_team" :
        !Attackable(threat) ? "not_attackable" : (WhyNot)true;

    public static Optional<Transform> NearestThreat(Brain brain) => NearestThreat(brain, null);
    public static Optional<Transform> NearestThreat(Brain brain, Func<Collider2D, bool> filter) {
        int team = brain.teamId;
        Vector3 creaturePosition = brain.transform.position;
        Collider2D[] charactersNearby =
            Physics2D.OverlapCircleAll(creaturePosition, Creature.neighborhood, LayerMask.GetMask("Player", "HealthCreature"));
        List<Transform> threats = new List<Transform>();
        foreach (Collider2D character in charactersNearby) {
            if ((bool)IsVisibleThreat(team, creaturePosition, character.transform) && (filter?.Invoke(character) != false))
                if (character.GetComponent<Creature>()?.brainConfig?.hasAttack == true ||
                        character.GetComponent<PlayerCharacter>() != null) {
                    threats.Add(character.transform);
                    Debug.DrawLine(brain.transform.position, character.transform.position, Color.cyan, .25f);
            }
        }
        if (threats.Count == 0) return Optional<Transform>.Empty();
        return Optional.Of(brain.transform.Nearest(threats));
    }
}
