using TMPro;
using UnityEngine;

public class UIClickLayout_BatteryBuilding : UIClickLayout_Building
{
	[Header("Battery Building")]
	[SerializeField]
	private TextMeshProUGUI lbEnergyName;

	[SerializeField]
	private TextMeshProUGUI lbEnergyAmount;

	[SerializeField]
	private UILoadingBar uiEnergyBar;

	public void SetEnergy(string name)
	{
		lbEnergyName.text = name;
	}

	public void UpdateEnergy(string amount, float val)
	{
		lbEnergyAmount.text = amount;
		uiEnergyBar.SetBar(val);
	}
}
