using TMPro;
using UnityEngine;

public class UIClickLayout_TrailGateOld : UIClickLayout
{
	[SerializeField]
	private TextMeshProUGUI lbGate;

	[SerializeField]
	private UIButton btToggleGateNot;

	private bool notPrev;

	public void SetGateOld(TrailGate_Old gate_old)
	{
		notPrev = gate_old.not;
		ShowNot(gate_old.not);
		btToggleGateNot.Init(delegate
		{
			gate_old.not = !gate_old.not;
			ShowNot(gate_old.not);
		});
	}

	public void UpdateGateOld(TrailGate_Old gate_old)
	{
		if (gate_old.not != notPrev)
		{
			notPrev = gate_old.not;
			ShowNot(gate_old.not);
		}
	}

	private void ShowNot(bool not)
	{
		lbGate.text = (not ? Loc.GetUI("GATE_OLD_NOT") : Loc.GetUI("GATE_OLD"));
	}
}
