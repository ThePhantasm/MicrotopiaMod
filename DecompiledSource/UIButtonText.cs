using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonText : UIButton
{
	[SerializeField]
	private TextMeshProUGUI lbText;

	public UIButton Init(Action _onClick, string text)
	{
		lbText.text = text;
		return Init(_onClick);
	}

	public void SetDisabled(bool disabled)
	{
		lbText.color = (disabled ? new Color(0.35f, 0.35f, 0.35f) : Color.white);
		if (TryGetComponent<Button>(out var component))
		{
			component.enabled = !disabled;
		}
		if (TryGetComponent<ButtonHover>(out var component2))
		{
			component2.enabled = !disabled;
		}
	}
}
