using System.Collections.Generic;
using UnityEngine;

public class Catapult : Building
{
	[Header("Catapult")]
	[SerializeField]
	private Transform device;

	[SerializeField]
	private Transform ammoPoint;

	[SerializeField]
	private float maxDistance = 50f;

	[SerializeField]
	private float durWindUp;

	[SerializeField]
	private float durLaunch;

	[SerializeField]
	private float durWindDown;

	[SerializeField]
	private float timeStartLaunch;

	[SerializeField]
	private float timeReleasePickup;

	[SerializeField]
	private float timeLowerArm;

	[SerializeField]
	private bool animateHeight;

	[SerializeField]
	private float launchHeight;

	private Storage targetStorage;

	private List<Storage> assignedStorages = new List<Storage>();

	private float tShooting;

	private Pickup ammo;

	private int storageId;

	private List<int> storageIds = new List<int>();

	private bool targetIsFull;

	private bool triedUnassigned;

	private PickupState triedThrowing;

	[SerializeField]
	private AudioLink audioRotateToTargetLoop;

	[SerializeField]
	private AudioLink audioShoot;

	[SerializeField]
	private AudioLink audioRotateBackLoop;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(tShooting);
		WriteConfig(save);
		save.Write((!(ammo == null)) ? ammo.linkId : 0);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.version < 52)
		{
			save.ReadInt();
			save.ReadInt();
			save.ReadInt();
			tShooting = -1f;
		}
		else
		{
			tShooting = save.ReadFloat();
		}
		storageIds.Clear();
		if (save.version < 78)
		{
			int num = save.ReadInt();
			if (num != 0)
			{
				storageIds.Add(num);
			}
		}
		else
		{
			ReadConfig(save);
		}
		ammo = GameManager.instance.FindLink<Pickup>(save.ReadInt());
		if (ammo != null)
		{
			ammo.SetStatus(PickupStatus.IN_CONTAINER, ammoPoint);
			ammo.transform.localPosition = Vector3.zero;
			ammo.transform.localRotation = Quaternion.identity;
			if (storageId > 0 && save.version < 52)
			{
				tShooting = 0f;
			}
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		foreach (int storageId in storageIds)
		{
			Assign(GameManager.instance.FindLink<Storage>(storageId));
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		ValidateStorages();
		save.Write(assignedStorages.Count);
		foreach (Storage assignedStorage in assignedStorages)
		{
			save.Write(assignedStorage);
		}
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			BuildingLink buildingLink = save.ReadBuilding();
			if (buildingLink.postpone)
			{
				storageIds.Add(buildingLink.id);
			}
			else
			{
				Assign(buildingLink.building);
			}
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (!during_load)
		{
			tShooting = -1f;
		}
		else
		{
			ChooseTargetStorage();
		}
	}

	private void ValidateStorages()
	{
		for (int num = assignedStorages.Count - 1; num >= 0; num--)
		{
			if (assignedStorages[num] == null)
			{
				assignedStorages.RemoveAt(num);
			}
		}
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		bool let_ant_wait = false;
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld)
		{
			return;
		}
		if (tShooting == -1f)
		{
			if (ammo == null || assignedStorages.Count == 0 || !ChooseTargetStorage())
			{
				return;
			}
			StartThrow();
		}
		Quaternion quaternion;
		if (targetStorage == null)
		{
			if (tShooting < durWindUp + durLaunch)
			{
				tShooting = durWindUp + durLaunch;
			}
			quaternion = device.rotation;
		}
		else
		{
			quaternion = Quaternion.LookRotation(Toolkit.LookVector(targetStorage.transform.position.TransformYPosition(device), device.position));
		}
		if (tShooting == 0f)
		{
			StartLoopAudio(audioRotateToTargetLoop);
		}
		else if (tShooting < durWindUp)
		{
			float time = tShooting / durWindUp;
			if (animateHeight)
			{
				float y = Mathf.Lerp(0f, launchHeight, GlobalValues.standard.curveSIn.Evaluate(time));
				device.transform.localPosition = new Vector3(0f, y, 0f);
			}
			device.rotation = Quaternion.Lerp(base.transform.rotation, quaternion, GlobalValues.standard.curveSIn.Evaluate(time));
			if (tShooting.TriggersAtTime(durWindUp, dt))
			{
				StopAudio();
			}
		}
		else if (tShooting < durWindUp + durLaunch)
		{
			float time2 = tShooting - durWindUp;
			if (time2.TriggersAtTime(timeStartLaunch, dt))
			{
				StopAudio();
				if (targetStorage != null && ammo != null && targetStorage.CanInsert(ammo.type, ExchangeType.BUILDING_IN, null, ref let_ant_wait))
				{
					if (anim != null)
					{
						anim.SetBool(ClickableObject.paramDoAction, value: true);
					}
					PlayAudio(audioShoot);
				}
			}
			if (time2.TriggersAtTime(timeReleasePickup, dt) && targetStorage != null && ammo != null && targetStorage.CanInsert(ammo.type, ExchangeType.BUILDING_IN, null, ref let_ant_wait))
			{
				incomingPickups_intake.Remove(ammo);
				if (ammo != null)
				{
					ammo.Exchange(targetStorage, targetStorage.transform.position, ExchangeAnimationType.ARC);
					ammo.SetOriginBuilding(this);
				}
				ammo = null;
			}
			if (time2.TriggersAtTime(durLaunch, dt))
			{
				StartLoopAudio(audioRotateBackLoop);
			}
			if (animateHeight)
			{
				device.transform.localPosition = new Vector3(0f, launchHeight, 0f);
			}
			device.rotation = quaternion;
		}
		else
		{
			if (!(tShooting < durWindUp + durLaunch + durWindDown))
			{
				StopAudio();
				tShooting = -1f;
				return;
			}
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramDoAction, value: false);
			}
			float time3 = (tShooting - (durWindUp + durLaunch)) / durWindDown;
			if (animateHeight)
			{
				float y2 = Mathf.Lerp(launchHeight, 0f, GlobalValues.standard.curveSIn.Evaluate(time3));
				device.transform.localPosition = new Vector3(0f, y2, 0f);
			}
			device.rotation = Quaternion.Lerp(quaternion, base.transform.rotation, GlobalValues.standard.curveSIn.Evaluate(time3));
		}
		tShooting += dt;
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		SetAssignLine(is_selected);
	}

	public override void SetAssignLine(bool show)
	{
		ValidateStorages();
		if (show && assignedStorages.Count > 0)
		{
			List<ClickableObject> list = new List<ClickableObject>();
			foreach (Storage assignedStorage in assignedStorages)
			{
				list.Add(assignedStorage);
			}
			ShowAssignLines(list, AssignType.CATAPULT);
		}
		else
		{
			HideAssignLines();
		}
	}

	private void StartThrow()
	{
		tShooting = 0f;
	}

	private bool ChooseTargetStorage()
	{
		targetStorage = null;
		ValidateStorages();
		if (assignedStorages.Count == 0 || ammo == null)
		{
			return false;
		}
		List<Storage> list = new List<Storage>();
		foreach (Storage assignedStorage in assignedStorages)
		{
			bool let_ant_wait = false;
			if (assignedStorage.CanInsert(ammo.type, ExchangeType.BUILDING_IN, null, ref let_ant_wait))
			{
				list.Add(assignedStorage);
			}
		}
		if (list.Count == 1)
		{
			targetStorage = list[0];
		}
		else if (list.Count > 0)
		{
			float num = float.MaxValue;
			foreach (Storage item in list)
			{
				float num2 = Vector3.Angle(base.transform.forward, Toolkit.LookVector(base.transform.position, item.transform.position));
				if (num2 < num)
				{
					num = num2;
					targetStorage = item;
				}
			}
		}
		return targetStorage != null;
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		ClearBillboard();
		UpdateBillboard(cancel_temporary: true);
		ValidateStorages();
		if (assignedStorages.Count == 0)
		{
			if (show_billboard)
			{
				triedUnassigned = true;
				UpdateBillboardTempory();
			}
			return false;
		}
		PickupData pickupData = PickupData.Get(_type);
		if (pickupData.state == PickupState.LIVING || pickupData.state == PickupState.LIQUID)
		{
			if (show_billboard)
			{
				triedThrowing = pickupData.state;
				UpdateBillboardTempory();
			}
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (Storage assignedStorage in assignedStorages)
		{
			if (!flag && assignedStorage is Stockpile stockpile && (stockpile.allowedPickupTypes.Contains(_type) || stockpile.allowedPickupTypes.Contains(PickupType.NONE)))
			{
				flag = true;
			}
			if (!flag2 && assignedStorage.CanInsert(_type, exchange, point, ref let_ant_wait))
			{
				flag2 = true;
			}
		}
		if (flag && !flag2 && show_billboard)
		{
			targetIsFull = true;
			UpdateBillboardTempory();
		}
		if (!flag || !flag2)
		{
			return false;
		}
		if (tShooting >= 0f)
		{
			let_ant_wait = true;
			return false;
		}
		if (ammo == null)
		{
			return incomingPickups_intake.Count == 0;
		}
		return false;
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		ammo = _pickup;
		ammo.SetStatus(PickupStatus.IN_CONTAINER, ammoPoint);
		ammo.transform.localPosition = Vector3.zero;
		ammo.transform.localRotation = Quaternion.identity;
		if (ChooseTargetStorage())
		{
			StartThrow();
		}
	}

	public override bool CanAssignTo(ClickableObject target, out string error)
	{
		if (target is Stockpile item)
		{
			error = "";
			if (assignedStorages.Contains(item))
			{
				return false;
			}
			return Vector3.Distance(base.transform.position, target.transform.position) < AssigningMaxRange();
		}
		return base.CanAssignTo(target, out error);
	}

	public override void Assign(ClickableObject target, bool add = true)
	{
		if (target is Storage item)
		{
			if (!add)
			{
				if (assignedStorages.Contains(item))
				{
					assignedStorages.Remove(item);
				}
			}
			else if (target != null)
			{
				if (!assignedStorages.Contains(item))
				{
					assignedStorages.Add(item);
					if (ammo != null)
					{
						StartThrow();
					}
				}
				else
				{
					Debug.LogError("Tried assigning catapult to non-storage building, shouldn't happen");
				}
			}
		}
		ClearBillboard();
		UpdateBillboard(cancel_temporary: true);
	}

	public override float AssigningMaxRange()
	{
		return maxDistance;
	}

	public override IEnumerable<ClickableObject> EAssignedObjects()
	{
		foreach (Storage assignedStorage in assignedStorages)
		{
			if (assignedStorage != null)
			{
				yield return assignedStorage;
			}
		}
	}

	public override AssignType GetAssignType()
	{
		return AssignType.CATAPULT;
	}

	public override AfterAssignAction ActionAfterAssign()
	{
		return AfterAssignAction.CONTINUE;
	}

	public void ClearAssigned()
	{
		assignedStorages.Clear();
		SetAssignLine(show: false);
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetButtonWithText(delegate
		{
			Assign(null);
			Gameplay.instance.StartAssign(this, AssignType.CATAPULT);
		}, clear_on_click: true, Loc.GetUI("BUILDING_ASSIGN_TARGET"));
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		ui_building.SetButton(UIClickButtonType.Generic1, delegate
		{
			Assign(null);
			Gameplay.instance.StartAssign(this, AssignType.CATAPULT);
		}, InputAction.InteractBuilding);
		ui_building.SetButton(UIClickButtonType.Generic2, delegate
		{
			ClearAssigned();
		}, InputAction.DropPickup);
		ui_building.UpdateButton(UIClickButtonType.Generic1, enabled: true, Loc.GetUI("BUILDING_ASSIGN_TARGET"));
		ui_building.UpdateButton(UIClickButtonType.Generic2, enabled: true, Loc.GetUI("BUILDING_CLEAR_ASSIGNED"));
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (triedThrowing != PickupState.NONE)
		{
			switch (triedThrowing)
			{
			case PickupState.LIVING:
				code_desc = "BUILDING_CATAPULT_LARVAE";
				break;
			case PickupState.LIQUID:
				code_desc = "BUILDING_CATAPULT_FLUID";
				break;
			}
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		}
		if (targetIsFull)
		{
			code_desc = "BUILDING_CATAPULT_FULL";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		if (triedUnassigned)
		{
			code_desc = "BUILDING_CATAPULT_ASSIGN";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	protected override void ClearBillboard()
	{
		targetIsFull = false;
		triedUnassigned = false;
		triedThrowing = PickupState.NONE;
	}

	public override bool CanCopySettings()
	{
		return true;
	}

	public override void OnConfigPaste()
	{
		base.OnConfigPaste();
		if (Gameplay.instance.IsSelected(this))
		{
			SetAssignLine(show: true);
		}
	}
}
