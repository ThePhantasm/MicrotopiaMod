using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGraph : MonoBehaviour
{
	private struct Line
	{
		public List<float> values;

		public Color lineColor;

		public float lineWidth;
	}

	[SerializeField]
	private RawImage destinationImage;

	[SerializeField]
	private Material matLine;

	[SerializeField]
	private Camera renderCam;

	[SerializeField]
	private float textureSizeFactor = 1f;

	[SerializeField]
	private TMP_Text lbYMin;

	[SerializeField]
	private TMP_Text lbYMax;

	private bool inited;

	private RenderTexture renderTexture;

	private List<GameObject> lineObs = new List<GameObject>();

	private List<Line> lines = new List<Line>();

	private int width;

	private int height;

	private float normalHeight;

	private void Init()
	{
		RectTransform rectTransform = destinationImage.rectTransform;
		width = Mathf.CeilToInt(rectTransform.rect.width * textureSizeFactor);
		height = Mathf.CeilToInt(rectTransform.rect.height * textureSizeFactor);
		renderTexture = new RenderTexture(width, height, 16);
		renderTexture.Create();
		renderCam.backgroundColor = destinationImage.color;
		renderCam.targetTexture = renderTexture;
		destinationImage.color = Color.white;
		normalHeight = GetComponent<RectTransform>().sizeDelta.y;
		inited = true;
	}

	public void Prepare()
	{
		if (!inited)
		{
			Init();
		}
		ClearLines();
	}

	public void SetSmall(bool small)
	{
		GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, small ? 64f : normalHeight);
		TMP_Text tMP_Text = lbYMin;
		bool flag = (lbYMax.enabled = !small);
		tMP_Text.enabled = flag;
	}

	private void ClearLines()
	{
		foreach (GameObject lineOb in lineObs)
		{
			if (lineOb != null)
			{
				Object.Destroy(lineOb);
			}
		}
		lineObs.Clear();
		lines.Clear();
	}

	public void AddLine(List<float> _values, Color _line_color, float _line_width = 1f)
	{
		lines.Add(new Line
		{
			values = _values,
			lineColor = _line_color,
			lineWidth = _line_width
		});
	}

	public void Draw()
	{
		StartCoroutine(CDraw());
	}

	public IEnumerator CDraw()
	{
		float num = 0.001f;
		foreach (Line line in lines)
		{
			foreach (float value in line.values)
			{
				if (value > num)
				{
					num = value;
				}
			}
		}
		float num2 = 4.95f;
		float num3 = num2 * ((float)width / (float)height);
		foreach (Line line2 in lines)
		{
			GameObject gameObject = new GameObject("Line");
			gameObject.transform.SetParent(base.gameObject.transform);
			gameObject.transform.localPosition = Vector3.zero;
			LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.sharedMaterial = matLine;
			int numCornerVertices = 4;
			lineRenderer.numCapVertices = 4;
			lineRenderer.numCornerVertices = numCornerVertices;
			gameObject.layer = 20;
			int count = line2.values.Count;
			Vector3[] array = null;
			for (int i = 0; i < 2; i++)
			{
				float num4 = float.MinValue;
				int num5 = 0;
				for (int j = 0; j < count; j++)
				{
					float v = (-1f + (float)j / (float)(count - 1) * 2f) * num3;
					v = v.RoundAt(0.2f);
					if (v != num4)
					{
						num4 = v;
						if (i == 1)
						{
							float y = (line2.values[j] / num * 2f - 1f) * num2;
							array[num5] = new Vector3(v, y, 0f);
						}
						num5++;
					}
				}
				if (i == 0)
				{
					array = new Vector3[num5];
				}
			}
			lineRenderer.SetColor(line2.lineColor);
			lineRenderer.widthMultiplier = line2.lineWidth * 0.1f;
			lineRenderer.positionCount = array.Length;
			lineRenderer.SetPositions(array);
			lineRenderer.useWorldSpace = false;
			lineObs.Add(gameObject);
		}
		int num6 = Mathf.RoundToInt(num);
		if (num6 == 0)
		{
			lbYMax.enabled = false;
		}
		else
		{
			int num7 = num6;
			int num8 = 1;
			while (num7 >= 10)
			{
				num7 /= 10;
				num8 *= 10;
			}
			switch (num7)
			{
			case 3:
				num7 = 2;
				break;
			case 6:
				num7 = 5;
				break;
			case 7:
				num7 = 5;
				break;
			case 9:
				num7 = 8;
				break;
			}
			num6 = num7 * num8;
			lbYMax.text = num6.ToString();
			Vector2 anchoredPosition = lbYMax.rectTransform.anchoredPosition;
			anchoredPosition.y = (float)num6 / num * (normalHeight - 10f);
			lbYMax.rectTransform.anchoredPosition = anchoredPosition;
			lbYMax.enabled = true;
		}
		yield return null;
		renderCam.Render();
		destinationImage.texture = renderTexture;
	}

	private void OnDestroy()
	{
		ClearLines();
		if (renderTexture != null)
		{
			renderTexture.Release();
		}
	}
}
