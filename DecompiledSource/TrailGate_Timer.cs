using System;
using UnityEngine;
using UnityEngine.UI;

public class TrailGate_Timer : TrailGate
{
	public Image radial;

	[NonSerialized]
	public float interval = 30f;

	[NonSerialized]
	public bool minutes;

	public const float maxInterval = 60f;

	private double lastPass;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_TIMER;
	}

	public override void Init(bool during_load = false)
	{
		externalControl = true;
		base.Init(during_load);
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Timer trailGate_Timer = other as TrailGate_Timer;
		interval = trailGate_Timer.interval;
		minutes = trailGate_Timer.minutes;
		if (copy_mode == GateCopyMode.Settings)
		{
			lastPass = 0.0;
		}
		else
		{
			lastPass = trailGate_Timer.lastPass;
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(interval);
		save.Write(minutes);
		save.Write((float)lastPass);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		interval = save.ReadFloat();
		minutes = save.ReadBool();
		lastPass = save.ReadFloat();
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		float num = interval;
		if (minutes)
		{
			num *= 60f;
		}
		bool flag = GameManager.instance.gameTime > lastPass + (double)num;
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
	}

	public override void EnterGate(Ant _ant)
	{
		base.EnterGate(_ant);
		lastPass = GameManager.instance.gameTime;
	}

	public override void UpdateVisual(float dt)
	{
		base.UpdateVisual(dt);
		float num = interval;
		if (minutes)
		{
			num *= 60f;
		}
		radial.fillAmount = Mathf.Clamp01((float)(GameManager.instance.gameTime - lastPass) / num);
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_TIMER;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateTimer obj = (UIClickLayout_TrailGateTimer)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATETIMER"));
		obj.SetGate(this);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		((UIClickLayout_TrailGateTimer)ui_click).UpdateGate(this);
	}
}
