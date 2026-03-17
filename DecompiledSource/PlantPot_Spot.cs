using UnityEngine;

public class PlantPot_Spot
{
	public Transform growPoint;

	public Plant plant;

	public Pickup seed;

	private bool growing;

	private float grown;

	private float growDuration;

	public PlantPot_Spot(Transform grow_point)
	{
		growPoint = grow_point;
		plant = null;
	}

	public void Write(Save save)
	{
		if (plant == null)
		{
			save.Write(0);
			return;
		}
		save.Write(seed.linkId);
		save.Write(plant.linkId);
		if (!growing)
		{
			save.Write(-1f);
			return;
		}
		save.Write(grown);
		save.Write(growDuration);
	}

	public void Read(Save save)
	{
		int num = save.ReadInt();
		if (num > 0)
		{
			seed = GameManager.instance.FindLink<Pickup>(num);
			plant = null;
			grown = save.ReadFloat();
			growing = grown >= 0f;
			if (growing)
			{
				growDuration = save.ReadFloat();
			}
		}
	}

	public void Plant(Pickup _seed)
	{
		seed = _seed;
		growing = false;
		if (seed.type != PickupType.SEED_BERRY)
		{
			Debug.LogError("No plant found for seed " + seed.type);
			return;
		}
		plant = null;
		if (!(plant == null))
		{
			grown = 0f;
			Debug.LogError("PlantPot: werkt nu niet meer, moet even gefixt worden voor nieuwe Plants");
		}
	}

	public void StartGrow(float dur)
	{
		growDuration = dur;
		growing = true;
	}

	public bool IsPlanted()
	{
		return plant != null;
	}

	public void SpotUpdate(float dt)
	{
		if (growing)
		{
			grown += dt / growDuration;
			if (grown >= 1f)
			{
				grown = 1f;
				growing = false;
			}
		}
	}

	public bool IsGrowing()
	{
		return growing;
	}

	public float GetTimeLeftGrowing()
	{
		return growDuration * (1f - grown);
	}
}
