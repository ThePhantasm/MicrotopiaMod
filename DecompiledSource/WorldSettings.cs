using System.Collections.Generic;
using UnityEngine;

public static class WorldSettings
{
	public static string startingBiome;

	public static AntSpecies antSpecies;

	public static int seedInt;

	public static bool cheatsEnabled;

	public static bool quickInstinct;

	public static bool sandbox;

	public static int[] startingGrounds;

	public static string seed { get; private set; }

	public static void Write(Save save)
	{
		save.Write(startingBiome);
		save.Write((int)antSpecies);
		save.Write(seed);
		save.Write(cheatsEnabled);
		save.Write(quickInstinct);
		save.Write(sandbox);
	}

	public static void Read(Save save)
	{
		if (save.version < 14)
		{
			FillDefault();
			return;
		}
		startingBiome = save.ReadString();
		antSpecies = (AntSpecies)save.ReadInt();
		SetSeed(save.ReadString());
		cheatsEnabled = save.ReadBool();
		quickInstinct = save.ReadBool();
		if (save.version > 39)
		{
			sandbox = save.ReadBool();
		}
	}

	public static void FillDefault()
	{
		startingBiome = GetNormalStartBiome();
		antSpecies = AntSpecies.DEFAULT;
		SetSeed(null);
		cheatsEnabled = (quickInstinct = (sandbox = false));
	}

	public static void FillFromDebugSettings()
	{
		startingBiome = DebugSettings.standard.startingBiome;
		if (string.IsNullOrEmpty(startingBiome))
		{
			startingBiome = GetNormalStartBiome();
		}
		antSpecies = DebugSettings.standard.antSpecies;
		SetSeed(DebugSettings.standard.seed);
		cheatsEnabled = DebugSettings.standard.cheatsEnabled;
		quickInstinct = DebugSettings.standard.quickInstinct;
		sandbox = DebugSettings.standard.startInSandbox;
	}

	public static void SetSeed(string _seed)
	{
		if (DebugSettings.standard.demo && !string.IsNullOrEmpty(DebugSettings.standard.demoLockedSeed))
		{
			seed = DebugSettings.standard.demoLockedSeed;
		}
		else
		{
			seed = _seed;
			if (string.IsNullOrEmpty(seed))
			{
				seed = GetRandomSeed();
			}
		}
		seedInt = SeedStringToInt(seed);
	}

	public static string GetNormalStartBiome()
	{
		return "BiomeBlue2";
	}

	public static IEnumerable<string> EPossibleStartBiomesForSandbox()
	{
		yield return "BiomeBlue2";
		yield return "BiomeGreen";
		yield return "BiomeScrapara";
		yield return "BiomeToxicWaste";
		yield return "BiomeConcrete";
		yield return "BiomeBlank";
	}

	public static IEnumerable<AntSpecies> EPossibleAntSpecies()
	{
		yield return AntSpecies.DEFAULT;
	}

	public static int SeedStringToInt(string s)
	{
		return Mathf.Abs(s.GetHashCode());
	}

	public static string GetRandomSeed()
	{
		string[] array = new string[23]
		{
			"Dark", "Bright", "Foggy", "Misty", "Hidden", "Forgotten", "Secret", "Old", "Smelly", "Rusty",
			"Moist", "Arid", "Murky", "Desolate", "Low", "Cold", "Distant", "Warm", "Narrow", "Green",
			"Grey", "Brown", "Black"
		};
		string[] array2 = new string[19]
		{
			"Rubble", "Stone", "Iron", "Scrub", "Dust", "Puddle", "Waste", "Scrap", "Forest", "Grass",
			"Weed", "Rock", "Dirt", "Mud", "Critter", "Copper", "Debris", "Pebble", "Moss"
		};
		string[] array3 = new string[13]
		{
			"Heights", "Lands", "Islands", "Valley", "Hollows", "Fields", "Plains", "Country", "Regions", "Domains",
			"Grounds", "Crags", "Cliffs"
		};
		return array[Random.Range(0, array.Length)] + array2[Random.Range(0, array2.Length)] + array3[Random.Range(0, array3.Length)];
	}
}
