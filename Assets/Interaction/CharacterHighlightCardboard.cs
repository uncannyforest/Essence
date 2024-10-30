using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// DEPRECATED BEFORE RELEASE :/
// grouping together 2.5D character highlight functions
public class CharacterHighlightCardboard {

    // public static Cardboard New(Character character, bool hover) {
    //     Cardboard result = AddSelect(
    //         character.transform,
    //         character.broadGirth ? WorldInteraction.I.largeCharacterSelectPrefab_new : WorldInteraction.I.smallCharacterSelectPrefab_new,
    //         GlobalConfig.I.elevation.groundLevelHighlight);
    //     if (!hover) result.GetComponentInChildren<SpriteRenderer>().color = WorldInteraction.I.followingCharacterColor;
    //     return result;
    // }

    public static Cardboard AddSelect(Transform parent, Cardboard prefab, float z) {
        Cardboard result = GameObject.Instantiate(prefab, parent);
        result.transform.localPosition = new Vector3(0, 0, z);
        return result;
    }

    public static void SetHighlightHoverToFollowing(GameObject highlight) {
        highlight.GetComponentInChildren<SpriteRenderer>().color = WorldInteraction.I.followingCharacterColor;
    }
}
