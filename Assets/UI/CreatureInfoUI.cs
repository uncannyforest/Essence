using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreatureInfoUI : MonoBehaviour {
    private Text creatureName;
    private Text species;
    private Text levelExp;
    private Text str;
    private Text def;
    private Text exe;
    private Text spd;
    private Text res;

    void Awake() {
        creatureName = transform.Find("Info/Line 1/Name").GetComponentStrict<Text>();
        species = transform.Find("Info/Line 1/Species").GetComponentStrict<Text>();
        levelExp = transform.Find("Info/Level & EXP").GetComponentStrict<Text>();
        str = transform.Find("Info/Table/STR").GetComponentStrict<Text>();
        def = transform.Find("Info/Table/DEF").GetComponentStrict<Text>();
        exe = transform.Find("Info/Table/EXE").GetComponentStrict<Text>();
        spd = transform.Find("Info/Table/SPD").GetComponentStrict<Text>();
        res = transform.Find("Info/Table/RES").GetComponentStrict<Text>();
    }

    void OnEnable() {
        Character character = WorldInteraction.I.PeekFollowingCharacter();
        if (character == null) {
            Debug.LogWarning("How did we open creature UI with no creature?");
            return;
        }
        Creature creature = character.GetComponentStrict<Creature>();
        creatureName.text = "Name: " + creature.gameObject.name;
        species.text = "Type: " + creature.creatureName;
        Stats stats = creature.stats;
        levelExp.text = "LVL " + stats.Level + " (" + stats.Exp + " EXP / next level in " + stats.NextLevelIn + " EXP)";
        str.text = "STR: " + stats.Str;
        def.text = "DEF: " + stats.Def;
        exe.text = "EXE: " + stats.Exe.ToString("0.0");
        spd.text = "SPD: " + stats.Spd.ToString("0.0");
        res.text = "CAP: " + stats.Res;
    }
}
