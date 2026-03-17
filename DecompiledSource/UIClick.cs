using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIClick : UIBase
{
	[SerializeField]
	private List<UIClickLayout> layouts = new List<UIClickLayout>();

	[HideInInspector]
	public UIClickLayout currentLayout;

	[Space(10f)]
	[SerializeField]
	private float deltaY = -300f;

	[SerializeField]
	private float moveTime = 0.5f;

	private Coroutine cAnim;

	public void Init()
	{
		foreach (UIClickLayout layout in layouts)
		{
			layout.Init();
		}
	}

	public void Setup(UIClickType _type)
	{
		currentLayout = null;
		foreach (UIClickLayout layout in layouts)
		{
			layout.Clear();
			layout.SetObActive(active: false);
			if (layout.type == _type)
			{
				if (currentLayout != null)
				{
					Debug.LogWarning("Found multiple UIClickLayouts of type " + _type);
				}
				currentLayout = layout;
			}
		}
		if (currentLayout == null)
		{
			Debug.LogError("Didn't find UIClickLayout for " + _type);
			return;
		}
		currentLayout.SetObActive(active: true);
		if (cAnim != null)
		{
			StopCoroutine(cAnim);
		}
		cAnim = StartCoroutine(CAnimOpenClick());
	}

	public void Clear()
	{
		foreach (UIClickLayout layout in layouts)
		{
			layout.Clear();
			layout.SetObActive(active: false);
		}
	}

	private IEnumerator CAnimOpenClick()
	{
		float start = deltaY;
		float end = 0f;
		if (!(Time.deltaTime > moveTime))
		{
			for (float t = 0f; t < moveTime; t += Time.deltaTime)
			{
				Vector2 anchoredPosition = rtBase.anchoredPosition;
				anchoredPosition.y = start + (end - start) * GlobalValues.standard.curveEaseIn.Evaluate(t / moveTime);
				rtBase.anchoredPosition = anchoredPosition;
				yield return null;
			}
			Vector2 anchoredPosition2 = rtBase.anchoredPosition;
			anchoredPosition2.y = end;
			rtBase.anchoredPosition = anchoredPosition2;
		}
	}
}
