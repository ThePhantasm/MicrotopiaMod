using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CamController : Singleton
{
	public static CamController instance;

	[SerializeField]
	private Camera cam;

	[SerializeField]
	private Camera camTrailOverlay;

	[SerializeField]
	private float moveSpeed = 0.1f;

	public float steerRotateSpeed = 0.5f;

	[SerializeField]
	private float zoomSpeed = 0.001f;

	[SerializeField]
	private float maxAngle = 40f;

	[SerializeField]
	private float angleSnapSpeed = 5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float startZoom = 1f;

	[SerializeField]
	private MinMax zoomDistMinMax;

	private float curZoom;

	private float zoomFactor;

	private float camZStart;

	[SerializeField]
	private float rotateSpeed = 3f;

	[SerializeField]
	[Range(0f, 1.1f)]
	private float switchMapAtZoom = 0.9f;

	[SerializeField]
	private LayerMask mapCullingMask;

	[SerializeField]
	private float mapSwitchBlackDuration = 0.1f;

	[SerializeField]
	private float farPlaneRegular = 2500f;

	[SerializeField]
	private float farPlaneMap = 10000f;

	[SerializeField]
	private Transform tfListener;

	private Vector3? dragPos;

	private Vector3? lastDragPos;

	private bool camMoved;

	private float camDragDistMouse;

	private float camDragDur;

	private float camDragRotDist;

	private Transform followTarget;

	private Factory followFactoryIngredient;

	private Vector3? mouseZoomPos;

	private Vector2? lastMousePos;

	private float movedDisLeft;

	private float movedDisRight;

	private float movedDisUp;

	private float movedDisDown;

	private float rotatedDisLeft;

	private float rotatedDisRight;

	private float zoomedDisIn;

	private float zoomedDisOut;

	private bool camAnimActive;

	private Vector3 camAnimPos1;

	private Vector3 camAnimPos2;

	private Quaternion camAnimRot1;

	private Quaternion camAnimRot2;

	private float camAnimZoom1;

	private float camAnimZoom2;

	private float camAnimFrac;

	private float camAnimDuration;

	private Ease camAnimEase;

	private int camCullingMask;

	private int overlayCullingMask;

	private bool freeCameraAngleOk;

	private Coroutine cSwitchToMap;

	private float zoomGame;

	private float zoomMap;

	private Ground lastClosestGround;

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
		Reset();
		camZStart = cam.transform.localPosition.z;
		zoomGame = Mathf.Clamp01(startZoom);
		SetCurZoom(zoomGame, towards_mouse: false);
		zoomMap = Mathf.Clamp01(switchMapAtZoom + 0.1f);
		camCullingMask = cam.cullingMask;
		overlayCullingMask = ((!(camTrailOverlay == null)) ? camTrailOverlay.cullingMask : 0);
		camAnimDuration = 3f;
		camAnimPos1 = (camAnimPos2 = base.transform.position);
		camAnimRot1 = (camAnimRot2 = base.transform.rotation);
		camAnimZoom1 = (camAnimZoom2 = curZoom);
	}

	public void Init()
	{
		AudioManager.instance.UpdateMainCamera();
	}

	public static void Clear()
	{
		instance.Reset();
	}

	private void UpdateListener()
	{
		if (tfListener != null)
		{
			tfListener.position = (base.transform.position + cam.transform.position) / 2f;
		}
	}

	public static Vector3 GetListenerPos()
	{
		return instance.tfListener.position;
	}

	public static Vector3 GetCamPos()
	{
		return instance.cam.transform.position;
	}

	private void Reset()
	{
		camMoved = true;
		camDragDistMouse = (camDragDur = 0f);
		camDragRotDist = 0f;
	}

	public void CamUpdate()
	{
		float deltaTime = Time.deltaTime;
		float num = 0f;
		Vector2 vector = Input.mousePosition;
		if (lastMousePos.HasValue)
		{
			num = Vector2.Distance(vector, lastMousePos.Value);
		}
		lastMousePos = vector;
		if (camAnimActive)
		{
			camAnimFrac = Mathf.Clamp01(camAnimFrac + deltaTime / camAnimDuration);
			float t = Toolkit.Easify(camAnimFrac, camAnimEase);
			base.transform.SetPositionAndRotation(Vector3.Lerp(camAnimPos1, camAnimPos2, t), Quaternion.Lerp(camAnimRot1, camAnimRot2, t));
			SetZoom(Mathf.Lerp(camAnimZoom1, camAnimZoom2, t));
			camMoved = true;
			if (camAnimFrac == 1f)
			{
				camAnimActive = false;
			}
		}
		else
		{
			dragPos = GetDragPos();
			if (followTarget != null)
			{
				Vector3 position = followTarget.position;
				if (position.y < -50f)
				{
					SetFollowTarget(null);
				}
				Vector3 forward = base.transform.forward;
				Vector3 vector2 = ((!(forward.y >= 0f)) ? (position + forward * (position.y / (0f - forward.y))) : position);
				base.transform.position = vector2;
				dragPos -= vector2;
			}
			if (InputManager.camMove != Vector2.zero)
			{
				Vector2 camMove = InputManager.camMove;
				float num2 = moveSpeed * (1f + curZoom * curZoom * 20f) * deltaTime;
				Vector3 vector3 = (base.transform.position.ZeroPosition() - cam.transform.position.ZeroPosition()).normalized * num2;
				Vector3 vector4 = base.transform.right * num2;
				MoveCam(camMove.y * vector3 + camMove.x * vector4);
				if (camMove.x > 0f)
				{
					movedDisRight += deltaTime;
				}
				if (camMove.x < 0f)
				{
					movedDisLeft += deltaTime;
				}
				if (camMove.y > 0f)
				{
					movedDisUp += deltaTime;
				}
				if (camMove.y < 0f)
				{
					movedDisDown += deltaTime;
				}
			}
			float num3 = 0f - InputManager.zoomDelta;
			if (num3 != 0f && InputManager.MouseInScene())
			{
				SetCurZoom(Mathf.Clamp01(curZoom + num3 * zoomSpeed), towards_mouse: true);
				if (num3 > 0f)
				{
					zoomedDisOut += num3;
				}
				if (num3 < 0f)
				{
					zoomedDisIn -= num3;
				}
			}
			Vector2 d_rot = Vector2.zero;
			bool flag = false;
			if (InputManager.camDragRotate.HasValue)
			{
				flag = true;
				d_rot = InputManager.camDragRotate.Value;
				camDragRotDist += Mathf.Abs(d_rot.x) + Mathf.Abs(d_rot.y);
			}
			else if (camDragRotDist != 0f)
			{
				camDragRotDist = ((camDragRotDist == 1000f) ? 0f : 1000f);
			}
			if (InputManager.camKeyRotate != 0f)
			{
				flag = true;
				d_rot.x = InputManager.camKeyRotate;
			}
			if (flag)
			{
				RotateCamera(d_rot);
				float num4 = 0f - d_rot.x;
				if (num4 > 0f)
				{
					rotatedDisLeft += num4;
				}
				if (num4 < 0f)
				{
					rotatedDisRight += num4;
				}
				camMoved = true;
			}
			else if (!Player.freeCameraAngle)
			{
				if (base.transform.rotation.eulerAngles.x < maxAngle)
				{
					if (freeCameraAngleOk)
					{
						Quaternion b = Quaternion.Euler(maxAngle, base.transform.rotation.eulerAngles.y, base.transform.rotation.eulerAngles.z);
						base.transform.rotation = Quaternion.Lerp(base.transform.rotation, b, angleSnapSpeed * deltaTime);
					}
				}
				else
				{
					freeCameraAngleOk = true;
				}
			}
			camMoved |= UpdateZoom(deltaTime);
			if ((InputManager.camDragHeldCombined || InputManager.camDragHeldLoose) && !InputManager.camDragRotate.HasValue)
			{
				if (InputManager.camDragDown || !dragPos.HasValue || !lastDragPos.HasValue)
				{
					camDragDistMouse = (camDragDur = 0f);
				}
				else
				{
					Vector3 delta = lastDragPos.Value - dragPos.Value;
					float magnitude = delta.magnitude;
					if (InputManager.camDragHeldLoose)
					{
						camDragDistMouse = 10000f;
					}
					camDragDistMouse += num;
					camDragDur += deltaTime;
					if (IsDraggingCam())
					{
						if (magnitude > 6000f * deltaTime)
						{
							delta = delta.normalized * (6000f * deltaTime);
						}
						MoveCam(delta);
						dragPos = GetDragPos();
						if (delta.x > 0f)
						{
							movedDisLeft += delta.x;
						}
						if (delta.x < 0f)
						{
							movedDisRight += delta.x;
						}
						if (delta.z > 0f)
						{
							movedDisUp += delta.z;
						}
						if (delta.z < 0f)
						{
							movedDisDown += delta.z;
						}
					}
				}
			}
			else if (!InputManager.camDragUp)
			{
				camDragDistMouse = (camDragDur = 0f);
			}
			lastDragPos = dragPos;
		}
		GameManager gameManager = GameManager.instance;
		if (!(gameManager != null))
		{
			return;
		}
		if (cSwitchToMap == null)
		{
			if (InputManager.toggleMap)
			{
				if (gameManager.mapMode)
				{
					zoomMap = curZoom;
					cSwitchToMap = StartCoroutine(CSwitchToMap(map: false, zoomGame));
				}
				else
				{
					zoomGame = curZoom;
					cSwitchToMap = StartCoroutine(CSwitchToMap(map: true, zoomMap));
				}
			}
			bool flag2 = curZoom > switchMapAtZoom;
			if (gameManager.mapMode != flag2)
			{
				cSwitchToMap = StartCoroutine(CSwitchToMap(flag2));
			}
		}
		if (camMoved || followTarget != null)
		{
			gameManager.UpdateBiomeInfluence();
			if (lastClosestGround != gameManager.closestGround)
			{
				lastClosestGround = gameManager.closestGround;
				UIGame.instance.CountInventory();
			}
			UpdateListener();
			camMoved = false;
		}
	}

	public void SetCam(Vector3 pos, float pitch, float yaw, float zoom)
	{
		base.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
		SetZoom(zoom);
	}

	public IEnumerator KCamAnim(float dur, Vector3 target_pos, float target_pitch, float? target_yaw, float target_zoom, Ease ease)
	{
		GameStatus status = GameManager.instance.GetStatus();
		if (status != GameStatus.BUSY_SYS && status != GameStatus.PASSIVE)
		{
			Debug.LogError($"KCamAnim: assume state to be BUSY_SYS or PASSIVE but is {GameManager.instance.GetStatus()}");
		}
		KoroutineId kid = SetFinalizer(delegate
		{
			camAnimActive = false;
		});
		try
		{
			camAnimPos1 = base.transform.position;
			camAnimRot1 = base.transform.rotation;
			camAnimZoom1 = curZoom;
			camAnimPos2 = target_pos;
			Vector3 eulerAngles = base.transform.rotation.eulerAngles;
			eulerAngles.x = target_pitch;
			if (target_yaw.HasValue)
			{
				eulerAngles.y = target_yaw.Value;
			}
			camAnimRot2 = Quaternion.Euler(eulerAngles);
			camAnimZoom2 = target_zoom;
			camAnimFrac = 0f;
			camAnimActive = true;
			camAnimEase = ease;
			camAnimDuration = dur;
			do
			{
				if (GameManager.instance.GetStatus() != GameStatus.MENU)
				{
					CamUpdate();
				}
				yield return null;
			}
			while (camAnimActive);
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void SetZoom(float z)
	{
		SetCurZoom(Mathf.Clamp01(z), towards_mouse: false);
		UpdateZoom();
	}

	private void SetCurZoom(float z, bool towards_mouse)
	{
		curZoom = z;
		mouseZoomPos = ((towards_mouse && followTarget == null) ? GetScreenPosAtZero(Input.mousePosition) : ((Vector3?)null));
	}

	private bool UpdateZoom(float dt = 1000f)
	{
		float num = 0f - zoomDistMinMax.Lerp(curZoom * curZoom);
		Vector3 localPosition = cam.transform.localPosition;
		float y = cam.transform.position.y;
		if (localPosition.z == num)
		{
			return false;
		}
		localPosition.z = Mathf.Lerp(localPosition.z, num, 10f * dt);
		if (Mathf.Abs(num - localPosition.z) < 0.1f)
		{
			localPosition.z = num;
		}
		Vector3? vector = null;
		if (mouseZoomPos.HasValue)
		{
			vector = mouseZoomPos.Value - cam.transform.position;
		}
		cam.transform.localPosition = localPosition;
		if (vector.HasValue)
		{
			float num2 = cam.transform.position.y - y;
			Vector3 vector2 = vector.Value.SetY(0f) * (num2 / vector.Value.y);
			Vector3 v = base.transform.position - cam.transform.position;
			vector2 -= v.SetY(0f) * (num2 / v.y);
			float num3 = vector2.magnitude / Mathf.Abs(num2);
			if (num3 > 8f)
			{
				vector2 *= 8f / num3;
			}
			base.transform.position += vector2;
		}
		if (Lighting.instance != null)
		{
			Lighting.instance.SetFogOffset(camZStart - localPosition.z);
		}
		zoomFactor = (0f - localPosition.z) / 100f;
		return true;
	}

	private IEnumerator CSwitchToMap(bool map, float snap_zoom = -1f)
	{
		if (mapSwitchBlackDuration > 0f)
		{
			UIGlobal.instance.GoBlack(black: true, mapSwitchBlackDuration);
			yield return new WaitForSeconds(mapSwitchBlackDuration);
		}
		if (snap_zoom >= 0f)
		{
			SetZoom(snap_zoom);
		}
		cam.GetUniversalAdditionalCameraData().SetRenderer(map ? 1 : 0);
		cam.farClipPlane = (map ? farPlaneMap : farPlaneRegular);
		cam.nearClipPlane = cam.farClipPlane / 25000f;
		GameManager.instance.SetMap(map);
		AudioManager.PlayUI(map ? UISfx.MapModeActivate : UISfx.MapModeDeactivate);
		if (mapSwitchBlackDuration > 0f)
		{
			UIGlobal.instance.GoBlack(black: false, mapSwitchBlackDuration);
			yield return new WaitForSeconds(mapSwitchBlackDuration);
		}
		cSwitchToMap = null;
	}

	private Vector3? GetDragPos()
	{
		return GetScreenPosAtZero(InputManager.mousePosition);
	}

	private void RotateCamera(Vector2 d_rot)
	{
		Vector3 eulerAngles = base.transform.rotation.eulerAngles;
		float value = eulerAngles.x + d_rot.y;
		value = Mathf.Clamp(value, 5f, 89.9f);
		float y = eulerAngles.y - d_rot.x;
		base.transform.rotation = Quaternion.Euler(value, y, 0f);
	}

	public void CamUpdateTheater()
	{
		RotateCamera(new Vector2(rotateSpeed * Time.deltaTime, 0f));
	}

	public bool IsDraggingCam()
	{
		if (Gameplay.instance != null && Gameplay.instance.DeselectIsRelevant())
		{
			if (!(camDragDistMouse > 80f))
			{
				return camDragDur > 0.18f;
			}
			return true;
		}
		return camDragDistMouse > 5f;
	}

	public bool IsDragRotatingCam()
	{
		return camDragRotDist >= 0.5f;
	}

	public void ToggleFollow(Transform tf)
	{
		if (followTarget == tf)
		{
			SetFollowTarget(null);
		}
		else
		{
			SetFollowTarget(tf);
		}
		lastDragPos = null;
	}

	public void SetFollowTarget(Transform tf)
	{
		followTarget = tf;
		followFactoryIngredient = null;
		if (followTarget != null && followTarget.TryGetComponent<ClickableObject>(out var component))
		{
			Gameplay.instance.Select(component);
		}
	}

	public Transform GetFollowTarget()
	{
		return followTarget;
	}

	public void SetFollowFactoryIngredient(Factory factory)
	{
		followFactoryIngredient = factory;
	}

	public Factory GetFollowFactoryIngredient()
	{
		return followFactoryIngredient;
	}

	public void View(Transform tf)
	{
		SetFollowTarget(null);
		base.transform.position = tf.position.SetY(0f);
		lastDragPos = null;
	}

	private void MoveCam(Vector3 delta)
	{
		Vector3 pos = base.transform.position + delta;
		if (GameManager.instance != null)
		{
			GameManager.instance.LimitCameraTarget(ref pos);
		}
		base.transform.position = pos;
		camMoved = true;
		if (followTarget != null)
		{
			SetFollowTarget(null);
		}
	}

	public Vector3? GetScreenPosAtZero(Vector2 screen_pos)
	{
		Ray ray = Camera.main.ScreenPointToRay(screen_pos);
		if (!new Plane(Vector3.up, 0f).Raycast(ray, out var enter))
		{
			return null;
		}
		return ray.GetPoint(enter);
	}

	public Camera GetCam()
	{
		return cam;
	}

	public void Read(Save save)
	{
		Vector3 position = save.ReadVector3();
		Quaternion rotation = Quaternion.Euler(save.ReadVector3());
		base.transform.SetPositionAndRotation(position, rotation);
		SetZoom(save.ReadFloat());
		UpdateListener();
		camMoved = true;
	}

	public void Write(Save save)
	{
		save.Write(base.transform.position);
		save.Write(base.transform.rotation.eulerAngles);
		save.Write(curZoom);
	}

	public void GetMovedDis(out float _left, out float _right, out float _up, out float _down)
	{
		_left = Mathf.Abs(movedDisLeft);
		_right = Mathf.Abs(movedDisRight);
		_up = Mathf.Abs(movedDisUp);
		_down = Mathf.Abs(movedDisDown);
	}

	public void GetRotatedDis(out float _left, out float _right)
	{
		_left = Mathf.Abs(rotatedDisLeft);
		_right = Mathf.Abs(rotatedDisRight);
	}

	public void GetZoomedDis(out float _in, out float _out)
	{
		_in = Mathf.Abs(zoomedDisIn);
		_out = Mathf.Abs(zoomedDisOut);
	}

	public void ResetDis()
	{
		movedDisLeft = 0f;
		movedDisRight = 0f;
		movedDisUp = 0f;
		movedDisDown = 0f;
		rotatedDisLeft = 0f;
		rotatedDisRight = 0f;
		zoomedDisIn = 0f;
		zoomedDisOut = 0f;
	}

	public float GetZoomFactor()
	{
		return zoomFactor;
	}

	public Texture2D GetScreenshot()
	{
		int height = 360;
		RenderTexture renderTexture = new RenderTexture(640, height, 24)
		{
			antiAliasing = 1
		};
		Texture2D texture2D = new Texture2D(640, height, TextureFormat.RGB24, mipChain: false);
		cam.targetTexture = renderTexture;
		cam.Render();
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(cam.pixelRect, 0, 0);
		texture2D.Apply();
		cam.targetTexture = null;
		RenderTexture.active = null;
		renderTexture.Release();
		Object.Destroy(renderTexture);
		return texture2D;
	}

	public void SetTrailOverlay(bool active)
	{
		camTrailOverlay.enabled = active;
		UpdateCamCulling();
	}

	public void UpdateCamCulling()
	{
		if (GameManager.instance != null && GameManager.instance.mapMode)
		{
			cam.cullingMask = mapCullingMask;
		}
		else if (Filters.IsActive(Filter.FLOATING_TRAILS))
		{
			cam.cullingMask = camCullingMask & ~overlayCullingMask;
			camTrailOverlay.cullingMask = overlayCullingMask;
		}
		else
		{
			cam.cullingMask = camCullingMask;
		}
		if (Filters.IsActive(Filter.HIDE_TRAILS))
		{
			int num = Toolkit.Mask(Layers.Trails, Layers.Splits);
			cam.cullingMask &= ~num;
			camTrailOverlay.cullingMask &= ~num;
		}
		if (Filters.IsActive(Filter.HIDE_ANTS))
		{
			int num2 = Toolkit.Mask(Layers.Ants);
			cam.cullingMask &= ~num2;
			camTrailOverlay.cullingMask &= ~num2;
		}
	}

	public IEnumerator KWorldIntro()
	{
		KoroutineId kid = SetFinalizer(delegate
		{
			GameManager.instance.SetStatus(GameStatus.RUNNING);
		});
		try
		{
			GameManager.instance.SetStatus(GameStatus.PASSIVE);
			SetCam(new Vector3(0f, 0f, 0f), 10f, 90f, 0.5f);
			yield return StartKoroutine(kid, KCamAnim(8f, new Vector3(0f, 0f, 0f), 30f, 90f, 0.2f, Ease.InOut));
		}
		finally
		{
			StopKoroutine(kid);
		}
	}
}
