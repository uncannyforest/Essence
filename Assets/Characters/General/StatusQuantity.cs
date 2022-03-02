using System;
using UnityEngine;

public class StatusQuantity : MonoBehaviour {
    public int max = 1;
    public bool startAtMax = true;
    public GameObject statBarPrefab;
    public Color statBarColor = new Color(.8f, 0, .2f);
    
    public Action ReachedZero;
    public Action ReachedMax;

    protected int level;
    protected StatBar statBar = null;
    
    public int Level {
        get => level;
    }
    public float LevelPercent {
        get => level / (float)max;
    }

    virtual protected void Awake() {
        if (startAtMax) level = max;
        if (statBarPrefab != null) statBar = StatBar.Instantiate(statBarPrefab, this, statBarColor);
    }

    virtual public void Reset() {
        if (startAtMax) ResetTo(max);
        else ResetTo(0);
    }

    virtual public void ResetTo(int level) {
        this.level = level;
        statBar?.SetPercentWithoutVisibility((float) level / max);
        statBar?.Hide();
    }



    virtual public bool IsFull() {
        return level == max;
    }

    virtual public void Decrease(int quantity) {
        level -= quantity;
        if (level > 0) {
            statBar?.SetPercent((float) level / max);
        }
        else if (ReachedZero != null) ReachedZero();
    }

    virtual public void Increase(int quantity) {
        level = Math.Min(max, level + quantity);
        statBar?.SetPercent((float) level / max);
        if (IsFull() && ReachedMax != null) ReachedMax();
    }

    public void HideStatBar() {
        statBar?.Hide();
    }
}
