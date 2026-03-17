using System.Collections.Generic;
using UnityEngine;

public class NuptialFlightData
{
	public NuptialFlightStage stage;

	public double timeStart;

	public int nDrones;

	public int nDronesAttracted;

	public Dictionary<AntCaste, int> dicFlownGynes = new Dictionary<AntCaste, int>();

	public NuptialFlightData()
	{
		stage = NuptialFlightStage.NONE;
		timeStart = 0.0;
		nDrones = Mathf.RoundToInt((float)Random.Range(GlobalValues.standard.nuptialFlightCountRange.x, GlobalValues.standard.nuptialFlightCountRange.y) * GlobalValues.standard.nuptialFlightLevels[Progress.GetNuptialFlightLevel()].busyness);
	}

	public void Write(Save save)
	{
		save.Write((int)stage);
		save.Write((float)timeStart);
		save.Write(nDrones);
		save.Write(nDronesAttracted);
		save.Write(dicFlownGynes.Count);
		foreach (KeyValuePair<AntCaste, int> dicFlownGyne in dicFlownGynes)
		{
			save.Write((int)dicFlownGyne.Key);
			save.Write(dicFlownGyne.Value);
		}
	}

	public void Read(Save save)
	{
		stage = (NuptialFlightStage)save.ReadInt();
		timeStart = save.ReadFloat();
		nDrones = save.ReadInt();
		dicFlownGynes = new Dictionary<AntCaste, int>();
		if (save.version >= 60 && save.version < 65)
		{
			int num = save.ReadInt();
			if (num > 0)
			{
				dicFlownGynes.Add(AntCaste.GYNE, num);
			}
		}
		nDronesAttracted = save.ReadInt();
		if (save.version >= 65)
		{
			int num2 = save.ReadInt();
			for (int i = 0; i < num2; i++)
			{
				dicFlownGynes.Add((AntCaste)save.ReadInt(), save.ReadInt());
			}
		}
	}

	public void SetStage(NuptialFlightStage _stage)
	{
		stage = _stage;
	}

	public float GetProgress()
	{
		switch (stage)
		{
		case NuptialFlightStage.NONE:
		case NuptialFlightStage.WAITING:
			return 0f;
		case NuptialFlightStage.WARM_UP:
		case NuptialFlightStage.ACTIVE:
			return (float)(GameManager.instance.gameTime - timeStart) / (GlobalValues.standard.nuptialFlightWarmUp + GlobalValues.standard.nuptialFlightDuration);
		case NuptialFlightStage.FLY_OFF:
		case NuptialFlightStage.DONE:
			return 1f;
		default:
			return 0f;
		}
	}

	public float GetNGynesFlown(bool rounded = false)
	{
		float num = 0f;
		foreach (KeyValuePair<AntCaste, int> dicFlownGyne in dicFlownGynes)
		{
			num += (float)dicFlownGyne.Value;
		}
		if (rounded)
		{
			return num * Mathf.Ceil(GetProgress());
		}
		return num * GetProgress();
	}

	public double GetEndTime()
	{
		return timeStart + (double)(GlobalValues.standard.nuptialFlightWarmUp + GlobalValues.standard.nuptialFlightDuration);
	}
}
