using System;
using System.Collections.Generic;
using UnityEngine;

public class SequenceAction : ListItemWithParams
{
	public SequenceActionType actionType;

	private int intValue;

	private float floatValue;

	private string stringValue;

	public SequenceAction(string txt)
		: base(txt)
	{
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "SequenceAction";
		if (!Enum.TryParse<SequenceActionType>(strs[0].Trim(), out var result))
		{
			Debug.LogWarning(className + ": '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
			return;
		}
		switch (result)
		{
		case SequenceActionType.RESEARCH_COST_SET:
			if (ArgCountOk(txt, strs, 1))
			{
				intValue = strs[1].Trim().ToInt(0, className + ": '" + txt + "' parse error");
			}
			break;
		case SequenceActionType.RESEARCH_TIME_SET:
			if (ArgCountOk(txt, strs, 1))
			{
				floatValue = strs[1].Trim().ToFloat(0f, className + ": '" + txt + "' parse error");
			}
			break;
		case SequenceActionType.SET_MIDDLE_MESSAGE:
		case SequenceActionType.ENABLE_UI:
		case SequenceActionType.DISABLE_UI:
		case SequenceActionType.UNLOCK_BUILDING_SILENT:
		case SequenceActionType.UNLOCK_BUILDING_POPUP:
		case SequenceActionType.RESEARCH_UNLOCK_SET:
			if (ArgCountOk(txt, strs, 1))
			{
				stringValue = strs[1].Trim();
			}
			break;
		case SequenceActionType.ENABLE_ARROW_POINTER_3D:
			if (ArgCountOk(txt, strs, 2))
			{
				stringValue = strs[1].Trim();
				floatValue = strs[2].Trim().ToFloat(0f, className + ": '" + txt + "' parse error");
			}
			break;
		}
		if (result == SequenceActionType.ENABLE_UI || (uint)(result - 7) <= 1u)
		{
			BuildingData.CheckBuildingCode(stringValue, "SequenceAction: ");
		}
	}

	public static List<SequenceAction> ParseList(string str)
	{
		List<SequenceAction> list = new List<SequenceAction>();
		foreach (string item in str.EListItems())
		{
			list.Add(new SequenceAction(item));
		}
		return list;
	}

	public void PerformAction()
	{
		switch (actionType)
		{
		case SequenceActionType.ENABLE_ARROW_POINTER_3D:
			GameManager.instance.tutorialGround.SetArrowPointer(stringValue, floatValue);
			break;
		case SequenceActionType.DISABLE_ARROW_POINTER_3D:
			GameManager.instance.tutorialGround.SetArrowPointer("");
			break;
		case SequenceActionType.SET_PLACE_QUEEN_TRUE:
			Gameplay.CAN_PLACE_QUEEN = true;
			break;
		case SequenceActionType.SET_PLACE_QUEEN_FALSE:
			Gameplay.CAN_PLACE_QUEEN = false;
			break;
		case SequenceActionType.UNLOCK_BUILDING_SILENT:
			Progress.UnlockBuilding(stringValue);
			break;
		case SequenceActionType.UNLOCK_BUILDING_POPUP:
			Progress.UnlockBuilding(stringValue);
			break;
		case SequenceActionType.RESEARCH_COST_SET:
			Sequence.TUTORIAL_RESEARCH_COST = intValue;
			break;
		case SequenceActionType.RESEARCH_COST_REVERT:
			Sequence.TUTORIAL_RESEARCH_COST = -1;
			break;
		case SequenceActionType.RESEARCH_TIME_SET:
			Sequence.TUTORIAL_RESEARCH_TIME = floatValue;
			break;
		case SequenceActionType.RESEARCH_TIME_REVERT:
			Sequence.TUTORIAL_RESEARCH_TIME = -1f;
			break;
		case SequenceActionType.RESEARCH_UNLOCK_SET:
			Sequence.TUTORIAL_RESEARCH_UNLOCK = stringValue;
			break;
		case SequenceActionType.RESEARCH_UNLOCK_REVERT:
			Sequence.TUTORIAL_RESEARCH_UNLOCK = "";
			break;
		case SequenceActionType.ENABLE_UI:
		case SequenceActionType.DISABLE_UI:
			break;
		}
	}
}
