using System;
using System.Collections.Generic;
using UnityEngine;

public class SequenceCheck : ListItemWithParams
{
	public SequenceCheckType checkType;

	private bool isSatisfied;

	private float floatValue;

	private string nameValue;

	private bool boolValue;

	private int intValue;

	private Vector2 rangeValue;

	public SequenceCheck(string txt)
		: base(txt)
	{
	}

	protected override void Parse(string txt, string[] strs)
	{
		className = "SequenceCheck";
		if (!Enum.TryParse<SequenceCheckType>(strs[0].Trim(), out var result))
		{
			Debug.LogWarning(className + ": '" + txt + "' parse error (enum '" + strs[0] + "' invalid)");
			return;
		}
		switch (result)
		{
		case SequenceCheckType.TIME_MOVED_UP:
		case SequenceCheckType.TIME_MOVED_DOWN:
		case SequenceCheckType.TIME_MOVED_LEFT:
		case SequenceCheckType.TIME_MOVED_RIGHT:
		case SequenceCheckType.TIME_ZOOMED_IN:
		case SequenceCheckType.TIME_ZOOMED_OUT:
		case SequenceCheckType.TIME_ROTATED_CAMERA:
			if (ArgCountOk(txt, strs, 1))
			{
				floatValue = strs[1].Trim().ToFloat(0f, className + ": '" + txt + "' parse error");
			}
			break;
		case SequenceCheckType.N_QUEENS:
		case SequenceCheckType.N_QUEENS_GOT_PICKUPS:
		case SequenceCheckType.N_ANTS:
		case SequenceCheckType.N_ANTS_WALKING_ON_TRAIL:
		case SequenceCheckType.N_LARVAE_IN_BUILDING:
		case SequenceCheckType.N_TRAILS:
		case SequenceCheckType.N_TRAILS_MAX:
		case SequenceCheckType.N_TRAILS_REMOVED:
		case SequenceCheckType.N_BUILDINGS_UNLOCKED:
		case SequenceCheckType.N_BUILDINGS_PLACED:
		case SequenceCheckType.N_BUILDINGS_BEING_BUILD:
		case SequenceCheckType.N_BUILDINGS_COMPLETED:
			if (ArgCountOk(txt, strs, 1))
			{
				intValue = strs[1].Trim().ToInt(0, className + ": '" + txt + "' parse error");
			}
			break;
		case SequenceCheckType.IS_GAMEPLAYMODE:
		case SequenceCheckType.IS_TRAILTYPE:
		case SequenceCheckType.IS_NOT_GROUNDGROUP:
			if (ArgCountOk(txt, strs, 1))
			{
				nameValue = strs[1].Trim();
			}
			break;
		case SequenceCheckType.N_ANTS_CARRYING_PICKUP:
		case SequenceCheckType.N_ANTS_CARRYING_PICKUP_CATEGORY:
		case SequenceCheckType.N_PICKUPS_TYPE:
			if (ArgCountOk(txt, strs, 2))
			{
				nameValue = strs[1].Trim();
				intValue = strs[2].Trim().ToInt(0, className + ": '" + txt + "' parse error");
			}
			break;
		}
		switch (result)
		{
		case SequenceCheckType.IS_GAMEPLAYMODE:
			Toolkit.CheckEnum(nameValue, typeof(GameplayMode), "SEQUENCECHECK");
			break;
		case SequenceCheckType.IS_TRAILTYPE:
			Toolkit.CheckEnum(nameValue, typeof(TrailType), "SEQUENCECHECK");
			break;
		case SequenceCheckType.IS_NOT_GROUNDGROUP:
			Toolkit.CheckEnum(nameValue, typeof(GroundGroup), "SEQUENCECHECK");
			break;
		case SequenceCheckType.N_ANTS_CARRYING_PICKUP:
		case SequenceCheckType.N_PICKUPS_TYPE:
			Toolkit.CheckEnum(nameValue, typeof(PickupType), "SEQUENCECHECK");
			break;
		case SequenceCheckType.N_ANTS_CARRYING_PICKUP_CATEGORY:
			Toolkit.CheckEnum(nameValue, typeof(PickupCategory), "SEQUENCECHECK");
			break;
		}
	}

