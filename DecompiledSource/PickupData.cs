using System;
using System.Collections.Generic;
using UnityEngine;

public class PickupData
{
	private static Dictionary<PickupType, PickupData> dicPickupData;

	public PickupType type;

	public GameObject prefab;

	public int order;

	public string title;

	public string description;

	public List<PickupCategory> categories;

	public float energyAmount;

	public float weight;

	public float decay;

	public PickupState state;

	public List<PickupCost> components = new List<PickupCost>();

	public List<StatusEffect> statusEffects = new List<StatusEffect>();

	private bool? canElectrolyse;

	private FactoryRecipeData electrolyseRecipe;

	public bool inDemo;

	public bool planned;

	private float height = -1f;

	public static PickupData Get(PickupType pickup_type)
	{
		if (dicPickupData == null)
		{
			dicPickupData = new Dictionary<PickupType, PickupData>();
			foreach (PickupData pickup in PrefabData.pickups)
			{
				dicPickupData.Add(pickup.type, pickup);
			}
		}
		if (dicPickupData.TryGetValue(pickup_type, out var value))
		{
			return value;
		}
		Debug.LogWarning("PickupData: Couldn't find pickup with code " + pickup_type);
		if (PrefabData.pickups.Count == 0)
		{
			return null;
		}
		return PrefabData.pickups[0];
	}

	public static PickupType ParsePickupType(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return PickupType.NONE;
		}
		if (Enum.TryParse<PickupType>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: PickupType parse error; '" + str + "' invalid");
		return PickupType.NONE;
	}

	public static PickupState ParsePickupState(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return PickupState.NONE;
		}
		if (Enum.TryParse<PickupState>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: PickupState parse error; '" + str + "' invalid");
		return PickupState.NONE;
	}

	public static List<PickupType> ParseListPickupType(string str, string context = "")
	{
		List<PickupType> list = new List<PickupType>();
		foreach (string item in str.EListItems())
		{
			if (Enum.TryParse<PickupType>(item.ToUpper(), out var result))
			{
				list.Add(result);
			}
			else
			{
				Debug.LogError(context + "Don't know pickup type " + item);
			}
		}
		if (list.Count == 0)
		{
			list.Add(PickupType.NONE);
		}
		return list;
	}

	public static IEnumerable<PickupType> EAllPickupTypes()
	{
		foreach (PickupData pickup in PrefabData.pickups)
		{
			if (pickup.type != PickupType.NONE && pickup.type != PickupType.ANY)
			{
				yield return pickup.type;
			}
		}
	}

	public static IEnumerable<PickupType> EAllLarvae()
	{
		yield return PickupType.LARVAE_T1;
		yield return PickupType.LARVAE_T2;
		yield return PickupType.LARVAE_T3;
	}

	public static bool ListContainsPickupType(List<PickupType> _list, PickupType _type)
	{
		if (_type == PickupType.NONE || _list.Contains(PickupType.NONE))
		{
			return false;
		}
		if (!_list.Contains(_type))
		{
			return _list.Contains(PickupType.ANY);
		}
		return true;
	}

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}

	public string GetDescription()
	{
		return Loc.GetObject(description);
	}

	public bool IsEdible()
	{
		return energyAmount > 0f;
	}

	public Sprite GetIcon()
	{
		return Resources.Load<Sprite>("Pickup Icons/" + prefab.name);
	}

	public bool CanElectrolyse(out FactoryRecipeData _data)
	{
		if (!canElectrolyse.HasValue)
		{
			canElectrolyse = false;
			foreach (FactoryRecipeData factoryRecipe in PrefabData.factoryRecipes)
			{
				if (!(factoryRecipe.energyCost <= 0f) && factoryRecipe.costsPickup.Count == 1 && factoryRecipe.costsPickup[0].type == type)
				{
					canElectrolyse = true;
					electrolyseRecipe = factoryRecipe;
				}
			}
		}
		_data = electrolyseRecipe;
		return canElectrolyse.Value;
	}

	public float GetHeight()
	{
		if (height == -1f)
		{
			height = prefab.GetComponent<Pickup>().height;
		}
		return height;
	}
}
