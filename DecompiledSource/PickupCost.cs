using System;
using System.Collections.Generic;
using UnityEngine;

public class PickupCost : ListItemWithParams
{
	public PickupType type;

	public PickupCategory category;

	public int intValue;

	public PickupCost(string txt)
		: base(txt)
	{
	}

	public PickupCost(PickupCost other)
	{
		type = other.type;
		category = other.category;
		intValue = other.intValue;
	}

	public PickupCost(PickupType _type, int _amount)
	{
		type = _type;
		intValue = _amount;
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "PickupCost";
		if (!ArgCountOk(txt, strs, 1))
		{
			return;
		}
		string value = strs[0].Trim();
		if (Enum.TryParse<PickupType>(value, out var result))
		{
			type = result;
			category = PickupCategory.NONE;
		}
		else
		{
			if (!Enum.TryParse<PickupCategory>(value, out var result2))
			{
				Debug.LogWarning(className + ": '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
				return;
			}
			type = PickupType.NONE;
			category = result2;
		}
		intValue = strs[1].Trim().ToInt(0, className + ": '" + txt + "' parse error");
	}

	public static List<PickupCost> ParseList(string str)
	{
		List<PickupCost> list = new List<PickupCost>();
		foreach (string item in str.EListItems())
		{
			list.Add(new PickupCost(item));
		}
		return list;
	}
}
