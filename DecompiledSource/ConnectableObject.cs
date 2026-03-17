using System;
using System.Collections.Generic;
using UnityEngine;

public class ConnectableObject : ClickableObject
{
	protected List<Trail> nearbyTrails = new List<Trail>();

	protected static List<PickupType> emptyPickupList = new List<PickupType>();

	protected static List<ConnectableObject> toUpdateActionPoints = new List<ConnectableObject>();

	public const int ACTION_POINTS_BLOCK_DIST = 3;

	private Collider[] actionPointColliders;

	[NonSerialized]
	public bool actionPointToCenter;

	public virtual IEnumerable<ExchangeType> EPossibleExchangeTypes()
	{
		yield break;
	}

	public virtual bool HasExchangeType(ExchangeType exchange_type)
	{
		return false;
	}

	public virtual ExchangeType TrailInteraction(Trail _trail)
	{
		return ExchangeType.NONE;
	}

	public bool HasTrailInteraction(Trail _trail)
	{
		TrailType trailType = _trail.trailType;
		if (trailType == TrailType.ELDER || (uint)(trailType - 49) <= 1u)
		{
			return false;
		}
		return TrailInteraction(_trail) != ExchangeType.NONE;
	}

	public void DirectReconnect()
	{
		DisconnectFromTrails();
		ConnectToTrails();
	}

	protected void ConnectToTrails()
	{
		float range = GetRadius() * 1.5f + 5f;
		foreach (Trail item in Toolkit.EFindTrailsNear(base.transform.position, range))
		{
			if (!item.CanConnect() || !HasTrailInteraction(item))
			{
				continue;
			}
			foreach (ConnectableObject item2 in item.EFindNearbyConnectables())
			{
				if (item2 == this)
				{
					nearbyTrails.Add(item);
					item.AddNearbyConnectable(this);
					break;
				}
			}
		}
		UpdateNearbyActionPoints();
	}

	protected void DisconnectFromTrails()
	{
		foreach (Trail nearbyTrail in nearbyTrails)
		{
			nearbyTrail.RemoveFromNearbyConnectables(this);
			nearbyTrail.SetActionPoint(this, active: false);
		}
		nearbyTrails.Clear();
	}

	public void AddTrail(Trail trail)
	{
		nearbyTrails.Add(trail);
	}

	public void RemoveTrail(Trail trail)
	{
		nearbyTrails.Remove(trail);
	}

	public void UpdateNearbyActionPoints()
	{
		List<Trail> list = new List<Trail>();
		List<float> list2 = new List<float>();
		Vector3 position = base.transform.position;
		foreach (Trail nearbyTrail in nearbyTrails)
		{
			if (!nearbyTrail.deleted && (HasTrailInteraction(nearbyTrail) || nearbyTrail.trailType.IsBuildingTrail()))
			{
				float progressNear = nearbyTrail.GetProgressNear(position);
				float num = (nearbyTrail.GetPos(progressNear) - position).sqrMagnitude;
				if (progressNear == 0f)
				{
					num += 0.01f;
				}
				list.Add(nearbyTrail);
				list2.Add(num);
			}
		}
		while (list.Count > 0)
		{
			float num2 = float.MaxValue;
			int index = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list2[i] < num2)
				{
					num2 = list2[i];
					index = i;
				}
			}
			Trail trail = list[index];
			trail.SetActionPoint(this, active: true);
			list.RemoveAt(index);
			list2.RemoveAt(index);
			if (trail.trailType == TrailType.MINING)
			{
				continue;
			}
			foreach (Trail item in trail.ELinkedTrails(3))
			{
				if (!(item == trail))
				{
					int num3 = list.IndexOf(item);
					if (num3 >= 0)
					{
						item.SetActionPoint(this, active: false);
						list.RemoveAt(num3);
						list2.RemoveAt(num3);
					}
				}
			}
		}
	}

	protected override void DoDelete()
	{
		DisconnectFromTrails();
		base.DoDelete();
	}

	public static void ToUpdateAdd(ConnectableObject ob)
	{
		if (!toUpdateActionPoints.Contains(ob))
		{
			toUpdateActionPoints.Add(ob);
		}
	}

	public static void HandleActionPointUpdates()
	{
		foreach (ConnectableObject toUpdateActionPoint in toUpdateActionPoints)
		{
			if (toUpdateActionPoint != null)
			{
				toUpdateActionPoint.UpdateNearbyActionPoints();
			}
		}
		toUpdateActionPoints.Clear();
	}

	public virtual ConnectableObject GetObject()
	{
		return this;
	}

	public virtual ExchangePoint GetExchangePoint()
	{
		return null;
	}

	public float GetClosestProgress(Trail trail, out Vector3 best_pos_collider, bool force_end = false)
	{
		if (actionPointColliders == null)
		{
			List<Collider> list = new List<Collider>(GetComponentsInChildren<Collider>(includeInactive: false));
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (list[num].bounds.min.y > 3f)
				{
					list.RemoveAt(num);
				}
			}
			actionPointColliders = list.ToArray();
		}
		else
		{
			for (int i = 0; i < actionPointColliders.Length; i++)
			{
				Collider collider = actionPointColliders[i];
				if (!(collider == null) && !collider.transform.IsChildOf(base.transform))
				{
					actionPointColliders[i] = null;
				}
			}
		}
		float num2 = float.MaxValue;
		Vector3 posStart = trail.posStart;
		Vector3 posEnd = trail.posEnd;
		float num3 = 5.1f / trail.length;
		best_pos_collider = Vector3.zero;
		for (int j = 0; j < actionPointColliders.Length; j++)
		{
			Collider collider2 = actionPointColliders[j];
			if (collider2 == null)
			{
				continue;
			}
			float num4 = 0f;
			if (force_end)
			{
				num4 = 1.1f;
				num3 = 0f;
			}
			else if ((collider2.transform.position - posStart).sqrMagnitude > (collider2.transform.position - posEnd).sqrMagnitude)
			{
				num4 = 1f;
				num3 = 0f - Mathf.Abs(num3);
			}
			bool flag = false;
			float num5 = float.MaxValue;
			while (!flag)
			{
				if (num4 > 1f)
				{
					num4 = 1f;
					flag = true;
				}
				else if (num4 < 0f)
				{
					num4 = 0f;
					flag = true;
				}
				Vector3 pos = trail.GetPos(num4);
				Vector3 vector = actionPointColliders[j].ClosestPoint(pos).SetY(0f);
				float sqrMagnitude = (pos - vector).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					best_pos_collider = vector;
				}
				if (sqrMagnitude > num5)
				{
					flag = true;
				}
				num5 = sqrMagnitude;
				num4 += num3;
			}
		}
		if (best_pos_collider == Vector3.zero)
		{
			Debug.LogWarning("GetClosestProgress -> ?", base.gameObject);
			best_pos_collider = base.transform.position;
		}
		if (force_end)
		{
			return 1f;
		}
		return trail.GetProgressNear(best_pos_collider);
	}
}
