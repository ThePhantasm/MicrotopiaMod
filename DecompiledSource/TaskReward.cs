using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskReward : ListItemWithParams
{
	public TaskRewardType type;

	private int intValue;

	private float floatValue;

	private string stringValue;

	private TrailType trailTypeValue;

	private Tutorial tutorialValue;

	private string recipeValue;

	public TaskReward(string txt)
		: base(txt)
	{
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "TaskReward";
		if (!Enum.TryParse<TaskRewardType>(strs[0].Trim(), out type))
		{
			Debug.LogWarning("TaskReward: '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
			return;
		}
		switch (type)
		{
		case TaskRewardType.GIVE_QUEEN_ENERGY:
			if (ArgCountOk(txt, strs, 1))
			{
				floatValue = strs[1].Trim().ToFloat(0f, "SubTask: '" + txt + "' parse error");
			}
			break;
		case TaskRewardType.GENERAL_UNLOCK:
			if (ArgCountOk(txt, strs, 1))
			{
				GeneralUnlocks generalUnlocks = Progress.ParseGeneralUnlock(strs[1].Trim());
				intValue = (int)generalUnlocks;
			}
			break;
		case TaskRewardType.REVEAL_BIOME:
		case TaskRewardType.GIVE_TECH:
			if (ArgCountOk(txt, strs, 1))
			{
				stringValue = strs[1].Trim();
			}
			break;
		case TaskRewardType.BUILDING:
			if (ArgCountOk(txt, strs, 1))
			{
				stringValue = strs[1].Trim();
				BuildingData.CheckBuildingCode(stringValue, "TaskReward: ");
			}
			break;
		case TaskRewardType.TRAILTYPE:
			if (ArgCountOk(txt, strs, 1))
			{
				trailTypeValue = TrailData.ParseTrailType(strs[1].Trim());
			}
			break;
		case TaskRewardType.RECIPE:
			if (ArgCountOk(txt, strs, 1))
			{
				recipeValue = strs[1].Trim();
			}
			break;
		case TaskRewardType.TUTORIAL:
			if (ArgCountOk(txt, strs, 1))
			{
				tutorialValue = UITutorial.ParseTutorialScreen(strs[1].Trim());
			}
			break;
		case TaskRewardType.TUTORIAL_AFTER_TIME:
			if (ArgCountOk(txt, strs, 2))
			{
				tutorialValue = UITutorial.ParseTutorialScreen(strs[1].Trim());
				floatValue = strs[2].Trim().ToFloat(0f, "SubTask: '" + txt + "' parse error");
			}
			break;
		default:
			Debug.LogWarning("TaskReward: '" + txt + "' parse error (enum '" + strs[0] + "' valid but unknown)");
			break;
		case TaskRewardType.UNLOCK_EVERYTHING:
		case TaskRewardType.RESET_CAMERA_DISTANCES:
		case TaskRewardType.QUEEN_MAKE_LARVA:
		case TaskRewardType.ADD_REVEAL:
			break;
		}
	}

	public static List<TaskReward> ParseList(string str)
	{
		List<TaskReward> list = new List<TaskReward>();
		foreach (string item in str.EListItems())
		{
			list.Add(new TaskReward(item));
		}
		return list;
	}

	public void Give(bool during_load = false)
	{
		switch (type)
		{
		case TaskRewardType.BUILDING:
			Progress.UnlockBuilding(stringValue, during_load);
			break;
		case TaskRewardType.TRAILTYPE:
			Progress.UnlockTrail(trailTypeValue, during_load);
			break;
		case TaskRewardType.GENERAL_UNLOCK:
			Progress.Unlock((GeneralUnlocks)intValue);
			break;
		case TaskRewardType.UNLOCK_EVERYTHING:
			foreach (TrailData trail in PrefabData.trails)
			{
				Progress.UnlockTrail(trail.type, during_load);
			}
			{
				foreach (BuildingData building in PrefabData.buildings)
				{
					Progress.UnlockBuilding(building.code, during_load);
				}
				break;
			}
		case TaskRewardType.GIVE_QUEEN_ENERGY:
			if (during_load)
			{
				break;
			}
			{
				foreach (Queen item in GameManager.instance.EQueens())
				{
					item.AddEnergy(floatValue);
				}
				break;
			}
		case TaskRewardType.QUEEN_MAKE_LARVA:
			if (during_load)
			{
				break;
			}
			{
				foreach (Queen item2 in GameManager.instance.EQueens())
				{
					item2.AddBonusLarva(1);
				}
				break;
			}
		case TaskRewardType.TUTORIAL:
			if (!during_load)
			{
				UIGame.instance.SetTutorial(tutorialValue);
			}
			break;
		case TaskRewardType.TUTORIAL_AFTER_TIME:
			if (!during_load)
			{
				UIGame.instance.SetTutorialAfterTime(tutorialValue, floatValue);
			}
			break;
		case TaskRewardType.RESET_CAMERA_DISTANCES:
			if (!during_load)
			{
				CamController.instance.ResetDis();
			}
			break;
		case TaskRewardType.REVEAL_BIOME:
			if (!during_load)
			{
				if (stringValue == "RANDOM")
				{
					GameManager.instance.AddBiome();
				}
				else
				{
					GameManager.instance.AddBiome(stringValue);
				}
			}
			break;
		case TaskRewardType.RECIPE:
			Progress.UnlockRecipe(recipeValue);
			break;
		case TaskRewardType.GIVE_TECH:
			TechTree.GiveTech(stringValue);
			break;
		case TaskRewardType.ADD_REVEAL:
			Progress.AddReveal();
			break;
		}
	}
}
