using UnityEngine;

public struct DesireMessage {
    public enum Priority {
        Closest,
        MostRecent
    }

    public Target target;
}
