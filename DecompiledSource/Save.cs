using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class Save : ISaveContainer
{
	private const bool USE_COMPRESSION = false;

	public const int VERSION = 94;

	private FileStream fileStream;

	private GZipStream zipStream;

	private BinaryWriter writer;

	private BinaryReader reader;

	public const string saveNameBusy = "_busy";

	public const string saveNamePreload = "_preload";

	public int version;

	public string fileName { get; private set; }

	public int GetVersion()
	{
		return version;
	}

	public SaveType GetSaveType()
	{
		return SaveType.GameSave;
	}

	public Save()
	{
		version = 94;
	}

	public void StartWriting(string filename)
	{
		fileName = filename;
		fileStream = File.Create(Files.GameSave("_busy", bg: false));
		writer = new BinaryWriter(fileStream);
		Write(version);
	}

	public void DoneWriting(bool success)
	{
		writer?.Dispose();
		fileStream?.Dispose();
		if (success)
		{
			string text = Files.GameSave("_busy", bg: false);
			File.Copy(text, fileName, overwrite: true);
			File.Delete(text);
		}
	}

	public void StartReading(string filename)
	{
		fileName = filename;
		fileStream = File.Open(filename, FileMode.Open);
		reader = new BinaryReader(fileStream);
		version = ReadInt();
	}

	public void DoneReading()
	{
		reader?.Dispose();
		fileStream?.Dispose();
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

	public void Write(ushort s)
	{
		writer.Write(s);
	}

	public void Write(ulong ul)
	{
		writer.Write(ul);
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

	public void WriteYRot(Quaternion q)
	{
		writer.Write(q.eulerAngles.y);
	}

	public void Write(DateTime dt)
	{
		writer.Write(dt.Year);
		writer.Write(dt.Month);
		writer.Write(dt.Day);
		writer.Write(dt.Hour);
		writer.Write(dt.Minute);
		writer.Write(dt.Second);
	}

	public void Write(Guid guid)
	{
		Write(guid.ToByteArray());
	}

	public void Write(byte[] data)
	{
		if (data == null)
		{
			writer.Write(0);
			return;
		}
		writer.Write(data.Length);
		if (data.Length != 0)
		{
			writer.Write(data);
		}
	}

	public void Write(Building b)
	{
		writer.Write((!(b == null)) ? b.linkId : 0);
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

	public ushort ReadUShort()
	{
		return reader.ReadUInt16();
	}

	public ulong ReadULong()
	{
		return reader.ReadUInt64();
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

	public Quaternion ReadYRot()
	{
		return Quaternion.Euler(0f, reader.ReadSingle(), 0f);
	}

	public DateTime ReadDateTime()
	{
		int year = reader.ReadInt32();
		int month = reader.ReadInt32();
		int day = reader.ReadInt32();
		int hour = reader.ReadInt32();
		int minute = reader.ReadInt32();
		int second = reader.ReadInt32();
		return new DateTime(year, month, day, hour, minute, second);
	}

	public Guid ReadGuid()
	{
		return new Guid(ReadData());
	}

	public byte[] ReadData()
	{
		int num = reader.ReadInt32();
		if (num == 0)
		{
			return new byte[0];
		}
		return reader.ReadBytes(num);
	}

	public BuildingLink ReadBuilding()
	{
		return new BuildingLink(reader.ReadInt32());
	}

	public void DebugLoad(string str)
	{
		if (ReadInt() == 28)
		{
			Debug.Log("Load check ok - " + str);
		}
		else
		{
			Debug.LogError("Load check ERROR - " + str);
		}
	}

	public void DebugSave()
	{
		Write(28);
	}
}
