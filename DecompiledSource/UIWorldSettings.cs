using System;
using System.Collections.Generic;
using UnityEngine;

public class UIWorldSettings : UIBaseSingleton
{
	public static UIWorldSettings instance;

	[SerializeField]
	private UIButtonText btCancel;

	[SerializeField]
	private UIButtonText btCreate;

	[SerializeField]
	private GameObject pfSetting;

	[SerializeField]
	private Transform settingsPanel;

	private static List<UISettings_Setting> settings;

	private static List<string> startingBiomesForSandbox;

	private static List<AntSpecies> antSpecies;

	private UISettings_Setting settingQuickInstinct;

	private UISettings_Setting settingStartingBiome;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init(Action action_cancel, Action action_create)
	{
		WorldSettings.FillDefault();
		btCancel.Init(delegate
		{
			action_cancel();
			StartClose();
		});
		btCreate.Init(delegate
		{
			action_create();
			CheckSandboxOnStart();
			StartClose();
		});
		startingBiomesForSandbox = new List<string>();
		foreach (string item in WorldSettings.EPossibleStartBiomesForSandbox())
		{
			startingBiomesForSandbox.Add(item);
		}
		antSpecies = new List<AntSpecies>();
		foreach (AntSpecies item2 in WorldSettings.EPossibleAntSpecies())
		{
			antSpecies.Add(item2);
		}
		settings = new List<UISettings_Setting>();
		if (DebugSettings.standard.demo && DebugSettings.standard.demoLockedSeed != "")
		{
			UISettings_Setting uISettings_Setting = AddSetting();
			uISettings_Setting.InitInputFieldNonInteractable("WORLD_SEED");
			uISettings_Setting.FillValue(DebugSettings.standard.demoLockedSeed);
		}
		else
		{
			AddSetting().InitInputField("WORLD_SEED", () => WorldSettings.seed, delegate(string str)
			{
				WorldSettings.SetSeed(str);
			});
		}
		if (DebugSettings.standard.hasSandbox)
		{
			AddSetting().InitDropdown("WORLD_SANDBOX", delegate(List<string> strs)
			{
				strs.Add(Loc.GetUI("GENERIC_NO"));
				string text = "";
				text = " - " + Loc.GetUI("WORLD_CHEATS_WARNING_STEAM");
				strs.Add(Loc.GetUI("GENERIC_YES") + text);
			}, () => WorldSettings.sandbox ? 1 : 0, delegate(int index)
			{
				WorldSettings.sandbox = index == 1;
				SandboxSettingUpdated();
			});
		}
		AddSetting().InitDropdown("WORLD_ANT_SPECIES", delegate(List<string> strs)
		{
			foreach (AntSpecies antSpecy in antSpecies)
			{
				strs.Add(Loc.GetObject("ANTSPECIES_" + antSpecy));
			}
		}, () => antSpecies.IndexOf(WorldSettings.antSpecies), delegate(int index)
		{
			WorldSettings.antSpecies = antSpecies[index];
		});
		settingStartingBiome = AddSetting();
		settingStartingBiome.InitDropdown("WORLD_START_BIOME", delegate(List<string> strs)
		{
			foreach (string item3 in startingBiomesForSandbox)
			{
				switch (item3)
				{
				case "BiomeBlue2":
					strs.Add(Loc.GetObject("BIOME_" + BiomeType.BLUE));
					break;
				case "BiomeScrapara":
					strs.Add(Loc.GetObject("BIOME_" + BiomeType.DESERT));
					break;
				case "BiomeGreen":
					strs.Add(Loc.GetObject("BIOME_" + BiomeType.JUNGLE));
					break;
				case "BiomeToxicWaste":
					strs.Add(Loc.GetObject("BIOME_" + BiomeType.TOXIC));
					break;
				case "BiomeConcrete":
					strs.Add(Loc.GetObject("BIOME_" + BiomeType.CONCRETE));
					break;
				case "BiomeBlank":
					strs.Add(Loc.GetUI("WORLD_SANDBOX"));
					break;
				default:
					strs.Add(item3);
					break;
				}
			}
		}, () => startingBiomesForSandbox.IndexOf(WorldSettings.startingBiome), delegate(int index)
		{
			WorldSettings.startingBiome = startingBiomesForSandbox[index];
		});
		if (!DebugSettings.standard.demo)
		{
			settingQuickInstinct = AddSetting();
			settingQuickInstinct.InitToggle("WORLD_QUICK_INSTINCT", () => WorldSettings.quickInstinct, delegate(bool b)
			{
				WorldSettings.quickInstinct = b;
			});
		}
		SandboxSettingUpdated();
	}

	private UISettings_Setting AddSetting()
	{
		UISettings_Setting component = UnityEngine.Object.Instantiate(pfSetting, settingsPanel).GetComponent<UISettings_Setting>();
		settings.Add(component);
		return component;
	}

	private void SandboxSettingUpdated()
	{
		if (settingQuickInstinct != null)
		{
			settingQuickInstinct.Show(!WorldSettings.sandbox);
		}
		if (settingStartingBiome != null)
		{
			settingStartingBiome.Show(WorldSettings.sandbox);
		}
	}

	private void CheckSandboxOnStart()
	{
		if (!WorldSettings.sandbox)
		{
			WorldSettings.startingBiome = WorldSettings.GetNormalStartBiome();
		}
	}
}
