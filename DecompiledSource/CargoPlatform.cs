using System.Collections.Generic;
using UnityEngine;

public class CargoPlatform : CargoProcessor
{
	[Header("Cargo Platform")]
	[SerializeField]
	private BuildingTrail inputTrail;

	[SerializeField]
	private BuildingTrail outputTrail;

	private bool droppingAnt;

	protected override List<BuildingTrail> GetBuildingTrails()
	{
		return new List<BuildingTrail>(buildingTrails) { inputTrail, outputTrail };
	}

	protected override bool IsLoaded(CargoAnt segment)
	{
		if (droppingAnt)
		{
			if (segment.carriedAnt == null)
			{
				droppingAnt = false;
			}
			return false;
		}
		return segment.carriedAnt != null;
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		if (curSegment != null && curSegment.carriedAnt == null)
		{
			return _ant.data.canBeCarried;
		}
		return false;
	}

	public override float UseBuilding(int _entrance, Ant ant, out bool ant_entered)
	{
		curSegment.PickupAnt(ant);
		SetLoadDone();
		ant_entered = false;
		return 0f;
	}

	protected override void SetReady(CargoAnt segment)
	{
		base.SetReady(segment);
		droppingAnt = false;
		Ant carriedAnt = segment.carriedAnt;
		if (carriedAnt != null)
		{
			Trail trail = listSpawnedTrails[2][0];
			curSegment.DropAnt(carriedAnt, trail);
			droppingAnt = true;
		}
	}
}
