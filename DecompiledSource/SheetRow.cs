using System;
using System.Collections.Generic;
using UnityEngine;

public class SheetRow
{
	private Dictionary<string, string> values;

	public SheetRow()
	{
		values = new Dictionary<string, string>();
	}

	public void Add(string col_name, string val)
	{
		values[col_name] = val;
	}

	public string GetString(string col_name, bool replace_slash_n)
	{
		return GetString(col_name, "", replace_slash_n);
	}

	public string GetString(string col_name, string def = "", bool replace_slash_n = false)
	{
		col_name = col_name.ToLowerInvariant();
		if (!values.ContainsKey(col_name))
		{
			Debug.LogError("Couldn't find column '" + col_name + "'");
			return def;
		}
		if (values[col_name] == null)
		{
			return def;
		}
		string text = values[col_name];
		if (replace_slash_n)
		{
			text = text.Replace("|", "\n");
		}
		if (replace_slash_n)
		{
			text = text.Replace("\\n", "\r\n");
		}
		return text;
	}

	public int GetInt(string col_name, int def = -1)
	{
		string text = GetString(col_name, null);
		if (string.IsNullOrEmpty(text))
		{
			return def;
		}
		try
		{
			return Convert.ToInt32(text);
		}
		catch
		{
			Debug.LogError("Couldn't convert '" + text + "' to int");
			return def;
		}
	}

	public float GetFloat(string col_name, float def = -1f)
	{
		string text = GetString(col_name, null);
		if (string.IsNullOrEmpty(text))
		{
			return def;
		}
		return text.ToFloat(def, "SheetReader");
	}

	public bool GetBool(string col_name, bool def = false)
	{
		string text = GetString(col_name, null);
		if (string.IsNullOrEmpty(text))
		{
			return def;
		}
		switch (text.ToLowerInvariant())
		{
		case "0":
		case "n":
		case "no":
		case "f":
		case "false":
			return false;
		case "1":
		case "y":
		case "yes":
		case "t":
		case "true":
		case "x":
		case "/":
			return true;
		default:
			Debug.LogError("Couldn't convert '" + text + "' to bool");
			return def;
		}
	}

	public static bool Skip(string str)
	{
		if (!string.IsNullOrEmpty(str))
		{
			return str.StartsWith("//");
		}
		return true;
	}

	public static bool Skip(params string[] strs)
	{
		for (int i = 0; i < strs.Length; i++)
		{
			if (Skip(strs[i]))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasColumn(string name)
	{
		return values.ContainsKey(name.ToLowerInvariant());
	}
}
