using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDialogBase : UIWindow
{
	public delegate void DialogEvent(UIDialogBase dialogBase);

	[SerializeField]
	[Tooltip("Can be left empty; close = possible second button that behaves as btCancel")]
	protected UIButton btOk;

	[SerializeField]
	[Tooltip("Can be left empty; close = possible second button that behaves as btCancel")]
	protected UIButton btCancel;

	[SerializeField]
	[Tooltip("Can be left empty; close = possible second button that behaves as btCancel")]
	protected UIButton btYes;

	[SerializeField]
	[Tooltip("Can be left empty; close = possible second button that behaves as btCancel")]
	protected UIButton btNo;

	protected bool waitForSelection;

	protected Dictionary<DialogResult, Action> actions = new Dictionary<DialogResult, Action>();

	[NonSerialized]
	public DialogResult dialogResult;

	public static List<UIDialogBase> dialogsOpen = new List<UIDialogBase>();

	[SerializeField]
	protected TextMeshProUGUI lbText;

	public bool IsTopmostDialog
	{
		get
		{
			if (dialogsOpen.Contains(this))
			{
				return dialogsOpen[dialogsOpen.Count - 1] == this;
			}
			return false;
		}
	}

	public static event DialogEvent OnDialogOpened;

	public static event DialogEvent OnDialogClosed;

	protected override void MyAwake()
	{
		base.MyAwake();
		InitButton(btOk, DialogResult.OK);
		InitButton(btCancel, DialogResult.CANCEL);
		InitButton(btYes, DialogResult.YES);
		InitButton(btNo, DialogResult.NO);
		SetButtonVisible(DialogResult.OK, vis: false);
		SetButtonVisible(DialogResult.CANCEL, vis: false);
		SetButtonVisible(DialogResult.YES, vis: false);
		SetButtonVisible(DialogResult.NO, vis: false);
	}

	protected virtual void Start()
	{
	}

	private UIButton InitButton(UIButton bt, DialogResult res)
	{
		if (bt == null)
		{
			return null;
		}
		bt.Init(delegate
		{
			OnDialogButtonClick(res);
		});
		return bt;
	}

	protected override void OnSpawn()
	{
		base.OnSpawn();
		dialogsOpen.Add(this);
		UIDialogBase.OnDialogOpened?.Invoke(this);
	}

	public override void StartClose()
	{
		dialogsOpen.Remove(this);
		UIDialogBase.OnDialogClosed?.Invoke(this);
		base.StartClose();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	public IEnumerator CWaitForSelection(Action<DialogResult> callbackResult = null)
	{
		waitForSelection = true;
		while (dialogResult == DialogResult.NONE)
		{
			yield return null;
		}
		callbackResult?.Invoke(dialogResult);
		StartClose();
		yield return null;
	}

	public virtual void OnDialogButtonClick(DialogResult dialog_result)
	{
		dialogResult = dialog_result;
		if (actions.TryGetValue(dialog_result, out var value))
		{
			value?.Invoke();
		}
		if (!waitForSelection)
		{
			StartClose();
		}
	}

	public void SetButtonVisible(DialogResult button, bool vis)
	{
		UIButton button2 = GetButton(button);
		if (button2 != null)
		{
			button2.SetObActive(vis);
		}
	}

	public UIButton GetButton(DialogResult dialog_result)
	{
		return dialog_result switch
		{
			DialogResult.OK => btOk, 
			DialogResult.CANCEL => btCancel, 
			DialogResult.YES => btYes, 
			DialogResult.NO => btNo, 
			_ => null, 
		};
	}

	public void SetAction(DialogResult dialog_result, Action action)
	{
		actions[dialog_result] = action;
		SetButtonVisible(dialog_result, vis: true);
	}

	public void SetText(string txt)
	{
		lbText.text = txt;
	}

	protected void Update()
	{
		MyUpdate();
	}

	protected virtual void MyUpdate()
	{
		if (GameManager.CLOSE_ALL_DIALOGUE)
		{
			StartClose();
		}
	}
}
