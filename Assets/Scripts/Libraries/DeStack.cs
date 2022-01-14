using System.Collections.Generic;

public class DeStack<T> : List<T> { // borrowing all List functionality
    public void Push(T item) {
        Add(item);
    }

    public T Pop() {
        T result = this[Count - 1];
        RemoveAt(Count - 1);
        return result;
    }

    public T Peek() {
        return this[Count - 1];
    }

    public void PushToBottom(T item) {
        Insert(0, item);
    }

    public T PopFromBottom() {
        T result = this[0];
        RemoveAt(0);
        return result;
    }

    public T PeekBottom() {
        return this[0];
    }
}
