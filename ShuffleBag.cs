using System;
using System.Collections.Generic;

/*
 * Generic version of: http://gamedevelopment.tutsplus.com/tutorials/shuffle-bags-making-random-feel-more-random--gamedev-1249
 *
 * A random generator that guarantees events occur with a controllable amount.
 * It's a good idea to make sure that improbable events actually happen from time to time.
 *
 * -Eric
 */
public class ShuffleBag<T>
{
	private Random random = new Random();
	private List<T> data;
	
	private T currentItem;
	private int currentPosition = -1;
	
	private int Capacity { get { return data.Capacity; } }
	public int Size { get { return data.Count; } }
	
	public ShuffleBag(int initialCapacity)
	{
		data = new List<T>(initialCapacity);
	}
	
	/**
	 * Add an item some number of times. The ratio of this amount
	 * to the total bag capacity is the probability it will happen.
	 */
	public void Add(T item, int amount=1)
	{
		for (int i = 0; i < amount; i++)
			data.Add(item);
		
		currentPosition = Size - 1;
	}
	
	/**
	 * Get a random sample from the bag.
	 */
	public T Next()
	{
		if (currentPosition < 1)
		{
			currentPosition = Size - 1;
			currentItem = data[0];
			
			return currentItem;
		}
		
		var pos = random.Next(currentPosition);
		
		currentItem = data[pos];
		data[pos] = data[currentPosition];
		data[currentPosition] = currentItem;
		currentPosition--;
		
		return currentItem;
	}
}

