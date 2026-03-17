using System.Collections.Generic;
using UnityEngine;

public class UnlockRecipeData
{
	public static Dictionary<string, UnlockRecipeData> dicUnlockRecipe;

	public bool completed;

	public string code;

	public string title;

	public List<PickupCost> costsPickup = new List<PickupCost>();

	public List<AntCasteAmount> costsAnt = new List<AntCasteAmount>();

	public string unlockIsland;

	public List<string> unlockBuildings = new List<string>();

	public List<string> unlockRecipes = new List<string>();

	public string nextUnlock;

	public bool repeatable;

	public int tier;

	public string reqUnlock;

	public string reqBuilding;

	private static void BuildDictionary()
	{
		if (dicUnlockRecipe != null)
		{
			return;
		}
		dicUnlockRecipe = new Dictionary<string, UnlockRecipeData>();
		foreach (UnlockRecipeData unlockRecipe in TechTree.unlockRecipes)
		{
			dicUnlockRecipe.Add(unlockRecipe.code, unlockRecipe);
		}
	}

	public static UnlockRecipeData Get(string _recipe)
	{
		BuildDictionary();
		if (_recipe == "")
		{
			return null;
		}
		if (dicUnlockRecipe.TryGetValue(_recipe, out var value))
		{
			return value;
		}
		Debug.LogWarning("UnlockRecipeData: Couldn't find unlock recipe with code " + _recipe);
		if (TechTree.unlockRecipes.Count == 0)
		{
			return null;
		}
		return TechTree.unlockRecipes[0];
	}

	public static List<string> ParseListUnlockRecipe(string str, string context = "")
	{
		List<string> list = new List<string>();
		if (!SheetRow.Skip(str))
		{
			foreach (string item in str.EListItems())
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool CheckRecipeCode(string s, string class_name = "")
	{
		BuildDictionary();
		if (dicUnlockRecipe.ContainsKey(s))
		{
			return true;
		}
		Debug.LogWarning(class_name + s + " not recognized as unlock recipe");
		return false;
	}

	public void Clear()
	{
		completed = false;
	}

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}

	public Sprite GetIcon()
	{
		if (unlockIsland != "")
		{
			switch (unlockIsland)
			{
			case "BiomeBlue2":
				return PrefabData.GetBiomeIcon(BiomeType.BLUE);
			case "BiomeScrapara":
				return PrefabData.GetBiomeIcon(BiomeType.DESERT);
			case "BiomeGreen":
				return PrefabData.GetBiomeIcon(BiomeType.JUNGLE);
			case "BiomeToxicwaste":
				return PrefabData.GetBiomeIcon(BiomeType.TOXIC);
			case "BiomeConcrete":
				return PrefabData.GetBiomeIcon(BiomeType.CONCRETE);
			}
		}
		else
		{
			if (unlockBuildings.Count > 0)
			{
				return BuildingData.Get(unlockBuildings[0]).GetIcon();
			}
			if (unlockRecipes.Count > 0)
			{
				return FactoryRecipeData.Get(unlockRecipes[0]).GetIcon();
			}
		}
		return null;
	}

	public void Unlock(bool during_load)
	{
		foreach (string unlockBuilding in unlockBuildings)
		{
			Progress.UnlockBuilding(unlockBuilding, during_load);
		}
		foreach (string unlockRecipe in unlockRecipes)
		{
			Progress.UnlockRecipe(unlockRecipe, during_load);
		}
		if (!repeatable)
		{
			completed = true;
		}
	}

	public bool IsCompleted()
	{
		return completed;
	}

	public bool NeedsAnts()
	{
		return costsAnt.Count > 0;
	}

	public bool NeedsPickups()
	{
		return costsPickup.Count > 0;
	}
}
