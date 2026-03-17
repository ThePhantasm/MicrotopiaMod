using System.Collections.Generic;
using System.IO;

public static class Files
{
	public const string PLAYER_SAVE_FILENAME = "main.psav";

	public static string FodsPrefabs()
	{
		return Platform.current.GetExtFileName("prefabs.fods");
	}

	public static string FodsBiome()
	{
		return Platform.current.GetExtFileName("biome.fods");
	}

	public static string FodsSequences()
	{
		return Platform.current.GetExtFileName("sequences.fods");
	}

	public static string FodsInstinct()
	{
		return Platform.current.GetExtFileName("instinct.fods");
	}

	public static string FodsTechTree()
	{
		return Platform.current.GetExtFileName("techtree.fods");
	}

	public static string FodsLoc()
	{
		return Platform.current.GetExtFileName("loc.fods");
	}

	public static string FodsChangelog()
	{
		return Platform.current.GetExtFileName("Changelog.fods");
	}

	public static string GameSave(string name, bool bg)
	{
		if (bg)
		{
			return Platform.current.GetExtFileName(name + ".bg");
		}
		return Path.Combine(Platform.current.GetPlayerFileDir(), name + ".sav");
	}

	public static string GameSaveImage(string name)
	{
		return Path.Combine(Platform.current.GetPlayerFileDir(), name + ".png");
	}

	public static string[] GetGameSaves()
	{
		return Directory.GetFiles(Platform.current.GetPlayerFileDir(), "*.sav", SearchOption.TopDirectoryOnly);
	}

	public static string PlayerSave()
	{
		return Path.Combine(Platform.current.GetPlayerFileDir(), "main.psav");
	}

	public static string BlueprintPath(Blueprint blueprint)
	{
		return blueprint.localPath;
	}

	public static string LocalBlueprintPath(string code)
	{
		return Path.Combine(Platform.current.GetBlueprintsDir(), "BP" + code);
	}

	public static string BlueprintFile(string dir, string code, bool ensure_path = false)
	{
		if (ensure_path && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		return Path.Combine(dir, code + ".bp");
	}

	public static string BlueprintFile(Blueprint blueprint, bool ensure_path = false)
	{
		return BlueprintFile(blueprint.localPath, blueprint.code, ensure_path);
	}

	public static string BlueprintImage(Blueprint blueprint)
	{
		return Path.Combine(BlueprintPath(blueprint), blueprint.code + ".bpi");
	}

	public static IEnumerable<string> ELocalBlueprintCodes()
	{
		string[] dirs = Directory.GetDirectories(Platform.current.GetBlueprintsDir(), "BP*", SearchOption.TopDirectoryOnly);
		foreach (string text in dirs)
		{
			string text2 = Path.GetFileName(text)[2..].Trim();
			if (!(text2 == "") && File.Exists(BlueprintFile(text, text2)))
			{
				yield return text2;
			}
		}
	}

	public static string GetBlueprintCodeFromPath(string path)
	{
		string[] files = Directory.GetFiles(path, "*.bp", SearchOption.TopDirectoryOnly);
		if (files.Length == 0)
		{
			return null;
		}
		return Path.GetFileNameWithoutExtension(files[0]);
	}
}
