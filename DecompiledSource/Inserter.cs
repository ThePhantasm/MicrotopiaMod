using System.Collections.Generic;

public class Inserter : Building
{
	private List<Pickup> stuckPickups = new List<Pickup>();

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld)
		{
			return;
		}
		foreach (Pickup item in new List<Pickup>(stuckPickups))
		{
			if (SendPickup(item))
			{
				stuckPickups.Remove(item);
			}
		}
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (stuckPickups.Count == 0)
		{
			return ground.GetStockpileSpace(_type) > 0;
		}
		return false;
	}

	protected override void OnPickupArrival_Intake(Pickup p, ExchangePoint point)
	{
		if (!SendPickup(p))
		{
			stuckPickups.Add(p);
		}
	}

	private bool SendPickup(Pickup p)
	{
		return GameManager.instance.TryExchangePickupToInventory(ground, base.transform.position, p);
	}
}
