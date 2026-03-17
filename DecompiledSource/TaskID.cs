public struct TaskID
{
	private int typ;

	private string instinctCode;

	private Building building;

	public static TaskID Nup()
	{
		return new TaskID
		{
			typ = 1,
			instinctCode = null,
			building = null
		};
	}

	public static TaskID Instinct(Task _task)
	{
		return new TaskID
		{
			typ = 2,
			instinctCode = _task?.code,
			building = null
		};
	}

	public static TaskID Building(Building _building)
	{
		return new TaskID
		{
			typ = 3,
			instinctCode = null,
			building = _building
		};
	}

	public override bool Equals(object obj)
	{
		return Equals((TaskID)obj);
	}

	public override int GetHashCode()
	{
		int num = typ;
		if (instinctCode != null)
		{
			num ^= instinctCode.GetHashCode();
		}
		if (building != null)
		{
			num ^= building.GetHashCode();
		}
		return num;
	}

	public static bool operator ==(TaskID a, TaskID b)
	{
		if (a.typ != b.typ)
		{
			return false;
		}
		switch (a.typ)
		{
		case 1:
			return true;
		case 2:
			if (a.instinctCode != null && b.instinctCode != null)
			{
				return a.instinctCode == b.instinctCode;
			}
			return false;
		case 3:
			if (a.building != null && b.building != null)
			{
				return a.building == b.building;
			}
			return false;
		default:
			return false;
		}
	}

	public static bool operator !=(TaskID a, TaskID b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		return typ switch
		{
			1 => "Nup", 
			2 => "Instinct " + ((instinctCode == null) ? "Null" : instinctCode), 
			3 => "Building " + ((building == null) ? "Null" : building.data.code), 
			_ => "None", 
		};
	}

	public static TaskID Read(Save save)
	{
		int num = save.ReadInt();
		string text = ((num == 2) ? save.ReadString() : null);
		Building building = ((num == 3) ? GameManager.instance.FindLink<Building>(save.ReadInt()) : null);
		return new TaskID
		{
			typ = num,
			instinctCode = text,
			building = building
		};
	}

	public void Write(Save save)
	{
		save.Write(typ);
		if (typ == 2)
		{
			save.Write((instinctCode == null) ? "" : instinctCode);
		}
		if (typ == 3)
		{
			save.Write((!(building == null)) ? building.linkId : 0);
		}
	}
}
