using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Creature))]
public class Stats : MonoBehaviour {
    [SerializeField] private int currentExp;
    public int minDistanceFromOrigin;

    public int Exp {
        get => currentExp;
        set {
            if (value < currentExp)
                throw new ArgumentException("Cannot decrease Exp (" + currentExp + " -> " + value + ")");
            if (value > currentExp)// + 2)
                Debug.Log(gameObject.name + " just gained " + (value - currentExp) + " EXP, reaching " + value + " EXP");
            currentExp = value;
            bool justLeveledUp = currentExp == LevelToExp(Level);
            if (justLeveledUp) OnLevelUp();
        }
    }
    public int Level { get => Mathf.FloorToInt(Mathf.Sqrt(currentExp / 10)); }

    private Creature creature;

    void Start() {
        creature = GetComponent<Creature>();
        InitializeStats();
    }

    private void InitializeStats() {
        gameObject.name = GenerateName() + " " + creature.creatureShortName;
        int initLevel = GetInitLevel(Terrain.I.CellAt(transform.position));
        currentExp = LevelToExp(initLevel);
    }

    public int GetInitLevel(Vector2Int position) =>
        Mathf.Max(0, (position.sqrMagnitude - minDistanceFromOrigin * minDistanceFromOrigin) / 20736) + 1;
    public int LevelToExp(int level) => level * level * 10;

    private void OnLevelUp() {
        Debug.Log(gameObject.name + " just reached level " + Level);
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
