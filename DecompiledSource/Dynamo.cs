using System.Collections.Generic;
using UnityEngine;

public class Dynamo : Storage
{
	[Header("Dynamo")]
	[SerializeField]
	private List<PickupType> activators = new List<PickupType>();

	[SerializeField]
	private int requiredCharges = 1;

	[SerializeField]
	private PickupType product = PickupType.ENERGY_POD;

	private int currentCharges;

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		return true;
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		foreach (PickupType item in _ant.ECarryingPickupTypes())
		{
			if (activators.Contains(item))
			{
				currentCharges++;
				if (currentCharges >= requiredCharges)
				{
					currentCharges = 0;
					Pickup pickup = GameManager.instance.SpawnPickup(product);
					pickup.SetStatus(PickupStatus.IN_CONTAINER, base.transform);
					OnPickupArrival_Intake(pickup, null);
					break;
				}
			}
		}
		ant_entered = false;
		return 0f;
	}
}
