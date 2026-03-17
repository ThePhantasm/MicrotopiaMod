using System.Collections.Generic;
using UnityEngine;

public class BiomeObjectData
{
	private static Dictionary<string, BiomeObjectData> dicBobData_Codes;

	private static Dictionary<GameObject, BiomeObjectData> dicBobData_Prefabs;

	public string code;

	public GameObject prefab;

	public string title;

	public string description;

	public List<ExchangeType> exchangeTypes = new List<ExchangeType>();

	public List<PickupCost> pickups = new List<PickupCost>();

	public bool infinite;

	public PickupType fruit;

	public bool unclickable;

	public bool trailsPassThrough;

	public float hardness;

	public float pollution;

	public static BiomeObjectData Get(string _code)
	{
		if (dicBobData_Codes == null)
		{
			dicBobData_Codes = new Dictionary<string, BiomeObjectData>();
			foreach (BiomeObjectData biomeObject in PrefabData.biomeObjects)
			{
				dicBobData_Codes.Add(biomeObject.code, biomeObject);
			}
		}
		if (dicBobData_Codes.TryGetValue(_code, out var value))
		{
			return value;
		}
		Debug.LogWarning("BiomeObjectData: Couldn't find biome object with code " + _code);
		if (PrefabData.biomeObjects.Count == 0)
		{
			return null;
		}
		return PrefabData.biomeObjects[0];
	}

	public static BiomeObjectData Get(GameObject _prefab)
	{
		if (dicBobData_Prefabs == null)
		{
			dicBobData_Prefabs = new Dictionary<GameObject, BiomeObjectData>();
			foreach (BiomeObjectData biomeObject in PrefabData.biomeObjects)
			{
				dicBobData_Prefabs.Add(biomeObject.prefab, biomeObject);
			}
		}
		if (dicBobData_Prefabs.TryGetValue(_prefab, out var value))
		{
			return value;
		}
		Debug.LogWarning("BiomeObjectData: Couldn't find biome object with prefab " + _prefab.name);
		if (PrefabData.biomeObjects.Count == 0)
		{
			return null;
		}
		return PrefabData.biomeObjects[0];
	}

	public static string GetCodeFromBiomeObject(BiomeObject bob)
	{
		string text = bob.name;
		int num = text.IndexOf('(');
		if (num >= 0)
		{
			text = text[..num].Trim();
		}
		foreach (BiomeObjectData biomeObject in PrefabData.biomeObjects)
		{
			if (biomeObject.prefab.name == text)
			{
				return biomeObject.code;
			}
		}
		Debug.LogError("Don't know code for BiomeObject " + bob.name);
		return null;
	}

	public bool HasPickups()
	{
		foreach (PickupCost pickup in pickups)
		{
			if (pickup.intValue > 0)
			{
				return true;
			}
		}
		return false;
	}

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}

	public string GetDescription()
	{
		return "";
	}
}
