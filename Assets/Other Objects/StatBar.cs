using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatBar {
    public readonly GameObject gameObject;
    public readonly SpriteRenderer level;
    public readonly SpriteRenderer background;
    public readonly MonoBehaviour parentComponent;
    
    private const float visibilityTime = 2f;

    private StatBar(GameObject statBar, MonoBehaviour parentComponent) {
        this.gameObject = statBar;
        this.parentComponent = parentComponent;
        this.level = statBar.transform.Find("Level").GetComponent<SpriteRenderer>();
        this.background = statBar.transform.Find("Background").GetComponent<SpriteRenderer>();
    }

    public static StatBar Instantiate(GameObject prefab, MonoBehaviour parentComponent, Color color) {
        GameObject gameObject = GameObject.Instantiate(prefab, parentComponent.transform);
        StatBar statBar = new StatBar(gameObject, parentComponent);
        statBar.level.color = color;
        statBar.SetPercentWithoutVisibility(1f);
        statBar.Hide();
        return statBar;
    }

    public void SetPercent(float percent) {
        SetPercentWithoutVisibility(percent);
        Show();
        parentComponent.CancelInvoke();
        parentComponent.Invoke("HideStatBar", visibilityTime);
    }

    public void SetPercentWithoutVisibility(float percent) {
        Vector3 oldScale = level.transform.localScale;

        level.transform.localScale = new Vector3(percent / 2f, oldScale.y, oldScale.z);
    }

    private void Show() {
        level.enabled = true;
        background.enabled = true;
    }

    public void Hide() {
        level.enabled = false;
        background.enabled = false;
    }
}
