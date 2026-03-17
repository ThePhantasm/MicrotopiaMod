using System.Collections.Generic;
using UnityEngine;

public class FeedingStation : Storage
{
	private float pullFromDispenserTimer;

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld)
		{
			return;
		}
		if (pullFromDispenserTimer > 0f)
		{
			pullFromDispenserTimer = Mathf.Clamp(pullFromDispenserTimer - dt, 0f, float.MaxValue);
		}
		if (!(pullFromDispenserTimer <= 0f) || GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true) != 0)
		{
			return;
		}
		foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
		{
			if (!buildingAttachPoint.HasDispenser(out var dis))
			{
				continue;
			}
			foreach (KeyValuePair<PickupType, int> dicAvailablePickup in dis.GetDicAvailablePickups(include_incoming: false))
			{
				if (PickupData.Get(dicAvailablePickup.Key).IsEdible())
				{
					Pickup pickup = dis.ExtractPickup(dicAvailablePickup.Key);
					pickup.SetStatus(PickupStatus.IN_CONTAINER, base.transform);
					OnPickupArrival_Intake(pickup, null);
				}
			}
		}
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (ant.GetCarryingPickupsCount() > 0)
		{
			return false;
		}
		foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
		{
			if (!buildingAttachPoint.HasDispenser(out var dis))
			{
				continue;
			}
			foreach (KeyValuePair<PickupType, int> dicAvailablePickup in dis.GetDicAvailablePickups(include_incoming: false))
			{
				if (PickupData.Get(dicAvailablePickup.Key).IsEdible())
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		return pullFromDispenserTimer <= 0f;
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		float result = 0f;
		if (GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false) > 0)
		{
			Pickup p = ExtractPickup(PickupType.ANY);
			_ant.EatPickup(p);
			result = 0.2f;
			pullFromDispenserTimer = 0.5f;
		}
		ant_entered = false;
		return result;
	}
}
