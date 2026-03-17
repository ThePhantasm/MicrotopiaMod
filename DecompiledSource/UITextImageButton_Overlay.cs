using System;
using TMPro;
using UnityEngine;

[Serializable]
public class UITextImageButton_Overlay
{
	public OverlayTypes type;

	public GameObject obTop;

	public RectTransform rtTop;

	public TextMeshProUGUI lbText;

	public void SetActive(bool target)
	{
		if (obTop != null)
		{
			obTop.SetActive(target);
		}
		if (rtTop != null)
		{
			rtTop.SetObActive(target);
		}
	}

	public void SetText(string txt)
	{
		if (lbText != null)
		{
			lbText.text = txt;
		}
	}
}
