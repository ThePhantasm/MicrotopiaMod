using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameManager : Singleton
{
	public static GameManager instance;

	public static bool isQuitting;

	[NonSerialized]
	public double playTime;

	[NonSerialized]
	public double gameTime;

	private GameStatus gameStatus;

	private GameStatus lastPlayTimeStatus = GameStatus.RUNNING;

	[NonSerialized]
	public bool runWorld;

	[NonSerialized]
	public bool runEditing;

	private float worldSpeed = 1f;

	public bool theater;

	[NonSerialized]
	public bool mapMode;

	public float gameFadeInDuration = 1f;

	public float gameFadeInDurationNewGame = 4f;

	private List<Ground> grounds = new List<Ground>();

	public Transform spawnParent;

	private Circle groundsCircle;

	public static bool CLOSE_ALL_DIALOGUE;

	private List<Ant> allAnts = new List<Ant>();

	private List<Building> allBuildings = new List<Building>();

	private List<Building> allFixedUpdateBuildings = new List<Building>();

	private HashSet<Building> buildingsBeingBuilt = new HashSet<Building>();

	private List<Queen> allQueens = new List<Queen>();

	private HashSet<Trail> allTrails = new HashSet<Trail>();

	private HashSet<Split> allSplits = new HashSet<Split>();

	private List<TrailGate> allTrailGates = new List<TrailGate>();

	private HashSet<Pickup> allPickups = new HashSet<Pickup>();

	private List<Pickup> allLarvae = new List<Pickup>();

	private HashSet<BiomeObject> allBiomeObjects = new HashSet<BiomeObject>();

	private List<Explosion> allExplosions = new List<Explosion>();

	private int antCount;

	private Dictionary<PickupType, int> dicTotalPickupInventory = new Dictionary<PickupType, int>();

	private bool shouldCheckBuildingBuildings;

	private bool shouldCountPickupInventory;

	private bool postponeAllPickupsChange;

	private List<Pickup> pickupsToRemove = new List<Pickup>();

	private List<Pickup> pickupsToAdd = new List<Pickup>();

	private UIEscMenu uiEscMenu;

	private HashSet<Animator> pausableAnimators = new HashSet<Animator>();

	private HashSet<ParticleSystem> pausableParticles = new HashSet<ParticleSystem>();

	private List<Billboard> activeBillboards = new List<Billboard>();

	private Dictionary<GameObject, (float, Quaternion)> dicRotatingPointers = new Dictionary<GameObject, (float, Quaternion)>();

	private UIHover uiHover;

	[NonSerialized]
	public Ground closestGround;

	private float ambienceFactor;

	private float updateSometimesCounter;

	private List<Stockpile> seen_stockpiles = new List<Stockpile>();

	private PickupType prevSeenPickupType;

	private string prevSeenBuildingCode;

	[NonSerialized]
	public int hiddenDuringMap;

	private bool doCheckLinkGates;

	private Coroutine cAddBiome;

	private Dictionary<int, ISaveable> dicLinks = new Dictionary<int, ISaveable>();

	public Ground tutorialGround => grounds[0];

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	protected override void Awake()
	{
		Application.quitting += delegate
		{
			isQuitting = true;
		};
		base.Awake();
	}

	private void Start()
	{
		if (theater)
		{
			return;
		}
		UIGlobal.instance.GoBlack(black: true, 0f);
		GameInit.instance.Setup(delegate(string fatal_error)
		{
			if (fatal_error == null)
			{
				string text = GlobalGameState.saveFile;
				bool debug_start = false;
				if (text == null)
				{
					debug_start = true;
					text = DebugSettings.standard.loadOnStartup.Trim();
					if (text == "" || !File.Exists(Files.GameSave(text, theater)))
					{
						WorldSettings.FillFromDebugSettings();
					}
				}
				StartKoroutine(KStartGame(text, debug_start));
			}
		}, delegate
		{
		});
	}

	public void Init()
	{
		uiEscMenu = UIBaseSingleton.Get(UIEscMenu.instance);
		uiEscMenu.Show(target: false);
	}

	public IEnumerator KStartGame(string save_file, bool debug_start)
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			if (!theater)
			{
				AudioManager.instance.Init();
			}
			Filters.Init();
			CamController.instance.Init();
			instance.Init();
			History.Init();
			if (!theater)
			{
				Lighting.instance.Init();
				Gameplay.instance.Init();
				UIBaseSingleton.Get(UIGame.instance, show: false);
			}
			string text = Files.GameSave(save_file, theater);
			if (save_file != "" && !File.Exists(text))
			{
				Debug.LogWarning("Loading '" + text + "': file doesn't exist" + (theater ? "" : ", starting new world"));
				save_file = "";
			}
			if (save_file == "" && theater)
			{
				Debug.LogWarning("Mainmenu: No valid savegame; not starting background scene");
				yield break;
			}
			bool load_failed = false;
			yield return StartKoroutine(kid, instance.KStartLoadGame(save_file, theater, debug_start, mid_game: false, delegate
			{
				load_failed = true;
			}, theater ? null : ((Action)delegate
			{
				GlobalGameState.GoToMainMenu();
			})));
			if (load_failed)
			{
				yield break;
			}
			if (CaptureMaker.instance != null)
			{
				CaptureMaker.instance.DoStart();
			}
			if (theater)
			{
				yield break;
			}
			if (save_file == "" && !DebugSettings.standard.skipIntro)
			{
				UIGlobal.instance.GoBlack(black: false, gameFadeInDurationNewGame);
				yield return StartKoroutine(kid, CamController.instance.KWorldIntro());
				if (DebugSettings.standard.playtest)
				{
					StartCoroutine(CShowPlaytestWelcome());
				}
			}
			else
			{
				UIGlobal.instance.GoBlack(black: false, gameFadeInDuration);
			}
			UIGame.instance.SetVisible(visible: true);
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void Update()
	{
		if (gameStatus == GameStatus.INIT || gameStatus == GameStatus.STOPPED || gameStatus == GameStatus.TUTORIAL)
		{
			return;
		}
		if (theater)
		{
			runEditing = false;
			CamController.instance.CamUpdateTheater();
		}
		else
		{
			InputManager.InputUpdate(runEditing && !mapMode);
			if (InputManager.escape && gameStatus != GameStatus.UNPAUSABLE)
			{
				if (Filters.IsActive(Filter.HIDE_UI))
				{
					Filters.Select(Filter.HIDE_UI, selected: false);
				}
				else if (runEditing && Gameplay.instance.GetActivity() != Activity.NONE)
				{
					Gameplay.instance.SetActivity(Activity.NONE);
					UIGlobal.SetHardwareCursor();
					UIHover.instance.Outit();
				}
				else if (runEditing && Gameplay.instance.GetSelectionCount() > 0)
				{
					Gameplay.instance.StopPlaying();
					Gameplay.instance.ClearFocus();
				}
				else if (gameStatus != GameStatus.MENU)
				{
					OpenEscMenu();
				}
				else if (!(UISettings.instance != null) || !UISettings.instance.inputPolling)
				{
					CloseAllMenuUI(resume_last_gamestate: true);
				}
			}
			if (uiHover == null)
			{
				uiHover = UIBaseSingleton.Get(UIHover.instance, show: false);
			}
			uiHover.UpdateHover();
			UIGame.instance.UIUpdate();
			UpdateBillboards();
		}
		if (runEditing)
		{
			playTime += Time.deltaTime;
			if (Player.timeBetweenAutoSaves > 0f)
			{
				Player.remainingAutoSaveTime -= Time.deltaTime;
				if (Player.remainingAutoSaveTime < 0f)
				{
					AutoSave();
				}
			}
			if (InputManager.quickSave)
			{
				QuickSave();
			}
			if (InputManager.quickLoad)
			{
				QuickLoad();
				return;
			}
			if (InputManager.pause)
			{
				switch (gameStatus)
				{
				case GameStatus.RUNNING:
					SetStatus(GameStatus.PAUSED);
					return;
				case GameStatus.PAUSED:
					SetStatus(GameStatus.RUNNING);
					return;
				}
			}
			if (Gameplay.instance.GetActivity() == Activity.NONE || (Gameplay.instance.GetActivity() == Activity.TRAIL_EDITING && !Gameplay.instance.IsDrawingTrail()))
			{
				if (InputManager.blueprints && Progress.HasUnlocked(GeneralUnlocks.BLUEPRINTS))
				{
					UIBlueprints uIBlueprints = UIBaseSingleton.Get(UIBlueprints.instance);
					uIBlueprints.Init();
					uIBlueprints.transform.SetAsLastSibling();
				}
				if (InputManager.techTree && Progress.HasUnlocked(GeneralUnlocks.TECH_TREE))
				{
					UIBaseSingleton.Get(UITechTree.instance).Init();
				}
			}
			Filters.CheckInput();
			CamController.instance.CamUpdate();
			Sequence.SequenceUpdate();
			if (!mapMode)
			{
				Gameplay.instance.GameplayUpdate();
			}
			else
			{
				Gameplay.instance.MapUpdate();
			}
		}
		float num = Time.deltaTime;
		if (num > GlobalValues.standard.maxDeltaTime)
		{
			num = GlobalValues.standard.maxDeltaTime;
		}
		num *= worldSpeed;
		for (int num2 = allBuildings.Count - 1; num2 >= 0; num2--)
		{
			allBuildings[num2].BuildingUpdate(num, runWorld);
		}
		postponeAllPickupsChange = true;
		foreach (Pickup allPickup in allPickups)
		{
			allPickup.PickupUpdate(num, runWorld);
		}
		postponeAllPickupsChange = false;
		if (pickupsToRemove.Count > 0)
		{
			foreach (Pickup item in pickupsToRemove)
			{
				RemovePickup(item);
			}
			pickupsToRemove.Clear();
		}
		if (pickupsToAdd.Count > 0)
		{
			foreach (Pickup item2 in pickupsToAdd)
			{
				AddPickup(item2);
			}
			pickupsToAdd.Clear();
		}
		if (runWorld)
		{
			gameTime += num;
			float num3 = 1f / GlobalValues.standard.antExpensiveUpdatePerSecond;
			int count = allAnts.Count;
			float num4 = (float)count * (num / num3);
			if (num4 > (float)count)
			{
				num4 = count;
			}
			int num5 = Mathf.FloorToInt(updateSometimesCounter);
			updateSometimesCounter += num4;
			int num6 = Mathf.FloorToInt(updateSometimesCounter);
			if (num6 >= count)
			{
				for (int i = num5; i < count; i++)
				{
					allAnts[i].DoUpdateSometimes(num3);
				}
				num5 = 0;
				num6 -= count;
				updateSometimesCounter -= count;
			}
			for (int j = num5; j < num6; j++)
			{
				allAnts[j].DoUpdateSometimes(num3);
			}
			foreach (Ground ground in grounds)
			{
				ground.GroundUpdate(num);
			}
			EffectArea.Update();
			Ant.TOP_SPEED = 0f;
			for (int num7 = allAnts.Count - 1; num7 >= 0; num7--)
			{
				allAnts[num7].AntUpdate(num);
			}
			for (int num8 = allExplosions.Count - 1; num8 >= 0; num8--)
			{
				allExplosions[num8].EffectUpdate(num);
			}
			if (!theater)
			{
				NuptialFlight.Update(num);
			}
			foreach (KeyValuePair<GameObject, (float, Quaternion)> item3 in new Dictionary<GameObject, (float, Quaternion)>(dicRotatingPointers))
			{
				if (item3.Key == null)
				{
					dicRotatingPointers.Remove(item3.Key);
					continue;
				}
				if (item3.Value.Item1 > 0f)
				{
					dicRotatingPointers[item3.Key] = (item3.Value.Item1 - num, item3.Value.Item2);
					continue;
				}
				item3.Key.transform.rotation = Quaternion.Lerp(item3.Key.transform.rotation, item3.Value.Item2, 10f * num);
				if (Quaternion.Angle(item3.Key.transform.rotation, item3.Value.Item2) < 0.01f)
				{
					item3.Key.transform.rotation = item3.Value.Item2;
					dicRotatingPointers.Remove(item3.Key);
				}
			}
			if (doCheckLinkGates)
			{
				doCheckLinkGates = false;
				CheckLinkGates();
			}
		}
		if (shouldCountPickupInventory)
		{
			shouldCountPickupInventory = false;
			DoCountPickupInventory();
		}
		if (shouldCheckBuildingBuildings)
		{
			shouldCheckBuildingBuildings = false;
			DoCheckBuildingBuildings();
		}
		ConnectableObject.HandleActionPointUpdates();
		Split.HandleDissolves();
		Split.UpdateBillboards();
		if (runEditing)
		{
			Split.UpdateCounterGates();
		}
		if (!theater)
		{
			if (UITechTree.instance != null && UITechTree.instance.isActiveAndEnabled)
			{
				UITechTree.instance.TechTreeUpdate();
				AudioManager.SetGameMusic(MusicType.TechTree, BiomeType.NONE, _busy: false, _polluted: false, 0);
			}
			else if (closestGround != null)
			{
				bool flag = closestGround.GetPollution() > 50f || DebugSettings.standard.musicAlwaysPolluted;
				bool busy = (!flag && closestGround.IsBusy()) || DebugSettings.standard.musicAlwaysBusy;
				int busyness = closestGround.GetBusyness();
				AudioManager.SetGameMusic(mapMode ? MusicType.Map : MusicType.Biome, closestGround.biome.biomeType, busy, flag, busyness);
			}
			else
			{
				AudioManager.SetGameMusic(MusicType.Map, BiomeType.NONE, _busy: false, _polluted: false, 0);
			}
		}
	}

	private void FixedUpdate()
	{
		if (runEditing && !theater)
		{
			Gameplay.instance.GameplayFixedUpdate();
		}
		float xdt = Time.fixedDeltaTime * worldSpeed;
		foreach (Building allFixedUpdateBuilding in allFixedUpdateBuildings)
		{
			allFixedUpdateBuilding.BuildingFixedUpdate(xdt, runWorld);
		}
		if (!runWorld)
		{
			return;
		}
		for (int num = allAnts.Count - 1; num >= 0; num--)
		{
			Ant ant = allAnts[num];
			if (ant.moveState == MoveState.Launched || ant.moveState == MoveState.DeadAndLaunched)
			{
				ant.LaunchingFixedUpdate(xdt);
			}
		}
		if (!theater)
		{
			NuptialFlight.FixedUpdate(xdt);
		}
	}

	private void LateUpdate()
	{
		if (!theater)
		{
			Gameplay.instance.GameplayLateUpdate();
		}
		if (CLOSE_ALL_DIALOGUE)
		{
			CLOSE_ALL_DIALOGUE = false;
		}
	}

	private void OnGUI()
	{
		if (!theater)
		{
			Gameplay.instance.GUI(runEditing);
		}
	}

	public float GetPlaySpeed()
	{
		if (!runWorld)
		{
			return 0f;
		}
		return worldSpeed;
	}

	public void InitAnimator(Animator anim, AnimationCulling culling, Renderer renderer_delayed = null)
	{
		if (!(anim == null))
		{
			pausableAnimators.Add(anim);
			switch (culling)
			{
			case AnimationCulling.Never:
				anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				break;
			case AnimationCulling.Always:
				anim.cullingMode = AnimatorCullingMode.CullCompletely;
				break;
			case AnimationCulling.Delayed:
				anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				renderer_delayed.gameObject.AddComponent<AnimCullDelayer>();
				break;
			}
		}
	}

	public void RemovePausableAnimator(Animator anim)
	{
		if (anim != null)
		{
			pausableAnimators.Remove(anim);
		}
	}

	public void AddPausableParticles(ParticleSystem ps)
	{
		if (ps != null)
		{
			pausableParticles.Add(ps);
		}
	}

	public void RemovePausableParticles(ParticleSystem ps)
	{
		if (ps != null)
		{
			pausableParticles.Remove(ps);
		}
	}

	public void SetStatus(GameStatus status)
	{
		if (gameStatus == status)
		{
			return;
		}
		if (gameStatus == GameStatus.RUNNING || gameStatus == GameStatus.PAUSED || gameStatus == GameStatus.BUSY_SYS || gameStatus == GameStatus.PASSIVE)
		{
			lastPlayTimeStatus = gameStatus;
		}
		bool flag = runWorld;
		bool flag2 = runEditing;
		gameStatus = status;
		runWorld = gameStatus == GameStatus.RUNNING || gameStatus == GameStatus.PASSIVE;
		runEditing = gameStatus == GameStatus.RUNNING || gameStatus == GameStatus.PAUSED;
		if (!runWorld && flag)
		{
			CheckPausables();
			foreach (Animator pausableAnimator in pausableAnimators)
			{
				pausableAnimator.StartPlayback();
			}
			foreach (ParticleSystem pausableParticle in pausableParticles)
			{
				if (pausableParticle.isPlaying)
				{
					pausableParticle.Pause();
				}
			}
			Shader.SetGlobalFloat("_GlobalShaderSpeed", 0f);
			UIGame.instance.SetPause(target: true);
			UpdateWorldMuted();
			AudioManager.SetPause(paused: true);
		}
		if (runWorld && !flag)
		{
			CheckPausables();
			foreach (Animator pausableAnimator2 in pausableAnimators)
			{
				pausableAnimator2.StopPlayback();
			}
			foreach (ParticleSystem pausableParticle2 in pausableParticles)
			{
				if (pausableParticle2.isPaused)
				{
					pausableParticle2.Play();
				}
			}
			Shader.SetGlobalFloat("_GlobalShaderSpeed", 1f);
			if (UIGame.instance != null)
			{
				UIGame.instance.SetPause(target: false);
			}
			instance.CheckBuildingBuildings();
			UpdateWorldMuted();
			AudioManager.SetPause(paused: false);
		}
		if (!runEditing && flag2)
		{
			Gameplay.instance.StopPlaying();
			worldSpeed = 1f;
		}
		if (runEditing && !flag2)
		{
			if (UIGame.instance != null)
			{
				UIGame.instance.CountInventory();
			}
			if (Gameplay.instance != null)
			{
				Gameplay.instance.ClearFocus();
			}
		}
		if ((gameStatus == GameStatus.INIT || gameStatus == GameStatus.STOPPED) && !theater)
		{
			AudioManager.StopMusic();
		}
	}

	public void UpdateWorldMuted()
	{
		AudioManager.SetWorldMuted(!runWorld || theater || mapMode);
	}

	private void CheckPausables()
	{
		bool flag = false;
		foreach (Animator pausableAnimator in pausableAnimators)
		{
			if (pausableAnimator == null)
			{
				flag = true;
			}
		}
		if (flag)
		{
			List<Animator> list = new List<Animator>(pausableAnimators);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (list[num] == null)
				{
					list.RemoveAt(num);
				}
			}
			pausableAnimators = new HashSet<Animator>(list);
		}
		flag = false;
		foreach (ParticleSystem pausableParticle in pausableParticles)
		{
			if (pausableParticle == null)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			return;
		}
		List<ParticleSystem> list2 = new List<ParticleSystem>(pausableParticles);
		for (int num2 = list2.Count - 1; num2 >= 0; num2--)
		{
			if (list2[num2] == null)
			{
				list2.RemoveAt(num2);
			}
		}
		pausableParticles = new HashSet<ParticleSystem>(list2);
	}

	public GameStatus GetStatus()
	{
		return gameStatus;
	}

	private void ResetTotalPickupInventory()
	{
		dicTotalPickupInventory.Clear();
		foreach (PickupType item in PickupData.EAllPickupTypes())
		{
			dicTotalPickupInventory.Add(item, 0);
		}
	}

	public void UpdatePickupInventory()
	{
		CountPickupInventory();
		CheckBuildingBuildings();
	}

	public void CountPickupInventory()
	{
		shouldCountPickupInventory = true;
	}

	private void DoCountPickupInventory()
	{
		ResetTotalPickupInventory();
		Dictionary<PickupType, int> pickupInventory = new Dictionary<PickupType, int>();
		foreach (Ground ground in grounds)
		{
			Dictionary<PickupType, int> dictionary = ground.UpdateInventory(ref dicTotalPickupInventory);
			if (ground == closestGround)
			{
				pickupInventory = dictionary;
			}
		}
		if (UIGame.instance != null && !UIGame.instance.AntInventory())
		{
			if (mapMode)
			{
				UIGame.instance.SetPickupInventory(dicTotalPickupInventory);
			}
			else
			{
				UIGame.instance.SetPickupInventory(pickupInventory);
			}
		}
	}

	public void CountAntInventory()
	{
		if (UIGame.instance == null || !UIGame.instance.AntInventory())
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<PickupType, int> dictionary2 = new Dictionary<PickupType, int>();
		Dictionary<AntCaste, int> dictionary3 = new Dictionary<AntCaste, int>();
		if (mapMode)
		{
			foreach (Building item in EBuildings())
			{
				if (!item.countAsAnt)
				{
					continue;
				}
				if (item is Incubator incubator)
				{
					FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(incubator.GetProcessingRecipe());
					if (factoryRecipeData != null)
					{
						if (!dictionary2.ContainsKey(factoryRecipeData.costsPickup[0].type))
						{
							dictionary2.Add(factoryRecipeData.costsPickup[0].type, 0);
						}
						dictionary2[factoryRecipeData.costsPickup[0].type]++;
					}
				}
				else
				{
					if (!dictionary.ContainsKey(item.data.code))
					{
						dictionary.Add(item.data.code, 0);
					}
					dictionary[item.data.code]++;
				}
			}
			foreach (Pickup allLarva in allLarvae)
			{
				if (!dictionary2.ContainsKey(allLarva.type))
				{
					dictionary2.Add(allLarva.type, 0);
				}
				dictionary2[allLarva.type]++;
			}
			foreach (Ant allAnt in allAnts)
			{
				if (!allAnt.IsDead())
				{
					if (!dictionary3.ContainsKey(allAnt.caste))
					{
						dictionary3.Add(allAnt.caste, 0);
					}
					dictionary3[allAnt.caste]++;
				}
			}
			foreach (AntCaste item2 in History.EAntCastes())
			{
				if (!dictionary3.ContainsKey(item2))
				{
					dictionary3.Add(item2, 0);
				}
			}
		}
		else if (closestGround != null)
		{
			foreach (Building item3 in closestGround.EBuildings())
			{
				if (!item3.countAsAnt)
				{
					continue;
				}
				if (item3 is Incubator incubator2)
				{
					FactoryRecipeData factoryRecipeData2 = FactoryRecipeData.Get(incubator2.GetProcessingRecipe());
					if (factoryRecipeData2 != null)
					{
						if (!dictionary2.ContainsKey(factoryRecipeData2.costsPickup[0].type))
						{
							dictionary2.Add(factoryRecipeData2.costsPickup[0].type, 0);
						}
						dictionary2[factoryRecipeData2.costsPickup[0].type]++;
					}
				}
				else
				{
					if (!dictionary.ContainsKey(item3.data.code))
					{
						dictionary.Add(item3.data.code, 0);
					}
					dictionary[item3.data.code]++;
				}
			}
			foreach (Pickup allLarva2 in allLarvae)
			{
				if (Toolkit.GetGround(allLarva2.transform.position) == closestGround)
				{
					if (!dictionary2.ContainsKey(allLarva2.type))
					{
						dictionary2.Add(allLarva2.type, 0);
					}
					dictionary2[allLarva2.type]++;
				}
			}
			foreach (Ant item4 in closestGround.EAnts())
			{
				if (!item4.IsDead())
				{
					if (!dictionary3.ContainsKey(item4.caste))
					{
						dictionary3.Add(item4.caste, 0);
					}
					dictionary3[item4.caste]++;
				}
			}
		}
		UIGame.instance.SetAntInventory(dictionary, dictionary2, dictionary3);
	}

	public void CheckBuildingBuildings()
	{
		shouldCheckBuildingBuildings = true;
	}

	private void DoCheckBuildingBuildings()
	{
		List<string> list = null;
		int count = buildingsBeingBuilt.Count;
		if (count == 0)
		{
			return;
		}
		Building[] array = new Building[count];
		int num = Time.frameCount % count;
		foreach (Building item2 in buildingsBeingBuilt)
		{
			array[num++] = item2;
			if (num >= count)
			{
				num = 0;
			}
		}
		for (int i = 0; i < count; i++)
		{
			Building building = array[i];
			if (building.IsPaused())
			{
				continue;
			}
			string item = $"{building.ground.GetInstanceID()}_{building.data.code}";
			if (list != null && list.Contains(item))
			{
				continue;
			}
			building.GetDicsRequiredPickups(out var dic_types, out var _);
			foreach (KeyValuePair<PickupType, int> item3 in dic_types)
			{
				if (!TryExchangePickupTypeFromInventoryToBuilding(item3.Key, building, item3.Value))
				{
					if (list == null)
					{
						list = new List<string>();
					}
					list.Add(item);
				}
			}
		}
	}

	private bool TryExchangePickupTypeFromInventoryToBuilding(PickupType pickup_type, Building target, int count)
	{
		List<Ground> list = new List<Ground> { target.ground };
		if (target is Bridge bridge)
		{
			Ground otherGround = bridge.GetOtherGround();
			if (otherGround != target.ground)
			{
				list.Add(otherGround);
			}
		}
		if (Player.crossIslandBuilding)
		{
			foreach (Ground item in EGrounds())
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		int num = 0;
		foreach (Ground item2 in list)
		{
			if (item2 != null)
			{
				num += item2.GetInventoryAmount(pickup_type);
			}
		}
		List<Stockpile> list2 = new List<Stockpile>();
		foreach (Stockpile item3 in EStockpiles())
		{
			if (item3.crossIsland && !list.Contains(item3.ground))
			{
				int collectedAmount = item3.GetCollectedAmount(pickup_type, BuildingStatus.COMPLETED, include_incoming: false);
				num += collectedAmount;
				if (collectedAmount > 0)
				{
					list2.Add(item3);
				}
			}
		}
		if (num == 0)
		{
			return false;
		}
		count = Mathf.Clamp(num, 0, count);
		List<Stockpile> list3 = new List<Stockpile>();
		foreach (Ground item4 in list)
		{
			list3.AddRange(item4.EStockpilesForExtract(pickup_type, only_open_to_smart: false));
		}
		list3.AddRange(list2);
		if (list3.Count == 0)
		{
			return false;
		}
		Dictionary<Storage, float> distances = new Dictionary<Storage, float>();
		float x = target.GetInsertPos().x;
		float z = target.GetInsertPos().z;
		foreach (Stockpile item5 in list3)
		{
			float num2 = item5.transform.position.x - x;
			float num3 = item5.transform.position.z - z;
			distances.Add(item5, num2 * num2 + num3 * num3);
		}
		list3.Sort((Stockpile s1, Stockpile s2) => distances[s1].CompareTo(distances[s2]));
		int num4 = 0;
		while (num4 < count)
		{
			if (list3.Count == 0)
			{
				return false;
			}
			if (!list3[0].HasExtractablePickup(ExchangeType.BUILDING_OUT, pickup_type))
			{
				list3.RemoveAt(0);
				continue;
			}
			Pickup pickup = list3[0].ExtractPickup(pickup_type);
			if (pickup == null)
			{
				Debug.LogWarning("Failed exchanging pickup with type " + pickup_type.ToString() + " from " + list3[0].name + " to " + target.name);
				list3.RemoveAt(0);
			}
			else
			{
				pickup.SetWoosh(active: true);
				pickup.Exchange(target, target.GetInsertPos(), ExchangeAnimationType.SHOOT, UnityEngine.Random.Range(0f, 0.5f));
			}
			num4++;
		}
		return true;
	}

	public bool TryExchangePickupToInventory(Ground ground, Vector3 near_pos, Pickup p, Storage exclude_stockpile = null, bool teleport = false, bool exclude_empty_stockpiles = false)
	{
		if (ground == null)
		{
			return false;
		}
		Stockpile stockpileForInsert = ground.GetStockpileForInsert(p.type, near_pos, exclude_stockpile, exclude_empty_stockpiles);
		if (stockpileForInsert == null)
		{
			return false;
		}
		p.Exchange(stockpileForInsert, stockpileForInsert.GetInsertPos(p), teleport ? ExchangeAnimationType.TELEPORT : ExchangeAnimationType.SHOOT, UnityEngine.Random.Range(0f, 0.5f));
		return true;
	}

	public void AddBuildingBuilding(Building _build)
	{
		if (!buildingsBeingBuilt.Contains(_build))
		{
			buildingsBeingBuilt.Add(_build);
		}
	}

	public void RemoveBuildingBuilding(Building _build)
	{
		if (buildingsBeingBuilt.Contains(_build))
		{
			buildingsBeingBuilt.Remove(_build);
		}
	}

	public int GetNPickupsInInventory(PickupType _type)
	{
		switch (_type)
		{
		case PickupType.NONE:
			return 0;
		case PickupType.ANY:
		{
			int num = 0;
			{
				foreach (KeyValuePair<PickupType, int> item in dicTotalPickupInventory)
				{
					num += item.Value;
				}
				return num;
			}
		}
		default:
			return dicTotalPickupInventory[_type];
		}
	}

	public void UpdateAntCount()
	{
		int num = antCount;
		antCount = allLarvae.Count;
		foreach (Ant allAnt in allAnts)
		{
			if (!allAnt.IsDead())
			{
				antCount++;
			}
		}
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.countAsAnt)
			{
				antCount++;
			}
		}
		if (num < 100 && antCount >= 100)
		{
			Platform.current.GainAchievement(Achievement.POP_100);
		}
		if (num < 500 && antCount >= 500)
		{
			Platform.current.GainAchievement(Achievement.POP_500);
		}
		foreach (Ground ground in grounds)
		{
			ground.UpdateAntCount();
		}
	}

	public int GetAntCount()
	{
		return antCount;
	}

	public IEnumerable<Ant> EAnts()
	{
		foreach (Ant allAnt in allAnts)
		{
			yield return allAnt;
		}
	}

	public int GetAntIndex(Ant ant)
	{
		return allAnts.IndexOf(ant);
	}

	public Ant GetNextAntOfCaste(AntCaste caste, ref int cur_index, Ground ground)
	{
		int count = allAnts.Count;
		if (count == 0)
		{
			return null;
		}
		for (int i = cur_index + 1; i < count; i++)
		{
			if (allAnts[i].caste == caste && allAnts[i].currentGround == ground)
			{
				cur_index = i;
				return allAnts[i];
			}
		}
		if (cur_index < count)
		{
			for (int j = 0; j <= cur_index; j++)
			{
				if (allAnts[j].caste == caste && allAnts[j].currentGround == ground)
				{
					cur_index = j;
					return allAnts[j];
				}
			}
		}
		cur_index = -1;
		return null;
	}

	public Building GetNextBuildingWithCode(string code, ref int cur_index, Ground ground)
	{
		if (code != prevSeenBuildingCode)
		{
			cur_index = -1;
		}
		prevSeenBuildingCode = code;
		int count = allBuildings.Count;
		if (count == 0)
		{
			return null;
		}
		for (int i = cur_index + 1; i < count; i++)
		{
			if (allBuildings[i].data.code == code && allBuildings[i].ground == ground)
			{
				cur_index = i;
				return allBuildings[i];
			}
		}
		if (cur_index < count)
		{
			for (int j = 0; j <= cur_index; j++)
			{
				if (allBuildings[j].data.code == code && allBuildings[j].ground == ground)
				{
					cur_index = j;
					return allBuildings[j];
				}
			}
		}
		cur_index = -1;
		return null;
	}

	public Pickup GetNextPickupOfType(PickupType pickup_type, ref int cur_index, Ground ground)
	{
		if (pickup_type != prevSeenPickupType)
		{
			cur_index = -1;
		}
		prevSeenPickupType = pickup_type;
		for (int i = 0; i < 2; i++)
		{
			int num = -1;
			foreach (Pickup allPickup in allPickups)
			{
				num++;
				if (((i != 0 || num < cur_index + 1) && (i != 1 || num > cur_index)) || allPickup.type != pickup_type)
				{
					continue;
				}
				Vector3 position = allPickup.transform.position;
				Stockpile stockpile = null;
				if (Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out var hitInfo, 110f, Toolkit.Mask(Layers.Buildings)))
				{
					stockpile = hitInfo.transform.GetComponentInParent<Stockpile>();
				}
				if (stockpile == null)
				{
					if (Toolkit.GetGround(position) != ground)
					{
						continue;
					}
				}
				else
				{
					if (stockpile.ground != ground || seen_stockpiles.Contains(stockpile))
					{
						continue;
					}
					seen_stockpiles.Add(stockpile);
				}
				cur_index = num;
				return allPickup;
			}
			if (i == 0)
			{
				seen_stockpiles.Clear();
			}
		}
		cur_index = -1;
		return null;
	}

	public bool IsAntCasteInScene(AntCaste _caste)
	{
		foreach (Ant allAnt in allAnts)
		{
			if (allAnt.data.caste == _caste)
			{
				return true;
			}
		}
		return false;
	}

	public int GetNPickupsCarried(PickupType _type)
	{
		int num = 0;
		foreach (Ant allAnt in allAnts)
		{
			foreach (PickupType item in allAnt.ECarryingPickupTypes())
			{
				if (item == _type || _type == PickupType.ANY)
				{
					num++;
				}
			}
		}
		return num;
	}

	public int GetPopulationIntensity()
	{
		return Mathf.FloorToInt(antCount / 25);
	}

	public IEnumerable<Building> EBuildings()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced())
			{
				yield return allBuilding;
			}
		}
	}

	public IEnumerable<Building> EBuildings(string code)
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced() && allBuilding.data.code == code)
			{
				yield return allBuilding;
			}
		}
	}

	public int GetBuildingCount(string code, bool only_completed = false)
	{
		int num = 0;
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced() && allBuilding.data.code == code && ((allBuilding.currentStatus == BuildingStatus.BUILDING && !only_completed) || allBuilding.currentStatus == BuildingStatus.COMPLETED))
			{
				num++;
			}
		}
		return num;
	}

	public bool IsBuildingInScene(string code, bool only_completed = false)
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced() && allBuilding.data.code == code && ((allBuilding.currentStatus == BuildingStatus.BUILDING && !only_completed) || allBuilding.currentStatus == BuildingStatus.COMPLETED))
			{
				return true;
			}
		}
		return false;
	}

	public void RefreshUnlocksBuildings()
	{
		foreach (Building allBuilding in allBuildings)
		{
			allBuilding.Refresh();
		}
	}

	public List<FlightPad> GetAllLaunchPads()
	{
		List<FlightPad> list = new List<FlightPad>();
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced() && allBuilding is FlightPad { launchPad: not false } flightPad)
			{
				list.Add(flightPad);
			}
		}
		return list;
	}

	public IEnumerable<FlightPad> ELaunchPads()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced() && allBuilding is FlightPad { launchPad: not false } flightPad)
			{
				yield return flightPad;
			}
		}
	}

	public IEnumerable<Catapult> ECatapults()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is Catapult catapult && allBuilding.IsPlaced())
			{
				yield return catapult;
			}
		}
	}

	public IEnumerable<Stockpile> EStockpiles()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is Stockpile stockpile && allBuilding.IsPlaced())
			{
				yield return stockpile;
			}
		}
	}

	public IEnumerable<BatteryBuilding> EBatteryBuildings()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is BatteryBuilding batteryBuilding && allBuilding.IsPlaced())
			{
				yield return batteryBuilding;
			}
		}
	}

	public IEnumerable<DispenserRegular> EDispensers()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is DispenserRegular dispenserRegular && dispenserRegular.IsPlaced())
			{
				yield return dispenserRegular;
			}
		}
	}

	public IEnumerable<Beacon> EBeacons()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is Beacon beacon && beacon.IsPlaced())
			{
				yield return beacon;
			}
		}
	}

	public IEnumerable<Unlocker> EUnlockers()
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is Unlocker unlocker && unlocker.IsPlaced() && unlocker.unlockerType == UnlockerType.Unlocker)
			{
				yield return unlocker;
			}
		}
	}

	public IEnumerable<T> EBuildings<T>() where T : Building
	{
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding is T val && val.IsPlaced())
			{
				yield return val;
			}
		}
	}

	public int GetQueenCount()
	{
		return allQueens.Count;
	}

	public IEnumerable<Queen> EQueens()
	{
		foreach (Queen allQueen in allQueens)
		{
			yield return allQueen;
		}
	}

	public IEnumerable<Pickup> EAllPickups()
	{
		foreach (Pickup allPickup in allPickups)
		{
			yield return allPickup;
		}
	}

	public IEnumerable<Pickup> EAllPickupsOfType(PickupType _type)
	{
		foreach (Pickup allPickup in allPickups)
		{
			if (allPickup.type == _type)
			{
				yield return allPickup;
			}
		}
	}

	public int GetTrailCount()
	{
		return allTrails.Count;
	}

	public IEnumerable<Trail> ETrails()
	{
		foreach (Trail allTrail in allTrails)
		{
			yield return allTrail;
		}
	}

	public IEnumerable<TrailGate> ETrailGates()
	{
		foreach (TrailGate allTrailGate in allTrailGates)
		{
			yield return allTrailGate;
		}
	}

	public void StartCheckLinkGates()
	{
		doCheckLinkGates = true;
	}

	private void CheckLinkGates()
	{
		foreach (TrailGate allTrailGate in allTrailGates)
		{
			if (allTrailGate is TrailGate_Link trailGate_Link)
			{
				trailGate_Link.CheckLinkedAnts();
			}
		}
	}

	public IEnumerable<TrailGate_Link> GetLinkedGates(Ant _ant)
	{
		foreach (TrailGate allTrailGate in allTrailGates)
		{
			if (!(allTrailGate is TrailGate_Link trailGate_Link))
			{
				continue;
			}
			foreach (ClickableObject item in trailGate_Link.EAssignedObjects())
			{
				if (item == _ant)
				{
					yield return trailGate_Link;
					break;
				}
			}
		}
	}

	public void RemoveAntFromLinkGates(Ant _ant)
	{
		foreach (TrailGate allTrailGate in allTrailGates)
		{
			if (allTrailGate is TrailGate_Link)
			{
				allTrailGate.Assign(_ant, add: false);
			}
		}
	}

	public IEnumerable<BiomeObject> EBiomeObjects(Ground _ground)
	{
		foreach (BiomeObject allBiomeObject in allBiomeObjects)
		{
			if (allBiomeObject.ground == _ground)
			{
				yield return allBiomeObject;
			}
		}
	}

	public bool IsMenuOpen()
	{
		return uiEscMenu.isActiveAndEnabled;
	}

	public void CloseAllMenuUI(bool resume_last_gamestate)
	{
		uiEscMenu.Show(target: false);
		if (UILoadSave.instance != null)
		{
			UILoadSave.instance.Show(target: false);
		}
		if (UITechTree.instance != null)
		{
			UITechTree.instance.Show(target: false);
		}
		if (UISettings.instance != null)
		{
			UISettings.instance.StartClose();
		}
		if (UIReportScreen.instance != null)
		{
			UIReportScreen.instance.Show(target: false);
		}
		if (UIFeedback.instance != null)
		{
			UIFeedback.instance.Show(target: false);
		}
		if (UIBlueprints.instance != null)
		{
			UIBlueprints.instance.Show(target: false);
		}
		CLOSE_ALL_DIALOGUE = true;
		if (resume_last_gamestate)
		{
			SetStatus(lastPlayTimeStatus);
			Filters.Update(Filter.HIDE_UI);
		}
	}

	public void ShowReportScreenDelayed(int nup_flight)
	{
		StartCoroutine(CShowReportScreen(nup_flight));
	}

	private IEnumerator CShowReportScreen(int nup_flight)
	{
		yield return new WaitForSeconds(10f);
		UIBaseSingleton.Get(UIReportScreen.instance).Init(nup_flight);
	}

	private IEnumerator CShowPlaytestWelcome()
	{
		yield return new WaitForSeconds(3f);
		UIBaseSingleton.Get(UITutorial.instance).Init(Tutorial.PLAYTEST_WELCOME, log_mode: false);
	}

	public void OpenEscMenu()
	{
		uiEscMenu.transform.SetAsLastSibling();
		uiEscMenu.Show(target: true);
		Filters.Update(Filter.HIDE_UI);
		SetStatus(GameStatus.MENU);
		UIGlobal.SetHardwareCursor();
	}

	public void AddBilboard(Billboard bb, bool target)
	{
		if (target)
		{
			if (!activeBillboards.Contains(bb))
			{
				activeBillboards.Add(bb);
			}
		}
		else if (activeBillboards.Contains(bb))
		{
			activeBillboards.Remove(bb);
		}
	}

	private void UpdateBillboards()
	{
		for (int num = activeBillboards.Count - 1; num >= 0; num--)
		{
			Billboard billboard = activeBillboards[num];
			if (billboard == null)
			{
				activeBillboards.RemoveAt(num);
			}
			else
			{
				billboard.BillboardUpdate();
			}
		}
	}

	public void RotatePointer(GameObject _pointer, Quaternion target_rot)
	{
		if (dicRotatingPointers.ContainsKey(_pointer))
		{
			if (_pointer.transform.rotation != dicRotatingPointers[_pointer].Item2)
			{
				_pointer.transform.rotation = dicRotatingPointers[_pointer].Item2;
			}
			dicRotatingPointers[_pointer] = (0.5f, target_rot);
		}
		else
		{
			dicRotatingPointers.Add(_pointer, (0.5f, target_rot));
		}
	}

	private IEnumerable<ClickableObject> EAllAssignments()
	{
		foreach (Catapult item in ECatapults())
		{
			yield return item;
		}
		foreach (DispenserRegular item2 in EDispensers())
		{
			yield return item2;
		}
		foreach (TrailGate item3 in ETrailGates())
		{
			yield return item3;
		}
		foreach (FlightPad item4 in ELaunchPads())
		{
			yield return item4;
		}
	}

	public void SetMap(bool map)
	{
		mapMode = map;
		if (mapMode)
		{
			Lighting.instance.Apply(GlobalValues.standard.mapLighting);
		}
		else
		{
			UpdateBiomeInfluence();
		}
		Filters.Update(Filter.FLOATING_TRAILS);
		UIHoverClickOb.instance.transform.localScale = (map ? Vector3.zero : Vector3.one);
		if (map)
		{
			UIGame.instance.uiBuildingMenu.Show(target: false);
			UIGame.instance.uiClick.Show(target: false);
			hiddenDuringMap = 0;
			if (UIGame.instance.uiLogicControl.IsShown())
			{
				hiddenDuringMap |= 1;
				UIGame.instance.uiLogicControl.Show(target: false);
			}
			if (UIFloorSelection.instance != null && UIFloorSelection.instance.IsShown())
			{
				hiddenDuringMap |= 2;
				UIFloorSelection.instance.Show(target: false);
			}
		}
		else
		{
			Gameplay.instance.UpdateSelected(refresh: true);
			if ((hiddenDuringMap & 1) != 0)
			{
				UIGame.instance.uiLogicControl.Show(target: true);
			}
			if ((hiddenDuringMap & 2) != 0)
			{
				UIFloorSelection.instance.Show(target: true);
			}
		}
		UIGame.instance.CountInventory();
		UIGame.instance.SetInventoryTitle();
		UpdateWorldMuted();
		UIGame.instance.ShowGraph(map && UIGame.instance.AntInventory() && gameStatus != GameStatus.BUSY_SYS);
		UIGame.instance.UpdateNoticeInventory();
	}

	public void UpdateBiomeInfluence()
	{
		closestGround = null;
		if (grounds.Count == 0 || theater)
		{
			return;
		}
		Vector3 position = CamController.instance.transform.position;
		List<(Ground, float)> list = new List<(Ground, float)>();
		foreach (Ground ground in grounds)
		{
			float item = ground.DistanceToShape(position);
			list.Add((ground, item));
		}
		list.Sort(((Ground, float) a, (Ground, float) b) => a.Item2.CompareTo(b.Item2));
		closestGround = list[0].Item1;
		if (mapMode)
		{
			return;
		}
		float num = 150f;
		float item2 = list[0].Item2;
		float num2 = ((list.Count == 1) ? (num * 2f) : list[1].Item2);
		if (item2 == 0f)
		{
			ambienceFactor = 1f;
			Lighting.instance.Apply(closestGround.biome.lighting);
		}
		else
		{
			float num3 = (item2 + num2) * 0.5f;
			if (num3 > num)
			{
				num3 = num;
			}
			ambienceFactor = Mathf.Clamp01(1f - item2 / num3);
			Lighting.instance.Apply(BiomeLighting.Lerp(GlobalValues.standard.spaceLighting, closestGround.biome.lighting, ambienceFactor));
		}
		SetAmbienceAudio();
	}

	public void SetAmbienceAudio()
	{
		AudioManager.SetAmbience((closestGround != null) ? closestGround.biome.biomeType : BiomeType.NONE, ambienceFactor);
	}

	private void AddGround(Ground ground)
	{
		grounds.Add(ground);
		Circle globalShapeCircle = ground.globalShapeCircle;
		globalShapeCircle.pos += ground.transform.position;
		groundsCircle = ((groundsCircle.radius == 0f) ? globalShapeCircle : new Circle(groundsCircle, globalShapeCircle));
	}

	public IEnumerable<Ground> EGrounds()
	{
		foreach (Ground ground in grounds)
		{
			yield return ground;
		}
	}

	public Ground GetGroundInstance(int id)
	{
		if (id == -10)
		{
			return null;
		}
		if (id < 0 || id >= grounds.Count)
		{
			Debug.LogError($"Ground instance with id {id} doesn't exist");
			return null;
		}
		return grounds[id];
	}

	public int GetGroundInstanceId(Ground ground)
	{
		if (ground == null)
		{
			return -10;
		}
		int num = grounds.IndexOf(ground);
		if (num < 0)
		{
			Debug.LogError("Can't get instance id for ground " + ((ground == null) ? "NULL" : ground.name));
		}
		return num;
	}

	public void LimitCameraTarget(ref Vector3 pos)
	{
		if ((pos - groundsCircle.pos).sqrMagnitude > groundsCircle.radiusSq)
		{
			pos = groundsCircle.pos + (pos - groundsCircle.pos).normalized * groundsCircle.radius;
		}
	}

	public Vector3 GetRandomPosOnEdge()
	{
		Vector3 pos = groundsCircle.pos;
		float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
		pos.x += groundsCircle.radius * Mathf.Cos(f);
		pos.z += groundsCircle.radius * Mathf.Sin(f);
		return pos;
	}

	public IEnumerator KStartEnvironment()
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			grounds = new List<Ground>();
			string address = TechTree.GetNextBiomesToSpawn(0)[0];
			Biome biome = null;
			yield return StartKoroutine(KGetBiome(address, delegate(Biome result)
			{
				biome = result;
			}));
			if (!(biome == null))
			{
				int seedInt = WorldSettings.seedInt;
				int num = 0;
				Toolkit.SetRandomSeed(seedInt, num);
				Vector3 zero = Vector3.zero;
				Quaternion ground_rot = Toolkit.RandomYRotation();
				int num2 = biome.PickGroundIndex();
				biome.groundPrefabs[num2].InitShape(ground_rot, force: true);
				Ground ground = Ground.Create(biome, zero, ground_rot, num2);
				yield return StartKoroutine(kid, ground.KFill(spawnParent, seedInt, num + 1, biome.spawnUnlocker));
				ground.biomeAddress = address;
				yield return StartKoroutine(kid, ground.KGenerateEcology());
				AddGround(ground);
				Toolkit.ResetRandomState();
				UpdateBiomeInfluence();
				yield return null;
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void CleanUpAll()
	{
		pausableAnimators.Clear();
		DestroyAllInHashSet(ref allPickups);
		allLarvae.Clear();
		foreach (Trail allTrail in allTrails)
		{
			allTrail.DeleteGate();
		}
		DestroyAllInHashSet(ref allTrails);
		DestroyAllInHashSet(ref allSplits);
		DestroyAllInList(ref allBuildings);
		allFixedUpdateBuildings.Clear();
		buildingsBeingBuilt.Clear();
		DestroyAllInList(ref allQueens);
		DestroyAllInList(ref allAnts);
		DestroyAllInHashSet(ref allBiomeObjects);
		foreach (Ground ground in grounds)
		{
			ground.Delete();
		}
		grounds.Clear();
		groundsCircle = default(Circle);
		updateSometimesCounter = 0f;
		BlueprintManager.Clear();
		Instinct.Clear();
		Progress.Clear();
		NuptialFlight.Clear();
		TechTree.Clear();
		CamController.Clear();
		AudioManager.Clear();
		BuildingConfig.ClearClipboard();
		TrailGate.ClearClipboard();
		CloseAllMenuUI(resume_last_gamestate: false);
	}

	public void DestroyAllInList<T>(ref List<T> list) where T : MonoBehaviour
	{
		for (int num = list.Count - 1; num >= 0; num--)
		{
			T val = list[num];
			if (val != null)
			{
				UnityEngine.Object.Destroy(val.gameObject);
			}
		}
		list.Clear();
	}

	public void DestroyAllInHashSet<T>(ref HashSet<T> set) where T : MonoBehaviour
	{
		List<T> list = new List<T>(set);
		DestroyAllInList(ref list);
		set.Clear();
	}

	private IEnumerator KGetBiome(string address, Action<Biome> funcResult)
	{
		Biome result = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			funcResult(result);
		});
		try
		{
			if (!address.StartsWith("Biomes/"))
			{
				address = "Biomes/" + address;
			}
			AsyncOperationHandle<Biome> loading = Addressables.LoadAssetAsync<Biome>(address);
			yield return loading;
			result = loading.Result;
			if (result == null)
			{
				Debug.LogError("Unknown biome address '" + address + "'");
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public string AddBiome(bool with_anim = true)
	{
		return AddBiome(TechTree.GetNextBiomesToSpawn(instance.GetGroundCount()), with_anim);
	}

	public string AddBiome(string biome_address, bool with_anim = true)
	{
		List<string> list = new List<string>();
		list.Add(biome_address);
		return AddBiome(list, with_anim);
	}

	public string AddBiome(List<string> biome_addresses, bool with_anim = true)
	{
		int seedInt = WorldSettings.seedInt;
		int num = GetGroundCount() * 10;
		Toolkit.SetRandomSeed(seedInt, num);
		string text = biome_addresses[UnityEngine.Random.Range(0, biome_addresses.Count)];
		cAddBiome = StartKoroutine(KAddBiome(text, seedInt, num + 1, with_anim));
		return text;
	}

	private IEnumerator KAddBiome(string biome_address, int generation_seed, int generation_seed_index, bool with_anim)
	{
		bool was_paused = GetStatus() == GameStatus.PAUSED;
		SetStatus(GameStatus.BUSY_SYS);
		Ground ground = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			if (ground != null)
			{
				ground.SetDissolve(0f);
			}
			GameStatus status = ((!was_paused) ? GameStatus.RUNNING : GameStatus.PAUSED);
			if (gameStatus == GameStatus.BUSY_SYS)
			{
				SetStatus(status);
			}
			if (gameStatus == GameStatus.MENU && lastPlayTimeStatus == GameStatus.BUSY_SYS)
			{
				lastPlayTimeStatus = status;
			}
			UIGame.instance.ShowGraph(mapMode && UIGame.instance.AntInventory());
			cAddBiome = null;
			UIGame.instance.SetRevealIsland();
		});
		try
		{
			_ = KoroutineId.empty;
			if (DebugSettings.standard.alsoAnimateOnCheatSpawnBiome)
			{
				with_anim = true;
			}
			Biome biome = null;
			yield return StartKoroutine(kid, KGetBiome(biome_address, delegate(Biome result)
			{
				biome = result;
			}));
			if (biome == null)
			{
				yield break;
			}
			Quaternion rot = Toolkit.RandomYRotation();
			int ground_index = biome.PickGroundIndex();
			Ground ground2 = biome.groundPrefabs[ground_index];
			ground2.InitShape(rot, force: true);
			Vector3 pos = Vector3.zero;
			yield return StartKoroutine(kid, KFindGroundSpawnLocation(ground2, delegate(Vector3 res)
			{
				pos = res;
			}));
			float target_pitch = 55f;
			float target_zoom = 0.8f;
			if (with_anim)
			{
				AudioManager.PlayUI(UISfx.IslandAppearCamWoosh);
				KoroutineId kid2;
				yield return StartKoroutine(kid, CamController.instance.KCamAnim(2f, pos, target_pitch, null, target_zoom, Ease.InOut), out kid2);
			}
			ground = Ground.Create(biome, pos, rot, ground_index);
			if (with_anim)
			{
				ground.SetDissolve(1f);
			}
			UIGame.instance.SetWaitImage(active: true);
			AudioManager.PlayUILoop(UISfx.IslandLoading);
			yield return null;
			yield return StartKoroutine(kid, ground.KFill(spawnParent, generation_seed, generation_seed_index, biome.spawnUnlocker));
			yield return StartKoroutine(kid, ground.KGenerateEcology());
			ground.biomeAddress = biome_address;
			AddGround(ground);
			Toolkit.ResetRandomState();
			UpdateBiomeInfluence();
			AudioManager.StopUILoop();
			yield return null;
			UIGame.instance.SetWaitImage(active: false);
			if (with_anim)
			{
				AudioManager.PlayUI(AudioLinks.standard.GetClipIslandAppears(biome.biomeType));
				float appear_dur = 4f;
				for (float f = 0f; f < 1f; f += Time.deltaTime / appear_dur)
				{
					ground.SetDissolve(1f - f);
					while (gameStatus == GameStatus.MENU)
					{
						yield return null;
					}
					yield return null;
				}
			}
			switch (biome.biomeType)
			{
			case BiomeType.DESERT:
				Platform.current.GainAchievement(Achievement.DESERT);
				break;
			case BiomeType.JUNGLE:
				Platform.current.GainAchievement(Achievement.JUNGLE);
				break;
			case BiomeType.TOXIC:
				Platform.current.GainAchievement(Achievement.TOXIC);
				break;
			case BiomeType.CONCRETE:
				Platform.current.GainAchievement(Achievement.CONCRETE);
				break;
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public bool BusyAddingBiome()
	{
		return cAddBiome != null;
	}

	private IEnumerator KFindGroundSpawnLocation(Ground ground, Action<Vector3> pos_result)
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			bool flag = false;
			int attempt = 0;
			int max_attempts = 5;
			Vector3 pos = Vector3.zero;
			while (!flag && attempt++ < max_attempts)
			{
				float far = 15000f;
				float num = 150f;
				Layers layer = Layers.IgnoreRaycast;
				List<GameObject> tmp_colliders = new List<GameObject>();
				foreach (Ground ground2 in grounds)
				{
					for (int i = 0; i < ground2.shapeCircles.Length; i++)
					{
						Circle circle = ground2.shapeCircles[i];
						GameObject gameObject = new GameObject("Tmp_ground_collider");
						gameObject.layer = (int)layer;
						gameObject.AddComponent<SphereCollider>().radius = circle.radius + num;
						gameObject.transform.SetParent(ground2.transform, worldPositionStays: false);
						gameObject.transform.position = ground2.transform.position + circle.pos;
						tmp_colliders.Add(gameObject);
					}
				}
				yield return new WaitForSeconds(0.1f);
				float num2 = UnityEngine.Random.Range(0f, MathF.PI * 2f);
				float num3 = float.MaxValue;
				for (float num4 = 0f; num4 < 359f; num4 += 60f)
				{
					Vector3 vector = new Vector3(Mathf.Sin(num2 + num4), 0f, Mathf.Cos(num2 + num4));
					float num5 = float.MaxValue;
					for (int j = 0; j < ground.shapeCircles.Length; j++)
					{
						Circle circle2 = ground.shapeCircles[j];
						if (Physics.SphereCast(far * vector + circle2.pos, circle2.radius, -vector, out var hitInfo, far, Toolkit.Mask(layer)) && hitInfo.distance < num5)
						{
							num5 = hitInfo.distance;
						}
					}
					Vector3 vector2 = (far - num5) * vector;
					float sqrMagnitude = vector2.sqrMagnitude;
					if (sqrMagnitude < num3)
					{
						num3 = sqrMagnitude;
						pos = vector2;
					}
				}
				foreach (GameObject item in tmp_colliders)
				{
					UnityEngine.Object.Destroy(item);
				}
				Vector3 vector3 = pos;
				flag = true;
				for (int k = 0; k < ground.shapeCircles.Length; k++)
				{
					Circle circle3 = ground.shapeCircles[k];
					foreach (Ground ground3 in grounds)
					{
						Vector3 position = ground3.transform.position;
						for (int l = 0; l < ground3.shapeCircles.Length; l++)
						{
							Circle circle4 = ground3.shapeCircles[l];
							if ((vector3 + circle3.pos - (position + circle4.pos)).sqrMagnitude < (circle3.radius + circle4.radius) * (circle3.radius + circle4.radius))
							{
								flag = false;
							}
						}
					}
				}
			}
			pos_result(pos);
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public int GetGroundCount(bool include_reveals = false)
	{
		int num = grounds.Count;
		if (include_reveals)
		{
			num += Progress.GetReveals();
		}
		return num;
	}

	public Ant SpawnAnt(AntCaste caste, Vector3 pos, Quaternion rot, Save save = null)
	{
		AntCasteData antCasteData = AntCasteData.Get(caste);
		Ant component = UnityEngine.Object.Instantiate(antCasteData.prefab, pos, rot).GetComponent<Ant>();
		component.Fill(antCasteData);
		if (save != null)
		{
			component.Read(save);
		}
		AddAnt(component, save != null);
		return component;
	}

	public void AddAnt(Ant ant, bool during_load = false)
	{
		if (allAnts.Contains(ant))
		{
			return;
		}
		allAnts.Add(ant);
		if (ant.anim != null)
		{
			if ((ant.caste == AntCaste.DRONE || ant.caste == AntCaste.DRONE_SMALL || ant.caste == AntCaste.DRONE_T2 || ant.caste == AntCaste.DRONE_T2) && ant.rends.Count > 0 && ant.rends[0] != null)
			{
				InitAnimator(ant.anim, AnimationCulling.Delayed, ant.rends[0]);
			}
			else
			{
				InitAnimator(ant.anim, AnimationCulling.Always);
			}
		}
		ant.Init(during_load);
		UpdateAntCount();
	}

	public void DeleteAnt(Ant ant)
	{
		allAnts.Remove(ant);
		if (ant.anim != null)
		{
			RemovePausableAnimator(ant.anim);
		}
		UnityEngine.Object.Destroy(ant.gameObject);
		UpdateAntCount();
	}

	public void AddBuilding(Building _building)
	{
		bool flag = true;
		if (_building is Queen || _building is GyneMaker || _building.data.code == "NEST" || _building.data.code == "COMBINER2")
		{
			flag = false;
		}
		if (_building is Queen item && !allQueens.Contains(item))
		{
			allQueens.Add(item);
		}
		allBuildings.Add(_building);
		if (_building.NeedsFixedUpdate())
		{
			allFixedUpdateBuildings.Add(_building);
		}
		foreach (Animator item2 in _building.EPausableAnimators())
		{
			InitAnimator(item2, flag ? AnimationCulling.Always : AnimationCulling.Never);
		}
		foreach (ParticleSystem item3 in _building.EPausableParticles())
		{
			AddPausableParticles(item3);
		}
		UpdateAntCount();
	}

	public void RemoveBuilding(Building _building)
	{
		if (_building is Queen item && allQueens.Contains(item))
		{
			allQueens.Remove(item);
		}
		if (_building.ground != null)
		{
			_building.ground.RemoveBuilding(_building);
		}
		if (allBuildings.Contains(_building))
		{
			allBuildings.Remove(_building);
		}
		if (allFixedUpdateBuildings.Contains(_building))
		{
			allFixedUpdateBuildings.Remove(_building);
		}
		foreach (Animator item2 in _building.EPausableAnimators())
		{
			RemovePausableAnimator(item2);
		}
		foreach (ParticleSystem item3 in _building.EPausableParticles())
		{
			RemovePausableParticles(item3);
		}
		UpdateAntCount();
	}

	public Pickup SpawnPickup(PickupType _type, Vector3 pos, Quaternion rot, Save from_save = null)
	{
		return SpawnPickup(PickupData.Get(_type).prefab, _type, pos, rot, from_save);
	}

	public Pickup SpawnPickup(GameObject prefab, PickupType type, Vector3 pos, Quaternion rot, Save save = null)
	{
		Pickup component = UnityEngine.Object.Instantiate(prefab, pos, rot).GetComponent<Pickup>();
		component.Fill(type);
		if (save != null)
		{
			component.Read(save);
		}
		component.Init(save != null);
		AddPickup(component);
		return component;
	}

	public Pickup SpawnPickup(PickupType _type)
	{
		return SpawnPickup(_type, Vector3.zero, Quaternion.identity);
	}

	public void AddPickup(Pickup p)
	{
		if (postponeAllPickupsChange)
		{
			pickupsToAdd.Add(p);
		}
		else if (allPickups.Add(p) && p.data.categories.Contains(PickupCategory.LIVING))
		{
			if (!allLarvae.Contains(p))
			{
				allLarvae.Add(p);
			}
			UpdateAntCount();
		}
	}

	public void RemovePickup(Pickup p)
	{
		if (postponeAllPickupsChange)
		{
			pickupsToRemove.Add(p);
		}
		else if (allPickups.Remove(p) && allLarvae.Contains(p))
		{
			allLarvae.Remove(p);
			UpdateAntCount();
		}
	}

	public Trail NewTrail(TrailType _type, TrailGate trail_gate = null, Ant _owner = null)
	{
		return NewTrail(_type, trail_gate, _owner, is_action_trail: false, is_invisible: false, null, visual_only: false);
	}

	public Trail NewTrail(Save from_save)
	{
		TrailType type = (TrailType)from_save.ReadInt();
		return NewTrail(type, null, null, is_action_trail: false, is_invisible: false, from_save, visual_only: false);
	}

	public Trail NewTrail_Building(TrailType _type, bool is_invisible)
	{
		return NewTrail(_type, null, null, is_action_trail: false, is_invisible, null, visual_only: false);
	}

	public Trail NewTrail_Action(TrailType _type)
	{
		return NewTrail(_type, null, null, is_action_trail: true, is_invisible: false, null, visual_only: false);
	}

	public Trail NewTrail_VisualOnly(TrailType _type)
	{
		return NewTrail(_type, null, null, is_action_trail: false, is_invisible: false, null, visual_only: true);
	}

	private Trail NewTrail(TrailType _type, TrailGate trail_gate, Ant _owner, bool is_action_trail, bool is_invisible, Save from_save, bool visual_only)
	{
		if (_type == TrailType.NONE)
		{
			Debug.LogError("Tried creating trail with type NONE, shouldn't happen");
			_type = TrailType.HAULING;
		}
		Trail component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(Trail))).GetComponent<Trail>();
		component.Fill(TrailData.Get(_type));
		if (from_save != null)
		{
			component.Read(from_save);
		}
		component.Init(TrailStatus.HOVERING, trail_gate, _owner, is_action_trail, is_invisible, from_save != null);
		if (!visual_only)
		{
			allTrails.Add(component);
			if (component.trailGate != null)
			{
				allTrailGates.Add(component.trailGate);
			}
		}
		return component;
	}

	public Split NewSplit(Vector3 pos, Save from_save = null)
	{
		Split component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(Split)), pos, Quaternion.identity).GetComponent<Split>();
		if (from_save != null)
		{
			component.Read(from_save);
		}
		else
		{
			Split.AddChangedSplit(component);
		}
		component.Init(from_save != null);
		allSplits.Add(component);
		return component;
	}

	public void RemoveTrail(Trail trail)
	{
		allTrails.Remove(trail);
		if (trail.trailGate != null)
		{
			allTrailGates.Remove(trail.trailGate);
		}
	}

	public void RemoveSplit(Split split)
	{
		allSplits.Remove(split);
	}

	public BiomeObject SpawnBiomeObject(string code, Ground ground, Vector3 pos, Quaternion rot, Transform parent, float size, Save save)
	{
		return SpawnBiomeObject(BiomeObjectData.Get(code).prefab, code, ground, pos, rot, parent, size, -1, save);
	}

	public BiomeObject SpawnBiomeObject(string code, Ground ground, Vector3 pos, Quaternion rot, Transform parent, float size, int mesh_index = -1)
	{
		return SpawnBiomeObject(BiomeObjectData.Get(code).prefab, code, ground, pos, rot, parent, size, mesh_index);
	}

	public BiomeObject SpawnBiomeObject(GameObject prefab, string code, Ground ground, Vector3 pos, Quaternion rot, Transform parent, float size, int mesh_index = -1, Save save = null)
	{
		BiomeObject component = UnityEngine.Object.Instantiate(prefab, pos, rot, parent).GetComponent<BiomeObject>();
		component.code = code;
		component.spawnSize = size;
		if (save != null)
		{
			component.Read(save);
		}
		else
		{
			component.ground = ground;
			component.meshIndex = mesh_index;
		}
		component.Init(save != null);
		AddBiomeObject(component);
		return component;
	}

	public void AddBiomeObject(BiomeObject bob)
	{
		allBiomeObjects.Add(bob);
		foreach (Animator randomizable in bob.randomizables)
		{
			InitAnimator(randomizable, AnimationCulling.Always);
		}
	}

	public void RemoveBiomeObject(BiomeObject bob)
	{
		allBiomeObjects.Remove(bob);
		foreach (Animator randomizable in bob.randomizables)
		{
			RemovePausableAnimator(randomizable);
		}
		UnityEngine.Object.Destroy(bob.gameObject);
	}

	public NuptialFlightActor SpawnNuptialFlightActor(AntCaste _caste)
	{
		NuptialFlightActor component = UnityEngine.Object.Instantiate(AntCasteData.Get(_caste).prefab_nuptialFlight).GetComponent<NuptialFlightActor>();
		if (component.anim != null)
		{
			InitAnimator(component.anim, AnimationCulling.Always);
		}
		return component;
	}

	public void DeleteNuptialFlightActor(NuptialFlightActor actor)
	{
		if (actor.anim != null)
		{
			RemovePausableAnimator(actor.anim);
		}
		UnityEngine.Object.Destroy(actor.gameObject);
	}

	public Explosion SpawnExplosion(ExplosionType _type, Vector3 pos)
	{
		Explosion component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetExplosionPrefab(_type), pos, Toolkit.RandomYRotation()).GetComponent<Explosion>();
		allExplosions.Add(component);
		return component;
	}

	public void DeleteExplosion(Explosion explo)
	{
		allExplosions.Remove(explo);
		UnityEngine.Object.Destroy(explo.gameObject);
	}

	public bool DontSave()
	{
		if (gameStatus != GameStatus.BUSY_SYS && gameStatus != GameStatus.PASSIVE)
		{
			if (gameStatus == GameStatus.MENU)
			{
				if (lastPlayTimeStatus != GameStatus.BUSY_SYS)
				{
					return lastPlayTimeStatus == GameStatus.PASSIVE;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public void QuickSave()
	{
		if (!DontSave())
		{
			SaveGame("_quick");
		}
	}

	public void QuickLoad()
	{
		SaveGame("_preload");
		LoadGameMidGame("_quick", bg: false, null, delegate
		{
			instance.LoadGameMidGame("_preload", bg: false, null, delegate
			{
			});
		});
	}

	public void AutoSave()
	{
		if (DontSave())
		{
			return;
		}
		int nAutosaveSlots = GlobalValues.standard.nAutosaveSlots;
		string save_name;
		if (nAutosaveSlots < 2)
		{
			save_name = "_auto";
		}
		else
		{
			int nextAutoSaveNumber = Player.nextAutoSaveNumber;
			nextAutoSaveNumber = Mathf.Clamp(nextAutoSaveNumber, 1, nAutosaveSlots);
			save_name = $"_auto{nextAutoSaveNumber}";
			nextAutoSaveNumber++;
			if (nextAutoSaveNumber > nAutosaveSlots)
			{
				nextAutoSaveNumber = 1;
			}
			Player.nextAutoSaveNumber = nextAutoSaveNumber;
		}
		SaveGame(save_name);
	}

	public bool SaveGame(string save_name)
	{
		Save save = new Save();
		bool flag = false;
		try
		{
			string filename = Files.GameSave(save_name, bg: false);
			save.StartWriting(filename);
			save.Write((int)Platform.GetGameType());
			save.Write((float)playTime);
			save.Write((float)gameTime);
			save.Write(DateTime.Now);
			if (save_name != "_preload")
			{
				SaveScreenshot(save_name);
			}
			WorldSettings.Write(save);
			Progress.Write(save);
			Instinct.Write(save);
			TechTree.Write(save);
			NuptialFlight.Write(save);
			int id = 0;
			foreach (Trail allTrail in allTrails)
			{
				allTrail.linkId = ((allTrail.IsPlaced() && !allTrail.IsAction()) ? (++id) : 0);
			}
			using (HashSet<Split>.Enumerator enumerator2 = allSplits.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					id = (enumerator2.Current.linkId = id + 1);
				}
			}
			using (List<Ant>.Enumerator enumerator3 = allAnts.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					id = (enumerator3.Current.linkId = id + 1);
				}
			}
			using (HashSet<Pickup>.Enumerator enumerator4 = allPickups.GetEnumerator())
			{
				while (enumerator4.MoveNext())
				{
					id = (enumerator4.Current.linkId = id + 1);
				}
			}
			using (HashSet<BiomeObject>.Enumerator enumerator5 = allBiomeObjects.GetEnumerator())
			{
				while (enumerator5.MoveNext())
				{
					id = (enumerator5.Current.linkId = id + 1);
				}
			}
			foreach (Building allBuilding in allBuildings)
			{
				allBuilding.SetLinkIds(ref id);
			}
			foreach (Ground ground in grounds)
			{
				ground.SetLinkIds(ref id);
			}
			CamController.instance.Write(save);
			save.Write(grounds.Count);
			foreach (Ground ground2 in grounds)
			{
				ground2.Write(save);
			}
			save.Write(allPickups.Count);
			foreach (Pickup allPickup in allPickups)
			{
				if (allPickup == null)
				{
					Debug.LogError("Tried writing pickup null, shouldn't happen");
					save.Write(0);
					continue;
				}
				save.Write((int)allPickup.type);
				save.Write(allPickup.transform.position);
				save.WriteYRot(allPickup.transform.rotation);
				allPickup.Write(save);
			}
			save.Write(allBiomeObjects.Count);
			foreach (BiomeObject allBiomeObject in allBiomeObjects)
			{
				if (allBiomeObject == null)
				{
					save.Write("NULL");
					Debug.LogError("Tried writing null object, shouldn't happen");
					continue;
				}
				save.Write(allBiomeObject.code);
				save.Write(allBiomeObject.transform.position);
				save.WriteYRot(allBiomeObject.transform.rotation);
				save.Write(allBiomeObject.spawnSize);
				allBiomeObject.Write(save);
			}
			foreach (Trail item in new List<Trail>(allTrails))
			{
				if (item == null)
				{
					Debug.Log("Tried saving a null trail, removing from list");
					allTrails.Remove(item);
				}
			}
			int num5 = 0;
			foreach (Trail allTrail2 in allTrails)
			{
				if (allTrail2.linkId != 0)
				{
					num5++;
				}
			}
			save.Write(num5);
			foreach (Trail allTrail3 in allTrails)
			{
				if (allTrail3.linkId != 0)
				{
					save.Write((int)allTrail3.trailType);
					allTrail3.Write(save);
				}
			}
			int num6 = 0;
			foreach (Split allSplit in allSplits)
			{
				if (allSplit.HasTrailWithLinkId())
				{
					num6++;
				}
			}
			save.Write(num6);
			foreach (Split allSplit2 in allSplits)
			{
				if (allSplit2.HasTrailWithLinkId())
				{
					save.Write(allSplit2.transform.position);
					allSplit2.Write(save);
				}
			}
			save.Write(allAnts.Count);
			foreach (Ant allAnt in allAnts)
			{
				save.Write((int)allAnt.caste);
				Transform transform = allAnt.transform;
				save.Write(transform.position);
				save.WriteYRot(transform.rotation);
				allAnt.Write(save);
			}
			List<Building> list = new List<Building>();
			foreach (Building allBuilding2 in allBuildings)
			{
				if (allBuilding2.IsPlaced())
				{
					list.Add(allBuilding2);
				}
			}
			save.Write(list.Count);
			foreach (Building item2 in list)
			{
				save.Write(item2.data.code);
				Transform transform2 = item2.transform;
				save.Write(transform2.position);
				save.WriteYRot(transform2.rotation);
				item2.Write(save);
			}
			Filters.Write(save);
			History.Write(save);
			BlueprintManager.Write(save);
			UIGame.instance.Write(save);
			Player.lastSave = save_name;
			flag = true;
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.ToString());
		}
		finally
		{
			save.DoneWriting(flag);
			if (flag)
			{
				Debug.Log("Saved game to " + save.fileName);
			}
			else
			{
				Debug.LogError("Saving failed");
			}
		}
		if (flag)
		{
			Player.Save();
		}
		return flag;
	}

	public void LoadGameMidGame(string savename, bool bg, Action fail_action, Action after_error_action)
	{
		Filters.Select(Filter.HIDE_UI, selected: false);
		StartKoroutine(KStartLoadGame(savename, bg, debug_start: false, mid_game: true, fail_action, after_error_action));
	}

	public IEnumerator KStartLoadGame(string save_name, bool bg, bool debug_start, bool mid_game, Action fail_action, Action after_error_action)
	{
		UILoading ui_loading = null;
		bool black = false;
		bool success = false;
		KoroutineId kid = SetFinalizer(delegate
		{
			if (ui_loading != null)
			{
				UnityEngine.Object.Destroy(ui_loading.gameObject);
			}
			if (black)
			{
				UIGlobal.instance.GoBlack(black: false, (save_name == "" && !theater) ? gameFadeInDurationNewGame : gameFadeInDuration);
				UIGame.instance.SetVisible(visible: true);
			}
			if (!success)
			{
				if (fail_action != null)
				{
					fail_action();
				}
				if (after_error_action != null)
				{
					UIDialogBase dialog = UIBase.Spawn<UIDialogBase>();
					dialog.SetText(Loc.GetUI("LOADSAVE_LOAD_ERROR"));
					dialog.SetAction(DialogResult.OK, delegate
					{
						dialog.StartClose();
						after_error_action();
					});
				}
			}
			else
			{
				SetStatus(GameStatus.RUNNING);
			}
		});
		try
		{
			if (save_name != "" && !File.Exists(Files.GameSave(save_name, bg)))
			{
				Debug.LogError("Couldn't find '" + save_name + "'");
				yield break;
			}
			if (mid_game)
			{
				black = true;
				UIGlobal.instance.GoBlack(black: true, 0f);
				UIGame.instance.SetVisible(visible: false);
				yield return null;
			}
			if (!bg && save_name != "")
			{
				ui_loading = UIBase.Spawn<UILoading>();
				ui_loading.Init();
			}
			yield return null;
			SetStatus(GameStatus.STOPPED);
			CleanUpAll();
			if (save_name != "")
			{
				bool load_ok = false;
				yield return StartKoroutine(kid, KLoadGame(save_name, bg, delegate(float progress)
				{
					if (ui_loading != null)
					{
						ui_loading.SetProgress(progress);
					}
				}, delegate(bool res)
				{
					load_ok = res;
				}));
				if (!load_ok)
				{
					yield break;
				}
			}
			else
			{
				Debug.Log("Starting new game");
				Progress.playthroughId = Player.StartNewPlaythrough(debug_start);
				yield return StartKoroutine(kid, KStartEnvironment());
				Instinct.SetFirstUncompletedInstinct();
			}
			ResetTotalPickupInventory();
			UpdateAntCount();
			if (UIGame.instance != null)
			{
				UIGame.instance.RefreshTasks();
			}
			success = true;
			Debug.Log("Game started");
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public static bool LoadHeader(Save save, out float play_time, out float game_time, out DateTime date_time, out GameType game_type)
	{
		try
		{
			game_type = ((save.version >= 47) ? ((GameType)save.ReadInt()) : GameType.NotSet);
			play_time = save.ReadFloat();
			game_time = ((save.version >= 58) ? save.ReadFloat() : play_time);
			date_time = save.ReadDateTime();
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("Couldn't load save header: " + ex.Message);
			game_type = GameType.Unknown;
			game_time = (play_time = 0f);
			date_time = DateTime.MinValue;
			return false;
		}
	}

	private IEnumerator KLoadGame(string save_name, bool bg, Action<float> action_progress, Action<bool> action_result)
	{
		string filename = Files.GameSave(save_name, bg);
		Save save = new Save();
		string doing = "";
		StringBuilder sb_counts = new StringBuilder();
		KoroutineId kid = SetFinalizer(delegate
		{
			save.DoneReading();
			bool flag = doing == "";
			if (!flag)
			{
				Debug.LogError("Something went wrong while loading " + doing);
			}
			Debug.Log($"Loading done\n{sb_counts}");
			action_result(flag);
		});
		try
		{
			save.StartReading(filename);
			Debug.Log($"Loading from {save.fileName}.. (v.{save.version})");
			LoadHeader(save, out var play_time, out var game_time, out var _, out var _);
			playTime = play_time;
			gameTime = game_time;
			Player.ResetCheats();
			doing = "Progress";
			WorldSettings.Read(save);
			Progress.Read(save);
			Instinct.Read(save);
			TechTree.Read(save);
			NuptialFlight.Read(save);
			action_progress(0.1f);
			yield return null;
			doing = "Camera";
			if (save.version >= 55)
			{
				CamController.instance.Read(save);
			}
			doing = "Grounds";
			int n = save.ReadInt();
			sb_counts.AppendLine($"#grounds: {n}");
			float progress_base = 0.1f;
			float progress_delta = 0.5f / (float)n;
			for (int i = 0; i < n; i++)
			{
				string address = save.ReadString();
				if (!address.StartsWith("Biomes/"))
				{
					address = "Biomes/" + address;
				}
				AsyncOperationHandle<Biome> loading = Addressables.LoadAssetAsync<Biome>(address);
				yield return loading;
				action_progress(progress_base + progress_delta * 0.1f);
				yield return null;
				Biome result = loading.Result;
				if (result == null)
				{
					Debug.LogError("Unknown biome address '" + address + "'");
					yield break;
				}
				int generation_seed;
				int generation_seed_index;
				if (save.version < 14)
				{
					save.ReadString();
					generation_seed = 0;
					generation_seed_index = 0;
				}
				else
				{
					generation_seed = save.ReadInt();
					generation_seed_index = save.ReadInt();
				}
				Vector3 ground_pos = save.ReadVector3();
				Quaternion ground_rot = save.ReadYRot();
				int ground_index = ((save.version >= 8) ? save.ReadInt() : 0);
				Ground ground = Ground.Create(result, ground_pos, ground_rot, ground_index);
				yield return StartKoroutine(kid, ground.KFill(spawnParent, generation_seed, generation_seed_index, result.spawnUnlocker, save));
				action_progress(progress_base + progress_delta * 0.3f);
				yield return null;
				ground.biomeAddress = address;
				yield return StartKoroutine(kid, ground.KRead(save, delegate(float progress)
				{
					action_progress(progress_base + progress_delta * (0.3f + 0.5f * progress));
				}));
				AddGround(ground);
				action_progress(progress_base + progress_delta * 0.9f);
				yield return null;
				progress_base += progress_delta;
			}
			UpdateBiomeInfluence();
			action_progress(0.7f);
			yield return null;
			doing = "Pickups";
			n = save.ReadInt();
			sb_counts.AppendLine($"#pickups: {n}");
			for (int num = 0; num < n; num++)
			{
				PickupType pickupType = (PickupType)save.ReadInt();
				if (pickupType != PickupType.NONE)
				{
					SpawnPickup(pickupType, save.ReadVector3(), save.ReadYRot(), save);
				}
			}
			action_progress(0.73f);
			yield return null;
			doing = "BiomeObjects";
			n = save.ReadInt();
			sb_counts.AppendLine($"#biomeobjects: {n}");
			for (int num2 = 0; num2 < n; num2++)
			{
				string text = save.ReadString();
				if (text != "NULL")
				{
					SpawnBiomeObject(text, null, save.ReadVector3(), save.ReadYRot(), spawnParent, save.ReadFloat(), save);
				}
			}
			action_progress(0.77f);
			yield return null;
			doing = "Trails";
			n = save.ReadInt();
			sb_counts.AppendLine($"#trails: {n}");
			for (int num3 = 0; num3 < n; num3++)
			{
				NewTrail(save);
			}
			doing = "Splits";
			n = save.ReadInt();
			sb_counts.AppendLine($"#splits: {n}");
			for (int num4 = 0; num4 < n; num4++)
			{
				Vector3 pos = save.ReadVector3();
				NewSplit(pos, save);
			}
			doing = "Trails (LoadLinkSplits)";
			foreach (Trail allTrail in allTrails)
			{
				allTrail.LoadLinkSplits();
			}
			action_progress(0.8f);
			yield return null;
			doing = "Ants";
			n = save.ReadInt();
			sb_counts.AppendLine($"#ants: {n}");
			for (int num5 = 0; num5 < n; num5++)
			{
				AntCaste caste = (AntCaste)save.ReadInt();
				Vector3 pos2 = save.ReadVector3();
				Quaternion rot = save.ReadYRot();
				SpawnAnt(caste, pos2, rot, save);
			}
			action_progress(0.83f);
			yield return null;
			doing = "Buildings";
			n = save.ReadInt();
			sb_counts.AppendLine($"#buildings: {n}");
			for (int num6 = 0; num6 < n; num6++)
			{
				BuildingData buildingData = BuildingData.Get(save.ReadString());
				Vector3 position = save.ReadVector3();
				Quaternion rotation = save.ReadYRot();
				Building component = UnityEngine.Object.Instantiate(buildingData.prefab, position, rotation).GetComponent<Building>();
				component.Fill(buildingData);
				component.Read(save);
				component.Init(save != null);
			}
			action_progress(0.87f);
			yield return null;
			doing = "Grounds (LoadLinkPickups)";
			foreach (Ground ground2 in grounds)
			{
				ground2.LoadLinkPickups();
			}
			doing = "Buildings (LoadLinkBuildings)";
			foreach (Building allBuilding in allBuildings)
			{
				allBuilding.LoadLinkBuildings();
			}
			foreach (Building allBuilding2 in allBuildings)
			{
				allBuilding2.UpdateBillboard();
			}
			doing = "Trails (LoadLinkAntsAndConnectables)";
			HashSet<ConnectableObject> hashSet = new HashSet<ConnectableObject>();
			foreach (Trail item in new List<Trail>(allTrails))
			{
				if (item.linkId != 0)
				{
					item.LoadLinkAntsAndConnectables(hashSet);
				}
			}
			foreach (ConnectableObject item2 in hashSet)
			{
				if (item2 == null)
				{
					Debug.LogError("Object null while loading, shouldn't happen");
				}
				else
				{
					item2.UpdateNearbyActionPoints();
				}
			}
			doing = "Ants (LoadLinkActionPointsAndBuildings)";
			foreach (Ant allAnt in allAnts)
			{
				allAnt.LoadLinks();
			}
			doing = "Pickups (LoadLinkAntsAndContainers)";
			foreach (Pickup allPickup in allPickups)
			{
				allPickup.LoadLinkAntsAndContainers();
			}
			action_progress(0.9f);
			yield return null;
			doing = "Rest";
			if (save.version < 55)
			{
				CamController.instance.Read(save);
			}
			Filters.Read(save);
			History.Read(save);
			BlueprintManager.Read(save);
			if (save.version >= 93)
			{
				UIGame.instance.Read(save);
			}
			doing = "";
			foreach (Trail allTrail2 in allTrails)
			{
				allTrail2.linkId = 0;
			}
			foreach (Split allSplit in allSplits)
			{
				allSplit.linkId = 0;
			}
			foreach (Ant allAnt2 in allAnts)
			{
				allAnt2.linkId = 0;
			}
			foreach (Pickup allPickup2 in allPickups)
			{
				allPickup2.linkId = 0;
			}
			foreach (BiomeObject allBiomeObject in allBiomeObjects)
			{
				allBiomeObject.linkId = 0;
			}
			dicLinks.Clear();
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public void AddLinkId(ISaveable s, int id, string name = "")
	{
		if (id != 0 && !dicLinks.TryAdd(id, s))
		{
			Debug.LogError($"Tried to add duplicated link id {id} for object {name}, shouldn't happen");
		}
	}

	public T FindLink<T>(int id) where T : ClickableObject
	{
		if (id == 0)
		{
			return null;
		}
		if (!dicLinks.TryGetValue(id, out var value))
		{
			Debug.LogWarning($"Couldn't find link id {id}");
			return null;
		}
		T val = value as T;
		if (val == null)
		{
			Debug.LogWarning(string.Format("Object with linkId {0} ({1}) should be {2}", id, (val == null) ? "???" : val.name, typeof(T).Name));
		}
		return val;
	}

	private void SaveScreenshot(string name)
	{
		Texture2D screenshot = CamController.instance.GetScreenshot();
		File.WriteAllBytes(Files.GameSaveImage(name), screenshot.EncodeToPNG());
		UnityEngine.Object.Destroy(screenshot);
	}
}
