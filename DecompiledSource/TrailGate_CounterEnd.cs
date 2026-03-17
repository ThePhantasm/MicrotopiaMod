public class TrailGate_CounterEnd : TrailGate
{
	private bool unlinkAnts = true;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_COUNTER_END;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_CounterEnd trailGate_CounterEnd = other as TrailGate_CounterEnd;
		unlinkAnts = trailGate_CounterEnd.unlinkAnts;
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(unlinkAnts);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		if (save.GetVersion() >= 83)
		{
			unlinkAnts = save.ReadBool();
		}
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		return true;
	}

	public override void EnterGate(Ant _ant)
	{
		if (unlinkAnts)
		{
			GameManager.instance.RemoveAntFromLinkGates(_ant);
		}
		base.EnterGate(_ant);
	}

	public override void UpdateVisual(float dt)
	{
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_COUNTEREND;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateCounterEnd obj = (UIClickLayout_TrailGateCounterEnd)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_COUNTEREND"));
		obj.SetCheckbox(0, unlinkAnts, delegate(bool is_on)
		{
			unlinkAnts = is_on;
		});
		obj.UpdateCheckbox(0, enabled: true, Loc.GetUI("GATE_END_UNLINK"));
	}

	public override void SetClickUi_LogicControl(UIClickLayout ui_click)
	{
		UIClickLayout_TrailGateCounterEnd obj = (UIClickLayout_TrailGateCounterEnd)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_COUNTEREND"));
		obj.SetCheckbox(0, unlinkAnts, delegate(bool is_on)
		{
			unlinkAnts = is_on;
		});
		obj.UpdateCheckbox(0, enabled: true, Loc.GetUI("GATE_END_UNLINK"));
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		Checkbox checkbox = ((UIClickLayout_TrailGateCounterEnd)ui_click).GetCheckbox(0);
		if (checkbox.toggleBox != null)
		{
			checkbox.toggleBox.isOn = unlinkAnts;
		}
	}
}
