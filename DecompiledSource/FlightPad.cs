using System;
using System.Collections.Generic;
using UnityEngine;

public class FlightPad : Building
{
	[Header("Flight Pad")]
	public bool launchPad;

	public Transform landPoint;

	public float landPointRadius = 1f;

	public static float maxFlightHeight = 50f;

	private FlightPad targetLandPad;

	private int targetLandPadId;

	private Trail exitTrail;

	private bool assignWarning;

	private bool spaceWarning;

	[NonSerialized]
	public int relocation;

	public override void Write(Save save)
	{
		base.Write(save);
		WriteConfig(save);
		save.Write((!(exitTrail == null)) ? exitTrail.linkId : 0);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		ReadConfig(save);
		exitTrail = GameManager.instance.FindLink<Trail>(save.ReadInt());
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		targetLandPad = GameManager.instance.FindLink<FlightPad>(targetLandPadId);
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write(targetLandPad);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		BuildingLink buildingLink = save.ReadBuilding();
		if (buildingLink.postpone)
		{
			targetLandPadId = buildingLink.id;
		}
		else
		{
			targetLandPad = buildingLink.building as FlightPad;
		}
		if (save.GetSaveType() == SaveType.CopyConfig && Gameplay.instance.IsSelected(this))
		{
			SetAssignLine(show: true);
		}
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		ClearBillboard();
		UpdateBillboard();
		if (_ant.data.flying)
		{
			if (targetLandPad == null)
			{
				assignWarning = true;
				UpdateBillboard();
				return false;
			}
			if (targetLandPad.GetAntsOnTrails().Count > 1)
			{
				spaceWarning = true;
				UpdateBillboard();
				return false;
			}
			return true;
		}
		return true;
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		if (_ant.data.flying)
		{
			if (targetLandPad != null)
			{
				Vector3 position = targetLandPad.landPoint.position;
				position.x += UnityEngine.Random.insideUnitCircle.x * landPointRadius;
				position.z += UnityEngine.Random.insideUnitCircle.y * landPointRadius;
				_ant.StartFlying(position, targetLandPad);
			}
		}
		else
		{
			Vector3 vector = base.transform.position - base.transform.forward * 11f + new Vector3(UnityEngine.Random.insideUnitCircle.x, 0f, UnityEngine.Random.insideUnitCircle.y);
			Quaternion rotation = Quaternion.LookRotation(Toolkit.LookVector(_ant.transform.position, vector.TargetYPosition(_ant.transform.position.y)));
			_ant.SetCurrentTrail(null);
			_ant.transform.SetPositionAndRotation(vector, rotation);
		}
		ant_entered = true;
		return 0f;
	}

	public void LandOnPad(Ant _ant)
	{
		_ant.GetOnNewTrail(exitTrail);
	}

	public override bool CanAssignTo(ClickableObject target, out string error)
	{
		if (launchPad && target is FlightPad { launchPad: false })
		{
			error = "";
			return true;
		}
		return base.CanAssignTo(target, out error);
	}

	public override void Assign(ClickableObject target, bool add = true)
	{
		if (!(target is FlightPad))
		{
			Debug.LogError("Tried assigning non-flight pad to flight pad, shouldn't happen");
			return;
		}
		ClearBillboard();
		UpdateBillboard();
		if (add)
		{
			targetLandPad = (FlightPad)target;
		}
		else
		{
			targetLandPad = null;
		}
		Gameplay.instance.Select(this);
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (is_selected)
		{
			SetAssignLine(show: true);
			{
				foreach (ClickableObject item in EObjectsAssignedToThis())
				{
					item.ShowAssignLine(this, AssignType.FLIGHT);
				}
				return;
			}
		}
		HideAssignLines();
		foreach (ClickableObject item2 in EObjectsAssignedToThis())
		{
			item2.HideAssignLines();
		}
	}

	public override void SetAssignLine(bool show)
	{
		if (show && launchPad && targetLandPad != null)
		{
			ShowAssignLine(targetLandPad, AssignType.FLIGHT);
		}
		else
		{
			HideAssignLines();
		}
	}

	public static Vector3 GetPointInFlightArc(Vector3 start, Vector3 end, float progress)
	{
		Vector3 result = start * (1f - progress) + end * progress;
		AnimationCurve curveParabola = GlobalValues.standard.curveParabola;
		float num = Vector3.Distance(start, end);
		result.y += curveParabola.Evaluate(progress) * Mathf.Clamp(num / 2f, 0f, maxFlightHeight);
		return result;
	}

	protected override void SetBuildingTrailActionPoint(Trail _trail, ExchangeType _type)
	{
		base.SetBuildingTrailActionPoint(_trail, _type);
		if (_type == ExchangeType.EXIT)
		{
			exitTrail = _trail;
		}
	}

	public override IEnumerable<ClickableObject> EAssignedObjects()
	{
		if (launchPad && (bool)targetLandPad)
		{
			yield return targetLandPad;
		}
	}

	public override AssignType GetAssignType()
	{
		return AssignType.FLIGHT;
	}

	public override IEnumerable<ClickableObject> EObjectsAssignedToThis()
	{
		foreach (FlightPad item in GameManager.instance.ELaunchPads())
		{
			foreach (ClickableObject item2 in item.EAssignedObjects())
			{
				if (item2 == this)
				{
					yield return item;
					break;
				}
			}
		}
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (!ant.data.flying || AnyAntsOnBuildingTrails(trail))
		{
			return false;
		}
		return base.CheckIfGateIsSatisfied(ant, trail, out warning);
	}

	public override void Relocate(Vector3 pos, Quaternion rot)
	{
		base.Relocate(pos, rot);
		relocation++;
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		if (launchPad)
		{
			ui_hover.SetButtonWithText(delegate
			{
				Gameplay.instance.StartAssign(this, AssignType.FLIGHT);
			}, clear_on_click: true, Loc.GetUI("BUILDING_ASSIGN_TARGET"));
		}
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		if (launchPad)
		{
			ui_building.SetButton(UIClickButtonType.Generic1, delegate
			{
				Gameplay.instance.StartAssign(this, AssignType.FLIGHT);
			}, InputAction.InteractBuilding);
			ui_building.UpdateButton(UIClickButtonType.Generic1, enabled: true, Loc.GetUI("BUILDING_ASSIGN_TARGET"));
		}
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (assignWarning)
		{
			code_desc = "BUILDING_LAUNCH_CONNECT";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		if (spaceWarning)
		{
			code_desc = "BUILDING_LAUNCH_NOSPACE";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	protected override void ClearBillboard()
	{
		assignWarning = false;
		spaceWarning = false;
	}

	public override bool CanCopySettings()
	{
		return true;
	}
}
