using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIGlobal : Singleton
{
	public delegate void OnResolutionChange();

	public static UIGlobal instance;

	public Canvas canvas;

	public CanvasScaler canvasScaler;

	[SerializeField]
	private Image imBlack;

	private float curBlack;

	private float blackTarget;

	private float blackChange;

	[SerializeField]
	private GraphicRaycaster graphicsRaycaster;

	public Vector2 screenSize;

	public float screenRatio;

	[SerializeField]
	private List<UILayerObject> uiLayers = new List<UILayerObject>();

	public OnResolutionChange onResolutionChange;

	private static MouseCursorType mouseCursorType;

	public static EventSystem eventSystem { get; private set; }

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	private void Start()
	{
		eventSystem = EventSystem.current;
		ApplyCanvasScale();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	private void Update()
	{
		if (Mathf.Abs(screenSize.x - (float)Screen.width) > float.Epsilon || Mathf.Abs(screenSize.y - (float)Screen.height) > float.Epsilon)
		{
			ResolutionChanged();
		}
		bool flag = Gameplay.instance == null || Gameplay.instance.ShouldUIBeInteractable();
		if (graphicsRaycaster.enabled != flag)
		{
			graphicsRaycaster.enabled = flag;
		}
		if (curBlack != blackTarget)
		{
			curBlack = Mathf.Clamp01(curBlack + blackChange * Time.unscaledDeltaTime);
			UpdateBlack();
		}
	}

	public void ResolutionChanged()
	{
		screenSize = new Vector2(Screen.width, Screen.height);
		screenRatio = screenSize.x / screenSize.y;
		onResolutionChange?.Invoke();
	}

	public static float GetScale()
	{
		return instance.canvas.transform.localScale.x;
	}

	public static void ApplyCanvasScale()
	{
		instance.canvasScaler.referenceResolution = new Vector2(1920f / Player.uiScale, 1080f / Player.uiScale);
	}

	public Transform GetLayer(UILayer _layer)
	{
		foreach (UILayerObject uiLayer in uiLayers)
		{
			if (uiLayer.layer == _layer)
			{
				return uiLayer.obLayer;
			}
		}
		return base.transform;
	}

	public void GoBlack(bool black, float dur)
	{
		if (!(imBlack == null))
		{
			blackTarget = (black ? 1f : 0f);
			if (dur == 0f)
			{
				curBlack = blackTarget;
				UpdateBlack();
			}
			else if (curBlack != blackTarget)
			{
				blackChange = Mathf.Sign(blackTarget - curBlack) / dur;
			}
		}
	}

	private void UpdateBlack()
	{
		imBlack.SetObActive(curBlack > 0f);
		imBlack.color = imBlack.color.SetAlpha(curBlack);
	}

	public static void SetHardwareCursor(MouseCursorType cursor_type = MouseCursorType.Normal)
	{
		if (mouseCursorType != cursor_type)
		{
			mouseCursorType = cursor_type;
			AssetLinks.CursorInfo cursorInfo;
			switch (cursor_type)
			{
			default:
				return;
			case MouseCursorType.Normal:
				cursorInfo = AssetLinks.standard.cursorArrow;
				break;
			case MouseCursorType.Drag:
				cursorInfo = AssetLinks.standard.cursorHand;
				break;
			case MouseCursorType.DragHold:
				cursorInfo = AssetLinks.standard.cursorHandHold;
				break;
			case MouseCursorType.CameraMove:
				cursorInfo = AssetLinks.standard.cursorCameraMove;
				break;
			case MouseCursorType.CameraRotate:
				cursorInfo = AssetLinks.standard.cursorCameraRotate;
				break;
			case MouseCursorType.TrailDraw:
				cursorInfo = AssetLinks.standard.cursorTrailDraw;
				break;
			case MouseCursorType.TrailErase:
				cursorInfo = AssetLinks.standard.cursorTrailErase;
				break;
			case MouseCursorType.BuildingHover:
				cursorInfo = AssetLinks.standard.cursorBuildingHover;
				break;
			case MouseCursorType.BuildingPlace:
				cursorInfo = AssetLinks.standard.cursorBuildingPlace;
				break;
			case MouseCursorType.BuildingRotate:
				cursorInfo = AssetLinks.standard.cursorBuildingRotate;
				break;
			case MouseCursorType.Click:
				cursorInfo = AssetLinks.standard.cursorClick;
				break;
			case MouseCursorType.Pipette:
				cursorInfo = AssetLinks.standard.cursorPipette;
				break;
			}
			Cursor.SetCursor(cursorInfo.image, cursorInfo.hotspot, CursorMode.Auto);
		}
	}

	public bool IsPointerOverUIElement()
	{
		return IsPointerOverUIElement(GetEventSystemRaycastResults());
	}

	private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
	{
		for (int i = 0; i < eventSystemRaysastResults.Count; i++)
		{
			if (eventSystemRaysastResults[i].gameObject.layer == LayerMask.GetMask("UI"))
			{
				return true;
			}
		}
		return false;
	}

	private static List<RaycastResult> GetEventSystemRaycastResults()
	{
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = InputManager.mousePosition;
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		return list;
	}
}
