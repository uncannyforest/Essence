using UnityEngine;

public class PixelPerfectContentFitter : MonoBehaviour {
    public bool roundPositionUpOnScreen = false;

    private float cachedHeight;

    void Update() {
        float height = ((RectTransform)transform).rect.height;
        if (height != cachedHeight) {
            cachedHeight = height;
            if (Mathf.RoundToInt(height) % 2 != 0) {
                if (roundPositionUpOnScreen) ((RectTransform)transform).anchoredPosition = new Vector2(0, .5f);
                else ((RectTransform)transform).anchoredPosition = new Vector2(0, -.5f);
            } else ((RectTransform)transform).anchoredPosition = new Vector2(0, 0);
        }
    }
}
