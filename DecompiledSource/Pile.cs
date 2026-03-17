using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Pile
{
	public Transform pilePos;

	public PileType pileType = PileType.INPUT;

	public int maxHeight;

	public ExchangePoint assignedExchangePoint;

	private List<Pickup> piledPickups;

	[NonSerialized]
	public PickupType pickuptype;

	public Pile(Transform _pos, PileType _type, int _height)
	{
		pilePos = _pos;
		pileType = _type;
		maxHeight = _height;
	}

	public void Init()
	{
		piledPickups = new List<Pickup>();
	}

	public void Write(Save save)
	{
		save.Write(piledPickups.Count);
		foreach (Pickup piledPickup in piledPickups)
		{
			save.Write(piledPickup.linkId);
		}
	}

	public static void Read(Save save, out List<int> _links)
	{
		int num = save.ReadInt();
		if (num == 0)
		{
			_links = null;
			return;
		}
		_links = new List<int>();
		for (int i = 0; i < num; i++)
		{
			_links.Add(save.ReadInt());
		}
	}

	public void ReservePile(PickupType _type)
	{
		pickuptype = _type;
	}

	public void AddToPile(Pickup p, ref bool extractable_pickups_changed, bool during_load = false)
	{
		if (piledPickups.Count == 0)
		{
			pickuptype = p.type;
			extractable_pickups_changed = true;
		}
		piledPickups.Add(p);
		p.SetStatus(PickupStatus.IN_CONTAINER, pilePos);
		if (!during_load)
		{
			p.transform.position = GetPos(p);
			p.transform.rotation = pilePos.rotation;
		}
	}

	public Vector3 GetPos(Pickup p)
	{
		if (piledPickups.Contains(p))
		{
			Vector3 position = pilePos.transform.position;
			position.x += UnityEngine.Random.Range(-0.1f, 0.1f);
			position.y += (float)piledPickups.IndexOf(p) * p.height;
			position.z += UnityEngine.Random.Range(-0.1f, 0.1f);
			return position;
		}
		Debug.LogError("Height asked for pickup not present in pile.");
		return Vector3.zero;
	}

	public Vector3 GetTopPos(Pickup pickup)
	{
		Vector3 position = pilePos.transform.position;
		position.y += (float)piledPickups.Count * pickup.height;
		return position;
	}

	public Pickup TakeFromPile(ref bool extractable_pickups_changed)
	{
		Pickup pickup = piledPickups[^1];
		piledPickups.Remove(pickup);
		if (piledPickups.Count == 0)
		{
			pickuptype = PickupType.NONE;
			extractable_pickups_changed = true;
		}
		return pickup;
	}

	public bool IsFull()
	{
		return piledPickups.Count >= maxHeight;
	}

	public bool IsEmpty()
	{
		if (maxHeight > 0)
		{
			return piledPickups.Count == 0;
		}
		return false;
	}

	public int GetHeight()
	{
		return piledPickups.Count;
	}

	public void DeleteAll()
	{
		if (piledPickups == null)
		{
			return;
		}
		foreach (Pickup piledPickup in piledPickups)
		{
			piledPickup.Delete();
		}
	}

	public List<Pickup> GetPickups()
	{
		return piledPickups;
	}
}
