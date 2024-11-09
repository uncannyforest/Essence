using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Damage : MonoBehaviour {
    public int power;
    public int safeTeam; // don't damage this team
    public Transform blame;

    public Team Source {
        set {
            safeTeam = value.TeamId;
            blame = value.transform;
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.isTrigger) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain")) {
            Destroy(gameObject);
            return;
        }
        Health target = other.GetComponent<Health>();
        if (target == null) return;
        int targetTeam = target.GetComponentStrict<Team>().TeamId;
        if (targetTeam == safeTeam) return;
        target.Decrease(power, blame);
        Destroy(gameObject);
    }
}
