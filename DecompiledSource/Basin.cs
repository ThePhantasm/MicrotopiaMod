using System.Collections.Generic;
using UnityEngine;

public class Basin : Building
{
	public Renderer liquidSurface;

	public Vector2 liquidSurfaceRange;

	public float storageCapacity = 100f;

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		UpdateMesh();
	}

	private void UpdateMesh()
	{
		int collectedAmount = GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false);
		float y = (liquidSurfaceRange.y - liquidSurfaceRange.x) * ((float)GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false) / storageCapacity) + liquidSurfaceRange.x;
		liquidSurface.transform.localPosition = liquidSurface.transform.localPosition.TargetYPosition(y);
		if (collectedAmount <= 0)
		{
			return;
		}
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
		int collectedAmount = GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true);
		if (pickupData.state == PickupState.LIQUID && (float)(collectedAmount + 1) < storageCapacity)
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
		return base.CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(_pickup, point);
		_pickup.Delete();
		UpdateMesh();
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false) > 0)
		{
			return true;
		}
		return base.CanExtract(exchange, ref let_ant_wait, show_billboard);
	}

	public override List<PickupType> GetExtractablePickups(ExchangeType exchange)
	{
		if (exchange != ExchangeType.BUILDING_OUT)
		{
			return ConnectableObject.emptyPickupList;
		}
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
		ui_hover.UpdateInfo("Filled " + ((float)collectedAmount / storageCapacity).ToString("0%"));
	}
}
