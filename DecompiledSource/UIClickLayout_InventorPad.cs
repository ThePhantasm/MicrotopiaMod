using TMPro;
using UnityEngine;

public class UIClickLayout_InventorPad : UIClickLayout_Building
{
	[Header("Inventor Pad")]
	[SerializeField]
	private TextMeshProUGUI lbEnergyName;

	[SerializeField]
	private TextMeshProUGUI lbEnergyAmount;

	[SerializeField]
	private TextMeshProUGUI lbHealthName;

	[SerializeField]
	private TextMeshProUGUI lbHealthAmount;

	[SerializeField]
	private TextMeshProUGUI lbHealthStatus;

	[SerializeField]
	private UILoadingBar uiEnergyBar;

	[SerializeField]
	private UILoadingBar uiHealthBar;

	[SerializeField]
	private UILoadingBar uiRadDeathBar;

	public void SetEnergy(string name)
	{
		lbEnergyName.text = name;
	}

	public void UpdateEnergy(string amount, float val)
	{
		lbEnergyAmount.text = amount;
		uiEnergyBar.SetBar(val);
	}

	public void SetHealth(string name)
	{
		lbHealthName.text = name;
		uiRadDeathBar.SetObActive(active: false);
	}

	public void UpdateHealth(string amount, float val)
	{
		lbHealthAmount.text = amount;
		uiHealthBar.SetBar(val);
	}

	public void UpdateRadDeath(float val)
	{
		uiRadDeathBar.SetObActive(active: true);
		uiRadDeathBar.SetBar(val);
	}

	public void UpdateStatusEffects(string status_text)
	{
		lbHealthStatus.text = status_text;
	}
}
