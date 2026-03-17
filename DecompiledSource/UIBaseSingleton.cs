using UnityEngine;

public abstract class UIBaseSingleton : UIBase
{
	private static bool showOnAwake;

	private GameObject obUI;

	protected override void MyAwake()
	{
		base.MyAwake();
		obUI = base.gameObject;
		SetInstance();
		SetVisible(showOnAwake);
	}

	public virtual void SetVisible(bool visible)
	{
		obUI.SetObActive(visible);
	}

	public bool IsVisible()
	{
		return obUI.activeSelf;
	}

	protected virtual void OnEnable()
	{
		SetInstance();
	}

	protected abstract void SetInstance();

	protected abstract void ClearInstance();

	protected void SetInstance<T>(ref T static_instance, T instance) where T : UIBaseSingleton
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
		ClearInstance();
		base.OnDestroy();
	}

	protected override void Close()
	{
		ClearInstance();
		base.Close();
	}

	public static T Get<T>(T instance, bool show = true) where T : UIBaseSingleton
	{
		return Get(instance, null, show);
	}

	public static T Get<T>(T instance, GameObject prefab, bool show = true) where T : UIBaseSingleton
	{
		showOnAwake = show;
		T val = instance;
		if (val != null)
		{
			val.SetVisible(show);
		}
		else
		{
			val = UIBase.Spawn<T>(prefab);
		}
		return val;
	}
}
