using System;
using UnityEngine;

[Serializable]
public class BuggeConfig {
}

[RequireComponent(typeof(Anthopoid))]
public class Bugge : Species<BuggeConfig> {
    override public Brain Brain(BrainConfig generalConfig) {
        return new BuggeBrain(this, generalConfig, speciesConfig);
    }
}

public class BuggeBrain : Brain {
    private BuggeConfig bugge;

    public BuggeBrain(Bugge species, BrainConfig general, BuggeConfig bugge) : base(species, general) {
        this.bugge = bugge;
    }

    override public bool CanTame(Transform player) => false;
}
