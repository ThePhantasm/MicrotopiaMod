using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITrailTypeButton : UIBase
{
	public Button btTrailType;

	public GameObject obSelected;

	public TextMeshProUGUI lbName;

	public TrailButtonGraphic[] buttonGraphics;

	[NonSerialized]
	public TrailType type;

	public void Init(TrailTypeColor ptc, Action onClick)
	{
		lbName.text = ptc.name;
		TrailButtonGraphic[] array = buttonGraphics;
		foreach (TrailButtonGraphic obj in array)
		{
			obj.imUnselected.color = AssetLinks.standard.GetTrailMaterial(ptc.type).color;
			obj.imSelected.color = AssetLinks.standard.GetTrailMaterial(ptc.type).color;
		}
		RegisterButton(btTrailType, onClick);
		type = ptc.type;
	}

	public void SetSelected(TrailType _type, bool _target)
	{
		TrailButtonGraphic trailButtonGraphic = null;
		bool flag = false;
		TrailButtonGraphic[] array = buttonGraphics;
		foreach (TrailButtonGraphic trailButtonGraphic2 in array)
		{
			if (trailButtonGraphic == null && trailButtonGraphic2.type == TrailType.NONE)
			{
				trailButtonGraphic = trailButtonGraphic2;
			}
			if (trailButtonGraphic2.type == _type)
			{
				flag = true;
				trailButtonGraphic2.imUnselected.SetObActive(!_target);
				trailButtonGraphic2.imSelected.SetObActive(_target);
				btTrailType.targetGraphic = (_target ? trailButtonGraphic2.imSelected : trailButtonGraphic2.imUnselected);
			}
			else
			{
				trailButtonGraphic2.imUnselected.SetObActive(active: false);
				trailButtonGraphic2.imSelected.SetObActive(active: false);
			}
		}
		if (!flag && trailButtonGraphic != null)
		{
			trailButtonGraphic.imUnselected.SetObActive(!_target);
			trailButtonGraphic.imSelected.SetObActive(_target);
			btTrailType.targetGraphic = (_target ? trailButtonGraphic.imSelected : trailButtonGraphic.imUnselected);
		}
	}
}
