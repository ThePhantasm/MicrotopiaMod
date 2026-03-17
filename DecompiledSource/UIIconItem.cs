using System;
using System.Collections.Generic;
using UnityEngine;

public class UIIconItem : UITextImageButton
{
	public void Init(PickupType _pickup, List<OverlayTypes> overlays = null, Action on_click = null)
	{
		Init();
		SetButton(on_click);
		switch (_pickup)
		{
		case PickupType.NONE:
			SetText("?");
			SetImage(null);
			break;
		case PickupType.ANY:
			SetText(Loc.GetUI("BUILDING_ANY_MATERIAL"));
			SetImage(null);
			break;
		default:
			SetText("");
			SetImage(AssetLinks.standard.GetPickupThumbnail(_pickup));
			break;
		}
		if (overlays == null)
		{
			return;
		}
		foreach (OverlayTypes overlay in overlays)
		{
			AddOverlay(overlay);
		}
	}

	public void Init(AntCaste _ant, List<OverlayTypes> overlays = null, Action on_click = null)
	{
		Init();
		SetText("");
		SetButton(on_click);
		if (_ant != AntCaste.NONE)
		{
			SetImage(AssetLinks.standard.GetAntCasteThumbnail(_ant));
		}
		if (overlays == null)
		{
			return;
		}
		foreach (OverlayTypes overlay in overlays)
		{
			AddOverlay(overlay);
		}
	}

	public void Init(FactoryRecipeData data)
	{
		if (data.productAnts.Count > 0)
		{
			Init(data.productAnts[0].type);
		}
		else if (data.productPickups.Count > 0)
		{
			Init(data.productPickups[0].type);
		}
		else
		{
			Init(PickupType.NONE);
		}
	}

	public void Init(Sprite sprite)
	{
		Init();
		SetText("");
		SetButton(null);
		SetImage(sprite);
	}
}
