public struct BuildingLink
{
	public int id;

	public Building building;

	public bool postpone;

	public BuildingLink(int _id)
	{
		id = _id;
		building = null;
		postpone = true;
	}

	public BuildingLink(Building _building)
	{
		id = 0;
		building = _building;
		postpone = false;
	}
}
