using UnityEngine;
using System.Collections.Generic;
/**
 * Create a bag of contents that are shuffled as they are extracted.
 * Superior to weighted random in instances where a quota of events should be met.
 * -Eric
 */
public class ShuffleBag<T> {
	List<T> list;
	int index;
	
	public ShuffleBag() {
		list = new List<T>();
		index = 0;
	}
	
	public T Next() {
		T res = list[index];

		int swapWith = Random.Range(0, index);
		
		var temp = list[index];
		list[index] = list[swapWith];
		list[swapWith] = temp;
		
		index = (index + 1) % Count;
		
		return res;
	}
	
	public void Add(T t, int count) {
		for (int i = 0; i < count; i++) {
			list.Add(t);
		}
		index = 0;
	}
	
	public void Shuffle() {
		index = 0;
		for (int i = 0; i < list.Count; i++) {
			Next();
		}
	}
	
	public int Count {
		get {
			return list.Count;
		}
	}
}
