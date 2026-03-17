using System.Collections.Generic;
using UnityEngine;

public class Tech
{
	private static Dictionary<string, Tech> dicTech;

	public TechStatus status;

	public string code;

	public List<string> requiredTechs = new List<string>();

	public string title;

	public string description;

	public string task;

	public List<InventorPointsCost> costs = new List<InventorPointsCost>();

	public List<string> rewardBuildings = new List<string>();

	public List<string> rewardRecipes = new List<string>();

	public List<TrailType> rewardTrailTypes = new List<TrailType>();

	public List<GeneralUnlocks> rewardGeneralUnlocks = new List<GeneralUnlocks>();

	public bool rewardIsland;

	public bool hidden;

	public bool inDemo;

	public bool inDemoDescription;

	public bool alwaysDone;

	public TechType techType;

	public Tech()
	{
		status = TechStatus.NONE;
	}

	public static Tech Get(string tech_code, string warning = "")
	{
		if (dicTech == null)
		{
			dicTech = new Dictionary<string, Tech>();
			foreach (Tech tech in TechTree.techs)
			{
				dicTech.Add(tech.code, tech);
			}
		}
		if (dicTech.TryGetValue(tech_code, out var value))
		{
			return value;
		}
		Debug.LogWarning(warning + ": Couldn't find tech with code " + tech_code);
		if (TechTree.techs.Count == 0)
		{
			return null;
		}
		return TechTree.techs[0];
	}

	public void Unlock(bool during_load)
	{
		foreach (string rewardBuilding in rewardBuildings)
		{
			Progress.UnlockBuilding(rewardBuilding, during_load);
		}
		foreach (string rewardRecipe in rewardRecipes)
		{
			Progress.UnlockRecipe(rewardRecipe, during_load);
		}
		foreach (TrailType rewardTrailType in rewardTrailTypes)
		{
			Progress.UnlockTrail(rewardTrailType, during_load);
		}
		foreach (GeneralUnlocks rewardGeneralUnlock in rewardGeneralUnlocks)
		{
			Progress.Unlock(rewardGeneralUnlock);
		}
		if (rewardIsland)
		{
			Progress.AddReveal();
		}
		SetStatus(TechStatus.DONE);
	}

	public void SetStatus(TechStatus _status)
	{
		if (alwaysDone)
		{
			status = TechStatus.DONE;
		}
		else
		{
			status = _status;
		}
	}

	public TechStatus GetStatus()
	{
		if (alwaysDone)
		{
			return TechStatus.DONE;
		}
		if (status == TechStatus.NONE && IsAvailable())
		{
			return TechStatus.OPEN;
		}
		if (status == TechStatus.OPEN && IsIdea(out var _task) && _task.status == TaskStatus.COMPLETED)
		{
			SetStatus(TechStatus.DONE);
		}
		return status;
	}

	public bool IsAvailable()
	{
		if (DebugSettings.standard.techtreeAllAvailable)
		{
			return true;
		}
		if (DebugSettings.standard.demo && !inDemo)
		{
			return false;
		}
		if (requiredTechs.Count == 0)
		{
			return true;
		}
		foreach (string requiredTech in requiredTechs)
		{
			if (Get(requiredTech).status != TechStatus.DONE)
			{
				return false;
			}
		}
		return true;
	}

	public bool CostReached(out bool need_inventor, out int inventor_tier, out bool need_gyne, out int gyne_tier)
	{
		need_inventor = false;
		inventor_tier = 1;
		need_gyne = false;
		gyne_tier = 1;
		if (DebugSettings.standard.techtreeFreeTechs || Player.cheatFreeTechTree)
		{
			return true;
		}
		Dictionary<InventorPoints, int> dicInventorPoints = Progress.GetDicInventorPoints(preview: true);
		foreach (InventorPointsCost cost in costs)
		{
			if (cost.amount != 0 && (!dicInventorPoints.ContainsKey(cost.type) || dicInventorPoints[cost.type] < cost.amount))
			{
				switch (cost.type)
				{
				case InventorPoints.REGULAR_T1:
					need_inventor = true;
					break;
				case InventorPoints.REGULAR_T2:
					need_inventor = true;
					inventor_tier = 2;
					break;
				case InventorPoints.REGULAR_T3:
					need_inventor = true;
					inventor_tier = 3;
					break;
				case InventorPoints.GYNE_T1:
					need_gyne = true;
					break;
				case InventorPoints.GYNE_T2:
					need_gyne = true;
					gyne_tier = 2;
					break;
				case InventorPoints.GYNE_T3:
					need_gyne = true;
					gyne_tier = 3;
					break;
				default:
					Debug.LogWarning("Should display inventor point " + cost.type.ToString() + " in interface");
					return false;
				}
			}
		}
		if (!need_inventor)
		{
			return !need_gyne;
		}
		return false;
	}

	public bool IsIdea(out Task _task)
	{
		_task = null;
		if (task == "")
		{
			return false;
		}
		_task = Instinct.Get(task);
		return true;
	}

	public string GetTitle()
	{
		if (title != "")
		{
			return Loc.GetTechTree(title);
		}
		if (rewardRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(rewardRecipes[0]);
			if (factoryRecipeData.productAnts.Count > 0)
			{
				return AntCasteData.Get(factoryRecipeData.productAnts[0].type).GetTitleFull();
			}
		}
		if (rewardBuildings.Count > 0)
		{
			return BuildingData.Get(rewardBuildings[0]).GetTitle();
		}
		if (rewardTrailTypes.Count > 0)
		{
			return TrailData.Get(rewardTrailTypes[0]).GetTitle();
		}
		if (rewardRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData2 = FactoryRecipeData.Get(rewardRecipes[0]);
			if (factoryRecipeData2.productPickups.Count > 0)
			{
				return PickupData.Get(factoryRecipeData2.productPickups[0].type).GetTitle();
			}
		}
		return "?" + code + "?";
	}

