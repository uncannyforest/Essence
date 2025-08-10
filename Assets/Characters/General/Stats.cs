using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

[RequireComponent(typeof(Creature))]
public class Stats : MonoBehaviour {
    [SerializeField] private int currentExp;
    public int minDistanceFromOrigin;
    public float minStr = 3;
    public float minDef = 25;
    public float minExe = 2;
    public float exeIncrEvery = 10;
    public float minSpd = 1f;
    public float minRes = 10;

    public int Exp {
        get => currentExp;
        set {
            if (value < currentExp)
                throw new ArgumentException("Cannot decrease Exp (" + currentExp + " -> " + value + ")");
            if (value > currentExp + 2)
                Debug.Log(gameObject.name + " just gained " + (value - currentExp) + " EXP, reaching " + value + " EXP");
            currentExp = value;
            bool justLeveledUp = currentExp == LevelToExp(Level);
            if (justLeveledUp) OnLevelUp(true);
        }
    }
    public int Level { get => Mathf.FloorToInt(Mathf.Sqrt(currentExp / GlobalConfig.I.expToLevelUp)); }
    public int Str { get => Mathf.FloorToInt(minStr * Level); }
    public int Def { get => Mathf.FloorToInt(minDef * Level); }
    public float Exe { get => minExe + Mathf.Log(Level, exeIncrEvery); }
    public float ExeTime { get => 1 / Exe; }
    public float Spd { get => minSpd + Mathf.Log(Level, 1000); }
    public int Res { get => Mathf.FloorToInt(minRes * Level); }
    public int NextLevelIn { get => LevelToExp(Level + 1) - Exp; }

    public Action<Stats> LeveledUp;

    private Creature creature;

    void Start() {
        creature = GetComponent<Creature>();
        GameObject levelDisplay = GameObject.Instantiate(GeneralAssetLibrary.P.levelDisplay, transform);
        if (currentExp == 0) InitializeStats(); // if not deserialized from save
        else gameObject.GetComponentInChildren<TextMesh>().text = "" + Level;
    }

    private void InitializeStats() {
        gameObject.name = GenerateName() + " " + creature.creatureShortName;
        int initLevel = GetInitLevel(Terrain.I.CellAt(transform.position));
        Debug.Log("Setting currentExp for new creature " + gameObject.name);
        SetExp(LevelToExp(initLevel));
    }

    public int GetInitLevel(Vector2Int position) {
        Displacement distanceFromOrigin = Disp.FT(new Vector2Int(Terrain.Dim, Terrain.Dim) / 2, position);
        return Mathf.Max(0,
            ((int)distanceFromOrigin.sqrMagnitude - minDistanceFromOrigin * minDistanceFromOrigin) /
            (GlobalConfig.I.creatureStartLevelDistance * GlobalConfig.I.creatureStartLevelDistance))
            + 1;
    }
    public int LevelToExp(int level) => level * level * GlobalConfig.I.expToLevelUp;

    // for initialization situations where you're not just incrementing
    public void SetExp(int exp) {
        currentExp = exp;
        OnLevelUp(false);
    }

    private void OnLevelUp(bool displayMessage) {
        if (displayMessage && GameManager.I.YourTeam.SameTeam(creature)) TextDisplay.I.ShowMiniText(gameObject.name + " just reached level " + Level + "!");
        if (LeveledUp != null) LeveledUp(this);
        TextMesh levelDisplay = gameObject.GetComponentInChildren<TextMesh>();
        if (levelDisplay != null) levelDisplay.text = "" + Level;
    }

    public static string GenerateName() {
        char[] consonants = "bcdfghjklmnpqrstvwxyz".ToCharArray();
        char[] vowels = "aeiou".ToCharArray();
        if (Randoms.CoinFlip) {
            return (char)(Randoms.InArray(consonants) - 32) + ""
                + Randoms.InArray(vowels)
                + Randoms.InArray(consonants)
                + Randoms.InArray(vowels)
                + Randoms.InArray(consonants);
        } else {
            switch (Random.Range(0, 4)) {
                case 0:
                    return (char)(Randoms.InArray(consonants) - 32) + ""
                        + Randoms.InArray(vowels)
                        + Randoms.InArray(consonants)
                        + Randoms.InArray(vowels);
                case 1:
                    return (char)(Randoms.InArray(vowels) - 32) + ""
                        + Randoms.InArray(consonants)
                        + Randoms.InArray(vowels)
                        + Randoms.InArray(consonants);
                case 2: 
                    return (char)(Randoms.InArray(consonants) - 32) + ""
                        + Randoms.InArray(vowels)
                        + Randoms.InArray(vowels)
                        + Randoms.InArray(consonants);
                default:
                    return (char)(Randoms.InArray(vowels) - 32) + ""
                        + Randoms.InArray(consonants)
                        + Randoms.InArray(consonants)
                        + Randoms.InArray(vowels);
            }
        }
    }
}
