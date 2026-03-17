using System;
using System.Collections.Generic;
using UnityEngine;

public class ResearchRecipeData
{
	public static Dictionary<string, ResearchRecipeData> dicResearchRecipe;

	public static Dictionary<FactoryRecipe, ResearchRecipeData> dicResearchRecipe_old;

	public string code;

	public string title;

	public AntCaste caste;

	public List<PickupCost> costs;

	public float energy;

	public InventorPoints productCurrency;

	public int productQuantity;

	private static void BuildDictionary()
	{
		if (dicResearchRecipe != null)
		{
			return;
		}
		dicResearchRecipe = new Dictionary<string, ResearchRecipeData>();
		foreach (ResearchRecipeData researchRecipe in TechTree.researchRecipes)
		{
			dicResearchRecipe.Add(researchRecipe.code, researchRecipe);
		}
	}

	public static ResearchRecipeData Get(string _recipe)
	{
		BuildDictionary();
		if (_recipe == "")
		{
			return null;
		}
		if (dicResearchRecipe.TryGetValue(_recipe, out var value))
		{
			return value;
		}
		Debug.LogWarning("ResearchRecipeData: Couldn't find research recipe with code " + _recipe);
		if (TechTree.researchRecipes.Count == 0)
		{
			return null;
		}
		return TechTree.researchRecipes[0];
	}

	public static bool CheckRecipeCode(string s, string class_name = "")
	{
		BuildDictionary();
		if (dicResearchRecipe.ContainsKey(s))
		{
			return true;
		}
		Debug.LogWarning(class_name + s + " not recognized as research recipe");
		return false;
	}

	public static InventorPoints ParseInventorPoints(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return InventorPoints.NONE;
		}
		if (Enum.TryParse<InventorPoints>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("TechTree: ResearchCurrency parse error; '" + str + "' invalid");
		return InventorPoints.NONE;
	}
}
