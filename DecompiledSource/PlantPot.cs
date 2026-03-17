using System.Collections.Generic;
using UnityEngine;

public class PlantPot : Building
{
	[Header("Plant Pot")]
	public Transform[] growPoints;

	private List<PlantPot_Spot> plantSpots;

	public override void Write(Save save)
	{
		base.Write(save);
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			plantSpot.Write(save);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		MakePlantSpots();
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			plantSpot.Read(save);
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		MakePlantSpots();
	}

	private void MakePlantSpots()
	{
		if (plantSpots == null)
		{
			plantSpots = new List<PlantPot_Spot>();
			Transform[] array = growPoints;
			foreach (Transform grow_point in array)
			{
				plantSpots.Add(new PlantPot_Spot(grow_point));
			}
		}
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		bool flag = true;
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			if (!plantSpot.IsPlanted())
			{
				flag = false;
			}
		}
		if (flag)
		{
			return false;
		}
		return _type.IsCategory(PickupCategory.SEED);
	}

	protected override void PrepareForPickup_Intake(Pickup _pickup, ExchangePoint _point)
	{
		base.PrepareForPickup_Intake(_pickup, _point);
		List<PlantPot_Spot> list = new List<PlantPot_Spot>();
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			if (!plantSpot.IsPlanted())
			{
				list.Add(plantSpot);
			}
		}
		list[Random.Range(0, list.Count)].Plant(_pickup);
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(_pickup, point);
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			if (plantSpot.seed == _pickup)
			{
				plantSpot.StartGrow(50f * Random.Range(0.7f, 1.3f));
			}
		}
		_pickup.Delete();
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		string text = "";
		int num = 0;
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			if (!plantSpot.IsPlanted())
			{
				num++;
			}
		}
		if (num > 0)
		{
			text = ((num != 1) ? (text + "Room for " + num + " seeds") : (text + "Room for 1 seed"));
		}
		bool flag = false;
		foreach (PlantPot_Spot plantSpot2 in plantSpots)
		{
			if (plantSpot2.IsPlanted() && plantSpot2.IsGrowing())
			{
				if (!flag)
				{
					flag = true;
					text += "\n";
				}
				text += "\n";
				text = text + "Time until grown: " + plantSpot2.GetTimeLeftGrowing().Unit(PhysUnit.TIME_MINUTES);
			}
		}
		ui_hover.UpdateInfo(text);
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld)
		{
			return;
		}
		foreach (PlantPot_Spot plantSpot in plantSpots)
		{
			plantSpot.SpotUpdate(dt);
		}
	}
}
