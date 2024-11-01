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

    public Transform Tamer {
        get => tamer;
        set {
            Transform oldTamer = tamer;
            tamer = value;
            HandleTamerChanged(oldTamer);
        }
    }

    public bool Listening {
        get => Tamer != null;
    }

    override protected void Awake() {
        base.Awake();
        ReachedMax += HandleTamed;
    }

    public void StartTaming(Transform tamer, Action tamedHandler) {
        this.Tamer = tamer;
        this.Tamed = tamedHandler;
    }

    public ExpandableInfo? StopTaming(Transform tamer) {
        if (this.Tamer != null) {
            if (this.Tamer != tamer) return null;
            this.Tamer = null;
            Reset();
            return Creature.GenerateTamingInfo(gameObject, insufficientTimeInfoShort, insufficientTimeInfoLong);
        } else {
            ExpandableInfo? result = error;
            error = null;
            return result;
        }
    }

    public void HandleTamerChanged(Transform oldTamer) {
        if (Tamer != null) {
            CharacterController movement = GetComponent<Creature>().OverrideControl(this);
            movement.IdleFacing(Tamer.position);
        } else {
            GetComponent<Creature>().ReleaseControl();
            GetComponent<Creature>().Follow(oldTamer);
        }
    }

    public void HandleTamed() {
        if (GetComponent<Creature>().TryTame(Tamer)) {
            if (Tamed != null) Tamed();
        } else error = GetComponent<Creature>().TamingInfo;
        this.Tamer = null;
        Reset();
    }
    
    void Update() {
        if (Tamer != null) {
            Debug.Log("HEY!" + max * Time.deltaTime / timeToTame);
            GetComponent<Team>()?.OnAttack(Tamer);
            Increase((int)(max * Time.fixedDeltaTime / timeToTame));
        }
    }

    override protected void OnMaxChanged(Stats stats) {
        max = stats.Def;
        Reset();
    }
}
