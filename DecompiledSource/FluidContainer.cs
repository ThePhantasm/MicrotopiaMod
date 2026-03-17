using System.Collections.Generic;
using UnityEngine;

public class FluidContainer : Stockpile
{
	public Renderer liquidSurface;

	public Transform sphereParent;

	public Vector2 sphereSizeRange;

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		UpdateMesh();
	}

	private void UpdateMesh()
	{
		int collectedAmount = GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false);
		if (collectedAmount == 0)
		{
			sphereParent.transform.localScale = Vector3.zero;
			return;
		}
		float f = (float)collectedAmount / (float)data.storageCapacity;
		float num = sphereSizeRange.y * Mathf.Pow(f, 1f / 3f);
		sphereParent.transform.localScale = Vector3.one * num;
		PickupType type = PickupType.NONE;
		foreach (KeyValuePair<PickupType, int> dicCollectedPickup in GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false))
		{
			if (dicCollectedPickup.Value > 0)
			{
				type = dicCollectedPickup.Key;
				break;
			}
		}
		liquidSurface.sharedMaterial = AssetLinks.standard.GetPickupMaterial(type);
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		PickupData pickupData = PickupData.Get(_type);
		ClearBillboard();
		UpdateBillboard(cancel_temporary: true);
		if (!allowedStates.Contains(pickupData.state))
		{
			if (show_billboard)
			{
				triedInsert = pickupData.state;
				UpdateBillboardTempory();
			}
			return false;
		}
		int collectedAmount = GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true);
		if (collectedAmount < data.storageCapacity)
		{
			if (collectedAmount == 0)
			{
				return true;
			}
			Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: true);
			if (dicCollectedPickups.ContainsKey(_type) && dicCollectedPickups[_type] > 0)
			{
				return true;
			}
		}
		return false;
	}

	public override bool HasSpaceLeft(PickupType pickup_type, PileType pile_type, ExchangePoint point, out int n)
	{
		n = Mathf.RoundToInt(data.storageCapacity) - GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true);
		return n > 0;
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(_pickup, point);
		UpdateMesh();
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false) > 0)
		{
			return true;
		}
		return base.CanExtract(exchange, ref let_ant_wait, show_billboard: false);
	}

	public override List<PickupType> GetExtractablePickups(ExchangeType exchange)
	{
		if (exchange != ExchangeType.BUILDING_OUT)
		{
			return ConnectableObject.emptyPickupList;
		}
		return base.GetExtractablePickups(exchange);
	}

	public override List<PickupType> GetExtractablePickupsInternal()
	{
		List<PickupType> list = new List<PickupType>();
		foreach (KeyValuePair<PickupType, int> dicCollectedPickup in GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false))
		{
			if (dicCollectedPickup.Value > 0)
			{
				list.Add(dicCollectedPickup.Key);
			}
		}
		return list;
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		RemovePickup(_type, 1, BuildingStatus.COMPLETED);
		UpdateMesh();
		return GameManager.instance.SpawnPickup(_type, GetExtractPos(), Quaternion.identity);
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		return true;
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		ant_entered = false;
		if (_ant.GetCarryingPickupsCount() > 0 && _ant.CanDoExchange(this, ExchangeType.BUILDING_IN, null, out var _))
		{
			return _ant.StartExchangePickup(this, ExchangeType.BUILDING_IN);
		}
		return 0f;
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (ant.GetCarryingPickupsCount() > 0 && ant.CanDoExchange(this, ExchangeType.BUILDING_IN, null, out var _))
		{
			return true;
		}
		return false;
	}

	public override bool CanDispense()
	{
		return true;
	}

	protected override void UpdateTopPoint()
	{
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetInfo();
		ui_hover.SetButtonWithText(delegate
		{
			PlaceDispenser();
		}, clear_on_click: true, Loc.GetUI("BUILDING_PLACEDISPENSER"));
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		int collectedAmount = GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false);
		string uI = Loc.GetUI("BUILDING_FLUID_FILLED", (collectedAmount / data.storageCapacity).ToString("0"));
		if (uI == "")
		{
			uI = Loc.GetUI("GENERIC_PERCENTAGE", (collectedAmount / data.storageCapacity).ToString("0"));
		}
		ui_hover.UpdateInfo(uI);
	}

	protected override string CapacityInfo()
	{
		return Loc.GetUI("BUILDING_FLUID_CAPACTIY", data.storageCapacity.ToString());
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (triedInsert != PickupState.NONE && !allowedStates.Contains(triedInsert))
		{
			code_desc = "BUILDING_LIQUIDSTO_WRONG";
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}
}
