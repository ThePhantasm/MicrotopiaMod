using System;

public class TrailGate_Life : TrailGate
{
	[NonSerialized]
	public bool not = true;

	[NonSerialized]
	public float lifeValue = 30f;

	public const float maxLifeValue = 600f;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_LIFE;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Life trailGate_Life = other as TrailGate_Life;
		not = trailGate_Life.not;
		lifeValue = trailGate_Life.lifeValue;
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(not);
		save.Write(lifeValue);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		not = save.ReadBool();
		lifeValue = save.ReadFloat();
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		if (_ant == null)
		{
			return true;
		}
		bool flag = (not ? (!_ant.IsImmortal() && _ant.energy < lifeValue) : (_ant.IsImmortal() || _ant.energy > lifeValue));
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_LIFE;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateLife obj = (UIClickLayout_TrailGateLife)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATELIFE"));
		obj.SetGate(this);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		((UIClickLayout_TrailGateLife)ui_click).UpdateLife(this);
	}
}
