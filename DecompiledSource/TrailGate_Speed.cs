using System;

public class TrailGate_Speed : TrailGate
{
	[NonSerialized]
	public bool lowerThan;

	[NonSerialized]
	public bool baseSpeedOnly;

	[NonSerialized]
	public float speedValue = 30f;

	public const float maxSpeedValue = 50f;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_SPEED;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Speed trailGate_Speed = other as TrailGate_Speed;
		lowerThan = trailGate_Speed.lowerThan;
		baseSpeedOnly = trailGate_Speed.baseSpeedOnly;
		speedValue = trailGate_Speed.speedValue;
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(lowerThan);
		save.Write(baseSpeedOnly);
		save.Write(speedValue);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		lowerThan = save.ReadBool();
		baseSpeedOnly = save.ReadBool();
		speedValue = save.ReadFloat();
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		if (_ant == null)
		{
			return true;
		}
		float num = _ant.GetSpeed();
		if (!baseSpeedOnly)
		{
			num *= _ant.GetSpeedFactor();
		}
		bool flag = num >= speedValue;
		if (lowerThan)
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
		return UIClickType.TRAILGATE_SPEED;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateSpeed obj = (UIClickLayout_TrailGateSpeed)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATESPEED"));
		obj.SetGate(this);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		((UIClickLayout_TrailGateSpeed)ui_click).UpdateGate(this);
	}
}
