using UnityEngine;

public class GeneralAssetLibrary : MonoBehaviour {
    private static GeneralAssetLibrary instance;
    public static GeneralAssetLibrary P {
        get => instance;
    }
    void Awake() { if (instance == null) instance = this; }
    
    public UnityEngine.Material spriteDefault;
    public UnityEngine.Material spriteSolidColor;
    public GameObject levelDisplay;
}
