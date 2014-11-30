using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System;


/**
 * This code automatically sets up an object pool of the instances you attach it to.
 * Usually, Destroy is an expensive operation with processing overhead. Recycling objects
 * in a pool keeps memory use consistent and won't slam the GC with loads of work.
 *
 * Creating:
 * When you want a new object, ask the Pool if one is available, and if so,
 * retrieve it. This object will have its Start method called automatically.
 * Logically, not all objects can be pooled, and you'll need to make sure Start
 * can handle setting up the object.
 *
 * Destroying:
 * Instead of destroying the object, call Pool.PoolObject(gameObject) on it. The PrePool
 * method will be called on all children, followed by deactivation.
 *
 * On Start:
 * Don't forget to call Pool.Clear() at the start of your scene if you have destroyed pooled
 * instances. This happens by default when Unity restarts a scene.
 *
 * -Eric
 */
public class Pool : MonoBehaviour
{
	// Set the poolType in the inspector.
	public string poolType;
	
	private static Dictionary<string, LinkedList<GameObject>> pools;
	
	static Pool()
	{
		pools = new Dictionary<string, LinkedList<GameObject>>();
	}
	
	/**
	 * Deactivate the object and add to the pool.
	 */
	void DeactivateAndPool()
	{
		BroadcastMessage("PrePool", SendMessageOptions.DontRequireReceiver);
		gameObject.SetActive(false);
		GetPool().AddLast(gameObject);
	}
	
	private LinkedList<GameObject> GetPool()
	{
		if (pools.ContainsKey(poolType))
			return pools[poolType];
		else
		{
			var list = new LinkedList<GameObject>();
			pools[poolType] = list;
			return list;
		}
	}
	
	/**
	 * Check if a pooled object is available for use.
	 */
	public static bool InstanceAvailable(string poolType)
	{
		return pools.ContainsKey(poolType) && pools[poolType].Count > 0;
	}
	
	/**
	 * Get a pooled instance. Will be activated.
	 */
	public static GameObject GetInstance(string poolType)
	{
		return GetInstance(poolType, Vector3.zero);
	}
	
	/**
	 * Get a pooled instance. Will be activated and given initial position.
	 */
	public static GameObject GetInstance(string poolType, Vector3 pos)
	{
		return GetInstance(poolType, pos, Quaternion.identity);
	}
	
	/**
	 * Get a pooled instance. Will be activated and given initial position/rotation.
	 */
	public static GameObject GetInstance(string poolType, Vector3 pos, Quaternion rot)
	{
		GameObject g = pools[poolType].First.Value;
		pools[poolType].RemoveFirst();
		
		if (g == null)
			throw new Exception("Object of type " + poolType + " has been improperly destroyed.");
		
		g.transform.position = pos;
		g.transform.rotation = rot;
		g.SetActive(true);
		g.BroadcastMessage("Start", SendMessageOptions.DontRequireReceiver);
		
		return g;
	}
	
	/**
	 * Pool and deactivate an instance.
	 */
	public static void PoolObject(GameObject self)
	{		
		if (self.activeSelf)
			self.BroadcastMessage("DeactivateAndPool", SendMessageOptions.RequireReceiver);
	}
	
	/**
	 * Clear the pools. You should do this when a scene is first started,
	 * as many objects will be destroyed by Unity. 
	 */
	public static void Clear()
	{
		pools.Clear();
	}
	
	/**
	 * Print out pool contents.
	 */
	public static void DebugPools()
	{
		StringBuilder str = new StringBuilder("Pool Counts: ");
		foreach (var key in pools.Keys)
		{
			str.Append(key).Append(": ").Append(pools[key].Count).Append(" | ");
		}
		Debug.Log(str);
	}
}
