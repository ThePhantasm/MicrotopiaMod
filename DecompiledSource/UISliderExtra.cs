using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISliderExtra : MonoBehaviour
{
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private UIHoldButton btDecrease;

	[SerializeField]
	private UIHoldButton btIncrease;

	private int maxValue;

	private Func<int> getValue;

	private Action<int> setValue;

	private Func<int, string> overrideInputText;

	private SliderCurve sliderCurve;

	public void Init(int max_value, Func<int> get_value, Action<int> set_value, SliderCurve curve = SliderCurve.Linear)
	{
		maxValue = max_value;
		getValue = get_value;
		setValue = set_value;
		overrideInputText = null;
		sliderCurve = curve;
		slider.onValueChanged.RemoveAllListeners();
		inputField.onValueChanged.RemoveAllListeners();
		UpdateValue();
		slider.onValueChanged.AddListener(delegate(float f)
		{
			int num = SliderToValue(f);
			ShowInput(num);
			setValue(num);
		});
		inputField.onValueChanged.AddListener(delegate(string txt)
		{
			int num = txt.ToInt(0);
			if (num < 0)
			{
				num = -num;
				ShowInput(num);
			}
			slider.SetValueWithoutNotify(ValueToSlider(num));
			setValue(num);
		});
		btDecrease.Init(delegate(bool first)
		{
			Delta(-1, first);
		});
		btIncrease.Init(delegate(bool first)
		{
			Delta(1, first);
		});
	}

	public void UpdateValue()
	{
		int value = getValue();
		slider.value = ValueToSlider(value);
		inputField.text = value.ToString();
	}

	private void Delta(int d, bool first)
	{
		int num = getValue();
		if (first)
		{
			num += d;
		}
		else
		{
			float num2 = ValueToSlider(num);
			num2 += 0.01f * (float)d;
			num2 = Mathf.Clamp01(num2);
			int num3 = SliderToValue(num2);
			if (num3 == num)
			{
				num3 = num + d;
			}
			num = num3;
		}
		num = Mathf.Clamp(num, 0, maxValue);
		ShowInput(num);
		slider.SetValueWithoutNotify(ValueToSlider(num));
		setValue(num);
	}

	private void ShowInput(int value)
	{
		string textWithoutNotify = ((overrideInputText != null) ? overrideInputText(value) : value.ToString());
		inputField.SetTextWithoutNotify(textWithoutNotify);
	}

	public void OverrideInputField(Func<int, string> override_text)
	{
		overrideInputText = override_text;
		inputField.interactable = false;
		inputField.onValueChanged.RemoveAllListeners();
		ShowInput(getValue());
	}

	private float ValueToSlider(int value)
	{
		float num = Mathf.Clamp01((float)value / (float)maxValue);
		if (sliderCurve == SliderCurve.Quadratic)
		{
			num = Mathf.Sqrt(num);
		}
		return num;
	}

	private int SliderToValue(float f)
	{
		if (sliderCurve == SliderCurve.Quadratic)
		{
			f *= f;
		}
		return Mathf.RoundToInt((float)maxValue * f);
	}
}
