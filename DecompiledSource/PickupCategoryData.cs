using System;
using System.Collections.Generic;
using UnityEngine;

public class PickupCategoryData
{
	private static Dictionary<PickupCategory, PickupCategoryData> dicPickupCategoryData;

	public PickupCategory category;

	public int order;

	public string title;

	public bool showInInventory;

	public PickupType examplePickup;

	public static PickupCategoryData Get(PickupCategory pickup_cat)
	{
		if (dicPickupCategoryData == null)
		{
			dicPickupCategoryData = new Dictionary<PickupCategory, PickupCategoryData>();
			foreach (PickupCategoryData pickupCategory in PrefabData.pickupCategories)
			{
				dicPickupCategoryData.Add(pickupCategory.category, pickupCategory);
			}
		}
		if (dicPickupCategoryData.TryGetValue(pickup_cat, out var value))
		{
			return value;
		}
		Debug.LogWarning("PickupCategoryData: Couldn't find pickup category with code " + pickup_cat);
		if (PrefabData.pickupCategories.Count == 0)
		{
			return null;
		}
		return PrefabData.pickupCategories[0];
	}

	public static PickupCategory ParsePickupCategory(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return PickupCategory.NONE;
		}
		if (Enum.TryParse<PickupCategory>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: PickupCategory parse error; '" + str + "' invalid");
		return PickupCategory.NONE;
	}

	public static List<PickupCategoryData> GetInventoryCategories()
	{
		List<PickupCategoryData> list = new List<PickupCategoryData>();
		foreach (PickupCategoryData pickupCategory in PrefabData.pickupCategories)
		{
			if (pickupCategory.showInInventory)
			{
				list.Add(pickupCategory);
			}
		}
		list.Sort((PickupCategoryData c1, PickupCategoryData c2) => c1.order.CompareTo(c2.order));
		return list;
	}

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}
}
