using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextDisplay : MonoBehaviour {
    public Text fullText;
    public string defaultText = "Paused";

    private bool isFullTextUp = true;
    public bool IsFullTextUp {
        get => isFullTextUp;
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
}
