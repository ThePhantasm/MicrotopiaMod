using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGame : UIBaseSingleton
{
	[Serializable]
	public class TechCurrencyTIB
	{
		public InventorPoints type;

		public UITextImageButton tib;
	}

	public static UIGame instance;

	[Space(10f)]
	public UIBuildingMenu uiBuildingMenu;

	public UIBuildingButtonHover uiBuildingHover;

	public UITask_NuptialFlight uiNuptialFlight;

	public UITask_InstinctIdea prefabTask;

	public UITask_TrackBuilding prefabTrackBuilding;

	[SerializeField]
	private GameObject obProgression;

	[Space(10f)]
	public UIClick uiClick;

	public UILogicControl uiLogicControl;

	[Space(10f)]
	[SerializeField]
	private TextMeshProUGUI lbAntCount;

	[SerializeField]
	private TextMeshProUGUI lbMaxPop;

	[SerializeField]
	private TextMeshProUGUI lbLarvaRate;

	[SerializeField]
	private TextMeshProUGUI lbLarvaUnit;

	[SerializeField]
	private GameObject obAntCount;

	[SerializeField]
	private GameObject obMaxPop;

	[SerializeField]
	private GameObject obLarvaRate;

	[SerializeField]
	private RectTransform rtPause;

	[SerializeField]
	private RectTransform rtMiddleMessage;

	[SerializeField]
	private TMP_Text lbMiddleMessage;

	[SerializeField]
	private GameObject obWait;

	[Space(10f)]
	[SerializeField]
	private TextMeshProUGUI lbHungerTier;

	[SerializeField]
	private TextMeshProUGUI lbEnergyAmount;

	[SerializeField]
	private GameObject obHunger;

	[SerializeField]
	private GameObject obHungerHoverDescription;

	[SerializeField]
	private Image imHungerBar;

	[SerializeField]
	private Slider slHungerBar;

	[SerializeField]
	private UITextImageButton tibHungerBarHover;

	[SerializeField]
	private UITextImageButton tibAntCountHover;

	[SerializeField]
	private UITextImageButton tibLarvaRateHover;

	[SerializeField]
	private UITextImageButton tibRevealIsland;

	[SerializeField]
	private RectTransform rtFilterMenuPoint;

	[SerializeField]
	private TMP_Dropdown ddFilters;

	[Space(10f)]
	[SerializeField]
	private UITextImageButton tibTutorialButton;

	[SerializeField]
	private UITextImageButton tibFilters;

	[SerializeField]
	private UITextImageButton tibFeedback;

	[SerializeField]
	private UITextImageButton tibEscapeMenu;

	[SerializeField]
	private UITextImageButton tibToggleDragging;

	[SerializeField]
	private UITextImageButton tibPausePlay;

	[SerializeField]
	private RectTransform rtDraggingDisabled;

	[SerializeField]
	private Sprite sprPause;

	[SerializeField]
	private Sprite sprPlay;

	[Space(10f)]
	[SerializeField]
	private UIInventoryCategory prefabInventoryItem;

	private List<UIInventoryCategory> inventoryPickupCategories = new List<UIInventoryCategory>();

	private UIInventoryCategory inventoryAntBuildingCategory;

	private UIInventoryCategory inventoryAntPickupCategory;

	private UIInventoryCategory inventoryAntCategory;

	[SerializeField]
	private UITextImageButton tibInventoryTitle;

	private bool antInventory;

	[SerializeField]
	private UIGraph uiGraph;

	[SerializeField]
	private TMP_Text lbGraphHint;

	[SerializeField]
	private UITextImageButton btGraphHour;

	[SerializeField]
	private UITextImageButton btGraphInfinite;

	[Space(10f)]
	[SerializeField]
	private UITextImageButton btOpenTechTree;

	[SerializeField]
	private Color colButtonRegular;

	[SerializeField]
	private Color colButtonHighlight;

	[SerializeField]
	private UITechCurrency prefabUiTechCurrency;

	[SerializeField]
	private RectTransform rtTopButtons;

	[SerializeField]
	private List<TechCurrencyTIB> listButtonCurrencies = new List<TechCurrencyTIB>();

	[SerializeField]
	private RectTransform rtBottomCurrencies;

	private List<UITechCurrency> spawnedCurrencies_active = new List<UITechCurrency>();

	private List<UITechCurrency> spawnedCurrencies_disabled = new List<UITechCurrency>();

	private float techtreeButtonTimer;

	private float topButtonXOrig;

	private float timeHideMiddleMessage;

	private int antCountHashPrev = int.MinValue;

	private List<UITask_InstinctIdea> spawnedTasks = new List<UITask_InstinctIdea>();

	private List<UITask_TrackBuilding> spawnedTrackers = new List<UITask_TrackBuilding>();

	private List<Building> trackedBuildings = new List<Building>();

	private TaskID lastTaskOpen;

	private string hungerHovertext;

	private string antCountHoverText;

	private string larvaRateHoverText;

	private float collectedEnergy;

	private float drainRate;

	private bool hoveringHunger;

	private bool hoveringAntCount;

	private bool hoveringLarvaRate;

	private bool inventoryInited;

	private int clickThroughIndexAnts = -1;

	private int clickThroughIndexBuildings = -1;

	private int clickThroughIndexPickups = -1;

	private List<(AntCaste, Color)> antCastesInGraph = new List<(AntCaste, Color)>();

	[SerializeField]
	private Color[] graphColors;

	private int graphTime;

	[SerializeField]
	private UINotice noticeInventory;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		UpdateAntCount(force_update: true);
		uiBuildingMenu.Init(uiBuildingHover);
		if (topButtonXOrig == 0f)
		{
			topButtonXOrig = rtTopButtons.anchoredPosition.x;
		}
		if (DebugSettings.standard.ProgressionEnabled())
		{
			obProgression.SetObActive(active: true);
			RefreshTasks();
			rtTopButtons.anchoredPosition = rtTopButtons.anchoredPosition.SetX(topButtonXOrig);
		}
		else
		{
			obProgression.SetObActive(active: false);
			rtTopButtons.anchoredPosition = rtTopButtons.anchoredPosition.SetX(-45f);
		}
		tibHungerBarHover.SetOnPointerEnter(delegate
		{
			UIHover.instance.Init(tibHungerBarHover);
			UIHover.instance.SetWidth();
			UIHover.instance.SetText(hungerHovertext);
			hoveringHunger = true;
		});
		tibHungerBarHover.SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(tibHungerBarHover);
			hoveringHunger = false;
		});
		obHungerHoverDescription.SetObActive(active: false);
		tibAntCountHover.SetOnPointerEnter(delegate
		{
			if (GameManager.instance.GetAntCount() >= 5)
			{
				UIHover.instance.Init(tibAntCountHover);
				UIHover.instance.SetWidth();
				UIHover.instance.SetText(antCountHoverText);
				hoveringAntCount = true;
			}
		});
		tibAntCountHover.SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(tibAntCountHover);
			hoveringAntCount = false;
		});
		tibLarvaRateHover.SetOnPointerEnter(delegate
		{
			UIHover.instance.Init(tibLarvaRateHover);
			UIHover.instance.SetWidth();
			UIHover.instance.SetText(larvaRateHoverText);
			hoveringLarvaRate = true;
		});
		tibLarvaRateHover.SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(tibLarvaRateHover);
			hoveringLarvaRate = false;
		});
		obLarvaRate.SetObActive(active: false);
		if (DebugSettings.standard.playtest)
		{
			tibFeedback.SetObActive(active: true);
			tibFeedback.SetButton(delegate
			{
				UIBaseSingleton.Get(UIFeedback.instance).Init();
			});
			tibFeedback.SetHoverLocUI("BUTTON_SEND_FEEDBACK");
		}
		else
		{
			tibFeedback.SetObActive(active: false);
		}
		tibEscapeMenu.SetButton(delegate
		{
			GameManager.instance.OpenEscMenu();
		});
		tibEscapeMenu.SetHoverLocUI("GENERIC_ESC_MENU");
		tibTutorialButton.SetButton(delegate
		{
			ShowTutorialLog();
		});
		SetTutorialButton();
		tibTutorialButton.SetHoverLocUI("BUTTON_TUTORIAL_LOG");
		tibToggleDragging.SetButton(delegate
		{
			Player.disableTrailDragging = !Player.disableTrailDragging;
			UpdateTrailDragLock();
			tibToggleDragging.DoOnPointerExit();
			tibToggleDragging.DoOnPointerEnter();
		});
		UpdateTrailDragLock();
		tibPausePlay.SetButton(delegate
		{
			switch (GameManager.instance.GetStatus())
			{
			case GameStatus.RUNNING:
				GameManager.instance.SetStatus(GameStatus.PAUSED);
				break;
			case GameStatus.PAUSED:
				GameManager.instance.SetStatus(GameStatus.RUNNING);
				break;
			}
		});
		tibRevealIsland.SetButton(delegate
		{
			if (Progress.CanReveal())
			{
				GameManager.instance.AddBiome();
			}
			SetRevealIsland();
		});
		SetRevealIsland();
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		foreach (Filter item in Filters.EOptions())
		{
			string text = "";
			string text2;
			switch (item)
			{
			case Filter.NONE:
				text2 = Loc.GetUI("GENERIC_NONE");
				break;
			case Filter.HIDE_UI:
				text = InputManager.GetDesc(InputAction.FilterHideUI);
				text2 = Loc.GetUI("VIEW_HIDE_UI");
				break;
			case Filter.FLOATING_TRAILS:
				text = InputManager.GetDesc(InputAction.FilterFloatingTrails);
				text2 = Loc.GetUI("VIEW_TRAILS_FOREGROUND");
				break;
			case Filter.HIDE_TRAILS:
				text = InputManager.GetDesc(InputAction.FilterHideTrails);
				text2 = Loc.GetUI("VIEW_HIDE_TRAILS");
				break;
			case Filter.HIDE_ANTS:
				text = InputManager.GetDesc(InputAction.FilterHideAnts);
				text2 = Loc.GetUI("VIEW_HIDE_ANTS");
				break;
			default:
				text2 = item.ToString() + "_???";
				break;
			}
			if (text != "")
			{
				text2 = text2 + " (" + text + ")";
			}
			list.Add(new TMP_Dropdown.OptionData(text2));
		}
		ddFilters.AddOptions(list);
		ddFilters.onValueChanged.AddListener(delegate(int index)
		{
			Filters.OnlySelect((Filter)index, selected: true);
		});
		btOpenTechTree.SetButton(delegate
		{
			UIBaseSingleton.Get(UITechTree.instance).Init();
		});
		techtreeButtonTimer = float.MaxValue;
		rtMiddleMessage.SetObActive(active: false);
		obWait.SetObActive(active: false);
		UpdateTechTreeButtonCurrencies();
		SetInventoryTitle();
		tibInventoryTitle.SetButton(delegate
		{
			antInventory = !antInventory;
			SetInventoryTitle();
			CountInventory();
			ShowGraph(GameManager.instance.mapMode && antInventory);
			if (GameManager.instance.mapMode && antInventory && GameManager.instance.GetAntCount() > 10)
			{
				Player.SetNotice(Notice.TOGGLE_INVENTORY);
			}
			UpdateNoticeInventory();
		});
		uiGraph.SetObActive(active: false);
		btGraphHour.SetButton(delegate
		{
			SetGraphTime(1);
		});
		btGraphInfinite.SetButton(delegate
		{
			SetGraphTime(0);
		});
		antCastesInGraph.Clear();
		graphTime = 0;
		UpdateNoticeInventory();
		uiClick.Init();
		SetSelected(null);
		SetLogicControl(null);
	}

	public void UIUpdate()
	{
		if (!IsVisible())
		{
			return;
		}
		uiBuildingMenu.UIUpdate();
		if (Progress.HasUnlocked(GeneralUnlocks.NUPTIALFLIGHT_UI))
		{
			uiNuptialFlight.UIUpdate();
		}
		bool flag = false;
		foreach (UITask_InstinctIdea spawnedTask in spawnedTasks)
		{
			if (spawnedTask.isActiveAndEnabled)
			{
				spawnedTask.UIUpdate();
			}
		}
		foreach (UITask_TrackBuilding spawnedTracker in spawnedTrackers)
		{
			if (spawnedTracker.isActiveAndEnabled)
			{
				if (spawnedTracker.building == null)
				{
					flag = true;
				}
				else
				{
					spawnedTracker.UIUpdate();
				}
			}
		}
		if (flag)
		{
			RefreshTasks();
		}
		obHunger.SetObActive(Hunger.main != null && Progress.HasUnlocked(GeneralUnlocks.HUNGER_BAR));
		btOpenTechTree.SetObActive(Progress.HasUnlocked(GeneralUnlocks.TECH_TREE));
		UpdateAntCount();
		if (timeHideMiddleMessage > 0f && Time.realtimeSinceStartup > timeHideMiddleMessage)
		{
			MiddleMessage();
		}
		if (spawnedCurrencies_active.Count > 0)
		{
			foreach (UITechCurrency item in new List<UITechCurrency>(spawnedCurrencies_active))
			{
				item.CurrencyUpdate();
			}
		}
		float num = 0.5f;
		if (techtreeButtonTimer < num)
		{
			float t = GlobalValues.standard.curveEaseIn.Evaluate(techtreeButtonTimer / num);
			btOpenTechTree.rtBase.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, t);
			btOpenTechTree.SetImageColor(Color.Lerp(colButtonHighlight, colButtonRegular, t));
			techtreeButtonTimer += Time.deltaTime;
		}
		if (InputManager.trailDragLock && tibToggleDragging.gameObject.activeInHierarchy)
		{
			tibToggleDragging.Click();
		}
	}

	public void RefreshTasks()
	{
		SetupTasks();
		OpenATask();
	}

	public void SetupTasks()
	{
		List<Task> currentTasks = Instinct.GetCurrentTasks();
		prefabTask.SetObActive(active: false);
		if (spawnedTasks.Count < currentTasks.Count)
		{
			int num = currentTasks.Count - spawnedTasks.Count;
			for (int i = 0; i < num; i++)
			{
				UITask_InstinctIdea uITask_InstinctIdea = UnityEngine.Object.Instantiate(prefabTask, prefabTask.transform.parent);
				spawnedTasks.Add(uITask_InstinctIdea);
				uITask_InstinctIdea.SetObActive(active: false);
			}
		}
		foreach (UITask_InstinctIdea spawnedTask in spawnedTasks)
		{
			spawnedTask.SetObActive(active: false);
		}
		if (Progress.HasUnlocked(GeneralUnlocks.NUPTIALFLIGHT_UI))
		{
			uiNuptialFlight.SetObActive(active: true);
			uiNuptialFlight.Init(delegate
			{
				ToggleUITask(uiNuptialFlight);
			});
		}
		else
		{
			uiNuptialFlight.SetObActive(active: false);
		}
		prefabTrackBuilding.SetObActive(active: false);
		for (int num2 = trackedBuildings.Count - 1; num2 >= 0; num2--)
		{
			if (trackedBuildings[num2] == null)
			{
				trackedBuildings.RemoveAt(num2);
			}
		}
		if (spawnedTrackers.Count < trackedBuildings.Count)
		{
			int num3 = trackedBuildings.Count - spawnedTrackers.Count;
			for (int num4 = 0; num4 < num3; num4++)
			{
				UITask_TrackBuilding uITask_TrackBuilding = UnityEngine.Object.Instantiate(prefabTrackBuilding, prefabTrackBuilding.transform.parent);
				spawnedTrackers.Add(uITask_TrackBuilding);
				uITask_TrackBuilding.SetObActive(active: false);
			}
		}
		foreach (UITask_TrackBuilding spawnedTracker in spawnedTrackers)
		{
			spawnedTracker.SetObActive(active: false);
		}
		for (int num5 = 0; num5 < currentTasks.Count; num5++)
		{
			UITask_InstinctIdea ui_task = spawnedTasks[num5];
			ui_task.Init(currentTasks[num5], delegate
			{
				ToggleUITask(ui_task);
			});
			ui_task.SetObActive(active: true);
		}
		for (int num6 = 0; num6 < trackedBuildings.Count; num6++)
		{
			UITask_TrackBuilding ui_task2 = spawnedTrackers[num6];
			ui_task2.Init(trackedBuildings[num6], delegate
			{
				ToggleUITask(ui_task2);
			});
			ui_task2.SetObActive(active: true);
		}
	}

	public void CloseAllUITasks()
	{
		foreach (UITask item in EAllTasks())
		{
			item.Open(target: false, instant: true);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(rtBase);
	}

	private void ToggleUITask(UITask ui_task)
	{
		lastTaskOpen = default(TaskID);
		foreach (UITask item in EAllTasks())
		{
			bool flag = item == ui_task && !item.IsOpen();
			if (flag)
			{
				lastTaskOpen = item.GetUID();
			}
			item.Open(flag);
		}
	}

	private void OpenUITask(UITask ui_task)
	{
		lastTaskOpen = default(TaskID);
		foreach (UITask item in EAllTasks())
		{
			if (item == ui_task)
			{
				lastTaskOpen = item.GetUID();
				item.Open(target: true, instant: true);
			}
			else
			{
				item.Open(target: false);
			}
		}
	}

	public void TrackBuilding(Building b, bool track = true)
	{
		if (track)
		{
			if (!trackedBuildings.Contains(b))
			{
				trackedBuildings.Add(b);
				SetupTasks();
			}
			{
				foreach (UITask_TrackBuilding spawnedTracker in spawnedTrackers)
				{
					if (spawnedTracker.building == b)
					{
						OpenUITask(spawnedTracker);
						break;
					}
				}
				return;
			}
		}
		trackedBuildings.Remove(b);
		RefreshTasks();
	}

	public bool IsTrackingBuilding(Building b)
	{
		return trackedBuildings.Contains(b);
	}

	public void OpenATask()
	{
		if (spawnedTasks.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (UITask item in EAllTasks())
		{
			bool flag2 = item.GetUID() == lastTaskOpen;
			item.Open(flag2, instant: true);
			flag = flag || flag2;
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(rtBase);
		if (!flag)
		{
			OpenUITask(spawnedTasks[0]);
		}
	}

	public void OpenTargetTask(string task_code)
	{
		foreach (UITask_InstinctIdea spawnedTask in spawnedTasks)
		{
			if (spawnedTask.GetTask().code == task_code)
			{
				OpenUITask(spawnedTask);
				break;
			}
		}
	}

	private IEnumerable<UITask> EAllTasks()
	{
		if (uiNuptialFlight.gameObject.activeSelf)
		{
			yield return uiNuptialFlight;
		}
		foreach (UITask_InstinctIdea spawnedTask in spawnedTasks)
		{
			if (spawnedTask.gameObject.activeSelf)
			{
				yield return spawnedTask;
			}
		}
		foreach (UITask_TrackBuilding spawnedTracker in spawnedTrackers)
		{
			if (spawnedTracker.gameObject.activeSelf)
			{
				yield return spawnedTracker;
			}
		}
	}

	public void UpdateNuptialFlightStats()
	{
		uiNuptialFlight.UpdateStats();
	}

	public Vector3 GetTechTreeButtonPos()
	{
		return btOpenTechTree.rtBase.position;
	}

	public void StartCurrencyAnimation(InventorPoints points_type, int n, Transform start_point)
	{
		if (spawnedCurrencies_disabled.Count < n)
		{
			int num = n - spawnedCurrencies_disabled.Count;
			for (int i = 0; i < num; i++)
			{
				UITechCurrency uITechCurrency = UIBase.Spawn<UITechCurrency>(prefabUiTechCurrency.gameObject);
				uITechCurrency.Show(target: false);
				spawnedCurrencies_disabled.Add(uITechCurrency);
			}
		}
		for (int j = 0; j < n; j++)
		{
			UITechCurrency uITechCurrency2 = spawnedCurrencies_disabled[0];
			spawnedCurrencies_disabled.Remove(uITechCurrency2);
			spawnedCurrencies_active.Add(uITechCurrency2);
			uITechCurrency2.Init(points_type);
			uITechCurrency2.StartAnimation(points_type, start_point, 0f, UnityEngine.Random.Range(0.2f, 0.3f), UnityEngine.Random.Range(0.5f, 1.5f));
		}
	}

	public void EndCurrency(InventorPoints points_type, UITechCurrency _curr)
	{
		_curr.Show(target: false);
		if (spawnedCurrencies_active.Contains(_curr))
		{
			spawnedCurrencies_active.Remove(_curr);
		}
		spawnedCurrencies_disabled.Add(_curr);
		techtreeButtonTimer = 0f;
		Progress.AddInventorPoints(points_type, 1, preview: true);
		UpdateTechTreeButtonCurrencies();
		if (UITechTree.instance != null)
		{
			UITechTree.instance.SetInventorPoints();
		}
		if (points_type == InventorPoints.GYNE_T1 || points_type == InventorPoints.GYNE_T2 || points_type == InventorPoints.GYNE_T3)
		{
			float[] array = new float[3] { 1f, 1.1f, 1.2f };
			AudioManager.PlayUI(UISfx.DevelopShimmerGyne, array[UnityEngine.Random.Range(0, array.Length)]);
		}
		else if (Toolkit.CoinFlip())
		{
			float[] array2 = new float[2] { 1f, 1.1f };
			AudioManager.PlayUI(UISfx.DevelopShimmer, array2[UnityEngine.Random.Range(0, array2.Length)]);
		}
	}

	public void UpdateTechTreeButtonCurrencies()
	{
		Dictionary<InventorPoints, int> dicInventorPoints = Progress.GetDicInventorPoints(preview: true);
		bool active = false;
		foreach (TechCurrencyTIB listButtonCurrency in listButtonCurrencies)
		{
			if (dicInventorPoints.ContainsKey(listButtonCurrency.type))
			{
				listButtonCurrency.tib.SetObActive(active: true);
				listButtonCurrency.tib.SetText(dicInventorPoints[listButtonCurrency.type].ToString());
				if (listButtonCurrency.type == InventorPoints.GYNE_T1 || listButtonCurrency.type == InventorPoints.GYNE_T2 || listButtonCurrency.type == InventorPoints.GYNE_T3)
				{
					active = true;
				}
			}
			else
			{
				listButtonCurrency.tib.SetObActive(active: false);
			}
		}
		rtBottomCurrencies.SetObActive(active);
	}

	private void InitInventory()
	{
		inventoryInited = true;
		prefabInventoryItem.SetObActive(active: false);
		foreach (UIInventoryCategory inventoryPickupCategory in inventoryPickupCategories)
		{
			UnityEngine.Object.Destroy(inventoryPickupCategory.gameObject);
		}
		inventoryPickupCategories.Clear();
		if (inventoryAntBuildingCategory != null)
		{
			UnityEngine.Object.Destroy(inventoryAntBuildingCategory.gameObject);
		}
		inventoryAntBuildingCategory = null;
		if (inventoryAntPickupCategory != null)
		{
			UnityEngine.Object.Destroy(inventoryAntPickupCategory.gameObject);
		}
		inventoryAntBuildingCategory = null;
		if (inventoryAntCategory != null)
		{
			UnityEngine.Object.Destroy(inventoryAntCategory.gameObject);
		}
		inventoryAntBuildingCategory = null;
		foreach (PickupCategoryData inventoryCategory in PickupCategoryData.GetInventoryCategories())
		{
			if (inventoryCategory.category == PickupCategory.LIVING)
			{
				continue;
			}
			UIInventoryCategory component = UnityEngine.Object.Instantiate(prefabInventoryItem, prefabInventoryItem.transform.parent).GetComponent<UIInventoryCategory>();
			component.Init(inventoryCategory.GetTitle());
			inventoryPickupCategories.Add(component);
			List<PickupData> list = new List<PickupData>();
			foreach (PickupType item in PickupData.EAllPickupTypes())
			{
				if (item.IsCategory(inventoryCategory.category))
				{
					list.Add(PickupData.Get(item));
				}
			}
			list.Sort((PickupData t1, PickupData t2) => t1.order.CompareTo(t2.order));
			foreach (PickupData item2 in list)
			{
				component.AddPickupItem(item2.type);
			}
			component.SetObActive(active: false);
		}
		for (int num = 0; num < 3; num++)
		{
			UIInventoryCategory component2 = UnityEngine.Object.Instantiate(prefabInventoryItem, prefabInventoryItem.transform.parent).GetComponent<UIInventoryCategory>();
			switch (num)
			{
			case 0:
				component2.Init(Loc.GetUI("INVENTORY_ANT_CAT_BUILDINGS"));
				inventoryAntBuildingCategory = component2;
				foreach (string item3 in BuildingData.EAllAntBuildings())
				{
					component2.AddBuildingItem(item3);
				}
				break;
			case 1:
				component2.Init(Loc.GetUI("INVENTORY_ANT_CAT_LARVAE"));
				inventoryAntPickupCategory = component2;
				foreach (PickupType item4 in PickupData.EAllLarvae())
				{
					component2.AddPickupItem(item4);
				}
				break;
			case 2:
				component2.Init(Loc.GetUI("INVENTORY_ANT_CAT_WORKERS"));
				inventoryAntCategory = component2;
				foreach (AntCasteData antCaste in PrefabData.antCastes)
				{
					component2.AddAntItem(antCaste.caste);
				}
				break;
			}
		}
	}

	public void ResetInventory()
	{
		inventoryInited = false;
	}

	public void CountInventory()
	{
		if (antInventory)
		{
			GameManager.instance.CountAntInventory();
		}
		else
		{
			GameManager.instance.CountPickupInventory();
		}
	}

	public void SetPickupInventory(Dictionary<PickupType, int> dic_inventory)
	{
		if (!inventoryInited)
		{
			InitInventory();
		}
		int num = 0;
		inventoryAntBuildingCategory.Hide();
		inventoryAntPickupCategory.Hide();
		inventoryAntCategory.Hide();
		foreach (UIInventoryCategory inventoryPickupCategory in inventoryPickupCategories)
		{
			num += inventoryPickupCategory.ShowPickups(dic_inventory);
		}
	}

	public void SetAntInventory(Dictionary<string, int> dic_buildingAnts, Dictionary<PickupType, int> dic_larvae, Dictionary<AntCaste, int> dic_ants)
	{
		if (!inventoryInited)
		{
			InitInventory();
		}
		int num = 0;
		foreach (UIInventoryCategory inventoryPickupCategory in inventoryPickupCategories)
		{
			inventoryPickupCategory.Hide();
		}
		num += inventoryAntBuildingCategory.ShowBuildings(dic_buildingAnts);
		num += inventoryAntPickupCategory.ShowPickups(dic_larvae);
		num += inventoryAntCategory.ShowAnts(dic_ants);
	}

	public void SetInventoryTitle()
	{
		if (antInventory)
		{
			tibInventoryTitle.SetText(GameManager.instance.mapMode ? Loc.GetUI("INVENTORY_ANTS_GLOBAL") : Loc.GetUI("INVENTORY_ANTS"));
		}
		else
		{
			tibInventoryTitle.SetText(GameManager.instance.mapMode ? Loc.GetUI("INVENTORY_PICKUPS_GLOBAL") : Loc.GetUI("INVENTORY_PICKUPS"));
		}
	}

	public bool AntInventory()
	{
		return antInventory;
	}

	private string GetInventoryTitleHover()
	{
		if (!antInventory)
		{
			return Loc.GetUI("GAME_TOGGLE_INVENTORY_PICKUP");
		}
		return Loc.GetUI("GAME_TOGGLE_INVENTORY_ANT");
	}

	public void ShowTutorialLog()
	{
		UIBaseSingleton.Get(UITutorial.instance).Init(UITutorial.latsSelectedTutorial, log_mode: true);
	}

	public bool SetTutorial(Tutorial _tutorial, Action on_close = null)
	{
		if (_tutorial == Tutorial.NONE || (Player.HasSeenTutorial(_tutorial) && !DebugSettings.standard.alwaysPopupTutorials))
		{
			return false;
		}
		Player.SetSeenTutorial(_tutorial);
		SetTutorialButton();
		UIBaseSingleton.Get(UITutorial.instance).Init(_tutorial, log_mode: false, on_close);
		return true;
	}

	public void SetTutorialAfterTime(Tutorial _tutorial, float wait)
	{
		if (!Player.HasSeenTutorial(_tutorial))
		{
			StartCoroutine(CTutorialAfterTimer(_tutorial, wait));
		}
	}

	private IEnumerator CTutorialAfterTimer(Tutorial tut, float wait)
	{
		float t = 0f;
		while (t < wait)
		{
			switch (GameManager.instance.GetStatus())
			{
			case GameStatus.RUNNING:
			case GameStatus.PAUSED:
				t += Time.deltaTime;
				break;
			case GameStatus.STOPPED:
				yield break;
			}
			yield return null;
		}
		SetTutorial(tut);
	}

	private void SetTutorialButton()
	{
		tibTutorialButton.SetObActive(Player.seenTutorials.Count > 0);
	}

	public void MiddleMessage()
	{
		rtMiddleMessage.SetObActive(active: false);
		timeHideMiddleMessage = 0f;
	}

	public void MiddleMessage(string str, float dur = 3f)
	{
		rtMiddleMessage.SetObActive(active: true);
		lbMiddleMessage.text = str;
		timeHideMiddleMessage = Time.realtimeSinceStartup + dur;
	}

	public void SetSelected(ClickableObject click_ob, bool refresh = false)
	{
		if (click_ob == null || click_ob.GetUiClickType() == UIClickType.OLD || click_ob.GetUiClickType() == UIClickType.NONE)
		{
			uiBuildingMenu.Show(!GameManager.instance.mapMode);
			uiClick.Clear();
			uiClick.Show(target: false);
			return;
		}
		uiBuildingMenu.Show(target: false);
		uiClick.Show(target: true);
		if (!refresh)
		{
			uiClick.Setup(click_ob.GetUiClickType());
		}
		click_ob.SetClickUi(uiClick.currentLayout);
	}

	public void SetLogicControl(TrailGate click_ob)
	{
		if (click_ob == null || click_ob.GetUiClickType() == UIClickType.OLD || click_ob.GetUiClickType() == UIClickType.NONE)
		{
			uiLogicControl.SetObActive(active: false);
			return;
		}
		uiLogicControl.SetObActive(active: true);
		uiLogicControl.Init(click_ob.GetUiClickType());
		click_ob.SetClickUi_LogicControl(uiLogicControl.currentLayout);
	}

	public void UpdateLogicControl(TrailGate _gate)
	{
		if (uiLogicControl.isActiveAndEnabled && !(_gate == null))
		{
			uiLogicControl.UpdateLogicControl(_gate);
		}
	}

	public void UpdateNoticeInventory()
	{
		noticeInventory.Show(noticeInventory.ShouldNotice() && GameManager.instance.mapMode && !antInventory && GameManager.instance.GetAntCount() > 10);
	}

	public void SetRevealIsland()
	{
		tibRevealIsland.SetObActive(Progress.CanReveal());
	}

	public void SetWaitImage(bool active)
	{
		obWait.SetObActive(active);
	}

	public void SetPause(bool target)
	{
		rtPause.SetObActive(target);
		tibPausePlay.SetImage(target ? sprPlay : sprPause);
		string uI = Loc.GetUI(target ? "INPUT_GAME_RESUME" : "INPUT_GAME_PAUSE");
		tibPausePlay.SetHoverText(uI);
	}

	public void UpdateAntCount(bool force_update = false)
	{
		int antCount = GameManager.instance.GetAntCount();
		int num = ((Hunger.main == null || !Progress.HasUnlocked(GeneralUnlocks.HUNGER_BAR)) ? (-1) : Hunger.main.maxPopulation);
		int num2 = 100 * num + antCount;
		if (num2 != antCountHashPrev || force_update)
		{
			antCountHashPrev = num2;
			lbAntCount.text = antCount.ToString();
			obMaxPop.SetObActive(num > 0);
			lbMaxPop.text = num.ToString();
		}
	}

	private string GetAntCountMessage()
	{
		string text = "";
		if (GameManager.instance.GetAntCount() > 0)
		{
			text = Loc.GetUI("GAME_POPULATION", GameManager.instance.GetAntCount().ToString());
			if (Hunger.main != null)
			{
				text = text + "\n" + Loc.GetUI("GAME_POPULATION_MAX", Hunger.main.maxPopulation.ToString());
			}
		}
		return text;
	}

	public void UpdateHungerBar(int tier, float frac, Color col, float collected_energy, float drain_rate, float larva_rate)
	{
		slHungerBar.value = frac;
		imHungerBar.color = col;
		string text = ((tier == 0) ? Loc.GetUI("HUNGER_EMPTY") : ((tier >= GlobalValues.standard.hungerTiers.Count - 1) ? Loc.GetUI("HUNGER_MAX") : Loc.GetUI("HUNGER_STAGEX", tier.ToString())));
		if (text == "")
		{
			text = tier.ToString();
		}
		lbHungerTier.text = text;
		lbHungerTier.color = col;
		lbLarvaRate.text = larva_rate.ToString();
		lbLarvaUnit.text = Loc.GetUI("GAME_LARVARATE_UNIT");
		collectedEnergy = collected_energy;
		drainRate = drain_rate;
		string text2 = (Mathf.Round(collectedEnergy * 10f) / 10f).ToString();
		string text3 = (Mathf.Round(drainRate * 100f) / 100f).ToString();
		hungerHovertext = Loc.GetUI("GAME_ENERGY_AMOUNT", text2) + "\n" + Loc.GetUI("GAME_ENERGY_DRAIN", text3);
		if (hoveringHunger)
		{
			UIHover.instance.SetText(hungerHovertext);
		}
		antCountHoverText = GetAntCountMessage();
		if (hoveringAntCount)
		{
			UIHover.instance.SetText(antCountHoverText);
		}
		if (tier < 5)
		{
			obLarvaRate.SetObActive(active: false);
			return;
		}
		obLarvaRate.SetObActive(active: true);
		larvaRateHoverText = Loc.GetUI("GAME_LARVARATE", larva_rate.ToString());
		if (hoveringLarvaRate)
		{
			UIHover.instance.SetText(larvaRateHoverText);
		}
	}

	public void SetFilterSelected(Filter _filter)
	{
		ddFilters.SetValueWithoutNotify((int)_filter);
	}

	public void InventoryClicked(string building_code, UIInventoryItem inv_item)
	{
		if (!GameManager.instance.mapMode)
		{
			Building nextBuildingWithCode = GameManager.instance.GetNextBuildingWithCode(building_code, ref clickThroughIndexBuildings, GameManager.instance.closestGround);
			if (nextBuildingWithCode != null)
			{
				CamController.instance.View(nextBuildingWithCode.transform);
			}
		}
	}

	public void InventoryClicked(PickupType pickup_type, UIInventoryItem inv_item)
	{
		if (!GameManager.instance.mapMode)
		{
			Pickup nextPickupOfType = GameManager.instance.GetNextPickupOfType(pickup_type, ref clickThroughIndexPickups, GameManager.instance.closestGround);
			if (nextPickupOfType != null)
			{
				CamController.instance.View(nextPickupOfType.transform);
			}
		}
	}

	public void InventoryClicked(AntCaste ant_caste, UIInventoryItem inv_item)
	{
		if (GameManager.instance.mapMode)
		{
			int num = -1;
			for (int i = 0; i < antCastesInGraph.Count; i++)
			{
				if (antCastesInGraph[i].Item1 == ant_caste)
				{
					num = i;
				}
			}
			if (num == -1)
			{
				if (antCastesInGraph.Count < 6)
				{
					Color color = PickGraphColor();
					inv_item.SetHighlight(color);
					antCastesInGraph.Add((ant_caste, color));
					UpdateGraph();
				}
			}
			else
			{
				antCastesInGraph.RemoveAt(num);
				inv_item.SetHighlight();
				UpdateGraph();
			}
		}
		else
		{
			Transform followTarget = CamController.instance.GetFollowTarget();
			Ant ant = ((followTarget == null) ? null : followTarget.GetComponent<Ant>());
			clickThroughIndexAnts = ((ant != null) ? GameManager.instance.GetAntIndex(ant) : clickThroughIndexAnts);
			ant = GameManager.instance.GetNextAntOfCaste(ant_caste, ref clickThroughIndexAnts, GameManager.instance.closestGround);
			CamController.instance.SetFollowTarget(ant.transform);
		}
	}

	private Color PickGraphColor()
	{
		for (int i = 0; i < graphColors.Length; i++)
		{
			Color color = graphColors[i];
			bool flag = true;
			for (int j = 0; j < antCastesInGraph.Count; j++)
			{
				if (antCastesInGraph[j].Item2 == color)
				{
					flag = false;
				}
			}
			if (flag)
			{
				return color;
			}
		}
		return new Color(0.5f + UnityEngine.Random.value * 0.5f, 0.5f + UnityEngine.Random.value * 0.5f, 0.5f + UnityEngine.Random.value * 0.5f);
	}

	public void ShowGraph(bool show)
	{
		uiGraph.SetObActive(show);
		foreach (var item4 in antCastesInGraph)
		{
			AntCaste item = item4.Item1;
			Color item2 = item4.Item2;
			UIInventoryItem item3 = inventoryAntCategory.GetItem(item);
			if (item3 != null)
			{
				if (show)
				{
					item3.SetHighlight(item2);
				}
				else
				{
					item3.SetHighlight();
				}
			}
		}
		if (show)
		{
			UpdateGraphButtons();
			UpdateGraph();
		}
	}

	private void UpdateGraph()
	{
		uiGraph.Prepare();
		int amount = ((graphTime == 1) ? 60 : Mathf.Max(History.GetCount(), 15));
		bool flag = true;
		foreach (var item3 in antCastesInGraph)
		{
			AntCaste item = item3.Item1;
			Color item2 = item3.Item2;
			List<float> populationHistory = History.GetPopulationHistory(item, amount);
			uiGraph.AddLine(populationHistory, item2, 1.5f);
			flag = false;
		}
		uiGraph.Draw();
		uiGraph.SetSmall(flag);
		lbGraphHint.SetObActive(flag);
		btGraphHour.Show(!flag);
		btGraphInfinite.Show(!flag);
	}

	private void UpdateGraphButtons()
	{
		btGraphHour.SetImageColor((graphTime == 1) ? Color.white : Color.grey);
		btGraphInfinite.SetImageColor((graphTime == 0) ? Color.white : Color.grey);
	}

	private void SetGraphTime(int t)
	{
		if (graphTime != t)
		{
			graphTime = t;
			UpdateGraphButtons();
			UpdateGraph();
		}
	}

	public void UpdateTrailDragLock()
	{
		rtDraggingDisabled.SetObActive(Player.disableTrailDragging);
		string text = Loc.GetUI(Player.disableTrailDragging ? "INPUT_TRAILDRAGGING_UNLOCK" : "INPUT_TRAILDRAGGING_LOCK");
		string desc = InputManager.GetDesc(InputAction.TrailDragLock);
		if (!string.IsNullOrEmpty(desc))
		{
			text = text + " (" + desc + ")";
		}
		tibToggleDragging.SetHoverText(text);
	}

	public void OnMenuClose()
	{
		UpdateTrailDragLock();
	}

	public void Read(Save save)
	{
		trackedBuildings = new List<Building>();
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Building building = GameManager.instance.FindLink<Building>(save.ReadInt());
			if (building != null)
			{
				trackedBuildings.Add(building);
			}
		}
		lastTaskOpen = TaskID.Read(save);
		Gameplay.instance.currentGroup = (BuildingGroup)save.ReadInt();
	}

	public void Write(Save save)
	{
		save.Write(trackedBuildings.Count);
		foreach (Building trackedBuilding in trackedBuildings)
		{
			save.Write((!(trackedBuilding == null)) ? trackedBuilding.linkId : 0);
		}
		lastTaskOpen.Write(save);
		save.Write((int)Gameplay.instance.currentGroup);
	}
}
