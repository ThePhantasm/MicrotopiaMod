using System.Collections.Generic;
using UnityEngine;

public class UnlockerSpawner : MonoBehaviour
{
	public void SpawnUnlocker()
	{
		List<BuildingData> list = new List<BuildingData>();
		foreach (BuildingData building in PrefabData.buildings)
		{
			if (building.code.Contains("UNLOCKER_"))
			{
				list.Add(building);
			}
		}
		Object.Instantiate(list[Random.Range(0, list.Count)].prefab, base.transform).GetComponent<Building>().transform.rotation = Quaternion.identity.RandomYRotation();
	}
}
