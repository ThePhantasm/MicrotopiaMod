using TMPro;
using UnityEngine;

public class UIClickLayout_TrailGateTimer : UIClickLayout
{
	[SerializeField]
	private TextMeshProUGUI lbGateTimer;

	[SerializeField]
	private UIButton btToggleGateTimerMode;

	[SerializeField]
	private UISliderExtra sliderGateSpeed;

	private int hashPrev;

	public void SetGate(TrailGate_Timer gate_timer)
	{
		sliderGateSpeed.Init(Mathf.RoundToInt(60f), () => Mathf.RoundToInt(gate_timer.interval), delegate(int value)
		{
			gate_timer.interval = value;
		});
		SetText(gate_timer);
		btToggleGateTimerMode.Init(delegate
		{
			gate_timer.minutes = !gate_timer.minutes;
			SetText(gate_timer);
		});
		hashPrev = GetHash(gate_timer);
	}

	public void SetText(TrailGate_Timer gate_timer)
	{
		lbGateTimer.text = Loc.GetUI(gate_timer.minutes ? "GATE_TIMER_MINUTES" : "GATE_TIMER_SECONDS");
	}

	private int GetHash(TrailGate_Timer gate_timer)
	{
		return ((!gate_timer.minutes) ? 1 : 0) + Mathf.RoundToInt(gate_timer.interval * 1000f);
	}

	public void UpdateGate(TrailGate_Timer gate_timer)
	{
		int hash = GetHash(gate_timer);
		if (hash != hashPrev)
		{
			hashPrev = hash;
			SetText(gate_timer);
			sliderGateSpeed.UpdateValue();
		}
	}
}
