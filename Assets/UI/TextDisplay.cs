using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextDisplay : MonoBehaviour {
    public Text fullText;
    public string defaultText = "Paused";
    public string secondTip = "Navigate along the grid using the AWEF keys. You can change this to WASD with the button in the lower left.";

    private bool isFullTextUp = true;
    public bool IsFullTextUp {
        get => isFullTextUp;
    }

    private HashSet<string> infoDisplayed = new HashSet<string>();

    public static TextDisplay I;

    void Awake() {
        I = this;
    }

    void Start() {
        Time.timeScale = 0;
        fullText.transform.parent.gameObject.SetActive(true);
    }

    public void ToggleFullText() {
        if (IsFullTextUp) HideFullText();
        else ShowFullText(defaultText);
    }

    public void HideFullText() {
        if (!DisplayedYet("second tip")) { // Also how to use DisplayedYet and CheckpointInfo in other classes.
            CheckpointInfo("second tip", secondTip);
            return;
        }
        fullText.transform.parent.gameObject.SetActive(false);
        Time.timeScale = 1;
        isFullTextUp = false;
    }

    public void ShowFullText(string content) {
        fullText.text = content;
        fullText.transform.parent.gameObject.SetActive(true);
        Time.timeScale = 0;
        isFullTextUp = true;
    }

    public void CheckpointInfo(string key, string content) {
        infoDisplayed.Add(key);
        ShowFullText(content);
    }

    public bool DisplayedYet(string key) => infoDisplayed.Contains(key);
}
