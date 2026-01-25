using System.Linq;
using UnityEngine;

[RequireComponent(typeof(FeatureHooks))]
public class Egg : MonoBehaviour {
    public MeshRenderer egg;
    public MeshRenderer nest;
    [SerializeField] public string species;
    [SerializeField] private int team;

    public int Team {
        get => team;
        set {
            team = value;
            if (value == 0) nest.material.color = Color.gray;
            else nest.material.color = new Color(1/16f, 1f, 7/8f);
        }
    }

    private FeatureHooks feature;

    void Start() {
        feature = GetComponent<FeatureHooks>();
        if (feature.serializedFields != null) Deserialize(feature.serializedFields);
        feature.SerializeFields += Serialize;
    }

    int[] Serialize() => new int[] { team }.Concat(FeatureHooks.SerializeString(species)).ToArray();
    void Deserialize(int[] fields) {
        Team = fields[0];
        species = FeatureHooks.DeserializeString(fields, 1);
    }
}
