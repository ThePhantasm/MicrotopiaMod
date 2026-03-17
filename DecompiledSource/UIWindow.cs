using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWindow : UIBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	protected bool animated = true;

	private Image[] images;

	private Text[] texts;

	private Outline[] outlines;

	private float[] imagesA;

	private float[] textsA;

	private float[] outlinesA;

	protected bool Entered { get; private set; }

	protected override void MyAwake()
	{
		base.MyAwake();
		if (animated)
		{
			images = GetComponentsInChildren<Image>(includeInactive: true);
			imagesA = new float[images.Length];
			for (int i = 0; i < images.Length; i++)
			{
				imagesA[i] = images[i].color.a;
			}
			texts = GetComponentsInChildren<Text>(includeInactive: true);
			textsA = new float[texts.Length];
			for (int j = 0; j < texts.Length; j++)
			{
				textsA[j] = texts[j].color.a;
			}
			StartCoroutine(WindowGrow());
		}
	}

	public override void StartClose()
	{
		if (!(this == null))
		{
			if (animated)
			{
				StartCoroutine(CWindowShrink());
			}
			else
			{
				Close();
			}
		}
	}

	public virtual IEnumerator WindowGrow()
	{
		float smallSize = 0.8f;
		base.transform.localScale = Vector3.one * smallSize;
		float duration = 0.25f;
		for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
		{
			base.transform.localScale = Vector3.one - Vector3.one * (1f - smallSize) * (duration - t);
			SetAlpha(t / duration, outline: false);
			yield return null;
		}
		SetAlpha(1f, outline: true);
	}

	public virtual IEnumerator CWindowShrink()
	{
		float smallSize = 0.5f;
		base.transform.localScale = Vector3.one;
		float duration = 0.15f;
		for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
		{
			base.transform.localScale = Vector3.one - Vector3.one * (1f - smallSize) * t;
			SetAlpha((duration - t) / duration, outline: false);
			yield return null;
		}
		Close();
	}

	private void SetAlpha(float a, bool outline)
	{
		if (images != null)
		{
			for (int i = 0; i < images.Length; i++)
			{
				Color color = images[i].color;
				color.a = imagesA[i] * a;
				images[i].color = color;
			}
		}
		if (texts != null)
		{
			for (int j = 0; j < texts.Length; j++)
			{
				Color color2 = texts[j].color;
				color2.a = textsA[j] * a;
				texts[j].color = color2;
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		Entered = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Entered = false;
	}
}
