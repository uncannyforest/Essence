using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// grouping together 2D character highlight functions
public class CharacterHighlight2D {

    public static SortingGroup New(Character character, bool hover) {
        SortingGroup result = AddGroup(
            character.spriteSorter,
            character.broadGirth ? WorldInteraction.I.largeCharacterSelectPrefab.GetComponent<SortingGroup>() : WorldInteraction.I.smallCharacterSelectPrefab.GetComponent<SortingGroup>(),
            GlobalConfig.I.elevation.groundLevelHighlight);
        if (!hover) result.GetComponentInChildren<SpriteRenderer>().color = WorldInteraction.I.followingCharacterColor;
        return result;
    }

    public static SortingGroup AddGroup(SpriteSorter spriteSorter, SortingGroup prefab, float z) {
        SortingGroup result = GameObject.Instantiate(prefab, spriteSorter.transform);
        result.transform.localPosition = new Vector3(0, 0, z);
        return result;
    }

    public static void SetHighlightHoverToFollowing(GameObject highlight) {
        highlight.GetComponentInChildren<SpriteRenderer>().color = WorldInteraction.I.followingCharacterColor;
    }
}
