using TMPro;
using UnityEngine;

public class UIClickLayout_TrailGateLife : UIClickLayout
{
	[SerializeField]
	private TextMeshProUGUI lbGateLife;

	[SerializeField]
	private TextMeshProUGUI lbGateLifeToggle;

	[SerializeField]
	private UIButton btToggleGateLife;

	[SerializeField]
	private UISliderExtra sliderGateLife;

	private int hashPrev;

	public void SetGate(TrailGate_Life gate_life)
	{
		sliderGateLife.Init(Mathf.RoundToInt(600f), () => Mathf.RoundToInt(gate_life.lifeValue), delegate(int value)
		{
			gate_life.lifeValue = value;
		}, SliderCurve.Quadratic);
		sliderGateLife.OverrideInputField((int value) => value.Unit(((float)value > 60f) ? PhysUnit.TIME_MINUTES : PhysUnit.TIME));
		ShowNot(gate_life.not);
		btToggleGateLife.Init(delegate
		{
			gate_life.not = !gate_life.not;
			ShowNot(gate_life.not);
		});
		hashPrev = GetHash(gate_life);
	}

	private int GetHash(TrailGate_Life gate_life)
	{
		return ((!gate_life.not) ? 1 : 2) + Mathf.RoundToInt(gate_life.lifeValue * 1000f);
	}

	public void UpdateLife(TrailGate_Life gate_life)
	{
		int hash = GetHash(gate_life);
		if (hash != hashPrev)
		{
			hashPrev = hash;
			sliderGateLife.UpdateValue();
			ShowNot(gate_life.not);
		}
	}

	private void ShowNot(bool not)
	{
		lbGateLife.text = Loc.GetUI(not ? "GATE_LIFE_LOWER" : "GATE_LIFE_HIGHER");
		lbGateLifeToggle.text = (not ? "<" : ">");
	}
}
