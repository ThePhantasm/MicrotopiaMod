using System.Collections.Generic;
using UnityEngine;

public class ActionPointPin : ClickableObject
{
	[SerializeField]
	private Collider col;

	[SerializeField]
	private List<PinShape> listMeshes = new List<PinShape>();

	private ActionPoint actionPoint;

	public void Init(ActionPoint ap)
	{
		if (actionPoint != ap)
		{
			actionPoint = ap;
			SetMesh(actionPoint.exchangeType, actionPoint.activated);
		}
	}

	private void SetMesh(ExchangeType _exchange, bool _activated)
	{
		List<GameObject> list = new List<GameObject>();
		foreach (PinShape listMesh in listMeshes)
		{
			foreach (GameObject mesh in listMesh.meshes)
			{
				mesh.SetObActive(active: false);
				if (listMesh.exchange == _exchange && _activated)
				{
					list.Add(mesh);
				}
			}
			foreach (GameObject item in listMesh.meshesDisabled)
			{
				item.SetObActive(active: false);
				if (listMesh.exchange == _exchange && !_activated)
				{
					list.Add(item);
				}
			}
		}
		foreach (GameObject item2 in list)
		{
			item2.SetObActive(active: true);
		}
	}

	public void SetClickable(bool target)
	{
		col.enabled = target;
	}

	public override bool IsClickable()
	{
		return false;
	}

	public void ToggleActivated()
	{
		actionPoint.activated = !actionPoint.activated;
		SetMesh(actionPoint.exchangeType, actionPoint.activated);
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		string title = "";
		switch (actionPoint.exchangeType)
		{
		case ExchangeType.PICKUP:
		case ExchangeType.PICKUP_CORPSE:
			if (actionPoint.connectableObject is Pickup)
			{
				title = "Pick up from ground";
			}
			else if (actionPoint.connectableObject is PickupContainer)
			{
				title = "Pick up from pile";
			}
			break;
		case ExchangeType.BUILDING_IN:
			title = "Put in building";
			break;
		case ExchangeType.BUILDING_OUT:
			title = "Take from building";
			break;
		case ExchangeType.FORAGE:
			title = "Forage from plant";
			break;
		case ExchangeType.MINE:
			title = "Mine from deposit";
			break;
		case ExchangeType.PLANT_CUT:
			title = "Cut down plant";
			break;
		}
		ui_hover.SetTitle(title);
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
	}
}
