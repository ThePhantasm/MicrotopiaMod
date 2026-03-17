using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public static class TechTree
{
	public static List<Tech> techs = new List<Tech>();

	public static List<ResearchRecipeData> researchRecipes = new List<ResearchRecipeData>();

	public static Dictionary<string, List<List<string>>> dicBiomesToSpawn = new Dictionary<string, List<List<string>>>();

	public static List<UnlockRecipeData> unlockRecipes = new List<UnlockRecipeData>();

	public static Dictionary<int, List<string>> dicUnlockTiers = new Dictionary<int, List<string>>();

	public static bool Init()
	{
		techs.Clear();
		XmlDocument xmlDoc = SheetReader.GetXmlDoc(Files.FodsTechTree());
		if (xmlDoc == null)
		{
			return false;
		}
		List<string> list = new List<string>();
		foreach (SheetRow item in SheetReader.ERead(xmlDoc, "TechTree"))
		{
			string text = item.GetString("Code");
			if (!SheetRow.Skip(text))
			{
				list.Add(text);
			}
		}
		foreach (SheetRow item2 in SheetReader.ERead(xmlDoc, "TechTree"))
		{
			string text2 = item2.GetString("Code");
			if (SheetRow.Skip(text2))
			{
				continue;
			}
			Tech tech = new Tech();
			tech.code = text2;
			string str = item2.GetString("REQUIRED_TECH");
			if (!SheetRow.Skip(str))
			{
				tech.requiredTechs = new List<string>();
				foreach (string item3 in str.EListItems())
				{
					if (!list.Contains(item3))
					{
						Debug.LogError(text2 + ": don't know required tech " + item3);
					}
					else
					{
						tech.requiredTechs.Add(item3);
					}
				}
			}
			tech.title = item2.GetString("Title", replace_slash_n: true);
			tech.description = item2.GetString("Desc", replace_slash_n: true);
			tech.task = item2.GetString("Task", replace_slash_n: true);
			tech.costs = InventorPointsCost.ParseList(item2.GetString("Cost"));
			string str2 = item2.GetString("unlock_buildings");
			if (!SheetRow.Skip(str2))
			{
				tech.rewardBuildings = new List<string>();
				foreach (string item4 in str2.EListItems())
				{
					if (BuildingData.CheckBuildingCode(item4, "Tech tree " + text2 + ": "))
					{
						tech.rewardBuildings.Add(item4);
					}
				}
			}
			tech.rewardRecipes = FactoryRecipeData.ParseListFactoryRecipe(item2.GetString("unlock_recipes"), "TechTree " + tech.code + ":");
			tech.rewardTrailTypes = TrailData.ParseListTrailType(item2.GetString("unlock_trailtypes"), "TechTree " + tech.code + ":");
			tech.rewardGeneralUnlocks = ParseListGeneralUnlock(item2.GetString("unlock general"), "TechTree " + tech.code + ":");
			tech.rewardIsland = item2.GetBool("give_island");
			tech.hidden = item2.GetBool("Hidden");
			tech.inDemo = item2.GetBool("in_demo");
			tech.inDemoDescription = item2.GetBool("desc in demo");
			tech.techType = ParseTechType(item2.GetString("Tech Type"));
			if (item2.GetBool("Auto Done"))
			{
				tech.alwaysDone = true;
				tech.SetStatus(TechStatus.DONE);
			}
			techs.Add(tech);
		}
		researchRecipes.Clear();
		foreach (SheetRow item5 in SheetReader.ERead(xmlDoc, "Research Recipes"))
		{
			string text3 = item5.GetString("Code");
			if (!SheetRow.Skip(text3))
			{
				ResearchRecipeData researchRecipeData = new ResearchRecipeData();
				researchRecipeData.code = text3;
				researchRecipeData.title = item5.GetString("Title", replace_slash_n: true);
				researchRecipeData.caste = AntCasteData.ParseAntCaste(item5.GetString("Caste"));
				researchRecipeData.costs = PickupCost.ParseList(item5.GetString("Costs"));
				researchRecipeData.energy = item5.GetFloat("Energy");
				researchRecipeData.productCurrency = ResearchRecipeData.ParseInventorPoints(item5.GetString("Product"));
				researchRecipeData.productQuantity = item5.GetInt("Product Quantity");
				researchRecipes.Add(researchRecipeData);
			}
		}
		dicBiomesToSpawn.Clear();
		dicBiomesToSpawn.Add(WorldSettings.GetNormalStartBiome(), new List<List<string>>());
		foreach (SheetRow item6 in SheetReader.ERead(xmlDoc, "Exploration Order"))
		{
			if (SheetRow.Skip(item6.GetString("Order")))
			{
				continue;
			}
			string normalStartBiome = WorldSettings.GetNormalStartBiome();
			List<string> list2 = new List<string>();
			foreach (string item7 in item6.GetString(normalStartBiome).EListItems())
			{
				list2.Add(item7);
			}
			dicBiomesToSpawn[normalStartBiome].Add(list2);
		}
		unlockRecipes.Clear();
		foreach (SheetRow item8 in SheetReader.ERead(xmlDoc, "Unlock Recipes"))
		{
			string text4 = item8.GetString("Code");
			if (SheetRow.Skip(text4))
			{
				continue;
			}
			UnlockRecipeData unlockRecipeData = new UnlockRecipeData();
			unlockRecipeData.code = text4;
			unlockRecipeData.title = item8.GetString("Title", replace_slash_n: true);
			unlockRecipeData.costsPickup = PickupCost.ParseList(item8.GetString("Costs pickups"));
			unlockRecipeData.costsAnt = AntCasteAmount.ParseList(item8.GetString("Costs ants"));
			unlockRecipeData.unlockIsland = item8.GetString("Unlock island");
			string str3 = item8.GetString("unlock buildings");
			if (!SheetRow.Skip(str3))
			{
				unlockRecipeData.unlockBuildings = new List<string>();
				foreach (string item9 in str3.EListItems())
				{
					if (BuildingData.CheckBuildingCode(item9, "UnlockeRecipe " + text4 + ": "))
					{
						unlockRecipeData.unlockBuildings.Add(item9);
					}
				}
			}
			unlockRecipeData.unlockRecipes = FactoryRecipeData.ParseListFactoryRecipe(item8.GetString("unlock recipes"), "UnlockeRecipe " + unlockRecipeData.code + ":");
			unlockRecipeData.nextUnlock = item8.GetString("Next unlock");
			unlockRecipeData.repeatable = item8.GetBool("Repeatable");
			unlockRecipeData.tier = item8.GetInt("Tier");
			unlockRecipeData.reqUnlock = item8.GetString("Req unlock");
			string text5 = item8.GetString("Req building");
			if (!SheetRow.Skip(text5) && BuildingData.CheckBuildingCode(text5, "UnlockeRecipe " + text4 + ": "))
			{
				unlockRecipeData.reqBuilding = text5;
			}
			unlockRecipes.Add(unlockRecipeData);
			if (unlockRecipeData.tier >= 0)
			{
				if (!dicUnlockTiers.ContainsKey(unlockRecipeData.tier))
				{
					dicUnlockTiers.Add(unlockRecipeData.tier, new List<string>());
				}
				dicUnlockTiers[unlockRecipeData.tier].Add(unlockRecipeData.code);
			}
		}
		return true;
	}

	public static void Write(Save save)
	{
		List<Tech> list = new List<Tech>();
		foreach (Tech tech in techs)
		{
			if (tech.status == TechStatus.DONE)
			{
				list.Add(tech);
			}
		}
		save.Write(list.Count);
		foreach (Tech item in list)
		{
			save.Write(item.code);
		}
		List<UnlockRecipeData> list2 = new List<UnlockRecipeData>();
		foreach (UnlockRecipeData unlockRecipe in unlockRecipes)
		{
			if (unlockRecipe.IsCompleted())
			{
				list2.Add(unlockRecipe);
			}
		}
		save.Write(list2.Count);
		foreach (UnlockRecipeData item2 in list2)
		{
			save.Write(item2.code);
		}
	}

	public static void Read(Save save)
	{
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Tech.Get(save.ReadString()).Unlock(during_load: true);
		}
		if (save.version >= 91)
		{
			int num2 = save.ReadInt();
			for (int j = 0; j < num2; j++)
			{
				UnlockRecipeData.Get(save.ReadString()).Unlock(during_load: true);
			}
		}
	}

	public static void Clear()
	{
		foreach (Tech tech in techs)
		{
			tech.SetStatus(TechStatus.NONE);
		}
		foreach (UnlockRecipeData unlockRecipe in unlockRecipes)
		{
			unlockRecipe.Clear();
		}
	}

	public static void GiveTech(string _tech)
	{
		Tech.Get(_tech)?.Unlock(during_load: false);
	}

	public static string GetInventorPointsCode(InventorPoints _curr)
	{
		Dictionary<InventorPoints, int> dicInventorPoints = Progress.GetDicInventorPoints(preview: true);
		if (dicInventorPoints.Count == 1 && dicInventorPoints.ContainsKey(InventorPoints.REGULAR_T1))
		{
			return "TECHTREE_INVENTORPOINTS";
		}
		switch (_curr)
		{
		case InventorPoints.REGULAR_T1:
			return "TECHTREE_INVENTORPOINTS_T1";
		case InventorPoints.REGULAR_T2:
			return "TECHTREE_INVENTORPOINTS_T2";
		case InventorPoints.REGULAR_T3:
			return "TECHTREE_INVENTORPOINTS_T3";
		case InventorPoints.INDUSTRIAL:
			return "TECHTREE_INVENTORPOINTS_RED";
		case InventorPoints.ECO:
			return "TECHTREE_INVENTORPOINTS_GREEN";
		case InventorPoints.GYNE_T1:
			return "TECHTREE_GYNEPOINTS_T1";
		case InventorPoints.GYNE_T2:
			return "TECHTREE_GYNEPOINTS_T2";
		case InventorPoints.GYNE_T3:
			return "TECHTREE_GYNEPOINTS_T3";
		default:
			Debug.LogWarning("Don't know code for Research Currency " + _curr);
			return "";
		}
	}

	public static List<string> GetNextBiomesToSpawn(int c)
	{
		if (!dicBiomesToSpawn.TryGetValue(WorldSettings.startingBiome, out var value))
		{
			if (!WorldSettings.sandbox)
			{
				Debug.LogWarning("No biome spawn list found for starting biome " + WorldSettings.startingBiome);
			}
			if (c == 0)
			{
				return new List<string> { WorldSettings.startingBiome };
			}
			using Dictionary<string, List<List<string>>>.Enumerator enumerator = dicBiomesToSpawn.GetEnumerator();
			if (enumerator.MoveNext())
			{
				value = enumerator.Current.Value;
			}
		}
		return value[c];
	}

	public static TechType ParseTechType(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return TechType.NONE;
		}
		if (Enum.TryParse<TechType>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("TechTree: TechType parse error; '" + str + "' invalid");
		return TechType.NONE;
	}

	public static List<GeneralUnlocks> ParseListGeneralUnlock(string str, string context = "")
	{
		List<GeneralUnlocks> list = new List<GeneralUnlocks>();
		foreach (string item in str.EListItems())
		{
			if (Enum.TryParse<GeneralUnlocks>(item.ToUpper(), out var result))
			{
				list.Add(result);
			}
			else
			{
				Debug.LogError(context + "Don't know general unlock " + item);
			}
		}
		return list;
	}
}
