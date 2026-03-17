using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingData
{
	private static Dictionary<string, BuildingData> dicBuildingData;

	public string code;

	public GameObject prefab;

	public int showOrder;

	public string title;

	public string titleParent;

	public string description;

	public BuildingGroup group;

	public bool inBuildMenu;

	public List<PickupCost> baseCosts = new List<PickupCost>();

	public bool autoRecipe;

	public int maxBuildCount;

	public bool noDemolish;

	public int minAntCount;

	public float pollution;

	public int storageCapacity;

	public Tutorial tutorial;

	public bool inDemo;

	public string parentBuilding;

	public List<string> recipes = new List<string>();

	public static BuildingData Get(string building_code)
	{
		if (dicBuildingData == null)
		{
			dicBuildingData = new Dictionary<string, BuildingData>();
			foreach (BuildingData building in PrefabData.buildings)
			{
				dicBuildingData.Add(building.code, building);
			}
		}
		if (dicBuildingData.TryGetValue(building_code, out var value))
		{
			return value;
		}
		Debug.LogWarning("BuildingData: Couldn't find building with code " + building_code);
		if (PrefabData.buildings.Count == 0)
		{
			return null;
		}
		return PrefabData.buildings[0];
	}

	public static BuildingGroup ParseBuildingGroup(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return BuildingGroup.NONE;
		}
		if (Enum.TryParse<BuildingGroup>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Buildings: BuildingGroup parse error; '" + str + "' invalid");
		return BuildingGroup.NONE;
	}

	public static bool CheckBuildingCode(string s, string class_name = "")
	{
		foreach (BuildingData building in PrefabData.buildings)
		{
			if (building.code == s)
			{
				return true;
			}
		}
		Debug.LogWarning(class_name + s + " not recognized as building");
		return false;
	}

	public static List<BuildingGroup> GetAllBuildingGroups()
	{
		List<BuildingGroup> list = new List<BuildingGroup>();
		foreach (BuildingGroup value in Enum.GetValues(typeof(BuildingGroup)))
		{
			if (value != BuildingGroup.NONE && value != BuildingGroup.ANY && (value != BuildingGroup.OLD || DebugSettings.standard.showOldBuildings))
			{
				list.Add(value);
			}
		}
		return list;
	}

	public static IEnumerable<string> EAllAntBuildings()
	{
		yield return "QUEEN";
	}

	public static string GetCodeFromBuilding(Building b)
	{
		string text = b.name;
		int num = text.IndexOf('(');
		if (num >= 0)
		{
			text = text[..num].Trim();
		}
		foreach (BuildingData building in PrefabData.buildings)
		{
			if (building.prefab.name == text)
			{
				return building.code;
			}
		}
		Debug.LogError("Don't know code for Building " + b.name);
		return null;
	}

	public Sprite GetIcon()
	{
		return Resources.Load<Sprite>("Building Icons/" + prefab.name);
	}

	public string GetTitle()
	{
		return Loc.GetObject(title);
	}

	public string GetTitleParent()
	{
		if (titleParent == "")
		{
			return GetTitle();
		}
		return Loc.GetObject(titleParent);
	}

	public string GetDescription()
	{
		return Loc.GetObject(description);
	}
}
