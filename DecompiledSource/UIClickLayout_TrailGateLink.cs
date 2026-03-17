using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIClickLayout_TrailGateLink : UIClickLayout
{
	[Header("Link Gate")]
	[SerializeField]
	private UISliderExtra sliderCrewSize;

	[SerializeField]
	private RectTransform rtLinkedAnts;

	[SerializeField]
	private TextMeshProUGUI lbLinkedCount;

	public void SetLink(TrailGate_Link gate_link, bool show_panel)
	{
		sliderCrewSize.Init(50, () => gate_link.crewSize, delegate(int value)
		{
			gate_link.crewSize = value;
		});
		rtLinkedAnts.SetObActive(show_panel);
	}

	public void UpdateLink(List<Ant> linked_ants)
	{
		lbLinkedCount.text = Loc.GetUI("GATE_LINKED_ANTS", linked_ants.Count.ToString());
		sliderCrewSize.UpdateValue();
	}
}
