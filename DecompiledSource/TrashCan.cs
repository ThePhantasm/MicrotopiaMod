public class TrashCan : Building
{
	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		return true;
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		if (incomingPickups_intake.Contains(_pickup))
		{
			incomingPickups_intake.Remove(_pickup);
		}
		_pickup.Delete();
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}
}
