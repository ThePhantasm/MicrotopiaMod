using System;
using System.Collections.Generic;
using UnityEngine;

public class TrailData
{
	private static Dictionary<TrailType, TrailData> dicTrailData_old;

	public TrailType type;

	public int showOrder;

	public string title;

	public string titleProfession;

	public string description;

	public List<ExchangeType> exchangeTypes = new List<ExchangeType>();

	public TrailType parentType;

	public bool logic;

	public bool eraser;

	public bool inBuildMenu;

	public bool inGame;

	public bool snapToConnectable;

	public Tutorial tutorial;

	public bool inDemo;

	public bool elder;

	public string shortcutKey;

	public List<int> trailPages = new List<int>();

	public static TrailData Get(TrailType trail_type)
	{
		if (dicTrailData_old == null)
		{
			dicTrailData_old = new Dictionary<TrailType, TrailData>();
			foreach (TrailData trail in PrefabData.trails)
			{
				dicTrailData_old.Add(trail.type, trail);
			}
		}
		if (dicTrailData_old.TryGetValue(trail_type, out var value))
		{
			return value;
		}
		Debug.LogWarning("TrailData: Couldn't find trail with code " + trail_type);
		if (PrefabData.trails.Count == 0)
		{
			return null;
		}
		return PrefabData.trails[0];
	}

	public static TrailType ParseTrailType(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return TrailType.NONE;
		}
		if (Enum.TryParse<TrailType>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("PrefabData: TrailType parse error; '" + str + "' invalid");
		return TrailType.NONE;
	}

	public static List<TrailType> ParseListTrailType(string str, string context = "")
	{
		List<TrailType> list = new List<TrailType>();
		foreach (string item in str.EListItems())
		{
			if (Enum.TryParse<TrailType>(item.ToUpper(), out var result))
			{
				list.Add(result);
			}
			else
			{
				Debug.LogError(context + "Don't know trail type " + item);
			}
		}
		return list;
	}

	public static ExchangeType ParseExchangeType(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return ExchangeType.NONE;
		}
		if (Enum.TryParse<ExchangeType>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("PrefabData: TrailType parse error; '" + str + "' invalid");
		return ExchangeType.NONE;
	}

	public static List<ExchangeType> ParseListExchangeType(string str, string context = "")
	{
		List<ExchangeType> list = new List<ExchangeType>();
		foreach (string item in str.EListItems())
		{
			if (Enum.TryParse<ExchangeType>(item.ToUpper(), out var result))
			{
				list.Add(result);
			}
			else
			{
				Debug.LogError(context + "Don't know exchange type " + item);
			}
		}
		if (list.Count == 0)
		{
			list.Add(ExchangeType.NONE);
		}
		return list;
	}

	public static List<TrailType> ExchangeTypesToTrailTypes(IEnumerable<ExchangeType> exchanges)
	{
		List<TrailType> list = new List<TrailType>();
		foreach (ExchangeType exchange in exchanges)
		{
			TrailType trailType = TrailType.NONE;
			switch (exchange)
			{
			case ExchangeType.PICKUP:
				trailType = TrailType.HAULING;
				break;
			case ExchangeType.FORAGE:
				trailType = TrailType.FORAGING;
				break;
			case ExchangeType.MINE:
				trailType = TrailType.MINING;
				break;
			case ExchangeType.PLANT_CUT:
				trailType = TrailType.PLANT_CUTTING;
				break;
			case ExchangeType.PICKUP_CORPSE:
				trailType = TrailType.CORPSE_HAULING;
				break;
			}
			if (trailType != TrailType.NONE && !list.Contains(trailType))
			{
				list.Add(trailType);
			}
		}
		list.Sort((TrailType t1, TrailType t2) => t1.CompareTo(t2));
		return list;
	}

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}

	public string GetDescription()
	{
		return Loc.GetObject(description);
	}
}
