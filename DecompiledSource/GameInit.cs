using System;
using System.Collections;
using UnityEngine;

public class GameInit : Singleton
{
	public static GameInit instance;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Setup(Action<string> action_done, Action<float> func_progress)
	{
		StartKoroutine(KSetup(action_done, func_progress));
	}

	private IEnumerator KSetup(Action<string> action_done, Action<float> func_progress)
	{
		string fatal_error = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			if (fatal_error != null)
			{
				Debug.LogError(fatal_error);
			}
			action_done(fatal_error);
		});
		try
		{
			if (Platform.current == null)
			{
				yield return StartKoroutine(kid, KInitPrePlatform(delegate(string err)
				{
					fatal_error = err;
				}, delegate(float f)
				{
					func_progress(f * 0.2f);
				}));
				if (fatal_error != null)
				{
					yield break;
				}
				Platform.Select();
				yield return StartKoroutine(kid, Platform.current.KInit(delegate(string err)
				{
					fatal_error = err;
				}, delegate(float f)
				{
					func_progress(0.25f + f * 0.2f);
				}));
				if (fatal_error != null)
				{
					yield break;
				}
				yield return StartKoroutine(kid, KInitPostPlatform(delegate(string err)
				{
					fatal_error = err;
				}, delegate(float f)
				{
					func_progress(0.5f + f * 0.2f);
				}));
				if (fatal_error != null)
				{
					yield break;
				}
			}
			if (!GlobalGameState.resourcesLoaded)
			{
				yield return StartKoroutine(kid, KLoadResources(delegate(string err)
				{
					fatal_error = err;
				}, delegate(float f)
				{
					func_progress(0.75f + f * 0.2f);
				}));
				if (fatal_error == null)
				{
				}
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public IEnumerator KInitPrePlatform(Action<string> callback, Action<float> func_progress)
	{
		string fatal_error = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			func_progress(1f);
			callback(fatal_error);
		});
		try
		{
			func_progress(0f);
			fatal_error = "First Init failed";
			yield return StartCoroutine(DebugSettings.CInit());
			Debug.Log("Version: " + DebugSettings.standard.currentVersion);
			CommandLine.Process();
			fatal_error = null;
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public IEnumerator KInitPostPlatform(Action<string> callback, Action<float> func_progress)
	{
		string fatal_error = null;
		KoroutineId koroutineId = SetFinalizer(delegate
		{
			func_progress(1f);
			callback(fatal_error);
		});
		try
		{
			func_progress(0f);
			fatal_error = "InputManager Init failed";
			if (!InputManager.Init())
			{
				yield break;
			}
			fatal_error = null;
			if (DebugSettings.standard.resetPlayerSave)
			{
				Player.Reset();
			}
			else
			{
				StartKoroutine(koroutineId, Player.KLoad(this, delegate(bool success)
				{
					if (!success)
					{
						Debug.LogError("Couldn't load player data; resetting");
						Player.Reset();
					}
				}));
				if (fatal_error != null)
				{
					yield break;
				}
			}
			fatal_error = "Localisation Init failed";
			if (!Loc.Init())
			{
				yield break;
			}
			fatal_error = "Changelog Init failed";
			if (Changelog.Init())
			{
				fatal_error = "BlueprintManager Init failed";
				if (BlueprintManager.RefreshBlueprints())
				{
					fatal_error = null;
				}
			}
		}
		finally
		{
			StopKoroutine(koroutineId);
		}
	}

	public IEnumerator KLoadResources(Action<string> callback, Action<float> func_progress)
	{
		string fatal_error = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			func_progress(1f);
			if (callback != null)
			{
				callback(fatal_error);
			}
			GlobalGameState.resourcesLoaded = true;
		});
		try
		{
			Debug.Log("Loading resources...");
			func_progress(0f);
			fatal_error = "Data Init failed";
			yield return StartKoroutine(kid, PrefabData.KInit(this));
			if (PrefabData.buildings.Count == 0)
			{
				fatal_error = "BuildingData Init failed";
				yield break;
			}
			if (PrefabData.trails.Count == 0)
			{
				fatal_error = "TrailData Init failed";
				yield break;
			}
			yield return null;
			fatal_error = "Tech Tree Init failed";
			if (TechTree.Init())
			{
				fatal_error = "Instinct Init failed";
				if (Instinct.Init())
				{
					fatal_error = "AssetLinks Init failed";
					yield return StartCoroutine(AssetLinks.CInit());
					fatal_error = "GlobalValues Init failed";
					yield return StartCoroutine(GlobalValues.CInit());
					fatal_error = "AudioLinks Init failed";
					yield return StartCoroutine(AudioLinks.CInit());
					fatal_error = null;
					InputManager.InitPostResources();
					UIGlobal.SetHardwareCursor();
					ClickableObject.InitAnimParams();
					Debug.Log("Loading resources done");
				}
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}
}
