using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Patterns
{
	/// <summary>
	/// Object pool capable of sophisticated instantiation policies.
	/// </summary>
	[Serializable]
	public sealed class ObjectPool
	{
		public GameObject Prefab;
		public int PreWarmCount = 20;
		public EmptyPolicy WhenEmpty = EmptyPolicy.ReclaimOldest;

		private readonly Queue<GameObject> _deadPool = new Queue<GameObject>();
		private readonly LinkedListDictionary<int, GameObject> _livePool = new LinkedListDictionary<int, GameObject>();

		/// <summary>
		/// Warm up the pool. Required in Reclaim mode.
		/// </summary>
		public void Warm()
		{
			if (Prefab != null && PreWarmCount > 0)
			{
				for (int i = 0; i < PreWarmCount; i++)
				{
					ReturnToPool(Object.Instantiate(Prefab));
				}
			}
		}

		/// <summary>
		/// Return an instance to the pool.
		/// </summary>
		/// <param name="instance"></param>
		public void ReturnToPool(GameObject instance)
		{
			Assert.IsNotNull(instance);
			instance.SetActive(false);
			instance.transform.SetParent(null);
			_deadPool.Enqueue(instance);
		}

		public void ReturnToPool(Component instance)
		{
			Assert.IsNotNull(instance);
			ReturnToPool(instance.gameObject);
		}

		/// <summary>
		/// Fetch an object from the pool. Possibly instantiating a new one.
		/// </summary>
		/// <param name="wasReclaimedWhileAlive">true if this is a reclaimed object from the live pool.</param>
		/// <returns></returns>
		public GameObject FetchFromPool(out bool wasReclaimedWhileAlive)
		{
			GameObject instance = null;
			wasReclaimedWhileAlive = false;

			if (_deadPool.Count > 0)
			{
				instance = _deadPool.Dequeue();
			}
			else if (WhenEmpty == EmptyPolicy.InstantiateNew)
			{
				if (Prefab != null)
				{
					instance = Object.Instantiate(Prefab);
				}
			}
			else if (WhenEmpty == EmptyPolicy.ReclaimOldest)
			{
				if (_livePool.Count > 0)
				{
					instance = _livePool.RemoveOldest();
					instance.SetActive(false);
					wasReclaimedWhileAlive = true;
				}
				else if (Debug.isDebugBuild)
				{
					Debug.LogError("Dead and Live Pools are empty. Please Warm Up instances in Reclaim Mode.");
				}
			}

			if (instance != null)
			{
				_livePool.Add(instance.GetInstanceID(), instance);
				instance.SetActive(true);
			}

			return instance;
		}

		public T FetchFromPool<T>(out bool wasReclaimedWhileAlive) where T : Object
		{
			GameObject instance = FetchFromPool(out wasReclaimedWhileAlive);
			return instance == null ? null : instance.GetComponent<T>();
		}

		public GameObject FetchFromPool()
		{
			bool wasReclaimedWhileAlive;
			return FetchFromPool(out wasReclaimedWhileAlive);
		}

		public T FetchFromPool<T>() where T : Object
		{
			bool wasReclaimedWhileAlive;
			return FetchFromPool<T>(out wasReclaimedWhileAlive);
		}

		public enum EmptyPolicy
		{
			InstantiateNew,
			ReclaimOldest
		}
	}
}
