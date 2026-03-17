using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingBar : MonoBehaviour
{
	[SerializeField]
	private RectTransform bar;

	[SerializeField]
	private RectTransform background;

	[NonSerialized]
	public float currentValue = 0.2f;

	[SerializeField]
	private bool useFill;

	[SerializeField]
	private Image fillImage;

	public void SetBar(float val)
	{
		val = Mathf.Clamp01(val);
		if (fillImage == null || !useFill)
		{
			bar.sizeDelta = new Vector2(background.rect.width * val, bar.sizeDelta.y);
		}
		else if (useFill && fillImage != null)
		{
			fillImage.fillAmount = val;
		}
		currentValue = val;
	}

	public void SetBar(float part, float whole)
	{
		if (whole == 0f)
		{
			SetBar(0f);
		}
		else
		{
			SetBar(part / whole);
		}
	}

	public void SetBar(float min, float max, float value)
	{
		SetBar(Mathf.InverseLerp(min, max, value));
	}

	public IEnumerator ProgressBar(float targetFraction, float duration)
	{
		float startFraction = currentValue;
		currentValue = targetFraction;
		for (float t = 0f; t < duration; t += Time.deltaTime)
		{
			float num = startFraction + (targetFraction - startFraction) * (t / duration);
			SetBar(num);
			yield return null;
		}
		SetBar(targetFraction);
	}
}
