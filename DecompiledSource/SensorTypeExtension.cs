public static class SensorTypeExtension
{
	public static string GetTitle(this SensorType _type, bool not = false)
	{
		switch (_type)
		{
		case SensorType.IS_CARRYING_PICKUP:
			if (not)
			{
				return Loc.GetUI("SENSOR_CARRY_NOT");
			}
			return Loc.GetUI("SENSOR_CARRY");
		case SensorType.IS_CARRYING_PICKUP_TYPE:
			if (not)
			{
				return Loc.GetUI("SENSOR_CARRYMAT_NOT");
			}
			return Loc.GetUI("SENSOR_CARRYMAT");
		case SensorType.IS_CASTE:
			if (not)
			{
				return Loc.GetUI("SENSOR_CASTE_NOT");
			}
			return Loc.GetUI("SENSOR_CASTE");
		case SensorType.ENERGY_HIGHER_THAN:
			if (not)
			{
				return Loc.GetUI("SENSOR_ENERGYHIGHER_NOT");
			}
			return Loc.GetUI("SENSOR_ENERGYHIGHER");
		case SensorType.ENERGY_LOWER_THAN:
			if (not)
			{
				return Loc.GetUI("SENSOR_ENERGYLOWER_NOT");
			}
			return Loc.GetUI("SENSOR_ENERGYLOWER");
		case SensorType.ONE_IN_N:
			if (not)
			{
				return Loc.GetUI("SENSOR_ENERY_NOT");
			}
			return Loc.GetUI("SENSOR_EVERY");
		case SensorType.RANDOM_PERCENTAGE:
			if (not)
			{
				return Loc.GetUI("SENSOR_RANDOM_NOT");
			}
			return Loc.GetUI("SENSOR_RANDOM");
		default:
			return "";
		}
	}
}
