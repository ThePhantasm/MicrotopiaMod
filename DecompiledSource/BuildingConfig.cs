using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuildingConfig : ISaveContainer
{
	private static BuildingConfig clipboard;

	private static string clipboardBuildingCode;

	private List<Building> buildingRefs = new List<Building>();

	private byte[] data;

	private string dispenserAttached;

	private BinaryWriter writer;

	private BinaryReader reader;

	public int GetVersion()
	{
		return 94;
	}

	public SaveType GetSaveType()
	{
		return SaveType.CopyConfig;
	}

	public static void ClearClipboard()
	{
		clipboard = null;
	}

	public static BuildingConfig GetConfig(Building building)
	{
		BuildingConfig buildingConfig = new BuildingConfig();
		buildingConfig.CopyFrom(building);
		return buildingConfig;
	}

	private void CopyFrom(Building building)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (writer = new BinaryWriter(memoryStream))
		{
			building.WriteConfig(this);
			dispenserAttached = null;
			if (building.buildingAttachPoints.Count > 0 && building.buildingAttachPoints[0].HasDispenser(out var dis))
			{
				dispenserAttached = dis.data.code;
				dis.WriteConfig(this);
			}
		}
		memoryStream.Flush();
		data = memoryStream.GetBuffer();
	}

	public void ApplyTo(Building building)
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		using (reader = new BinaryReader(memoryStream))
		{
			building.ReadConfig(this);
			if (dispenserAttached != null)
			{
				Building building2 = BuildingEditing.SpawnBuilding(dispenserAttached);
				BuildingAttachPoint buildingAttachPoint = building.buildingAttachPoints[0];
				building2.transform.SetPositionAndRotation(buildingAttachPoint.GetPosition(), buildingAttachPoint.GetRotation());
				building2.PlaceBuilding();
				building2.ReadConfig(this);
				building.SetAttachment(building2, buildingAttachPoint);
			}
		}
		memoryStream.Flush();
	}

	public static bool CanPasteClipboard(Building building)
	{
		if (clipboard == null)
		{
			return false;
		}
		string code = building.data.code;
		if (code.StartsWith("STOCKPILE") && clipboardBuildingCode.StartsWith("STOCKPILE"))
		{
			return true;
		}
		return code == clipboardBuildingCode;
	}

	public static bool CopyToClipboard(Building building)
	{
		if (!building.CanCopySettings())
		{
			return false;
		}
		clipboard = GetConfig(building);
		clipboardBuildingCode = building.data.code;
		return true;
	}

	public static bool PasteClipboard(Building building)
	{
		if (!CanPasteClipboard(building))
		{
			return false;
		}
		clipboard.ApplyTo(building);
		return true;
	}

	public void Write(int i)
	{
		writer.Write(i);
	}

	public void Write(float f)
	{
		writer.Write(f);
	}

	public void Write(byte b)
	{
		writer.Write(b);
	}

	public void Write(Vector3 v)
	{
		writer.Write(v.x);
		writer.Write(v.y);
		writer.Write(v.z);
	}

	public void Write(Vector2 v)
	{
		writer.Write(v.x);
		writer.Write(v.y);
	}

	public void Write(string str)
	{
		writer.Write(str);
	}

	public void Write(bool b)
	{
		writer.Write(b);
	}

	public void Write(Building building)
	{
		int count = buildingRefs.Count;
		buildingRefs.Add(building);
		writer.Write(count);
	}

	public int ReadInt()
	{
		return reader.ReadInt32();
	}

	public float ReadFloat()
	{
		return reader.ReadSingle();
	}

	public byte ReadByte()
	{
		return reader.ReadByte();
	}

	public Vector3 ReadVector3()
	{
		return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	public Vector2 ReadVector2()
	{
		return new Vector2(reader.ReadSingle(), reader.ReadSingle());
	}

	public string ReadString()
	{
		return reader.ReadString();
	}

	public bool ReadBool()
	{
		return reader.ReadBoolean();
	}

	public BuildingLink ReadBuilding()
	{
		int num = ReadInt();
		if (num < 0 || num >= buildingRefs.Count)
		{
			return new BuildingLink(null);
		}
		return new BuildingLink(buildingRefs[num]);
	}
}
