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
        if (hover) AlsoShowStats(character);
        else result.material.color = WorldInteraction.I.followingCharacterColor;
        return result;
    }

    public static MeshRenderer AddSelect(Transform parent, MeshRenderer prefab, float z) {
        MeshRenderer result = GameObject.Instantiate(prefab, parent);
        result.transform.localPosition = new Vector3(0, 0, z);
        return result;
    }

    public static void SetHighlightHoverToFollowing(MeshRenderer highlight) {
        highlight.material.color = WorldInteraction.I.followingCharacterColor;
        AlsoHideStats(highlight);
    }

    public static void Clear(MeshRenderer highlight) {
        if (highlight != null) {
            AlsoHideStats(highlight);
            GameObject.Destroy(highlight.gameObject);
        }
    }

    private static void AlsoShowStats(Character character) {
        foreach (StatusQuantity sq in character.GetComponents<StatusQuantity>()) {
            if (sq.showOnHover) sq.ShowStatBar();
            else sq.HideStatBar();
        }
    }

    private static void AlsoHideStats(MeshRenderer highlight) {
        foreach (StatusQuantity sq in highlight.transform.parent.GetComponents<StatusQuantity>())
            if (sq.showOnHover)
                sq.HideStatBar();
    }
}
