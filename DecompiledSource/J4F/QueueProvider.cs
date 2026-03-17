using System.Collections.Generic;
using UnityEngine;

namespace J4F;

public class QueueProvider : MonoBehaviour
{
	private void Start()
	{
	}

	public virtual List<GameObject> GetPrefabs()
	{
		return new List<GameObject>();
	}
}
