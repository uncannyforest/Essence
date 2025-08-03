using System.Text.RegularExpressions;
using UnityEngine;

public class CreatureLibrary : MonoBehaviour {
    private static CreatureLibrary instance;
    public static CreatureLibrary P {
        get => instance;
    }
    void Awake() {
        if (instance == null) instance = this;
    }

    // input format: "Red Dwarf" or "red dwarf"
    // convert string to: "redDwarf"
    public Creature BySpeciesName(string species) {
        string afterSpaceUpperCase = Regex.Replace(species, " .", m => m.ToString().ToUpper());
        string removeSpaces = Regex.Replace(afterSpaceUpperCase, " ", "");
        string firstLetterLowerCase = Regex.Replace(removeSpaces, "^.", m => m.ToString().ToLower());
        Debug.Log("Loading creature:" + firstLetterLowerCase);
        return (Creature)this.GetType().GetField(firstLetterLowerCase).GetValue(this);
    }

    public Creature bunny;
    public Creature arrowwiggle;
    public Creature stipule;
    public Creature archer;
    public Creature redDwarf;
    public Creature moose;
    public Creature axe;
}
