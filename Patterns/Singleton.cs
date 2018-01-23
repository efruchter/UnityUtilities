using UnityEngine.Assertions;

namespace Patterns
{
	/// <summary>
	/// Singleton manager.
	/// </summary>
	public static class Singleton<T> where T : class
	{
		public delegate void SingletonReady(T t);

		private static T _singleton;
		private static SingletonReady _onAdd;

		/// <summary>
		/// O(1) Fetch or add a singleton.
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
				return _singleton != null
					&& !_singleton.Equals(null);
			}
		}

		/// <summary>
		/// Get a singleton.
		/// </summary>
		/// <returns>The singleton if available.</returns>
		public static T GetSingleton()
		{
			return _singleton;
		}

		/// <summary>
		/// Perform an action as soon as the singleton is available.
		/// </summary>
		public static SingletonReady InstanceReady
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
		public static void GetSingletonDeferred(SingletonReady onAdd)
		{
			if (onAdd == null)
			{
				return;
			}

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

			Assert.IsFalse(!allowReplace && Exists, "Replacing a singleton that already exists. Check the stack trace for more info. If you would like to allow replacement, call AddSingleton and set allowReplace to true.");

			_singleton = singleton;
			if (_onAdd != null)
			{
				_onAdd(singleton);
				_onAdd = null;
			}
		}
	}
}
