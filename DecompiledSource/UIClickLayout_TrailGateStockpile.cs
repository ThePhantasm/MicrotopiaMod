using TMPro;
using UnityEngine;

public class UIClickLayout_TrailGateStockpile : UIClickLayout
{
	[SerializeField]
	private TextMeshProUGUI lbGateStockpile;

	[SerializeField]
	private TextMeshProUGUI lbToggleHigher;

	[SerializeField]
	private UIButton btToggleHigher;

	[SerializeField]
	private UISliderExtra sliderGateStockpile;

	private int hashPrev;

	public void SetGate(TrailGate_Stockpile gate_stockpile)
	{
		SetText(gate_stockpile, need_stockpile: true);
		if (gate_stockpile.stockpile != null)
		{
			sliderGateStockpile.SetObActive(active: true);
			btToggleHigher.SetObActive(active: true);
			sliderGateStockpile.Init(500, () => gate_stockpile.amount, delegate(int value)
			{
				gate_stockpile.amount = value;
			}, SliderCurve.Quadratic);
			btToggleHigher.Init(delegate
			{
				gate_stockpile.lowerThan = !gate_stockpile.lowerThan;
				SetText(gate_stockpile, need_stockpile: true);
			});
		}
		else
		{
			sliderGateStockpile.SetObActive(active: false);
			btToggleHigher.SetObActive(active: false);
		}
		hashPrev = GetHash(gate_stockpile);
	}

	public void SetGate_LogicControl(TrailGate_Stockpile gate_stockpile)
	{
		SetText(gate_stockpile, need_stockpile: false);
		sliderGateStockpile.SetObActive(active: true);
		btToggleHigher.SetObActive(active: true);
		sliderGateStockpile.Init(500, () => gate_stockpile.amount, delegate(int value)
		{
			gate_stockpile.amount = value;
		}, SliderCurve.Quadratic);
		btToggleHigher.Init(delegate
		{
			gate_stockpile.lowerThan = !gate_stockpile.lowerThan;
			SetText(gate_stockpile, need_stockpile: false);
		});
	}

	public void SetText(TrailGate_Stockpile gate_stockpile, bool need_stockpile)
	{
		if (gate_stockpile.stockpile == null && need_stockpile)
		{
			lbGateStockpile.text = Loc.GetUI("GATE_STOCKPILE_SHOULD_ASSIGN");
		}
		else
		{
			lbGateStockpile.text = Loc.GetUI(gate_stockpile.lowerThan ? "GATE_STOCKPILE_LOWER" : "GATE_STOCKPILE_HIGHER");
		}
		lbToggleHigher.text = (gate_stockpile.lowerThan ? "<" : ">");
	}

	private int GetHash(TrailGate_Stockpile gate_stockpile)
	{
		return ((!gate_stockpile.lowerThan) ? 1 : 0) + gate_stockpile.amount * 2 + ((!(gate_stockpile.stockpile == null)) ? (gate_stockpile.stockpile.GetInstanceID() % 10000) : 0);
	}

	public bool UpdateGate(TrailGate_Stockpile gate_stockpile)
	{
		int hash = GetHash(gate_stockpile);
		if (hash != hashPrev)
		{
			hashPrev = hash;
			SetText(gate_stockpile, need_stockpile: true);
			sliderGateStockpile.UpdateValue();
			return true;
		}
		return false;
	}
}
