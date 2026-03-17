using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITechTreeLine : UIBase
{
	public int prefabId;

	public RectTransform rtLine;

	public Image imLine;

	public Image imLineBG;

	public float startDelay = 0.6f;

	public Vector2 speedRange = new Vector2(0.8f, 1.2f);

	private float currentProgress;

	private float duration;

	private static Dictionary<int, List<Material>> dicMats = new Dictionary<int, List<Material>>();

	public void UpdateLine(Vector2 start, Vector2 end)
	{
		rtBase.anchoredPosition = start;
		rtBase.rotation = Quaternion.LookRotation(Toolkit.LookVector(start, end), -Vector3.forward);
		rtLine.sizeDelta = new Vector2(rtLine.sizeDelta.x, Vector2.Distance(start, end));
		imLine.rectTransform.sizeDelta = new Vector2(imLine.rectTransform.sizeDelta.x, GetLength() * Mathf.Clamp01(currentProgress));
		imLineBG.rectTransform.sizeDelta = new Vector2(imLineBG.rectTransform.sizeDelta.x, GetLength());
	}

	public void InitMaterial()
	{
		if (!dicMats.ContainsKey(prefabId))
		{
			List<Material> list = new List<Material>();
			for (int i = 0; i < 4; i++)
			{
				Material material = new Material(imLine.material);
				material.SetFloat("_Offset", UnityEngine.Random.Range(0f, 100f));
				list.Add(material);
			}
			dicMats.Add(prefabId, list);
		}
		List<Material> list2 = dicMats[prefabId];
		imLine.material = list2[UnityEngine.Random.Range(0, list2.Count)];
	}

	public float GetLength()
	{
		return rtLine.sizeDelta.y;
	}

	public void StartLine(float _length, Action on_complete)
	{
		if (currentProgress == 0f)
		{
			duration = _length / 1200f * UnityEngine.Random.Range(speedRange.x, speedRange.y);
			StartCoroutine(CLineAnimation(on_complete));
		}
	}

	public void SetLineInstant(float f)
	{
		currentProgress = f;
	}

	private IEnumerator CLineAnimation(Action on_complete)
	{
		yield return new WaitForSeconds(startDelay);
		for (float t = 0f; t < duration; t += Time.deltaTime)
		{
			currentProgress = t / duration;
			yield return null;
		}
		currentProgress = 1f;
		on_complete?.Invoke();
	}
}
