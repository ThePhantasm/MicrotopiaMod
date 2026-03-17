using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITutorial : UIBaseSingleton
{
	public static UITutorial instance;

	public RectTransform rtItems;

	public TextMeshProUGUI lbPrev;

	public TextMeshProUGUI lbNext;

	public TextMeshProUGUI lbWishlist;

	public TextMeshProUGUI lbDemoQuit;

	public TextMeshProUGUI lbDemoComplete;

	public UIButton btPrev;

	public UIButton btNext;

	public UIButton btWishlist;

	public UITextImageButton btClose;

	public UITextImageButton prefabTutorialItem;

	public List<TutorialScreenLink> tutorials = new List<TutorialScreenLink>();

	private int currentScreen;

	private bool logMode;

	private Dictionary<Tutorial, TutorialScreenLink> dicTutorials;

	private bool wasPaused;

	private List<Tutorial> tutorialsInList = new List<Tutorial>();

	private List<UITextImageButton> spawnedTutorialItems = new List<UITextImageButton>();

	public static Tutorial latsSelectedTutorial = Tutorial.WORKERS;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init(Tutorial _tutorial, bool log_mode, Action on_close = null)
	{
		if (dicTutorials == null)
		{
			dicTutorials = new Dictionary<Tutorial, TutorialScreenLink>();
			foreach (TutorialScreenLink tutorial in tutorials)
			{
				dicTutorials.Add(tutorial.tutorial, tutorial);
			}
		}
		if (_tutorial == Tutorial.NONE)
		{
			Show(target: false);
			return;
		}
		latsSelectedTutorial = _tutorial;
		logMode = log_mode;
		if (logMode)
		{
			rtItems.SetObActive(active: true);
			if (spawnedTutorialItems.Count == 0)
			{
				tutorialsInList.Clear();
				foreach (TutorialScreenLink tutorial2 in tutorials)
				{
					if (tutorial2.inList)
					{
						tutorialsInList.Add(tutorial2.tutorial);
					}
				}
				for (int i = 0; i < tutorialsInList.Count; i++)
				{
					UITextImageButton uITextImageButton = UnityEngine.Object.Instantiate(prefabTutorialItem, prefabTutorialItem.transform.parent);
					spawnedTutorialItems.Add(uITextImageButton);
					uITextImageButton.SetObActive(active: true);
				}
				prefabTutorialItem.SetObActive(active: false);
			}
			for (int j = 0; j < tutorialsInList.Count; j++)
			{
				Tutorial tut = tutorialsInList[j];
				spawnedTutorialItems[j].ResetOverlays();
				if (Player.HasSeenTutorial(tut) || DebugSettings.standard.logShowAllTutorials || Player.cheatShowAllTutorials)
				{
					spawnedTutorialItems[j].SetText(Loc.GetTutorial("TUTT_" + tut));
					spawnedTutorialItems[j].SetButton(delegate
					{
						Init(tut, log_mode, on_close);
					});
					if (tut == _tutorial)
					{
						spawnedTutorialItems[j].AddOverlay(OverlayTypes.SELECTED);
					}
				}
				else
				{
					spawnedTutorialItems[j].SetText(Loc.GetTutorial("TUTT_NONE"));
					spawnedTutorialItems[j].SetButton(null);
				}
			}
			btClose.SetObActive(active: true);
			btClose.SetButton(delegate
			{
				CloseTutorial(on_close);
			});
		}
		else
		{
			rtItems.SetObActive(active: false);
			btClose.SetObActive(active: false);
		}
		currentScreen = 0;
		UpdateScreen(currentScreen, _tutorial);
		int screen_count = dicTutorials[_tutorial].screens.Count;
		btPrev.Init(delegate
		{
			if (_tutorial == Tutorial.DEMO_COMPLETE || _tutorial == Tutorial.DEMO_QUIT)
			{
				CloseTutorial(on_close);
			}
			else
			{
				currentScreen = Mathf.Clamp(currentScreen - 1, 0, screen_count - 1);
				UpdateScreen(currentScreen, _tutorial);
			}
		});
		btNext.Init(delegate
		{
			currentScreen = Mathf.Clamp(currentScreen + 1, 0, screen_count);
			if (currentScreen < screen_count)
			{
				UpdateScreen(currentScreen, _tutorial);
			}
			else
			{
				CloseTutorial(on_close);
			}
		});
		btWishlist.Init(delegate
		{
			Application.OpenURL(GlobalValues.standard.steamPageLink);
		});
		lbDemoQuit.SetText(DebugSettings.standard.prologue ? Loc.GetTutorial("TUT_PROLOGUEQUIT0") : Loc.GetTutorial("TUT_DEMOQUIT0"));
		lbDemoComplete.SetText(DebugSettings.standard.prologue ? Loc.GetTutorial("TUT_PROLOGUECOMPLETE0") : Loc.GetTutorial("TUT_DEMOCOMPLETE0"));
		Show(target: true);
		wasPaused = GameManager.instance.GetStatus() == GameStatus.PAUSED;
		GameManager.instance.SetStatus(GameStatus.TUTORIAL);
		Filters.Update(Filter.HIDE_UI);
	}

	private void UpdateScreen(int i, Tutorial _tutorial)
	{
		foreach (TutorialScreenLink tutorial in tutorials)
		{
			foreach (GameObject screen in tutorial.screens)
			{
				screen.SetObActive(active: false);
			}
		}
		if (!dicTutorials.ContainsKey(_tutorial))
		{
			Debug.LogWarning("No screens found for tutorial " + _tutorial);
			return;
		}
		dicTutorials[_tutorial].screens[i].SetObActive(active: true);
		if (_tutorial == Tutorial.DEMO_COMPLETE || _tutorial == Tutorial.DEMO_QUIT)
		{
			btPrev.SetObActive(active: true);
			lbPrev.text = Loc.GetUI("GENERIC_OK");
			btNext.SetObActive(active: false);
			btWishlist.SetObActive(active: true);
			lbWishlist.text = Loc.GetUI("TUTORIAL_WISHLISTNOW");
		}
		else
		{
			btPrev.SetObActive(i != 0);
			lbPrev.text = Loc.GetUI("GENERIC_PREVIOUS");
			btNext.SetObActive(i < dicTutorials[_tutorial].screens.Count - 1 || !logMode);
			lbNext.text = ((i < dicTutorials[_tutorial].screens.Count - 1) ? Loc.GetUI("GENERIC_NEXT") : Loc.GetUI("GENERIC_OK"));
			btWishlist.SetObActive(active: false);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape) && btClose.gameObject.activeInHierarchy)
		{
			btClose.Click();
		}
	}

	public void SetBtPrevText(string s)
	{
		lbPrev.text = s;
	}

	public static Tutorial ParseTutorialScreen(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return Tutorial.NONE;
		}
		if (Enum.TryParse<Tutorial>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("TutorialScreen parse error; '" + str + "' invalid");
		return Tutorial.NONE;
	}

	private void CloseTutorial(Action on_close)
	{
		Show(target: false);
		if (GameManager.instance.GetStatus() == GameStatus.TUTORIAL)
		{
			GameManager.instance.SetStatus((!wasPaused) ? GameStatus.RUNNING : GameStatus.PAUSED);
		}
		on_close?.Invoke();
		Filters.Update(Filter.HIDE_UI);
	}

	public static Tutorial ParseTutorial(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return Tutorial.NONE;
		}
		if (Enum.TryParse<Tutorial>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Tutorial parse error; '" + str + "' invalid");
		return Tutorial.NONE;
	}
}
