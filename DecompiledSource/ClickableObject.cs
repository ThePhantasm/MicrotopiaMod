using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class ClickableObject : MonoBehaviour, ISaveable
{
	public Transform topPoint;

	public Transform sidePoint;

	public Transform assignLinePoint;

	private bool topFromScript;

	protected float radius = -1f;

	private Billboard billboard;

	protected ClickableObject billboardListener;

	private float billboardRemainingTime;

	private bool billboardActive;

	private bool billboardActiveTarget;

	private HighlightEffect highlightEffect;

	[NonSerialized]
	public HighlightType curHighlight;

	[NonSerialized]
	public HighlightType prevHighlight;

	[SerializeField]
	protected Transform highlightOverride;

	private List<AssignLine> assignLines = new List<AssignLine>();

	protected bool showingAssignLines;

	protected Hologram hologram;

	protected static int paramWalk;

	protected static int paramWalkSpeed;

	protected static int paramDie;

	protected static int paramLaunched;

	protected static int paramMine;

	protected static int paramFly;

	protected static int paramCarry;

	protected static int paramDoAction;

	protected static int paramSkipWinding;

	protected static int paramOpen;

	protected static int paramSpeed;

	public bool deleted { get; private set; }

	public int linkId { get; set; }

	public static void InitAnimParams()
	{
		paramWalk = Animator.StringToHash("Walk");
		paramWalkSpeed = Animator.StringToHash("Walk Speed");
		paramDie = Animator.StringToHash("Die");
		paramLaunched = Animator.StringToHash("Launched");
		paramMine = Animator.StringToHash("Mine");
		paramFly = Animator.StringToHash("Fly");
		paramCarry = Animator.StringToHash("Carry");
		paramDoAction = Animator.StringToHash("DoAction");
		paramSkipWinding = Animator.StringToHash("SkipWinding");
		paramOpen = Animator.StringToHash("Open");
		paramSpeed = Animator.StringToHash("Speed");
	}

	public virtual void Init(bool during_load = false)
	{
	}

	public virtual void Write(Save save)
	{
		save.Write(linkId);
	}

	public virtual void Read(Save save)
	{
		linkId = save.ReadInt();
		GameManager.instance.AddLinkId(this, linkId, base.gameObject.name);
	}

	private void OnEnable()
	{
		if (TryGetComponent<HighlightEffect>(out var component))
		{
			UnityEngine.Object.Destroy(component);
		}
	}

	public virtual void OnSelected(bool is_selected, bool was_selected)
	{
		UpdateBillboard();
	}

	public virtual bool IsClickable()
	{
		return true;
	}

	public virtual bool OpenUiOnClick()
	{
		return true;
	}

	public virtual bool ShouldPlayClickAudio()
	{
		return true;
	}

	public virtual void OnHoverEnter()
	{
		if (!HasHologram())
		{
			return;
		}
		if (topPoint == null)
		{
			Debug.LogError(base.name + ": need top point for hologram.");
			return;
		}
		if (hologram == null)
		{
			hologram = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(Hologram))).GetComponent<Hologram>();
			hologram.transform.parent = topPoint;
			hologram.transform.position = topPoint.position;
		}
		hologram.StartHologram(this);
		hologram.SetObActive(active: true);
	}

	public virtual void OnHoverExit()
	{
		if (hologram != null)
		{
			hologram.SetObActive(active: false);
		}
	}

	public virtual bool CanAssignTo(ClickableObject target, out string error)
	{
		error = "";
		return false;
	}

	public virtual void Assign(ClickableObject target, bool add = true)
	{
	}

	public virtual Vector3 GetAssignLinePos(AssignType assign_type)
	{
		switch (assign_type)
		{
		case AssignType.FLIGHT:
		case AssignType.CATAPULT:
			if (assignLinePoint != null)
			{
				return assignLinePoint.position;
			}
			return topPoint.position;
		case AssignType.SEND:
			if (assignLinePoint != null)
			{
				return assignLinePoint.position.TargetYPosition(5f);
			}
			return base.transform.position.TargetYPosition(5f);
		case AssignType.RETRIEVE:
			if (assignLinePoint != null)
			{
				return assignLinePoint.position.TargetYPosition(1f);
			}
			return base.transform.position.TargetYPosition(1f);
		case AssignType.GATE:
			if (assignLinePoint != null)
			{
				return assignLinePoint.position.TargetYPosition(6.36f);
			}
			return base.transform.position.TargetYPosition(6.36f);
		case AssignType.LINK:
			if (assignLinePoint != null)
			{
				return assignLinePoint.position;
			}
			return base.transform.position.TargetYPosition(6.36f);
		default:
			Debug.LogWarning("Don't know assign line position for type " + assign_type);
			return topPoint.position;
		}
	}

	public void UpdateAssignLines()
	{
		if (!showingAssignLines)
		{
			return;
		}
		foreach (AssignLine assignLine in assignLines)
		{
			if (assignLine != null && assignLine.isActiveAndEnabled)
			{
				assignLine.UpdateLine();
			}
		}
	}

	public void HideAssignLines()
	{
		showingAssignLines = false;
		foreach (AssignLine assignLine in assignLines)
		{
			if (assignLine != null)
			{
				assignLine.SetObActive(active: false);
			}
		}
	}

	public void ShowAssignLine(ClickableObject target, AssignType line_type = AssignType.NONE)
	{
		if (line_type == AssignType.NONE)
		{
			line_type = GetAssignType();
		}
		ShowAssignLines(new List<ClickableObject> { target }, line_type);
	}

	public void ShowAssignLines(List<ClickableObject> targets, AssignType line_type = AssignType.NONE, AssignLineStatus line_status = AssignLineStatus.WHITE)
	{
		if (line_type == AssignType.NONE)
		{
			line_type = GetAssignType();
		}
		List<AssignLineData> list = new List<AssignLineData>();
		foreach (ClickableObject target in targets)
		{
			list.Add(new AssignLineData(GetAssignLinePos(line_type), target.GetAssignLinePos(line_type), line_type, line_status));
		}
		ShowAssignLines(list);
	}

	public void ShowAssignLine(Vector3 start_pos, Vector3 end_pos, AssignType line_type, AssignLineStatus line_status = AssignLineStatus.WHITE)
	{
		ShowAssignLines(new List<AssignLineData>
		{
			new AssignLineData(start_pos, end_pos, line_type, line_status)
		});
	}

	public void ShowAssignLines(List<AssignLineData> data)
	{
		if (assignLines.Count < data.Count)
		{
			int num = data.Count - assignLines.Count;
			for (int i = 0; i < num; i++)
			{
				AssignLine component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(AssignLine))).GetComponent<AssignLine>();
				component.Init();
				assignLines.Add(component);
			}
		}
		foreach (AssignLine assignLine in assignLines)
		{
			assignLine.SetObActive(active: false);
		}
		for (int j = 0; j < data.Count; j++)
		{
			assignLines[j].SetLine(data[j].startPos, data[j].endPos, data[j].lineType, data[j].lineStatus);
			assignLines[j].SetObActive(active: true);
		}
		showingAssignLines = true;
	}

	public virtual void SetAssignLine(bool show)
	{
		Debug.LogWarning(base.name + ": ShowAssignLine(bool): not implemented");
	}

	public virtual float AssigningMaxRange()
	{
		return float.MaxValue;
	}

	public virtual IEnumerable<ClickableObject> EAssignedObjects()
	{
		yield break;
	}

	public virtual IEnumerable<ClickableObject> EObjectsAssignedToThis()
	{
		yield break;
	}

	public virtual AssignType GetAssignType()
	{
		return AssignType.NONE;
	}

	public virtual AfterAssignAction ActionAfterAssign()
	{
		return AfterAssignAction.SELECT_OTHER;
	}

	public virtual void CheckAssigned()
	{
	}

	public virtual void OnClickDelete()
	{
	}

	public void Delete()
	{
		if (Gameplay.instance != null)
		{
			Gameplay.instance.ClearIfSelected(this);
		}
		if (UIHover.instance != null)
		{
			UIHover.instance.Outit(this);
		}
		DoDelete();
	}

	protected virtual void DoDelete()
	{
		int count = assignLines.Count;
		for (int i = 0; i < count; i++)
		{
			UnityEngine.Object.Destroy(assignLines[i].gameObject);
		}
		assignLines.Clear();
		showingAssignLines = false;
		deleted = true;
	}

	public Transform SetTopPoint()
	{
		if (!topFromScript)
		{
			if (topPoint != null)
			{
				return topPoint;
			}
			topFromScript = true;
		}
		if (topPoint == null)
		{
			topPoint = new GameObject("Top Point").transform;
			topPoint.parent = base.transform;
		}
		Vector3 position = base.transform.position;
		RaycastHit[] array = Physics.RaycastAll(new Vector3(base.transform.position.x, base.transform.position.y + 200f, base.transform.position.z), Vector3.down);
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit = array[i];
			if (raycastHit.transform.GetComponentInParent<ClickableObject>() == this && raycastHit.point.y > position.y)
			{
				position = raycastHit.point;
			}
		}
		topPoint.transform.position = position;
		return topPoint;
	}

	public virtual float GetRadius()
	{
		if (sidePoint != null && radius < 0f)
		{
			radius = (base.transform.position.XZ() - sidePoint.position.XZ()).magnitude;
		}
		if (!(radius < 0f))
		{
			return radius;
		}
		return 1f;
	}

	public virtual Vector3 GetPosNextToOb(Vector3 origin)
	{
		return base.transform.position.ZeroPosition() + Toolkit.LookVectorNormalized(base.transform.position.ZeroPosition(), origin.ZeroPosition()) * (2.5f + GetRadius());
	}

	public virtual float GetHeight()
	{
		return topPoint.transform.position.y - base.transform.position.y;
	}

	public IEnumerator CUpdateBillboardDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		UpdateBillboard();
	}

	public void UpdateBillboard(bool cancel_temporary = false)
	{
		if (cancel_temporary)
		{
			billboardRemainingTime = 0f;
		}
		string code_desc;
		string txt_onBillboard;
		Color col;
		Transform parent;
		BillboardType billboardType = GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (UIHoverClickOb.GetSelected() == this)
		{
			billboardType = BillboardType.NONE;
		}
		if (billboard == null)
		{
			if (billboardType == BillboardType.NONE || billboardType == BillboardType.DONT_SHOW_BILLBOARD)
			{
				return;
			}
			billboard = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(Billboard))).GetComponent<Billboard>();
			StartCoroutine(CBillboard());
		}
		if (parent == null)
		{
			billboard.transform.position = topPoint.position;
			billboard.transform.parent = topPoint;
		}
		else
		{
			billboard.transform.position = parent.position;
			billboard.transform.parent = parent;
		}
		if (billboardType == BillboardType.NONE || billboardType == BillboardType.DONT_SHOW_BILLBOARD)
		{
			billboardActiveTarget = false;
			return;
		}
		billboardActiveTarget = true;
		billboard.Init(billboardType, code_desc, txt_onBillboard, col);
	}

	private IEnumerator CBillboard()
	{
		yield return null;
		billboardActive = !billboardActiveTarget;
		while (!(billboard == null))
		{
			if (billboardRemainingTime > 0f)
			{
				billboardRemainingTime -= Time.deltaTime;
				if (billboardRemainingTime <= 0f)
				{
					ClearBillboard();
					UpdateBillboard();
				}
			}
			if (billboardActive != billboardActiveTarget)
			{
				billboardActive = billboardActiveTarget;
				billboard.SetObActive(billboardActive);
			}
			yield return null;
		}
		Debug.LogError("CBillboard() but billboard is null");
	}

	public virtual BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		code_desc = "";
		txt_onBillboard = "";
		col = Color.white;
		parent = null;
		return BillboardType.NONE;
	}

	public void ForceDisableBillboard()
	{
		if (billboard != null)
		{
			billboard.SetObActive(active: false);
		}
	}

	public void SetBillboardListener(ClickableObject ob)
	{
		billboardListener = ob;
	}

	protected void UpdateBillboardTempory()
	{
		UpdateBillboard(cancel_temporary: true);
		billboardRemainingTime = 10f;
	}

	protected virtual void ClearBillboard()
	{
	}

	public virtual HologramShape GetHologramShape(out PickupType _pickup, out AntCaste _ant)
	{
		_pickup = PickupType.NONE;
		_ant = AntCaste.NONE;
		return HologramShape.None;
	}

	protected void UpdateHologram()
	{
		if (hologram != null && hologram.isActiveAndEnabled)
		{
			hologram.UpdateHologram();
		}
	}

	protected virtual bool HasHologram()
	{
		return false;
	}

	public virtual float HologramSize()
	{
		return 1f;
	}

	public virtual void SetHoverUI(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle("!TITLE MISSING!");
	}

	public virtual void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		Vector3 positionFromWorld = ui_hover.GetPositionFromWorld((topPoint == null) ? base.transform.position : topPoint.position);
		positionFromWorld.y += 1f;
		positionFromWorld.z = 0f;
		ui_hover.SetPosition(positionFromWorld);
	}

	public virtual UIClickType GetUiClickType()
	{
		return UIClickType.OLD;
	}

	public virtual void SetClickUi(UIClickLayout ui_click)
	{
		ui_click.SetTitle("!TITLE MISSING!");
	}

	public virtual void SetClickUi_LogicControl(UIClickLayout ui_click)
	{
		SetClickUi(ui_click);
	}

	public virtual void UpdateClickUi(UIClickLayout ui_click)
	{
	}

	public void UpdateHighlight()
	{
		if (curHighlight == prevHighlight)
		{
			return;
		}
		if (curHighlight == HighlightType.NONE)
		{
			if (highlightEffect == null)
			{
				Debug.LogWarning("UpdateHighlight: highlight stops, but none found for " + base.name);
			}
			else
			{
				UnityEngine.Object.Destroy(highlightEffect);
				highlightEffect = null;
			}
		}
		else
		{
			bool flag = false;
			if (prevHighlight == HighlightType.NONE)
			{
				if (highlightEffect != null)
				{
					Debug.LogWarning("UpdateHighlight: highlight starts, but already exists for " + base.name);
				}
				else
				{
					highlightEffect = base.gameObject.AddComponent<HighlightEffect>();
					flag = true;
				}
			}
			float innerGlow = 0f;
			float outline = 0f;
			Color outlineColor = Color.white;
			float overlay = 0f;
			Color overlayColor = Color.white;
			float overlayAnimationSpeed = 1.5f;
			float overlayMinIntensity = 0.45f;
			switch (curHighlight)
			{
			case HighlightType.INNERGLOW_WHITE_SOFT:
				innerGlow = 0.22f;
				break;
			case HighlightType.INNERGLOW_WHITE_STRONG:
				innerGlow = 0.44f;
				break;
			case HighlightType.OUTLINE_WHITE:
				outline = 0.1f;
				break;
			case HighlightType.OUTLINE_RED:
				outline = 0.1f;
				outlineColor = new Color(0.56f, 0.01f, 0f);
				overlay = 0.2f;
				overlayColor = new Color(0.36f, 0.01f, 0f);
				break;
			case HighlightType.OUTLINE_YELLOW:
				outline = 0.1f;
				outlineColor = new Color(0.78f, 0.65f, 0f);
				overlay = 0.2f;
				overlayColor = new Color(0.96f, 0.67f, 0f);
				break;
			case HighlightType.OUTLINE_BLUE:
				outline = 0.1f;
				outlineColor = new Color(0.15f, 0.25f, 0.95f);
				overlay = 0.2f;
				overlayColor = new Color(0.15f, 0.25f, 0.95f);
				overlayAnimationSpeed = 1f;
				break;
			case HighlightType.OUTLINE_SELECT:
				outline = 0.1f;
				outlineColor = new Color(0.3f, 0.3f, 0.4f);
				overlay = 0.3f;
				overlayColor = new Color(0.3f, 0.3f, 0.4f);
				overlayAnimationSpeed = 1f;
				overlayMinIntensity = 0.65f;
				break;
			}
			highlightEffect.innerGlow = innerGlow;
			highlightEffect.innerGlowColor = Color.white;
			highlightEffect.innerGlowWidth = 2f;
			highlightEffect.outline = outline;
			highlightEffect.outlineWidth = 1f;
			highlightEffect.outlineColor = outlineColor;
			highlightEffect.outlineQuality = HighlightPlus.QualityLevel.Highest;
			highlightEffect.outlineVisibility = Visibility.AlwaysOnTop;
			highlightEffect.overlay = overlay;
			highlightEffect.overlayColor = overlayColor;
			highlightEffect.overlayMinIntensity = overlayMinIntensity;
			highlightEffect.overlayAnimationSpeed = overlayAnimationSpeed;
			if (prevHighlight == HighlightType.NONE)
			{
				highlightEffect.highlighted = true;
				if (flag)
				{
					highlightEffect.Init(highlightOverride);
				}
			}
			else
			{
				highlightEffect.Refresh();
			}
		}
		prevHighlight = curHighlight;
	}

	public void SetHighlightPaused(bool paused)
	{
		if (highlightEffect != null)
		{
			highlightEffect.ignore = paused;
		}
	}

	public virtual void OnConfigPaste()
	{
		CheckAssigned();
	}
}
