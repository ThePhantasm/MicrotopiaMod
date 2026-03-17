using System.Collections.Generic;
using UnityEngine;

public class Magnet : Storage
{
	[Header("Magnet")]
	[SerializeField]
	private Transform magnetHead;

	[SerializeField]
	private GameObject obPowered;

	[SerializeField]
	private GameObject obUnpowered;

	[SerializeField]
	private float drainPerSec;

	private PickupType magnetTargetType = PickupType.IRON_RAW;

	private ClickableObject magnetTarget;

	private float t = float.MaxValue;

	[SerializeField]
	private AudioLink audioRotateToTargetLoop;

	private bool doRotateSound = true;

	private float storedEnergy;

	private bool hasBattery = true;

	private bool powered;

	private bool materialNotFound;

	private List<PickupType> magnetableTypes = new List<PickupType>
	{
		PickupType.IRON_RAW,
		PickupType.COPPER_RAW,
		PickupType.GLASS_RAW,
		PickupType.GOLD_RAW
	};

	public override void Write(Save save)
	{
		base.Write(save);
	}

	public override void Read(Save save)
	{
		base.Read(save);
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write((int)magnetTargetType);
		save.Write(magnetHead.transform.rotation.eulerAngles);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		magnetTargetType = (PickupType)save.ReadInt();
		extractablePickupsChanged = true;
		if (save.GetSaveType() == SaveType.GameSave)
		{
			magnetHead.transform.rotation = Quaternion.Euler(save.ReadVector3());
		}
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (currentStatus != BuildingStatus.COMPLETED)
		{
			return;
		}
		if (storedEnergy > 0f)
		{
			SetPowered(target: true);
			storedEnergy = Mathf.Clamp(storedEnergy - drainPerSec * dt, 0f, float.MaxValue);
		}
		else
		{
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
		if (!powered)
		{
			if (!doRotateSound)
			{
				StopAudio();
				doRotateSound = true;
			}
			return;
		}
		if (magnetTarget == null)
		{
			t += dt;
			if (t > 1f)
			{
				if (ShouldDoMagnet())
				{
					magnetTarget = GetMagnetTarget();
					materialNotFound = magnetTarget == null;
					UpdateBillboard();
				}
				t = 0f;
			}
			return;
		}
		if (magnetTarget is Pickup pickup && pickup.GetStatus() != PickupStatus.ON_GROUND)
		{
			magnetTarget = null;
			return;
		}
		Quaternion quaternion = Quaternion.LookRotation(magnetTarget.transform.position.TargetYPosition(0f) - magnetHead.position.TargetYPosition(0f));
		if (magnetHead.transform.rotation != quaternion)
		{
			if (doRotateSound)
			{
				StartLoopAudio(audioRotateToTargetLoop);
				doRotateSound = false;
			}
			magnetHead.transform.rotation = Quaternion.RotateTowards(magnetHead.transform.rotation, quaternion, 22.5f * dt);
			return;
		}
		if (!doRotateSound)
		{
			StopAudio();
			doRotateSound = true;
		}
		t += dt;
		if (!(t > 1f))
		{
			return;
		}
		if (ShouldDoMagnet())
		{
			if (magnetTarget is Pickup pickup2)
			{
				if (pickup2.type == magnetTargetType && pickup2.GetStatus() == PickupStatus.ON_GROUND)
				{
					pickup2.transform.rotation = insertPoint.rotation;
					pickup2.Exchange(this, insertPoint.position, ExchangeAnimationType.MAGNET_PULL);
				}
				magnetTarget = null;
			}
			else if (magnetTarget is BiomeObject biomeObject)
			{
				if (biomeObject.HasExtractablePickup(ExchangeType.PICKUP, magnetTargetType))
				{
					Pickup pickup3 = biomeObject.ExtractPickup(magnetTargetType);
					pickup3.transform.rotation = insertPoint.rotation;
					pickup3.Exchange(this, insertPoint.position, ExchangeAnimationType.MAGNET_PULL);
				}
				else
				{
					magnetTarget = null;
				}
			}
		}
		t = 0f;
	}

	private void SetPowered(bool target)
	{
		powered = target;
		obPowered.SetObActive(target);
		obUnpowered.SetObActive(!target);
	}

	public bool ShouldDoMagnet()
	{
		if (GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true) > 0)
		{
			return false;
		}
		return true;
	}

	private ClickableObject GetMagnetTarget()
	{
		ClickableObject result = null;
		float num = float.MaxValue;
		foreach (BiomeObject item in GameManager.instance.EBiomeObjects(ground))
		{
			if (item.HasExchangeType(ExchangeType.PICKUP) && item.HasExtractablePickup(ExchangeType.PICKUP, magnetTargetType))
			{
				float num2 = Vector3.Distance(insertPoint.position, item.transform.position);
				if (num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		foreach (Pickup item2 in ground.EPickupsOnGround(magnetTargetType))
		{
			if (item2.GetStatus() == PickupStatus.ON_GROUND)
			{
				float num3 = Vector3.Distance(insertPoint.position, item2.transform.position);
				if (num3 < num)
				{
					num = num3;
					result = item2;
				}
			}
		}
		return result;
	}

	protected override void OnPickupArrival_Intake(Pickup p, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(p, point);
		materialNotFound = false;
		UpdateBillboard();
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		Pickup pickup = base.ExtractPickup(_type);
		pickup.transform.position = extractPoint.position;
		return pickup;
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.MAGNET;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		UIClickLayout_DispenserSmart obj = (UIClickLayout_DispenserSmart)ui_building;
		obj.SetInventory(target: true);
		List<PickupType> draft = new List<PickupType> { magnetTargetType };
		obj.SetIcons(Loc.GetUI("BUILDING_MAGNET_ATTRACTS"), Loc.GetUI("BUILDING_MAGNET_CLICK"), draft, magnetableTypes, delegate
		{
			magnetTargetType = draft[0];
			extractablePickupsChanged = true;
			ClearBillboard();
			UpdateBillboard(cancel_temporary: true);
		});
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
		UIClickLayout_DispenserSmart obj = (UIClickLayout_DispenserSmart)ui_click;
		obj.UpdateIcons(new List<PickupType> { magnetTargetType });
		obj.inventoryGrid.Update(Loc.GetUI("BUILDING_MAGNET_CONTAINS"), GetExtractablePickups(ExchangeType.BUILDING_OUT), Loc.GetUI("GENERIC_EMPTY"));
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (!hasBattery)
		{
			code_desc = "BUILDING_REQ_ENERGY";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		if (materialNotFound)
		{
			code_desc = "BUILDING_MAGNET_MATERIAL_NOT_FOUND";
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
