using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[RequireComponent(typeof(RectTransform))]
public class LayoutMaxSize : LayoutElement
{
	public float maxHeight = -1f;

	public float maxWidth = -1f;

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();
		UpdateMaxSizes();
	}

	public override void CalculateLayoutInputVertical()
	{
		base.CalculateLayoutInputVertical();
		UpdateMaxSizes();
	}

	protected override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		UpdateMaxSizes();
	}

	private void OnValidate()
	{
		UpdateMaxSizes();
	}

	private void UpdateMaxSizes()
	{
		if (maxHeight != -1f)
		{
			if (preferredHeight == -1f && maxHeight < GetComponent<RectTransform>().sizeDelta.y)
			{
				preferredHeight = maxHeight;
			}
			else if (preferredHeight != -1f && base.transform.childCount > 0)
			{
				bool flag = true;
				float num = 0f;
				float num2 = 0f;
				for (int i = 0; i < base.transform.childCount; i++)
				{
					RectTransform component = base.transform.GetChild(i).GetComponent<RectTransform>();
					if (!(component == null))
					{
						Vector3 localPosition = component.localPosition;
						Vector2 sizeDelta = component.sizeDelta;
						Vector2 pivot = component.pivot;
						if (flag)
						{
							num = localPosition.y + sizeDelta.y * (1f - pivot.y);
							num2 = localPosition.y - sizeDelta.y * pivot.y;
						}
						else
						{
							num = Mathf.Max(num, localPosition.y + sizeDelta.y * (1f - pivot.y));
							num2 = Mathf.Min(num2, localPosition.y - sizeDelta.y * pivot.y);
						}
						flag = false;
					}
				}
				if (flag)
				{
					return;
				}
				float num3 = Mathf.Abs(num - num2);
				if (preferredHeight > num3)
				{
					preferredHeight = -1f;
				}
			}
		}
		if (maxWidth == -1f)
		{
			return;
		}
		if (preferredWidth == -1f && maxWidth < GetComponent<RectTransform>().sizeDelta.x)
		{
			preferredWidth = maxWidth;
		}
		else
		{
			if (preferredWidth == -1f || base.transform.childCount <= 0)
			{
				return;
			}
			bool flag2 = true;
			float num4 = 0f;
			float num5 = 0f;
			for (int j = 0; j < base.transform.childCount; j++)
			{
				RectTransform component2 = base.transform.GetChild(j).GetComponent<RectTransform>();
				if (!(component2 == null))
				{
					Vector3 localPosition2 = component2.localPosition;
					Vector2 sizeDelta2 = component2.sizeDelta;
					Vector2 pivot2 = component2.pivot;
					if (flag2)
					{
						num4 = localPosition2.x + sizeDelta2.x * (1f - pivot2.x);
						num5 = localPosition2.x - sizeDelta2.x * pivot2.x;
					}
					else
					{
						num4 = Mathf.Max(num4, localPosition2.x + sizeDelta2.x * (1f - pivot2.x));
						num5 = Mathf.Min(num5, localPosition2.x - sizeDelta2.x * pivot2.x);
					}
					flag2 = false;
				}
			}
			if (!flag2)
			{
				float num6 = Mathf.Abs(num4 - num5);
				if (preferredWidth > num6)
				{
					preferredWidth = -1f;
				}
			}
		}
	}
}
