/// <summary>
/// Extension methods for bit-operations on a 32 bit int.
/// </summary>
public static class BitUtil
{
    /// <summary>
    /// Set a bit to a value.
    /// </summary>
    /// <param name="n">number</param>
    /// <param name="bit">bit</param>
    /// <param name="value">Should the bit be 1?</param>
    /// <returns>the number with bit changed</returns>
    public static int Set(this int n, int bit, bool value)
    {
        return value ? Set(n, bit) : Clear(n, bit);
    }

    /// <summary>
    /// Sets a bit to 1.
    /// </summary>
    /// <param name="n">number</param>
    /// <param name="bit">bit</param>
    /// <returns>the number with bit changed</returns>
    public static int Set(this int n, int bit)
    {
        return n | (1 << bit);
    }

    /// <summary>
    /// Set a bit to 0.
    /// </summary>
    /// <param name="n">number</param>
    /// <param name="bit">bit</param>
    /// <returns>the number with bit changed</returns>
    public static int Clear(this int n, int bit)
    {
        return n & ~(1 << bit);
    }

    /// <summary>
    /// Get if a bit is 1.
    /// </summary>
    /// <param name="n">number</param>
    /// <param name="bit">bit</param>
    /// <returns>True if the bit is 1</returns>
    public static bool Get(this int n, int bit)
    {
        return (n & (1 << bit)) != 0;
    }

    /// <summary>
    /// Every bit is set to 1.
    /// </summary>
    public const int All = ~0;

    /// <summary>
    /// Every bit is set to 0.
    /// </summary>
    public const int None = 0;

    /// <summary>
    /// BitSet that checks inclusiveness on [0, 31] entries. Backed by an int, can be used in the same way.
    /// </summary>
    public struct BitSet
    {
        public int value;

        public void Add(int bit)
        {
            if (bit < 0 || bit >= 32)
            {
                throw new System.ArgumentOutOfRangeException("Bit must be in the range [0, 31].");
            }
            value = value.Set(bit);
        }

        public void Remove(int bit)
        {
            if (bit < 0 || bit >= 32)
            {
                throw new System.ArgumentOutOfRangeException("Bit must be in the range [0, 31].");
            }
            value = value.Clear(bit);
        }

        public bool Contains(int bit)
        {
            if (bit < 0 || bit >= 32)
            {
                throw new System.ArgumentOutOfRangeException("Bit must be in the range [0, 31].");
            }
            return value.Get(bit);
        }

        public void Clear()
        {
            value = BitUtil.None;
        }

        public void AddAll()
        {
            value = BitUtil.All;
        }
    }
}
