using System.IO;
using UnityEngine;

public class BlueprintData : ISaveContainer
{
	private Blueprint blueprint;

	private byte[] data;

	private BinaryWriter writer;

	private BinaryReader reader;

	public int version => blueprint.version;

	public int GetVersion()
	{
		return blueprint.version;
	}

	public SaveType GetSaveType()
	{
		return SaveType.Blueprint;
	}

	public BlueprintData(Blueprint _blueprint)
	{
		blueprint = _blueprint;
	}

	public bool IsEmpty()
	{
		if (data != null)
		{
			return data.Length == 0;
		}
		return true;
	}

	public void Store(Building building)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (writer = new BinaryWriter(memoryStream))
		{
			building.Write(this);
		}
		memoryStream.Flush();
		data = memoryStream.GetBuffer();
	}

	public void Retrieve(Building building)
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		using (reader = new BinaryReader(memoryStream))
		{
			building.Read(this);
		}
		memoryStream.Flush();
	}

	public void Store(TrailGate trail_gate)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (writer = new BinaryWriter(memoryStream))
		{
			trail_gate.WriteConfig(this);
		}
		memoryStream.Flush();
		data = memoryStream.GetBuffer();
	}

	public void Retrieve(TrailGate trail_gate)
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		using (reader = new BinaryReader(memoryStream))
		{
			trail_gate.ReadConfig(this);
		}
		memoryStream.Flush();
	}

	public void SaveToFile(Save save)
	{
		save.Write(data);
	}

	public void LoadFromFile(Save save)
	{
		data = save.ReadData();
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
		writer.Write(blueprint.GetBuildingId(building));
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
		return new BuildingLink(blueprint.GetBuilding(ReadInt()));
	}
}
