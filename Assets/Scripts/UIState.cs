using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Not used any more!  Delete soon
public class UIState : MonoBehaviour {
    private Text text;

    private string tool = "Praxel";
    private int numScales = 0;
    private int numSoil = 0;

    public WorldInteraction.Mode Tool {
        set {
            tool = value.ToString();
            UpdateText();
        }
    }

    public void SetScales(int value) {
            numScales = value;
            UpdateText();
    }

    public void SetSoil(int value)  {
            numSoil = value;
            UpdateText();
    }

    void Start() {
        text = GetComponent<Text>();
        UpdateText();
    }

    private void UpdateText() {
        text.text = tool + "\nScale: " + numScales + "\nSoil: " + numSoil;
    }


}
