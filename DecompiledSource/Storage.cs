using System.Collections.Generic;
using UnityEngine;

public class Storage : Building
{
	[Header("Storage")]
	public List<Pile> piles;

	private List<List<int>> pilePickups_storedData;

	private static Dictionary<string, Vector3Int> dic_spaceInPiles = new Dictionary<string, Vector3Int>();

	public override void Write(Save save)
	{
		base.Write(save);
		foreach (Pile pile in piles)
		{
			pile.Write(save);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		pilePickups_storedData = new List<List<int>>();
		if (HasPiles())
		{
			for (int i = 0; i < piles.Count; i++)
			{
				piles[i].Init();
				Pile.Read(save, out var _links);
				pilePickups_storedData.Add(_links);
			}
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (during_load)
		{
			for (int i = 0; i < piles.Count; i++)
			{
				if (pilePickups_storedData == null || pilePickups_storedData.Count <= i || pilePickups_storedData[i] == null)
				{
					continue;
				}
				bool extractable_pickups_changed = false;
				for (int j = 0; j < pilePickups_storedData[i].Count; j++)
				{
					Pickup pickup = GameManager.instance.FindLink<Pickup>(pilePickups_storedData[i][j]);
					if (pickup == null)
					{
						Debug.LogError("Pickup returned null while loading, shouldn't happen");
					}
					else
					{
						piles[i].AddToPile(pickup, ref extractable_pickups_changed, during_load: true);
					}
				}
			}
			return;
		}
		foreach (Pile pile in piles)
		{
			pile.Init();
		}
	}

	protected override void DoDelete()
	{
		foreach (Pile pile in piles)
		{
			pile.DeleteAll();
		}
		base.DoDelete();
		GameManager.instance.CountPickupInventory();
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		int n;
		if (exchange == ExchangeType.BUILDING_IN)
		{
			return HasSpaceLeft(_type, PileType.NONE, point, out n);
		}
		return false;
	}

	protected override void OnPickupArrival_Intake(Pickup p, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(p, point);
		if (HasPiles())
		{
			AddToPiles(p, PileType.NONE, point);
		}
		else
		{
			p.Delete();
		}
		GameManager.instance.UpdatePickupInventory();
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_OUT || !HasPiles())
		{
			return base.CanExtract(exchange, ref let_ant_wait, show_billboard);
		}
		foreach (Pile pile in piles)
		{
			if (!pile.IsEmpty())
			{
				return true;
			}
		}
		return false;
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
		if (!HasPiles())
		{
			return ConnectableObject.emptyPickupList;
		}
		List<PickupType> list = new List<PickupType>();
		foreach (Pile pile in piles)
		{
			if (!pile.IsEmpty() && !list.Contains(pile.pickuptype))
			{
				list.Add(pile.pickuptype);
			}
		}
		return list;
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		Pickup pickup;
		if (HasPiles())
		{
			RemovePickup(_type, 1, BuildingStatus.COMPLETED);
			pickup = TakeFromPiles(_type, PileType.NONE);
			if (pickup == null)
			{
				Debug.LogError(base.name + ": Tried taking pickup " + _type.ToString() + " from pile but returned null");
				pickup = GameManager.instance.SpawnPickup(_type);
			}
		}
		else
		{
			pickup = base.ExtractPickup(_type);
		}
		GameManager.instance.CountPickupInventory();
		return pickup;
	}

	public override void DropPickups(PickupType _type, int n = 1, bool try_inventory = false)
	{
		if (!HasPiles())
		{
			base.DropPickups(_type, n);
			return;
		}
		if (GetCollectedAmount(_type, BuildingStatus.COMPLETED, include_incoming: false) == 0)
		{
			Debug.LogWarning(base.name + ": Tried to drop amount " + n + " of pickup " + _type.ToString() + " while not enough collected, shouldn't happen");
			return;
		}
		PlayAudioShort(AudioManager.GetDropSfx(_type));
		for (int i = 0; i < n; i++)
		{
			Pickup p = TakeFromPiles(_type, PileType.NONE);
			RemovePickup(_type, 1, BuildingStatus.COMPLETED);
			if (!try_inventory || !GameManager.instance.TryExchangePickupToInventory(ground, base.transform.position, p))
			{
				DropPickup(p);
			}
		}
	}

	protected bool HasPiles(PileType pile_type = PileType.NONE)
	{
		if (pile_type == PileType.NONE)
		{
			return piles.Count > 0;
		}
		foreach (Pile pile in piles)
		{
			if (pile.pileType == pile_type)
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool HasSpaceLeft(PickupType pickup_type, PileType pile_type, ExchangePoint assigned_point, out int n)
	{
		if (!HasPiles(pile_type))
		{
			n = int.MaxValue;
			return true;
		}
		n = 0;
		foreach (Pile pile in GetPiles(pickup_type, pile_type, with_content: false, space_left: false))
		{
			if (assigned_point != null && pile.assignedExchangePoint != null)
			{
				if (pile.assignedExchangePoint == assigned_point)
				{
					n += pile.maxHeight - pile.GetHeight();
				}
			}
			else
			{
				n += pile.maxHeight - pile.GetHeight();
			}
		}
		foreach (Pickup item in incomingPickups_intake)
		{
			if (item.type == pickup_type)
			{
				n--;
			}
		}
		n = Mathf.Clamp(n, 0, int.MaxValue);
		return n > 0;
	}

	protected void AddToPiles(Pickup p, PileType pile_type = PileType.NONE, ExchangePoint point = null, bool content_priority = false)
	{
		if (point != null)
		{
			foreach (Pile pile in piles)
			{
				if (pile.assignedExchangePoint == point)
				{
					pile.AddToPile(p, ref extractablePickupsChanged);
					return;
				}
			}
		}
		List<Pile> list = GetPiles(p.type, pile_type, with_content: false, space_left: true, content_priority);
		int count = list.Count;
		if (count > 0)
		{
			int index = ((p.pileSelection == 0) ? Random.Range(0, count) : (p.pileSelection % count));
			list[index].AddToPile(p, ref extractablePickupsChanged);
		}
		else
		{
			Debug.LogError(base.name + ": Tried adding pickup to piles while no space left, shouldn't happen.");
		}
	}

	public override Vector3 GetInsertPos(Pickup pickup = null)
	{
		if (pickup == null)
		{
			return base.GetInsertPos((Pickup)null);
		}
		int num = 0;
		List<Pile> list = null;
		if (piles.Count > 1)
		{
			list = GetPiles(pickup.type, PileType.INPUT, with_content: false, space_left: true);
			num = list.Count;
		}
		if (num < 2)
		{
			pickup.pileSelection = 0;
			return base.GetInsertPos((Pickup)null);
		}
		pickup.pileSelection = Random.Range(1, 10000);
		return list[pickup.pileSelection % num].GetTopPos(pickup);
	}

	protected Pickup TakeFromPiles(PickupType pickup_type, PileType pile_type)
	{
		List<Pile> list = GetPiles(pickup_type, pile_type, with_content: true, space_left: false);
		if (list.Count == 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)].TakeFromPile(ref extractablePickupsChanged);
	}

	protected List<Pile> GetPiles(PickupType pickup_type, PileType pile_type, bool with_content, bool space_left, bool content_priority = false)
	{
		if (piles.Count == 0)
		{
			Debug.LogError("Tried to get pile on building without piles, shouldn't happen");
			return null;
		}
		List<Pile> list = new List<Pile>();
		List<Pile> list2 = new List<Pile>();
		for (int i = 0; i < piles.Count; i++)
		{
			Pile pile = piles[i];
			bool flag = pile.IsEmpty();
			bool flag2 = pile.IsFull();
			if ((pile_type == PileType.NONE || pile.pileType == pile_type || pile.pileType == PileType.NONE) && (pickup_type == PickupType.ANY || pile.pickuptype == PickupType.NONE || pile.pickuptype == pickup_type) && !(with_content && flag) && !(space_left && flag2))
			{
				if (content_priority && flag)
				{
					list2.Add(pile);
				}
				else
				{
					list.Add(pile);
				}
			}
		}
		if (content_priority && list.Count == 0 && list2.Count > 0)
		{
			list.AddRange(list2);
		}
		return list;
	}

	public int GetSpaceInPiles(PileType _type)
	{
		if (!dic_spaceInPiles.ContainsKey(data.code))
		{
			Vector3Int zero = Vector3Int.zero;
			foreach (Pile pile in piles)
			{
				switch (pile.pileType)
				{
				case PileType.NONE:
					zero.x += pile.maxHeight;
					break;
				case PileType.INPUT:
					zero.y += pile.maxHeight;
					break;
				case PileType.OUTPUT:
					zero.z += pile.maxHeight;
					break;
				}
			}
			dic_spaceInPiles.Add(data.code, zero);
		}
		return _type switch
		{
			PileType.NONE => dic_spaceInPiles[data.code].x, 
			PileType.INPUT => dic_spaceInPiles[data.code].y, 
			PileType.OUTPUT => dic_spaceInPiles[data.code].z, 
			_ => 0, 
		};
	}
}
