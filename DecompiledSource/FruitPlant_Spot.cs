using UnityEngine;

public class FruitPlant_Spot
{
	public Transform growPoint;

	public Pickup fruit;

	public float growTime;

	public float timeLeft;

	public FruitPlant_Spot(Transform grow_point)
	{
		growPoint = grow_point;
		fruit = null;
		growTime = 0f;
		timeLeft = 0f;
	}

	public bool HasFruit()
	{
		return fruit != null;
	}
}
