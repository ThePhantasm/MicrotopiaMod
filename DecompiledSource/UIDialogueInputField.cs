using System;
using TMPro;
using UnityEngine;

public class UIDialogueInputField : UIDialogBase
{
	[SerializeField]
	private TMP_InputField ifInput;

	private bool allowEmpty = true;

	public string GetInput()
	{
		return ifInput.text;
	}

	public void SetOnValueChanged(Action<string> on_value_changed)
	{
		ifInput.onValueChanged.AddListener(delegate(string v)
		{
			CheckEmpty();
			on_value_changed(v);
		});
	}

	public void SetValue(string str)
	{
		ifInput.text = str;
	}

	public void SetAllowEmpty(bool allow)
	{
		allowEmpty = allow;
		CheckEmpty();
		if (allow)
		{
			btOk.SetObActive(active: true);
		}
	}

	private void CheckEmpty()
	{
		if (!allowEmpty)
		{
			btOk.SetObActive(!IsEmpty());
		}
	}

	private bool IsEmpty()
	{
		return ifInput.text.Trim() == "";
	}

	protected override void MyUpdate()
	{
		base.MyUpdate();
		if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && (allowEmpty || !IsEmpty()))
		{
			OnDialogButtonClick(DialogResult.OK);
		}
		if (Input.GetKeyDown(KeyCode.Escape) && btCancel.gameObject.activeInHierarchy)
		{
			OnDialogButtonClick(DialogResult.CANCEL);
		}
	}

	public void SetFocus()
	{
		UIGlobal.eventSystem.SetSelectedGameObject(ifInput.gameObject);
		ifInput.selectionFocusPosition = 0;
	}
}
