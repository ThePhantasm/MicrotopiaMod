using System.Collections.Generic;
using UnityEngine;

public class DispenserRegular : Dispenser
{
	[Header("Dispenser")]
	public float maxDistance = 10f;

	private Building connectedBuilding;

	private int connectedStorageId = -1;

	private float wantEmptyWarningCountdown = -1f;

	private bool wantEmptyWarning;

	private bool emptyWarning;

	private float emptyWarningTimer;

	public override void Write(Save save)
	{
		base.Write(save);
		WriteConfig(save);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		ReadConfig(save);
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write(connectedBuilding);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		BuildingLink buildingLink = save.ReadBuilding();
		if (buildingLink.postpone)
		{
			connectedStorageId = buildingLink.id;
		}
		else
		{
			connectedBuilding = buildingLink.building;
		}
		if (save.GetSaveType() == SaveType.CopyConfig && Gameplay.instance.IsSelected(this))
		{
			SetAssignLine(show: true);
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		connectedBuilding = GameManager.instance.FindLink<Storage>(connectedStorageId);
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!emptyWarning)
		{
			if (wantEmptyWarning)
			{
				wantEmptyWarningCountdown += dt;
				if (wantEmptyWarningCountdown > 180f)
				{
					wantEmptyWarning = false;
					wantEmptyWarningCountdown = 0f;
					emptyWarning = true;
				}
			}
			else
			{
				wantEmptyWarningCountdown = 0f;
			}
		}
		else
		{
			emptyWarningTimer -= dt;
			if (emptyWarningTimer < 0f)
			{
				emptyWarningTimer = 1f;
				bool let_ant_wait = false;
				emptyWarning = connectedBuilding == null || !connectedBuilding.CanExtract(ExchangeType.BUILDING_OUT, ref let_ant_wait);
				UpdateBillboard();
			}
		}
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		bool let_ant_wait2 = false;
		if (connectedBuilding != null && connectedBuilding.CanExtract(exchange, ref let_ant_wait2))
		{
			if (wantEmptyWarning || emptyWarning)
			{
				wantEmptyWarning = false;
				emptyWarning = false;
				UpdateBillboard();
			}
			return exchange == ExchangeType.BUILDING_OUT;
		}
		if (connectedBuilding == null)
		{
			emptyWarning = true;
		}
		else
		{
			wantEmptyWarning = true;
		}
		return false;
	}

	public override List<PickupType> GetExtractablePickups(ExchangeType exchange)
	{
		if (connectedBuilding == null)
		{
			return ConnectableObject.emptyPickupList;
		}
		return connectedBuilding.GetExtractablePickups(exchange);
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		Pickup pickup = connectedBuilding.ExtractPickup(_type);
		pickup.transform.SetPositionAndRotation(extractPoint.position, extractPoint.rotation);
		return pickup;
	}

