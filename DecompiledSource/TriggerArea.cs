using System.Collections.Generic;
using UnityEngine;

public class TriggerArea : MonoBehaviour
{
	[SerializeField]
	private List<Collider> cols;

	[SerializeField]
	private List<Layers> layers;

	private List<Collider> hitCols = new List<Collider>();

	private void OnEnable()
	{
		hitCols.Clear();
	}

	private void OnTriggerStay(Collider other)
	{
		if ((layers.Count <= 0 || layers.Contains((Layers)other.gameObject.layer)) && layers.Contains((Layers)other.gameObject.layer) && !cols.Contains(other) && !hitCols.Contains(other))
		{
			hitCols.Add(other);
		}
	}

	public IEnumerable<Collider> EOverlapping()
	{
		foreach (Collider hitCol in hitCols)
		{
			if (hitCol != null)
			{
				yield return hitCol;
			}
		}
	}

	public void ResetOverlap()
	{
		hitCols.Clear();
	}
}