	public static List<SequenceCheck> ParseList(string str)
	{
		List<SequenceCheck> list = new List<SequenceCheck>();
		foreach (string item in str.EListItems())
		{
			list.Add(new SequenceCheck(item));
		}
		return list;
	}

	public void ResetSequenceCheck()
	{
		isSatisfied = false;
	}

	public bool SequenceCheckSatisfied()
	{
		if (isSatisfied)
		{
			return true;
		}
		int num = 0;
		switch (checkType)
		{
		case SequenceCheckType.N_QUEENS:
			if (GameManager.instance.GetQueenCount() >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_QUEENS_GOT_PICKUPS:
			foreach (Queen item in GameManager.instance.EQueens())
			{
				if (item.energy > 0f)
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_ANTS:
			foreach (Ant item2 in GameManager.instance.EAnts())
			{
				if (item2.moveState != MoveState.Animated)
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_ANTS_CARRYING_PICKUP:
			foreach (Ant item3 in GameManager.instance.EAnts())
			{
				foreach (PickupType item4 in item3.ECarryingPickupTypes())
				{
					if (item4.ToString() == nameValue.ToUpper())
					{
						num++;
						break;
					}
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_ANTS_CARRYING_PICKUP_CATEGORY:
			foreach (Ant item5 in GameManager.instance.EAnts())
			{
				foreach (PickupType item6 in item5.ECarryingPickupTypes())
				{
					if (item6.IsCategory((PickupCategory)Enum.Parse(typeof(PickupCategory), nameValue)))
					{
						num++;
						break;
					}
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_ANTS_WALKING_ON_TRAIL:
			foreach (Ant item7 in GameManager.instance.EAnts())
			{
				if (item7.currentTrail != null)
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_PICKUPS_TYPE:
			foreach (Pickup item8 in GameManager.instance.EAllPickups())
			{
				if (item8.type.ToString() == nameValue.ToUpper())
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_TRAILS:
			if (GameManager.instance.GetTrailCount() >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_TRAILS_MAX:
			if (GameManager.instance.GetTrailCount() <= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_TRAILS_REMOVED:
			if (Gameplay.N_TRAIL_DESTROYED >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_BUILDINGS_UNLOCKED:
			if (Progress.GetUnlockedBuildings().Count >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_BUILDINGS_PLACED:
			foreach (Building item9 in GameManager.instance.EBuildings())
			{
				if (item9.currentStatus == BuildingStatus.BUILDING || item9.currentStatus == BuildingStatus.COMPLETED)
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_BUILDINGS_BEING_BUILD:
			foreach (Building item10 in GameManager.instance.EBuildings())
			{
				if (item10.currentStatus == BuildingStatus.BUILDING && item10.dicCollectedPickups_build.Count > 0)
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.N_BUILDINGS_COMPLETED:
			foreach (Building item11 in GameManager.instance.EBuildings())
			{
				if (item11.currentStatus == BuildingStatus.COMPLETED)
				{
					num++;
				}
			}
			if (num >= intValue)
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.IS_TRAILTYPE:
			if (Gameplay.instance.GetTrailType().ToString() == nameValue.ToUpper())
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.IS_NOT_TRAILING:
			if (!Gameplay.instance.IsDrawingTrail())
			{
				isSatisfied = true;
			}
			break;
		case SequenceCheckType.IS_NOT_GROUNDGROUP:
			if (GameManager.instance.tutorialGround.group.ToString() != nameValue.ToUpper())
			{
				isSatisfied = true;
			}
			break;
		}
		return isSatisfied;
	}
}
