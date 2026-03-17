using System.Collections.Generic;
using UnityEngine;

public static class NuptialFlight
{
	private static List<NuptialFlightData> nuptialFlightData = new List<NuptialFlightData>();

	private static int currentFlight = -1;

	private static List<float> nupFly_spawnTimes = new List<float>();

	private static int spawnedDrones;

	private static List<NuptialFlightActor> nupFly_spawns = new List<NuptialFlightActor>();

	public static void Write(Save save)
	{
		save.Write(nuptialFlightData.Count);
		foreach (NuptialFlightData nuptialFlightDatum in nuptialFlightData)
		{
			nuptialFlightDatum.Write(save);
		}
		save.Write(spawnedDrones);
		save.Write(nupFly_spawns.Count);
		foreach (NuptialFlightActor nupFly_spawn in nupFly_spawns)
		{
			save.Write((int)nupFly_spawn.caste);
			nupFly_spawn.Write(save);
		}
	}

	public static void Read(Save save)
	{
		NuptialFlight.nuptialFlightData.Clear();
		int num;
		if (save.version >= 59)
		{
			num = save.ReadInt();
			for (int i = 0; i < num; i++)
			{
				NuptialFlightData nuptialFlightData = new NuptialFlightData();
				nuptialFlightData.Read(save);
				NuptialFlight.nuptialFlightData.Add(nuptialFlightData);
			}
		}
		if (save.version < 59)
		{
			save.ReadFloat();
			save.ReadFloat();
			save.ReadInt();
		}
		spawnedDrones = save.ReadInt();
		List<NuptialFlightActor> list = new List<NuptialFlightActor>();
		List<NuptialFlightActor> list2 = new List<NuptialFlightActor>();
		num = save.ReadInt();
		for (int j = 0; j < num; j++)
		{
			AntCaste antCaste = AntCaste.DRONE;
			if (save.version >= 57)
			{
				antCaste = (AntCaste)save.ReadInt();
			}
			NuptialFlightActor nuptialFlightActor = GameManager.instance.SpawnNuptialFlightActor(antCaste);
			if (save.version >= 69)
			{
				nupFly_spawns.Add(nuptialFlightActor);
			}
			nuptialFlightActor.Read(save);
			nuptialFlightActor.Init();
			if (save.version < 69)
			{
				nuptialFlightActor.Delete();
				continue;
			}
			if (nuptialFlightActor.GetMode() == NuptialFlightMode.FOLLOWING)
			{
				list.Add(nuptialFlightActor);
			}
			if (antCaste == AntCaste.GYNE || antCaste == AntCaste.GYNE_T2 || antCaste == AntCaste.GYNE_T3)
			{
				list2.Add(nuptialFlightActor);
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		foreach (NuptialFlightActor item in list)
		{
			NuptialFlightActor target = list2[Random.Range(0, list2.Count)];
			item.InitFollowing(target);
		}
	}

	public static void Update(float dt)
	{
		if (currentFlight == -1)
		{
			UpdateCurrentData();
		}
		NuptialFlightData nuptialFlightData = GetCurrentFlight();
		double gameTime = GameManager.instance.gameTime;
		switch (nuptialFlightData.stage)
		{
		case NuptialFlightStage.WAITING:
			if (gameTime > nuptialFlightData.timeStart)
			{
				StartFlight();
			}
			break;
		case NuptialFlightStage.WARM_UP:
			UpdateActors(dt);
			if (gameTime > nuptialFlightData.timeStart + (double)GlobalValues.standard.nuptialFlightWarmUp)
			{
				AudioManager.PlayMusicExtra(UISfx.NuptialFlightTrack);
				nuptialFlightData.SetStage(NuptialFlightStage.ACTIVE);
			}
			break;
		case NuptialFlightStage.ACTIVE:
			SpawnActors();
			UpdateActors(dt);
			if (!(gameTime > nuptialFlightData.timeStart + (double)(GlobalValues.standard.nuptialFlightWarmUp + GlobalValues.standard.nuptialFlightDuration)))
			{
				break;
			}
			foreach (KeyValuePair<AntCaste, int> dicFlownGyne in nuptialFlightData.dicFlownGynes)
			{
				switch (dicFlownGyne.Key)
				{
				case AntCaste.GYNE:
					Progress.AddInventorPoints(InventorPoints.GYNE_T1, dicFlownGyne.Value, preview: false);
					break;
				case AntCaste.GYNE_T2:
					Progress.AddInventorPoints(InventorPoints.GYNE_T2, dicFlownGyne.Value, preview: false);
					break;
				case AntCaste.GYNE_T3:
					Progress.AddInventorPoints(InventorPoints.GYNE_T3, dicFlownGyne.Value, preview: false);
					break;
				}
			}
			foreach (NuptialFlightActor nupFly_spawn in nupFly_spawns)
			{
				switch (nupFly_spawn.caste)
				{
				case AntCaste.GYNE:
					UIGame.instance.StartCurrencyAnimation(InventorPoints.GYNE_T1, 1, nupFly_spawn.transform);
					break;
				case AntCaste.GYNE_T2:
					UIGame.instance.StartCurrencyAnimation(InventorPoints.GYNE_T2, 1, nupFly_spawn.transform);
					break;
				case AntCaste.GYNE_T3:
					UIGame.instance.StartCurrencyAnimation(InventorPoints.GYNE_T3, 1, nupFly_spawn.transform);
					break;
				}
			}
			Platform.current.GainAchievement(Achievement.FIRST_NUPTIAL);
			GameManager.instance.ShowReportScreenDelayed(currentFlight);
			nuptialFlightData.SetStage(NuptialFlightStage.FLY_OFF);
			break;
		case NuptialFlightStage.FLY_OFF:
			UpdateActors(dt);
			if (gameTime > nuptialFlightData.timeStart + (double)(GlobalValues.standard.nuptialFlightWarmUp + GlobalValues.standard.nuptialFlightDuration + GlobalValues.standard.nuptialFlightFlyOff))
			{
				ClearActors();
				nuptialFlightData.SetStage(NuptialFlightStage.DONE);
			}
			break;
		case NuptialFlightStage.DONE:
			UpdateCurrentData();
			break;
		case NuptialFlightStage.NONE:
			break;
		}
	}

	public static void FixedUpdate(float xdt)
	{
		if (nupFly_spawns.Count <= 0)
		{
			return;
		}
		foreach (NuptialFlightActor nupFly_spawn in nupFly_spawns)
		{
			nupFly_spawn.DoFixedUpdate(xdt);
		}
	}

	private static void UpdateCurrentData()
	{
		currentFlight = -1;
		for (int i = 0; i < nuptialFlightData.Count; i++)
		{
			if (nuptialFlightData[i].stage != NuptialFlightStage.DONE)
			{
				currentFlight = i;
				break;
			}
		}
		if (currentFlight == -1)
		{
			nuptialFlightData.Add(new NuptialFlightData());
			currentFlight = nuptialFlightData.Count - 1;
		}
	}

	public static void StartFlight()
	{
		NuptialFlightData nuptialFlightData = GetCurrentFlight();
		if (nuptialFlightData.stage != NuptialFlightStage.NONE && nuptialFlightData.stage != NuptialFlightStage.WAITING)
		{
			return;
		}
		ClearActors();
		nuptialFlightData.SetStage(NuptialFlightStage.WARM_UP);
		nuptialFlightData.timeStart = GameManager.instance.gameTime;
		AudioManager.PlayUI(UISfx.NuptialFlightIntro);
		foreach (Building item in GameManager.instance.EBuildings())
		{
			if (item is GyneTower gyneTower)
			{
				gyneTower.StartGyne();
			}
		}
		NuptialFlightData nuptialFlightData2 = new NuptialFlightData();
		nuptialFlightData2.timeStart = GameManager.instance.gameTime + (double)GlobalValues.standard.nuptialFlightSeasonLength;
		nuptialFlightData2.SetStage(NuptialFlightStage.WAITING);
		NuptialFlight.nuptialFlightData.Add(nuptialFlightData2);
	}

	private static void SpawnActors()
	{
		if (spawnedDrones < GetCurrentFlight().nDrones)
		{
			float num = GlobalValues.standard.nuptialFlightDuration / (float)GetCurrentFlight().nDrones;
			if (GameManager.instance.gameTime > GetCurrentFlight().timeStart + (double)(GlobalValues.standard.nuptialFlightWarmUp + num * (float)spawnedDrones))
			{
				List<AntCaste> castes = GlobalValues.standard.nuptialFlightLevels[Progress.GetNuptialFlightLevel()].castes;
				NuptialFlightActor nuptialFlightActor = SpawnActor(castes[Random.Range(0, castes.Count)]);
				spawnedDrones++;
				Vector3 position = CamController.instance.transform.position;
				Vector2 vector = new Vector2(300f, 600f);
				Vector2 vector2 = Random.insideUnitCircle.normalized * Random.Range(vector.x, vector.y);
				Vector2 vector3 = Random.insideUnitCircle.normalized * Random.Range(vector.x, vector.y);
				Vector3 start = new Vector3(position.x + vector2.x, Random.Range(GlobalValues.standard.nuptialFlightHeightRange.x, GlobalValues.standard.nuptialFlightHeightRange.y), position.z + vector2.y);
				Vector3 end = new Vector3(position.x + vector3.x, Random.Range(GlobalValues.standard.nuptialFlightHeightRange.x, GlobalValues.standard.nuptialFlightHeightRange.y), position.z + vector3.y);
				nuptialFlightActor.InitStraightLine(start, end);
			}
		}
	}

	private static void UpdateActors(float dt)
	{
		foreach (NuptialFlightActor item in new List<NuptialFlightActor>(nupFly_spawns))
		{
			item.DoUpdate(dt);
			if (item.FlyingDone() && !item.IsVisible())
			{
				nupFly_spawns.Remove(item);
				item.Delete();
			}
		}
	}

	public static NuptialFlightActor SpawnActor(AntCaste _caste)
	{
		NuptialFlightActor nuptialFlightActor = GameManager.instance.SpawnNuptialFlightActor(_caste);
		nupFly_spawns.Add(nuptialFlightActor);
		return nuptialFlightActor;
	}

	public static void ClearActors()
	{
		spawnedDrones = 0;
		nupFly_spawnTimes.Clear();
		foreach (NuptialFlightActor item in new List<NuptialFlightActor>(nupFly_spawns))
		{
			if (item != null)
			{
				item.Delete();
			}
		}
		nupFly_spawns.Clear();
	}

	public static void Clear()
	{
		ClearActors();
		currentFlight = -1;
		nuptialFlightData.Clear();
	}

	public static bool IsNuptialFlightActive()
	{
		float progress;
		return IsNuptialFlightActive(out progress);
	}

	public static bool IsNuptialFlightActive(out float progress)
	{
		NuptialFlightData nuptialFlightData = GetCurrentFlight();
		if (nuptialFlightData == null)
		{
			progress = 0f;
			return false;
		}
		progress = nuptialFlightData.GetProgress();
		if (progress == 0f || progress == 1f)
		{
			return false;
		}
		return true;
	}

	public static int GetSeenNuptialFlights()
	{
		int num = 0;
		foreach (NuptialFlightData nuptialFlightDatum in nuptialFlightData)
		{
			if (nuptialFlightDatum.stage >= NuptialFlightStage.ACTIVE)
			{
				num++;
			}
		}
		return num;
	}

	public static NuptialFlightStage GetCurrentStage()
	{
		return GetCurrentFlight().stage;
	}

	public static NuptialFlightData GetCurrentFlight()
	{
		if (currentFlight < 0)
		{
			return null;
		}
		return GetFlightData(currentFlight);
	}

	public static NuptialFlightData GetNextFlight()
	{
		if (currentFlight >= nuptialFlightData.Count - 1)
		{
			return null;
		}
		return GetFlightData(currentFlight + 1);
	}

	public static NuptialFlightData GetFlightData(int i)
	{
		return nuptialFlightData[i];
	}

	public static IEnumerable<NuptialFlightData> EFlightData()
	{
		foreach (NuptialFlightData nuptialFlightDatum in nuptialFlightData)
		{
			yield return nuptialFlightDatum;
		}
	}

	public static void AddGyneFlown(AntCaste _gyne)
	{
		Dictionary<AntCaste, int> dicFlownGynes = GetCurrentFlight().dicFlownGynes;
		if (!dicFlownGynes.ContainsKey(_gyne))
		{
			dicFlownGynes.Add(_gyne, 0);
		}
		dicFlownGynes[_gyne]++;
		switch (_gyne)
		{
		case AntCaste.GYNE:
			Platform.current.GainAchievement(Achievement.GYNE_T1);
			break;
		case AntCaste.GYNE_T2:
			Platform.current.GainAchievement(Achievement.GYNE_T2);
			break;
		case AntCaste.GYNE_T3:
			Platform.current.GainAchievement(Achievement.GYNE_T3);
			break;
		}
		Platform.current.UpdateGynesFlown();
	}

	public static void AddDroneAttracted()
	{
		GetCurrentFlight().nDronesAttracted++;
	}
}
