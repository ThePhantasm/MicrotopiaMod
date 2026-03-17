using System;
using TMPro;
using UnityEngine;

[Serializable]
public class ButtonWithHotkey
{
	public UIClickButtonType buttonType;

	public UIButton btButton;

	public UITextImageButton btButton_better;

	public TMP_Text lbButton;

	public GameObject obHotkey;

	public TMP_Text lbHotkey;

	public void SetButton(Action on_click, InputAction input_action)
	{
		if (btButton != null)
		{
			if (on_click != null)
			{
				btButton.SetObActive(active: true);
				btButton.Init(on_click);
				if (obHotkey != null)
				{
					Toolkit.SetHotkeyButton(obHotkey, lbHotkey, InputManager.GetDesc(input_action));
				}
			}
			else
			{
				btButton.SetObActive(active: false);
			}
		}
		if (!(btButton_better != null))
		{
			return;
		}
		if (on_click != null)
		{
			btButton_better.SetObActive(active: true);
			btButton_better.SetButton(on_click);
			string desc = InputManager.GetDesc(input_action);
			if (obHotkey != null)
			{
				obHotkey.SetObActive(desc != "");
				lbHotkey.text = desc;
			}
		}
		else
		{
			btButton_better.SetObActive(active: false);
		}
	}

	public void SetInteractable(bool i)
	{
		if (btButton != null)
		{
			btButton.interactable = i;
		}
		if (btButton_better != null)
		{
			btButton_better.SetInteractable(i);
		}
	}

	public void SetHover(string loc_txt)
	{
		if (btButton_better != null)
		{
			btButton_better.SetHoverLocUI(loc_txt);
		}
	}
}
