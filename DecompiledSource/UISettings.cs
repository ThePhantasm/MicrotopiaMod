using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISettings : UIBaseSingleton
{
	private enum SettingsTab
	{
		General,
		Input,
		Game
	}

	public static UISettings instance;

	[SerializeField]
	private UITextImageButton btGeneral;

	[SerializeField]
	private UITextImageButton btInput;

	[SerializeField]
	private UITextImageButton btGame;

	[SerializeField]
	private UIButtonText btOk;

	[SerializeField]
	private GameObject pfSetting;

	[SerializeField]
	private Transform settingsPanel;

	private static List<UISettings_Setting> settings;

	private static List<Vector2Int> resolutions;

	private static List<Language> languages;

	private static List<FullScreenMode> fullScreenModes;

	private FullScreenMode fullScreenMode;

	private Vector2Int resolution;

	private UISettings_Setting settingResolution;

	private UISettings_Setting settingFpsLimit;

	private Action actionDone;

	private SettingsTab curTab;

	private bool inGame;

	public const float RENDER_SCALE_MIN = 0.5f;

	public const float RENDER_SCALE_MAX = 2f;

	private Coroutine cSetRenderScale;

	private List<UISettings_Setting> configSettings;

	public bool inputPolling;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	private static void EnsureResolutionList()
	{
		if (resolutions != null)
		{
			return;
		}
		Resolution[] array = Screen.resolutions;
		resolutions = new List<Vector2Int>();
		for (int i = 0; i < array.Length; i++)
		{
			Vector2Int item = new Vector2Int(array[i].width, array[i].height);
			if (item.x >= 1000 && item.y >= 700 && !resolutions.Contains(item))
			{
				resolutions.Add(item);
			}
		}
		resolutions.Sort((Vector2Int a, Vector2Int b) => -(a.x * 10000 + a.y).CompareTo(b.x * 10000 + b.y));
	}

	public void Init(bool in_game, Action action_done)
	{
		actionDone = action_done;
		inGame = in_game;
		btOk.Init(delegate
		{
			StartClose();
		});
		btGeneral.SetButton(delegate
		{
			SetTab(SettingsTab.General);
		});
		btInput.SetButton(delegate
		{
			SetTab(SettingsTab.Input);
		});
		btGame.SetButton(delegate
		{
			SetTab(SettingsTab.Game);
		});
		settings = new List<UISettings_Setting>();
		SetTab(SettingsTab.General);
	}

	private void SetTab(SettingsTab tab)
	{
		ClearSettings();
		curTab = tab;
		switch (curTab)
		{
		case SettingsTab.General:
			AddGeneralSettings();
			SetSelected(btGeneral);
			break;
		case SettingsTab.Input:
			AddInputSettings();
			SetSelected(btInput);
			break;
		case SettingsTab.Game:
			AddWorldSettings();
			SetSelected(btGame);
			break;
		}
	}

	private void ClearSettings()
	{
		foreach (UISettings_Setting setting in settings)
		{
			UnityEngine.Object.Destroy(setting.gameObject);
		}
		settings.Clear();
	}

	private void AddGeneralSettings()
	{
		UIGlobal uIGlobal = UIGlobal.instance;
		uIGlobal.onResolutionChange = (UIGlobal.OnResolutionChange)Delegate.Combine(uIGlobal.onResolutionChange, new UIGlobal.OnResolutionChange(OnResolutionChange));
		EnsureResolutionList();
		languages = new List<Language>();
		foreach (Language item in Loc.EAllowedLanguages())
		{
			languages.Add(item);
		}
		fullScreenModes = new List<FullScreenMode>
		{
			FullScreenMode.FullScreenWindow,
			FullScreenMode.ExclusiveFullScreen,
			FullScreenMode.Windowed
		};
		fullScreenMode = Screen.fullScreenMode;
		resolution = new Vector2Int(Screen.width, Screen.height);
		AddSetting().InitText(Loc.GetUI("SETTINGS_AUDIO"));
		AddSetting().InitSlider("SETTING_VOLUME_MASTER", 0f, 1f, () => Player.globalVolume, delegate(float v)
		{
			Player.globalVolume = v;
			AudioManager.instance.UpdateVolumes();
		});
		AddSetting().InitSlider("SETTING_MUSICVOLUME", 0f, 1f, () => Player.musicVolume, delegate(float v)
		{
			Player.musicVolume = v;
			AudioManager.instance.UpdateVolumes();
		});
		AddSetting().InitSlider("SETTING_VOLUME_UI", 0f, 1f, () => Player.uiVolume, delegate(float v)
		{
			Player.uiVolume = v;
			AudioManager.instance.UpdateVolumes();
		});
		AddSetting().InitSlider("SETTING_VOLUME_WORLD", 0f, 1f, () => Player.worldVolume, delegate(float v)
		{
			Player.worldVolume = v;
			AudioManager.instance.UpdateVolumes();
		});
		AddSetting().InitEmpty();
		AddSetting().InitText(Loc.GetUI("SETTINGS_DISPLAY"));
		AddSetting().InitDropdown("SETTING_FULLSCREEN", delegate(List<string> strs)
		{
			foreach (FullScreenMode fullScreenMode in fullScreenModes)
			{
				strs.Add(Loc.GetUI("SETTING_FULLSCREEN_" + fullScreenMode.ToString().ToUpperInvariant()));
			}
		}, () => fullScreenModes.IndexOf(fullScreenMode), delegate(int index)
		{
			fullScreenMode = fullScreenModes[index];
			ApplyScreenSettings();
		});
		settingResolution = AddSetting();
		settingResolution.InitDropdown("SETTING_RESOLUTION", delegate(List<string> strs)
		{
			foreach (Vector2Int resolution in resolutions)
			{
				strs.Add($"{resolution.x} x {resolution.y}");
			}
		}, () => resolutions.IndexOf(resolution), delegate(int index)
		{
			resolution = resolutions[index];
			ApplyScreenSettings();
		});
		FillResolution();
		AddSetting().InitSlider("SETTING_RENDERSCALE", 0.5f, 2f, () => Player.renderScale, delegate(float v)
		{
			if (v > 0.99f && v < 1.01f)
			{
				v = 1f;
			}
			SetRenderScale(v);
		});
		AddSetting().InitToggle("SETTING_VSYNC", () => Player.vSyncCount != 0, delegate(bool b)
		{
			Player.SetFps(b ? 1 : 0, Player.fpsLimit);
			UpdateFpsLimitSetting();
		});
		settingFpsLimit = AddSetting();
		settingFpsLimit.InitDropdown("SETTING_FPSLIMIT", delegate(List<string> strs)
		{
			strs.Add("30");
			strs.Add("60");
			strs.Add("90");
			strs.Add("120");
			strs.Add(Loc.GetUI("SETTING_FPSLIMIT_UNLIMITED"));
		}, delegate
		{
			int fpsLimit = Player.fpsLimit;
			if (fpsLimit <= 0)
			{
				return 4;
			}
			if (fpsLimit < 45)
			{
				return 0;
			}
			if (fpsLimit < 75)
			{
				return 1;
			}
			return (fpsLimit < 105) ? 2 : 3;
		}, delegate(int index)
		{
			Player.SetFps(Player.vSyncCount, index switch
			{
				0 => 30, 
				1 => 60, 
				2 => 90, 
				3 => 120, 
				_ => -1, 
			});
		});
		UpdateFpsLimitSetting();
		AddSetting().InitToggle("SETTING_FOG", () => !Player.lessFog, delegate(bool b)
		{
			Player.lessFog = !b;
			if (GameManager.instance != null)
			{
				GameManager.instance.UpdateBiomeInfluence();
			}
		});
		AddSetting().InitEmpty();
		AddSetting().InitText(Loc.GetUI("SETTINGS_INTERFACE"));
		AddSetting().InitDropdown("SETTING_LANGUAGE", delegate(List<string> strs)
		{
			foreach (Language language2 in languages)
			{
				strs.Add(Loc.GetLanguageName(language2));
			}
		}, () => languages.IndexOf(Player.language), delegate(int index)
		{
			Language language = languages[index];
			if (Player.language != language)
			{
				Player.language = language;
				OnLanguageChange();
				ClearSettings();
				AddGeneralSettings();
			}
		});
		AddSetting().InitDropdown("SETTING_UISCALE", delegate(List<string> strs)
		{
			strs.Add(Loc.GetUI("SETTING_UISCALE_TINY"));
			strs.Add(Loc.GetUI("SETTING_UISCALE_SMALL"));
			strs.Add(Loc.GetUI("SETTING_UISCALE_REGULAR"));
			strs.Add(Loc.GetUI("SETTING_UISCALE_BIG"));
		}, () => Mathf.RoundToInt((Player.uiScale - 0.5f) / 0.25f), delegate(int index)
		{
			Player.uiScale = 0.5f + 0.25f * (float)index;
			UIGlobal.ApplyCanvasScale();
		});
		AddSetting().InitEmpty();
		AddSetting().InitText(Loc.GetUI("SETTING_GAMEPLAY"));
		AddSetting().InitDropdown("SETTING_AUTOSAVE", delegate(List<string> strs)
		{
			strs.Add(Loc.GetUI("SETTING_AUTOSAVE_N_MINUTES", "5"));
			strs.Add(Loc.GetUI("SETTING_AUTOSAVE_N_MINUTES", "10"));
			strs.Add(Loc.GetUI("SETTING_AUTOSAVE_N_MINUTES", "15"));
			strs.Add(Loc.GetUI("SETTING_AUTOSAVE_N_MINUTES", "30"));
			strs.Add(Loc.GetUI("SETTING_AUTOSAVE_N_MINUTES", "60"));
			strs.Add(Loc.GetUI("SETTING_AUTOSAVE_ONLY_EXIT"));
		}, delegate
		{
			float num = Player.timeBetweenAutoSaves / 60f;
			if (num <= 0f)
			{
				return 5;
			}
			if (num < 7.5f)
			{
				return 0;
			}
			if (num < 12.5f)
			{
				return 1;
			}
			if (num < 42.5f)
			{
				return 2;
			}
			return (num < 45f) ? 3 : 4;
		}, delegate(int index)
		{
			float num = index switch
			{
				0 => 5f, 
				1 => 10f, 
				2 => 15f, 
				3 => 30f, 
				4 => 60f, 
				_ => -1f, 
			};
			if (num > 0f)
			{
				num *= 60f;
			}
			Player.timeBetweenAutoSaves = num;
			if (Player.remainingAutoSaveTime > num)
			{
				Player.ResetRemainingAutoSaveTime();
			}
		});
		AddSetting().InitToggle("SETTING_ANGLESNAP", () => Player.freeCameraAngle, delegate(bool b)
		{
			Player.freeCameraAngle = b;
		});
		AddSetting().InitDropdown("SETTING_MOUSE_ROTATION_STEP", delegate(List<string> strs)
		{
			strs.Add(Loc.GetUI("SETTING_YES"));
			strs.Add(Loc.GetUI("SETTING_NO"));
			strs.Add(Loc.GetUI("SETTING_MOUSE_ROTATION_STEP_ONLY"));
		}, () => (int)Player.buildRotMode, delegate(int index)
		{
			Player.buildRotMode = (BuildingRotationSetting)index;
		});
		AddSetting().InitButtonOnly("SETTINGS_RESTOREDEFAULTS", delegate
		{
			UIDialogBase uIDialogBase = UIBase.Spawn<UIDialogBase>();
			uIDialogBase.SetText(Loc.GetUI("SETTINGS_RESTOREDEFAULTS_CONFIRM"));
			uIDialogBase.SetAction(DialogResult.YES, delegate
			{
				int nextAutoSaveNumber = Player.nextAutoSaveNumber;
				string lastSave = Player.lastSave;
				Language language = Player.language;
				Player.Reset();
				Player.nextAutoSaveNumber = nextAutoSaveNumber;
				Player.lastSave = lastSave;
				AudioManager.instance.UpdateVolumes();
				UIGlobal.ApplyCanvasScale();
				if (Player.language != language)
				{
					OnLanguageChange();
				}
				ClearSettings();
				AddGeneralSettings();
			});
			uIDialogBase.SetAction(DialogResult.NO, uIDialogBase.StartClose);
		});
	}

	public override void StartClose()
	{
		Player.Save();
		if (actionDone != null)
		{
			actionDone();
		}
		base.StartClose();
	}

	protected override void OnDestroy()
	{
		if (UIGlobal.instance != null)
		{
			UIGlobal uIGlobal = UIGlobal.instance;
			uIGlobal.onResolutionChange = (UIGlobal.OnResolutionChange)Delegate.Remove(uIGlobal.onResolutionChange, new UIGlobal.OnResolutionChange(OnResolutionChange));
		}
		base.OnDestroy();
	}

	private int GetResolutionsIndex(out bool exact)
	{
		int result = 0;
		int num = int.MaxValue;
		for (int i = 0; i < resolutions.Count; i++)
		{
			Vector2Int vector2Int = resolutions[i];
			int num2 = vector2Int.x - resolution.x;
			int num3 = vector2Int.y - resolution.y;
			int num4 = num2 * num2 + num3 * num3;
			if (num4 < num)
			{
				num = num4;
				result = i;
			}
		}
		exact = num == 0;
		return result;
	}

	private UISettings_Setting AddSetting()
	{
		UISettings_Setting component = UnityEngine.Object.Instantiate(pfSetting, settingsPanel).GetComponent<UISettings_Setting>();
		settings.Add(component);
		return component;
	}

	private void ApplyScreenSettings()
	{
		bool exact;
		int resolutionsIndex = GetResolutionsIndex(out exact);
		if (!exact && fullScreenMode != FullScreenMode.Windowed)
		{
			resolution = resolutions[resolutionsIndex];
		}
		SetScreen(fullScreenMode, resolution.x, resolution.y);
	}

	public static void SetScreen(FullScreenMode fs_mode, int w, int h)
	{
		if ((uint)fs_mode <= 1u || fs_mode == FullScreenMode.Windowed)
		{
			Screen.SetResolution(w, h, fs_mode);
		}
		else
		{
			SetScreenDefault();
		}
	}

	public static void SetScreenDefault()
	{
		EnsureResolutionList();
		Vector2Int vector2Int = resolutions[0];
		SetScreen(FullScreenMode.FullScreenWindow, vector2Int.x, vector2Int.y);
	}

	private void OnResolutionChange()
	{
		if (!(instance == null))
		{
			resolution = new Vector2Int(Screen.width, Screen.height);
			if (curTab == SettingsTab.General)
			{
				FillResolution();
			}
		}
	}

	private void FillResolution()
	{
		if (fullScreenMode == FullScreenMode.Windowed)
		{
			settingResolution.SetReadOnly(read_only: true, $"{resolution.x} x {resolution.y}");
			return;
		}
		settingResolution.SetReadOnly(read_only: false);
		if (resolutions.Contains(resolution))
		{
			settingResolution.FillValue();
		}
		else
		{
			settingResolution.FillValue($"{resolution.x} x {resolution.y}");
		}
	}

	private void SetSelected(UITextImageButton bt)
	{
		foreach (UITextImageButton item in new List<UITextImageButton> { btGeneral, btInput, btGame })
		{
			item.ResetOverlays();
		}
		bt.AddOverlay(OverlayTypes.SELECTED);
	}

	private void OnLanguageChange()
	{
		Loc.UpdateLanguage(reload_text: true);
		foreach (UISettings_Setting setting in settings)
		{
			setting.UpdateLanguage();
		}
		GameManager.instance.UpdatePickupInventory();
		if (Gameplay.instance != null)
		{
			Gameplay.DoRefreshUnlocks();
			UIEscMenu.instance.Init();
			UIGame.instance.ResetInventory();
			UIGame.instance.SetInventoryTitle();
			UIGame.instance.RefreshTasks();
			UIGame.instance.UIUpdate();
			if (Hunger.main != null)
			{
				Hunger.main.EnergyChanged();
			}
		}
	}

	private void SetRenderScale(float v)
	{
		if (cSetRenderScale != null)
		{
			StopCoroutine(cSetRenderScale);
		}
		cSetRenderScale = StartCoroutine(CSetRenderScale(v));
	}

	private IEnumerator CSetRenderScale(float v)
	{
		yield return new WaitForSeconds(0.2f);
		Player.SetRenderScale(v);
		cSetRenderScale = null;
	}

	private void UpdateFpsLimitSetting()
	{
		if (Player.vSyncCount == 0)
		{
			settingFpsLimit.SetReadOnly(read_only: false);
			settingFpsLimit.SetGrey(grey: false);
		}
		else
		{
			settingFpsLimit.SetReadOnly(read_only: true, (Player.fpsLimit <= 0) ? Loc.GetUI("SETTING_FPSLIMIT_UNLIMITED") : $"{Player.fpsLimit}");
			settingFpsLimit.SetGrey(grey: true);
		}
	}

	public static int AllowedVsyncValue(int v)
	{
		return Mathf.Clamp(v, 0, 1);
	}

	public static int AllowedFpsLimitValue(int v)
	{
		if (v <= 0)
		{
			return -1;
		}
		if (v < 45)
		{
			return 30;
		}
		if (v < 75)
		{
			return 60;
		}
		if (v < 105)
		{
			return 90;
		}
		return 120;
	}

	private void AddInputSettings()
	{
		configSettings = new List<UISettings_Setting>();
		AddInputConfig("INPUT_SELECT", InputAction.Select);
		AddInputConfig("INPUT_DESELECT_CAM_DRAG", InputAction.DeselectOrCamDrag);
		AddInputConfig("INPUT_DESELECT", InputAction.Deselect);
		AddSetting().InitEmpty();
		AddInputConfig("INPUT_CAM_DRAG", InputAction.CamDrag);
		AddInputConfig("INPUT_CAM_LEFT", InputAction.CamLeft);
		AddInputConfig("INPUT_CAM_RIGHT", InputAction.CamRight);
		AddInputConfig("INPUT_CAM_UP", InputAction.CamUp);
		AddInputConfig("INPUT_CAM_DOWN", InputAction.CamDown);
		AddSetting().InitSlider("INPUT_CAM_SPEED", 0.25f, 5f, () => InputManager.camKeysMoveSpeed, delegate(float f)
		{
			InputManager.camKeysMoveSpeed = f;
		});
		AddSetting().InitEmpty();
		AddInputConfig("INPUT_CAM_ROTATE", InputAction.CamRotate);
		AddSetting().InitInputConfig("INPUT_CAM_ROTATE_ALT", InputManager.GetDesc_BothMouseButtons());
		AddInputConfig("INPUT_CAM_ROTATE_LEFT", InputAction.CamRotateLeft);
		AddInputConfig("INPUT_CAM_ROTATE_RIGHT", InputAction.CamRotateRight);
		AddSetting().InitSlider("INPUT_CAM_ROTATE_SPEED", 0.25f, 5f, () => InputManager.camKeysRotateSpeed, delegate(float f)
		{
			InputManager.camKeysRotateSpeed = f;
		});
		AddSetting().InitToggle("SETTING_CAMERA_INVERTED", () => Player.invertCamera, delegate(bool b)
		{
			Player.invertCamera = b;
		});
		AddSetting().InitEmpty();
		AddSetting().InitInputConfig("INPUT_CAM_ZOOM", InputManager.GetDesc_Scroll());
		AddInputConfig("INPUT_CAM_ZOOM_IN", InputAction.CamZoomIn);
		AddInputConfig("INPUT_CAM_ZOOM_OUT", InputAction.CamZoomOut);
		AddSetting().InitSlider("INPUT_CAM_ZOOM_SPEED", 0.25f, 5f, () => InputManager.camZoomSpeed, delegate(float f)
		{
			InputManager.camZoomSpeed = f;
		});
		AddInputConfig("INPUT_TOGGLE_MAPMODE", InputAction.ToggleMap);
		AddSetting().InitEmpty();
		AddInputConfig("INPUT_PAUSE", InputAction.Pause);
		AddInputConfig("INPUT_QUICKSAVE", InputAction.QuickSave);
		AddInputConfig("INPUT_QUICKLOAD", InputAction.QuickLoad);
		AddInputConfig("INPUT_PIPETTE_TOOL", InputAction.Pipette);
		AddInputConfig("INPUT_SELECT_MULTIPLE", InputAction.SelectMultiple);
		AddInputConfig("INPUT_DONTSNAP", InputAction.DontSnap);
		AddInputConfig("INPUT_TRAILDRAGGING_LOCK", InputAction.TrailDragLock);
		AddInputConfig("GAME_TECHTREE", InputAction.TechTree);
		AddInputConfig("BLUEPRINTS", InputAction.BlueprintManager);
		AddSetting().InitEmpty();
		AddInputConfig("INPUT_DELETE", InputAction.Delete);
		AddInputConfig("INPUT_RELOCATE", InputAction.Relocate);
		AddInputConfig("INPUT_FOLLOW_ANT", InputAction.FollowAnt);
		AddInputConfig("INPUT_DROP_PICKUP", InputAction.DropPickup);
		AddInputConfig("INPUT_INTERACT_BUILDING", InputAction.InteractBuilding);
		AddInputConfig("INPUT_PLACE_DISPENSER", InputAction.PlaceDispenser);
		AddInputConfig("INPUT_COPY_SETTINGS", InputAction.CopySettings);
		AddInputConfig("INPUT_PASTE_SETTINGS", InputAction.PasteSettings);
		AddInputConfig("INPUT_ROTATE_BUILDING_LEFT", InputAction.BuildingRotateLeft);
		AddInputConfig("INPUT_ROTATE_BUILDING_RIGHT", InputAction.BuildingRotateRight);
		AddSetting().InitSlider("SETTING_BUILDING_ROTATION_SPEED", 0.5f, 1.5f, () => InputManager.buildRotSpeed, delegate(float f)
		{
			InputManager.buildRotSpeed = f;
		});
		AddInputConfig("VIEW_HIDE_UI", InputAction.FilterHideUI);
		if (DebugSettings.standard.enableFilterHotKeys)
		{
			AddInputConfig("VIEW_TRAILS_FOREGROUND", InputAction.FilterFloatingTrails);
			AddInputConfig("VIEW_HIDE_TRAILS", InputAction.FilterHideTrails);
			AddInputConfig("VIEW_HIDE_ANTS", InputAction.FilterHideAnts);
		}
		AddSetting().InitEmpty();
		foreach (var (header_code, input_action) in InputManager.ETrailShortcutSettings())
		{
			AddInputConfig(header_code, input_action);
		}
		AddInputConfig("_obs_TRAIL_ERASER", InputAction.Eraser);
		AddInputConfig("_obs_TRAIL_FLOORSELECTOR", InputAction.FloorSelector);
		AddInputConfig("INPUT_PREVIOUS_BUILD_GROUP", InputAction.PrevGroup);
		AddInputConfig("INPUT_NEXT_BUILD_GROUP", InputAction.NextGroup);
		CheckInputDuplicates();
		AddSetting().InitEmpty();
		AddSetting().InitButtonOnly("SETTINGS_RESTOREDEFAULTS", delegate
		{
			UIDialogBase uIDialogBase = UIBase.Spawn<UIDialogBase>();
			uIDialogBase.SetText(Loc.GetUI("SETTINGS_RESTOREDEFAULTS_CONFIRM"));
			uIDialogBase.SetAction(DialogResult.YES, delegate
			{
				InputManager.ResetToDefault();
				InputManager.camKeysMoveSpeed = 1f;
				InputManager.camKeysRotateSpeed = 1f;
				InputManager.camZoomSpeed = 1f;
				ClearSettings();
				AddInputSettings();
			});
			uIDialogBase.SetAction(DialogResult.NO, uIDialogBase.StartClose);
		});
	}

	private void AddInputConfig(string header_code, InputAction input_action)
	{
		UISettings_Setting cs = AddSetting();
		cs.InitInputConfig(header_code, input_action, delegate(bool polling)
		{
			inputPolling = polling;
			foreach (UISettings_Setting configSetting in configSettings)
			{
				if (polling)
				{
					configSetting.SetButtonVisible(configSetting == cs);
				}
				else
				{
					configSetting.SetButtonVisible(vis: true);
				}
			}
			CheckInputDuplicates();
		});
		configSettings.Add(cs);
	}

	private void CheckInputDuplicates()
	{
		List<InputAction> configDuplicates = InputManager.GetConfigDuplicates();
		foreach (UISettings_Setting configSetting in configSettings)
		{
			configSetting.SetValueError(configDuplicates.Contains(configSetting.inputAction));
		}
	}

	private void AddWorldSettings()
	{
		if (inGame)
		{
			AddSetting().InitReadOnly("WORLD_SEED", WorldSettings.seed, selectable: true);
			AddSetting().InitReadOnly("WORLD_ANT_SPECIES", Loc.GetObject($"ANTSPECIES_{WorldSettings.antSpecies}"));
		}
		else
		{
			AddSetting().InitReadOnly("WORLD_SEED", Loc.GetUI("SETTING_NOTAVAILABLE"));
			AddSetting().InitReadOnly("WORLD_ANT_SPECIES", Loc.GetUI("SETTING_NOTAVAILABLE"));
		}
		AddSetting().InitEmpty();
		AddSetting().InitToggle("WORLD_CROSS_ISLAND_BUILDING", () => Player.crossIslandBuilding, delegate(bool b)
		{
			Player.crossIslandBuilding = b;
		});
		if (WorldSettings.cheatsEnabled)
		{
			AddSetting().InitEmpty();
			AddSetting().InitText(Loc.GetUI("WORLD_CHEATS_ENABLED") ?? "");
			AddSetting().InitToggle("?Unlock everything", () => Player.cheatEnableAllBuildings, delegate(bool b)
			{
				Player.cheatEnableAllBuildings = b;
			});
			AddSetting().InitToggle("?Immortal ants", () => Player.cheatImmortalAnts, delegate(bool b)
			{
				Player.cheatImmortalAnts = b;
			});
			AddSetting().InitToggle("?Free buildings", () => Player.cheatFreeBuildings, delegate(bool b)
			{
				Player.cheatFreeBuildings = b;
			});
			AddSetting().InitToggle("?Free larvae", () => Player.cheatFreeLarvae, delegate(bool b)
			{
				Player.cheatFreeLarvae = b;
			});
			AddSetting().InitToggle("?Free development tree", () => Player.cheatFreeTechTree, delegate(bool b)
			{
				Player.cheatFreeTechTree = b;
			});
			AddSetting().InitToggle("?Can Delete Everything", () => Player.cheatDeletableEverything, delegate(bool b)
			{
				Player.cheatDeletableEverything = b;
			});
			AddSetting().InitToggle("?Objectives always satisfied", () => Player.cheatFreeObjectives, delegate(bool b)
			{
				Player.cheatFreeObjectives = b;
			});
			AddSetting().InitToggle("?Reveal all tutorials", () => Player.cheatShowAllTutorials, delegate(bool b)
			{
				Player.cheatShowAllTutorials = b;
			});
			AddSetting().InitToggle("?Show full inventory", () => Player.cheatShowFullInventory, delegate(bool b)
			{
				Player.cheatShowFullInventory = b;
			});
		}
	}
}
