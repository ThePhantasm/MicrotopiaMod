using UnityEngine;

public static class Platform
{
	public static PlatformBase current;

	public static void Select()
	{
		GameObject gameObject = new GameObject("Platform");
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(gameObject);
		}
		current = gameObject.AddComponent<PlatformSteam>();
	}

	public static GameType GetGameType()
	{
		if (DebugSettings.standard.prologue)
		{
			return GameType.Prologue;
		}
		if (DebugSettings.standard.demo)
		{
			return GameType.Demo;
		}
		if (DebugSettings.standard.playtest)
		{
			return GameType.PlayTest;
		}
		return GameType.FullGame;
	}

	public static bool CanLoad(GameType game_type, int version)
	{
		if (version > 94)
		{
			return false;
		}
		GameType gameType = GetGameType();
		switch (game_type)
		{
		case GameType.NotSet:
		case GameType.Unknown:
			return true;
		case GameType.Demo:
		case GameType.Prologue:
		case GameType.PlayTest:
			return true;
		case GameType.FullGame:
			return gameType == GameType.FullGame;
		default:
			Debug.LogError($"Platform.CanLoad: don't know {game_type}");
			return true;
		}
	}
}
