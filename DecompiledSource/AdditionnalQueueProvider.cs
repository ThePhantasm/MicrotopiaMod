using System.Collections.Generic;
using J4F;
using UnityEngine;

public class AdditionnalQueueProvider : QueueProvider
{
	public List<GameObject> addPrefabList;

	public override List<GameObject> GetPrefabs()
	{
		return addPrefabList;
	}
}
