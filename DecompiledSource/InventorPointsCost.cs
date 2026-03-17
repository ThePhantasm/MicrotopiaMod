using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorPointsCost : ListItemWithParams
{
	public InventorPoints type;

	public int amount;

	public InventorPointsCost(string txt)
		: base(txt)
	{
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "InventorPointsCost";
		if (ArgCountOk(txt, strs, 1))
		{
			if (Enum.TryParse<InventorPoints>(strs[0].Trim(), out var result))
			{
				type = result;
				amount = strs[1].Trim().ToInt(0, className + ": '" + txt + "' parse error");
				return;
			}
			Debug.LogWarning(className + ": '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
		}
	}

	public static List<InventorPointsCost> ParseList(string str)
	{
		List<InventorPointsCost> list = new List<InventorPointsCost>();
		foreach (string item in str.EListItems())
		{
			list.Add(new InventorPointsCost(item));
		}
		return list;
	}
}
