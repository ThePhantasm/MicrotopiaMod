using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIInventoryCategory : UIBase
{
	[SerializeField]
	private TMP_Text lbCategory;

	[SerializeField]
	private UIInventoryItem prefabItem;

	private List<(PickupType, UIInventoryItem)> pickupItems = new List<(PickupType, UIInventoryItem)>();

	private List<(string, UIInventoryItem)> buildingItems = new List<(string, UIInventoryItem)>();

	private List<(AntCaste, UIInventoryItem)> antItems = new List<(AntCaste, UIInventoryItem)>();

	private bool shown;

	public void Init(string s)
	{
		prefabItem.SetObActive(active: false);
		lbCategory.text = s;
	}

	private UIInventoryItem AddItem()
	{
		return Object.Instantiate(prefabItem, prefabItem.transform.parent);
	}

	public void AddPickupItem(PickupType pickup_type)
	{
		UIInventoryItem item = AddItem();
		Sprite pickupThumbnail = AssetLinks.standard.GetPickupThumbnail(pickup_type);
		item.SetImage(pickupThumbnail);
		item.Init("-", delegate
		{
			UIGame.instance.InventoryClicked(pickup_type, item);
		});
		PickupData pickupData = PickupData.Get(pickup_type);
		string footer = ((pickupData.energyAmount <= 0f) ? "" : Loc.GetUI("INVENTORY_CONTAINEDENERGY", pickupData.energyAmount.ToString()));
		item.SetHoverInventory(pickupThumbnail, pickupData.GetTitle(), pickupData.GetDescription(), footer);
		pickupItems.Add((pickup_type, item));
	}

	public void AddBuildingItem(string building_code)
	{
		UIInventoryItem item = AddItem();
		Sprite buildingThumbnail = AssetLinks.standard.GetBuildingThumbnail(building_code);
		item.SetImage(buildingThumbnail);
		item.Init("-", delegate
		{
			UIGame.instance.InventoryClicked(building_code, item);
		});
		BuildingData buildingData = BuildingData.Get(building_code);
		item.SetHoverInventory(buildingThumbnail, buildingData.GetTitle(), buildingData.GetDescription(), "");
		buildingItems.Add((building_code, item));
	}

	public void AddAntItem(AntCaste ant_caste)
	{
		UIInventoryItem item = AddItem();
		Sprite antCasteThumbnail = AssetLinks.standard.GetAntCasteThumbnail(ant_caste);
		item.SetImage(antCasteThumbnail);
		item.Init("-", delegate
		{
			UIGame.instance.InventoryClicked(ant_caste, item);
		});
		AntCasteData antCasteData = AntCasteData.Get(ant_caste);
		string footer = (antCasteData.IsImmortal() ? Loc.GetUI("INVENTORY_LIFESPANINFINITE") : Loc.GetUI("INVENTORY_LIFESPAN", antCasteData.energy.Unit(PhysUnit.TIME_MINUTES)));
		item.SetHoverInventory(antCasteThumbnail, antCasteData.GetTitle(), antCasteData.GetDescription(), footer);
		antItems.Add((ant_caste, item));
	}

	public UIInventoryItem GetItem(AntCaste caste)
	{
		foreach (var (antCaste, result) in antItems)
		{
			if (antCaste == caste)
			{
				return result;
			}
		}
		return null;
	}

	public void Hide()
	{
		if (!shown)
		{
			return;
		}
		foreach (var pickupItem in pickupItems)
		{
			pickupItem.Item2.SetObActive(active: false);
		}
		foreach (var buildingItem in buildingItems)
		{
			buildingItem.Item2.SetObActive(active: false);
		}
		foreach (var antItem in antItems)
		{
			antItem.Item2.SetObActive(active: false);
		}
		this.SetObActive(active: false);
		shown = false;
	}

	public int ShowPickups(Dictionary<PickupType, int> pickups)
	{
		int num = 0;
		bool flag = Player.cheatShowFullInventory || DebugSettings.standard.showFullInventory;
		foreach (var pickupItem in pickupItems)
		{
			UITextImageButton item = pickupItem.Item2;
			PickupType item2 = pickupItem.Item1;
			UITextImageButton uITextImageButton = item;
			if (!pickups.TryGetValue(item2, out var value))
			{
				value = 0;
			}
			if (value == 0 && !flag && !Progress.HasCollected(item2))
			{
				uITextImageButton.SetObActive(active: false);
				continue;
			}
			uITextImageButton.SetObActive(active: true);
			uITextImageButton.SetText(value.ToString());
			num++;
		}
		shown = num > 0;
		this.SetObActive(shown);
		return num;
	}

	public int ShowBuildings(Dictionary<string, int> buildings)
	{
		int num = 0;
		bool flag = Player.cheatShowFullInventory || DebugSettings.standard.showFullInventory;
		foreach (var buildingItem in buildingItems)
		{
			UITextImageButton item = buildingItem.Item2;
			string item2 = buildingItem.Item1;
			UITextImageButton uITextImageButton = item;
			if (!buildings.TryGetValue(item2, out var value))
			{
				value = 0;
			}
			if (value == 0 && !flag)
			{
				uITextImageButton.SetObActive(active: false);
				continue;
			}
			uITextImageButton.SetObActive(active: true);
			uITextImageButton.SetText(value.ToString());
			num++;
		}
		shown = num > 0;
		this.SetObActive(shown);
		return num;
	}

	public int ShowAnts(Dictionary<AntCaste, int> ants)
	{
		int num = 0;
		bool flag = Player.cheatShowFullInventory || DebugSettings.standard.showFullInventory;
		foreach (var antItem in antItems)
		{
			UITextImageButton item = antItem.Item2;
			AntCaste item2 = antItem.Item1;
			UITextImageButton uITextImageButton = item;
			bool flag2 = true;
			if (!ants.TryGetValue(item2, out var value))
			{
				value = 0;
				flag2 = flag;
			}
			if (flag2)
			{
				uITextImageButton.SetObActive(active: true);
				uITextImageButton.SetText(value.ToString());
				num++;
			}
			else
			{
				uITextImageButton.SetObActive(active: false);
			}
		}
		shown = num > 0;
		this.SetObActive(shown);
		return num;
	}
}
