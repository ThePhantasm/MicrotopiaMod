using System.Collections.Generic;
using UnityEngine;

public class ExchangePoint : PickupContainer
{
	[Header("Exchange Point")]
	[SerializeField]
	private List<ExchangePointMesh> exchangeMeshes = new List<ExchangePointMesh>();

	[Space(10f)]
	[SerializeField]
	private ExchangeType exchangeType;

	[SerializeField]
	private GeneralUnlocks unlockable;

	private PickupContainer owner;

	private void Awake()
	{
		actionPointToCenter = true;
	}

	public override void Write(Save save)
	{
		base.Write(save);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.version < 56)
		{
			save.ReadInt();
		}
	}

	public void Refresh()
	{
		if (unlockable == GeneralUnlocks.NONE)
		{
			return;
		}
		if (Progress.HasUnlocked(unlockable))
		{
			if (!base.gameObject.activeSelf)
			{
				base.gameObject.SetObActive(active: true);
				ConnectToTrails();
			}
		}
		else if (base.gameObject.activeSelf)
		{
			base.gameObject.SetObActive(active: false);
			DisconnectFromTrails();
		}
	}

	public override float GetRadius()
	{
		return 1f;
	}

	public void SetOwner(PickupContainer _owner)
	{
		owner = _owner;
	}

	public override ConnectableObject GetObject()
	{
		return owner;
	}

	public override ExchangePoint GetExchangePoint()
	{
		return this;
	}

	public void SetStatus(BuildingStatus s)
	{
		GameObject ob = null;
		foreach (ExchangePointMesh exchangeMesh in exchangeMeshes)
		{
			exchangeMesh.mesh.SetObActive(active: false);
			if (exchangeMesh.type == exchangeType)
			{
				ob = exchangeMesh.mesh;
			}
		}
		ob.SetObActive(active: true);
		switch (s)
		{
		case BuildingStatus.BUILDING:
			ConnectToTrails();
			break;
		case BuildingStatus.COMPLETED:
			foreach (Trail nearbyTrail in nearbyTrails)
			{
				foreach (Ant currentAnt in nearbyTrail.currentAnts)
				{
					currentAnt.RefreshAntActionPoints();
				}
			}
			break;
		}
		Refresh();
	}

	public override ExchangeType TrailInteraction(Trail _trail)
	{
		if (_trail.CanDoExchangeType(exchangeType))
		{
			return exchangeType;
		}
		return ExchangeType.NONE;
	}

	public override bool CanInsert(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchangeType == ExchangeType.BUILDING_IN || exchangeType == ExchangeType.BUILDING_PROCESS)
		{
			return owner.CanInsert(_type, exchange, this, ref let_ant_wait, show_billboard);
		}
		return false;
	}

	public override void PrepareForPickup(Pickup _pickup, ExchangePoint _point)
	{
		owner.PrepareForPickup(_pickup, this);
	}

	public override void OnPickupArrival(Pickup _pickup, ExchangePoint point)
	{
		owner.OnPickupArrival(_pickup, this);
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		return owner.ExtractPickup(_type);
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		return owner.CanExtract(exchange, ref let_ant_wait, show_billboard);
	}

	public override List<PickupType> GetExtractablePickups(ExchangeType exchange)
	{
		return owner.GetExtractablePickups(exchange);
	}

	public override Vector3 GetInsertPos(Pickup pickup = null)
	{
		return owner.GetInsertPos(pickup);
	}

	public override Vector3 GetExtractPos()
	{
		return owner.GetExtractPos();
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI(ui_hover);
		switch (exchangeType)
		{
		case ExchangeType.BUILDING_IN:
			ui_hover.SetTitle(Loc.GetUI("GENERIC_INSERT"));
			break;
		case ExchangeType.BUILDING_OUT:
			ui_hover.SetTitle(Loc.GetUI("GENERIC_EXTRACT"));
			break;
		case ExchangeType.BUILDING_PROCESS:
			ui_hover.SetTitle(Loc.GetUI("BUILDING_USEBUILDING"));
			break;
		}
		ui_hover.SetInfo();
		ui_hover.UpdateInfo(owner.ExchangeDescription(exchangeType));
	}

	public void ReconnectToTrails()
	{
		DisconnectFromTrails();
		ConnectToTrails();
	}
}
