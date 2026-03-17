using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Blueprint
{
	public List<BlueprintBuilding> buildings = new List<BlueprintBuilding>();

	public List<BlueprintSplit> splits = new List<BlueprintSplit>();

	public List<BlueprintTrail> trails = new List<BlueprintTrail>();

	public int version;

	public string code;

	public string name;

	public DateTime creationDate;

	public DateTime uploadDate;

	public bool completelyLoaded;

	public ulong publishId;

	public ulong creatorId;

	public string localPath;

	public bool locked;

	public string missingComponents;

	public string description;

	public List<string> buildingsNeeded = new List<string>();

	public List<TrailType> trailTypesNeeded = new List<TrailType>();

	public Texture2D iconTexture;

	public Sprite iconSprite;

	private Vector3 basePos;

	private Quaternion baseRot;

	public Blueprint(Building main_building, Vector3 center)
	{
		version = 94;
		basePos = center;
		baseRot = main_building.transform.localRotation;
	}

	private Blueprint(Save from_save, bool only_header)
	{
		version = from_save.version;
		Read(from_save, only_header);
	}

	public void AddBuilding(Building building)
	{
		buildings.Add(new BlueprintBuilding(this, building, basePos, baseRot));
		if (!buildingsNeeded.Contains(building.data.code))
		{
			buildingsNeeded.Add(building.data.code);
		}
	}

	public void RetrieveData(Building b)
	{
		foreach (BlueprintBuilding building in buildings)
		{
			if (building.building == b)
			{
				building.RetrieveData(b);
				return;
			}
		}
		Debug.LogError("Couldn't find blueprint data for building " + b.transform.DebugName());
	}

	public int GetBuildingId(Building building)
	{
		if (building == null)
		{
			return -1;
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			if (buildings[i].building == building)
			{
				return i;
			}
		}
		return -1;
	}

	public Building GetBuilding(int id)
	{
		if (id < 0 || id >= buildings.Count)
		{
			return null;
		}
		return buildings[id].building;
	}

	public int AddSplit(Split split)
	{
		splits.Add(new BlueprintSplit(this, split, basePos, baseRot));
		return splits.Count - 1;
	}

	public int AddSplit(Vector3 pos)
	{
		splits.Add(new BlueprintSplit(this, pos, basePos, baseRot));
		return splits.Count - 1;
	}

	public BlueprintTrail AddTrail(int split_id_start, int split_id_end, TrailType trail_type)
	{
		trails.Add(new BlueprintTrail(this, split_id_start, split_id_end, trail_type));
		if (!trailTypesNeeded.Contains(trail_type))
		{
			trailTypesNeeded.Add(trail_type);
		}
		return trails[^1];
	}

	public Split GetSplit(int id)
	{
		if (id < 0 || id >= splits.Count)
		{
			return null;
		}
		return splits[id].split;
	}

	private BlueprintSplit GetBlueprintSplit(int id)
	{
		if (id < 0 || id >= splits.Count)
		{
			return null;
		}
		return splits[id];
	}

	public List<(Vector3, Vector3, TrailType)> GetTrailHoverData()
	{
		List<(Vector3, Vector3, TrailType)> list = new List<(Vector3, Vector3, TrailType)>();
		foreach (BlueprintTrail trail in trails)
		{
			BlueprintSplit blueprintSplit = GetBlueprintSplit(trail.splitIdStart);
			BlueprintSplit blueprintSplit2 = GetBlueprintSplit(trail.splitIdEnd);
			if (blueprintSplit != null && blueprintSplit2 != null)
			{
				list.Add((blueprintSplit.pos, blueprintSplit2.pos, trail.trailType));
			}
		}
		return list;
	}

	public void SaveToFile()
	{
		string filename = Files.BlueprintFile(this, ensure_path: true);
		Save save = new Save();
		bool flag = false;
		try
		{
			save.StartWriting(filename);
			Write(save);
			flag = true;
		}
		finally
		{
			save.DoneWriting(flag);
			if (flag)
			{
				Debug.Log("Saved blueprint " + save.fileName);
			}
		}
	}

	public static Blueprint LoadLocalFromCode(string code, bool only_header)
	{
		return LoadFromPath(Files.LocalBlueprintPath(code), code, only_header);
	}

	public static Blueprint LoadFromPath(string path, string code, bool only_header)
	{
		string text = Files.BlueprintFile(path, code);
		if (!File.Exists(text))
		{
			Debug.LogError("File doesn't exist: " + text);
			return null;
		}
		Save save = new Save();
		save.StartReading(text);
		int num = Toolkit.SaveVersion();
		if (save.version > num)
		{
			Debug.LogWarning($"Unexpected player save version ({save.version}, while my version is {num}), resetting");
			return null;
		}
		Blueprint obj = new Blueprint(save, only_header)
		{
			localPath = path,
			code = code
		};
		save.DoneReading();
		string path2 = Files.BlueprintImage(obj);
		Texture2D texture2D = null;
		if (File.Exists(path2))
		{
			byte[] data = File.ReadAllBytes(path2);
			texture2D = new Texture2D(2, 2);
			texture2D.LoadImage(data);
		}
		obj.SetIcon(texture2D);
		obj.completelyLoaded = true;
		return obj;
	}

	public void SetIcon(Texture2D tex)
	{
		iconTexture = tex;
		iconSprite = ((tex == null) ? null : Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2((float)tex.width / 2f, (float)tex.height / 2f)));
	}

	private void Write(Save save)
	{
		save.Write(name);
		save.Write(creationDate);
		save.Write(description);
		save.Write(buildingsNeeded.Count);
		foreach (string item in buildingsNeeded)
		{
			save.Write(item);
		}
		save.Write(trailTypesNeeded.Count);
		foreach (TrailType item2 in trailTypesNeeded)
		{
			save.Write((int)item2);
		}
		save.Write(creatorId);
		save.Write(publishId);
		save.Write(uploadDate);
		save.Write(buildings.Count);
		foreach (BlueprintBuilding building in buildings)
		{
			building.Write(save);
		}
		save.Write(splits.Count);
		foreach (BlueprintSplit split in splits)
		{
			split.Write(save);
		}
		save.Write(trails.Count);
		foreach (BlueprintTrail trail in trails)
		{
			trail.Write(save);
		}
	}

	private void Read(Save save, bool only_header)
	{
		name = save.ReadString();
		creationDate = save.ReadDateTime();
		if (save.version >= 88)
		{
			description = save.ReadString();
		}
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			buildingsNeeded.Add(save.ReadString());
		}
		num = save.ReadInt();
		for (int j = 0; j < num; j++)
		{
			trailTypesNeeded.Add((TrailType)save.ReadInt());
		}
		if (save.version < 77)
		{
			creatorId = 0uL;
			publishId = 0uL;
		}
		else if (save.version < 82)
		{
			save.ReadInt();
			save.ReadULong();
			creatorId = 0uL;
			publishId = 0uL;
		}
		else
		{
			creatorId = save.ReadULong();
			publishId = save.ReadULong();
		}
		if (save.version < 89)
		{
			uploadDate = default(DateTime);
		}
		else
		{
			uploadDate = save.ReadDateTime();
		}
		if (!only_header)
		{
			num = save.ReadInt();
			for (int k = 0; k < num; k++)
			{
				buildings.Add(new BlueprintBuilding(this, save));
			}
			num = save.ReadInt();
			for (int l = 0; l < num; l++)
			{
				splits.Add(new BlueprintSplit(this, save));
			}
			num = save.ReadInt();
			for (int m = 0; m < num; m++)
			{
				trails.Add(new BlueprintTrail(this, save));
			}
		}
	}

	public void ClearTexture()
	{
		if (iconTexture != null)
		{
			UnityEngine.Object.Destroy(iconTexture);
		}
		iconTexture = null;
		iconSprite = null;
	}

	public void UpdateLocked(bool fill_missing_components = false)
	{
		if (fill_missing_components)
		{
			locked = !Progress.CheckUnlocked(this, out missingComponents);
		}
		else
		{
			locked = !Progress.CheckUnlocked(this);
		}
	}

	public BlueprintShareType GetShareType()
	{
		if (publishId == 0L)
		{
			return BlueprintShareType.Local;
		}
		if (creatorId == 0L || creatorId == Platform.current.GetUserId())
		{
			if (Platform.current.BlueprintIsNotUploaded(this) && uploadDate < DateTime.Now.AddHours(-1.0))
			{
				publishId = 0uL;
				return BlueprintShareType.Local;
			}
			return BlueprintShareType.Shared;
		}
		return BlueprintShareType.Subscribed;
	}
}
