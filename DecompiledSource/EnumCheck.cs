using UnityEngine;

public static class EnumCheck
{
	public static bool IsValidType(this TrailType type)
	{
		if (type == TrailType.NONE)
		{
			return false;
		}
		return true;
	}

	public static bool IsCategory(this PickupType type, PickupCategory cat)
	{
		if (cat == PickupCategory.NONE)
		{
			return false;
		}
		return PickupData.Get(type).categories.Contains(cat);
	}

	public static bool EveryAntCanDo(this ExchangeType exchange)
	{
		switch (exchange)
		{
		case ExchangeType.BUILDING_IN:
		case ExchangeType.BUILDING_OUT:
		case ExchangeType.BUILDING_PROCESS:
		case ExchangeType.DROP:
		case ExchangeType.DELETE:
		case ExchangeType.ENTER:
		case ExchangeType.EXIT:
			return true;
		default:
			return false;
		}
	}

	public static PickupType GetExamplePickupType(this PickupCategory cat)
	{
		switch (cat)
		{
		case PickupCategory.FIBER:
			return PickupType.FIBER_SPIKETREE;
		case PickupCategory.ENERGY:
			return PickupType.ENERGY_POD;
		case PickupCategory.LIVING:
			return PickupType.LARVAE_T1;
		default:
			Debug.LogWarning("Don't know example pickup type for category " + cat);
			return PickupType.ENERGY_POD;
		}
	}
}
