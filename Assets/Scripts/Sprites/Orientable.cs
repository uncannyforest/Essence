using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Orientable))]
public class OrientableComponent : MonoBehaviour {
    public Orientable orientable { get => GetComponent<Orientable>(); }
}

public static class OMBExtension {
    public static OrientableChild OrientableChild(this MonoBehaviour mb) => new OrientableChild(mb.transform);
}

public class Orientable : MonoBehaviour {
    public Vector2 position => transform.position;

    virtual public void Start() {
        Orientor.SetRotation(transform);
    }
}

// Wraps transform of any child of a GameObject with an Orientable.
public class OrientableChild {
    public Transform transform;

    public GameObject gameObject { get => transform.gameObject; }
    public Vector2 position {
        get => transform.position;
        set => transform.position = value.WithZ(transform.position.z);
    } 
    public Vector3 position3 { set => transform.position = value; }
    public Orientable rootParent { get => transform.parent.GetComponentInParent<Orientable>(); }
    public int childCount { get => transform.childCount; }
    public IEnumerable<OrientableChild> children {
        get => from child in transform.Cast<Transform>()
            select new OrientableChild(child);
    }
    public OrientableChild(Transform transform) {
        this.transform = transform;
    }

    public IEnumerable<OrientableChild> GetChildren<T>() where T : Component  {
        return from component in transform.GetComponentsInChildren<T>()
            select new OrientableChild(component.transform);
    }
}
