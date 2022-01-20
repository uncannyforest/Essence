using System;
using UnityEngine;

[RequireComponent(typeof(Creature))]
[RequireComponent(typeof(Team))]
public class GoodTaste : StatusQuantity {
    public float timeToTame;
    public string insufficientTimeInfoShort = "Whistle song (press and hold) to tame";
    public string insufficientTimeInfoLong = "The <creature/> will not be tamed unless you whistle a song for sufficient time.  Press and hold the left mouse button to whistle a song.  A blue stat bar will appear beneath the <creature/> to indicate it can hear you; when the stat bar reaches the end the <creature/> will be tamed.";

    private Transform tamer;
    private Action Tamed;
    private ExpandableInfo? error = null;

    override protected void Awake() {
        base.Awake();
        ReachedMax += HandleTamed;
    }

    public void StartTaming(Transform tamer, Action tamedHandler) {
        this.tamer = tamer;
        this.Tamed = tamedHandler;
    }

    public ExpandableInfo? StopTaming(Transform tamer) {
        if (tamer != null) {
            if (this.tamer != tamer) return null;
            this.tamer = null;
            Reset();
            return Creature.GenerateTamingInfo(gameObject, insufficientTimeInfoShort, insufficientTimeInfoLong);
        } else {
            ExpandableInfo? result = error;
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
