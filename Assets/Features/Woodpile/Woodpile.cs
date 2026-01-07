using UnityEngine;

[RequireComponent(typeof(FeatureHooks))]
public class Woodpile : MonoBehaviour {
    public int maxPile = 10;
    public Transform planksParent;
    public Transform plankPrefab;
    public float gravityForce = 9.8f;
    public float animationFreezeBelowMotionSquared = .0001f;

    private int quantity = 1;

    void Start() {
        FeatureHooks featureHooks = GetComponent<FeatureHooks>();
        if (featureHooks.serializedFields != null) Deserialize(featureHooks.serializedFields);
        featureHooks.SerializeFields += Serialize;
        featureHooks.GetResourceQuantity = GetResourceQuantity;
        featureHooks.SetResourceQuantity = SetResourceQuantity;
    }

    int[] Serialize() => new int[] { quantity };
    void Deserialize(int[] fields) { quantity = fields[0]; }

    private int? GetResourceQuantity() => quantity;
    private bool SetResourceQuantity(int value) {
        if (value <= 0 || value > maxPile) return false;
        quantity = value;
        return true;
    }

    // rendering

    public bool visualizing = true;

    void FixedUpdate() {
        if (!visualizing && planksParent.childCount != quantity) {
            visualizing = true;
            foreach (Transform plank in planksParent) {
                plank.GetComponentStrict<Rigidbody>().isKinematic = false;
            }
        }
        if (!visualizing) return;

        if (planksParent.childCount > quantity) {
            for (int i = planksParent.childCount - 1; i >= quantity; i--) {
                GameObject.Destroy(planksParent.GetChild(i).gameObject);
            }
        } else if (planksParent.childCount < quantity) {
            Vector3 pos = transform.position;
            while (planksParent.childCount < quantity) {
                pos += Vector3.back / 2;
                GameObject.Instantiate(plankPrefab, pos, Random.rotation, planksParent);
            }
        }

        bool doneAnimating = false;
        foreach (Transform plank in planksParent) {
            Vector3 pos = plank.position;
            plank.GetComponentStrict<Rigidbody>().AddForce(Vector3.forward * gravityForce);
            if (Disp.FT(pos, plank.position).sqrMagnitude > animationFreezeBelowMotionSquared) {
                doneAnimating = false;
            }
        }
        if (doneAnimating) {
            visualizing = false;
            foreach (Transform plank in planksParent) {
                plank.GetComponentStrict<Rigidbody>().isKinematic = true;
            }
        }
    }
}
