namespace ADT
{
    using UnityEngine.Assertions;

    public class FixedRingBuffer<T>
    {
        public T[] _buffer;
        public DequeueMemoryPolicy dequeuePolicy;

        int _firstEntryIndex;

        public int Count { private set; get; }
        public int Capacity => _buffer.Length;
        public bool NonEmpty => Count > 0;
        public bool Empty => Count == 0;

        public FixedRingBuffer(int capacity, DequeueMemoryPolicy dequeuePolicy)
        {
            _buffer = new T[capacity];
            _firstEntryIndex = 0;
            this.dequeuePolicy = dequeuePolicy;
        }

        public FixedRingBuffer(int capacity) : this(capacity, DequeueMemoryPolicy.Clear)
        {
            
        }

        public int GetRawIndex(int relativeIndex)
        {
            Assert.IsTrue(_buffer.Length > 0);
            return (_firstEntryIndex + relativeIndex) % _buffer.Length;
        }

        public T this[int relativeIndex]
        {
            get
            {
                Assert.IsTrue(Count > 0);
                return _buffer[GetRawIndex(relativeIndex)];
            }

            set
            {
                Assert.IsTrue(Count >= 0);
                _buffer[GetRawIndex(relativeIndex)] = value;
            }
        }

        public T Dequeue()
        {
            Assert.IsTrue(Count > 0);

            var t = _buffer[_firstEntryIndex];

            if (dequeuePolicy == DequeueMemoryPolicy.Clear)
            {
                _buffer[_firstEntryIndex] = default;
            }

            _firstEntryIndex = GetRawIndex(1);
            Count--;

            return t;
        }

        public void Enqueue(T t)
        {
            Assert.IsTrue(Count <= Capacity);

            if (Count == Capacity)
            {
                Dequeue();
            }

            _buffer[GetRawIndex(Count)] = t;
            Count++;
        }

        public enum DequeueMemoryPolicy
        {
            Preserve, Clear
        }
    }
}
