using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class NavigationControls : MonoBehaviour {
    public InputManager inputManager;
    public Sprite AWEF;
    public Sprite WASD;

    public void OnButtonPress() {
        inputManager.useWASD = !inputManager.useWASD;
        GetComponent<Image>().sprite = inputManager.useWASD ? WASD : AWEF;
    }
}
