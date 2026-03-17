using System.Collections.Generic;
using System.Xml;

public static class Changelog
{
	private static Dictionary<string, string> dicChangelog = new Dictionary<string, string>();

	public static bool loaded;

	public static bool Init()
	{
		return LoadChangelogFods();
	}

	private static bool LoadChangelogFods()
	{
		XmlDocument xmlDoc = SheetReader.GetXmlDoc(Files.FodsChangelog());
		if (xmlDoc == null)
		{
			return false;
		}
		LoadSheet(xmlDoc, "Changelog", dicChangelog);
		loaded = true;
		return true;
	}

	private static void LoadSheet(XmlDocument fods, string sheet_tab, Dictionary<string, string> dict)
	{
		dict.Clear();
		string col_name = "English";
		foreach (SheetRow item in SheetReader.ERead(fods, sheet_tab))
		{
			string text = item.GetString("Code");
			if (!SheetRow.Skip(text))
			{
				string value = item.GetString(col_name).Replace("|", "\n");
				dict.Add(text, value);
			}
		}
	}

	private static string GetText(string code)
	{
		if (dicChangelog.TryGetValue(code, out var value))
		{
			if (value == "" && dicChangelog.TryGetValue(code + "_old", out var value2))
			{
				return value2.ToText();
			}
			return value.ToText();
		}
		return "?" + code + "?";
	}

	public static string GetText()
	{
		string text = "";
		bool flag = false;
		foreach (KeyValuePair<string, string> item in dicChangelog)
		{
			if (!flag)
			{
				flag = true;
			}
			else
			{
				text += "\n";
			}
			text += item.Value;
		}
		return text;
	}
}
