using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITechTreeBoxShape : UITextImageButton
{
	[Header("Tech Tree")]
	[SerializeField]
	private List<Animator> anims;

	public float moveRadius = 50f;

	public void UpdateBox(TechStatus _status)
	{
		ResetOverlays();
		Color imageColor = Color.white;
		Color textColor = Color.white;
		switch (_status)
		{
		case TechStatus.NONE:
			imageColor = new Color(0f, 0f, 0f, 0.5f);
			textColor = new Color(1f, 1f, 1f, 0.5f);
			AddOverlay(OverlayTypes.CLOSED);
			break;
		case TechStatus.OPEN:
			AddOverlay(OverlayTypes.OPEN);
			break;
		case TechStatus.DONE:
			AddOverlay(OverlayTypes.COMPLETED);
			break;
		}
		SetImageColor(imageColor);
		SetTextColor(textColor);
	}

	private void SetButtonColor(Color col)
	{
		ColorBlock colors = btButton.button.colors;
		colors.disabledColor = col;
		btButton.button.colors = colors;
	}

	public void Complete()
	{
		foreach (Animator anim in anims)
		{
			anim.SetBool("Complete", value: true);
		}
	}

	public void StartCompleted()
	{
		foreach (Animator anim in anims)
		{
			anim.SetBool("Start Completed", value: true);
		}
	}

	public void ResetPosition()
	{
		rtBase.anchoredPosition = Vector2.zero;
		foreach (UITextImageButton_Overlay overlay in overlays)
		{
			overlay.rtTop.anchoredPosition = Vector2.zero;
		}
	}
}
