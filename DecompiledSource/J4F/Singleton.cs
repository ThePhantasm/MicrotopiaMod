using UnityEngine;

namespace J4F;

public class Singleton<T> : J4FBehaviour where T : J4FBehaviour
{
	private static T _instance;

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
}
