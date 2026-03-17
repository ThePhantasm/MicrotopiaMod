using System;
using System.Collections.Generic;
using UnityEngine;

public static class Progress
{
	private static List<string> unlockedBuildings = new List<string>();

	private static List<string> newBuildings = new List<string>();

	private static List<TrailType> unlockedTrails = new List<TrailType>();

	private static List<TrailType> newTrails = new List<TrailType>();

	private static List<string> unlockedRecipes = new List<string>();

	private static List<GeneralUnlocks> generalUnlocks = new List<GeneralUnlocks>();

	private static List<PickupType> seenPickups = new List<PickupType>();

	private static HashSet<PickupType> collectedPickups = new HashSet<PickupType>();

	private static Dictionary<InventorPoints, int> inventorPoints = new Dictionary<InventorPoints, int>();

	private static Dictionary<InventorPoints, int> inventorPoints_preview = new Dictionary<InventorPoints, int>();

	private static int islandReveals = 0;

	private static int nupFlightLevel = 0;

	public static int inventorsCompleted = 0;

	public static Dictionary<PickupType, int> pickupsFedToInvenor = new Dictionary<PickupType, int>();

	public static Dictionary<PickupType, int> pickupsMined = new Dictionary<PickupType, int>();

	public static Dictionary<PickupType, int> pickupsFedToQueen = new Dictionary<PickupType, int>();

	public static int pickupsThrownToOtherIsland = 0;

	public static int antsRadExplodedWhileAirborn = 0;

	public static List<float> remainingLifespansWhenUpgraded = new List<float>();

	public static int nOldAntsUpgraded;

	public static int nLarvaeGrownInDesert;

	public static Dictionary<AntCaste, int> antcastesMade = new Dictionary<AntCaste, int>();

	public static Dictionary<BiomeType, int> launchHitBiomesFromOtherIslands = new Dictionary<BiomeType, int>();

	public static Dictionary<PickupType, int> pickupsManufactured = new Dictionary<PickupType, int>();

	public static Guid playthroughId;

	public static void Write(Save save)
	{
		save.Write(newBuildings.Count);
		foreach (string newBuilding in newBuildings)
		{
			save.Write(newBuilding);
		}
		save.Write(newTrails.Count);
		foreach (TrailType newTrail in newTrails)
		{
			save.Write((int)newTrail);
		}
		save.Write(seenPickups.Count);
		foreach (PickupType seenPickup in seenPickups)
		{
			save.Write((int)seenPickup);
		}
		save.Write(inventorPoints.Count);
		foreach (KeyValuePair<InventorPoints, int> inventorPoint in inventorPoints)
		{
			save.Write((int)inventorPoint.Key);
			save.Write(inventorPoint.Value);
		}
		save.Write(inventorsCompleted);
		save.Write(pickupsFedToInvenor.Count);
		foreach (KeyValuePair<PickupType, int> item in pickupsFedToInvenor)
		{
			save.Write((int)item.Key);
			save.Write(item.Value);
		}
		save.Write(pickupsMined.Count);
		foreach (KeyValuePair<PickupType, int> item2 in pickupsMined)
		{
			save.Write((int)item2.Key);
			save.Write(item2.Value);
		}
		save.Write(pickupsFedToQueen.Count);
		foreach (KeyValuePair<PickupType, int> item3 in pickupsFedToQueen)
		{
			save.Write((int)item3.Key);
			save.Write(item3.Value);
		}
		save.Write(pickupsThrownToOtherIsland);
		save.Write(antsRadExplodedWhileAirborn);
		save.Write(nOldAntsUpgraded);
		save.Write(antcastesMade.Count);
		foreach (KeyValuePair<AntCaste, int> item4 in antcastesMade)
		{
			save.Write((int)item4.Key);
			save.Write(item4.Value);
		}
		save.Write(launchHitBiomesFromOtherIslands.Count);
		foreach (KeyValuePair<BiomeType, int> launchHitBiomesFromOtherIsland in launchHitBiomesFromOtherIslands)
		{
			save.Write((int)launchHitBiomesFromOtherIsland.Key);
			save.Write(launchHitBiomesFromOtherIsland.Value);
		}
		save.Write(nupFlightLevel);
		save.Write(pickupsManufactured.Count);
		foreach (KeyValuePair<PickupType, int> item5 in pickupsManufactured)
		{
			save.Write((int)item5.Key);
			save.Write(item5.Value);
		}
		save.Write(nLarvaeGrownInDesert);
		save.Write(playthroughId);
	}

