using UnityEngine;

public class GadgetLibrary : MonoBehaviour {
    private static GadgetLibrary instance;
    public static GadgetLibrary P {
        get => instance;
    }
    void Awake() { if (instance == null) instance = this; }

    public Gadget fountain;
}
