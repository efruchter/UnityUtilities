using UnityEngine;
using System.Collections.Generic;

/**
 * Keep track of what we have already hit.
 *
 * -Eric
 */
public class CollisionSet
{
	private HashSet<int> set = new HashSet<int>();
	
	public bool CollideCheck(GameObject go)
	{
		var id = go.GetInstanceID();
		if (set.Contains(id))
			return false;
		set.Add(id);
		return true;	
	}
	
	public void Clear()
	{
		set.Clear();
	}
}
