using TMPro;
using UnityEngine;

public class UIDialogNewBuilding : UIDialogBase
{
	[Header("New Unlock")]
	[SerializeField]
	private TextMeshProUGUI lbTitle;

	[SerializeField]
	private TextMeshProUGUI lbDescription;

	[SerializeField]
	private UIIconItem uiIcon;

	public void SetDialogUnlock(string _title, string _desc, Sprite _image)
	{
		lbTitle.text = _title;
		lbDescription.text = _desc;
		uiIcon.SetImage(_image);
		SetAction(DialogResult.OK, StartClose);
	}
}
