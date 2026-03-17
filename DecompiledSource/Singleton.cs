using UnityEngine;

public abstract class Singleton : KoroutineBehaviour
{
	protected virtual void Awake()
	{
		SetInstance();
	}

	protected virtual void OnEnable()
	{
		SetInstance();
	}

	protected abstract void SetInstance();

	protected abstract void ClearInstance();

	protected void SetInstance<T>(ref T static_instance, T instance) where T : Singleton
	{
		if (!(static_instance == instance))
		{
			if (static_instance != null)
			{
				Debug.LogError("Overwriting " + static_instance?.ToString() + " instance with " + instance?.ToString() + ", shouldn't happen");
			}
			static_instance = instance;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		ClearInstance();
	}
}
