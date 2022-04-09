using System;

public struct Delta<T> {
    private enum Sign {
        None,
        Add,
        Remove,
    }

    private Sign sign; // Sign.None
    public bool Exists {
        get => sign != Sign.None;
    }
    public bool IsAdd {
        get => sign == Sign.Add;
    }
    public bool IsRemove {
        get => sign == Sign.Remove;
    }
    private T value;
    public T Value {
        get {
            if (IsAdd)
                return value;
            else
                throw new InvalidOperationException();
        }
    }

    private Delta(Sign sign) {
        this.sign = sign;
        this.value = default;
    }
    private Delta(Sign sign, T value) {
        this.sign = sign;
        this.value = value;
    }
    public static Delta<T> Add(T value) {
        return new Delta<T>(Sign.Add, value);
    }
    public static Delta<T> Remove() {
        return new Delta<T>(Sign.Remove);
    }
    public static Delta<T> None() {
        return new Delta<T>(Sign.None);
    }

    public override bool Equals(object obj) {
        if (obj is Optional<T>)
            return this.Equals((Optional<T>)obj);
        else
            return false;
    }
    public bool Equals(Delta<T> other) {
        if (IsAdd && other.IsAdd)
            return object.Equals(value, other.value);
        else
            return sign == other.sign;
    }
    public override int GetHashCode() {
        return sign.GetHashCode() + value.GetHashCode();
    }
}