using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "DebugSettings", menuName = "Microtopia/DebugSettings", order = 0)]
public class DebugSettings : ScriptableObject
{
	public static DebugSettings standard;

	[Tooltip("Savegame die geladen moet worden als niet specifiek new/load gekozen")]
	public string loadOnStartup;

	[Header("Build settings")]
	public GameType buildType;

	public string currentVersion;

	public bool dutch;

	public bool french;

	public bool german;

	public bool polish;

	public bool russian;

	public bool chinese;

	public bool korean;

	public bool japanese;

	public string demoLockedSeed;

	public bool hasSandbox;

	[Header("World settings (if not started through menu)")]
	public string startingBiome;

	public AntSpecies antSpecies;

	public string seed;

	public bool cheatsEnabled;

	public bool quickInstinct;

	public bool startInSandbox;

	[Header("Cheats")]
	public bool instantQueen;

	[SerializeField]
	private bool freeLarvae;

	public bool freeRecipes;

	[SerializeField]
	private bool freeBuildings;

	public bool freeUnlocks;

	[SerializeField]
	private bool unlockEverything;

	public bool showOldBuildings;

	public bool instantFactories;

	[SerializeField]
	private bool immortalAnts;

	[SerializeField]
	private bool deletableEverything;

	[SerializeField]
	private bool instinctAlwaysSatisfied;

	public bool techtreeAllAvailable;

	public bool techtreeFreeTechs;

	public bool inventorsAlwaysFull;

	public bool musicAlwaysBusy;

	public bool musicAlwaysPolluted;

	public bool logShowAllTutorials;

	public bool alwaysPopupTutorials;

	public bool showFullInventory;

	public int cheatHungerTier;

	[Header("Debug various")]
	[Tooltip("don't load main.psav on start")]
	public bool resetPlayerSave;

	[Tooltip("Random te spawnen biomes ('B')")]
	public string[] biomeAddressesToSpawn;

	[Tooltip("Als actief: 'B' doet ook cam animation en zo")]
	public bool alsoAnimateOnCheatSpawnBiome;

	[Tooltip("Activeer F3, F4, F6, F7, F8 voor camera beweging")]
	public bool camAnimFunctionKeys;

	[Tooltip("Activeer filter hotkeys (in de input settings zijn de keys in te stellen)")]
	public bool enableFilterHotKeys;

	[Tooltip("Skip intro stuff when starting new savegame")]
	public bool skipIntro;

	[Header("Debug lines")]
	public bool showFlightLines;

	[Header("Logs")]
	public bool logPickupExchange;

	public bool logBuildingUnlocks;

	public bool logPollution;

	public bool logPlantSpawnFail;

	[Header("Fonts")]
	public List<TMP_FontAsset> usedFonts;

	[Header("Fonts")]
	public List<TMP_FontAsset> fallbacksSC;

	[Header("Fonts")]
	public List<TMP_FontAsset> fallbacksJP;

	public bool demo => buildType == GameType.Demo;

	public bool prologue => buildType == GameType.Prologue;

	public bool playtest => buildType == GameType.PlayTest;

	public static IEnumerator CInit()
	{
		AsyncOperationHandle<DebugSettings> loading = Addressables.LoadAssetAsync<DebugSettings>("ScriptableObjects/DebugSettings");
		yield return loading;
		standard = loading.Result;
		standard.loadOnStartup = "";
		standard.instantQueen = false;
		standard.freeLarvae = false;
		standard.freeRecipes = false;
		standard.freeBuildings = false;
		standard.freeUnlocks = false;
		standard.unlockEverything = false;
		standard.showOldBuildings = false;
		standard.instantFactories = false;
		standard.immortalAnts = false;
		standard.deletableEverything = false;
		standard.instinctAlwaysSatisfied = false;
		standard.resetPlayerSave = false;
		standard.alsoAnimateOnCheatSpawnBiome = false;
		standard.camAnimFunctionKeys = false;
		standard.enableFilterHotKeys = false;
		standard.skipIntro = false;
		standard.showFlightLines = false;
		standard.logPickupExchange = false;
		standard.logBuildingUnlocks = false;
		standard.logPollution = false;
		standard.logPlantSpawnFail = false;
		standard.techtreeAllAvailable = false;
		standard.techtreeFreeTechs = false;
		standard.inventorsAlwaysFull = false;
		standard.musicAlwaysBusy = false;
		standard.musicAlwaysPolluted = false;
		standard.logShowAllTutorials = false;
		standard.alwaysPopupTutorials = false;
		standard.cheatHungerTier = 0;
		standard.startingBiome = WorldSettings.GetNormalStartBiome();
		standard.antSpecies = AntSpecies.DEFAULT;
		standard.seed = "";
		standard.cheatsEnabled = false;
		standard.quickInstinct = false;
		standard.biomeAddressesToSpawn = null;
		standard.showFullInventory = false;
	}

	public bool FreeLarvae()
	{
		if (!freeLarvae)
		{
			return Player.cheatFreeLarvae;
		}
		return true;
	}

	public bool FreeBuildings()
	{
		if (!freeBuildings && !Player.cheatFreeBuildings)
		{
			return WorldSettings.sandbox;
		}
		return true;
	}

	public bool UnlockEverything()
	{
		if (!unlockEverything && !Player.cheatEnableAllBuildings)
		{
			return WorldSettings.sandbox;
		}
		return true;
	}

	public bool DeletableEverything()
	{
		if (!deletableEverything && !Player.cheatDeletableEverything)
		{
			return WorldSettings.sandbox;
		}
		return true;
	}

	public bool ProgressionEnabled()
	{
		return !WorldSettings.sandbox;
	}

	public bool InstinctAlwaysSatisfied()
	{
		if (!instinctAlwaysSatisfied)
		{
			return Player.cheatFreeObjectives;
		}
		return true;
	}

	public bool ImmortalAnts()
	{
		if (!immortalAnts)
		{
			return Player.cheatImmortalAnts;
		}
		return true;
	}
}
