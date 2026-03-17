using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIEscMenu : UIBaseSingleton
{
	public static UIEscMenu instance;

	[SerializeField]
	private TextMeshProUGUI lbMenu;

	[SerializeField]
	private UIButtonText btResume;

	[SerializeField]
	private UIButtonText btRestart;

	[SerializeField]
	private UIButtonText btSaveGame;

	[SerializeField]
	private UIButtonText btLoadGame;

	[SerializeField]
	private UIButtonText btSettings;

	[SerializeField]
	private UIButtonText btQuitMenu;

	[SerializeField]
	private UIButtonText btQuitDesktop;

	[SerializeField]
	private List<GameObject> randomEnables = new List<GameObject>();

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
		Init();
	}

	public void Init()
	{
		lbMenu.text = Loc.GetUI("GENERIC_ESC_MENU");
		btResume.Init(delegate
		{
			GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
		}, Loc.GetUI("ESCMENU_RESUME"));
		btLoadGame.Init(delegate
		{
			UILoadSave ui = UIBaseSingleton.Get(UILoadSave.instance);
			ui.Init(LoadSaveType.LOAD, delegate(string save_name)
			{
				ui.Show(target: false);
				GameManager.instance.SaveGame("_preload");
				GameManager.instance.LoadGameMidGame(save_name, bg: false, null, delegate
				{
					GameManager.instance.LoadGameMidGame("_preload", bg: false, null, null);
				});
			}, null);
			ui.transform.SetAsLastSibling();
		}, Loc.GetUI("ESCMENU_LOADGAME"));
		if (GameManager.instance != null && GameManager.instance.DontSave())
		{
			btSaveGame.SetDisabled(disabled: true);
		}
		else
		{
			btSaveGame.SetDisabled(disabled: false);
			btSaveGame.Init(delegate
			{
				UILoadSave uILoadSave = UIBaseSingleton.Get(UILoadSave.instance);
				uILoadSave.Init(LoadSaveType.SAVE, delegate(string save_name)
				{
					SaveGame(save_name);
				}, null);
				uILoadSave.transform.SetAsLastSibling();
			}, Loc.GetUI("ESCMENU_SAVEGAME"));
		}
		btSettings.Init(delegate
		{
			UISettings uISettings = UIBaseSingleton.Get(UISettings.instance);
			uISettings.Init(in_game: true, delegate
			{
				if (UIGame.instance != null)
				{
					UIGame.instance.OnMenuClose();
				}
			});
			uISettings.transform.SetAsLastSibling();
		}, Loc.GetUI("MAINMENU_SETTINGS"));
		btQuitMenu.Init(delegate
		{
			if (DebugSettings.standard.playtest || (DebugSettings.standard.demo && Instinct.Get("DEMO2_8").status != TaskStatus.COMPLETED))
			{
				Tutorial tutorial = (DebugSettings.standard.playtest ? Tutorial.EA_DONE : Tutorial.DEMO_QUIT);
				GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
				UITutorial uITutorial = UIBaseSingleton.Get(UITutorial.instance);
				uITutorial.Init(tutorial, log_mode: false, delegate
				{
					GameManager.instance.AutoSave();
					GlobalGameState.GoToMainMenu();
				});
				uITutorial.SetBtPrevText(Loc.GetUI("ESCMENU_MAINMENU"));
			}
			else
			{
				GameManager.instance.AutoSave();
				GlobalGameState.GoToMainMenu();
			}
		}, Loc.GetUI("ESCMENU_MAINMENU"));
		btQuitDesktop.Init(delegate
		{
			if (DebugSettings.standard.playtest || (DebugSettings.standard.demo && Instinct.Get("DEMO2_8").status != TaskStatus.COMPLETED))
			{
				Tutorial tutorial = (DebugSettings.standard.playtest ? Tutorial.EA_DONE : Tutorial.DEMO_QUIT);
				GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
				UITutorial uITutorial = UIBaseSingleton.Get(UITutorial.instance);
				uITutorial.Init(tutorial, log_mode: false, delegate
				{
					GameManager.instance.AutoSave();
					GlobalGameState.Quit();
				});
				uITutorial.SetBtPrevText(Loc.GetUI("MAINMENU_QUIT"));
			}
			else
			{
				GameManager.instance.AutoSave();
				GlobalGameState.Quit();
			}
		}, Loc.GetUI("MAINMENU_QUIT"));
		foreach (GameObject randomEnable in randomEnables)
		{
			randomEnable.SetObActive(Toolkit.CoinFlip());
		}
	}

	private void SaveGame(string save_name)
	{
		if (GameManager.instance.SaveGame(save_name))
		{
			GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
			return;
		}
		UIDialogBase uIDialogBase = UIBase.Spawn<UIDialogBase>();
		uIDialogBase.SetText(Loc.GetUI("LOADSAVE_SAVE_ERROR"));
		uIDialogBase.SetAction(DialogResult.OK, uIDialogBase.StartClose);
	}
}
