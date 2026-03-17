using TMPro;
using UnityEngine;

public class UIFeedback : UIBaseSingleton
{
	public static UIFeedback instance;

	[SerializeField]
	private UITextImageButton btClose;

	[SerializeField]
	private UITextImageButton btSendFeedback;

	[SerializeField]
	private TMP_InputField ifFeedback;

	private bool firstTime = true;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init()
	{
		base.Show(target: true);
		btSendFeedback.SetText("Send feedback");
		btSendFeedback.SetInteractable(target: true);
		ifFeedback.interactable = true;
		if (firstTime)
		{
			btClose.SetButton(delegate
			{
				GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
			});
			btSendFeedback.SetButton(delegate
			{
				if (ifFeedback.text != "")
				{
					GoogleForms.Send(GoogleForm.MicrotopiaPlaytestText_nov24, ifFeedback.text);
					btSendFeedback.SetText("Sent");
					btSendFeedback.SetInteractable(target: false);
					ifFeedback.interactable = false;
				}
			});
			firstTime = false;
		}
		GameManager.instance.SetStatus(GameStatus.MENU);
	}

	public override void Show(bool target)
	{
		base.Show(target);
		ifFeedback.text = "";
	}
}
