using DTT.Utils.Extensions;
using UnityEngine;

public class DragZoomRect : MonoBehaviour
{
	public RectTransform rtViewport;

	public RectTransform rtLimits;

	public RectTransform rtContent;

	public Transform tfBackgroundCam;

	public float zoomSpeed = 0.1f;

	public float panSpeed = 500f;

	public float dragSpeed = 80f;

	public float maxZoom = 3f;

	public float backSpeedFactor = -1000f;

	public float backZoomFactor = -100f;

	private bool firstCheck;

	private float zoom;

	private Vector2 mousePosPrev;

	private RectTransform rt;

	private Vector2 basePosViewPort;

	private Vector3 basePosBackCam;

	public void Init(bool first_time)
	{
		if (first_time)
		{
			zoom = 1f;
			RectTransform rectTransform = rtViewport;
			RectTransform rectTransform2 = rtViewport;
			Vector2 vector = (rtViewport.anchorMax = new Vector2(0.5f, 0f));
			Vector2 pivot = (rectTransform2.anchorMin = vector);
			rectTransform.pivot = pivot;
			rtViewport.anchoredPosition = new Vector2(0f, 0f);
			mousePosPrev = Input.mousePosition;
			rt = GetComponent<RectTransform>();
			firstCheck = true;
			basePosBackCam = tfBackgroundCam.localPosition;
			basePosViewPort = rtViewport.localPosition.XY();
		}
		UpdateLimitsZoom(zoom);
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		Rect worldRect = rt.GetWorldRect();
		float zoomDelta = InputManager.zoomDelta;
		bool flag = zoomDelta != 0f || firstCheck;
		if (flag)
		{
			float num = Mathf.Pow(2f, zoomDelta * zoomSpeed);
			zoom *= num;
			if (zoom > maxZoom)
			{
				num *= maxZoom / zoom;
				zoom = maxZoom;
			}
			UpdateLimitsZoom(zoom);
			Rect worldRect2 = rtLimits.GetWorldRect();
			float b = worldRect.size.x / worldRect2.size.x;
			float num2 = Mathf.Max(worldRect.size.y / worldRect2.size.y, b);
			if (num2 > 1f)
			{
				zoom *= num2;
				num *= num2;
				UpdateLimitsZoom(zoom);
			}
			rtLimits.localPosition *= num;
			rtViewport.localScale = new Vector3(zoom, zoom, 1f);
			rtViewport.localPosition *= num;
			firstCheck = false;
		}
		Vector2 zero = Vector2.zero;
		Vector2 camMove = InputManager.camMove;
		if (camMove != Vector2.zero)
		{
			zero -= deltaTime * panSpeed * camMove;
		}
		Vector2 vector = (Vector2)Input.mousePosition - mousePosPrev;
		mousePosPrev = Input.mousePosition;
		if ((InputManager.camDragHeldCombined || InputManager.camDragHeldLoose) && vector != Vector2.zero)
		{
			zero += deltaTime * dragSpeed * 1000f / (float)Screen.width * vector;
		}
		UITechTreeBox.noHover = zero != Vector2.zero;
		if (UITechTreeBox.noHover)
		{
			UIHover.instance.Outit();
		}
		if (zero != Vector2.zero)
		{
			rtLimits.localPosition += (Vector3)zero;
		}
		for (int i = 0; i < 10; i++)
		{
			Rect worldRect2 = rtLimits.GetWorldRect();
			float num3 = 0f - worldRect.xMin;
			float num4 = 0f - worldRect.xMax;
			float num5 = 0f - worldRect.yMin;
			float num6 = 0f - worldRect.yMax;
			float num7 = 0f - worldRect2.xMin;
			float num8 = 0f - worldRect2.xMax;
			float num9 = 0f - worldRect2.yMin;
			float num10 = 0f - worldRect2.yMax;
			Vector2 zero2 = Vector2.zero;
			if (num7 < num3)
			{
				zero2.x = num3 - num7;
			}
			else if (num8 > num4)
			{
				zero2.x = num4 - num8;
			}
			if (num9 < num5)
			{
				zero2.y = num5 - num9;
			}
			else if (num10 > num6)
			{
				zero2.y = num6 - num10;
			}
			if (zero2 == Vector2.zero)
			{
				break;
			}
			zero2 = -zero2 * zoom;
			rtLimits.localPosition += (Vector3)zero2;
			zero += zero2;
		}
		bool flag2 = zero != Vector2.zero;
		if (flag2)
		{
			rtViewport.localPosition += (Vector3)zero;
		}
		if (flag || flag2)
		{
			float x = (rtContent.transform.position.x - base.transform.position.x) / (rtContent.rect.size.x * zoom);
			float y = (rtContent.transform.position.y - base.transform.position.y) / (rtContent.rect.size.y * zoom);
			Vector3 localPosition = basePosBackCam + new Vector3(x, y, 0f) * backSpeedFactor;
			localPosition.z = basePosBackCam.z + backZoomFactor / zoom;
			tfBackgroundCam.localPosition = localPosition;
		}
	}

	private void UpdateLimitsZoom(float z)
	{
		z /= Player.uiScale;
		rtLimits.localScale = new Vector3(z, z, 1f);
	}
}
