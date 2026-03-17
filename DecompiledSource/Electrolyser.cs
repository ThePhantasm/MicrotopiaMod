using System.Collections.Generic;
using UnityEngine;

public class Electrolyser : Building
{
	[SerializeField]
	private float energyConsumption = 10f;

	[SerializeField]
	private float electrolyseRate_base = 1f;

	[SerializeField]
	private float electrolyseRate_movSpeed;

	[SerializeField]
	private float enterBoost = 10f;

	[SerializeField]
	private GameObject pfBeamEffect;

	[SerializeField]
	private Explosion pfContactBoost;

	[SerializeField]
	private Transform beamOrigin;

	[SerializeField]
	private float coilRadius;

	[SerializeField]
	private ParticleSystem psElectricity;

	private List<PickupType> typesToElectrolyse = new List<PickupType>();

	private List<LineRenderer> listBeams = new List<LineRenderer>();

	private List<Ant> rememberedAnts = new List<Ant>();

	private List<int> rememberedAntIds = new List<int>();

	private bool isActive;

	[SerializeField]
	private AudioLink audioActiveLoop;

	private bool hasBattery;

	private bool powered;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(rememberedAnts.Count);
		foreach (Ant rememberedAnt in rememberedAnts)
		{
			save.Write(rememberedAnt.linkId);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.version >= 34)
		{
			int num = save.ReadInt();
			rememberedAntIds.Clear();
			for (int i = 0; i < num; i++)
			{
				rememberedAntIds.Add(save.ReadInt());
			}
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		rememberedAnts.Clear();
		foreach (int rememberedAntId in rememberedAntIds)
		{
			rememberedAnts.Add(GameManager.instance.FindLink<Ant>(rememberedAntId));
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		typesToElectrolyse.Clear();
		foreach (string item in EFactoryRecipes())
		{
			foreach (PickupCost item2 in FactoryRecipeData.Get(item).costsPickup)
			{
				if (!typesToElectrolyse.Contains(item2.type))
				{
					typesToElectrolyse.Add(item2.type);
				}
			}
		}
	}

	public override void BuildingFixedUpdate(float xdt, bool runWorld)
	{
		base.BuildingFixedUpdate(xdt, runWorld);
		if (currentStatus != BuildingStatus.COMPLETED || !runWorld)
		{
			return;
		}
		List<Ant> list = new List<Ant>();
		float num = 0f;
		FactoryRecipeData _data;
		foreach (Collider item in triggerArea.EOverlapping())
		{
			Ant componentInParent = item.GetComponentInParent<Ant>();
			if (list.Contains(componentInParent))
			{
				continue;
			}
			list.Add(componentInParent);
			foreach (Pickup carryingPickup in componentInParent.carryingPickups)
			{
				if (carryingPickup.data.CanElectrolyse(out _data))
				{
					num += 1f;
				}
			}
		}
		foreach (LineRenderer listBeam in listBeams)
		{
			listBeam.SetObActive(active: false);
		}
		powered = ground.EnergyAvailable(out var found_battery);
		if (powered)
		{
			if (num > 0f)
			{
				if (!isActive)
				{
					isActive = true;
					anim.SetBool(ClickableObject.paramDoAction, value: true);
					psElectricity.SetObActive(active: true);
					StartLoopAudio(audioActiveLoop);
				}
				float num2 = ground.GetEnergy(energyConsumption * xdt) / (energyConsumption * xdt);
				float num3 = 1f / num;
				int num4 = 0;
				foreach (Ant item2 in list)
				{
					bool flag = false;
					foreach (Pickup carryingPickup2 in item2.carryingPickups)
					{
						bool flag2 = false;
						if (!rememberedAnts.Contains(item2))
						{
							rememberedAnts.Add(item2);
							flag2 = true;
						}
						if (carryingPickup2.data.CanElectrolyse(out _data))
						{
							flag = true;
							item2.AddElectrolyse(electrolyseRate_base * xdt * num2 * num3);
							item2.AddElectrolyse(electrolyseRate_movSpeed * xdt * item2.velocity * num2 * num3);
							if (flag2)
							{
								item2.AddElectrolyse(enterBoost * num2 * num3);
								Explosion explosion = Object.Instantiate(pfContactBoost, carryingPickup2.transform.position, carryingPickup2.transform.rotation);
								explosion.transform.parent = carryingPickup2.transform;
								explosion.Init();
							}
							if (num4 + 1 > listBeams.Count)
							{
								LineRenderer component = Object.Instantiate(pfBeamEffect, base.transform).GetComponent<LineRenderer>();
								listBeams.Add(component);
							}
							listBeams[num4].SetObActive(active: true);
							if (coilRadius > 0f)
							{
								listBeams[num4].SetPosition(0, beamOrigin.position + Toolkit.LookVectorNormalized(beamOrigin.position, carryingPickup2.transform.position.TransformYPosition(beamOrigin)) * coilRadius);
							}
							else
							{
								listBeams[num4].SetPosition(0, beamOrigin.position);
							}
							listBeams[num4].SetPosition(1, carryingPickup2.transform.position + carryingPickup2.transform.up * carryingPickup2.GetRadius());
							num4++;
						}
					}
					if (flag)
					{
						item2.GainStatusEffect(StatusEffect.ELECTROLYSED);
					}
				}
			}
			for (int num5 = rememberedAnts.Count - 1; num5 >= 0; num5--)
			{
				Ant ant = rememberedAnts[num5];
				if (!list.Contains(ant))
				{
					rememberedAnts.RemoveAt(num5);
					ant.StopElectrolysing();
				}
			}
		}
		else
		{
			powered = false;
			if (isActive)
			{
				anim.SetBool(ClickableObject.paramDoAction, value: false);
				psElectricity.SetObActive(active: false);
				StopAudio();
				foreach (Ant rememberedAnt in rememberedAnts)
				{
					rememberedAnt.StopElectrolysing();
				}
				rememberedAnts.Clear();
				isActive = false;
			}
		}
		if (found_battery != hasBattery)
		{
			hasBattery = found_battery;
			UpdateBillboard();
		}
		triggerArea.ResetOverlap();
	}

	public override bool NeedsFixedUpdate()
	{
		return true;
	}

	public override void Relocate(Vector3 pos, Quaternion rot)
	{
		foreach (Ant rememberedAnt in rememberedAnts)
		{
			rememberedAnt.StopElectrolysing();
		}
		rememberedAnts.Clear();
		base.Relocate(pos, rot);
	}

	public override void Demolish()
	{
		foreach (Ant rememberedAnt in rememberedAnts)
		{
			rememberedAnt.StopElectrolysing();
		}
		rememberedAnts.Clear();
		base.Demolish();
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
		else if (rememberedAnts.Count > 0)
		{
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_ELECTROLYZER_BUSY"));
		}
		else
		{
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_ELECTROLYZER_POWERED"));
		}
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (!powered)
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_BEACON_NOPOWER"));
		}
		else if (rememberedAnts.Count > 0)
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_ELECTROLYZER_BUSY"));
		}
		else
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_ELECTROLYZER_POWERED"));
		}
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
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}
}
