using System.Collections.Generic;
using UnityEngine;

public class PlantData
{
	private static Dictionary<PlantType, PlantData> dicPlantData;

	public PlantType type;

	public float mass;

	public float dominance;

	public float growTime;

	public float wiltTime;

	public float spreadDelay;

	public float wiltDelay;

	public float clustering;

	public float distMin;

	public float distMax;

	public bool evenClustering;

	public MinMax pollutionRange;

	public float pollutionTolerance;

	public MinMax scaleRange;

	public bool ignoreGrooves;

	public static PlantData Get(PlantType plant_type)
	{
		if (dicPlantData == null)
		{
			dicPlantData = new Dictionary<PlantType, PlantData>();
			foreach (PlantData plant in PrefabData.plants)
			{
				dicPlantData.Add(plant.type, plant);
			}
		}
		if (dicPlantData.TryGetValue(plant_type, out var value))
		{
			return value;
		}
		Debug.LogWarning("PlantData: Couldn't find plant with plant type " + plant_type);
		return null;
	}
}
