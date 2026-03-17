using System.Collections.Generic;
using UnityEngine;

public class DispenserSmart : Dispenser
{
	private PickupType targetPickupType;

	private bool selectWarning;

	private bool emptyWarning;

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
		save.Write((int)targetPickupType);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		targetPickupType = (PickupType)save.ReadInt();
		extractablePickupsChanged = true;
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		ClearBillboard();
		UpdateBillboard(cancel_temporary: true);
		if (targetPickupType == PickupType.NONE)
		{
			if (show_billboard)
			{
				selectWarning = true;
				UpdateBillboardTempory();
			}
			return false;
		}
		if (GetAmountAvailableOnGround() == 0)
		{
			return false;
		}
		return true;
	}

	public override List<PickupType> GetExtractablePickupsInternal()
	{
		if (targetPickupType == PickupType.NONE)
		{
			return ConnectableObject.emptyPickupList;
		}
		return new List<PickupType> { targetPickupType };
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		List<Stockpile> list = new List<Stockpile>();
		foreach (Stockpile item in ground.EStockpilesForExtract(targetPickupType, only_open_to_smart: true))
		{
			if (item.OpenToSmartDispensers())
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			Debug.LogWarning("Tried exchanging from inventory to building but no suitable stockpile was found, function shouldn't have been fired");
			return null;
		}
		Dictionary<Storage, float> distances = new Dictionary<Storage, float>();
		float x = base.transform.position.x;
		float z = base.transform.position.z;
		foreach (Stockpile item2 in list)
		{
			float num = item2.transform.position.x - x;
			float num2 = item2.transform.position.z - z;
			distances.Add(item2, num * num + num2 * num2);
		}
		list.Sort((Stockpile s1, Stockpile s2) => distances[s1].CompareTo(distances[s2]));
		Pickup pickup = list[0].ExtractPickup(_type);
		pickup.transform.SetPositionAndRotation(extractPoint.position, extractPoint.rotation);
		return pickup;
	}

	public override Dictionary<PickupType, int> GetDicAvailablePickups(bool include_incoming)
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		if (targetPickupType != PickupType.NONE)
		{
			dictionary.Add(targetPickupType, GetAmountAvailableOnGround());
		}
		return dictionary;
	}

	public override List<PickupType> GetAllowedPickups()
	{
		List<PickupType> list = new List<PickupType>();
		if (targetPickupType != PickupType.NONE && targetPickupType != PickupType.ANY)
		{
			list.Add(targetPickupType);
		}
		return list;
	}

	public int GetAmountAvailableOnGround()
	{
		if (targetPickupType == PickupType.NONE || targetPickupType == PickupType.ANY)
		{
			return 0;
		}
		int num = 0;
		foreach (Stockpile item in ground.EStockpilesForExtract(targetPickupType, only_open_to_smart: true))
		{
			num += item.GetCollectedAmount(targetPickupType, BuildingStatus.COMPLETED, include_incoming: false);
		}
		return num;
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetInventory();
		List<PickupType> draft = new List<PickupType> { targetPickupType };
		ui_hover.SetIcons("", draft, new List<PickupState>
		{
			PickupState.SOLID,
			PickupState.DUST,
			PickupState.LIQUID
		}, delegate
		{
			targetPickupType = draft[0];
			extractablePickupsChanged = true;
			ClearBillboard();
			UpdateBillboard(cancel_temporary: true);
		});
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		if (targetPickupType != PickupType.NONE)
		{
			dictionary.Add(targetPickupType, ground.GetInventoryAmount(targetPickupType));
		}
		ui_hover.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), dictionary, Loc.GetUI("GENERIC_NOTHING"), no_text: false, include_zero: true);
		ui_hover.UpdateIcons(new List<PickupType> { targetPickupType });
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.DISPENSER_SMART;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		UIClickLayout_DispenserSmart obj = (UIClickLayout_DispenserSmart)ui_building;
		obj.SetInventory(target: true);
		List<PickupType> draft = new List<PickupType> { targetPickupType };
		obj.SetIcons("", Loc.GetUI("BUILDING_ALLOWED_MATERIAL_CLICK"), draft, new List<PickupState>
		{
			PickupState.SOLID,
			PickupState.DUST,
			PickupState.LIQUID,
			PickupState.LIVING
		}, delegate
		{
			targetPickupType = draft[0];
			extractablePickupsChanged = true;
			ClearBillboard();
			UpdateBillboard(cancel_temporary: true);
		});
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		UIClickLayout_DispenserSmart obj = (UIClickLayout_DispenserSmart)ui_click;
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		if (targetPickupType != PickupType.NONE)
		{
			dictionary.Add(targetPickupType, GetAmountAvailableOnGround());
		}
		obj.inventoryGrid.Update(Loc.GetUI("BUILDING_DISPENSES"), dictionary, Loc.GetUI("GENERIC_NOTHING"), no_text: false, include_zero: true);
		obj.UpdateIcons(new List<PickupType> { targetPickupType });
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (selectWarning)
		{
			code_desc = "BUILDING_DISSMART_SELECT";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		if (emptyWarning)
		{
			code_desc = "BUILDING_DISSMART_EMPTY";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	protected override void ClearBillboard()
	{
		selectWarning = false;
		emptyWarning = false;
	}

	public override bool CanCopySettings()
	{
		return true;
	}
}
