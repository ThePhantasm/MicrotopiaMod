using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIClickLayout_BiomeObject : UIClickLayout
{
	[Header("Biome Object")]
	[SerializeField]
	private RectTransform rtInventory;

	[SerializeField]
	private TextMeshProUGUI lbInventory;

	[SerializeField]
	private RectTransform rtSlots;

	[SerializeField]
	private RectTransform rtSlotsContent;

	[SerializeField]
	private TextMeshProUGUI lbSlots;

	[SerializeField]
	private UITextBox prefabSlotItem;

	[SerializeField]
	[FormerlySerializedAs("gridInventory")]
	private GridLayoutGroup inventoryGridLayoutGroup;

	[SerializeField]
	private TextMeshProUGUI lbCapabilities;

	[SerializeField]
	private UITextImageButton prefabExchangeType;

	private List<UITextBox> spawnedSlotItems = new List<UITextBox>();

	private List<UIIconItem> spawnedSlotItemIcons = new List<UIIconItem>();

	private List<UITextImageButton> spawnedExchangeItems = new List<UITextImageButton>();

	[NonSerialized]
	public UIIconGrid inventoryGrid;

	public override void Init()
	{
		base.Init();
		prefabExchangeType.SetObActive(active: false);
	}

	protected override void MyAwake()
	{
		base.MyAwake();
		inventoryGrid = new UIIconGrid(lbInventory, inventoryGridLayoutGroup, keep_constraints: true);
	}

	public void SetInventory(bool target)
	{
		rtInventory.SetObActive(target);
	}

	public void SetSlots(bool target)
	{
		rtSlots.SetObActive(target);
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
		lbCapabilities.text = desc;
		foreach (UITextImageButton spawnedExchangeItem in spawnedExchangeItems)
		{
			spawnedExchangeItem.SetObActive(active: false);
		}
	}

	public void AddCapability(TrailType _type)
	{
		AssetLinks.standard.GetTrailIcon(_type, out var col);
		AddCapability(TrailData.Get(_type).GetTitle(), col);
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
}
