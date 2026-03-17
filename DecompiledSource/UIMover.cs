using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIMover : MonoBehaviour
{
	[SerializeField]
	private Vector2 deltaPos;

	[SerializeField]
	private float duration = 1f;

	private Coroutine coroutine;

	private Vector2 startPos;

	private Vector2 startScreenSize;

	private void Start()
	{
		startPos = base.transform.position;
		startScreenSize = GetScreenSize();
	}

	public void DoMove(bool to)
	{
		if (coroutine != null)
		{
			StopCoroutine(coroutine);
		}
		if (to)
		{
			coroutine = StartCoroutine(CMove(GetStartPos() + ConvertPosition(deltaPos)));
		}
		else
		{
			coroutine = StartCoroutine(CMove(GetStartPos()));
		}
	}

	private IEnumerator CMove(Vector3 target)
	{
		Vector3 pos = base.transform.position;
		for (float t = 0f; t < duration; t += Time.deltaTime)
		{
			base.transform.position = pos + (target - pos) * (t / duration);
			yield return null;
		}
		base.transform.position = target;
	}

	public Vector2 GetStartPos()
	{
		if (GetScreenSize() != startScreenSize)
		{
			return new Vector2(startPos.x * (GetScreenSize().x / startScreenSize.x), startPos.y * (GetScreenSize().y / startScreenSize.y));
		}
		return startPos;
	}

	public Vector2 ConvertPosition(Vector2 pos)
	{
		if (UIGlobal.instance.canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
		{
			Vector2 referenceResolution = UIGlobal.instance.canvasScaler.referenceResolution;
			if (GetScreenSize() != referenceResolution)
			{
				return new Vector2(pos.x * (GetScreenSize().x / referenceResolution.x), pos.y * (GetScreenSize().y / referenceResolution.y));
			}
		}
		return pos;
	}

	public Vector2 GetScreenSize()
	{
		return new Vector2(Screen.width, Screen.height);
	}
}
