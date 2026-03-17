using TMPro;
using UnityEngine;

public class UIClickLayout_TrailGateSpeed : UIClickLayout
{
	[SerializeField]
	private TextMeshProUGUI lbGateSpeed;

	[SerializeField]
	private TextMeshProUGUI lbGateSpeedToggleHigher;

	[SerializeField]
	private UIButton btToggleGateSpeedMode;

	[SerializeField]
	private UIButton btToggleGateSpeedHigher;

	[SerializeField]
	private UISliderExtra sliderGateSpeed;

	private int hashPrev;

	public void SetGate(TrailGate_Speed gate_speed)
	{
		sliderGateSpeed.Init(Mathf.RoundToInt(50f), () => Mathf.RoundToInt(gate_speed.speedValue), delegate(int value)
		{
			gate_speed.speedValue = value;
		});
		SetText(gate_speed);
		btToggleGateSpeedMode.Init(delegate
		{
			gate_speed.baseSpeedOnly = !gate_speed.baseSpeedOnly;
			SetText(gate_speed);
		});
		btToggleGateSpeedHigher.Init(delegate
		{
			gate_speed.lowerThan = !gate_speed.lowerThan;
			SetText(gate_speed);
		});
		hashPrev = GetHash(gate_speed);
	}

	private void SetText(TrailGate_Speed gate_speed)
	{
		lbGateSpeed.text = Loc.GetUI((!gate_speed.baseSpeedOnly) ? (gate_speed.lowerThan ? "GATE_SPEED_CURRENT_LOWER" : "GATE_SPEED_CURRENT_HIGHER") : (gate_speed.lowerThan ? "GATE_SPEED_BASE_LOWER" : "GATE_SPEED_BASE_HIGHER"));
		lbGateSpeedToggleHigher.text = (gate_speed.lowerThan ? "<" : ">");
	}

	private int GetHash(TrailGate_Speed gate_speed)
	{
		return ((!gate_speed.baseSpeedOnly) ? 1 : 0) + (gate_speed.lowerThan ? 2 : 4) + Mathf.RoundToInt(gate_speed.speedValue * 100f);
	}

	public void UpdateGate(TrailGate_Speed gate_speed)
	{
		int hash = GetHash(gate_speed);
		if (hash != hashPrev)
		{
			hashPrev = hash;
			sliderGateSpeed.UpdateValue();
			SetText(gate_speed);
		}
	}
}
