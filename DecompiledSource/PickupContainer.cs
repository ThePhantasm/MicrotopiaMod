using System;
using System.Collections.Generic;
using UnityEngine;

public class PickupContainer : ConnectableObject
{
	[Header("Pickup Container")]
	public Transform insertPoint;

	public Transform extractPoint;

	[NonSerialized]
	public bool extractablePickupsChanged;

	private List<PickupType> cachedExtractablePickups = new List<PickupType>();

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		extractablePickupsChanged = true;
	}

	public virtual Vector3 GetInsertPos(Pickup pickup = null)
	{
		if (insertPoint != null)
		{
			return insertPoint.position;
		}
		return base.transform.position;
	}

	public virtual bool CanInsert(PickupType _type, ExchangeType type, ExchangePoint _point, ref bool let_ant_wait, bool show_billboard = false)
	{
		return false;
	}

	public virtual void PrepareForPickup(Pickup _pickup, ExchangePoint _point)
	{
	}

	public virtual void OnPickupArrival(Pickup _pickup, ExchangePoint point)
	{
		_pickup.SetStatus(PickupStatus.IN_CONTAINER, base.transform);
	}

	public virtual Vector3 GetExtractPos()
	{
		if (extractPoint != null)
		{
			return extractPoint.position;
		}
		return base.transform.position;
	}

	public virtual bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		return false;
	}

	public virtual List<PickupType> GetExtractablePickups(ExchangeType exchange)
	{
		if (extractablePickupsChanged)
		{
			cachedExtractablePickups = GetExtractablePickupsInternal();
			extractablePickupsChanged = false;
		}
		return cachedExtractablePickups;
	}

	public virtual List<PickupType> GetExtractablePickupsInternal()
	{
		return ConnectableObject.emptyPickupList;
	}

	public virtual bool HasExtractablePickup(ExchangeType exchange, PickupType pickup)
	{
		return GetExtractablePickups(exchange).Contains(pickup);
	}

	public virtual Pickup ExtractPickup(PickupType _type)
	{
		return null;
	}

	public virtual string ExchangeDescription(ExchangeType _type)
	{
		return _type switch
		{
			ExchangeType.BUILDING_IN => Loc.GetUI("BUILDING_INSERT_ZONE"), 
			ExchangeType.BUILDING_OUT => Loc.GetUI("BUILDING_EXTRACT_ZONE"), 
			ExchangeType.BUILDING_PROCESS => Loc.GetUI("BUILDING_PROCESS_ZONE"), 
			_ => "?? DONT KNOW EXCHANGE TYPE " + _type.ToString() + " ??", 
		};
	}
}
