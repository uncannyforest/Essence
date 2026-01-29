using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class Stats : MonoBehaviour {
    [SerializeField] private int currentExp;
    public int minDistanceFromOrigin;
    public float minStr = 2;
    public float minDef = 10;
    public float minExe = 2;
    public float exeIncrEvery = 10;
    public float minSpd = 1f;
    public float minRes = 10;

    public int Exp {
        get => currentExp;
        set => SetExp(value);
    }
    public int Level { get => Mathf.FloorToInt(Mathf.Sqrt(currentExp / GlobalConfig.I.expToLevelUp)); }
    private int lastLevel = 1;
    public int Str { get => Mathf.FloorToInt(minStr * Level); }
    public int Def { get => Mathf.FloorToInt(minDef * Level); }
    public float Exe { get => minExe + Mathf.Log(Level, exeIncrEvery); }
    public float ExeTime { get => 1 / Exe; }
    public float Spd { get => minSpd + Mathf.Log(Level, 1000); }
    public int Res { get => Mathf.FloorToInt(minRes * Level); }
    public int NextLevelIn { get => LevelToExp(Level + 1) - Exp; }

    public Action<Stats> LeveledUp;

    void Start() {
        InitializeDisplay();
        if (currentExp == 0) InitializeStats(); // if not deserialized from save
        else gameObject.GetComponentInChildren<TextMesh>().text = "" + Level;
    }

    virtual protected void InitializeDisplay() {
        GameObject levelDisplay = GameObject.Instantiate(GeneralAssetLibrary.P.levelDisplay, transform);
    }

    private void InitializeStats() {
        int initLevel = GetInitLevel(Terrain.I.CellAt(transform.position));
        Debug.Log("Setting currentExp for new creature " + gameObject.name);
        InitializeExp(LevelToExp(initLevel));
    }

    public int GetInitLevel(Vector2Int position) {
        Displacement distanceFromOrigin = Disp.FT(GameManager.I.Origin, position);
        return Mathf.Max(0,
            ((int)distanceFromOrigin.sqrMagnitude - minDistanceFromOrigin * minDistanceFromOrigin) /
            (GlobalConfig.I.creatureStartLevelDistance * GlobalConfig.I.creatureStartLevelDistance))
            + 1;
    }

    public int LevelToExp(int level) => level * level * GlobalConfig.I.expToLevelUp;

    // for initialization situations where you're not just incrementing
    public void InitializeExp(int exp) {
        currentExp = exp;
        OnLevelUp(false);
    }

    virtual public void SetExp(int value) {
        if (value < currentExp)
            throw new ArgumentException("Cannot decrease Exp (" + currentExp + " -> " + value + ")");
        currentExp = value;
        bool justLeveledUp = lastLevel != Level;
        if (justLeveledUp) OnLevelUp(true);
    }

    private void OnLevelUp(bool displayMessage) {
        lastLevel = Level;
        if (displayMessage && GameManager.I.YourTeam.SameTeam(this.GetComponentStrict<Creature>())) TextDisplay.I.ShowMiniText(gameObject.name + " just reached level " + Level + "!");
        if (LeveledUp != null) LeveledUp(this);
        TextMesh levelDisplay = gameObject.GetComponentInChildren<TextMesh>();
        if (levelDisplay != null) levelDisplay.text = "" + Level;
    }
}
