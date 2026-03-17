using System.Collections.Generic;
using UnityEngine;

public class FactoryRecipeData
{
	public static Dictionary<string, FactoryRecipeData> dicFactoryRecipe;

	public string code;

	public string title;

	public List<PickupCost> costsPickup;

	public List<AntCaste> costsAntOld;

	public List<AntCasteAmount> costsAnt;

	public List<PickupCost> productPickups;

	public List<AntCasteAmount> productAnts;

	public float energyCost;

	public float processTime;

	public bool alwaysUnlocked;

	public bool inDemo;

	public List<string> buildings;

	private static void BuildDictionary()
	{
		if (dicFactoryRecipe != null)
		{
			return;
		}
		dicFactoryRecipe = new Dictionary<string, FactoryRecipeData>();
		foreach (FactoryRecipeData factoryRecipe in PrefabData.factoryRecipes)
		{
			dicFactoryRecipe.Add(factoryRecipe.code, factoryRecipe);
		}
	}

	public static FactoryRecipeData Get(string _recipe)
	{
		BuildDictionary();
		if (_recipe == "")
		{
			return null;
		}
		if (dicFactoryRecipe.TryGetValue(_recipe, out var value))
		{
			return value;
		}
		Debug.LogWarning("FactoryRecipeData: Couldn't find factory recipe with code " + _recipe);
		if (PrefabData.factoryRecipes.Count == 0)
		{
			return null;
		}
		return PrefabData.factoryRecipes[0];
	}

	public static List<string> ParseListFactoryRecipe(string str, string context = "")
	{
		List<string> list = new List<string>();
		if (!SheetRow.Skip(str))
		{
			foreach (string item in str.EListItems())
			{
				if (CheckRecipeCode(item, context))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static bool CheckRecipeCode(string s, string class_name = "")
	{
		BuildDictionary();
		if (dicFactoryRecipe.ContainsKey(s))
		{
			return true;
		}
		Debug.LogWarning(class_name + s + " not recognized as factory recipe");
		return false;
	}

	public string GetTitle()
	{
		if (title.Contains("CHEAT_PICKUP_"))
		{
			return PickupData.Get(PickupData.ParsePickupType(title.Replace("CHEAT_PICKUP_", ""))).GetTitle();
		}
		if (title.Contains("CHEAT_ANT_"))
		{
			return AntCasteData.Get(AntCasteData.ParseAntCaste(title.Replace("CHEAT_ANT_", ""))).GetTitleFull();
		}
		return Loc.GetObject(title);
	}

	public Sprite GetIcon()
	{
		if (productAnts.Count > 0)
		{
			return AntCasteData.Get(productAnts[0].type).GetIcon();
		}
		if (productPickups.Count > 0)
		{
			return PickupData.Get(productPickups[0].type).GetIcon();
		}
		return null;
	}
}
