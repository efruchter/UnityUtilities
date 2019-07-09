using System.Collections.Generic;

namespace ADT
{
    public class TemporalRecord<T>
    {
        List<Record> _records;

        public TemporalRecord(int initialCapacity)
        {
            _records = new List<Record>(initialCapacity);
        }
        

        public TemporalRecord() : this(0) { }

        public struct Record
        {
            public float time;
            public T t;
        }

        public float GetDuration()
        {
            if (_records.Count > 0)
            {
                return _records[_records.Count - 1].time;
            }

            return 0;
        }

        /// <summary>
        /// Add a record. Inserting records is nlogn, try to only add records to the end.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="time"></param>
        public void AddRecord(in T record, in float time)
        {
            Record rec = new Record
            {
                t = record,
                time  = time
            };

            if (_records.Count == 0)
            {
                _records.Add(rec);
                return;
            }
            
            if (_records[_records.Count - 1].time < time)
            {
                _records.Add(rec);
                return;
            }

            // TODO: Insert the record with Binary Search
            _records.Add(rec);
            _records.Sort((a, b) => a.time.CompareTo(b.time));
        }

        /// <summary>
        /// A function that lerps between a and b by blend factor t.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns>The blended data.</returns>
        public delegate T LerpingFunction(T a, T b, float t);

        public T GetNearestVal(in float time)
        {
            return GetBlendedVal(time, (a, b, t) => (t < 0.5f) ? a : b);
        }

        /// <summary>
        /// Retrieve blended record and use a custom lerp to blend. O(logn) to pull a record.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="lerpingFunction"></param>
        /// <returns></returns>
        public T GetBlendedVal(in float time, in LerpingFunction lerpingFunction)
        {
            if (lerpingFunction == null)
                return GetNearestVal(time);

            if (_records.Count < 1)
                return default;
            
            if (_records.Count == 1)
                return _records[0].t;

            if (time >= _records[_records.Count - 1].time)
                return _records[_records.Count - 1].t;

            if (time <= _records[0].time)
                return _records[0].t;
        
            int L = 0, R = _records.Count - 2;
            while (L <= R)
            {
                int M = (L + R) / 2;

                if (_records[M].time <= time && _records[M + 1].time >= time)
                {
                    float remap (float val, float lowInterval, float highInterval)
                    {
                        if (lowInterval == highInterval)
                            return lowInterval;

                        return (val - lowInterval) / (highInterval - lowInterval);
                    }

                    float blend = remap(time, _records[M].time, _records[M + 1].time);
                    return lerpingFunction(_records[M].t, _records[M + 1].t, blend);
                }

                if (_records[M].time < time)
                    L = M + 1;
                else if (_records[M].time > time)
                    R = M - 1;
            }

            return default;
        }

        public void Clear()
        {
            _records.Clear();
        }

        /// <summary>
        /// Remove every other entry except the caps.
        /// </summary>
        public void SimplifyHalf()
        {
            for (int i = _records.Count - 2; i > 0; i -= 2)
                _records.RemoveAt(i);
        }
    }
}
