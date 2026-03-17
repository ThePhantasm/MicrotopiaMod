using UnityEngine;

namespace J4F;

public class SingletonPersist<T> : J4FBehaviour where T : J4FBehaviour
{
	protected static T _instance;

	protected static object _lock = new object();

	public static T Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindObjectOfType<T>();
			}
			return _instance;
		}
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		if (Instance == null || Instance.gameObject == base.gameObject)
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
