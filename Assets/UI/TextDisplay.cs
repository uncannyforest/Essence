using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public struct ExpandableInfo {
    public string miniText;
    public string fullText;
    public ExpandableInfo(string miniText, string fullText) {
        this.miniText = miniText;
        this.fullText = fullText;
    }
}

public class TextDisplay : MonoBehaviour {
    public float miniTextDuration = 3;
    public Text fullText;
    public Text miniText;
    public GameObject miniTextExpandIcon;
    public Color creatureName;
    public Color keyName;
    public string defaultText = "Paused";
    public string secondTip = "Informational messages will periodically display below. You can get more info on the message by clicking the message or pressing the <color=key>/</color> key.";
    public string thirdTip = "Navigate along the grid using the AWEF keys. You can change this to WASD with the button in the lower left.";
    public string expandableText = "You pressed <color=key>/</color>.  This will normally expand the last message.";

    private bool isFullTextUp = true;
    public bool IsFullTextUp {
        get => isFullTextUp;
    }

    private HashSet<string> tutorialDisplayed = new HashSet<string>();

    public static TextDisplay I;

    void Awake() {
        I = this;
    }

    void Start() {
        Time.timeScale = 0;
        fullText.transform.parent.gameObject.SetActive(true);
    }

    public void ShowTip(ExpandableInfo info) {
        expandableText = info.fullText;
        ShowMiniText(info.miniText);
        miniTextExpandIcon.SetActive(true);
    }

    public void ToggleExpandedInfo() {
        if (IsFullTextUp) HideFullText();
        else {
            ShowFullText(expandableText);
            HideMiniText();
        }
    }

    public void ShowMiniText(string content) {
        miniText.text = Colorize(content);
        miniTextExpandIcon.SetActive(false);
        miniText.transform.parent.gameObject.SetActive(true);
        CancelInvoke();
        Invoke("HideMiniText", miniTextDuration);
    }

    public void HideMiniText() {
        miniText.transform.parent.gameObject.SetActive(false);
    }

    public void ToggleFullText() {
        if (IsFullTextUp) HideFullText();
        else ShowFullText(defaultText);
    }

    public void ShowFullText(string content) {
        fullText.text = Colorize(content);
        fullText.transform.parent.gameObject.SetActive(true);
        Time.timeScale = 0;
        isFullTextUp = true;
    }

    public void HideFullText() {
        if (!DisplayedYet("third tip")) {
            if (!DisplayedYet("second tip")) {
                CheckpointInfo("second tip", secondTip);
                miniText.transform.parent.gameObject.SetActive(true);
                return;
            } else {
                HideMiniText();
                CheckpointInfo("third tip", thirdTip);
                return;
            }
        }
        fullText.transform.parent.gameObject.SetActive(false);
        Time.timeScale = 1;
        isFullTextUp = false;
    }

    public void CheckpointInfo(string key, string content) {
        tutorialDisplayed.Add(key);
        ShowFullText(content);
    }

    public bool DisplayedYet(string key) => tutorialDisplayed.Contains(key);

    public string Colorize(string input) {
        return input
            .Replace("=creature", "=#" + ColorUtility.ToHtmlStringRGB(creatureName))
            .Replace("=key", "=#" + ColorUtility.ToHtmlStringRGB(keyName));
    }
}
