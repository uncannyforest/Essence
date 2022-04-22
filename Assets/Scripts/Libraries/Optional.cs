using System;

public class Optional {
    public static Optional<T> Of<T>(T value) {
        return Optional<T>.Of(value);
    }
    public static Optional<T> Empty<T>() {
        return Optional<T>.Empty();
    }
}

// derived from https://stackoverflow.com/a/16199308 with modifications
public struct Optional<T> {
    public bool HasValue { get; private set; }
    private T value;
    public T Value {
        get {
            if (HasValue)
                return value;
            else
                throw new InvalidOperationException();
        }
    }
    // for Unity :)
    public bool IsDestroyed {
        get {
            return HasValue && Value == null;
        }
    }

    private Optional(T value) {
        this.value = value;
        HasValue = true;
    }
    public static Optional<T> Of(T value) {
        return new Optional<T>(value);
    }
    public static Optional<T> Empty() {
        return new Optional<T>();
    }

    public T Or(T defaultValue) {
        if (HasValue) return value;
        else return defaultValue;
    }

    public T Else(Func<T> fallback) {
        if (HasValue) return value;
        else return fallback();
    }

    public static explicit operator T(Optional<T> optional) {
        return optional.Value;
    }
    public static explicit operator Optional<T>(T value) {
        return new Optional<T>(value);
    }

    public override bool Equals(object obj) {
        if (obj is Optional<T>)
            return this.Equals((Optional<T>)obj);
        else
            return false;
    }
    public bool Equals(Optional<T> other) {
        if (HasValue && other.HasValue)
            return object.Equals(value, other.value);
        else
            return HasValue == other.HasValue;
    }
    public override int GetHashCode() {
        return base.GetHashCode() + value.GetHashCode();
    }
}