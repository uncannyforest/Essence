using System;
using UnityEngine;

// This class was originally created to handle the player's ability to select tiles OR characters.
// Independently, creature AI code has a "focus" which can be either a tile OR a character.
// That was originally separately handled by fields terrainFocus and characterFocus in CreatureState,
// but I am increasingly finding it more useful to have a single "terrain or character" class
// and am thus using this one for that in some code.
// Unfortunately, there are type discrepancies:
// here, terrain is Terrain.Position; in CreatureState, it is DesireMessage.Obstacle (I think that is superior*)
// here, character is Character; in CreatureState, it is Transform (I am undecided which is better).
// See TargetedBehavior.cs which uses Target to flexibly handle player-assigned targets and creature-found focuses the same.
//
// *DesireMessage.Obstacle has built-in checks for whether the intended terraforming is no longer possible.
public class Target : OneOf<Terrain.Position, Character> {
    private Target() : base() {}
    public Target(Terrain.Position t) : base(t) {}
    public Target(Character u) : base(u) {}
    new public static Target Neither { get => new Target(); }

    public Vector3 Position {
        get {
            if (this.Is(out Terrain.Position position))
                return Terrain.I.CellCenter(position);
            else if (this.Is(out Character character))
                return character.transform.position;
            else throw new InvalidOperationException("Neither");
        }
    }

    public WhyNot IfCharacter(Func<Character, WhyNot> check) {
        if (Is(out Character c)) return check(c);
        else return true;
    }

    public WhyNot IfTerrain(Func<Terrain.Position, WhyNot> check) {
        if (Is(out Terrain.Position p)) return check(p);
        else return true;
    }

    // My code is inconsistent whether a non-terrain target can be any Unity object (thus having a Transform)
    // or must be one with a Character component attached.  On the one hand, I see no reason that
    // associated code could not be used on any Transform; on the other, it feels clean to have a component
    // strictly indicating This Can Be A Target, which is the main reason I created Character, a very small class.
    // Unforunately I neglected to migrate all usages of Transform to Character, and now I am indecisive
    // as to which would be better.  I kicking the can down the road.  For now, some code uses the Transform type
    // but may assume a Character component is present: it is at the below line that that code will break
    // if an intended Target does not have a Character component attached.
    public static Target Character(Transform t) => new Target(t.GetComponentStrict<Character>());
}
