using TMPro;
using UnityEngine;

public class UIBuildingButton : UITextImageButton
{
	[Header("UI Building Button")]
	[SerializeField]
	private GameObject obHotkey;

	[SerializeField]
	private TextMeshProUGUI lbHotkey;

	public void SetHotkey(string _key)
	{
		Toolkit.SetHotkeyButton(obHotkey, lbHotkey, _key);
	}
}
