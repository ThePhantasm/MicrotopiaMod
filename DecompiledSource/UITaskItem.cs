using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITaskItem : MonoBehaviour
{
	[SerializeField]
	private TMP_Text lbTask;

	[SerializeField]
	private TMP_Text lbSliderStatus;

	[SerializeField]
	private Image imSlider;

	[SerializeField]
	private Slider slSlider;

	[SerializeField]
	private Color colPending;

	[SerializeField]
	private Color colCompleted;

	private bool wasPending;

	public void SetText(string s)
	{
		lbTask.SetObActive(s != "");
		lbTask.text = s;
	}

	public void SetSlider(float part, float whole)
	{
		part = Mathf.Clamp(part, 0f, whole);
		lbSliderStatus.text = $"{Mathf.Round(part * 100f) / 100f} / {whole}";
		float num = part / whole;
		slSlider.value = num;
		if (num == 1f)
		{
			imSlider.color = colCompleted;
			if (wasPending)
			{
				AudioManager.PlayUI(UISfx.InstinctItemDone);
				wasPending = false;
			}
		}
		else
		{
			imSlider.color = colPending;
			wasPending = true;
		}
	}
}
