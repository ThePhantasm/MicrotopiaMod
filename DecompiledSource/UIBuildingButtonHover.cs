using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIBuildingButtonHover : UIBase
{
	[Header("UIBuildingButtonHover")]
	[SerializeField]
	private RectTransform rtCost;

	[SerializeField]
	private RectTransform rtAntCount;

	[SerializeField]
	private RectTransform rtAlreadyBuilt;

	[SerializeField]
	private RectTransform rtInventory;

	[SerializeField]
	private RectTransform rtDescription;

	[SerializeField]
	private TextMeshProUGUI lbTitle;

	[SerializeField]
	private TextMeshProUGUI lbDescription;

	[SerializeField]
	private TextMeshProUGUI lbCost;

	[SerializeField]
	private TextMeshProUGUI lbAntCount;

	[SerializeField]
	private TextMeshProUGUI lbInventory;

	[SerializeField]
	private UIIconList uiIconList;

	public void SetHover(string _title, string _desc, string _cost = "", int ant_count = 0, bool built = false)
	{
		rtInventory.SetObActive(active: false);
		lbTitle.text = _title;
		rtDescription.SetObActive(_desc != "");
		lbDescription.text = _desc;
		if (ant_count == 0 && !built && _cost != "")
		{
			rtCost.SetObActive(active: true);
			lbCost.text = _cost;
		}
		else
		{
			rtCost.SetObActive(active: false);
		}
		if (ant_count > 0 && !built)
		{
			rtAntCount.SetObActive(active: true);
			lbAntCount.text = "NEED <b>" + ant_count + "</b> MORE ANTS TO \nUNLOCK THIS BUILDING";
		}
		else
		{
			rtAntCount.SetObActive(active: false);
		}
		if (built)
		{
			rtAlreadyBuilt.SetObActive(active: true);
		}
		else
		{
			rtAlreadyBuilt.SetObActive(active: false);
		}
	}

	public void SetInventory()
	{
		rtInventory.SetObActive(active: true);
	}

	public void UpdateInventory(string _title, Dictionary<PickupType, int> _pickups)
	{
		lbInventory.text = _title;
		uiIconList.SpawnList(_pickups, Loc.GetUI("GENERIC_FREE"));
	}
}
