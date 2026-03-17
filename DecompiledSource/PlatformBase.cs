using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class PlatformBase : KoroutineBehaviour
{
	protected bool inited;

	private float updateGynesFlownSoon;

	private List<Achievement> achievementsAchieved = new List<Achievement>();

	public bool hasWorkshop;

	public abstract IEnumerator KInit(Action<string> callback, Action<float> func_progress);

	public abstract void Outit();

	public abstract string GetUserName();

	public virtual ulong GetUserId()
	{
		return 0uL;
	}

	public virtual string GetUserName(ulong id)
	{
		return "?";
	}

	public virtual Language GetDefaultLanguage()
	{
		Language language;
		switch (Application.systemLanguage)
		{
		case SystemLanguage.English:
			language = Language.ENGLISH;
			break;
		case SystemLanguage.French:
			language = Language.FRENCH;
			break;
		case SystemLanguage.German:
			language = Language.GERMAN;
			break;
		case SystemLanguage.Japanese:
			language = Language.JAPANESE;
			break;
		case SystemLanguage.Chinese:
		case SystemLanguage.ChineseSimplified:
		case SystemLanguage.ChineseTraditional:
			language = Language.CHINESE_SIMPLIFIED;
			break;
		case SystemLanguage.Russian:
			language = Language.RUSSIAN;
			break;
		case SystemLanguage.Dutch:
			language = Language.DUTCH;
			break;
		case SystemLanguage.Korean:
			language = Language.KOREAN;
			break;
		case SystemLanguage.Polish:
			language = Language.POLISH;
			break;
		default:
			language = Language.NONE;
			break;
		}
		Language language2 = language;
		if (!Loc.AllowedLanguage(language2))
		{
			language2 = Language.ENGLISH;
		}
		return language2;
	}

	public virtual string GetExtFileName(string filename)
	{
		return Path.Combine(Application.streamingAssetsPath, filename);
	}

	public virtual string GetPlayerFileDir()
	{
		return Directory.GetParent(Application.dataPath).FullName;
	}

	public string GetBlueprintsDir()
	{
		string text = Path.Combine(GetPlayerFileDir(), "Blueprints");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public void UpdateGynesFlown()
	{
		if (!WorldSettings.sandbox)
		{
			updateGynesFlownSoon = 5f;
		}
	}

	protected virtual void UpdateGynesFlownReal(int v)
	{
	}

	public virtual bool GetGlobalStats(Action<PlatformGlobalStats> callback_result)
	{
		return false;
	}

	public void GainAchievement(Achievement achievement)
	{
		if (achievement != Achievement.None && !(GameManager.instance == null) && !GameManager.instance.theater && Platform.GetGameType() == GameType.FullGame && !WorldSettings.sandbox && !achievementsAchieved.Contains(achievement))
		{
			GainAchievementReal(achievement);
			achievementsAchieved.Add(achievement);
		}
	}

	protected virtual void GainAchievementReal(Achievement achievement)
	{
	}

	private void Update()
	{
		if (inited)
		{
			Process(Time.deltaTime);
		}
	}

	protected virtual void Process(float dt)
	{
		if (!(updateGynesFlownSoon > 0f))
		{
			return;
		}
		updateGynesFlownSoon -= dt;
		if (!(updateGynesFlownSoon <= 0f))
		{
			return;
		}
		int num = 0;
		foreach (NuptialFlightData item in NuptialFlight.EFlightData())
		{
			foreach (KeyValuePair<AntCaste, int> dicFlownGyne in item.dicFlownGynes)
			{
				num += dicFlownGyne.Value;
			}
		}
		Playthrough playthrough = Player.GetPlaythrough(Progress.playthroughId);
		if (playthrough != null && playthrough.UpdateGynesFlown(num))
		{
			int totalGynesFlown = Player.GetTotalGynesFlown();
			UpdateGynesFlownReal(totalGynesFlown);
			if (totalGynesFlown >= 50)
			{
				GainAchievement(Achievement.GYNE_50);
			}
		}
	}

	public virtual IEnumerable<string> ESubscribedBlueprintPaths()
	{
		yield break;
	}

	public virtual bool UploadBlueprint(Blueprint blueprint)
	{
		Debug.LogError("UploadBlueprint not supported for this platform");
		return false;
	}

	public virtual void RemoveUploadedBlueprint(Blueprint blueprint)
	{
		Debug.LogError("RemoveUploadedBlueprint not supported for this platform");
	}

	public virtual void UnsubscribeBlueprint(Blueprint blueprint)
	{
		Debug.LogError("UnsubscribeBlueprint not supported for this platform");
	}

	public virtual bool BlueprintIsNotUploaded(Blueprint blueprint)
	{
		Debug.LogError("BlueprintIsNotUploaded not supported for this platform");
		return false;
	}

	public virtual bool IsUploadActive()
	{
		return false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (inited)
		{
			Outit();
		}
	}
}
