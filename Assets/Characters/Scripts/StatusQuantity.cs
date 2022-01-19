using System;
using UnityEngine;

public class StatusQuantity : MonoBehaviour {
    public int max = 0;
    public bool startAtMax = true;
    public GameObject statBarPrefab;
    public Color statBarColor = new Color(.8f, 0, .2f);
    
    public Action ReachedZero;
    public Action ReachedMax;

    protected int level;
    protected StatBar statBar;
    
    public int Level {
        get => level;
    }

    virtual protected void Awake() {
        if (startAtMax) level = max;
        statBar = StatBar.Instantiate(statBarPrefab, this, statBarColor);
    }

    virtual public void Reset() {
        if (startAtMax) level = max;
        else level = 0;
        statBar.SetPercentWithoutVisibility((float) level / max);
        statBar.Hide();
    }

    virtual public bool IsFull() {
        return level == max;
    }

    virtual public void Decrease(int quantity) {
        level -= quantity;
        if (level > 0) {
            statBar.SetPercent((float) level / max);
        }
        else if (ReachedZero != null) ReachedZero();
    }

    virtual public void Increase(int quantity) {
        level = Math.Min(max, level + quantity);
        statBar.SetPercent((float) level / max);
        if (IsFull() && ReachedMax != null) ReachedMax();
    }

    public void HideStatBar() {
        statBar.Hide();
    }
}
