using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// grouping together 3D character highlight functions
public class CharacterHighlight3D {

    public static MeshRenderer New(Character character, bool hover) {
        MeshRenderer result = AddSelect(
            character.transform,
            WorldInteraction.I.characterSelectPrefab,
            -GlobalConfig.I.elevation.groundLevelHighlight);
        if (!hover) result.material.color = WorldInteraction.I.followingCharacterColor;
        return result;
    }

    public static MeshRenderer AddSelect(Transform parent, MeshRenderer prefab, float z) {
        MeshRenderer result = GameObject.Instantiate(prefab, parent);
        result.transform.localPosition = new Vector3(0, 0, z);
        return result;
    }

    public static void SetHighlightHoverToFollowing(MeshRenderer highlight) {
        highlight.material.color = WorldInteraction.I.followingCharacterColor;
    }
}
