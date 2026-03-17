using UnityEngine;

public class BlueprintBuilding
{
	public Vector2 pos;

	public float rot;

	public string code;

	private Blueprint blueprint;

	private BlueprintData data;

	public Building building;

	public BlueprintBuilding(Blueprint _blueprint, Building _building, Vector3 base_pos, Quaternion base_rot)
	{
		blueprint = _blueprint;
		building = _building;
		pos = (Quaternion.Inverse(base_rot) * (building.transform.position - base_pos)).XZ();
		rot = building.transform.localRotation.eulerAngles.y - base_rot.eulerAngles.y;
		code = building.data.code;
		data = new BlueprintData(blueprint);
	}

	public BlueprintBuilding(Blueprint _blueprint, Save from_save)
	{
		blueprint = _blueprint;
		Read(from_save);
	}

	public void StoreData()
	{
		data.Store(building);
	}

	public void SetBuilding(Building building)
	{
		this.building = building;
	}

	public void RetrieveData(Building building)
	{
		data.Retrieve(building);
	}

	public Split GetSplit(int nr)
	{
		return building.GetBuildingSplit(nr);
	}

	public void Write(Save save)
	{
		save.Write(pos);
		save.Write(rot);
		save.Write(code);
		data.SaveToFile(save);
	}

	private void Read(Save save)
	{
		pos = save.ReadVector2();
		rot = save.ReadFloat();
		code = save.ReadString();
		data = new BlueprintData(blueprint);
		data.LoadFromFile(save);
	}
}
