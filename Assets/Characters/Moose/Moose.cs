using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MooseConfig {
    public Sprite attackAction;
    public float meleeReach;
    public int attack;
}

[RequireComponent(typeof(Health))]
public class Moose : Species<MooseConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new MooseBrain(this, generalConfig, speciesConfig);
    }
}

public class MooseBrain : Brain {
    private MooseConfig moose;

    public MooseBrain(Moose species, BrainConfig general, MooseConfig moose) : base(species, general) {
        this.moose = moose;
    }

    override public bool CanTame(Transform player) => true;

    public override bool ExtractTamingCost(Transform player) => true;

    override public List<CreatureAction> Actions() {
        return new List<CreatureAction>() {
        };
    }

}
