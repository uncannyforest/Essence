using System;
using System.Collections.Generic;
using UnityEngine;

public class UncannyGrid<T> {
    private List<List<T>> quad1 = new List<List<T>>();
    
    private T get2D(List<List<T>> list2d, int x, int y) {
        if (list2d.Count <= x) return default(T);
        List<T> list = list2d[x];
        if (list.Count <= y) return default(T);
        return list[y];
    }

    private void set2D(List<List<T>> list2d, int x, int y, T value) {
        while (list2d.Count <= x) list2d.Add(new List<T>());
        List<T> list = list2d[x];
        while (list.Count <= y) list.Add(default(T));
        list[y] = value;
    }

    public T this[Vector2Int pos] {
        get {
            return get2D(quad1, pos.x, pos.y);
        }
        set {
            set2D(quad1, pos.x, pos.y, value);
        }
    }
}
