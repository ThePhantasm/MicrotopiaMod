using System;
using System.Collections.Generic;
using UnityEngine;

public class AntCasteAmount : ListItemWithParams
{
	public AntCaste type;

	public int intValue;

	public AntCasteAmount(string txt)
		: base(txt)
	{
	}

	public AntCasteAmount(AntCaste _caste, int _amount)
	{
		type = _caste;
		intValue = _amount;
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "AntCasteAmount";
		if (ArgCountOk(txt, strs, 1))
		{
			if (Enum.TryParse<AntCaste>(strs[0].Trim(), out var result))
			{
				type = result;
				intValue = strs[1].Trim().ToInt(0, className + ": '" + txt + "' parse error");
				return;
			}
			Debug.LogWarning(className + ": '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
		}
	}

	public static List<AntCasteAmount> ParseList(string str)
	{
		List<AntCasteAmount> list = new List<AntCasteAmount>();
		foreach (string item in str.EListItems())
		{
			list.Add(new AntCasteAmount(item));
		}
		return list;
	}
}
