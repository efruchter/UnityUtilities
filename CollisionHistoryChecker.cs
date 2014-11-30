using UnityEngine;
using System.Collections.Generic;

/**
 * Keep track of what we have already hit.
 *
 * -Eric
 */
public class CollisionHistoryChecker
{
	private HashSet<int> set = new HashSet<int>();
	
	/**
	 * Check gameObject for collision. If previously collided with,
	 * return false, otherwise register the collision and return true.
	 */
	public bool CollideCheck(GameObject go)
	{
		var id = go.GetInstanceID();
		if (set.Contains(id))
			return false;
		set.Add(id);
		return true;	
	}
	
	/*
	 * Clear collision history.
	 */
	public void Clear()
	{
		set.Clear();
	}
}
