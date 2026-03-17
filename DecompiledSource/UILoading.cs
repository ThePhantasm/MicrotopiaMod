using UnityEngine;

public class UILoading : UIBase
{
	[SerializeField]
	private UILoadingBar loadingBar;

	[SerializeField]
	private RectTransform rtLogo;

	[SerializeField]
	private RectTransform rtLogoChinese;

	public void Init(bool hide_logo = false)
	{
		loadingBar.SetBar(0f);
		bool flag = Player.language == Language.CHINESE_SIMPLIFIED;
		rtLogo.SetObActive(!hide_logo && !flag);
		rtLogoChinese.SetObActive(!hide_logo && flag);
	}

	public void SetProgress(float progress)
	{
		loadingBar.SetBar(progress);
	}
}
