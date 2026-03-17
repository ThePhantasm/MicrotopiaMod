using System;
using System.Collections.Generic;
using UnityEngine;

public class TrailGate_Stockpile : TrailGate
{
	[NonSerialized]
	public bool lowerThan = true;

	[NonSerialized]
	public int amount = 30;

	[NonSerialized]
	public Stockpile stockpile;

	private int stockpileId = -1;

	public const int maxAmount = 500;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_STOCKPILE;
	}

	public override void Init(bool during_load = false)
	{
		externalControl = true;
		base.Init(during_load);
	}

	protected override void GateUpdate()
	{
		base.GateUpdate();
		UpdateHologram();
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Stockpile trailGate_Stockpile = other as TrailGate_Stockpile;
		lowerThan = trailGate_Stockpile.lowerThan;
		amount = trailGate_Stockpile.amount;
		stockpile = trailGate_Stockpile.stockpile;
	}

	public override void CleanObjectLinks()
	{
		stockpile = null;
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(lowerThan);
		save.Write(amount);
		save.Write(stockpile);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		lowerThan = save.ReadBool();
		amount = save.ReadInt();
		BuildingLink buildingLink = save.ReadBuilding();
		if (buildingLink.postpone)
		{
			stockpileId = buildingLink.id;
		}
		else
		{
			stockpile = buildingLink.building as Stockpile;
		}
	}

	public override void LoadLinks()
	{
		if (stockpileId != -1)
		{
			stockpile = GameManager.instance.FindLink<Stockpile>(stockpileId);
		}
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		if (stockpile == null)
		{
			return false;
		}
		int collectedAmount = stockpile.GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false);
		bool flag = (lowerThan ? (collectedAmount < amount) : (collectedAmount > amount));
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
	}

	public override void UpdateVisual(float dt)
	{
		base.UpdateVisual(dt);
		UpdateAssignLines();
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		SetAssignLine(is_selected);
	}

	public override void SetAssignLine(bool show)
	{
		if (show && stockpile != null)
		{
			ShowAssignLine(stockpile, AssignType.GATE);
		}
		else
		{
			HideAssignLines();
		}
	}

	public override bool CanAssignTo(ClickableObject target, out string error)
	{
		if (target is Stockpile)
		{
			error = "";
			return true;
		}
		return base.CanAssignTo(target, out error);
	}

	public override void Assign(ClickableObject target, bool add = true)
	{
		if (!(target is Stockpile))
		{
			Debug.LogError("Wrong assignment");
			return;
		}
		if (add)
		{
			stockpile = (Stockpile)target;
		}
		else
		{
			stockpile = null;
		}
		UpdateBillboard();
	}

	public override IEnumerable<ClickableObject> EAssignedObjects()
	{
		if (stockpile != null)
		{
			yield return stockpile;
		}
	}

	public override AfterAssignAction ActionAfterAssign()
	{
		return AfterAssignAction.SELECT_SELF;
	}

	public override AssignType GetAssignType()
	{
		return AssignType.GATE;
	}

	public override void OnConfigPaste()
	{
		SetAssignLine(show: true);
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_STOCKPILE;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateStockpile obj = (UIClickLayout_TrailGateStockpile)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATESTOCKPILE"));
		obj.SetGate(this);
		obj.UpdateButton(UIClickButtonType.Assign, enabled: true, Loc.GetUI("GATE_STOCKPILE_ASSIGN"));
		obj.SetButton(UIClickButtonType.Assign, delegate
		{
			Gameplay.instance.StartAssign(this, AssignType.GATE);
		}, InputAction.InteractBuilding);
	}

	public override void SetClickUi_LogicControl(UIClickLayout ui_click)
	{
		UIClickLayout_TrailGateStockpile obj = (UIClickLayout_TrailGateStockpile)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATESTOCKPILE"));
		obj.SetGate_LogicControl(this);
		obj.UpdateButton(UIClickButtonType.Assign, enabled: false);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		if (((UIClickLayout_TrailGateStockpile)ui_click).UpdateGate(this) && Gameplay.instance.IsSelected(this))
		{
			SetAssignLine(show: true);
		}
	}

	protected override bool HasHologram()
	{
		return true;
	}

	public override HologramShape GetHologramShape(out PickupType _pickup, out AntCaste _ant)
	{
		_pickup = PickupType.NONE;
		_ant = AntCaste.NONE;
		if (GetCurrentBillboard(out var _, out var _, out var _, out var _) != BillboardType.NONE)
		{
			return HologramShape.None;
		}
		if (stockpile == null)
		{
			return HologramShape.QuestionMark;
		}
		return stockpile.GetHologramShape(out _pickup, out _ant);
	}
}
