using System;
using UnityEngine.Assertions;

namespace Patterns
{
	/// <summary>
	/// Singleton manager. Provides a warning when trying to double-add or get null singletons.
	/// </summary>
	public static class Singleton<T> where T : class
	{
		private static T _singleton;
		private static Action<T> _onAdd;

		/// <summary>
		/// O(1) Fetch or add of singleton.
		/// </summary>
		public static T Instance
		{
			set { AddSingleton(value); }
			get { return GetSingleton(); }
		}

		public static bool Exists
		{
			get
			{
				return _singleton != null;
			}
		}

		/// <summary>
		/// Get a singleton. Warning if not available.
		/// </summary>
		/// <returns>The singleton if available.</returns>
		public static T GetSingleton()
		{
			Assert.IsTrue(Exists, "Singleton does not exist at this moment. Advice: Fetch Singletons with GetSingletonDeferred, or later in the frame.");
			return _singleton;
		}

		/// <summary>
		/// Perform an action as soon as the singleton is available.
		/// </summary>
		public static Action<T> InstanceReady
		{
			set
			{
				GetSingletonDeferred(value);
			}
		}

		/// <summary>
		/// Get a singleton whenever it is available.
		/// </summary>
		/// <param name="onAdd">The callback to run when singleton Exists</param>
		public static void GetSingletonDeferred(Action<T> onAdd)
		{
			Assert.IsNotNull(onAdd, "Callback can not be null.");

			if (Exists)
			{
				onAdd(GetSingleton());
			}
			else if (_onAdd == null)
			{
				_onAdd = onAdd;
			}
			else
			{
				_onAdd += onAdd;
			}
		}

		/// <summary>
		/// Register a singleton. Replacement is illegal.
		/// </summary>
		/// <param name="singleton">The instance.</param>
		public static void AddSingleton(T singleton)
		{
			AddSingleton(singleton, false);
		}

		/// <summary>
		/// Release the singleton so others can use it.
		/// </summary>
		public static void Release()
		{
			_singleton = null;
			_onAdd = null;
		}

		/// <summary>
		/// Register a singleton.
		/// </summary>
		/// <param name="singleton">The instance.</param>
		/// <param name="allowReplace">If true, allow replacement without warning.</param>
		public static void AddSingleton(T singleton, bool allowReplace)
		{
			if (singleton == null)
			{
				Release();
				return;
			}

			Assert.IsFalse(!allowReplace && Exists, "Attempting to replace a singleton that already exists.");

			_singleton = singleton;
			if (_onAdd != null)
			{
				_onAdd(singleton);
				_onAdd = null;
			}
		}
	}
}
