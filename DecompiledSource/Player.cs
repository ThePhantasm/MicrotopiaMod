using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class Player
{
	public static Language language;

	public static float globalVolume;

	public static float musicVolume;

	public static float uiVolume;

	public static float worldVolume;

	public static float uiScale;

	public static float renderScale;

	public static string lastSave;

	public static bool freeCameraAngle;

	public static bool disableTrailDragging;

	public static bool invertCamera;

	public static bool crossIslandBuilding;

	public static int fpsLimit;

	public static int vSyncCount;

	public static bool lessFog;

	public static BuildingRotationSetting buildRotMode;

	public static float timeBetweenAutoSaves;

	public static int nextAutoSaveNumber;

	public static float remainingAutoSaveTime;

	public const bool cheats = false;

	public static bool cheatFreeLarvae;

	public static bool cheatFreeBuildings;

	public static bool cheatEnableAllBuildings;

	public static bool cheatDeletableEverything;

	public static bool cheatImmortalAnts;

	public static bool cheatFreeTechTree;

	public static bool cheatFreeObjectives;

	public static bool cheatShowAllTutorials;

	public static bool cheatShowFullInventory;

	public static List<Tutorial> seenTutorials = new List<Tutorial>();

	public static List<Notice> seenNotices = new List<Notice>();

	private static List<Playthrough> playthroughs = new List<Playthrough>();

	public static void Reset()
	{
		language = Platform.current.GetDefaultLanguage();
		globalVolume = 0.7f;
		musicVolume = 1f;
		uiVolume = 1f;
		worldVolume = 1f;
		uiScale = 1f;
		renderScale = 1f;
		vSyncCount = 1;
		fpsLimit = -1;
		lessFog = false;
		buildRotMode = BuildingRotationSetting.YES;
		invertCamera = false;
		lastSave = "";
		UISettings.SetScreenDefault();
		timeBetweenAutoSaves = 600f;
		nextAutoSaveNumber = 1;
		ResetRemainingAutoSaveTime();
		freeCameraAngle = false;
		crossIslandBuilding = false;
		ResetCheats();
	}

	public static void ResetCheats()
	{
		cheatFreeLarvae = (cheatFreeBuildings = (cheatEnableAllBuildings = (cheatDeletableEverything = (cheatImmortalAnts = (cheatFreeTechTree = (cheatFreeObjectives = (cheatShowAllTutorials = (cheatShowFullInventory = false))))))));
	}

	public static void Save()
	{
		Save save = new Save();
		bool flag = false;
		try
		{
			save.StartWriting(Files.PlayerSave());
			save.Write(DateTime.Now);
			save.Write((int)language);
			save.Write(globalVolume);
			save.Write(musicVolume);
			save.Write(uiVolume);
			save.Write(worldVolume);
			save.Write((int)Screen.fullScreenMode);
			save.Write(Screen.width);
			save.Write(Screen.height);
			save.Write(uiScale);
			save.Write(renderScale);
			save.Write(vSyncCount);
			save.Write(fpsLimit);
			save.Write(lessFog);
			save.Write(invertCamera);
			save.Write((int)buildRotMode);
			save.Write(b: false);
			save.Write(freeCameraAngle);
			save.Write(timeBetweenAutoSaves);
			save.Write(nextAutoSaveNumber);
			ResetRemainingAutoSaveTime();
			InputManager.WriteConfig(save);
			save.Write(CheatsToInt());
			save.Write(lastSave);
			save.Write(seenTutorials.Count);
			foreach (Tutorial seenTutorial in seenTutorials)
			{
				save.Write((int)seenTutorial);
			}
			save.Write(seenNotices.Count);
			foreach (Notice seenNotice in seenNotices)
			{
				save.Write((int)seenNotice);
			}
			save.Write(playthroughs.Count);
			foreach (Playthrough playthrough in playthroughs)
			{
				playthrough.Write(save);
			}
			flag = true;
		}
		finally
		{
			save.DoneWriting(flag);
			if (flag)
			{
				Debug.Log("Saved player data to " + save.fileName);
			}
		}
	}

	public static IEnumerator KLoad(KoroutineBehaviour caller, Action<bool> callback_success)
	{
		bool success = false;
		Save save = null;
		KoroutineBehaviour.KoroutineId kid = caller.SetFinalizer(delegate
		{
			if (save != null)
			{
				save.DoneReading();
				if (success)
				{
					Debug.Log("Loaded player data from " + save.fileName);
				}
			}
			callback_success(success);
		});
		try
		{
			string text = Files.PlayerSave();
			if (!File.Exists(text))
			{
				Debug.LogWarning("Couldn't find '" + text + "', reset player data");
				Reset();
				success = true;
				yield break;
			}
			save = new Save();
			save.StartReading(text);
			int num = Toolkit.SaveVersion();
			if (save.version < 3 || save.version > num)
			{
				Debug.LogWarning($"Unexpected player save version ({save.version}, while my version is {num}), resetting");
				save.DoneReading();
				Reset();
				success = true;
				yield break;
			}
			save.ReadDateTime();
			language = (Language)save.ReadInt();
			if (!Loc.AllowedLanguage(language))
			{
				language = Platform.current.GetDefaultLanguage();
			}
			globalVolume = ((save.version >= 46) ? save.ReadFloat() : 1f);
			musicVolume = save.ReadFloat();
			uiVolume = save.ReadFloat();
			worldVolume = ((save.version >= 72) ? save.ReadFloat() : uiVolume);
			FullScreenMode fs_mode = (FullScreenMode)save.ReadInt();
			int w = save.ReadInt();
			int h = save.ReadInt();
			uiScale = save.ReadFloat();
			if (uiScale < 0.4f)
			{
				Debug.LogWarning($"uiScale = {uiScale}; setting to default");
				uiScale = 1f;
			}
			SetRenderScale((save.version < 71) ? 1f : save.ReadFloat());
			int sync_count = ((save.version < 72) ? 1 : save.ReadInt());
			int fps_limit = ((save.version < 72) ? (-1) : save.ReadInt());
			SetFps(sync_count, fps_limit);
			lessFog = save.version >= 73 && save.ReadBool();
			invertCamera = save.version >= 72 && save.ReadBool();
			buildRotMode = ((save.version >= 85) ? ((BuildingRotationSetting)save.ReadInt()) : BuildingRotationSetting.YES);
			if (save.version >= 4)
			{
				save.ReadBool();
				freeCameraAngle = save.ReadBool();
			}
			if (save.version >= 32)
			{
				timeBetweenAutoSaves = save.ReadFloat();
				if (timeBetweenAutoSaves > 0f && timeBetweenAutoSaves < 59f)
				{
					Debug.LogWarning($"timeBetweenAutoSaves = {timeBetweenAutoSaves}; setting to default");
					timeBetweenAutoSaves = 600f;
				}
				nextAutoSaveNumber = save.ReadInt();
				ResetRemainingAutoSaveTime();
			}
			else
			{
				timeBetweenAutoSaves = 600f;
				nextAutoSaveNumber = 1;
			}
			if (save.version >= 27)
			{
				InputManager.ReadConfig(save);
			}
			if (save.version >= 15)
			{
				save.ReadInt();
			}
			lastSave = save.ReadString();
			if (save.version >= 41)
			{
				seenTutorials.Clear();
				int num2 = save.ReadInt();
				for (int num3 = 0; num3 < num2; num3++)
				{
					seenTutorials.Add((Tutorial)save.ReadInt());
				}
			}
			if (save.version >= 66)
			{
				seenNotices.Clear();
				int num4 = save.ReadInt();
				for (int num5 = 0; num5 < num4; num5++)
				{
					seenNotices.Add((Notice)save.ReadInt());
				}
			}
			if (save.version >= 70)
			{
				playthroughs.Clear();
				int num6 = save.ReadInt();
				for (int num7 = 0; num7 < num6; num7++)
				{
					playthroughs.Add(new Playthrough(save));
				}
			}
			if (CommandLine.overrideResolution)
			{
				UISettings.SetScreen(CommandLine.fullScreenMode, CommandLine.screenWidth, CommandLine.screenHeight);
			}
			else
			{
				UISettings.SetScreen(fs_mode, w, h);
			}
			UIGlobal.ApplyCanvasScale();
			success = true;
		}
		finally
		{
			caller.StopKoroutine(kid);
		}
	}

	public static void SetRenderScale(float v)
	{
		renderScale = Mathf.Clamp(v, 0.5f, 2f);
		UniversalRenderPipelineAsset universalRenderPipelineAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
		if (universalRenderPipelineAsset != null)
		{
			universalRenderPipelineAsset.renderScale = v;
		}
	}

	public static void SetFps(int sync_count, int fps_limit)
	{
		QualitySettings.vSyncCount = (vSyncCount = UISettings.AllowedVsyncValue(sync_count));
		Application.targetFrameRate = (fpsLimit = UISettings.AllowedFpsLimitValue(fps_limit));
	}

	private static int CheatsToInt()
	{
		int num = 0;
		if (cheatFreeLarvae)
		{
			num |= 1;
		}
		if (cheatFreeBuildings)
		{
			num |= 2;
		}
		if (cheatEnableAllBuildings)
		{
			num |= 4;
		}
		if (cheatDeletableEverything)
		{
			num |= 8;
		}
		if (cheatImmortalAnts)
		{
			num |= 0x10;
		}
		if (cheatFreeTechTree)
		{
			num |= 0x20;
		}
		if (cheatFreeObjectives)
		{
			num |= 0x40;
		}
		if (cheatShowAllTutorials)
		{
			num |= 0x80;
		}
		if (cheatShowFullInventory)
		{
			num |= 0x100;
		}
		return num;
	}

	private static void FillCheatsFromInt(int i, int version)
	{
		cheatFreeLarvae = (i & 1) != 0;
		cheatFreeBuildings = (i & 2) != 0;
		cheatEnableAllBuildings = (i & 4) != 0;
		cheatDeletableEverything = (i & 8) != 0;
		cheatImmortalAnts = (i & 0x10) != 0;
		if (version >= 50)
		{
			cheatFreeTechTree = (i & 0x20) != 0;
			cheatFreeObjectives = (i & 0x40) != 0;
			cheatShowAllTutorials = (i & 0x80) != 0;
			cheatShowFullInventory = (i & 0x100) != 0;
		}
	}

	public static bool IsLastSaveValid()
	{
		if (string.IsNullOrWhiteSpace(lastSave))
		{
			return false;
		}
		string path = Files.GameSave(lastSave, bg: false);
		if (!File.Exists(path))
		{
			return false;
		}
		UILoadSave.ReadFileShortData(path, out var version, out var _, out var _, out var game_type);
		if (!Platform.CanLoad(game_type, version))
		{
			return false;
		}
		return true;
	}

	public static void ResetRemainingAutoSaveTime()
	{
		if (timeBetweenAutoSaves > 0f)
		{
			remainingAutoSaveTime = timeBetweenAutoSaves;
		}
	}

	public static bool HasSeenTutorial(Tutorial tut)
	{
		return seenTutorials.Contains(tut);
	}

	public static void SetSeenTutorial(Tutorial tut)
	{
		if (!seenTutorials.Contains(tut))
		{
			seenTutorials.Add(tut);
		}
	}

	public static bool HasSeenNotice(Notice not)
	{
		return seenNotices.Contains(not);
	}

	public static void SetNotice(Notice not)
	{
		if (!seenNotices.Contains(not))
		{
			seenNotices.Add(not);
		}
	}

	public static Guid StartNewPlaythrough(bool debug_start)
	{
		Playthrough playthrough = new Playthrough(debug_start);
		playthroughs.Add(playthrough);
		return playthrough.guid;
	}

	public static Playthrough GetPlaythrough(Guid guid)
	{
		foreach (Playthrough playthrough in playthroughs)
		{
			if (playthrough.guid == guid)
			{
				return playthrough;
			}
		}
		Debug.LogWarning($"Couldn't find playthrough {guid}");
		return null;
	}

	public static int GetTotalGynesFlown()
	{
		int num = 0;
		foreach (Playthrough playthrough in playthroughs)
		{
			num += playthrough.gynesFlown;
		}
		return num;
	}
}
