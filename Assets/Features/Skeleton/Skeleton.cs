using UnityEngine;

[RequireComponent(typeof(FeatureHooks))]
public class Skeleton : MonoBehaviour {
    public float gravityForce = 9.8f;
    public float timeFallTowardsCellCenter = 1;
    public float animationFreezeBelowMotionSquared = .0001f;

    // rendering

    public bool visualizing = false;

    public void Initialize(GameObject prefab, Vector3 position, Quaternion rotation) {
        GameObject.Instantiate(prefab, position, rotation, transform);
        Displacement towardsCellCenter = -Terrain.I.PositionInCell(position);
        Debug.Log("Skeleton facing: " + rotation.eulerAngles + " falling: " + towardsCellCenter);
        foreach (Rigidbody bone in transform.GetComponentsInChildren<Rigidbody>()) {
            bone.AddForce(towardsCellCenter.ToVelocityGivenTime(timeFallTowardsCellCenter), ForceMode.VelocityChange);
        }
        visualizing = true;
    }

    void FixedUpdate() {
        if (!visualizing) return;

        bool doneAnimating = false;
        foreach (Rigidbody bone in transform.GetComponentsInChildren<Rigidbody>()) {
            Vector3 pos = bone.transform.position;
            bone.AddForce(Vector3.forward * gravityForce, ForceMode.Acceleration);
            if (Disp.FT(pos, bone.transform.position).sqrMagnitude > animationFreezeBelowMotionSquared) {
                doneAnimating = false;
            }
        }
        if (doneAnimating) {
            visualizing = false;
            foreach (Rigidbody bone in transform.GetComponentsInChildren<Rigidbody>()) {
                bone.isKinematic = true;
            }
        }
    }
}
