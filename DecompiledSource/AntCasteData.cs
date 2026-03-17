using System;
using System.Collections.Generic;
using UnityEngine;

public class AntCasteData
{
	private static Dictionary<AntCaste, AntCasteData> dicAntData;

	public AntCaste caste;

	public GameObject prefab;

	public GameObject prefab_nuptialFlight;

	public string title;

	public string description;

	public List<ExchangeType> exchangeTypes = new List<ExchangeType>();

	public List<string> canDo = new List<string>();

	public float speed;

	public float strength;

	public float energy;

	public float energyExtra;

	public float oldTime;

	public float mineSpeed;

	public bool flying;

	public bool canBeCarried;

	public bool mineForever;

	public int vulnerabilityBits;

	public int order;

	public float flightSpeed;

	public bool isGyne;

	public List<PickupCost> components = new List<PickupCost>();

	public AntCaste deathSpawn;

	public PickupType corpse;

	public bool inDemo;

	public static AntCasteData Get(AntCaste _caste)
	{
		if (dicAntData == null)
		{
			dicAntData = new Dictionary<AntCaste, AntCasteData>();
			foreach (AntCasteData antCaste in PrefabData.antCastes)
			{
				dicAntData.Add(antCaste.caste, antCaste);
			}
		}
		if (dicAntData.TryGetValue(_caste, out var value))
		{
			return value;
		}
		Debug.LogWarning("AntCasteData: Couldn't find ant with caste " + _caste);
		if (PrefabData.antCastes.Count == 0)
		{
			return null;
		}
		return PrefabData.antCastes[0];
	}

	public static AntCaste ParseAntCaste(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return AntCaste.NONE;
		}
		if (Enum.TryParse<AntCaste>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: AntCaste parse error; '" + str + "' invalid");
		return AntCaste.NONE;
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

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}

	public string GetTitleFull()
	{
		return Loc.GetObject(title + "_ANT");
	}

	public string GetDescription()
	{
		return Loc.GetObject(description);
	}

	public Sprite GetIcon()
	{
		return Resources.Load<Sprite>("Ant Caste Icons/" + prefab.name);
	}

	public List<string> GetCanDo()
	{
		List<string> list = new List<string>();
		foreach (string item in canDo)
		{
			list.Add(Loc.GetUI("ANT_CANDO_" + item));
		}
		return list;
	}

	public bool IsImmortal()
	{
		return energy <= 0f;
	}
}
