using System.Collections.Generic;
using UnityEngine;

public class FruitPlant : Plant
{
	public Vector2 growTimeRange = new Vector2(2f, 20f);

	public float rotTime = 120f;

	private FruitPlant_Spot[] fruitSpots;

	private int nFruitSpots;

	protected bool foragablePickupsChanged;

	private List<PickupType> cachedForagablePickups = new List<PickupType>();

	private List<(float, float, int)> fruitSpot_storedData = new List<(float, float, int)>();

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(nFruitSpots);
		for (int i = 0; i < nFruitSpots; i++)
		{
			FruitPlant_Spot fruitPlant_Spot = fruitSpots[i];
			save.Write(fruitPlant_Spot.growTime);
			save.Write(fruitPlant_Spot.timeLeft);
			save.Write((!(fruitPlant_Spot.fruit == null)) ? fruitPlant_Spot.fruit.linkId : 0);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		int num = save.ReadInt();
		fruitSpot_storedData = new List<(float, float, int)>();
		for (int i = 0; i < num; i++)
		{
			fruitSpot_storedData.Add((save.ReadFloat(), save.ReadFloat(), save.ReadInt()));
		}
	}

	public override void LoadLinkPickups()
	{
		for (int i = 0; i < fruitSpot_storedData.Count; i++)
		{
			fruitSpots[i].growTime = fruitSpot_storedData[i].Item1;
			fruitSpots[i].timeLeft = fruitSpot_storedData[i].Item2;
			Pickup pickup = GameManager.instance.FindLink<Pickup>(fruitSpot_storedData[i].Item3);
			if (pickup != null)
			{
				pickup.SetStatus(PickupStatus.IN_CONTAINER, fruitSpots[i].growPoint);
				fruitSpots[i].fruit = pickup;
			}
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (GameManager.instance == null)
		{
			nFruitSpots = 0;
			fruitSpots = new FruitPlant_Spot[0];
		}
		else
		{
			nFruitSpots = currentMesh.growPoints.Count;
			fruitSpots = new FruitPlant_Spot[nFruitSpots];
			int num = 0;
			foreach (Transform growPoint in currentMesh.growPoints)
			{
				fruitSpots[num++] = new FruitPlant_Spot(growPoint);
			}
		}
		if (!during_load)
		{
			FruitPlant_Spot[] array = fruitSpots;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].growTime = Random.Range(0f, GrowTimerRoll());
			}
		}
	}

	public override void UpdateGrowWilt(float dt)
	{
		base.UpdateGrowWilt(dt);
		if (state != PlantState.Grown)
		{
			return;
		}
		for (int i = 0; i < nFruitSpots; i++)
		{
			FruitPlant_Spot fruitPlant_Spot = fruitSpots[i];
			if (fruitPlant_Spot.HasFruit())
			{
				fruitPlant_Spot.timeLeft -= dt;
				if (fruitPlant_Spot.timeLeft <= 0f)
				{
					fruitPlant_Spot.fruit.Delete();
					fruitPlant_Spot.fruit = null;
					fruitPlant_Spot.growTime = GrowTimerRoll();
				}
				continue;
			}
			fruitPlant_Spot.growTime = Mathf.Max(fruitPlant_Spot.growTime - dt, 0f);
			if (fruitPlant_Spot.growTime <= 0f)
			{
				fruitPlant_Spot.fruit = GameManager.instance.SpawnPickup(data.fruit, fruitPlant_Spot.growPoint.position, fruitPlant_Spot.growPoint.rotation);
				fruitPlant_Spot.fruit.SetStatus(PickupStatus.IN_CONTAINER, fruitPlant_Spot.growPoint);
				fruitPlant_Spot.timeLeft = rotTime * Random.Range(0.8f, 1.2f);
				foragablePickupsChanged = true;
			}
		}
	}

	public override void SetState(PlantState new_state)
	{
		base.SetState(new_state);
		foragablePickupsChanged = true;
	}

	protected override void DoDelete()
	{
		for (int i = 0; i < nFruitSpots; i++)
		{
			FruitPlant_Spot fruitPlant_Spot = fruitSpots[i];
			if (fruitPlant_Spot.fruit != null)
			{
				fruitPlant_Spot.fruit.Delete();
			}
		}
		base.DoDelete();
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange == ExchangeType.FORAGE)
		{
			if (state != PlantState.Grown)
			{
				return false;
			}
			foreach (FruitPlant_Spot item in ESpotsWithFruit())
			{
				if (item.HasFruit())
				{
					return true;
				}
			}
			return false;
		}
		return base.CanExtract(exchange, ref let_ant_wait, show_billboard: false);
	}

	public override List<PickupType> GetExtractablePickups(ExchangeType exchange)
	{
		if (exchange == ExchangeType.FORAGE)
		{
			if (state != PlantState.Grown)
			{
				return ConnectableObject.emptyPickupList;
			}
			if (foragablePickupsChanged)
			{
				cachedForagablePickups.Clear();
				foreach (FruitPlant_Spot item in ESpotsWithFruit())
				{
					if (!cachedForagablePickups.Contains(item.fruit.type))
					{
						cachedForagablePickups.Add(item.fruit.type);
					}
				}
				foragablePickupsChanged = false;
			}
			return cachedForagablePickups;
		}
		return base.GetExtractablePickups(exchange);
	}

	public override bool HasExtractablePickup(ExchangeType exchange, PickupType pickup)
	{
		if (exchange == ExchangeType.FORAGE && state == PlantState.Grown)
		{
			foreach (FruitPlant_Spot item in ESpotsWithFruit())
			{
				if (item.fruit.type == pickup)
				{
					return true;
				}
			}
		}
		return base.HasExtractablePickup(exchange, pickup);
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		if (_type == data.fruit && state == PlantState.Grown)
		{
			List<FruitPlant_Spot> list = new List<FruitPlant_Spot>();
			foreach (FruitPlant_Spot item in ESpotsWithFruit())
			{
				if (item.fruit.type == _type)
				{
					list.Add(item);
				}
			}
			FruitPlant_Spot fruitPlant_Spot = list[Random.Range(0, list.Count)];
			Pickup fruit = fruitPlant_Spot.fruit;
			fruitPlant_Spot.fruit = null;
			foragablePickupsChanged = true;
			fruitPlant_Spot.growTime = GrowTimerRoll();
			return fruit;
		}
		return base.ExtractPickup(_type);
	}

	public IEnumerable<FruitPlant_Spot> ESpotsWithFruit()
	{
		if (state != PlantState.Grown)
		{
			yield break;
		}
		for (int i = 0; i < nFruitSpots; i++)
		{
			FruitPlant_Spot fruitPlant_Spot = fruitSpots[i];
			if (fruitPlant_Spot.HasFruit())
			{
				yield return fruitPlant_Spot;
			}
		}
	}

	public float GrowTimerRoll()
	{
		return Random.Range(growTimeRange.x, growTimeRange.y);
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI(ui_hover);
		if (nFruitSpots > 0)
		{
			ui_hover.SetSlots();
		}
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
		if (nFruitSpots <= 0 || state != PlantState.Grown)
		{
			return;
		}
		List<PickupType> list = new List<PickupType>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < nFruitSpots; i++)
		{
			FruitPlant_Spot fruitPlant_Spot = fruitSpots[i];
			if (fruitPlant_Spot.fruit == null)
			{
				list.Add(PickupType.NONE);
				list2.Add(fruitPlant_Spot.growTime.Unit(PhysUnit.TIME));
			}
			else
			{
				list.Add(fruitPlant_Spot.fruit.type);
				list2.Add("");
			}
		}
		ui_hover.UpdateSlots(Loc.GetUI("BIOME_FRUITS"), nFruitSpots, list, list2);
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_BiomeObject uIClickLayout_BiomeObject = (UIClickLayout_BiomeObject)ui_click;
		if (nFruitSpots > 0)
		{
			uIClickLayout_BiomeObject.SetSlots(target: true);
		}
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		base.UpdateClickUi(ui_click);
		UIClickLayout_BiomeObject uIClickLayout_BiomeObject = (UIClickLayout_BiomeObject)ui_click;
		if (nFruitSpots <= 0 || state != PlantState.Grown)
		{
			return;
		}
		List<PickupType> list = new List<PickupType>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < nFruitSpots; i++)
		{
			FruitPlant_Spot fruitPlant_Spot = fruitSpots[i];
			if (fruitPlant_Spot.fruit == null)
			{
				list.Add(PickupType.NONE);
				list2.Add(fruitPlant_Spot.growTime.Unit(PhysUnit.TIME));
			}
			else
			{
				list.Add(fruitPlant_Spot.fruit.type);
				list2.Add("");
			}
		}
		uIClickLayout_BiomeObject.UpdateSlots(Loc.GetUI("BIOME_FRUITS"), nFruitSpots, list, list2);
	}
}
