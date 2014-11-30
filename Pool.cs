using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System;


/**
 * This code automatically sets up an object pool of the instances you attach it to.
 * Usually, Destroy() is an expensive operation with heavy processing overhead. Recycling
 * objects in a pool keeps memory use consistent and won't slam the GC with loads of work.
 *
 * Creating
 * --------
 * When you want a new object, ask the Pool if one is available, and if so,
 * retrieve it. If the pool is empty, you must make a new instance.
 * 
 *  GameObject obj;
 *
 *  if (Pool.InstanceAvailable(objectType))
 *      obj = Pool.GetInstance(objectType);
 *  else
 *      obj = Instantiate(prefab);
 *
 * The object and its children will have their Start methods called automatically.
 * You'll need to make sure Start can handle setting up the object properly. Use Awake
 * to set up any permenant values or perform any expensive functions you only need to do
 * once.
 *
 * Destroying
 * ----------
 * Instead of destroying the object, call Pool.PoolObject(gameObject) on it. The PrePool
 * method will be called on all children, followed by deactivation.
 *
 *  void PrePool()
 *  {
 *      // Do whatever cleanup needs to be done.
 *  }
 *
 * On Scene Start
 * --------------
 * The pools are static, and will persist between Scenes, so don't forget to call Pool.Clear() at the
 * start of your scene if you have destroyed any pooled instances. This happens by default when Unity
 * closes a scene. If you get a warning when you try to GetInstance, this is likely the culprit.
 *
 * Working With Pooling
 * --------------------
 * You can warm up the object pools by creating and then pooling all of the objects you will need
 * at the start of each scene. Instantiate is expensive, so doing it only at the beginning of the
 * Scene is a very good idea.
 *
 * Due to the way Unity collisions work, you might sometimes collide with an object that has been
 * pooled already. If this happens, you might want to check and see if a object is active before
 * doing work on it. To check if an object is active and unpooled, use the activeSelf variable
 * inside GameObjects to see if they are active. This will happen most when you are using
 * CollisionStay. For most use, it will not be an issue.
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
