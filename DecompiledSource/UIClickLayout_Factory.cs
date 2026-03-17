using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIClickLayout_Factory : UIClickLayout_Building
{
	[Header("Factory")]
	[SerializeField]
	private UIRecipe uiRecipe;

	[SerializeField]
	private RectTransform rtSlots;

	[SerializeField]
	private RectTransform rtSlotsContent;

	[SerializeField]
	private TextMeshProUGUI lbSlots;

	[SerializeField]
	private UITextBox prefabSlotItem;

	private UIRecipeMenu uiRecipeMenu;

	private List<UITextBox> spawnedSlotItems = new List<UITextBox>();

	private List<UIIconItem> spawnedSlotItemIcons = new List<UIIconItem>();

	public override void Clear()
	{
		base.Clear();
		if (uiRecipeMenu != null)
		{
			Object.Destroy(uiRecipeMenu.gameObject);
		}
		uiRecipeMenu = null;
	}

	public void SetRecipe(Factory factory, bool show_ingredients, bool allow_change_recipe)
	{
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
		uiRecipe.SetChangeRecipeAllowed(allow_change_recipe);
	}

	public void UpdateRecipe(Factory factory, bool allow_change_recipe)
	{
		uiRecipe.UpdateRecipe(factory);
		uiRecipe.SetChangeRecipeAllowed(allow_change_recipe);
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
				UITextBox component = Object.Instantiate(prefabSlotItem, rtSlotsContent).GetComponent<UITextBox>();
				spawnedSlotItems.Add(component);
				UIIconItem component2 = Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), component.obBox.transform).GetComponent<UIIconItem>();
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
}
