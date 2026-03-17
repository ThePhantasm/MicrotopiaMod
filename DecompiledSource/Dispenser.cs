using System.Collections.Generic;

public class Dispenser : Building
{
	public virtual Dictionary<PickupType, int> GetDicAvailablePickups(bool include_incoming)
	{
		return new Dictionary<PickupType, int>();
	}

	public virtual List<PickupType> GetAllowedPickups()
	{
		return new List<PickupType>();
	}

	public override void OnSetAttached(Building _target)
	{
		base.OnSetAttached(_target);
		ExchangePoint[] array = exchangePoints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetObActive(_target == null);
		}
	}

	protected override bool HasHologram()
	{
		return true;
	}

	public override HologramShape GetHologramShape(out PickupType _pickup, out AntCaste _ant)
	{
		_pickup = PickupType.NONE;
		_ant = AntCaste.NONE;
		if (GetCurrentBillboard(out var _, out var _, out var _, out var _) != BillboardType.NONE)
		{
			return HologramShape.None;
		}
		List<PickupType> allowedPickups = GetAllowedPickups();
		if (allowedPickups.Count == 1 && allowedPickups[0] != PickupType.NONE)
		{
			_pickup = allowedPickups[0];
			return HologramShape.Pickup;
		}
		return base.GetHologramShape(out _pickup, out _ant);
	}
}
