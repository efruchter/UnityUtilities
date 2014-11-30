using UnityEngine;
using System.Collections.Generic;

/**
 * Keep track of what gameobjects we have already checked.
 * Useful for collision action response.
 *
 * -Eric
 */
public class GameObjectIdentitySet
{
	private HashSet<int> set = new HashSet<int>();
	
	/**
	 * Check gameObject's id. If previously checked,
	 * return false, otherwise register the id and return true.
	 */
	public bool Check(GameObject go)
	{
		var id = go.GetInstanceID();
		if (set.Contains(id))
			return false;
		set.Add(id);
		return true;	
	}
	
	/*
	 * Clear check history.
	 */
	public void Clear()
	{
		set.Clear();
	}
}
