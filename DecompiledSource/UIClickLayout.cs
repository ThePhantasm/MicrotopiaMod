using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIClickLayout : UIBase
{
	[Space(10f)]
	public UIClickType type;

	[SerializeField]
	private TextMeshProUGUI lbTitle;

	[SerializeField]
	private RectTransform rtInfo;

	[SerializeField]
	private TextMeshProUGUI lbInfo;

	[SerializeField]
	private List<ButtonWithHotkey> buttonsWithHotkey = new List<ButtonWithHotkey>();

	[SerializeField]
	private List<Checkbox> checkBoxes = new List<Checkbox>();

	[SerializeField]
	private UIClick_BottomButtons bottomButtons;

	public virtual void Init()
	{
	}

	public virtual void Clear()
	{
	}

	public void SetTitle(string _title)
	{
		lbTitle.text = _title;
	}

	public void SetInfo(string txt)
	{
		if (!(rtInfo == null) && !(lbInfo == null))
		{
			if (txt == "")
			{
				rtInfo.SetObActive(active: false);
				return;
			}
			rtInfo.SetObActive(active: true);
			lbInfo.text = txt;
		}
	}

	public ButtonWithHotkey GetButton(UIClickButtonType button_type, bool show_button_error = true)
	{
		foreach (ButtonWithHotkey item in buttonsWithHotkey)
		{
			if (item.buttonType == button_type)
			{
				return item;
			}
		}
		if (bottomButtons != null)
		{
			foreach (ButtonWithHotkey item2 in bottomButtons.buttonsWithHotkey)
			{
				if (item2.buttonType == button_type)
				{
					return item2;
				}
			}
		}
		if (show_button_error)
		{
			Debug.LogError($"{base.name}: Button {button_type} not set up", base.gameObject);
		}
		return null;
	}

	public void SetButton(UIClickButtonType button_type, Action on_click, InputAction input_action)
	{
		GetButton(button_type)?.SetButton(on_click, input_action);
	}

	public void SetButtonHover(UIClickButtonType button_type, string loc_txt)
	{
		ButtonWithHotkey button = GetButton(button_type);
		if (button != null && button.btButton_better != null)
		{
			button.btButton_better.SetHoverLocUI(loc_txt);
		}
	}

	public void UpdateButton(UIClickButtonType button_type, bool enabled, string txt = "", bool show_button_error = true)
	{
		ButtonWithHotkey button = GetButton(button_type, show_button_error);
		if (button != null)
		{
			if (button.btButton_better != null)
			{
				button.btButton_better.SetObActive(enabled);
			}
			if (button.btButton != null)
			{
				button.btButton.SetObActive(enabled);
			}
			if (button.lbButton != null)
			{
				button.lbButton.text = txt;
			}
		}
	}

	public bool ClickButton(UIClickButtonType button_type)
	{
		ButtonWithHotkey button = UIGame.instance.uiClick.currentLayout.GetButton(button_type, show_button_error: false);
		if (button == null)
		{
			return false;
		}
		if (button.btButton_better != null && button.btButton_better.gameObject.activeInHierarchy && button.btButton_better.IsInteractable())
		{
			button.btButton_better.Click();
			return true;
		}
		if (button.btButton != null && button.btButton.gameObject.activeInHierarchy && button.btButton.button.interactable)
		{
			button.btButton.Click();
			return true;
		}
		return false;
	}

	public Checkbox GetCheckbox(int i)
	{
		if (i >= checkBoxes.Count)
		{
			Debug.LogError(base.name + " checkbox " + i + " not set up");
			return null;
		}
		return checkBoxes[i];
	}

	public void SetCheckbox(int i, bool current, Action<bool> action_changed)
	{
		Checkbox checkbox = GetCheckbox(i);
		if (checkbox != null && checkbox.toggleBox != null)
		{
			Toggle toggleBox = checkBoxes[i].toggleBox;
			toggleBox.onValueChanged.RemoveAllListeners();
			toggleBox.isOn = current;
			toggleBox.onValueChanged.AddListener(delegate(bool is_on)
			{
				action_changed(is_on);
			});
		}
	}

	public void UpdateCheckbox(int i, bool enabled, string txt = "")
	{
		if (i < checkBoxes.Count)
		{
			Checkbox checkbox = checkBoxes[i];
			if (checkbox.rtCheckbox != null)
			{
				checkbox.rtCheckbox.SetObActive(enabled);
			}
			if (checkbox.lbCheckbox != null)
			{
				checkbox.lbCheckbox.text = txt;
			}
		}
	}
}
