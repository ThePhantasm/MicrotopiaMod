using System.Collections.Generic;

public class CargoStation : CargoProcessor
{
	protected override bool IsLoaded(CargoAnt segment)
	{
		return segment.GetCarryingPickupsCount() > 0;
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		if (curSegment != null)
		{
			return !curSegment.IsFull();
		}
		return false;
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		incomingPickups_intake.Remove(_pickup);
		if (curSegment == null)
		{
			_pickup.transform.SetPositionAndRotation(base.transform.position, Toolkit.RandomYRotation());
			_pickup.SetStatus(PickupStatus.ON_GROUND);
		}
		else
		{
			curSegment.DirectAddPickup(_pickup);
			SetLoadDone();
		}
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange == ExchangeType.BUILDING_OUT && curSegment != null)
		{
			return curSegment.GetCarryingPickupsCount() > 0;
		}
		return false;
	}

	public override List<PickupType> GetExtractablePickupsInternal()
	{
		if (curSegment == null)
		{
			return ConnectableObject.emptyPickupList;
		}
		List<PickupType> list = new List<PickupType>();
		foreach (PickupType item in curSegment.ECarryingPickupTypes())
		{
			list.Add(item);
		}
		return list;
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		if (curSegment == null)
		{
			return null;
		}
		Pickup pickup = curSegment.DirectRetrievePickup(_type);
		if (pickup != null)
		{
			SetUnloadDone();
		}
		return pickup;
	}
}
