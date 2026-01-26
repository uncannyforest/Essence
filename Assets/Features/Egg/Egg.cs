using System.Linq;
using UnityEngine;

[RequireComponent(typeof(FeatureHooks))]
[RequireComponent(typeof(Team))]
public class Egg : MonoBehaviour {
    public float baseHatchTime = 120;
    public MeshRenderer egg;
    public MeshRenderer nest;
    [SerializeField] private string species;

    public string Species {
        get => species;
        set {
            species = value;
        }
    }

    private FeatureHooks feature;
    private Team team;

    void Start() {
        feature = GetComponent<FeatureHooks>();
        team = GetComponent<Team>();
        if (feature.serializedFields != null) Deserialize(feature.serializedFields);
        feature.SerializeFields += Serialize;
        feature.GetResourceName = GetResourceName;
        this.Invoke(Hatch, baseHatchTime * Random.Range(.5f, 1));
        TeamChangedEventHandler(team.TeamId);
        team.changed += TeamChangedEventHandler;
    }

    int[] Serialize() => new int[] { team.TeamId }.Concat(FeatureHooks.SerializeString(species)).ToArray();
    void Deserialize(int[] fields) {
        team.TeamId = fields[0];
        species = FeatureHooks.DeserializeString(fields, 1);
    }

    void TeamChangedEventHandler(int value) {
        nest.material.color = team.Color;
    }

    string GetResourceName() => species + " egg";

    private void Hatch() {
        Creature prefab = CreatureLibrary.P.BySpeciesName(species);
        Creature newCreature = Instantiate(prefab, transform.position, Quaternion.identity, GameManager.I.worldBag);
        newCreature.GetComponentStrict<Team>().TeamId = team.TeamId;
        newCreature.GetComponentStrict<Stats>().SetExp(GlobalConfig.I.expToLevelUp);
        if (team.SameTeam(GameManager.I.YourTeam)) {
            TextDisplay.I.ShowMiniText(species + " hatched!");
        }
        Terrain.I.DestroyFeature((Vector2Int)GetComponent<FeatureHooks>().tile);
    }
}
