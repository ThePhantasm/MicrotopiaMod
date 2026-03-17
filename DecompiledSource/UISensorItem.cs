using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISensorItem : UIBase
{
	[Header("UISensorItem")]
	[SerializeField]
	private TextMeshProUGUI lbType;

	public UIButton btNot;

	public UIButton btDelete;

	[SerializeField]
	private TMP_Dropdown ddDropdown;

	[SerializeField]
	private Slider slValue;

	[SerializeField]
	private TMP_InputField inValue;

	[SerializeField]
	private TMP_InputField inPercentage;

	private TrailGateSensor trailGateSensor;

	private Dictionary<string, PickupType> dicTitlesPickupType;

	private Dictionary<string, AntCaste> dicTitlesAntCaste;

	public void Init()
	{
		btNot.Init(delegate
		{
			if (trailGateSensor != null)
			{
				trailGateSensor.not = !trailGateSensor.not;
				SetTitle(trailGateSensor);
			}
		});
		ddDropdown.onValueChanged.RemoveAllListeners();
		ddDropdown.onValueChanged.AddListener(delegate
		{
			ApplySensorData();
		});
		slValue.onValueChanged.RemoveAllListeners();
		slValue.onValueChanged.AddListener(delegate
		{
			ApplySliderValue();
		});
		inValue.onValueChanged.RemoveAllListeners();
		inValue.onValueChanged.AddListener(delegate
		{
			ApplyInputfieldValue();
		});
		inPercentage.onValueChanged.RemoveAllListeners();
		inPercentage.onValueChanged.AddListener(delegate
		{
			ApplyInputfieldPercentage();
		});
		dicTitlesPickupType = new Dictionary<string, PickupType>();
		foreach (PickupData pickup in PrefabData.pickups)
		{
			dicTitlesPickupType.Add(pickup.GetTitle(), pickup.type);
		}
		dicTitlesAntCaste = new Dictionary<string, AntCaste>();
		foreach (AntCasteData antCaste in PrefabData.antCastes)
		{
			if (antCaste.caste != AntCaste.QUEEN && antCaste.caste != AntCaste.CARGO_TRAIN)
			{
				dicTitlesAntCaste.Add(antCaste.GetTitle(), antCaste.caste);
			}
		}
	}

	public void Fill(TrailGateSensor sensor)
	{
		ddDropdown.SetObActive(active: false);
		slValue.SetObActive(active: false);
		inValue.SetObActive(active: false);
		inPercentage.SetObActive(active: false);
		btDelete.SetObActive(active: false);
		trailGateSensor = sensor;
		if (trailGateSensor == null)
		{
			return;
		}
		SetTitle(trailGateSensor);
		int value = -1;
		int num = 0;
		switch (trailGateSensor.sensorType)
		{
		case SensorType.IS_CARRYING_PICKUP:
			btDelete.SetObActive(active: true);
			break;
		case SensorType.IS_CARRYING_PICKUP_TYPE:
			ddDropdown.SetObActive(active: true);
			ddDropdown.ClearOptions();
			foreach (KeyValuePair<string, PickupType> item in dicTitlesPickupType)
			{
				ddDropdown.AddOptions(new List<string> { item.Key });
				if (item.Value == trailGateSensor.pickupType)
				{
					value = num;
				}
				num++;
			}
			ddDropdown.value = value;
			{
				foreach (TMP_Dropdown.OptionData option in ddDropdown.options)
				{
					_ = option;
					btDelete.SetObActive(active: true);
				}
				break;
			}
		case SensorType.IS_CASTE:
			ddDropdown.SetObActive(active: true);
			ddDropdown.ClearOptions();
			foreach (KeyValuePair<string, AntCaste> item2 in dicTitlesAntCaste)
			{
				ddDropdown.AddOptions(new List<string> { item2.Key });
				if (item2.Value == trailGateSensor.antCaste)
				{
					value = num;
				}
				num++;
			}
			ddDropdown.value = value;
			btDelete.SetObActive(active: true);
			break;
		case SensorType.ENERGY_HIGHER_THAN:
			slValue.SetObActive(active: true);
			inValue.SetObActive(active: true);
			btDelete.SetObActive(active: true);
			inValue.text = trailGateSensor.floatValue.ToString();
			slValue.value = trailGateSensor.floatValue / trailGateSensor.GetMaxValue();
			break;
		case SensorType.ENERGY_LOWER_THAN:
			slValue.SetObActive(active: true);
			inValue.SetObActive(active: true);
			btDelete.SetObActive(active: true);
			inValue.text = trailGateSensor.floatValue.ToString();
			slValue.value = trailGateSensor.floatValue / trailGateSensor.GetMaxValue();
			break;
		case SensorType.ONE_IN_N:
			slValue.SetObActive(active: true);
			inValue.SetObActive(active: true);
			btDelete.SetObActive(active: true);
			inValue.text = trailGateSensor.intValue.ToString();
			slValue.value = (float)trailGateSensor.intValue / trailGateSensor.GetMaxValue();
			break;
		case SensorType.RANDOM_PERCENTAGE:
			slValue.SetObActive(active: true);
			inPercentage.SetObActive(active: true);
			btDelete.SetObActive(active: true);
			inPercentage.text = trailGateSensor.floatValue.ToString();
			slValue.value = trailGateSensor.floatValue / trailGateSensor.GetMaxValue();
			break;
		}
	}

	public void SetRemove(Action _remove)
	{
		btDelete.Init(_remove);
	}

	private void ApplySliderValue()
	{
		string text = Mathf.RoundToInt(trailGateSensor.GetMaxValue() * slValue.value).ToString();
		inValue.text = text;
		inPercentage.text = text;
		ApplySensorData();
	}

	private void ApplyInputfieldValue()
	{
		slValue.value = inValue.text.ToFloat(0f) / trailGateSensor.GetMaxValue();
		ApplySensorData();
	}

	private void ApplyInputfieldPercentage()
	{
		slValue.value = inPercentage.text.ToFloat(0f) / trailGateSensor.GetMaxValue();
		ApplySensorData();
	}

	private void ApplySensorData()
	{
		if (trailGateSensor != null)
		{
			switch (trailGateSensor.sensorType)
			{
			case SensorType.IS_CARRYING_PICKUP_TYPE:
				trailGateSensor.pickupType = dicTitlesPickupType[ddDropdown.options[ddDropdown.value].text];
				break;
			case SensorType.IS_CASTE:
				trailGateSensor.antCaste = dicTitlesAntCaste[ddDropdown.options[ddDropdown.value].text];
				break;
			case SensorType.ENERGY_HIGHER_THAN:
				trailGateSensor.floatValue = inPercentage.text.ToFloat(0f);
				break;
			case SensorType.ENERGY_LOWER_THAN:
				trailGateSensor.floatValue = inPercentage.text.ToFloat(0f);
				break;
			case SensorType.ONE_IN_N:
				trailGateSensor.intValue = inValue.text.ToInt(0);
				break;
			case SensorType.RANDOM_PERCENTAGE:
				trailGateSensor.floatValue = inPercentage.text.ToFloat(0f);
				break;
			case SensorType.IS_CARRYING_PICKUP:
				break;
			}
		}
	}

	private void SetTitle(TrailGateSensor sensor)
	{
		lbType.text = sensor.sensorType.GetTitle(sensor.not);
	}
}
