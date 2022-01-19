using System;
using UnityEngine;

[RequireComponent(typeof(Creature))]
[RequireComponent(typeof(Team))]
public class GoodTaste : StatusQuantity {
    public float timeToTame;
    public string insufficientTimeInfo = "Whistle song (press & hold) to tame";

    private Transform tamer;
    private Action Tamed;
    private string error = null;

    override protected void Awake() {
        base.Awake();
        ReachedMax += HandleTamed;
    }

    public void StartTaming(Transform tamer, Action tamedHandler) {
        this.tamer = tamer;
        this.Tamed = tamedHandler;
    }

    public string StopTaming(Transform tamer) {
        if (this.tamer != tamer) return null;
        if (tamer != null) {
            this.tamer = null;
            Reset();
            return insufficientTimeInfo;
        } else {
            string result = error;
            error = null;
            return result;
        }
    }

    public void HandleTamed() {
        if (GetComponent<Creature>().TryTame(tamer)) {
            if (Tamed != null) Tamed();
        } else error = GetComponent<Creature>().TamingInfo;
        this.tamer = null;
        Reset();
    }
    
    void Update() {
        if (tamer != null) {
            Debug.Log("HEY!" + max * Time.deltaTime / timeToTame);
            GetComponent<Team>()?.OnAttack(tamer);
            Increase((int)(max * Time.fixedDeltaTime / timeToTame));
        }
    }
}
