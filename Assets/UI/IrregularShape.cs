using UnityEngine;
using UnityEngine.UI;

// Used to allow clicks to pass through transparent areas.
[RequireComponent(typeof(Image))]
public class IrregularShape : MonoBehaviour {
    void Start() {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.5f;
    }
}
