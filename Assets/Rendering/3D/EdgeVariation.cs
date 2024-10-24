using UnityEngine;
using System;

[Serializable] public class EdgeVariation {
    [NonSerialized] public Biome biome;

    public MeshRenderer xEdge;

    public Action<Transform> Render(bool rotate) => (Transform parent) => Instantiate(parent, rotate);

    protected void Instantiate(Transform parent, bool rotate = false) {
        GameObject go = GameObject.Instantiate(xEdge.gameObject, parent);
        if (rotate) {
            go.transform.localEulerAngles = new Vector3(0, 0, 90);
        } else {
            go.transform.localRotation = Quaternion.identity;
        }
        Renderer r = go.GetComponentStrict<Renderer>();
        r.material = biome.material;
    }
}
