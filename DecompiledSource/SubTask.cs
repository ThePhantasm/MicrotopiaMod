using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SubTask : ListItemWithParams
{
	public SubTaskType subTaskType;

	private float floatValue;

	private string stringValue;

	private bool boolValue;

	private int intValue;

	private PickupType pickupValue;

	private AntCaste antCasteValue;

	private InventorPoints inventorPointsValue;

	private BiomeType biomeValue;

	public float valueRequired = float.MinValue;

	public float valueCurrent = float.MinValue;

	private float storedFloatValue;

	public SubTask(string txt)
		: base(txt)
	{
		storedFloatValue = 0f;
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "SubTask";
		if (!Enum.TryParse<SubTaskType>(strs[0].Trim(), out subTaskType))
		{
			Debug.LogWarning("SubTask: '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
			return;
		}
		switch (subTaskType)
		{
		case SubTaskType.POPULATION:
		case SubTaskType.N_ANTS_ON_TRAIL:
		case SubTaskType.N_REVEALED_ISLANDS:
		case SubTaskType.N_CORPSES:
		case SubTaskType.N_PICKUPS_OF_SINGLE_TYPE_IN_INVENTORY:
		case SubTaskType.N_CATAPULT_THROWS_OTHER_ISLAND:
		case SubTaskType.N_ANTS_RADEXPLODED_IN_AIR:
		case SubTaskType.N_TECHS_UNLOCKED:
		case SubTaskType.N_GYNES_IN_TOWERS:
		case SubTaskType.N_GYNES_FLOWN:
		case SubTaskType.N_OLD_ANTS_REPURPOSED:
		case SubTaskType.N_ANTS_IN_BEACON:
		case SubTaskType.N_LARVAE_GROWN_IN_DESERT:
			if (ArgCountOk(txt, strs, 1))
			{
				intValue = strs[1].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.QUEEN_ENERGY_AT_LEAST:
		case SubTaskType.TOTAL_TRAIL_LENGTH:
		case SubTaskType.ANT_SPEED:
		case SubTaskType.ENERGY_STORED:
			if (ArgCountOk(txt, strs, 1))
			{
				floatValue = strs[1].Trim().ToFloat(0f, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.N_ANTS_UPGRADED_LIFESPAN_LOWER_THAN:
		case SubTaskType.N_ISLANDS_WITH_POPULATION:
		case SubTaskType.N_ISLANDS_WITH_BUILDINGS:
			if (ArgCountOk(txt, strs, 2))
			{
				intValue = strs[1].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
				floatValue = strs[2].Trim().ToFloat(1f, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.BUILD_BUILDING:
		case SubTaskType.COMPLETE_TASK:
			if (ArgCountOk(txt, strs, 1))
			{
				stringValue = strs[1].Trim();
			}
			break;
		case SubTaskType.COMPLETE_RESEARCH:
			if (ArgCountOk(txt, strs, 1))
			{
				stringValue = strs[1].Trim();
				if (stringValue != "EXPLORATION")
				{
					Tech.Get(stringValue, "Instinct");
				}
			}
			break;
		case SubTaskType.CREATE_ANTCASTE:
		case SubTaskType.FLY_OUT:
			if (ArgCountOk(txt, strs, 1))
			{
				antCasteValue = AntCasteData.ParseAntCaste(strs[1].Trim());
			}
			break;
		case SubTaskType.REVEAL_BIOME:
			if (ArgCountOk(txt, strs, 1))
			{
				biomeValue = Biome.ParseBiomeType(strs[1].Trim());
			}
			break;
		case SubTaskType.N_PICKUPS_IN_INVENTORY:
		case SubTaskType.N_PICKUPS_ON_ANTS:
		case SubTaskType.N_PICKUPS_FED_TO_INVENTOR:
		case SubTaskType.N_PICKUPS_MINED:
		case SubTaskType.N_PICKUPS_FED_TO_QUEEN:
		case SubTaskType.N_PICKUPS_MADE:
			if (ArgCountOk(txt, strs, 2))
			{
				pickupValue = PickupData.ParsePickupType(strs[1].Trim());
				intValue = strs[2].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.N_ANTS_FROM_CASTE:
		case SubTaskType.N_MADE_FROM_CASTE:
			if (ArgCountOk(txt, strs, 2))
			{
				antCasteValue = AntCasteData.ParseAntCaste(strs[1].Trim());
				intValue = strs[2].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.N_INVENTOR_POINTS:
			if (ArgCountOk(txt, strs, 2) && ArgCountOk(txt, strs, 2))
			{
				inventorPointsValue = ResearchRecipeData.ParseInventorPoints(strs[1].Trim());
				intValue = strs[2].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.N_ANTS_LAUNCHED_TO_BIOME:
			if (ArgCountOk(txt, strs, 2))
			{
				intValue = strs[1].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
				biomeValue = Biome.ParseBiomeType(strs[2].Trim());
			}
			break;
		case SubTaskType.N_PICKUPS_OF_TYPE_IN_BIOME:
			if (ArgCountOk(txt, strs, 3))
			{
				biomeValue = Biome.ParseBiomeType(strs[1].Trim());
				pickupValue = PickupData.ParsePickupType(strs[2].Trim());
				intValue = strs[3].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.FILL_UP_STOCKPILE:
		case SubTaskType.CONNECT_DISPENSER_TO_BUILDING:
			if (ArgCountOk(txt, strs, 2))
			{
				stringValue = strs[1].Trim();
				pickupValue = PickupData.ParsePickupType(strs[2].Trim());
			}
			break;
		case SubTaskType.N_ISLANDS_WITH_BUILDING:
			if (ArgCountOk(txt, strs, 2))
			{
				stringValue = strs[1].Trim();
				intValue = (intValue = strs[2].Trim().ToInt(0, "SubTask: '" + txt + "' parse error"));
			}
			break;
		case SubTaskType.N_ANTS_INSIDE_BUILDING:
			if (ArgCountOk(txt, strs, 3))
			{
				stringValue = strs[1].Trim();
				antCasteValue = AntCasteData.ParseAntCaste(strs[2].Trim());
				intValue = strs[3].Trim().ToInt(0, "SubTask: '" + txt + "' parse error");
			}
			break;
		case SubTaskType.LAND_QUEEN:
		case SubTaskType.CAMERA_MOVE_CARDINAL:
		case SubTaskType.CAMERA_ROTATE_LEFTRIGHT:
		case SubTaskType.CAMERA_ZOOM_INOUT:
		case SubTaskType.QUEEN_WAS_FED:
		case SubTaskType.SEEN_NUPTIAL_FLIGHT:
		case SubTaskType.COMPLETE_INVENTOR:
		case SubTaskType.CONNECT_LANDPAD:
		case SubTaskType.CONNECT_CATAPULT:
		case SubTaskType.SEEN_NUPTIAL_FLIGHT1:
		case SubTaskType.SEEN_NUPTIAL_FLIGHT2:
		case SubTaskType.USE_BEACON:
			break;
		}
	}

	public static List<SubTask> ParseList(string str)
	{
		List<SubTask> list = new List<SubTask>();
		foreach (string item in str.EListItems())
		{
			list.Add(new SubTask(item));
		}
		return list;
	}

	public string GetDesc()
	{
		List<string> list = new List<string>();
		string text = "";
		switch (subTaskType)
		{
		case SubTaskType.BUILD_BUILDING:
			list.Add(BuildingData.Get(stringValue).GetTitle());
			switch (stringValue)
			{
			case "NEST":
			case "SMELTER":
			case "WORKSHOP":
			case "COMBINER":
			case "COMBINER2":
			case "RADAR_TOWER":
			case "RADAR_UNLOCKER":
			case "STOCKPILE2":
			case "FLIGHT_PAD_LAUNCH":
			case "FLIGHT_PAD_LAND":
			case "INVENTOR_PAD":
			case "MONUMENT1":
			case "MONUMENT2":
			case "FLUID_CONTAINER":
			case "DISSOLVER":
			case "ELECTROLYZER_LARGE":
			case "ASSEMBLER":
			case "GYNE_TOWER":
				text = "_" + stringValue;
				break;
			default:
				Debug.LogWarning("BUILD_BUILDING " + stringValue + ": should add separate string");
				break;
			}
			break;
		case SubTaskType.CREATE_ANTCASTE:
		{
			list.Add(AntCasteData.Get(antCasteValue).GetTitle());
			AntCaste antCaste = antCasteValue;
			if ((uint)(antCaste - 3) <= 1u || antCaste == AntCaste.INVENTOR_T1 || antCaste == AntCaste.DIGGER_SMALL)
			{
				text = "_" + antCasteValue;
			}
			else
			{
				Debug.LogWarning("CREATE_ANTCASTE " + antCasteValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_ANTS_FROM_CASTE:
			list.Add(AntCasteData.Get(antCasteValue).GetTitle());
			switch (antCasteValue)
			{
			case AntCaste.WORKER_SMALL_T1:
			case AntCaste.WORKER_T1:
			case AntCaste.DRONE:
			case AntCaste.INVENTOR_T1:
			case AntCaste.INVENTOR_T2:
			case AntCaste.PRINCESS:
				text = "_" + antCasteValue;
				break;
			default:
				Debug.LogWarning("N_ANTS_FROM_CASTE " + antCasteValue.ToString() + ": should add seperate string");
				break;
			}
			break;
		case SubTaskType.N_PICKUPS_ON_ANTS:
			list.Add(PickupData.Get(pickupValue).GetTitle());
			if (pickupValue == PickupType.IRON_RAW)
			{
				text = "_" + pickupValue;
			}
			else
			{
				Debug.LogWarning("N_PICKUPS_ON_ANTS " + pickupValue.ToString() + ": should add seperate string");
			}
			break;
		case SubTaskType.N_PICKUPS_IN_INVENTORY:
			list.Add(PickupData.Get(pickupValue).GetTitle());
			switch (pickupValue)
			{
			case PickupType.ENERGY_POD2:
			case PickupType.ENERGY_POD3:
			case PickupType.ENERGY_POD4:
			case PickupType.ENERGY_POD5:
			case PickupType.ENERGY_POD6:
			case PickupType.ENERGY_POD7:
			case PickupType.ENERGY_POD8:
			case PickupType.ENERGY_POD9:
			case PickupType.ENERGY_POD10:
			case PickupType.IRON_RAW:
			case PickupType.IRON_BAR:
			case PickupType.COPPER_RAW:
			case PickupType.RESIN:
			case PickupType.STARCH:
			case PickupType.SCREW:
			case PickupType.IRON_PLATE:
			case PickupType.CONCRETE:
			case PickupType.CRYSTAL_RAW:
			case PickupType.ACID:
			case PickupType.DIODE:
			case PickupType.LIGHTBULB:
			case PickupType.CONCRETE_DUST:
			case PickupType.FABRIC:
			case PickupType.CRYSTAL_SEED:
			case PickupType.GLASS_DUST:
				text = "_" + pickupValue;
				break;
			default:
				Debug.LogWarning("N_PICKUPS_IN_INVENTORY " + pickupValue.ToString() + ": should add seperate string");
				break;
			}
			break;
		case SubTaskType.N_PICKUPS_MADE:
			list.Add(PickupData.Get(pickupValue).GetTitle());
			switch (pickupValue)
			{
			case PickupType.ENERGY_POD2:
			case PickupType.ENERGY_POD3:
			case PickupType.ENERGY_POD4:
			case PickupType.ENERGY_POD5:
			case PickupType.ENERGY_POD6:
			case PickupType.ENERGY_POD7:
			case PickupType.ENERGY_POD8:
			case PickupType.ENERGY_POD9:
			case PickupType.ENERGY_POD10:
			case PickupType.IRON_RAW:
			case PickupType.IRON_BAR:
			case PickupType.COPPER_RAW:
			case PickupType.RESIN:
			case PickupType.STARCH:
			case PickupType.SCREW:
			case PickupType.IRON_PLATE:
			case PickupType.CONCRETE:
			case PickupType.CRYSTAL_RAW:
			case PickupType.ACID:
			case PickupType.COPPER_WIRE:
			case PickupType.DIODE:
			case PickupType.LIGHTBULB:
			case PickupType.CONCRETE_DUST:
			case PickupType.FABRIC:
			case PickupType.CRYSTAL_SEED:
			case PickupType.GLASS_DUST:
				text = "_" + pickupValue;
				break;
			default:
				Debug.LogWarning("N_PICKUPS_MADE " + pickupValue.ToString() + ": should add seperate string");
				break;
			}
			break;
		case SubTaskType.N_PICKUPS_FED_TO_INVENTOR:
			list.Add(PickupData.Get(pickupValue).GetTitle());
			if (pickupValue == PickupType.ENERGY_POD)
			{
				text = "_" + pickupValue;
			}
			else
			{
				Debug.LogWarning("N_PICKUPS_FED_TO_INVENTOR " + pickupValue.ToString() + ": should add seperate string");
			}
			break;
		case SubTaskType.COMPLETE_RESEARCH:
			if (stringValue != "EXPLORATION")
			{
				list.Add(Tech.Get(stringValue).GetTitle());
			}
			switch (stringValue)
			{
			case "BUILD_RADAR":
			case "EXPLORATION":
			case "MAT_FABRIC":
			case "ANT_DRONE":
			case "BUILD_ASSEMBLER":
			case "BUILD_MONUMENT1":
			case "BUILD_MONUMENT2":
			case "ANT_INVENTOR_T2":
				text = "_" + stringValue;
				break;
			default:
				Debug.LogWarning("COMPLETE_RESEARCH " + stringValue + ": should add seperate string");
				break;
			}
			break;
		case SubTaskType.REVEAL_BIOME:
		{
			list.Add(Loc.GetObject("BIOME_" + biomeValue));
			BiomeType biomeType = biomeValue;
			if ((uint)(biomeType - 3) <= 1u || biomeType == BiomeType.ANY)
			{
				text = "_" + biomeValue;
			}
			else
			{
				Debug.LogWarning("REVEAL_ISLAND " + biomeValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_INVENTOR_POINTS:
		{
			list.Add(inventorPointsValue.ToString());
			InventorPoints inventorPoints = inventorPointsValue;
			if ((uint)(inventorPoints - 1) <= 4u)
			{
				text = "_" + inventorPointsValue;
			}
			else
			{
				Debug.LogWarning("N_INVENTOR_POINTS " + inventorPointsValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_PICKUPS_MINED:
		{
			if (pickupValue == PickupType.ANY)
			{
				list.Add(Loc.GetUI("BUILDING_ANY_MATERIAL"));
			}
			else
			{
				list.Add(PickupData.Get(pickupValue).GetTitle());
			}
			PickupType pickupType = pickupValue;
			if (pickupType == PickupType.ANY || pickupType == PickupType.COPPER_RAW || pickupType == PickupType.GLASS_RAW)
			{
				text = "_" + pickupValue;
			}
			else
			{
				Debug.LogWarning("N_PICKUPS_MINED " + pickupValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_PICKUPS_FED_TO_QUEEN:
		{
			list.Add(PickupData.Get(pickupValue).GetTitle());
			PickupType pickupType = pickupValue;
			if ((uint)(pickupType - 102) <= 8u)
			{
				text = "_" + pickupValue;
			}
			else
			{
				Debug.LogWarning("N_PICKUPS_MINED " + pickupValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_ANTS_UPGRADED_LIFESPAN_LOWER_THAN:
			list.Add(floatValue.ToString());
			break;
		case SubTaskType.N_ISLANDS_WITH_POPULATION:
			list.Add(floatValue.ToString());
			break;
		case SubTaskType.N_ISLANDS_WITH_BUILDINGS:
			list.Add(floatValue.ToString());
			break;
		case SubTaskType.N_MADE_FROM_CASTE:
		{
			list.Add(AntCasteData.Get(antCasteValue).GetTitle());
			AntCaste antCaste = antCasteValue;
			if (antCaste == AntCaste.DRONE || (uint)(antCaste - 17) <= 1u)
			{
				text = "_" + antCasteValue;
			}
			else
			{
				Debug.LogWarning("N_MADE_FROM_CASTE " + antCasteValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_ANTS_LAUNCHED_TO_BIOME:
		{
			list.Add(Loc.GetObject("BIOME_" + biomeValue));
			BiomeType biomeType = biomeValue;
			if (biomeType == BiomeType.TOXIC || biomeType == BiomeType.ANY)
			{
				text = "_" + biomeValue;
			}
			else
			{
				Debug.LogWarning("N_ANTS_LAUNCHED_TO_BIOME " + biomeValue.ToString() + ": should add seperate string");
			}
			break;
		}
		case SubTaskType.N_PICKUPS_OF_TYPE_IN_BIOME:
			list.Add(PickupData.Get(pickupValue).GetTitle());
			list.Add(Loc.GetObject("BIOME_" + biomeValue));
			if (pickupValue == PickupType.RESISTOR && biomeValue == BiomeType.TOXIC)
			{
				text = "_" + pickupValue.ToString() + "_" + biomeValue;
				break;
			}
			Debug.LogWarning("N_PICKUPS_OF_TYPE_IN_BIOME " + pickupValue.ToString() + " & " + biomeValue.ToString() + ": should add seperate string");
			break;
		case SubTaskType.FILL_UP_STOCKPILE:
			list.Add(BuildingData.Get(stringValue).GetTitle());
			list.Add(PickupData.Get(pickupValue).GetTitle());
			if (pickupValue == PickupType.ACID && stringValue == "FLUID_CONTAINER")
			{
				text = "_" + stringValue + "_" + pickupValue;
				break;
			}
			Debug.LogWarning("FILL_UP_STOCKPILE " + pickupValue.ToString() + " & " + biomeValue.ToString() + ": should add seperate string");
			break;
		case SubTaskType.CONNECT_DISPENSER_TO_BUILDING:
			list.Add(BuildingData.Get(stringValue).GetTitle());
			list.Add(PickupData.Get(pickupValue).GetTitle());
			if (pickupValue == PickupType.FABRIC && stringValue == "ASSEMBLER")
			{
				text = "_" + stringValue + "_" + pickupValue;
				break;
			}
			Debug.LogWarning("CONNECT_DISPENSER_TO_BUILDING " + pickupValue.ToString() + " & " + biomeValue.ToString() + ": should add seperate string");
			break;
		case SubTaskType.COMPLETE_TASK:
			list.Add(Loc.GetInstinct(stringValue + "_TITLE"));
			break;
		case SubTaskType.N_ANTS_INSIDE_BUILDING:
			list.Add(AntCasteData.Get(antCasteValue).GetTitle());
			list.Add(BuildingData.Get(stringValue).GetTitle());
			if (stringValue == "GYNE_MAKER")
			{
				switch (antCasteValue)
				{
				case AntCaste.WORKER_T1:
				case AntCaste.DRONE:
				case AntCaste.WORKER_T2:
				case AntCaste.WORKER_T3:
				case AntCaste.PRINCESS:
				case AntCaste.DRONE_T2:
				case AntCaste.WORKER_T1_IRON:
				case AntCaste.WORKER_BULB1:
				case AntCaste.WORKER_BULB2:
				case AntCaste.WORKER_BULB3:
				case AntCaste.WORKER_BULB4:
				case AntCaste.WORKER_BULB5:
				case AntCaste.WORKER_BULB6:
				case AntCaste.WORKER_LED:
				case AntCaste.WORKER_T2_ION:
				case AntCaste.WORKER_HEAVY:
				case AntCaste.WORKER_T3_ROYAL:
				case AntCaste.SENTINEL:
				case AntCaste.DRONE_T3:
					text = "_" + stringValue + "_" + antCasteValue;
					break;
				default:
					Debug.LogWarning("N_ANTS_INSIDE_BUILDING " + stringValue + " & " + antCasteValue.ToString() + ": should add seperate string");
					break;
				}
			}
			break;
		case SubTaskType.FLY_OUT:
		{
			list.Add(AntCasteData.Get(antCasteValue).GetTitle());
			AntCaste antCaste = antCasteValue;
			if (antCaste == AntCaste.GYNE || (uint)(antCaste - 39) <= 1u)
			{
				text = "_" + antCasteValue;
			}
			else
			{
				Debug.LogWarning("FLY_OUT " + antCasteValue.ToString() + ": should add seperate string");
			}
			break;
		}
		}
		string instinct = Loc.GetInstinct("SUBTASK_" + subTaskType.ToString() + text, list.ToArray());
		if (instinct == "")
		{
			instinct = Loc.GetInstinct("SUBTASK_" + subTaskType, list.ToArray());
		}
		return instinct;
	}

	public void RecalcValues(bool update_now = true)
	{
		if (valueRequired == float.MinValue)
		{
			valueRequired = CalcValueRequired();
			valueCurrent = CalcValueCurrent();
		}
		else if (update_now)
		{
			valueCurrent = CalcValueCurrent();
		}
	}

	public float CalcValueRequired()
	{
		switch (subTaskType)
		{
		case SubTaskType.BUILD_BUILDING:
		case SubTaskType.LAND_QUEEN:
		case SubTaskType.CREATE_ANTCASTE:
		case SubTaskType.QUEEN_WAS_FED:
		case SubTaskType.COMPLETE_RESEARCH:
		case SubTaskType.SEEN_NUPTIAL_FLIGHT:
		case SubTaskType.COMPLETE_INVENTOR:
		case SubTaskType.REVEAL_BIOME:
		case SubTaskType.CONNECT_LANDPAD:
		case SubTaskType.CONNECT_CATAPULT:
		case SubTaskType.CONNECT_DISPENSER_TO_BUILDING:
		case SubTaskType.COMPLETE_TASK:
		case SubTaskType.SEEN_NUPTIAL_FLIGHT1:
		case SubTaskType.SEEN_NUPTIAL_FLIGHT2:
		case SubTaskType.USE_BEACON:
		case SubTaskType.FLY_OUT:
			return 1f;
		case SubTaskType.CAMERA_ROTATE_LEFTRIGHT:
		case SubTaskType.CAMERA_ZOOM_INOUT:
			return 2f;
		case SubTaskType.CAMERA_MOVE_CARDINAL:
			return 4f;
		case SubTaskType.N_PICKUPS_IN_INVENTORY:
		case SubTaskType.N_PICKUPS_ON_ANTS:
		case SubTaskType.POPULATION:
		case SubTaskType.N_ANTS_ON_TRAIL:
		case SubTaskType.N_ANTS_FROM_CASTE:
		case SubTaskType.N_PICKUPS_FED_TO_INVENTOR:
		case SubTaskType.N_REVEALED_ISLANDS:
		case SubTaskType.N_INVENTOR_POINTS:
		case SubTaskType.N_PICKUPS_MINED:
		case SubTaskType.N_PICKUPS_FED_TO_QUEEN:
		case SubTaskType.N_CORPSES:
		case SubTaskType.N_PICKUPS_OF_SINGLE_TYPE_IN_INVENTORY:
		case SubTaskType.N_ANTS_UPGRADED_LIFESPAN_LOWER_THAN:
		case SubTaskType.N_CATAPULT_THROWS_OTHER_ISLAND:
		case SubTaskType.N_ISLANDS_WITH_BUILDINGS:
		case SubTaskType.N_MADE_FROM_CASTE:
		case SubTaskType.N_ANTS_LAUNCHED_TO_BIOME:
		case SubTaskType.N_PICKUPS_OF_TYPE_IN_BIOME:
		case SubTaskType.N_ANTS_RADEXPLODED_IN_AIR:
		case SubTaskType.N_TECHS_UNLOCKED:
		case SubTaskType.N_PICKUPS_MADE:
		case SubTaskType.N_GYNES_IN_TOWERS:
		case SubTaskType.N_GYNES_FLOWN:
		case SubTaskType.N_ISLANDS_WITH_BUILDING:
		case SubTaskType.N_ANTS_INSIDE_BUILDING:
		case SubTaskType.N_OLD_ANTS_REPURPOSED:
		case SubTaskType.N_ANTS_IN_BEACON:
		case SubTaskType.N_LARVAE_GROWN_IN_DESERT:
			return intValue;
		case SubTaskType.QUEEN_ENERGY_AT_LEAST:
		case SubTaskType.TOTAL_TRAIL_LENGTH:
		case SubTaskType.ANT_SPEED:
		case SubTaskType.ENERGY_STORED:
			return floatValue;
		case SubTaskType.N_ISLANDS_WITH_POPULATION:
			return (float)intValue * floatValue;
		case SubTaskType.FILL_UP_STOCKPILE:
			return BuildingData.Get(stringValue).storageCapacity;
		default:
			Debug.LogError("SubTask.GetValueRequired: dont know type " + subTaskType);
			return 1f;
		}
	}

	public float CalcValueCurrent()
	{
		switch (subTaskType)
		{
		case SubTaskType.BUILD_BUILDING:
			return GameManager.instance.IsBuildingInScene(stringValue, only_completed: true) ? 1 : 0;
		case SubTaskType.LAND_QUEEN:
		{
			float num13 = 0f;
			{
				foreach (Queen item in GameManager.instance.EQueens())
				{
					num13 = Mathf.Max(num13, item.GetStartupProgress());
				}
				return num13;
			}
		}
		case SubTaskType.CREATE_ANTCASTE:
			if (GameManager.instance.IsAntCasteInScene(antCasteValue))
			{
				return 1f;
			}
			foreach (Building item2 in GameManager.instance.EBuildings())
			{
				if (!(item2 is Factory factory2))
				{
					continue;
				}
				FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(factory2.GetProcessingRecipe());
				if (factoryRecipeData == null)
				{
					continue;
				}
				foreach (AntCasteAmount productAnt in factoryRecipeData.productAnts)
				{
					if (productAnt.type == antCasteValue)
					{
						return factory2.GetProcessTime() / factoryRecipeData.processTime;
					}
				}
			}
			return 0f;
		case SubTaskType.N_PICKUPS_ON_ANTS:
			return GameManager.instance.GetNPickupsCarried(pickupValue);
		case SubTaskType.N_PICKUPS_IN_INVENTORY:
			return GameManager.instance.GetNPickupsInInventory(pickupValue);
		case SubTaskType.POPULATION:
			return GameManager.instance.GetAntCount();
		case SubTaskType.CAMERA_MOVE_CARDINAL:
		{
			CamController.instance.GetMovedDis(out var _left, out var _right, out var _up, out var _down);
			int num15 = 0;
			if (_left > 0.2f)
			{
				num15++;
			}
			if (_right > 0.2f)
			{
				num15++;
			}
			if (_up > 0.2f)
			{
				num15++;
			}
			if (_down > 0.2f)
			{
				num15++;
			}
			return num15;
		}
		case SubTaskType.CAMERA_ROTATE_LEFTRIGHT:
		{
			CamController.instance.GetRotatedDis(out var _left2, out var _right2);
			int num26 = 0;
			if (_left2 != 0f)
			{
				num26++;
			}
			if (_right2 != 0f)
			{
				num26++;
			}
			return num26;
		}
		case SubTaskType.CAMERA_ZOOM_INOUT:
		{
			CamController.instance.GetZoomedDis(out var _in, out var _out);
			int num19 = 0;
			if (_in != 0f)
			{
				num19++;
			}
			if (_out != 0f)
			{
				num19++;
			}
			return num19;
		}
		case SubTaskType.N_ANTS_ON_TRAIL:
		{
			int num27 = 0;
			foreach (Ant item3 in GameManager.instance.EAnts())
			{
				if (item3.currentTrail != null && item3.currentTrail.trailType != TrailType.COMMAND)
				{
					num27++;
				}
			}
			return num27;
		}
		case SubTaskType.QUEEN_ENERGY_AT_LEAST:
			foreach (Queen item4 in GameManager.instance.EQueens())
			{
				if (item4.energy > storedFloatValue)
				{
					storedFloatValue = item4.energy;
				}
			}
			return storedFloatValue;
		case SubTaskType.QUEEN_WAS_FED:
			foreach (Queen item5 in GameManager.instance.EQueens())
			{
				if (item5.nTimesFed > 0)
				{
					return 1f;
				}
			}
			return 0f;
		case SubTaskType.N_ANTS_FROM_CASTE:
		{
			float num21 = 0f;
			foreach (Ant item6 in GameManager.instance.EAnts())
			{
				if (item6.data.caste == antCasteValue)
				{
					num21 += 1f;
				}
			}
			{
				foreach (Building item7 in GameManager.instance.EBuildings())
				{
					if (!(item7 is Factory factory4))
					{
						continue;
					}
					FactoryRecipeData factoryRecipeData3 = FactoryRecipeData.Get(factory4.GetProcessingRecipe());
					if (factoryRecipeData3 == null)
					{
						continue;
					}
					foreach (AntCasteAmount productAnt2 in factoryRecipeData3.productAnts)
					{
						if (productAnt2.type == antCasteValue)
						{
							num21 += factory4.GetProcessTime() / factoryRecipeData3.processTime;
						}
					}
				}
				return num21;
			}
		}
		case SubTaskType.COMPLETE_RESEARCH:
			if (stringValue == "EXPLORATION")
			{
				for (int i = 1; i <= 10; i++)
				{
					if (Tech.Get($"EXPLORATION_{i:00}").GetStatus() == TechStatus.DONE)
					{
						return 1f;
					}
				}
			}
			else if (Tech.Get(stringValue).GetStatus() == TechStatus.DONE)
			{
				return 1f;
			}
			return 0f;
		case SubTaskType.SEEN_NUPTIAL_FLIGHT:
		{
			if (NuptialFlight.GetSeenNuptialFlights() == 0)
			{
				return 0f;
			}
			if (NuptialFlight.GetSeenNuptialFlights() == 1 && NuptialFlight.IsNuptialFlightActive(out var progress3))
			{
				return progress3;
			}
			return 1f;
		}
		case SubTaskType.SEEN_NUPTIAL_FLIGHT1:
		{
			if (Progress.GetNuptialFlightLevel() < 1)
			{
				return 0f;
			}
			if (NuptialFlight.IsNuptialFlightActive(out var progress2))
			{
				return progress2;
			}
			return 1f;
		}
		case SubTaskType.SEEN_NUPTIAL_FLIGHT2:
		{
			if (Progress.GetNuptialFlightLevel() < 2)
			{
				return 0f;
			}
			if (NuptialFlight.IsNuptialFlightActive(out var progress))
			{
				return progress;
			}
			return 1f;
		}
		case SubTaskType.N_PICKUPS_FED_TO_INVENTOR:
			if (!Progress.pickupsFedToInvenor.ContainsKey(pickupValue))
			{
				return 0f;
			}
			return Progress.pickupsFedToInvenor[pickupValue];
		case SubTaskType.COMPLETE_INVENTOR:
		{
			float num2 = 0f;
			foreach (Ant item8 in GameManager.instance.EAnts())
			{
				if (item8 is AntInventor antInventor && antInventor.GetDeathProgress() > num2)
				{
					num2 = antInventor.GetDeathProgress();
				}
			}
			return Mathf.Clamp01((float)Progress.inventorsCompleted + num2);
		}
		case SubTaskType.REVEAL_BIOME:
			if (biomeValue == BiomeType.ANY)
			{
				return (GameManager.instance.GetGroundCount() > 1) ? 1 : 0;
			}
			foreach (Ground item9 in GameManager.instance.EGrounds())
			{
				if (item9.biome.biomeType == biomeValue)
				{
					return 1f;
				}
			}
			return 0f;
		case SubTaskType.N_REVEALED_ISLANDS:
			return GameManager.instance.EGrounds().ToList().Count - 1;
		case SubTaskType.CONNECT_LANDPAD:
			foreach (FlightPad item10 in GameManager.instance.ELaunchPads())
			{
				foreach (ClickableObject item11 in item10.EAssignedObjects())
				{
					if (item11 != null)
					{
						return 1f;
					}
				}
			}
			return 0f;
		case SubTaskType.N_INVENTOR_POINTS:
		{
			Dictionary<InventorPoints, int> dicInventorPoints = Progress.GetDicInventorPoints(preview: true);
			if (!dicInventorPoints.ContainsKey(inventorPointsValue))
			{
				return 0f;
			}
			return dicInventorPoints[inventorPointsValue];
		}
		case SubTaskType.N_PICKUPS_MINED:
			if (pickupValue == PickupType.ANY)
			{
				int num11 = 0;
				foreach (KeyValuePair<PickupType, int> item12 in Progress.pickupsMined)
				{
					num11 += item12.Value;
				}
				return num11;
			}
			if (!Progress.pickupsMined.ContainsKey(pickupValue))
			{
				return 0f;
			}
			return Progress.pickupsMined[pickupValue];
		case SubTaskType.N_PICKUPS_FED_TO_QUEEN:
			if (!Progress.pickupsFedToQueen.ContainsKey(pickupValue))
			{
				return 0f;
			}
			return Progress.pickupsFedToQueen[pickupValue];
		case SubTaskType.N_CORPSES:
		{
			int num9 = 0;
			foreach (Ant item13 in GameManager.instance.EAnts())
			{
				if (item13.IsDead())
				{
					num9++;
				}
			}
			return num9;
		}
		case SubTaskType.N_PICKUPS_OF_SINGLE_TYPE_IN_INVENTORY:
		{
			int num7 = 0;
			foreach (Ground item14 in GameManager.instance.EGrounds())
			{
				foreach (KeyValuePair<PickupType, int> item15 in item14.EInventory())
				{
					num7 = Mathf.Max(num7, item15.Value);
				}
			}
			return num7;
		}
		case SubTaskType.TOTAL_TRAIL_LENGTH:
		{
			float num5 = 0f;
			{
				foreach (Trail item16 in GameManager.instance.ETrails())
				{
					if (item16.trailType != TrailType.COMMAND)
					{
						num5 += item16.length;
					}
				}
				return num5;
			}
		}
		case SubTaskType.N_ANTS_UPGRADED_LIFESPAN_LOWER_THAN:
		{
			int num28 = 0;
			foreach (float item17 in Progress.remainingLifespansWhenUpgraded)
			{
				if (item17 < floatValue / 100f)
				{
					num28++;
				}
			}
			return num28;
		}
		case SubTaskType.N_OLD_ANTS_REPURPOSED:
			return Progress.nOldAntsUpgraded;
		case SubTaskType.CONNECT_CATAPULT:
			foreach (Catapult item18 in GameManager.instance.ECatapults())
			{
				foreach (ClickableObject item19 in item18.EAssignedObjects())
				{
					if (item19 != null)
					{
						return 1f;
					}
				}
			}
			return 0f;
		case SubTaskType.N_CATAPULT_THROWS_OTHER_ISLAND:
			return Progress.pickupsThrownToOtherIsland;
		case SubTaskType.N_ISLANDS_WITH_POPULATION:
		{
			int num24 = 0;
			List<Ground> list = new List<Ground>();
			foreach (Ground item20 in GameManager.instance.EGrounds())
			{
				if (item20.Population() > 0)
				{
					list.Add(item20);
				}
			}
			list.Sort((Ground g1, Ground g2) => g1.Population().CompareTo(g2.Population()));
			for (int num25 = 0; num25 < intValue && num25 < list.Count; num25++)
			{
				num24 += Mathf.Min(list[num25].Population(), Mathf.RoundToInt(floatValue));
			}
			return num24;
		}
		case SubTaskType.N_ISLANDS_WITH_BUILDINGS:
		{
			int num22 = 0;
			foreach (Ground item21 in GameManager.instance.EGrounds())
			{
				int num23 = 0;
				foreach (Building item22 in item21.EBuildings())
				{
					if (item22.IsPlaced() && item22.currentStatus == BuildingStatus.COMPLETED)
					{
						num23++;
					}
				}
				if ((float)num23 >= floatValue)
				{
					num22++;
				}
			}
			return num22;
		}
		case SubTaskType.ANT_SPEED:
			return Ant.TOP_SPEED;
		case SubTaskType.N_MADE_FROM_CASTE:
		{
			float num20 = 0f;
			if (Progress.antcastesMade.ContainsKey(antCasteValue))
			{
				num20 += (float)Progress.antcastesMade[antCasteValue];
			}
			{
				foreach (Building item23 in GameManager.instance.EBuildings())
				{
					if (!(item23 is Factory factory3))
					{
						continue;
					}
					FactoryRecipeData factoryRecipeData2 = FactoryRecipeData.Get(factory3.GetProcessingRecipe());
					if (factoryRecipeData2 == null)
					{
						continue;
					}
					foreach (AntCasteAmount productAnt3 in factoryRecipeData2.productAnts)
					{
						if (productAnt3.type == antCasteValue)
						{
							num20 += factory3.GetProcessTime() / factoryRecipeData2.processTime;
						}
					}
				}
				return num20;
			}
		}
		case SubTaskType.N_ANTS_LAUNCHED_TO_BIOME:
			if (biomeValue == BiomeType.ANY)
			{
				int num18 = 0;
				foreach (KeyValuePair<BiomeType, int> launchHitBiomesFromOtherIsland in Progress.launchHitBiomesFromOtherIslands)
				{
					num18 += launchHitBiomesFromOtherIsland.Value;
				}
				return num18;
			}
			if (Progress.launchHitBiomesFromOtherIslands.ContainsKey(biomeValue))
			{
				return Progress.launchHitBiomesFromOtherIslands[biomeValue];
			}
			return 0f;
		case SubTaskType.N_PICKUPS_OF_TYPE_IN_BIOME:
		{
			int num17 = 0;
			foreach (Ground item24 in GameManager.instance.EGrounds())
			{
				if (item24.biome.biomeType == biomeValue)
				{
					num17 += item24.GetInventoryAmount(pickupValue);
				}
			}
			return num17;
		}
		case SubTaskType.N_ANTS_RADEXPLODED_IN_AIR:
			return Progress.antsRadExplodedWhileAirborn;
		case SubTaskType.ENERGY_STORED:
		{
			float num16 = 0f;
			{
				foreach (BatteryBuilding item25 in GameManager.instance.EBatteryBuildings())
				{
					num16 += item25.storedEnergy;
				}
				return num16;
			}
		}
		case SubTaskType.FILL_UP_STOCKPILE:
		{
			int num14 = 0;
			foreach (Stockpile item26 in GameManager.instance.EStockpiles())
			{
				if (item26.data.code == stringValue)
				{
					num14 = Mathf.Max(num14, item26.GetCollectedAmount(pickupValue, BuildingStatus.COMPLETED, include_incoming: false));
				}
			}
			return num14;
		}
		case SubTaskType.CONNECT_DISPENSER_TO_BUILDING:
			foreach (Building item27 in GameManager.instance.EBuildings(stringValue))
			{
				foreach (BuildingAttachPoint buildingAttachPoint in item27.buildingAttachPoints)
				{
					if (buildingAttachPoint.HasDispenser(out var dis) && dis.HasExtractablePickup(ExchangeType.BUILDING_OUT, pickupValue))
					{
						return 1f;
					}
				}
			}
			return 0f;
		case SubTaskType.N_TECHS_UNLOCKED:
		{
			int num12 = 0;
			foreach (Tech tech in TechTree.techs)
			{
				if (tech.techType != TechType.IDEA && tech.status == TechStatus.DONE && !tech.alwaysDone)
				{
					num12++;
				}
			}
			return num12;
		}
		case SubTaskType.COMPLETE_TASK:
			if (Instinct.Get(stringValue).status == TaskStatus.COMPLETED)
			{
				return 1f;
			}
			return 0f;
		case SubTaskType.N_PICKUPS_MADE:
			if (Progress.pickupsManufactured.ContainsKey(pickupValue))
			{
				return Progress.pickupsManufactured[pickupValue];
			}
			return 0f;
		case SubTaskType.N_GYNES_IN_TOWERS:
		{
			int num10 = 0;
			foreach (Building item28 in GameManager.instance.EBuildings())
			{
				if (item28 is GyneTower gyneTower && gyneTower.HasGyne())
				{
					num10++;
				}
			}
			return num10;
		}
		case SubTaskType.USE_BEACON:
			foreach (Ant item29 in GameManager.instance.EAnts())
			{
				if (item29.HasStatusEffect(StatusEffect.CHARGED))
				{
					return 1f;
				}
			}
			return 0f;
		case SubTaskType.N_GYNES_FLOWN:
		{
			float num8 = 0f;
			{
				foreach (NuptialFlightData item30 in NuptialFlight.EFlightData())
				{
					num8 += item30.GetNGynesFlown();
				}
				return num8;
			}
		}
		case SubTaskType.FLY_OUT:
			foreach (NuptialFlightData item31 in NuptialFlight.EFlightData())
			{
				if (item31.dicFlownGynes.ContainsKey(antCasteValue) && item31.dicFlownGynes[antCasteValue] > 0)
				{
					return item31.GetProgress();
				}
			}
			return 0f;
		case SubTaskType.N_ISLANDS_WITH_BUILDING:
		{
			int num6 = 0;
			foreach (Ground item32 in GameManager.instance.EGrounds())
			{
				foreach (Building item33 in item32.EBuildings())
				{
					if (item33.data.code == stringValue && item33.IsPlaced() && item33.currentStatus == BuildingStatus.COMPLETED)
					{
						num6++;
						break;
					}
				}
			}
			return num6;
		}
		case SubTaskType.N_ANTS_INSIDE_BUILDING:
		{
			int num3 = 0;
			foreach (Building item34 in GameManager.instance.EBuildings(stringValue))
			{
				int num4 = 0;
				if (item34 is Factory factory)
				{
					foreach (Ant item35 in factory.antsInside)
					{
						if (item35 != null && item35.caste == antCasteValue)
						{
							num4++;
						}
					}
				}
				num3 = Mathf.Max(num3, num4);
			}
			return num3;
		}
		case SubTaskType.N_ANTS_IN_BEACON:
		{
			int num = 0;
			foreach (Ant item36 in GameManager.instance.EAnts())
			{
				if (item36.HasStatusEffect(StatusEffect.CHARGED))
				{
					num++;
				}
			}
			return num;
		}
		case SubTaskType.N_LARVAE_GROWN_IN_DESERT:
			return Progress.nLarvaeGrownInDesert;
		default:
			Debug.LogError("SubTask.GetValueCurrent: dont know type " + subTaskType);
			return 0f;
		}
	}
}
