using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Stats {
    public Stats(Creature creature) {
        creature.gameObject.name = GenerateName() + " " + creature.creatureShortName;
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
