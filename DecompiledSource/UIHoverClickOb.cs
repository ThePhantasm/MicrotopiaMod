using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIHoverClickOb : UIBaseSingleton
{
	[Serializable]
	public class StatusEffectUI
	{
		public StatusEffect effect;

		public UITextImageButton obEffect;
	}

	public static UIHoverClickOb instance;

	[SerializeField]
	private VerticalLayoutGroup verticalLayoutGroup;

	[SerializeField]
	private LayoutMaxSize layoutMaxSize;

	[SerializeField]
	private RectTransform rtDescription;

	[SerializeField]
	private RectTransform rtInfo;

	[SerializeField]
	private RectTransform rtCapabilities;

	[SerializeField]
	private RectTransform rtEnergy;

	[SerializeField]
	private RectTransform rtHealth;

	[SerializeField]
	private RectTransform rtRecipe;

	[SerializeField]
	private RectTransform rtSensors;

	[SerializeField]
	private RectTransform rtAddSensorItems;

	[SerializeField]
	private RectTransform rtAssigner;

	[SerializeField]
	private RectTransform rtIcons;

	[SerializeField]
	private RectTransform rtButtonWithText;

	[SerializeField]
	private RectTransform rtExchangePoints;

	[SerializeField]
	private RectTransform rtBottomButtons;

	[SerializeField]
	private RectTransform rtCargoStation;

	[SerializeField]
	private RectTransform rtInventory;

	[SerializeField]
	private RectTransform rtSlots;

	[SerializeField]
	private RectTransform rtSlotsContent;

	[SerializeField]
	private RectTransform rtPausePlay;

	[SerializeField]
	private RectTransform rtPause;

	[SerializeField]
	private RectTransform rtPlay;

	[SerializeField]
	private RectTransform rtStatusImage;

	[SerializeField]
	private RectTransform rtGateLife;

	[SerializeField]
	private RectTransform rtGateCarryCaste;

	[SerializeField]
	private RectTransform rtCannon;

	[SerializeField]
	private TextMeshProUGUI lbTitle;

	[SerializeField]
	private TextMeshProUGUI lbDescription;

	[SerializeField]
	private TextMeshProUGUI lbInfo;

	[SerializeField]
	private TextMeshProUGUI lbCapabilities;

	[SerializeField]
	private TextMeshProUGUI lbEnergyName;

	[SerializeField]
	private TextMeshProUGUI lbEnergyAmount;

	[SerializeField]
	private TextMeshProUGUI lbHealthName;

	[SerializeField]
	private TextMeshProUGUI lbHealthAmount;

	[SerializeField]
	private TextMeshProUGUI lbHealthStatus;

	[SerializeField]
	private TextMeshProUGUI lbButtonWithText;

	[SerializeField]
	private TextMeshProUGUI lbIcons;

	[SerializeField]
	private TextMeshProUGUI lbCargoLoadingWait;

	[SerializeField]
	private TextMeshProUGUI lbCargoUnloadingWait;

	[SerializeField]
	private TextMeshProUGUI lbInventory;

	[SerializeField]
	private TextMeshProUGUI lbSlots;

	[SerializeField]
	private TextMeshProUGUI lbGateLife;

	[SerializeField]
	private TextMeshProUGUI lbGateLifeToggle;

	[SerializeField]
	private TextMeshProUGUI lbGateCarryCaste;

	[SerializeField]
	private UIButton btAddSensor;

	[SerializeField]
	private UIButton btButtonWithText;

	[SerializeField]
	private UIButton btDelete;

	[SerializeField]
	private UIButton btRelocate;

	[SerializeField]
	private UIButton btFollow;

	[SerializeField]
	private UIButton btCargoLoadingWait;

	[SerializeField]
	private UIButton btCargoUnloadingWait;

	[SerializeField]
	private UIButton btPausePlay;

	[SerializeField]
	private UIButton btToggleGateLife;

	[SerializeField]
	private UIButton btToggleGatNot;

	[SerializeField]
	private GameObject obHotkeyButtonWithText;

	[SerializeField]
	private GameObject obHotkeyDelete;

	[SerializeField]
	private GameObject obHotkeyRelocate;

	[SerializeField]
	private GameObject obHotkeyFollow;

	[SerializeField]
	private TMP_Text lbHotkeyButtonWithText;

	[SerializeField]
	private TMP_Text lbHotkeyDelete;

	[SerializeField]
	private TMP_Text lbHotkeyRelocate;

	[SerializeField]
	private TMP_Text lbHotkeyFollow;

	[SerializeField]
	private UIButtonText btSensorItemPrefab;

	[SerializeField]
	private UILoadingBar uiEnergyBar;

	[SerializeField]
	private UILoadingBar uiHealthBar;

	[SerializeField]
	private UILoadingBar uiRadDeathBar;

	[SerializeField]
	private UIRecipe uiRecipe;

	[SerializeField]
	private UISensorItem uiSensorItemPrefab;

	[SerializeField]
	private UITextImageButton prefabExchangeType;

	[SerializeField]
	private UITextImageButton statusHoverTarget;

	[SerializeField]
	private UITextImageButton btIcons;

	[SerializeField]
	private UITextImageButton btGateCarryCaste;

	[SerializeField]
	private UITextBox prefabSlotItem;

	[SerializeField]
	private TMP_Dropdown ddChooseRecipe;

	[SerializeField]
	private GridLayoutGroup gridIcons;

	[SerializeField]
	private GridLayoutGroup gridGateCarryCaste;

	[SerializeField]
	[FormerlySerializedAs("gridInventory")]
	private GridLayoutGroup inventoryGridLayoutGroup;

	[SerializeField]
	private List<BillboardScreen> billboardImages = new List<BillboardScreen>();

	[SerializeField]
	private TMP_InputField inCrewSize;

	[SerializeField]
	private TMP_InputField inGateLife;

	[SerializeField]
	private Slider slCrewSize;

	[SerializeField]
	private Slider slGateLife;

	[SerializeField]
	private Slider slCannonRot;

	[SerializeField]
	private Slider slCannonAngle;

	[SerializeField]
	private Slider slCannonPower;

	[SerializeField]
	private List<StatusEffectUI> listStatusEffects = new List<StatusEffectUI>();

	private ClickableObject selectedOb;

	private UIItemMenu uiItemMenu;

	private UIRecipeMenu uiRecipeMenu;

	private List<UISensorItem> uiSensorItems = new List<UISensorItem>();

	private List<TrailGateSensor> currentSensors;

	private List<UITextImageButton> spawnedExchangeItems = new List<UITextImageButton>();

	private List<UIIconItem> spawnedIcons = new List<UIIconItem>();

	private List<UIIconItem> spawnedSlotItemIcons = new List<UIIconItem>();

	private List<UIIconItem> spawnedGateItems = new List<UIIconItem>();

	private List<UITextBox> spawnedSlotItems = new List<UITextBox>();

	[NonSerialized]
	public UIIconGrid inventoryGrid;

	private string statusText = "";

	private Color statusColor = Color.white;

	private Coroutine cSetSelected;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	protected override void MyAwake()
	{
		base.MyAwake();
		inventoryGrid = new UIIconGrid(lbInventory, inventoryGridLayoutGroup, keep_constraints: true);
	}

	public void Init()
	{
		prefabExchangeType.SetObActive(active: false);
		btSensorItemPrefab.SetObActive(active: false);
		foreach (SensorType sensorType in Enum.GetValues(typeof(SensorType)))
		{
			if (sensorType != SensorType.NONE)
			{
				UIButtonText component = UnityEngine.Object.Instantiate(btSensorItemPrefab, btSensorItemPrefab.transform.parent).GetComponent<UIButtonText>();
				component.Init(delegate
				{
					AddSensorItem(sensorType);
				}, sensorType.GetTitle());
				component.SetObActive(active: true);
			}
		}
		btAddSensor.Init(delegate
		{
			rtAddSensorItems.SetObActive(active: true);
		});
		rtAddSensorItems.SetObActive(active: false);
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			StatusEffectData statusEffectData = StatusEffectData.Get(listStatusEffect.effect);
			listStatusEffect.obEffect.SetText(statusEffectData.GetTitle());
			listStatusEffect.obEffect.SetHoverText(statusEffectData.GetHover());
		}
	}

	private void ClearHover()
	{
		lbTitle.text = "!TITLE MISSING!";
		rtDescription.SetObActive(active: false);
		rtInfo.SetObActive(active: false);
		rtCapabilities.SetObActive(active: false);
		rtEnergy.SetObActive(active: false);
		rtHealth.SetObActive(active: false);
		rtRecipe.SetObActive(active: false);
		rtSensors.SetObActive(active: false);
		rtIcons.SetObActive(active: false);
		rtExchangePoints.SetObActive(active: false);
		rtButtonWithText.SetObActive(active: false);
		rtBottomButtons.SetObActive(active: false);
		uiSensorItemPrefab.SetObActive(active: false);
		rtAssigner.SetObActive(active: false);
		rtCargoStation.SetObActive(active: false);
		rtInventory.SetObActive(active: false);
		rtSlots.SetObActive(active: false);
		rtPausePlay.SetObActive(active: false);
		rtStatusImage.SetObActive(active: false);
		rtGateLife.SetObActive(active: false);
		rtGateCarryCaste.SetObActive(active: false);
		rtCannon.SetObActive(active: false);
		if (uiItemMenu != null)
		{
			UnityEngine.Object.Destroy(uiItemMenu.gameObject);
		}
		if (uiRecipeMenu != null)
		{
			UnityEngine.Object.Destroy(uiRecipeMenu.gameObject);
		}
		uiRecipeMenu = null;
		uiItemMenu = null;
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			listStatusEffect.obEffect.SetObActive(active: false);
		}
	}

	private void ResetSize()
	{
		verticalLayoutGroup.enabled = false;
		layoutMaxSize.enabled = false;
		rtBase.sizeDelta = Vector2.zero;
		Canvas.ForceUpdateCanvases();
		verticalLayoutGroup.enabled = true;
		Canvas.ForceUpdateCanvases();
		layoutMaxSize.enabled = true;
		Canvas.ForceUpdateCanvases();
	}

	public void SetSelected(ClickableObject ob)
	{
		if (selectedOb != ob)
		{
			if (cSetSelected != null)
			{
				UIGlobal.instance.StopCoroutine(cSetSelected);
			}
			cSetSelected = UIGlobal.instance.StartCoroutine(CSetSelected(ob));
		}
	}

	private IEnumerator CSetSelected(ClickableObject ob)
	{
		if (selectedOb != null && ob != null)
		{
			SelectOb(null);
			yield return null;
		}
		SelectOb(ob);
	}

	private void SelectOb(ClickableObject ob)
	{
		selectedOb = ob;
		ClearHover();
		Show(ob != null);
		if (ob != null)
		{
			ResetSize();
			ob.SetHoverUI(this);
		}
	}

	public static ClickableObject GetSelected()
	{
		if (instance == null)
		{
			return null;
		}
		return instance.selectedOb;
	}

	public void SetTitle(string _title)
	{
		lbTitle.text = _title;
	}

	public void SetDescription(string _desc)
	{
		lbDescription.text = _desc;
		rtDescription.SetObActive(active: true);
	}

	public void UpdateDescription(string _desc)
	{
		lbDescription.text = _desc;
		rtDescription.SetObActive(_desc != "");
	}

	public void SetInfo()
	{
		rtInfo.SetObActive(active: true);
	}

	public void UpdateInfo(string _info)
	{
		lbInfo.text = _info;
	}

	public void SetInventory()
	{
		rtInventory.SetObActive(active: true);
	}

	public void SetSlots()
	{
		rtSlots.SetObActive(active: true);
		prefabSlotItem.SetObActive(active: false);
	}

	public void UpdateSlots(string _title, int n_slots, List<AntCaste> _ants, List<string> slot_names = null)
	{
		UpdateSlots(_title, n_slots, null, _ants, slot_names);
	}

	public void UpdateSlots(string _title, int n_slots, List<PickupType> _pickups, List<string> slot_names = null)
	{
		UpdateSlots(_title, n_slots, _pickups, null, slot_names);
	}

	private void UpdateSlots(string _title, int n_slots, List<PickupType> _pickups = null, List<AntCaste> _ants = null, List<string> slot_names = null)
	{
		lbSlots.Set(_title);
		if (spawnedSlotItems.Count < n_slots)
		{
			int num = n_slots - spawnedSlotItems.Count;
			for (int i = 0; i < num; i++)
			{
				UITextBox component = UnityEngine.Object.Instantiate(prefabSlotItem, rtSlotsContent).GetComponent<UITextBox>();
				spawnedSlotItems.Add(component);
				UIIconItem component2 = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), component.obBox.transform).GetComponent<UIIconItem>();
				spawnedSlotItemIcons.Add(component2);
			}
		}
		foreach (UITextBox spawnedSlotItem in spawnedSlotItems)
		{
			spawnedSlotItem.SetObActive(active: false);
		}
		foreach (UIIconItem spawnedSlotItemIcon in spawnedSlotItemIcons)
		{
			spawnedSlotItemIcon.SetObActive(active: false);
		}
		for (int j = 0; j < n_slots; j++)
		{
			spawnedSlotItems[j].SetObActive(active: true);
			string text = "";
			if (slot_names != null && j < slot_names.Count)
			{
				text = slot_names[j];
			}
			if (text == "")
			{
				spawnedSlotItems[j].listText[0].SetObActive(active: false);
			}
			else
			{
				spawnedSlotItems[j].listText[0].text = text;
				spawnedSlotItems[j].listText[0].SetObActive(active: true);
			}
			if (_pickups != null && j < _pickups.Count && _pickups[j] != PickupType.NONE)
			{
				spawnedSlotItemIcons[j].SetObActive(active: true);
				spawnedSlotItemIcons[j].Init(_pickups[j]);
				spawnedSlotItemIcons[j].SetHoverLocObjects(PickupData.Get(_pickups[j]).title);
				spawnedSlotItemIcons[j].SetRaycastTarget(target: true);
			}
			else if (_ants != null && j < _ants.Count && _ants[j] != AntCaste.NONE)
			{
				spawnedSlotItemIcons[j].SetObActive(active: true);
				spawnedSlotItemIcons[j].Init(_ants[j]);
				spawnedSlotItemIcons[j].SetHoverLocObjects(AntCasteData.Get(_ants[j]).title);
				spawnedSlotItemIcons[j].SetRaycastTarget(target: true);
			}
			else
			{
				spawnedSlotItemIcons[j].SetObActive(active: false);
			}
		}
	}

	public void SetCapabilities(string desc)
	{
		rtCapabilities.SetObActive(active: true);
		lbCapabilities.text = desc;
		foreach (UITextImageButton spawnedExchangeItem in spawnedExchangeItems)
		{
			spawnedExchangeItem.SetObActive(active: false);
		}
	}

	public void AddCapability(TrailType _type)
	{
		AddCapability(TrailData.Get(_type).GetTitle(), AssetLinks.standard.GetTrailMaterial(_type).color);
	}

	public void AddCapability(string s, Color col)
	{
		UITextImageButton uITextImageButton = null;
		foreach (UITextImageButton spawnedExchangeItem in spawnedExchangeItems)
		{
			if (!spawnedExchangeItem.isActiveAndEnabled)
			{
				uITextImageButton = spawnedExchangeItem;
				break;
			}
		}
		if (uITextImageButton == null)
		{
			uITextImageButton = UnityEngine.Object.Instantiate(prefabExchangeType, prefabExchangeType.transform.parent).GetComponent<UITextImageButton>();
			spawnedExchangeItems.Add(uITextImageButton);
		}
		uITextImageButton.SetObActive(active: true);
		uITextImageButton.SetText(s);
		uITextImageButton.SetImageColor(col);
	}

	public void SetEnergy(string name)
	{
		lbEnergyName.text = name;
		rtEnergy.SetObActive(active: true);
	}

	public void UpdateEnergy(string amount, float val)
	{
		lbEnergyAmount.text = amount;
		uiEnergyBar.SetBar(val);
	}

	public void SetHealth(string name)
	{
		lbHealthName.text = name;
		rtHealth.SetObActive(active: true);
		uiRadDeathBar.SetObActive(active: false);
	}

	public void UpdateHealth(string amount, float val)
	{
		lbHealthAmount.text = amount;
		uiHealthBar.SetBar(val);
	}

	public void UpdateRadDeath(float val)
	{
		uiRadDeathBar.SetObActive(active: true);
		uiRadDeathBar.SetBar(val);
	}

	public void UpdateStatusEffects(string status_text)
	{
		lbHealthStatus.text = status_text;
	}

	public void SetRecipe(Factory factory, bool show_ingredients)
	{
		rtRecipe.SetObActive(active: true);
		uiRecipe.SetRecipe(delegate
		{
			if (uiRecipeMenu == null)
			{
				uiRecipeMenu = UIBaseSingleton.Get(UIRecipeMenu.instance);
			}
			uiRecipeMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiRecipeMenu.SetPosition(uiRecipe.GetRecipeMenuPos());
			uiRecipeMenu.SetRecipes(factory);
			uiRecipeMenu.Show(target: true);
		}, show_ingredients);
	}

	public void UpdateRecipe(Factory factory)
	{
		uiRecipe.UpdateRecipe(factory);
	}

	public void SetSensors(List<TrailGateSensor> _sensors)
	{
		rtSensors.SetObActive(active: true);
		rtAddSensorItems.SetObActive(active: false);
		currentSensors = _sensors;
		Mathf.Clamp(_sensors.Count, 1, int.MaxValue);
		if (uiSensorItems.Count < _sensors.Count)
		{
			int num = _sensors.Count - uiSensorItems.Count;
			for (int i = 0; i < num; i++)
			{
				UISensorItem component = UnityEngine.Object.Instantiate(uiSensorItemPrefab, uiSensorItemPrefab.transform.parent).GetComponent<UISensorItem>();
				component.Init();
				uiSensorItems.Add(component);
			}
		}
		for (int j = 0; j < uiSensorItems.Count; j++)
		{
			if (j < _sensors.Count)
			{
				uiSensorItems[j].SetObActive(active: true);
				TrailGateSensor tgs = _sensors[j];
				uiSensorItems[j].Fill(tgs);
				uiSensorItems[j].SetRemove(delegate
				{
					DeleteSensor(tgs, _sensors);
				});
			}
			else
			{
				uiSensorItems[j].SetObActive(active: false);
			}
		}
	}

	public void UpdateSensors()
	{
	}

	private void AddSensorItem(SensorType _type)
	{
		if (currentSensors == null)
		{
			Debug.LogError("Tried adding sensor with no list stored, shouldn't happen");
			return;
		}
		currentSensors.Add(new TrailGateSensor(_type));
		SetSensors(currentSensors);
		rtAddSensorItems.SetObActive(active: false);
	}

	private void DeleteSensor(TrailGateSensor _sensor, List<TrailGateSensor> _sensors)
	{
		if (_sensors.Contains(_sensor))
		{
			_sensors.Remove(_sensor);
		}
		SetSensors(_sensors);
	}

	public void SetAssigner(TrailGate_Counter assigner)
	{
		rtAssigner.SetObActive(active: true);
		slCrewSize.onValueChanged.RemoveAllListeners();
		inCrewSize.onValueChanged.RemoveAllListeners();
		slCrewSize.value = (float)assigner.crewSize / 50f;
		inCrewSize.text = assigner.crewSize.ToString();
		slCrewSize.onValueChanged.AddListener(delegate(float v)
		{
			int crewSize = Mathf.RoundToInt(50f * v);
			inCrewSize.SetTextWithoutNotify(crewSize.ToString());
			assigner.crewSize = crewSize;
		});
		inCrewSize.onValueChanged.AddListener(delegate(string txt)
		{
			int num = txt.ToInt(0);
			slCrewSize.SetValueWithoutNotify((float)num / 50f);
			assigner.crewSize = num;
		});
	}

	public void SetIcons(string _txt, List<PickupType> pickups, List<PickupState> allowed_states, Action on_apply)
	{
		rtIcons.SetObActive(active: true);
		lbIcons.text = _txt;
		btIcons.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btIcons.rtBase.transform.position);
			uiItemMenu.InitPickupTypes(pickups, allowed_states, on_apply, default(PickupType));
			uiItemMenu.Show(target: true);
		});
		btIcons.SetHoverText(Loc.GetUI("BUILDING_ALLOWED_MATERIAL_CLICK"));
	}

	public void UpdateIcons(List<PickupType> pickups)
	{
		int num;
		if (spawnedIcons.Count < pickups.Count)
		{
			num = pickups.Count - spawnedIcons.Count;
			for (int i = 0; i < num; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridIcons.transform).GetComponent<UIIconItem>();
				spawnedIcons.Add(component);
			}
		}
		foreach (UIIconItem spawnedIcon in spawnedIcons)
		{
			spawnedIcon.SetObActive(active: false);
		}
		num = Mathf.Min(pickups.Count, 5);
		for (int j = 0; j < num; j++)
		{
			spawnedIcons[j].SetObActive(active: true);
			if (j < 4)
			{
				spawnedIcons[j].Init(pickups[j]);
			}
			else
			{
				spawnedIcons[j].Init("...");
			}
		}
		gridIcons.constraintCount = num;
	}

	public void SetButtonWithText(Action on_click, bool clear_on_click, string txt = "")
	{
		btButtonWithText.Init(delegate
		{
			if (clear_on_click)
			{
				Gameplay.instance.ClearFocus();
			}
			on_click();
		});
		if (txt != "")
		{
			lbButtonWithText.text = txt;
		}
		rtButtonWithText.SetObActive(active: true);
		obHotkeyButtonWithText.SetObActive(active: false);
	}

	public void UpdateButtonWithText(string txt, bool enabled = true)
	{
		lbButtonWithText.text = txt;
		rtButtonWithText.SetObActive(enabled);
	}

	public void ShowButtonWithText(bool target)
	{
		rtButtonWithText.SetObActive(target);
	}

	public void SetButtonWithTextHotkey(InputAction input_action)
	{
		SetHotkey(obHotkeyButtonWithText, lbHotkeyButtonWithText, input_action);
	}

	public void SetCargoButton(bool unload, Action on_click)
	{
		UIButton obj = (unload ? btCargoUnloadingWait : btCargoLoadingWait);
		if (!unload)
		{
			_ = lbCargoLoadingWait;
		}
		else
		{
			_ = lbCargoUnloadingWait;
		}
		obj.Init(on_click);
		rtCargoStation.SetObActive(active: true);
	}

	public void UpdateCargoButton(bool unload, string txt)
	{
		(unload ? lbCargoUnloadingWait : lbCargoLoadingWait).text = txt;
	}

	public void SetPausePlay(Action on_click)
	{
		rtPausePlay.SetObActive(active: true);
		btPausePlay.Init(on_click);
	}

	public void UpdatePausePlay(bool paused)
	{
		rtPause.SetObActive(!paused);
		rtPlay.SetObActive(paused);
	}

	public void SetStatusImage()
	{
		rtStatusImage.SetObActive(active: true);
		statusHoverTarget.SetOnPointerEnter(delegate
		{
			Color col;
			string text = GetStatusText(out col);
			if (text != "")
			{
				UIHover.instance.Init(this);
				UIHover.instance.SetText(text, col);
			}
		});
		statusHoverTarget.SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(this);
		});
	}

	public void UpdateStatusImage(BillboardType _status)
	{
		foreach (BillboardScreen billboardImage in billboardImages)
		{
			if (billboardImage.type == _status)
			{
				billboardImage.ob.SetObActive(active: true);
			}
			else
			{
				billboardImage.ob.SetObActive(active: false);
			}
		}
	}

	public void SetStatusText(string txt, Color col)
	{
		statusText = txt;
		statusColor = col;
	}

	private string GetStatusText(out Color col)
	{
		col = statusColor;
		return statusText;
	}

	public void SetGate(TrailGate_Life gate_life)
	{
		rtGateLife.SetObActive(active: true);
		slGateLife.onValueChanged.RemoveAllListeners();
		slGateLife.value = gate_life.lifeValue / 600f;
		float num = Mathf.Round(gate_life.lifeValue);
		inGateLife.text = ((num > 60f) ? num.Unit(PhysUnit.TIME_MINUTES) : num.Unit(PhysUnit.TIME));
		slGateLife.onValueChanged.AddListener(delegate(float v)
		{
			float num2 = 600f * v;
			num2 = Mathf.Round(num2 / 10f) * 10f;
			inGateLife.text = ((num2 > 60f) ? num2.Unit(PhysUnit.TIME_MINUTES) : num2.Unit(PhysUnit.TIME));
			gate_life.lifeValue = num2;
		});
		lbGateLife.text = Loc.GetUI(gate_life.not ? "GATE_LIFE_LOWER" : "GATE_LIFE_HIGHER");
		lbGateLifeToggle.text = (gate_life.not ? "<" : ">");
		btToggleGateLife.Init(delegate
		{
			gate_life.not = !gate_life.not;
			lbGateLife.text = Loc.GetUI(gate_life.not ? "GATE_LIFE_LOWER" : "GATE_LIFE_HIGHER");
			lbGateLifeToggle.text = (gate_life.not ? Loc.GetUI("GENERIC_<") : Loc.GetUI("GENERIC_>"));
		});
	}

	public void SetGateCarry(TrailGate_Carry gate_carry, List<PickupType> _pickups, Action on_apply)
	{
		rtGateCarryCaste.SetObActive(active: true);
		lbGateCarryCaste.text = (gate_carry.not ? Loc.GetUI("GATE_CARRY_NOT") : Loc.GetUI("GATE_CARRY"));
		btToggleGatNot.Init(delegate
		{
			gate_carry.not = !gate_carry.not;
			lbGateCarryCaste.text = (gate_carry.not ? Loc.GetUI("GATE_CARRY_NOT") : Loc.GetUI("GATE_CARRY"));
		});
		btGateCarryCaste.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btGateCarryCaste.rtBase.transform.position);
			uiItemMenu.InitPickupTypes(_pickups, on_apply, PickupType.ANY);
			uiItemMenu.Show(target: true);
		});
		btGateCarryCaste.SetHoverText(Loc.GetUI("GATE_CARRY_CLICK"));
	}

	public void SetGateCaste(TrailGate_Caste gate_caste, List<AntCaste> _castes, Action on_apply)
	{
		rtGateCarryCaste.SetObActive(active: true);
		lbGateCarryCaste.text = (gate_caste.not ? Loc.GetUI("GATE_CASTE_NOT") : Loc.GetUI("GATE_CASTE"));
		btToggleGatNot.Init(delegate
		{
			gate_caste.not = !gate_caste.not;
			lbGateCarryCaste.text = (gate_caste.not ? Loc.GetUI("GATE_CASTE_NOT") : Loc.GetUI("GATE_CASTE"));
		});
		btGateCarryCaste.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btGateCarryCaste.rtBase.transform.position);
			uiItemMenu.InitAntCastes(_castes, on_apply);
			uiItemMenu.Show(target: true);
		});
		btGateCarryCaste.SetHoverText(Loc.GetUI("GATE_CASTE_CLICK"));
	}

	public void UpdateGate(List<AntCaste> castes)
	{
		UpdateGate(null, castes);
	}

	public void UpdateGate(List<PickupType> _pickups, List<AntCaste> _castes = null)
	{
		int num = 3;
		if (spawnedGateItems.Count < num)
		{
			for (int i = 0; i < num; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridGateCarryCaste.transform).GetComponent<UIIconItem>();
				spawnedGateItems.Add(component);
			}
		}
		foreach (UIIconItem spawnedGateItem in spawnedGateItems)
		{
			spawnedGateItem.SetObActive(active: false);
		}
		int num2 = 0;
		int num3 = 0;
		if (_pickups != null)
		{
			num3 = _pickups.Count;
			num2 = Mathf.Min(_pickups.Count, num);
		}
		else if (_castes != null)
		{
			num3 = _castes.Count;
			num2 = Mathf.Min(_castes.Count, num);
		}
		for (int j = 0; j < num2 && (_pickups == null || (_pickups.Count >= j - 1 && (_castes == null || _castes.Count >= j - 1))); j++)
		{
			spawnedGateItems[j].SetObActive(active: true);
			if (num3 > num && j + 1 >= num)
			{
				spawnedGateItems[j].Init("...");
				break;
			}
			if (_pickups != null)
			{
				spawnedGateItems[j].Init(_pickups[j]);
			}
			else if (_castes != null)
			{
				spawnedGateItems[j].Init(_castes[j]);
			}
		}
		gridGateCarryCaste.constraintCount = num2;
	}

	public void SetCannon(AntLauncher _cannon)
	{
		rtCannon.SetObActive(active: true);
		slCannonRot.onValueChanged.RemoveAllListeners();
		slCannonAngle.onValueChanged.RemoveAllListeners();
		slCannonPower.onValueChanged.RemoveAllListeners();
		slCannonRot.value = _cannon.rotation;
		slCannonAngle.value = _cannon.angle;
		slCannonPower.value = 1f - _cannon.power;
		slCannonRot.onValueChanged.AddListener(delegate(float v)
		{
			_cannon.rotation = v;
			_cannon.UpdateTrajectory();
		});
		slCannonAngle.onValueChanged.AddListener(delegate(float v)
		{
			_cannon.angle = v;
			_cannon.UpdateTrajectory();
		});
		slCannonPower.onValueChanged.AddListener(delegate(float v)
		{
			_cannon.power = 1f - v;
		});
	}

	public void UpdateEffects(List<StatusEffect> _effects)
	{
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			listStatusEffect.obEffect.SetObActive(_effects.Contains(listStatusEffect.effect));
		}
	}

	public void UpdateEffectTitle(StatusEffect _effect, string _title)
	{
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			if (listStatusEffect.effect == _effect)
			{
				listStatusEffect.obEffect.SetText(_title);
			}
		}
	}

	public void SetBottomButtons(Action on_click_delete, Action on_click_relocate, Action on_click_follow)
	{
		if (on_click_delete != null)
		{
			btDelete.SetObActive(active: true);
			btDelete.Init(on_click_delete);
			SetHotkey(obHotkeyDelete, lbHotkeyDelete, InputAction.Delete);
		}
		else
		{
			btDelete.SetObActive(active: false);
		}
		if (on_click_relocate != null)
		{
			btRelocate.SetObActive(active: true);
			btRelocate.Init(on_click_relocate);
			SetHotkey(obHotkeyRelocate, lbHotkeyRelocate, InputAction.Relocate);
		}
		else
		{
			btRelocate.SetObActive(active: false);
		}
		if (on_click_follow != null)
		{
			btFollow.SetObActive(active: true);
			btFollow.Init(on_click_follow);
			SetHotkey(obHotkeyFollow, lbHotkeyFollow, InputAction.FollowAnt);
		}
		else
		{
			btFollow.SetObActive(active: false);
		}
		rtBottomButtons.SetObActive(active: true);
	}

	public void UpdateBottomButtons(bool enable_delete)
	{
		btDelete.SetObActive(enable_delete);
	}

	private void SetHotkey(GameObject ob, TMP_Text lb, InputAction input_action)
	{
		string desc = InputManager.GetDesc(input_action);
		ob.SetObActive(desc != "");
		lb.text = desc;
	}
}
