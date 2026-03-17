using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISettings_Setting : UIBase
{
	private enum SettingType
	{
		Empty,
		ValueButton,
		Dropdown,
		ButtonOnly,
		InputField,
		ReadOnly,
		Text,
		InputConfig,
		Slider,
		InputFieldNonInteractable
	}

	[SerializeField]
	private TMP_Text headerText;

	[SerializeField]
	private TMP_Text valueText;

	[SerializeField]
	private TMP_Text textText;

	[SerializeField]
	private TMP_Text buttonText;

	[SerializeField]
	private TMP_Text buttonMiddleText;

	[SerializeField]
	private UIButton button;

	[SerializeField]
	private UIButton buttonMiddle;

	[SerializeField]
	private UIButton buttonClear;

	[SerializeField]
	private TMP_Dropdown valueDropdown;

	[SerializeField]
	private TMP_InputField valueInputField;

	[SerializeField]
	private Slider valueSlider;

	[SerializeField]
	private UITextImageButton valueAfter;

	[SerializeField]
	private AutoLoc headerAutoLoc;

	private Func<string> funcSetString;

	private Func<float> funcSetFloat;

	private Func<int> funcSetIndex;

	private Action<List<string>> funcSetItems;

	private Action<string> funcStringChanged;

	private Action<float> funcFloatChanged;

	private Action actionButtonClicked;

	private KoroutineId kidPolling;

	[NonSerialized]
	public InputAction inputAction;

	private SettingType settingType;

	private Canvas canvasPrev;

	public void InitValueButton(string header_code, Func<string> func_set_value, Action action_button_clicked)
	{
		InitType(SettingType.ValueButton);
		SetHeaderCode(header_code);
		funcSetString = func_set_value;
		actionButtonClicked = action_button_clicked;
		FillValue();
		button.Init(delegate
		{
			StartCoroutine(CClick());
		});
	}

	public void InitDropdown(string header_code, Action<List<string>> func_set_items, Func<int> func_set_index, Action<int> action_index_changed)
	{
		InitType(SettingType.Dropdown);
		SetHeaderCode(header_code);
		funcSetItems = func_set_items;
		funcSetIndex = func_set_index;
		FillItems();
		FillValue();
		valueDropdown.onValueChanged.AddListener(delegate(int index)
		{
			action_index_changed(index);
		});
	}

	public void InitToggle(string header_code, Func<bool> func_set, Action<bool> action_changed)
	{
		InitType(SettingType.Dropdown);
		SetHeaderCode(header_code);
		funcSetItems = delegate(List<string> strs)
		{
			strs.Add(Loc.GetUI("GENERIC_NO"));
			strs.Add(Loc.GetUI("GENERIC_YES"));
		};
		funcSetIndex = () => func_set() ? 1 : 0;
		FillItems();
		FillValue();
		valueDropdown.onValueChanged.AddListener(delegate(int index)
		{
			action_changed(index == 1);
		});
	}

	public void InitInputField(string header_code, Func<string> func_set_value, Action<string> action_value_changed)
	{
		InitType(SettingType.InputField);
		SetHeaderCode(header_code);
		funcSetString = func_set_value;
		funcStringChanged = action_value_changed;
		FillValue();
		valueInputField.onValueChanged.AddListener(delegate(string str)
		{
			funcStringChanged(str);
		});
	}

	public void InitReadOnly(string header_code, string value, bool selectable = false)
	{
		InitType(SettingType.ReadOnly);
		if (selectable)
		{
			valueInputField.SetObActive(active: true);
			valueInputField.text = value;
			valueInputField.readOnly = true;
		}
		else
		{
			valueText.SetObActive(active: true);
			valueText.text = value;
		}
		SetHeaderCode(header_code);
	}

	public void InitSlider(string header_code, float min, float max, Func<float> func_set_value, Action<float> action_value_changed)
	{
		InitType(SettingType.Slider);
		SetHeaderCode(header_code);
		funcSetFloat = func_set_value;
		funcFloatChanged = action_value_changed;
		valueSlider.minValue = min;
		valueSlider.maxValue = max;
		FillValue();
		valueSlider.onValueChanged.AddListener(delegate(float f)
		{
			funcFloatChanged(f);
			ShowSliderPercentage(f);
		});
	}

	private void ShowSliderPercentage(float f)
	{
		valueAfter.SetText(Loc.GetUI("GENERIC_PERCENTAGE", Mathf.Round(f * 100f).ToString()));
	}

	public void InitInputFieldNonInteractable(string header_code)
	{
		InitType(SettingType.InputFieldNonInteractable);
		SetHeaderCode(header_code);
	}

	public void InitInputConfig(string header_code, InputAction input_action, Action<bool> action_polling_changed)
	{
		InitType(SettingType.InputConfig);
		SetHeaderCode(header_code);
		funcSetString = () => InputManager.GetDesc(input_action);
		inputAction = input_action;
		buttonText.text = Loc.GetUI("GENERIC_CHANGE");
		FillValue();
		button.Init(delegate
		{
			ToggleInputPolling(input_action, action_polling_changed);
		});
		buttonClear.Init(delegate
		{
			InputManager.ClearConfig(input_action);
			ToggleInputPolling(input_action, action_polling_changed);
		});
	}

	public void InitInputConfig(string header_code, string hard_value)
	{
		InitType(SettingType.InputConfig);
		SetHeaderCode(header_code);
		funcSetString = () => hard_value;
		FillValue();
		button.SetVisible(vis: false);
	}

	public void InitButtonOnly(string button_text_code, Action action_button_clicked)
	{
		InitType(SettingType.ButtonOnly);
		actionButtonClicked = action_button_clicked;
		buttonMiddleText.text = Loc.GetUI(button_text_code);
		buttonMiddle.Init(delegate
		{
			actionButtonClicked();
		});
	}

	public void InitText(string text)
	{
		InitType(SettingType.Text);
		textText.text = text;
	}

	public void InitEmpty()
	{
		InitType(SettingType.Empty);
	}

	private IEnumerator CClick()
	{
		actionButtonClicked();
		yield return null;
		FillValue();
	}

	private void ToggleInputPolling(InputAction input_action, Action<bool> polling_changed)
	{
		if (!kidPolling.IsRunning())
		{
			StartKoroutine(KInputConfig(input_action, polling_changed), out kidPolling);
		}
		else
		{
			StopKoroutine(kidPolling);
		}
	}

	private IEnumerator KInputConfig(InputAction input_action, Action<bool> polling_changed)
	{
		KoroutineId kid = SetFinalizer(delegate
		{
			buttonText.text = Loc.GetUI("GENERIC_CHANGE");
			buttonClear.SetObActive(active: false);
			polling_changed(obj: false);
			FillValue();
		});
		try
		{
			FillValue(Loc.GetUI("SETTINGS_PRESS_KEY"));
			buttonText.text = Loc.GetUI("GENERIC_CANCEL");
			buttonClear.SetObActive(active: true);
			polling_changed(obj: true);
			while (true)
			{
				bool ignore_left_click = button.ContainsMouse() || buttonClear.ContainsMouse();
				if (!InputManager.Poll(input_action, ignore_left_click))
				{
					yield return null;
					continue;
				}
				break;
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public void SetButtonVisible(bool vis)
	{
		button.SetVisible(vis);
	}

	private void SetHeaderCode(string header_code)
	{
		if (header_code.StartsWith("_obs_"))
		{
			headerAutoLoc.code = header_code[5..];
			headerAutoLoc.type = LocType.OBJECT;
		}
		else
		{
			headerAutoLoc.code = header_code;
			headerAutoLoc.type = LocType.UI;
		}
		headerAutoLoc.FillText();
	}

	public void FillItems()
	{
		if (settingType == SettingType.Dropdown)
		{
			valueDropdown.ClearOptions();
			List<string> list = new List<string>();
			funcSetItems(list);
			valueDropdown.AddOptions(list);
		}
	}

	public void FillValue()
	{
		switch (settingType)
		{
		case SettingType.ValueButton:
		case SettingType.InputConfig:
			valueText.text = funcSetString();
			break;
		case SettingType.Dropdown:
			valueDropdown.SetValueWithoutNotify(funcSetIndex());
			break;
		case SettingType.InputField:
		case SettingType.InputFieldNonInteractable:
			valueInputField.SetTextWithoutNotify(funcSetString());
			break;
		case SettingType.Slider:
		{
			float num = funcSetFloat();
			valueSlider.SetValueWithoutNotify(num);
			ShowSliderPercentage(num);
			break;
		}
		case SettingType.ButtonOnly:
		case SettingType.ReadOnly:
		case SettingType.Text:
			break;
		}
	}

	public void FillValue(string str_override)
	{
		switch (settingType)
		{
		case SettingType.ValueButton:
		case SettingType.InputConfig:
			valueText.text = str_override;
			break;
		case SettingType.Dropdown:
			valueDropdown.captionText.text = str_override;
			break;
		case SettingType.InputField:
		case SettingType.InputFieldNonInteractable:
			valueInputField.SetTextWithoutNotify(str_override);
			break;
		case SettingType.ButtonOnly:
		case SettingType.ReadOnly:
		case SettingType.Text:
		case SettingType.Slider:
			break;
		}
	}

	public void SetValueError(bool error)
	{
		valueText.color = (error ? Color.red : Color.white);
	}

	public void UpdateLanguage()
	{
		FillItems();
		FillValue();
	}

	private void InitType(SettingType _type)
	{
		settingType = _type;
		headerText.SetObActive(_type != SettingType.Empty && _type != SettingType.ButtonOnly && _type != SettingType.Text);
		valueText.SetObActive(_type == SettingType.ValueButton || _type == SettingType.InputConfig);
		textText.SetObActive(_type == SettingType.Text);
		button.SetObActive(_type == SettingType.ValueButton || _type == SettingType.InputConfig);
		buttonMiddle.SetObActive(_type == SettingType.ButtonOnly);
		buttonClear.SetObActive(active: false);
		valueDropdown.SetObActive(_type == SettingType.Dropdown);
		valueInputField.SetObActive(_type == SettingType.InputField || _type == SettingType.InputFieldNonInteractable);
		valueAfter.SetObActive(_type == SettingType.Slider);
		valueSlider.SetObActive(_type == SettingType.Slider);
		valueInputField.readOnly = _type == SettingType.InputFieldNonInteractable;
		valueInputField.textComponent.color = ((_type == SettingType.InputFieldNonInteractable) ? Color.gray : Color.white);
	}

	public void SetReadOnly(bool read_only, string txt = "")
	{
		if (read_only)
		{
			valueText.SetObActive(active: true);
			button.SetObActive(active: false);
			valueDropdown.SetObActive(active: false);
			valueInputField.SetObActive(active: false);
			valueText.text = txt;
		}
		else
		{
			InitType(settingType);
		}
	}

	public void SetGrey(bool grey)
	{
		Color color = (grey ? Color.grey : Color.white);
		headerText.color = color;
		valueText.color = color;
	}

	private void Update()
	{
		Canvas componentInChildren = valueDropdown.GetComponentInChildren<Canvas>();
		if (componentInChildren != canvasPrev)
		{
			if (componentInChildren != null)
			{
				componentInChildren.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
			}
			canvasPrev = componentInChildren;
		}
	}
}
