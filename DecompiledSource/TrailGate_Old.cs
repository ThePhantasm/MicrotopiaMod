using System;

public class TrailGate_Old : TrailGate
{
	[NonSerialized]
	public bool not;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_OLD;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Old trailGate_Old = other as TrailGate_Old;
		not = trailGate_Old.not;
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(not);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		not = save.ReadBool();
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		if (_ant == null)
		{
			return true;
		}
		bool flag = _ant.HasStatusEffect(StatusEffect.OLD);
		if (not)
		{
			flag = !flag;
		}
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_OLD;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateOld obj = (UIClickLayout_TrailGateOld)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATEOLD"));
		obj.SetGateOld(this);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		((UIClickLayout_TrailGateOld)ui_click).UpdateGateOld(this);
	}
}
