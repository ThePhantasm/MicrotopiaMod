using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIIconList : UIBase
{
	[SerializeField]
	private GridLayoutGroup gridInventory;

	private List<UIIconItem> spawnedInventory = new List<UIIconItem>();

	public void SpawnList(Dictionary<PickupType, int> _pickups, string empty_msg)
	{
		List<(PickupType, string)> list = new List<(PickupType, string)>();
		foreach (KeyValuePair<PickupType, int> _pickup in _pickups)
		{
			if (_pickup.Value > 0)
			{
				list.Add((_pickup.Key, $"x {_pickup.Value}"));
			}
		}
		SpawnList(null, list, empty_msg);
	}

	public void SpawnList(List<(AntCaste, string)> ant_icons, List<(PickupType, string)> pickup_icons, string empty_msg)
	{
		int num = Mathf.Max((ant_icons?.Count ?? 0) + (pickup_icons?.Count ?? 0), 1);
		int num2;
		if (spawnedInventory.Count < num)
		{
			num2 = num - spawnedInventory.Count;
			for (int i = 0; i < num2; i++)
			{
				UIIconItem component = Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridInventory.transform).GetComponent<UIIconItem>();
				spawnedInventory.Add(component);
			}
		}
		foreach (UIIconItem item in spawnedInventory)
		{
			item.SetObActive(active: false);
		}
		gridInventory.cellSize = new Vector2(100f, 50f);
		num2 = 0;
		if (ant_icons != null)
		{
			foreach (var (antCaste, text) in ant_icons)
			{
				if (!(text == "") && !(text == "0"))
				{
					spawnedInventory[num2].SetObActive(active: true);
					spawnedInventory[num2].Init(antCaste);
					spawnedInventory[num2].SetHoverLocObjects(AntCasteData.Get(antCaste).title);
					spawnedInventory[num2].SetRaycastTarget(target: true);
					spawnedInventory[num2].SetImageEnabled(target: true);
					spawnedInventory[num2].SetExtraText(0, text);
					num2++;
				}
			}
		}
		if (pickup_icons != null)
		{
			foreach (var (pickupType, text2) in pickup_icons)
			{
				if (!(text2 == "") && !(text2 == "0"))
				{
					spawnedInventory[num2].SetObActive(active: true);
					spawnedInventory[num2].Init(pickupType);
					spawnedInventory[num2].SetHoverLocObjects(PickupData.Get(pickupType).title);
					spawnedInventory[num2].SetRaycastTarget(target: true);
					spawnedInventory[num2].SetImageEnabled(target: true);
					spawnedInventory[num2].SetExtraText(0, text2);
					num2++;
				}
			}
		}
		if (num2 == 0)
		{
			spawnedInventory[0].SetObActive(active: true);
			spawnedInventory[0].Init(empty_msg);
			spawnedInventory[0].SetImageEnabled(target: false);
			spawnedInventory[0].SetHoverLocObjects("");
			gridInventory.cellSize = new Vector2(50f, 50f);
		}
	}
}
