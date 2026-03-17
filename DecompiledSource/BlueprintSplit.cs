using UnityEngine;

public class BlueprintSplit
{
	public Vector3 pos;

	private int buildingId;

	private int buildingSplitNr;

	private Blueprint blueprint;

	public Split split;

	public BlueprintSplit(Blueprint _blueprint, Split _split, Vector3 base_pos, Quaternion base_rot)
	{
		blueprint = _blueprint;
		split = _split;
		buildingId = (buildingSplitNr = -1);
		pos = Quaternion.Inverse(base_rot) * (split.transform.position - base_pos);
	}

	public BlueprintSplit(Blueprint _blueprint, Vector3 split_pos, Vector3 base_pos, Quaternion base_rot)
	{
		blueprint = _blueprint;
		split = null;
		buildingId = (buildingSplitNr = -1);
		pos = Quaternion.Inverse(base_rot) * (split_pos - base_pos);
	}

	public BlueprintSplit(Blueprint _blueprint, Save from_save)
	{
		blueprint = _blueprint;
		Read(from_save);
	}

	public void LinkToBuildingSplit(Building building)
	{
		buildingId = blueprint.GetBuildingId(building);
		buildingSplitNr = building.GetBuildingSplitNr(split);
		if (buildingId == -1 || buildingSplitNr == -1)
		{
			Debug.LogWarning("BlueprintSplit.LinkToBuildingSplit: couldn't get link");
		}
	}

	public Split FindBuildingSplit()
	{
		if (buildingId == -1 || buildingSplitNr == -1)
		{
			return null;
		}
		Building building = blueprint.GetBuilding(buildingId);
		if (building == null)
		{
			Debug.LogError($"BlueprintSplit.FindBuildingSplit: couldn't find building {buildingId}");
			return null;
		}
		Split buildingSplit = building.GetBuildingSplit(buildingSplitNr);
		if (buildingSplit == null)
		{
			Debug.LogError($"BlueprintSplit.FindBuildingSplit: couldn't find split {buildingSplitNr}");
			return null;
		}
		return buildingSplit;
	}

	public void Write(Save save)
	{
		save.Write(pos);
		save.Write(buildingId);
		if (buildingId != -1)
		{
			save.Write(buildingSplitNr);
		}
	}

	private void Read(Save save)
	{
		pos = save.ReadVector3();
		buildingId = save.ReadInt();
		buildingSplitNr = ((buildingId == -1) ? (-1) : save.ReadInt());
	}
}
