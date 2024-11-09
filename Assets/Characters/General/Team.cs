using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour {    
    [SerializeField] private int teamId; // 0 to 6; 0 means neutral

    public int TeamId {
        get => teamId;
        set {
            teamId = value;
            if (changed != null) changed(value);
        }
    }

    public Action<int> changed;

    public Color Color {
        get => new Color(1/16f, 1/2f, 1);
    }

    public void OnAttack(Transform assailant) {
        if (assailant == null) Debug.Log("Attacked by the terrain itself");
        else Broadcast(new DesireMessage() { assailant = Optional.Of(assailant) });
    }

    public int Broadcast(DesireMessage desireMessage) {
        int count = 0;
        foreach (Creature creature in BroadcastAudience())
            if (creature.ReceiveDesireMessage(desireMessage))
                count++;   
        return count;
    }

    private IEnumerable<Creature> BroadcastAudience() {
        Collider2D[] nearbyCreatures = Physics2D.OverlapCircleAll(transform.position, Creature.neighborhood, LayerMask.GetMask("Creature", "HealthCreature"));
        return (from creature in nearbyCreatures
            where SameTeam(creature)
            select creature.GetComponentStrict<Creature>()).AsEnumerable();
    }

    public bool SameTeam(GameObject other) => teamId == other.GetComponentStrict<Team>().teamId;
    public bool SameTeam(Collider2D other) => teamId == other.GetComponentStrict<Team>().teamId;
    public bool SameTeam(Transform other) => teamId == other.GetComponentStrict<Team>().teamId;
    public bool SameTeam(MonoBehaviour other) => teamId == other.GetComponentStrict<Team>().teamId;
    public static bool SameTeam(int me, GameObject other) => me == other.GetComponentStrict<Team>().teamId;
    public static bool SameTeam(int me, Collider2D other) => me == other.GetComponentStrict<Team>().teamId;
    public static bool SameTeam(int me, Transform other) => me == other.GetComponentStrict<Team>().teamId;
    public static bool SameTeam(int me, MonoBehaviour other) => me == other.GetComponentStrict<Team>().teamId;

}
