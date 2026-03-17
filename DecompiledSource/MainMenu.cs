using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : KoroutineBehaviour
{
	public enum AnimState
	{
		None,
		IntroStart,
		IntroEnd,
		MainStart,
		MainEnd
	}

	[SerializeField]
	private Transform tfMenu;

	[SerializeField]
	private UIButtonText btNewGame;

	[SerializeField]
	private UIButtonText btContinue;

	[SerializeField]
	private UIButtonText btLoadGame;

	[SerializeField]
	private UIButtonText btSettings;

	[SerializeField]
	private UIButtonText btCredits;

	[SerializeField]
	private UIButtonText btQuit;

	[SerializeField]
	private UIButton btWishlist;

	[SerializeField]
	private UIButton btDiscord;

	[SerializeField]
	private GameObject uiSettingsPrefab;

	[SerializeField]
	private GameObject uiWorldSettingsPrefab;

	[SerializeField]
	private string backgroundSaveGame;

	[SerializeField]
	private Image imLogo;

	[SerializeField]
	private Image imLogoChinese;

	[SerializeField]
	private TextMeshProUGUI lbDemo;

	[SerializeField]
	private TextMeshProUGUI lbDemoChinese;

	[SerializeField]
	private RectTransform rtVersionHistory;

	[SerializeField]
	private TextMeshProUGUI lbChangelog;

	[SerializeField]
	private UITextImageButton btCurrentVersion;

	[SerializeField]
	private UIButton btCloseVersionHistory;

	[SerializeField]
	private List<Animator> menuAnimators;

	[Tooltip("After triggering intro fade out, delay before starting main fade in")]
	[SerializeField]
	private float introFadeOutDuration;

	[Tooltip("After triggering intro fade out, delay before starting main fade in")]
	[SerializeField]
	private float menuFadeOutDuration;

	public AnimState debugAnimState;

	private AnimState animState;

	private bool inputEnabled;

	private bool chineseLogo;

	private static bool firstStartDone;

	private float timeShowSub;

	private void Awake()
	{
		debugAnimState = AnimState.None;
	}

	public void Start()
	{
		UIGlobal.instance.GoBlack(black: true, 0f);
		SetAnimState();
		GameInit.instance.Setup(delegate(string fatal_error)
		{
			if (fatal_error == null)
			{
				StartKoroutine(KStartMenu());
			}
		}, delegate
		{
		});
	}

	private string GetSubText()
	{
		return DebugSettings.standard.buildType switch
		{
			GameType.Demo => Loc.Upper(Loc.GetUI("GENERIC_DEMO")), 
			GameType.Prologue => Loc.Upper(Loc.GetUI("GENERIC_PROLOGUE")), 
			_ => "", 
		};
	}

	private IEnumerator KStartMenu()
	{
		bool done = false;
		KoroutineId kid = SetFinalizer(delegate
		{
			if (done)
			{
				SetAnimState(AnimState.MainStart);
				UIGlobal.instance.GoBlack(black: false, 0.1f);
				inputEnabled = true;
			}
		});
		try
		{
			AudioManager.instance.Init();
			chineseLogo = Player.language == Language.CHINESE_SIMPLIFIED;
			string sub = GetSubText();
			TextMeshProUGUI textMeshProUGUI = lbDemoChinese;
			string text = (lbDemo.text = sub);
			textMeshProUGUI.text = text;
			HideSub();
			btWishlist.SetObActive(active: false);
			btDiscord.SetObActive(active: false);
			btCurrentVersion.SetObActive(active: false);
			AudioManager.PlayMusic(MusicType.Menu, dont_stop: true);
			imLogo.SetObActive(!chineseLogo);
			imLogoChinese.SetObActive(chineseLogo);
			if (!firstStartDone)
			{
				SetAnimState(AnimState.IntroStart);
				UIGlobal.instance.GoBlack(black: false, 0.1f);
				if (sub != "")
				{
					ShowSub(0.5f);
				}
				while (!Input.anyKeyDown)
				{
					yield return null;
				}
				HideSub();
				SetAnimState(AnimState.IntroEnd);
				yield return new WaitForSeconds(introFadeOutDuration - 0.1f);
				UIGlobal.instance.GoBlack(black: true, 0.1f);
				yield return new WaitForSeconds(0.1f);
				firstStartDone = true;
			}
			btNewGame.Init(delegate
			{
				if (inputEnabled)
				{
					tfMenu.SetObActive(active: false);
					UIBaseSingleton.Get(UIWorldSettings.instance, uiWorldSettingsPrefab).Init(delegate
					{
						tfMenu.SetObActive(active: true);
					}, delegate
					{
						StartGame("");
					});
				}
			});
			if (Player.IsLastSaveValid())
			{
				btContinue.Init(delegate
				{
					if (inputEnabled)
					{
						StartGame(Player.lastSave);
					}
				});
			}
			else
			{
				btContinue.GetComponentInChildren<TMP_Text>().text = "";
				btContinue.GetComponentInChildren<AutoLoc>().code = "";
			}
			btLoadGame.Init(delegate
			{
				if (inputEnabled)
				{
					tfMenu.SetObActive(active: false);
					UIBaseSingleton.Get(UILoadSave.instance).Init(LoadSaveType.LOAD, delegate(string save_name)
					{
						UILoadSave.instance.Show(target: false);
						StartGame(save_name);
					}, delegate
					{
						tfMenu.SetObActive(active: true);
					});
				}
			});
			btSettings.Init(delegate
			{
				if (inputEnabled)
				{
					tfMenu.SetObActive(active: false);
					UIBaseSingleton.Get(UISettings.instance, uiSettingsPrefab).Init(in_game: false, delegate
					{
						bool flag = Player.language == Language.CHINESE_SIMPLIFIED;
						if (chineseLogo != flag)
						{
							chineseLogo = flag;
							string subText = GetSubText();
							(chineseLogo ? lbDemoChinese : lbDemo).SetObActive(subText != "");
							imLogo.SetObActive(!chineseLogo);
							imLogoChinese.SetObActive(chineseLogo);
							SetAnimState(AnimState.MainStart, forced: true);
						}
						tfMenu.SetObActive(active: true);
					});
				}
			});
			btCredits.Init(delegate
			{
				if (inputEnabled)
				{
					OpenCredits();
				}
			});
			btQuit.Init(delegate
			{
				Quit();
			});
			if (sub != "")
			{
				ShowSub(4.5f);
				btWishlist.SetObActive(active: true);
				btWishlist.Init(delegate
				{
					if (inputEnabled)
					{
						Application.OpenURL(GlobalValues.standard.steamPageLink);
					}
				});
			}
			btDiscord.SetObActive(active: true);
			btCurrentVersion.SetObActive(active: true);
			btCurrentVersion.SetText(DebugSettings.standard.currentVersion);
			btCurrentVersion.SetButton(delegate
			{
				StartCoroutine(COpenVersionHistory(open: true));
			});
			btCloseVersionHistory.Init(delegate
			{
				StartCoroutine(COpenVersionHistory(open: false));
			});
			lbChangelog.text = Changelog.GetText();
			yield return StartKoroutine(kid, GameManager.instance.KStartGame(backgroundSaveGame, debug_start: false));
			done = true;
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void StartGame(string save_file)
	{
		StartCoroutine(CContinue(delegate
		{
			GlobalGameState.GoToGame(save_file);
		}));
	}

	private void OpenCredits()
	{
		StartCoroutine(CContinue(delegate
		{
			GlobalGameState.GoToCredits();
		}));
	}

	private void Quit()
	{
		StartCoroutine(CContinue(delegate
		{
			GlobalGameState.Quit();
		}));
	}

	private IEnumerator CContinue(Action action)
	{
		inputEnabled = false;
		SetAnimState(AnimState.MainEnd);
		if (GetSubText() != "")
		{
			btWishlist.SetObActive(active: false);
			(chineseLogo ? lbDemoChinese : lbDemo).SetObActive(active: false);
		}
		btDiscord.SetObActive(active: false);
		for (float f = 0f; f < 1f; f += Time.deltaTime / menuFadeOutDuration)
		{
			AudioManager.SetMusicVolumeFactor(1f - f);
			yield return null;
		}
		AudioManager.StopMusic();
		AudioManager.SetMusicVolumeFactor(1f);
		UIGlobal.instance.GoBlack(black: true, 0f);
		action();
	}

	private void ShowSub(float delay)
	{
		timeShowSub = delay;
	}

	private void HideSub()
	{
		(chineseLogo ? lbDemoChinese : lbDemo).SetObActive(active: false);
		timeShowSub = 0f;
	}

	private void Update()
	{
		if (timeShowSub > 0f)
		{
			timeShowSub -= Time.deltaTime;
			if (timeShowSub < 0f)
			{
				(chineseLogo ? lbDemoChinese : lbDemo).SetObActive(active: true);
			}
		}
	}

	private void SetAnimState(AnimState state = AnimState.None, bool forced = false)
	{
		if (state == AnimState.None)
		{
			StartCoroutine(CMenuAnimReset());
		}
		if (!(animState != state || forced))
		{
			return;
		}
		animState = (debugAnimState = state);
		foreach (Animator item in EMenuAnimators())
		{
			item.SetInteger("MenuAnimState", (int)state);
		}
	}

	private IEnumerator CMenuAnimReset()
	{
		foreach (Animator item in EMenuAnimators())
		{
			item.SetBool("MenuAnimReset", value: true);
		}
		yield return null;
		foreach (Animator item2 in EMenuAnimators())
		{
			item2.SetBool("MenuAnimReset", value: false);
		}
	}

	private IEnumerable<Animator> EMenuAnimators()
	{
		foreach (Animator menuAnimator in menuAnimators)
		{
			if (menuAnimator.isActiveAndEnabled)
			{
				yield return menuAnimator;
			}
		}
	}

	private IEnumerator COpenVersionHistory(bool open)
	{
		float start = (open ? 0f : (0f - rtVersionHistory.sizeDelta.x));
		float end = (open ? (0f - rtVersionHistory.sizeDelta.x) : 0f);
		float duration = 0.5f;
		for (float t = 0f; t < duration; t += Time.deltaTime)
		{
			Vector2 anchoredPosition = rtVersionHistory.anchoredPosition;
			anchoredPosition.x = start + (end - start) * GlobalValues.standard.curveEaseIn.Evaluate(t / duration);
			rtVersionHistory.anchoredPosition = anchoredPosition;
			yield return null;
		}
		Vector2 anchoredPosition2 = rtVersionHistory.anchoredPosition;
		anchoredPosition2.x = end;
		rtVersionHistory.anchoredPosition = anchoredPosition2;
	}
}
