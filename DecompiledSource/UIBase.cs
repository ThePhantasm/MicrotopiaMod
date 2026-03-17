using System;
using UnityEngine;
using UnityEngine.UI;

public class UIBase : KoroutineBehaviour
{
	public RectTransform rtBase;

	public UILayer layer;

	private void Awake()
	{
		MyAwake();
	}

	public virtual void SetPosition(Vector3 pos)
	{
		rtBase.position = pos;
	}

	public virtual void SetLocalPosition(Vector3 pos)
	{
		rtBase.localPosition = pos;
	}

	public Vector3 GetPositionFromWorld(Vector3 world_pos)
	{
		return Camera.main.WorldToScreenPoint(world_pos);
	}

	public virtual void Show(bool target)
	{
		base.gameObject.SetObActive(target);
	}

	public bool IsShown()
	{
		return base.gameObject.activeSelf;
	}

	protected virtual void MyAwake()
	{
	}

	protected void RegisterButton(Button bt, Action onClick, string txt = null)
	{
		bt.onClick.AddListener(delegate
		{
			onClick();
		});
		if (txt != null)
		{
			bt.GetComponentInChildren<Text>().text = txt;
		}
	}

	public virtual void StartClose()
	{
		Close();
	}

	protected virtual void Close()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public static T Spawn<T>(GameObject prefab = null) where T : UIBase
	{
		if (prefab == null)
		{
			prefab = AssetLinks.standard.GetPrefab(typeof(T));
		}
		UILayer uILayer = UILayer.LAYER_GAME_FRONT;
		UIBase component = prefab.GetComponent<UIBase>();
		if (component != null)
		{
			uILayer = component.layer;
		}
		Transform parent = UIGlobal.instance.GetLayer(uILayer);
		T component2 = UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays: false).GetComponent<T>();
		if (component2 == null)
		{
			Debug.LogError("UIBase.Spawn(" + prefab.name + "): couldn't find " + typeof(T)?.ToString() + " in root of prefab");
		}
		else
		{
			component2.OnSpawn();
		}
		return component2;
	}

	protected virtual void OnSpawn()
	{
	}

	protected virtual AudioClip GetSpawnAudio()
	{
		return null;
	}
}
