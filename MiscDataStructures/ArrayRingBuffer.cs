using System.Collections.Generic;

/// <summary>
/// An array-based ring buffer. Faster random access than .NET Queue.
/// </summary>
public class ArrayRingBuffer<T> : IEnumerable<T>{
    public readonly T[] array;
    private int startingIndex = 0;
    private int count;

    public ArrayRingBuffer(int size, T defaultValue) {
        array = new T[size];
        for (int i = 0; i < array.Length; i++) {
            array[i] = defaultValue;
        }
    }

    public ArrayRingBuffer(int size) : this(size, default(T)) {
        ;
    }

    private int nextWriteIndex {
        get {
            return (startingIndex + count) % Capacity;
        }
    }

    public void Enqueue(T t) {
        array [nextWriteIndex] = t;
        count++;
        if (count > Capacity) {
            startingIndex = CalculateIndex (1);
            count = Capacity;
        }
    }

	public T Last{
		get{
			return this [count - 1];
		}
	}

    private int CalculateIndex(int relativeIndex) {
        if (relativeIndex < 0 || relativeIndex >= Count) {
            throw new System.ArgumentOutOfRangeException ("Index is out of range: " + relativeIndex);
        }
        return (startingIndex + relativeIndex) % Capacity;
    }

    public T this [int index] {
        set {
            array [CalculateIndex (index)] = value;
        }
        get {
            return array [CalculateIndex (index)];
        }
    }

    public int Capacity { get { return array.Length; } }
    public int Count { get { return count; } }

    #region IEnumerable implementation

    public IEnumerator<T> GetEnumerator () {
        for (int i = 0; i < Count; i++) {
            yield return this [i];
        }
    }

    #endregion

    #region IEnumerable implementation

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () {
        return GetEnumerator ();
    }

    #endregion

    public override string ToString() {
        System.Text.StringBuilder str = new System.Text.StringBuilder ();
        str.Append ("{");
        foreach (var t in this) {
            str.Append (" ");
            str.Append (t);
            str.Append (" ");
        }
        str.Append ("}");
        return str.ToString ();
    }
}
