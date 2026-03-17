using UnityEngine;

public class Beacon : Building
{
	[Header("Beacon")]
	[SerializeField]
	private GameObject obLight;

	[SerializeField]
	private GameObject obPowered;

	[SerializeField]
	private GameObject obUnpowered;

	[SerializeField]
	private float drainPerSec;

	[SerializeField]
	private bool needsBattery = true;

	private float storedEnergy;

	private bool hasBattery = true;

	private bool powered;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(storedEnergy);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.version < 44)
		{
			storedEnergy = 0f;
		}
		else
		{
			storedEnergy = save.ReadFloat();
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (currentStatus != BuildingStatus.COMPLETED || !runWorld)
		{
			return;
		}
		if (storedEnergy > 0f)
		{
			SetPowered(target: true);
			storedEnergy = Mathf.Clamp(storedEnergy - drainPerSec * dt, 0f, float.MaxValue);
			return;
		}
		if (ground.EnergyAvailable(out var found_battery))
		{
			ground.GetEnergy(drainPerSec * dt);
			SetPowered(target: true);
		}
		else
		{
			SetPowered(target: false);
		}
		if (found_battery != hasBattery)
		{
			hasBattery = found_battery;
			UpdateBillboard();
		}
	}

	private void SetPowered(bool target)
	{
		powered = target;
		obLight.SetObActive(target);
		obPowered.SetObActive(target);
		obUnpowered.SetObActive(!target);
		effectArea.SetActive(target);
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		if (PickupData.Get(_type).energyAmount > 0f)
		{
			return true;
		}
		return base.CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		if (incomingPickups_intake.Contains(_pickup))
		{
			incomingPickups_intake.Remove(_pickup);
		}
		storedEnergy += _pickup.data.energyAmount;
		_pickup.Delete();
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetInfo();
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		if (!powered)
		{
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_BEACON_NOPOWER"));
		}
		else if (storedEnergy > 0f)
		{
			float num = storedEnergy / drainPerSec;
			string text = ((num > 60f) ? num.Unit(PhysUnit.TIME_MINUTES) : num.Unit(PhysUnit.TIME));
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_BEACON_POWEREDFOR", text));
		}
		else
		{
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_BEACON_POWEREDBAT"));
		}
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (!powered)
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_BEACON_NOPOWER"));
		}
		else if (storedEnergy > 0f)
		{
			float num = storedEnergy / drainPerSec;
			string text = ((num > 60f) ? num.Unit(PhysUnit.TIME_MINUTES) : num.Unit(PhysUnit.TIME));
			ui_click.SetInfo(Loc.GetUI("BUILDING_BEACON_POWEREDFOR", text));
		}
		else
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_BEACON_POWEREDBAT"));
		}
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (needsBattery && !hasBattery)
		{
			code_desc = "BUILDING_REQ_ENERGY";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}
}
