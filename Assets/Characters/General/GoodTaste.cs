using System;
using UnityEngine;

[RequireComponent(typeof(Creature))]
[RequireComponent(typeof(Team))]
public class GoodTaste : StatusQuantity {
    public string insufficientTimeInfoShort = "Whistle song (press and hold) to tame";
    public string insufficientTimeInfoLong = "The <creature/> will not be tamed unless you whistle a song for sufficient time.  Press and hold the left mouse button to whistle a song.  A blue stat bar will appear beneath the <creature/> to indicate it can hear you; when the stat bar reaches the end the <creature/> will be tamed.";

    private Creature creature;
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
        creature = GetComponent<Creature>();
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
            CharacterController movement = creature.OverrideControl(this);
            if (creature.State.type != CreatureStateType.Faint) movement.IdleFacing(Tamer.position);
        } else {
            GetComponent<Creature>().ReleaseControl();
        }
    }

    public void HandleTamed() {
        Transform oldTamer = Tamer;
        this.Tamer = null;
        if (creature.team.SameTeam(oldTamer)) {
            creature.ForceTame(oldTamer);
            if (Tamed != null) Tamed();
        } else if (creature.TryTame(oldTamer)) {
            if (Tamed != null) Tamed();
        } else error = creature.TamingInfo;
        Reset();
    }
    
    void Update() {
        if (Tamer != null) {
            if (!creature.team.SameTeam(Tamer)) creature.team?.OnAttack(Tamer);
            Increase(1);
        }
    }

    override protected int? GetMaxFromStats(Stats stats) => stats.Def;
}
