using UnityEngine;
using UnityEngine.UI;

public class UIInventoryItem : UITextImageButton
{
	[SerializeField]
	private Image imHighlight;

	public void SetHighlight(Color col)
	{
		imHighlight.color = col;
		imHighlight.enabled = true;
	}

	public void SetHighlight()
	{
		imHighlight.enabled = false;
	}
}
