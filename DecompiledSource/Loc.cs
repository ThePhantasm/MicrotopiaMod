using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using TMPro;

public static class Loc
{
	private static Dictionary<string, string> dictUI = new Dictionary<string, string>();

	private static Dictionary<string, string> dictObjects = new Dictionary<string, string>();

	private static Dictionary<string, string> dictTutorial = new Dictionary<string, string>();

	private static Dictionary<string, string> dictInstinct = new Dictionary<string, string>();

	private static Dictionary<string, string> dictTechTree = new Dictionary<string, string>();

	private static Dictionary<string, string> dictCredits = new Dictionary<string, string>();

	private static List<AutoLoc> autoLocs = new List<AutoLoc>();

	public static bool loaded;

	public static CultureInfo culture { get; private set; }

	public static bool Init()
	{
		return LoadLocFods();
	}

	private static bool LoadLocFods()
	{
		XmlDocument xmlDoc = SheetReader.GetXmlDoc(Files.FodsLoc());
		if (xmlDoc == null)
		{
			return false;
		}
		LoadSheet(xmlDoc, "UI", dictUI);
		LoadSheet(xmlDoc, "Objects", dictObjects);
		LoadSheet(xmlDoc, "Tutorial", dictTutorial);
		LoadSheet(xmlDoc, "Instinct", dictInstinct);
		LoadSheet(xmlDoc, "TechTree", dictTechTree);
		LoadSheet(xmlDoc, "Credits", dictCredits);
		Language language = Player.language;
		if ((uint)(language - 5) <= 1u)
		{
			List<TMP_FontAsset> fallbackFontAssetTable = ((Player.language == Language.CHINESE_SIMPLIFIED) ? DebugSettings.standard.fallbacksSC : DebugSettings.standard.fallbacksJP);
			foreach (TMP_FontAsset usedFont in DebugSettings.standard.usedFonts)
			{
				usedFont.fallbackFontAssetTable = fallbackFontAssetTable;
			}
		}
		loaded = true;
		FillAutoLocs();
		return true;
	}

	public static IEnumerable<Language> EAllowedLanguages()
	{
		yield return Language.ENGLISH;
		if (DebugSettings.standard.dutch)
		{
			yield return Language.DUTCH;
		}
		if (DebugSettings.standard.french)
		{
			yield return Language.FRENCH;
		}
		if (DebugSettings.standard.german)
		{
			yield return Language.GERMAN;
		}
		if (DebugSettings.standard.polish)
		{
			yield return Language.POLISH;
		}
		if (DebugSettings.standard.russian)
		{
			yield return Language.RUSSIAN;
		}
		if (DebugSettings.standard.chinese)
		{
			yield return Language.CHINESE_SIMPLIFIED;
		}
		if (DebugSettings.standard.korean)
		{
			yield return Language.KOREAN;
		}
		if (DebugSettings.standard.japanese)
		{
			yield return Language.JAPANESE;
		}
	}

	public static bool AllowedLanguage(Language l)
	{
		foreach (Language item in EAllowedLanguages())
		{
			if (l == item)
			{
				return true;
			}
		}
		return false;
	}

	public static string FrenchSpace()
	{
		if (Player.language != Language.FRENCH)
		{
			return "";
		}
		return " ";
	}

	private static void LoadSheet(XmlDocument fods, string sheet_tab, Dictionary<string, string> dict)
	{
		dict.Clear();
		Language language = Player.language;
		string col_name = language.ToString();
		foreach (SheetRow item in SheetReader.ERead(fods, sheet_tab))
		{
			string text = item.GetString("Code");
			if (!SheetRow.Skip(text))
			{
				string value = ((language != Language.NONE) ? item.GetString(col_name).Replace("|", "\n") : text);
				dict.Add(text, value);
			}
		}
	}

	private static string GetText(Dictionary<string, string> dict, string code)
	{
		if (dict.TryGetValue(code, out var value))
		{
			if (value == "" && dict.TryGetValue(code + "_old", out var value2))
			{
				return value2.ToText();
			}
			return value.ToText();
		}
		return "?" + code + "?";
	}

	public static string GetUI(string code, params string[] vars)
	{
		return FillVars(GetText(dictUI, code), vars);
	}

	private static string FillVars(string str, string[] vars)
	{
		for (int i = 0; i < vars.Length; i++)
		{
			str = str.Replace($"^{i + 1}", vars[i]);
		}
		return str;
	}

	public static string GetObject(string code, params string[] vars)
	{
		string text = GetText(dictObjects, code);
		if (text == "" && dictObjects.ContainsKey(code + "_old"))
		{
			text = GetText(dictObjects, code + "_old");
		}
		return FillVars(text, vars);
	}

	public static string GetTutorial(string code)
	{
		return GetText(dictTutorial, code);
	}

	public static string GetInstinct(string code, params string[] vars)
	{
		return FillVars(GetText(dictInstinct, code), vars);
	}

	public static string GetTechTree(string code)
	{
		return GetText(dictTechTree, code);
	}

	public static string GetCredits(string code)
	{
		return GetText(dictCredits, code);
	}

	public static string Upper(string str)
	{
		if (culture == null)
		{
			return str.ToUpper();
		}
		return str.ToUpper(culture);
	}

	public static string LanguageToCultureCode(Language l)
	{
		return l switch
		{
			Language.ENGLISH => "en-US", 
			Language.FRENCH => "fr-FR", 
			Language.GERMAN => "de-DE", 
			Language.JAPANESE => "ja-JP", 
			Language.CHINESE_SIMPLIFIED => "zh-CN", 
			Language.RUSSIAN => "ru-RU", 
			Language.DUTCH => "nl-NL", 
			Language.POLISH => "pl-PL", 
			Language.KOREAN => "ko-KR", 
			_ => "en-US", 
		};
	}

	public static void UpdateLanguage(bool reload_text)
	{
		Language language = Player.language;
		culture = CultureInfo.CreateSpecificCulture(LanguageToCultureCode(language));
		if (language != Language.NONE && reload_text)
		{
			LoadLocFods();
		}
	}

	private static void FillAutoLocs()
	{
		foreach (AutoLoc autoLoc in autoLocs)
		{
			autoLoc.FillText();
		}
	}

	public static IEnumerable<Language> ELanguages()
	{
		for (Language language = Language.ENGLISH; language < Language._MAX; language++)
		{
			yield return language;
		}
	}

	public static string GetLanguageName(Language l)
	{
		return GetUI($"LANGUAGE_{l}");
	}

	public static void Register(AutoLoc auto_loc)
	{
		autoLocs.Add(auto_loc);
	}

	public static void Deregister(AutoLoc auto_loc)
	{
		autoLocs.Remove(auto_loc);
	}
}
