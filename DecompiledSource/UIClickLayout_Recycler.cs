using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIClickLayout_Recycler : UIClickLayout_Building
{
	[SerializeField]
	private TextMeshProUGUI lbProgress;

	[SerializeField]
	private RectTransform rtProgressBar;

	[SerializeField]
	private Slider slProgressBar;

	public void UpdateProgressBar(bool enabled, float progress, string txt)
	{
		rtProgressBar.SetObActive(enabled);
		slProgressBar.value = Mathf.Clamp01(progress);
		lbProgress.text = txt;
	}
}
