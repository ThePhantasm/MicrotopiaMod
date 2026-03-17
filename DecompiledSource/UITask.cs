using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITask : UIBase
{
	[SerializeField]
	private RectTransform rtBodyHeight;

	[SerializeField]
	private List<UIButton> btsToggleOpen;

	[SerializeField]
	private VerticalLayoutGroup vlgBody;

	[SerializeField]
	private List<GameObject> obsToggleOpen;

	[SerializeField]
	private List<GameObject> obsRandomizeOnOpen;

	protected bool open = true;

	private float openTime;

	private float openTimeTotal = 0.15f;

	public virtual void Init(Action on_click_toggleOpen)
	{
		foreach (UIButton item in btsToggleOpen)
		{
			item.Init(on_click_toggleOpen);
		}
	}

	public virtual void UIUpdate()
	{
		if (open && (float)vlgBody.padding.bottom != 0f)
		{
			openTime = Mathf.Clamp(openTime - Time.deltaTime, 0f, openTimeTotal);
			vlgBody.padding.bottom = Mathf.RoundToInt(Mathf.Lerp(0f, GetHeight(), openTime / openTimeTotal));
			LayoutRebuilder.ForceRebuildLayoutImmediate(rtBase);
		}
		if (!open && vlgBody.padding.bottom != GetHeight())
		{
			openTime = Mathf.Clamp(openTime + Time.deltaTime, 0f, openTimeTotal);
			vlgBody.padding.bottom = Mathf.RoundToInt(Mathf.Lerp(0f, GetHeight(), openTime / openTimeTotal));
			LayoutRebuilder.ForceRebuildLayoutImmediate(rtBase);
		}
	}

	private int GetHeight()
	{
		return -Mathf.RoundToInt(rtBodyHeight.sizeDelta.y);
	}

	public void ToggleOpen()
	{
		open = !open;
		SetToggleOpenObs();
	}

	public virtual void Open(bool target, bool instant = false)
	{
		if (instant || (target && open))
		{
			vlgBody.padding.bottom = Mathf.RoundToInt(target ? ((float)GetHeight()) : 0f);
			LayoutRebuilder.ForceRebuildLayoutImmediate(rtBase);
		}
		open = target;
		SetToggleOpenObs();
	}

	public bool IsOpen()
	{
		return open;
	}

	private void SetToggleOpenObs()
	{
		foreach (GameObject item in obsToggleOpen)
		{
			item.SetObActive(open);
		}
		if (!open)
		{
			return;
		}
		foreach (GameObject item2 in obsRandomizeOnOpen)
		{
			item2.SetObActive(Toolkit.CoinFlip());
		}
	}

	public virtual TaskID GetUID()
	{
		return default(TaskID);
	}
}
