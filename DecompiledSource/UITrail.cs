using System.Collections.Generic;
using UnityEngine;

public class UITrail : UIBase
{
	public UITrailTypeButton buttonPrefab;

	public List<TrailTypeColor> types = new List<TrailTypeColor>();

	private List<UITrailTypeButton> buttons = new List<UITrailTypeButton>();

	public void Init()
	{
		foreach (TrailTypeColor ttc in types)
		{
			UITrailTypeButton uITrailTypeButton = Object.Instantiate(buttonPrefab, buttonPrefab.transform.parent);
			uITrailTypeButton.Init(ttc, delegate
			{
				Gameplay.instance.SetTrailType(ttc.type);
				SetButtonsSelected(ttc.type);
			});
			buttons.Add(uITrailTypeButton);
		}
		buttonPrefab.SetObActive(active: false);
	}

	public void SetButtonsSelected(TrailType _type)
	{
		foreach (UITrailTypeButton button in buttons)
		{
			button.SetSelected(button.type, button.type == _type);
		}
	}

	public void SetButtonActive(TrailType _type, bool target = true)
	{
		foreach (UITrailTypeButton button in buttons)
		{
			if (button.type == _type)
			{
				button.Show(target);
			}
		}
	}
}
