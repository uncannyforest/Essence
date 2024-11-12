using System;
using UnityEngine;

public class StatusQuantity : MonoBehaviour {
    public int max = 1;
    public float startAtFraction = 1;
    public GameObject statBarPrefab;
    public Color statBarColor = new Color(.8f, 0, .2f);
    public float statBarOffset = .125f;
    public bool showOnHover = true;
    
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
        level = 1; // since max == 1, start full before resetting inside stats.LeveledUp
        if (statBarPrefab != null) {
            statBar = StatBar.Instantiate(statBarPrefab, this, statBarColor);
            foreach (Transform child in statBar.gameObject.transform) {
                Vector3 position = child.localPosition;
                position.y = -statBarOffset;
                child.localPosition = position;
            }
        }
        Stats stats = GetComponent<Stats>();
        if (stats != null) stats.LeveledUp += OnMaxChanged;
        else level = Mathf.RoundToInt(max * startAtFraction);
    }

    virtual public void Reset() {
        ResetTo(Mathf.RoundToInt(max * startAtFraction));
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

    public void ShowStatBar() {
        statBar?.Show();
    }

    public void HideStatBar() {
        statBar?.Hide();
    }

    private void OnMaxChanged(Stats stats) {
        if (GetMaxFromStats(stats) is int newMax) {
            Debug.Log(this + " changing max from " + max + " to " + newMax + " with prev level " + level + " increasing at " + startAtFraction);
            int diff = newMax - max;
            max = newMax;
            ResetTo(level + Mathf.RoundToInt(diff * startAtFraction));
        }
    }

    virtual protected int? GetMaxFromStats(Stats stats) => null;
}
