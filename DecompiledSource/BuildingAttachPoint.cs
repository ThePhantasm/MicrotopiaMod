using System;
using UnityEngine;

[Serializable]
public class BuildingAttachPoint
{
	[SerializeField]
	private Transform point;

	public AttachType type;

	private Building attachedBuilding;

	public bool CanHaveAttached(Building target)
	{
		if (attachedBuilding == null && type == AttachType.DISPENSER)
		{
			return target is Dispenser;
		}
		return false;
	}

	public void SetAttachment(Building _build)
	{
		attachedBuilding = _build;
	}

	public bool IsAttachment(Building _build)
	{
		return attachedBuilding == _build;
	}

	public bool HasAttachment(out Building att)
	{
		if (attachedBuilding != null)
		{
			att = attachedBuilding;
			return true;
		}
		att = null;
		return false;
	}

	public bool HasDispenser(out Dispenser dis)
	{
		if (type != AttachType.DISPENSER || attachedBuilding == null)
		{
			dis = null;
			return false;
		}
		dis = (Dispenser)attachedBuilding;
		return true;
	}

	public Vector3 GetPosition()
	{
		return point.position;
	}

	public Quaternion GetRotation()
	{
		return point.rotation;
	}
}
