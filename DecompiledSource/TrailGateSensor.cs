using UnityEngine;

public class TrailGateSensor
{
	public SensorType sensorType;

	public bool not;

	public PickupType pickupType;

	public AntCaste antCaste;

	public int intValue;

	public float floatValue;

	private int intCounter;

	public static float MAX_ENERGY;

	public TrailGateSensor(SensorType sensor_type)
	{
		SetSensor(sensor_type);
	}

	public void SetSensor(SensorType sensor_type)
	{
		sensorType = sensor_type;
		not = false;
		pickupType = PickupType.NONE;
		antCaste = AntCaste.NONE;
		intValue = 0;
		floatValue = 0f;
		switch (sensorType)
		{
		case SensorType.IS_CARRYING_PICKUP_TYPE:
			pickupType = PickupType.BERRY;
			break;
		case SensorType.IS_CASTE:
			antCaste = AntCaste.SENTRY;
			break;
		case SensorType.ENERGY_HIGHER_THAN:
			floatValue = 0f;
			break;
		case SensorType.ENERGY_LOWER_THAN:
			floatValue = GetMaxValue();
			break;
		case SensorType.ONE_IN_N:
			intValue = 3;
			break;
		case SensorType.RANDOM_PERCENTAGE:
			floatValue = 50f;
			break;
		}
		intCounter = 0;
	}

	public TrailGateSensor(TrailGateSensor other)
	{
		sensorType = other.sensorType;
		not = other.not;
		pickupType = other.pickupType;
		antCaste = other.antCaste;
		intValue = other.intValue;
		floatValue = other.floatValue;
		intCounter = other.intCounter;
	}

	public TrailGateSensor(Save from_save)
	{
		sensorType = (SensorType)from_save.ReadInt();
		not = from_save.ReadBool();
		pickupType = PickupType.NONE;
		antCaste = AntCaste.NONE;
		intValue = 0;
		floatValue = 0f;
		intCounter = 0;
		switch (sensorType)
		{
		case SensorType.IS_CARRYING_PICKUP_TYPE:
			pickupType = (PickupType)from_save.ReadInt();
			break;
		case SensorType.IS_CASTE:
			antCaste = (AntCaste)from_save.ReadInt();
			break;
		case SensorType.ENERGY_LOWER_THAN:
		case SensorType.ENERGY_HIGHER_THAN:
		case SensorType.RANDOM_PERCENTAGE:
			floatValue = from_save.ReadFloat();
			break;
		case SensorType.ONE_IN_N:
			intValue = from_save.ReadInt();
			intCounter = from_save.ReadInt();
			break;
		}
	}

	public void Write(Save save)
	{
		save.Write((int)sensorType);
		save.Write(not);
		switch (sensorType)
		{
		case SensorType.IS_CARRYING_PICKUP_TYPE:
			save.Write((int)pickupType);
			break;
		case SensorType.IS_CASTE:
			save.Write((int)antCaste);
			break;
		case SensorType.ENERGY_LOWER_THAN:
		case SensorType.ENERGY_HIGHER_THAN:
		case SensorType.RANDOM_PERCENTAGE:
			save.Write(floatValue);
			break;
		case SensorType.ONE_IN_N:
			save.Write(intValue);
			save.Write(intCounter);
			break;
		}
	}

	public bool IsSatisfied(Ant ant, bool final)
	{
		bool flag = false;
		switch (sensorType)
		{
		case SensorType.IS_CARRYING_PICKUP:
			flag = ant.GetCarryingPickupsCount() > 0;
			break;
		case SensorType.IS_CARRYING_PICKUP_TYPE:
			foreach (PickupType item in ant.ECarryingPickupTypes())
			{
				if (item == pickupType)
				{
					flag = true;
				}
			}
			break;
		case SensorType.IS_CASTE:
			flag = ant.caste == antCaste;
			break;
		case SensorType.ENERGY_LOWER_THAN:
			flag = ant.energy < floatValue;
			break;
		case SensorType.ENERGY_HIGHER_THAN:
			flag = ant.energy > floatValue;
			break;
		case SensorType.ONE_IN_N:
			if (intValue == 0)
			{
				break;
			}
			flag = intCounter == 0;
			if (final)
			{
				intCounter++;
				if (intCounter >= intValue)
				{
					intCounter = 0;
				}
			}
			break;
		case SensorType.RANDOM_PERCENTAGE:
			flag = Random.Range(0f, 100f) < floatValue;
			break;
		}
		if (!not)
		{
			return flag;
		}
		return !flag;
	}

	public float GetMaxValue()
	{
		switch (sensorType)
		{
		case SensorType.ENERGY_LOWER_THAN:
		case SensorType.ENERGY_HIGHER_THAN:
			if (MAX_ENERGY == 0f)
			{
				foreach (AntCasteData antCaste in PrefabData.antCastes)
				{
					if (antCaste.energy > MAX_ENERGY)
					{
						MAX_ENERGY = antCaste.energy;
					}
				}
			}
			return MAX_ENERGY;
		case SensorType.ONE_IN_N:
			return 100f;
		case SensorType.RANDOM_PERCENTAGE:
			return 100f;
		default:
			return 0f;
		}
	}
}
