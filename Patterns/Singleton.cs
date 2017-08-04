using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

/// <summary>
/// Singleton manager. Provides a warning when trying to double-add or get null singletons.
/// </summary>
public static class Singleton<T> where T : class
{
	private static T _singleton;
	private static Action<T> _onAdd;

	public static T Instance
	{
		set
		{
			AddSingleton (value);
		}
		get
		{
			return GetSingleton();
		}
	}

	public static T GetSingleton()
	{
		Assert.IsNotNull (_singleton, "SingletonManager: Warning, attempting to get a singleton that does not exist." +
			" Be sure you are not calling get before Awake()");

		return _singleton;
	}

	public static void GetSingletonDeferred(Action<T> onAdd)
	{
		Assert.IsNotNull(onAdd, "Callback can not be null.");

		T singleton = GetSingleton();
		if (singleton == null)
		{
			if (_onAdd == null)
			{
				_onAdd = onAdd;
			}
			else
			{
				_onAdd += onAdd;
			}
		}
		else
		{
			onAdd(singleton);
		}
	}

	public static void AddSingleton(T singleton)
	{
		AddSingleton(singleton, false);
	}

	public static void AddSingleton(T singleton, bool allowReplace)
	{
		Assert.IsNotNull (singleton, "Singleton can not be null.");
		Assert.IsFalse ((!allowReplace) && (_singleton != null), "Attempting to replace a singleton that already exists.");

		_singleton = singleton;

		if (_onAdd != null)
		{
			_onAdd (singleton);
			_onAdd = null;
		}
	}
}
