using System;
using System.Collections.Generic;
using UnityEngine;

public class BatteryBuilding : Building
{
	[SerializeField]
	private float energyCapacity = 100f;

	[SerializeField]
	private List<Transform> listProgressBars = new List<Transform>();

	[NonSerialized]
	public float storedEnergy;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(storedEnergy);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		storedEnergy = save.ReadFloat();
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		UpdateVisual();
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange == ExchangeType.BUILDING_IN)
		{
			PickupData pickupData = PickupData.Get(_type);
			if (pickupData.energyAmount > 0f && storedEnergy < energyCapacity - pickupData.energyAmount)
			{
				return true;
			}
		}
		return base.CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		if (incomingPickups_intake.Contains(_pickup))
		{
			incomingPickups_intake.Remove(_pickup);
		}
		storedEnergy = Mathf.Clamp(storedEnergy + _pickup.data.energyAmount, 0f, energyCapacity);
		UpdateVisual();
		_pickup.Delete();
	}

	public float GetEnergy(float amount)
	{
		float num = Mathf.Min(storedEnergy, amount);
		storedEnergy = Mathf.Clamp(storedEnergy - num, 0f, energyCapacity);
		UpdateVisual();
		return num;
	}

	private void UpdateVisual()
	{
		foreach (Transform listProgressBar in listProgressBars)
		{
			Vector3 localScale = listProgressBar.transform.localScale;
			localScale.y = storedEnergy / energyCapacity;
			listProgressBar.transform.localScale = localScale;
		}
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetEnergy(Loc.GetUI("BUILDING_BATTERY_STOREDENERGY"));
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		ui_hover.UpdateEnergy(Mathf.Round(storedEnergy) + " / " + Mathf.Round(energyCapacity), storedEnergy / energyCapacity);
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BATTERYBUILDING;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		((UIClickLayout_BatteryBuilding)ui_building).SetEnergy(Loc.GetUI("BUILDING_BATTERY_STOREDENERGY"));
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		((UIClickLayout_BatteryBuilding)ui_click).UpdateEnergy(Mathf.Round(storedEnergy) + " / " + Mathf.Round(energyCapacity), storedEnergy / energyCapacity);
	}
}
