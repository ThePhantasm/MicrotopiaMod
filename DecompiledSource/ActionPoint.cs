using UnityEngine;

public class ActionPoint
{
	public float trailProgress;

	public ConnectableObject connectableObject;

	public Trail mainTrail;

	public Trail actionTrail;

	public ExchangeType exchangeType;

	public bool activated = true;

	public ActionPoint(Trail _trail, ConnectableObject _connectable_object, bool _in_building = false)
	{
		trailProgress = -1f;
		mainTrail = _trail;
		connectableObject = _connectable_object;
		ExchangeType exchangeType = (mainTrail.trailType.IsBuildingTrail() ? ExchangeType.ENTER : _connectable_object.TrailInteraction(mainTrail));
		if (exchangeType == ExchangeType.NONE)
		{
			Debug.LogError($"Shouldn't happen; trail {_trail.name} ({_trail.trailType}), click_ob {_connectable_object.name}");
		}
		this.exchangeType = exchangeType;
		if (!_in_building)
		{
			actionTrail = GameManager.instance.NewTrail_Action(_trail.trailType);
			actionTrail.PlaceTrail(TrailStatus.ACTION);
			actionTrail.SetTrailShape(this.exchangeType);
			actionTrail.SetMaterial(mainTrail.trailType);
		}
	}

	public void UpdatePosition()
	{
		Vector3 best_pos_collider;
		if (mainTrail.IsInBuilding())
		{
			if (exchangeType != ExchangeType.ENTER)
			{
				Debug.LogWarning("action point in building " + connectableObject.name + ": unexpected exchangetype, not sure if placed correctly", connectableObject.gameObject);
			}
			trailProgress = 1f;
			best_pos_collider = ((actionTrail != null) ? mainTrail.GetPos(trailProgress) : Vector3.zero);
		}
		else if (mainTrail.commandTrailExchangeType != ExchangeType.NONE)
		{
			trailProgress = connectableObject.GetClosestProgress(mainTrail, out best_pos_collider, force_end: true);
		}
		else if (connectableObject.actionPointToCenter)
		{
			best_pos_collider = connectableObject.transform.position;
			trailProgress = mainTrail.GetProgressNear(best_pos_collider);
		}
		else
		{
			trailProgress = connectableObject.GetClosestProgress(mainTrail, out best_pos_collider);
		}
		if (actionTrail != null)
		{
			Vector3 pos = mainTrail.GetPos(trailProgress);
			if (!connectableObject.actionPointToCenter)
			{
				best_pos_collider += (best_pos_collider - pos).normalized * 1.5f;
			}
			actionTrail.SetStartEndPos(pos, best_pos_collider.TargetYPosition(pos.y));
		}
	}

	public void UpdatePin(bool clickable)
	{
		if (actionTrail != null)
		{
			actionTrail.SetPin(this, clickable);
		}
	}

	public ConnectableObject GetConnectedObject()
	{
		return connectableObject.GetObject();
	}

	public ExchangePoint GetExchangePoint()
	{
		return connectableObject.GetExchangePoint();
	}

	public int GetEntranceN()
	{
		if (mainTrail.entranceN == -1)
		{
			Debug.LogWarning("Tried to get entrance number from a trail where it wasn't set up, shouldn't happen");
			return 0;
		}
		return mainTrail.entranceN;
	}

	public void Delete()
	{
		foreach (Ant currentAnt in mainTrail.currentAnts)
		{
			currentAnt.RemoveActionPoint(this);
		}
		if (actionTrail != null)
		{
			actionTrail.Delete();
		}
	}

	public override string ToString()
	{
		return $"AP[{mainTrail.name}--{connectableObject.name} ({exchangeType}) p{trailProgress:0.00}]";
	}
}