	public override Dictionary<PickupType, int> GetDicAvailablePickups(bool include_incoming)
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		if (connectedBuilding != null)
		{
			foreach (KeyValuePair<PickupType, int> dicCollectedPickup in connectedBuilding.GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming))
			{
				if (!dictionary.ContainsKey(dicCollectedPickup.Key))
				{
					dictionary.Add(dicCollectedPickup.Key, 0);
				}
				dictionary[dicCollectedPickup.Key] += dicCollectedPickup.Value;
			}
		}
		return dictionary;
	}

	public override List<PickupType> GetAllowedPickups()
	{
		List<PickupType> list = new List<PickupType>();
		if (connectedBuilding != null && connectedBuilding is Stockpile stockpile)
		{
			list.AddRange(stockpile.allowedPickupTypes);
		}
		return list;
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		SetAssignLine(is_selected);
	}

	public override bool CanAssignTo(ClickableObject target, out string error)
	{
		if (target is Building building)
		{
			error = "";
			if (building.CanDispense())
			{
				return building.ground == ground;
			}
			return false;
		}
		return base.CanAssignTo(target, out error);
	}

	public override void Assign(ClickableObject target, bool add = true)
	{
		if (!(target is Building))
		{
			Debug.LogError("Wrong assignment");
			return;
		}
		if (add)
		{
			connectedBuilding = (Building)target;
		}
		else
		{
			connectedBuilding = null;
		}
		wantEmptyWarning = false;
		emptyWarning = false;
		UpdateBillboard();
		if (attachedTo != null)
		{
			attachedTo.UpdateBillboard();
		}
	}

	public override IEnumerable<ClickableObject> EAssignedObjects()
	{
		if (connectedBuilding != null)
		{
			yield return connectedBuilding;
		}
	}

	public override AssignType GetAssignType()
	{
		return AssignType.RETRIEVE;
	}

	public override float AssigningMaxRange()
	{
		return maxDistance;
	}

	public override void SetAssignLine(bool show)
	{
		if (show && connectedBuilding != null)
		{
			ShowAssignLine(connectedBuilding, AssignType.RETRIEVE);
		}
		else
		{
			HideAssignLines();
		}
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetInventory();
		ui_hover.SetButtonWithText(delegate
		{
			Gameplay.instance.StartAssign(this, AssignType.RETRIEVE);
		}, clear_on_click: true, Loc.GetUI("BUILDING_ASSIGN_TARGET"));
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		if (connectedBuilding != null)
		{
			if (connectedBuilding is Stockpile stockpile)
			{
				ui_hover.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), stockpile.GetDicExtractables(), Loc.GetUI("GENERIC_EMPTY"), no_text: false, include_zero: true);
			}
			else
			{
				ui_hover.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), connectedBuilding.GetExtractablePickups(ExchangeType.BUILDING_OUT), Loc.GetUI("GENERIC_EMPTY"));
			}
		}
		else
		{
			ui_hover.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), new List<PickupType>(), Loc.GetUI("GENERIC_NOTHING"));
		}
		ui_hover.UpdateButtonWithText((connectedBuilding == null) ? Loc.GetUI("BUILDING_ASSIGN_TARGET") : Loc.GetUI("BUILDING_CHANGE_TARGET"));
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.DISPENSER_REGULAR;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		ui_building.SetInfo("");
		ui_building.SetInventory(target: true);
		ui_building.SetButton(UIClickButtonType.PlaceDispenser, delegate
		{
			Gameplay.instance.StartAssign(this, AssignType.RETRIEVE);
		}, InputAction.InteractBuilding);
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		UIClickLayout_Building uIClickLayout_Building = (UIClickLayout_Building)ui_click;
		if (connectedBuilding != null)
		{
			if (connectedBuilding is Stockpile stockpile)
			{
				uIClickLayout_Building.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), stockpile.GetDicExtractables(), Loc.GetUI("GENERIC_EMPTY"), no_text: false, include_zero: true);
			}
			else
			{
				uIClickLayout_Building.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), connectedBuilding.GetExtractablePickups(ExchangeType.BUILDING_OUT), Loc.GetUI("GENERIC_EMPTY"));
			}
		}
		else
		{
			uIClickLayout_Building.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), new List<PickupType>(), Loc.GetUI("GENERIC_NOTHING"));
		}
		uIClickLayout_Building.UpdateButton(UIClickButtonType.PlaceDispenser, enabled: true, (connectedBuilding == null) ? Loc.GetUI("BUILDING_ASSIGN_TARGET") : Loc.GetUI("BUILDING_CHANGE_TARGET"));
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (emptyWarning)
		{
			if (connectedBuilding == null)
			{
				code_desc = "BUILDING_DISPENSER_NOSTOCKPILE";
			}
			else
			{
				code_desc = "BUILDING_DISPENSER_EMPTY";
			}
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	public override bool CanCopySettings()
	{
		return true;
	}
}
