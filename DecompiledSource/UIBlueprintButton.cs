using System;
using UnityEngine;

public class UIBlueprintButton : UITextImageButton
{
	[Header("Blueprint Button")]
	[SerializeField]
	private Color textColorEnabled;

	[SerializeField]
	private Color textColorDisabled;

	[NonSerialized]
	public Blueprint blueprint;

	[NonSerialized]
	public int barSortValue;

	[NonSerialized]
	public int creatorSortValue;

	public override void SetInteractable(bool target)
	{
		base.SetInteractable(target);
		SetTextColor(target ? textColorEnabled : textColorDisabled);
	}
}
