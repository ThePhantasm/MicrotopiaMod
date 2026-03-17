using System;
using System.Collections.Generic;

public class TrailGate_Caste : TrailGate
{
	[NonSerialized]
	public bool not;

	private List<AntCaste> antCastes = new List<AntCaste> { AntCaste.SENTRY };

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_CASTE;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Caste trailGate_Caste = other as TrailGate_Caste;
		not = trailGate_Caste.not;
		antCastes.Clear();
		foreach (AntCaste antCaste in trailGate_Caste.antCastes)
		{
			antCastes.Add(antCaste);
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(not);
		save.Write(antCastes.Count);
		foreach (AntCaste antCaste in antCastes)
		{
			save.Write((int)antCaste);
		}
	}

	public override void ReadConfig(ISaveContainer save)
	{
		not = save.ReadBool();
		int num = save.ReadInt();
		antCastes = new List<AntCaste>();
		for (int i = 0; i < num; i++)
		{
			antCastes.Add((AntCaste)save.ReadInt());
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
		bool flag = (not ? (!antCastes.Contains(_ant.caste)) : antCastes.Contains(_ant.caste));
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
		if (antCastes.Count == 0)
		{
			return HologramShape.QuestionMark;
		}
		if (antCastes[0] != AntCaste.NONE)
		{
			_ant = antCastes[0];
			return HologramShape.Ant;
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
		obj.SetTitle(Loc.GetObject("TRAIL_GATECASTE"));
		obj.SetGateCaste(this, antCastes, delegate
		{
		});
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		base.UpdateClickUi(ui_click);
		UIClickLayout_TrailGateInventory obj = (UIClickLayout_TrailGateInventory)ui_click;
		obj.UpdateGate(antCastes);
		obj.ShowNot(TrailType.GATE_CASTE, not);
	}
}