	public string GetDescription()
	{
		if (description != "")
		{
			return Loc.GetTechTree(description);
		}
		if (task != "")
		{
			return Instinct.Get(task).GetStory();
		}
		if (rewardRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(rewardRecipes[0]);
			if (factoryRecipeData.productAnts.Count > 0)
			{
				return AntCasteData.Get(factoryRecipeData.productAnts[0].type).GetDescription();
			}
		}
		if (rewardBuildings.Count > 0)
		{
			return BuildingData.Get(rewardBuildings[0]).GetDescription();
		}
		if (rewardTrailTypes.Count > 0)
		{
			return TrailData.Get(rewardTrailTypes[0]).GetDescription();
		}
		if (rewardRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData2 = FactoryRecipeData.Get(rewardRecipes[0]);
			if (factoryRecipeData2.productPickups.Count > 0)
			{
				return PickupData.Get(factoryRecipeData2.productPickups[0].type).GetDescription();
			}
		}
		return "?" + code + "?";
	}

	public Sprite GetIcon(out Color col)
	{
		col = Color.white;
		if (rewardRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(rewardRecipes[0]);
			if (factoryRecipeData.productAnts.Count > 0)
			{
				return AntCasteData.Get(factoryRecipeData.productAnts[0].type).GetIcon();
			}
		}
		if (rewardBuildings.Count > 0)
		{
			return BuildingData.Get(rewardBuildings[0]).GetIcon();
		}
		if (rewardTrailTypes.Count > 0)
		{
			return AssetLinks.standard.GetTrailIcon(rewardTrailTypes[0], out col);
		}
		if (rewardRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData2 = FactoryRecipeData.Get(rewardRecipes[0]);
			if (factoryRecipeData2.productPickups.Count > 0)
			{
				return PickupData.Get(factoryRecipeData2.productPickups[0].type).GetIcon();
			}
		}
		if (rewardGeneralUnlocks.Count > 0)
		{
			switch (rewardGeneralUnlocks[0])
			{
			case GeneralUnlocks.BLUEPRINTS:
				return AssetLinks.standard.spriteButtonBlueprints;
			case GeneralUnlocks.RADAR_JUNGLE:
				return PrefabData.GetBiomeIcon(BiomeType.JUNGLE);
			case GeneralUnlocks.RADAR_BLUE:
				return PrefabData.GetBiomeIcon(BiomeType.BLUE);
			case GeneralUnlocks.RADAR_TOXIC:
				return PrefabData.GetBiomeIcon(BiomeType.TOXIC);
			case GeneralUnlocks.RADAR_CONCRETE:
				return PrefabData.GetBiomeIcon(BiomeType.CONCRETE);
			}
		}
		return null;
	}

	public bool IsBuildingUnlock()
	{
		return rewardBuildings.Count > 0;
	}

	public bool IsRecipeUnlock(out BuildingData craft_building)
	{
		craft_building = null;
		if (rewardRecipes.Count > 0)
		{
			foreach (BuildingData building in PrefabData.buildings)
			{
				if (building.recipes.Contains(rewardRecipes[0]))
				{
					craft_building = building;
					return true;
				}
			}
		}
		return false;
	}

	public bool RequiredToCreate(out List<AntCaste> req_ants, out List<PickupType> req_pickups)
	{
		req_ants = new List<AntCaste>();
		req_pickups = new List<PickupType>();
		using (List<string>.Enumerator enumerator = rewardRecipes.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(enumerator.Current);
				foreach (AntCasteAmount item in factoryRecipeData.costsAnt)
				{
					if (!req_ants.Contains(item.type))
					{
						req_ants.Add(item.type);
					}
				}
				foreach (PickupCost item2 in factoryRecipeData.costsPickup)
				{
					if (!req_pickups.Contains(item2.type))
					{
						req_pickups.Add(item2.type);
					}
				}
				return true;
			}
		}
		using (List<string>.Enumerator enumerator = rewardBuildings.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				foreach (PickupCost baseCost in BuildingData.Get(enumerator.Current).baseCosts)
				{
					if (!req_pickups.Contains(baseCost.type))
					{
						req_pickups.Add(baseCost.type);
					}
				}
				return true;
			}
		}
		return false;
	}

	public static string GetUnlockMessage(TechType techType)
	{
		return techType switch
		{
			TechType.ANT => Loc.GetUI("TECHTREE_UNLOCKS_ANTRECIPE"), 
			TechType.BUILDING => Loc.GetUI("TECHTREE_UNLOCKS_BUILDING"), 
			TechType.EXPLORE => Loc.GetUI("TECHTREE_UNLOCKS_ISLAND"), 
			TechType.LOGIC_TRAIL => Loc.GetUI("TECHTREE_UNLOCKS_LOGICTRAIL"), 
			TechType.PICKUP => Loc.GetUI("TECHTREE_UNLOCKS_MATERIALRECIPE"), 
			TechType.PICKUP_ALTERNATIVE => Loc.GetUI("TECHTREE_UNLOCKS_MATERIALRECIPEALTERNATIVE"), 
			TechType.TRAIL => Loc.GetUI("TECHTREE_UNLOCKS_TRAIL"), 
			TechType.IDEA => Loc.GetUI("TECHTREE_UNLOCKS_IDEA"), 
			TechType.GENERAL => Loc.GetUI("TECHTREE_UNLOCKS_GENERAL"), 
			_ => "", 
		};
	}
}
