using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILogicControl : UIBase
{
	[SerializeField]
	private List<UIClickLayout> layouts;

	[HideInInspector]
	public UIClickLayout currentLayout;

	[Space(10f)]
	[SerializeField]
	private Vector2 moveDelta;

	[SerializeField]
	private float moveTime = 0.25f;

	private Coroutine cAnim;

	public void Init(UIClickType _type)
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

	private IEnumerator CAnimOpenClick()
	{
		Vector2 start = moveDelta;
		Vector2 end = Vector2.zero;
		for (float t = 0f; t < moveTime; t += Time.deltaTime)
		{
			rtBase.anchoredPosition = start + (end - start) * GlobalValues.standard.curveEaseIn.Evaluate(t / moveTime);
			yield return null;
		}
		rtBase.anchoredPosition = end;
	}

	public void UpdateLogicControl(TrailGate _gate)
	{
		if (!(currentLayout == null) && base.isActiveAndEnabled)
		{
			_gate.UpdateClickUi(currentLayout);
		}
	}
}
