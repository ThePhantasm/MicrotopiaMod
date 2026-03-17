using System;
using System.Collections.Generic;

public class TrailGate_Carry : TrailGate
{
	[NonSerialized]
	public bool not;

	private List<PickupType> pickupTypes = new List<PickupType> { PickupType.ANY };

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_CARRY;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Carry trailGate_Carry = other as TrailGate_Carry;
		not = trailGate_Carry.not;
		pickupTypes.Clear();
		foreach (PickupType pickupType in trailGate_Carry.pickupTypes)
		{
			pickupTypes.Add(pickupType);
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(not);
		save.Write(pickupTypes.Count);
		foreach (PickupType pickupType in pickupTypes)
		{
			save.Write((int)pickupType);
		}
	}

	public override void ReadConfig(ISaveContainer save)
	{
		not = save.ReadBool();
		int num = save.ReadInt();
		pickupTypes = new List<PickupType>();
		for (int i = 0; i < num; i++)
		{
			pickupTypes.Add((PickupType)save.ReadInt());
		}
	}

	protected override void GateUpdate()
	{
		base.GateUpdate();
		UpdateHologram();
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		if (_ant == null)
		{
			return true;
		}
		bool flag = false;
		if (not)
		{
			flag = true;
			foreach (PickupType item in _ant.ECarryingPickupTypes())
			{
				if (pickupTypes.Contains(item) || pickupTypes.Contains(PickupType.ANY))
				{
					flag = false;
					break;
				}
			}
		}
		else
		{
			foreach (PickupType item2 in _ant.ECarryingPickupTypes())
			{
				if (pickupTypes.Contains(item2) || pickupTypes.Contains(PickupType.ANY))
				{
					flag = true;
				}
			}
		}
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
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
		if (pickupTypes.Count == 0 || pickupTypes[0] == PickupType.ANY)
		{
			return HologramShape.QuestionMark;
		}
		if (pickupTypes[0] != PickupType.NONE)
		{
			_pickup = pickupTypes[0];
			return HologramShape.Pickup;
		}
		return base.GetHologramShape(out _pickup, out _ant);
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_INVENTORY;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateInventory obj = (UIClickLayout_TrailGateInventory)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATECARRY"));
		obj.SetGateCarry(this, pickupTypes, delegate
		{
		});
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		base.UpdateClickUi(ui_click);
		UIClickLayout_TrailGateInventory obj = (UIClickLayout_TrailGateInventory)ui_click;
		obj.UpdateGate(pickupTypes);
		obj.ShowNot(TrailType.GATE_CARRY, not);
	}
}