	public static void Read(Save save)
	{
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			newBuildings.Add(save.ReadString());
		}
		num = save.ReadInt();
		for (int j = 0; j < num; j++)
		{
			newTrails.Add((TrailType)save.ReadInt());
		}
		num = save.ReadInt();
		for (int k = 0; k < num; k++)
		{
			seenPickups.Add((PickupType)save.ReadInt());
		}
		num = save.ReadInt();
		for (int l = 0; l < num; l++)
		{
			InventorPoints key = (InventorPoints)save.ReadInt();
			int value = Mathf.Max(save.ReadInt(), 0);
			inventorPoints.Add(key, value);
			inventorPoints_preview.Add(key, value);
		}
		inventorsCompleted = save.ReadInt();
		pickupsFedToInvenor.Clear();
		num = save.ReadInt();
		for (int m = 0; m < num; m++)
		{
			PickupType key2 = (PickupType)save.ReadInt();
			int value2 = save.ReadInt();
			pickupsFedToInvenor.Add(key2, value2);
		}
		if (save.version >= 13 && save.version < 42)
		{
			save.ReadInt();
		}
		if (save.version >= 18 && save.version < 20)
		{
			save.ReadInt();
		}
		if (save.version >= 20)
		{
			pickupsMined.Clear();
			num = save.ReadInt();
			for (int n = 0; n < num; n++)
			{
				AddPickupMined((PickupType)save.ReadInt(), save.ReadInt());
			}
		}
		if (save.version >= 21)
		{
			pickupsFedToQueen.Clear();
			num = save.ReadInt();
			for (int num2 = 0; num2 < num; num2++)
			{
				AddPickupFedToQueen((PickupType)save.ReadInt(), save.ReadInt());
			}
		}
		if (save.version >= 39)
		{
			pickupsThrownToOtherIsland = save.ReadInt();
			antsRadExplodedWhileAirborn = save.ReadInt();
			if (save.version < 63)
			{
				num = save.ReadInt();
				for (int num3 = 0; num3 < num; num3++)
				{
					save.ReadFloat();
				}
			}
			else
			{
				nOldAntsUpgraded = save.ReadInt();
			}
			antcastesMade.Clear();
			num = save.ReadInt();
			for (int num4 = 0; num4 < num; num4++)
			{
				antcastesMade.Add((AntCaste)save.ReadInt(), save.ReadInt());
			}
			launchHitBiomesFromOtherIslands.Clear();
			num = save.ReadInt();
			for (int num5 = 0; num5 < num; num5++)
			{
				launchHitBiomesFromOtherIslands.Add((BiomeType)save.ReadInt(), save.ReadInt());
			}
		}
		if (save.version >= 45)
		{
			nupFlightLevel = save.ReadInt();
		}
		else
		{
			nupFlightLevel = 0;
		}
		if (save.version >= 53)
		{
			num = save.ReadInt();
			for (int num6 = 0; num6 < num; num6++)
			{
				pickupsManufactured.Add((PickupType)save.ReadInt(), save.ReadInt());
			}
		}
		if (save.version >= 67)
		{
			nLarvaeGrownInDesert = save.ReadInt();
		}
		if (save.version >= 70)
		{
			playthroughId = save.ReadGuid();
		}
	}

	public static void Clear()
	{
		unlockedBuildings.Clear();
		newBuildings.Clear();
		unlockedTrails.Clear();
		newTrails.Clear();
		unlockedRecipes.Clear();
		generalUnlocks.Clear();
		seenPickups.Clear();
		collectedPickups.Clear();
		inventorsCompleted = 0;
		inventorPoints.Clear();
		inventorPoints_preview.Clear();
		islandReveals = 0;
		pickupsFedToInvenor.Clear();
		pickupsMined.Clear();
		pickupsFedToQueen.Clear();
		pickupsManufactured.Clear();
		pickupsThrownToOtherIsland = 0;
		antsRadExplodedWhileAirborn = 0;
		remainingLifespansWhenUpgraded.Clear();
		nOldAntsUpgraded = 0;
		nLarvaeGrownInDesert = 0;
		antcastesMade.Clear();
		launchHitBiomesFromOtherIslands.Clear();
		nupFlightLevel = 0;
		playthroughId = Guid.Empty;
	}

	public static void UnlockBuilding(string code, bool during_load = false)
	{
		if (!unlockedBuildings.Contains(code))
		{
			unlockedBuildings.Add(code);
			if (!during_load)
			{
				newBuildings.Add(code);
			}
			if (DebugSettings.standard.logBuildingUnlocks)
			{
				Debug.Log(code + " was added to unlocked buildings " + Time.time);
			}
		}
	}

	public static bool HasUnlockedBuilding(string building_code)
	{
		if (DebugSettings.standard.UnlockEverything())
		{
			return true;
		}
		return unlockedBuildings.Contains(building_code);
	}

	public static List<BuildingData> GetUnlockedBuildings()
	{
		List<BuildingData> list = new List<BuildingData>();
		foreach (BuildingData building in PrefabData.buildings)
		{
			if ((!DebugSettings.standard.demo || building.inDemo) && (unlockedBuildings.Contains(building.code) || DebugSettings.standard.UnlockEverything()) && building.inBuildMenu)
			{
				bool flag = true;
				if (building.maxBuildCount > 0 && GameManager.instance.GetBuildingCount(building.code) >= building.maxBuildCount)
				{
					flag = false;
				}
				if (flag)
				{
					list.Add(building);
				}
			}
		}
		list.Sort((BuildingData b1, BuildingData b2) => b1.showOrder.CompareTo(b2.showOrder));
		return list;
	}

	public static bool HasNotUsedBuilding(string code)
	{
		return newBuildings.Contains(code);
	}

	public static void UseBuilding(string code)
	{
		if (newBuildings.Contains(code))
		{
			newBuildings.Remove(code);
		}
	}

	public static List<BuildingGroup> GetUnlockedBuildingGroups()
	{
		List<BuildingGroup> list = new List<BuildingGroup>();
		if (GetUnlockedTrailsInBuildMenu().Count > 0)
		{
			list.Add(BuildingGroup.TRAILS);
		}
		if (HasUnlocked(GeneralUnlocks.BLUEPRINTS))
		{
			list.Add(BuildingGroup.BLUEPRINTS);
		}
		foreach (BuildingGroup allBuildingGroup in BuildingData.GetAllBuildingGroups())
		{
			foreach (BuildingData unlockedBuilding in GetUnlockedBuildings())
			{
				if (unlockedBuilding.group == allBuildingGroup)
				{
					list.Add(allBuildingGroup);
					break;
				}
			}
		}
		list.Sort();
		return list;
	}

	public static void UnlockTrail(TrailType _type, bool during_load = false)
	{
		if (!unlockedTrails.Contains(_type))
		{
			unlockedTrails.Add(_type);
			if (!during_load)
			{
				newTrails.Add(_type);
			}
			if (DebugSettings.standard.logBuildingUnlocks)
			{
				Debug.Log(_type.ToString() + " was added to unlocked trails " + Time.time);
			}
			if (unlockedTrails.Contains(TrailType.GATE_COUNTER) && unlockedTrails.Contains(TrailType.GATE_LIFE) && unlockedTrails.Contains(TrailType.GATE_CARRY) && unlockedTrails.Contains(TrailType.GATE_CASTE) && unlockedTrails.Contains(TrailType.GATE_OLD) && unlockedTrails.Contains(TrailType.GATE_COUNTER_END) && unlockedTrails.Contains(TrailType.GATE_SPEED) && unlockedTrails.Contains(TrailType.GATE_TIMER) && unlockedTrails.Contains(TrailType.GATE_STOCKPILE))
			{
				Platform.current.GainAchievement(Achievement.LOGIC_GATES);
			}
		}
	}

	public static bool IsUnlocked(TrailType tt)
	{
		foreach (TrailData trail in PrefabData.trails)
		{
			if (trail.type == tt && (!DebugSettings.standard.demo || trail.inDemo) && (unlockedTrails.Contains(trail.type) || DebugSettings.standard.UnlockEverything()) && trail.inGame)
			{
				return true;
			}
		}
		return false;
	}

	public static List<TrailData> GetUnlockedTrailsInBuildMenu()
	{
		List<TrailData> list = new List<TrailData>();
		foreach (TrailData trail in PrefabData.trails)
		{
			if ((!DebugSettings.standard.demo || trail.inDemo) && (unlockedTrails.Contains(trail.type) || DebugSettings.standard.UnlockEverything()) && trail.inBuildMenu)
			{
				list.Add(trail);
			}
		}
		return list;
	}

	public static bool TrailCanHaveShortcutKey(TrailType trail_type, out string default_key)
	{
		default_key = null;
		foreach (TrailData trail in PrefabData.trails)
		{
			if (trail.type == trail_type)
			{
				if (DebugSettings.standard.demo && !trail.inDemo)
				{
					return false;
				}
				default_key = trail.shortcutKey;
				return trail.inGame;
			}
		}
		return false;
	}

	public static bool HasNotUsedTrail(TrailType type)
	{
		return newTrails.Contains(type);
	}

	public static void UseTrail(TrailType type)
	{
		if (newTrails.Contains(type))
		{
			newTrails.Remove(type);
		}
	}

	public static bool HasUnlocked(TrailType _trail)
	{
		if (DebugSettings.standard.UnlockEverything())
		{
			return true;
		}
		return unlockedTrails.Contains(_trail);
	}

	public static bool CanDoExchange(ExchangeType exchange_type)
	{
		return exchange_type switch
		{
			ExchangeType.FORAGE => HasUnlocked(TrailType.FORAGING), 
			ExchangeType.PLANT_CUT => HasUnlocked(TrailType.PLANT_CUTTING), 
			_ => true, 
		};
	}

	public static void UnlockRecipe(string _recipe, bool during_load = false)
	{
		if (!unlockedRecipes.Contains(_recipe))
		{
			unlockedRecipes.Add(_recipe);
		}
	}

	public static bool HasUnlockedRecipe(string _recipe)
	{
		if (DebugSettings.standard.UnlockEverything())
		{
			return true;
		}
		if (FactoryRecipeData.Get(_recipe).alwaysUnlocked)
		{
			return true;
		}
		return unlockedRecipes.Contains(_recipe);
	}

	public static void Unlock(GeneralUnlocks _unlock)
	{
		generalUnlocks.Add(_unlock);
	}

	public static bool HasUnlocked(GeneralUnlocks _unlock)
	{
		if (_unlock == GeneralUnlocks.NONE || generalUnlocks.Contains(_unlock) || DebugSettings.standard.UnlockEverything())
		{
			return true;
		}
		return false;
	}

	public static GeneralUnlocks ParseGeneralUnlock(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return GeneralUnlocks.NONE;
		}
		if (Enum.TryParse<GeneralUnlocks>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("GeneralUnlocks parse error; '" + str + "' invalid");
		return GeneralUnlocks.NONE;
	}

	public static List<PickupType> GetSeenPickups(params PickupType[] _include)
	{
		List<PickupType> list = new List<PickupType>();
		if (_include != null)
		{
			list.AddRange(_include);
		}
		if (DebugSettings.standard.UnlockEverything())
		{
			foreach (PickupType item in PickupData.EAllPickupTypes())
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		else
		{
			list.AddRange(seenPickups);
			foreach (string unlockedRecipe in unlockedRecipes)
			{
				foreach (PickupCost productPickup in FactoryRecipeData.Get(unlockedRecipe).productPickups)
				{
					if (!list.Contains(productPickup.type))
					{
						list.Add(productPickup.type);
					}
				}
			}
		}
		return list;
	}

	public static void AddSeenPickup(PickupType _type)
	{
		if (!seenPickups.Contains(_type))
		{
			seenPickups.Add(_type);
		}
		seenPickups.Sort((PickupType p1, PickupType p2) => PickupData.Get(p1).order.CompareTo(PickupData.Get(p2).order));
	}

	public static List<AntCaste> GetSeenAntCastes()
	{
		List<AntCaste> list = new List<AntCaste>();
		if (DebugSettings.standard.UnlockEverything())
		{
			foreach (AntCasteData antCaste in PrefabData.antCastes)
			{
				list.Add(antCaste.caste);
			}
			return list;
		}
		list.Add(AntCaste.SENTRY);
		foreach (string unlockedRecipe in unlockedRecipes)
		{
			foreach (AntCasteAmount productAnt in FactoryRecipeData.Get(unlockedRecipe).productAnts)
			{
				if (!list.Contains(productAnt.type))
				{
					list.Add(productAnt.type);
				}
			}
		}
		return list;
	}

	public static void AddInventorPoints(InventorPoints curr, int n, bool preview)
	{
		if (preview)
		{
			if (!inventorPoints_preview.ContainsKey(curr))
			{
				inventorPoints_preview.Add(curr, 0);
			}
			inventorPoints_preview[curr] += n;
		}
		else
		{
			if (!inventorPoints.ContainsKey(curr))
			{
				inventorPoints.Add(curr, 0);
			}
			inventorPoints[curr] += n;
		}
	}

	public static void RemoveInventorPoints(InventorPoints _points, int n)
	{
		if (!inventorPoints.ContainsKey(_points))
		{
			inventorPoints.Add(_points, 0);
		}
		inventorPoints[_points] -= n;
		if (!inventorPoints_preview.ContainsKey(_points))
		{
			inventorPoints_preview.Add(_points, 0);
		}
		inventorPoints_preview[_points] -= n;
	}

	public static Dictionary<InventorPoints, int> GetDicInventorPoints(bool preview)
	{
		return new Dictionary<InventorPoints, int>(preview ? inventorPoints_preview : inventorPoints);
	}

	public static List<InventorPoints> GetListInventorPoints()
	{
		return new List<InventorPoints>
		{
			InventorPoints.REGULAR_T1,
			InventorPoints.REGULAR_T2,
			InventorPoints.REGULAR_T3,
			InventorPoints.INDUSTRIAL,
			InventorPoints.ECO,
			InventorPoints.GYNE_T1,
			InventorPoints.GYNE_T2,
			InventorPoints.GYNE_T3
		};
	}

	public static void AddReveal()
	{
		islandReveals++;
		if (UIGame.instance != null)
		{
			UIGame.instance.SetRevealIsland();
		}
	}

	public static bool CanReveal()
	{
		if (islandReveals > GameManager.instance.GetGroundCount() - 1)
		{
			return !GameManager.instance.BusyAddingBiome();
		}
		return false;
	}

	public static int GetReveals()
	{
		return islandReveals;
	}

	public static void AddPickupFedToInventor(PickupType pickup, int count = 1)
	{
		if (!pickupsFedToInvenor.ContainsKey(pickup))
		{
			pickupsFedToInvenor.Add(pickup, 0);
		}
		pickupsFedToInvenor[pickup] += count;
	}

	public static void AddPickupMined(PickupType _type, int _count = 1)
	{
		if (!pickupsMined.ContainsKey(_type))
		{
			pickupsMined.Add(_type, 0);
		}
		pickupsMined[_type] += _count;
	}

	public static void AddPickupFedToQueen(PickupType _type, int _count = 1)
	{
		if (!pickupsFedToQueen.ContainsKey(_type))
		{
			pickupsFedToQueen.Add(_type, 0);
		}
		pickupsFedToQueen[_type] += _count;
	}

	public static void AddPickupManufactured(PickupType _type, int _count = 1)
	{
		if (!pickupsManufactured.ContainsKey(_type))
		{
			pickupsManufactured.Add(_type, 0);
		}
		pickupsManufactured[_type] += _count;
	}

	public static void AddAntCasteMade(AntCaste _caste, int _count = 1)
	{
		if (!antcastesMade.ContainsKey(_caste))
		{
			antcastesMade.Add(_caste, 0);
		}
		antcastesMade[_caste] += _count;
	}

	public static void AddBiomeHitFromLaunch(BiomeType _biome)
	{
		if (!launchHitBiomesFromOtherIslands.ContainsKey(_biome))
		{
			launchHitBiomesFromOtherIslands.Add(_biome, 0);
		}
		launchHitBiomesFromOtherIslands[_biome]++;
	}

	public static void IncreaseNuptialFlight()
	{
		nupFlightLevel++;
	}

	public static int GetNuptialFlightLevel()
	{
		return nupFlightLevel;
	}

	public static void SetCollected(PickupType _pickup)
	{
		collectedPickups.Add(_pickup);
	}

	public static bool HasCollected(PickupType _pickup)
	{
		return collectedPickups.Contains(_pickup);
	}

	public static bool CheckUnlocked(Blueprint blueprint)
	{
		if (DebugSettings.standard.UnlockEverything())
		{
			return true;
		}
		foreach (string item in blueprint.buildingsNeeded)
		{
			if (!unlockedBuildings.Contains(item))
			{
				return false;
			}
		}
		foreach (TrailType item2 in blueprint.trailTypesNeeded)
		{
			if (!unlockedTrails.Contains(item2))
			{
				return false;
			}
		}
		return true;
	}

	public static bool CheckUnlocked(Blueprint blueprint, out string missing_components)
	{
		missing_components = null;
		if (DebugSettings.standard.UnlockEverything())
		{
			return true;
		}
		foreach (string item in blueprint.buildingsNeeded)
		{
			if (!unlockedBuildings.Contains(item))
			{
				missing_components = missing_components + ", " + Loc.GetObject(BuildingData.Get(item).title);
			}
		}
		foreach (TrailType item2 in blueprint.trailTypesNeeded)
		{
			if (!unlockedTrails.Contains(item2))
			{
				missing_components = missing_components + ", " + Loc.GetObject(TrailData.Get(item2).title);
			}
		}
		if (missing_components != null)
		{
			missing_components = missing_components[2..];
			return false;
		}
		return true;
	}
}
