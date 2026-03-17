using UnityEngine;

public interface ISaveContainer
{
	int GetVersion();

	SaveType GetSaveType();

	void Write(int i);

	void Write(float f);

	void Write(byte b);

	void Write(Vector3 v);

	void Write(Vector2 v);

	void Write(string str);

	void Write(bool b);

	void Write(Building b);

	int ReadInt();

	float ReadFloat();

	byte ReadByte();

	Vector3 ReadVector3();

	Vector2 ReadVector2();

	string ReadString();

	bool ReadBool();

	BuildingLink ReadBuilding();
}
