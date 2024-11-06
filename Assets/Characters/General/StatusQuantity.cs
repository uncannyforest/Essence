using System;
using UnityEngine;

public class StatusQuantity : MonoBehaviour {
    public int max = 1;
    public bool startAtMax = true;
    public GameObject statBarPrefab;
    public Color statBarColor = new Color(.8f, 0, .2f);
    
    public Func<int, bool> Changing;
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
        Stats stats = GetComponent<Stats>();
        if (stats != null) stats.LeveledUp += OnMaxChanged;
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

    public bool IsZero() {
        return level <= 0;
    }

    virtual public bool Decrease(int quantity) {
        if (Changing != null && !Changing(-quantity)) return false;
        if (level <= 0) return false;
        level -= quantity;
        if (level > 0) {
            statBar?.SetPercent((float) level / max);
        } else {
            level = 0;
            if (ReachedZero != null) ReachedZero();
        }
        return true;
    }

    virtual public bool Increase(int quantity) {
        if (Changing != null && !Changing(quantity)) return false;
        if (level >= max) return false;
        level = Math.Min(max, level + quantity);
        statBar?.SetPercent((float) level / max);
        if (IsFull() && ReachedMax != null) ReachedMax();
        return true;
    }

    public void HideStatBar() {
        statBar?.Hide();
    }

    virtual protected void OnMaxChanged(Stats stats) {}
}
