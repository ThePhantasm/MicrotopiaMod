using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILoadSave : UIBaseSingleton
{
	private struct SaveData
	{
		public string file;

		public string name;

		public string niceName;

		public bool isSpecial;

		public DateTime dateTime;
	}

	public static UILoadSave instance;

	[SerializeField]
	private RectTransform rtScreenshot;

	[SerializeField]
	private TextMeshProUGUI lbTitle;

	[SerializeField]
	private TextMeshProUGUI lbSaveName;

	[SerializeField]
	private TextMeshProUGUI lbPlayTime;

	[SerializeField]
	private TextMeshProUGUI lbLastPlayed;

	[SerializeField]
	private TextMeshProUGUI lbError;

	[SerializeField]
	private TextMeshProUGUI lbLoadSaveButton;

	[SerializeField]
	private UIButton btDeleteSave;

	[SerializeField]
	private UIButtonText btBack;

	[SerializeField]
	private UIButtonText btLoadSave;

	[SerializeField]
	private RawImage imScreenshot;

	[SerializeField]
	private UITextImageButton btNewSave;

	[SerializeField]
	private UITextImageButton prefabBtSave;

	private List<UITextImageButton> spawnedItems = new List<UITextImageButton>();

	private int currentSelected = -1;

	private Texture2D screenshot;

	private Action<string> resultingAction;

	private Action cancelAction;

	private List<SaveData> saves;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init(LoadSaveType _type, Action<string> action, Action cancel_action, bool just_opened = true)
	{
		resultingAction = action;
		cancelAction = cancel_action;
		btBack.Init(delegate
		{
			if (cancelAction != null)
			{
				cancelAction();
			}
			StartClose();
		}, Loc.GetUI("GENERIC_BACK"));
		switch (_type)
		{
		case LoadSaveType.LOAD:
			lbTitle.text = Loc.GetUI("ESCMENU_LOADGAME");
			lbLoadSaveButton.text = Loc.GetUI("LOADSAVE_LOAD");
			btNewSave.SetObActive(active: false);
			break;
		case LoadSaveType.SAVE:
			if (just_opened)
			{
				screenshot = CamController.instance.GetScreenshot();
			}
			lbTitle.text = Loc.GetUI("ESCMENU_SAVEGAME");
			lbLoadSaveButton.text = Loc.GetUI("LOADSAVE_SAVE");
			btNewSave.SetObActive(active: true);
			btNewSave.Init("- " + Loc.GetUI("LOADSAVE_NEWFILE") + " -", delegate
			{
				SetSelected(0, _type);
			});
			btNewSave.ResetOverlays();
			break;
		}
		GatherSaves(_type == LoadSaveType.LOAD);
		int count = saves.Count;
		prefabBtSave.SetObActive(active: false);
		if (spawnedItems.Count < count)
		{
			int num = count - spawnedItems.Count;
			for (int num2 = 0; num2 < num; num2++)
			{
				UITextImageButton component = UnityEngine.Object.Instantiate(prefabBtSave, prefabBtSave.transform.parent).GetComponent<UITextImageButton>();
				spawnedItems.Add(component);
			}
		}
		foreach (UITextImageButton spawnedItem in spawnedItems)
		{
			spawnedItem.SetObActive(active: false);
		}
		for (int num3 = 0; num3 < count; num3++)
		{
			SaveData saveData = saves[num3];
			int target = num3 + 1;
			UITextImageButton uITextImageButton = spawnedItems[num3];
			uITextImageButton.Init(saveData.niceName, delegate
			{
				SetSelected(target, _type);
			});
			uITextImageButton.SetTextColor(saveData.isSpecial ? new Color(0.96f, 0.86f, 0.07f) : Color.white);
			if (saveData.isSpecial)
			{
				uITextImageButton.SetText("<i>" + uITextImageButton.GetText() + "</i>");
			}
			uITextImageButton.ResetOverlays();
			uITextImageButton.SetObActive(active: true);
		}
		SetSelected(-1, _type);
		Show(target: true);
	}

	private void GatherSaves(bool incl_quick_and_auto)
	{
		saves = new List<SaveData>();
		string[] gameSaves = Files.GetGameSaves();
		foreach (string text in gameSaves)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
			bool flag = fileNameWithoutExtension.StartsWith("_");
			if ((!incl_quick_and_auto && flag) || fileNameWithoutExtension == "_busy" || fileNameWithoutExtension == "_preload")
			{
				continue;
			}
			ReadFileShortData(text, out var version, out var _, out var date_time, out var game_type);
			if (!Platform.CanLoad(game_type, version))
			{
				continue;
			}
			SaveData item = new SaveData
			{
				file = text,
				name = fileNameWithoutExtension,
				dateTime = date_time,
				isSpecial = flag
			};
			if (flag)
			{
				if (fileNameWithoutExtension.StartsWith("_quick"))
				{
					item.niceName = Loc.GetUI("LOADSAVE_QUICKSAVE");
				}
				else if (fileNameWithoutExtension.StartsWith("_auto"))
				{
					item.niceName = Loc.GetUI("LOADSAVE_AUTOSAVE");
				}
				else
				{
					Debug.Log("GatherSaves: unknown " + fileNameWithoutExtension);
					item.niceName = fileNameWithoutExtension[1..];
				}
			}
			else
			{
				item.niceName = fileNameWithoutExtension;
			}
			saves.Add(item);
		}
		saves.Sort((SaveData a, SaveData b) => -a.dateTime.CompareTo(b.dateTime));
	}

	private void SetSelected(int n, LoadSaveType type)
	{
		currentSelected = n;
		foreach (UITextImageButton allSaveButton in GetAllSaveButtons())
		{
			allSaveButton.ResetOverlays();
		}
		btDeleteSave.SetObActive(active: false);
		btLoadSave.SetObActive(active: false);
		if (currentSelected < 1)
		{
			if (type == LoadSaveType.SAVE)
			{
				ShowDetails(broken: false, "", screenshot, (float)GameManager.instance.playTime, DateTime.Now);
			}
			else
			{
				ShowDetails();
			}
		}
		if (currentSelected == 0)
		{
			UIDialogueInputField dialog_input = UIBase.Spawn<UIDialogueInputField>();
			dialog_input.SetText(Loc.GetUI("LOADSAVE_FILENAME"));
			dialog_input.SetOnValueChanged(delegate(string str)
			{
				if (str.StartsWith("_"))
				{
					dialog_input.SetValue(str[1..]);
				}
			});
			dialog_input.SetAction(DialogResult.OK, delegate
			{
				string text = dialog_input.GetInput();
				while (text.StartsWith("_"))
				{
					text = text[1..];
				}
				if (text != "")
				{
					if (SaveNameExists(text))
					{
						UIDialogBase dialog = UIBase.Spawn<UIDialogBase>();
						dialog.SetText(Loc.GetUI("LOADSAVE_EXISTS", dialog_input.GetInput()));
						dialog.SetAction(DialogResult.YES, delegate
						{
							dialog.StartClose();
							resultingAction(dialog_input.GetInput());
						});
						dialog.SetAction(DialogResult.NO, dialog.StartClose);
					}
					else
					{
						resultingAction(dialog_input.GetInput());
					}
				}
			});
			dialog_input.SetAction(DialogResult.CANCEL, dialog_input.StartClose);
			dialog_input.SetAllowEmpty(allow: false);
			dialog_input.SetFocus();
		}
		else
		{
			if (currentSelected <= 0)
			{
				return;
			}
			int index = currentSelected - 1;
			spawnedItems[index].AddOverlay(OverlayTypes.SELECTED);
			SaveData save = saves[index];
			ReadFileShortData(save.file, out var version, out var play_time, out var date_time, out var _);
			Texture2D saveScreenshot = GetSaveScreenshot(save.name);
			bool flag = version < 0;
			ShowDetails(flag, save.niceName, saveScreenshot, play_time, date_time);
			if (!flag)
			{
				btLoadSave.SetObActive(active: true);
				switch (type)
				{
				case LoadSaveType.LOAD:
					btLoadSave.Init(delegate
					{
						resultingAction(save.name);
					});
					break;
				case LoadSaveType.SAVE:
					btLoadSave.Init(delegate
					{
						UIDialogBase uIDialogBase = UIBase.Spawn<UIDialogBase>();
						uIDialogBase.SetText(Loc.GetUI("LOADSAVE_OVERWRITE", save.name));
						uIDialogBase.SetAction(DialogResult.YES, delegate
						{
							resultingAction(save.name);
						});
						uIDialogBase.SetAction(DialogResult.NO, uIDialogBase.StartClose);
					});
					break;
				}
			}
			btDeleteSave.SetObActive(active: true);
			btDeleteSave.Init(delegate
			{
				UIDialogBase dialog = UIBase.Spawn<UIDialogBase>();
				dialog.SetText(Loc.GetUI("LOADSAVE_DELETE", save.name));
				dialog.SetAction(DialogResult.YES, delegate
				{
					dialog.StartClose();
					if (File.Exists(save.file))
					{
						File.Delete(save.file);
					}
					else
					{
						Debug.LogError("No file to delete found at " + save.file);
					}
					Init(type, resultingAction, cancelAction, just_opened: false);
				});
				dialog.SetAction(DialogResult.NO, dialog.StartClose);
			});
		}
	}

	private void ShowDetails()
	{
		rtScreenshot.SetObActive(active: false);
		lbSaveName.SetObActive(active: false);
		lbPlayTime.SetObActive(active: false);
		lbLastPlayed.SetObActive(active: false);
		lbError.SetObActive(active: false);
	}

	private void ShowDetails(bool broken, string save_name, Texture2D screenshot, float playtime, DateTime dt)
	{
		rtScreenshot.SetObActive(active: true);
		lbSaveName.SetObActive(active: true);
		lbPlayTime.SetObActive(active: true);
		lbLastPlayed.SetObActive(active: true);
		lbError.SetObActive(broken);
		lbSaveName.text = save_name;
		imScreenshot.texture = screenshot;
		if (broken)
		{
			lbPlayTime.text = Loc.GetUI("LOADSAVE_PLAYTIME", Loc.GetUI("GENERIC_???"));
			lbLastPlayed.text = Loc.GetUI("LOADSAVE_LASTPLAYED", Loc.GetUI("GENERIC_???"));
			lbError.text = Loc.GetUI("LOADSAVE_BROKEN");
		}
		else
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Round(playtime));
			lbPlayTime.text = Loc.GetUI("LOADSAVE_PLAYTIME", $"{Mathf.FloorToInt((float)timeSpan.TotalHours)}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}");
			lbLastPlayed.text = Loc.GetUI("LOADSAVE_LASTPLAYED", dt.ToString("g", Loc.culture));
		}
	}

	private List<UITextImageButton> GetAllSaveButtons()
	{
		List<UITextImageButton> list = new List<UITextImageButton>();
		list.Add(btNewSave);
		list.AddRange(spawnedItems);
		return list;
	}

	public Texture2D GetSaveScreenshot(string name)
	{
		string path = Files.GameSaveImage(name);
		if (File.Exists(path))
		{
			byte[] data = File.ReadAllBytes(path);
			Texture2D texture2D = new Texture2D(2, 2);
			texture2D.LoadImage(data);
			return texture2D;
		}
		return null;
	}

	private bool SaveNameExists(string name)
	{
		return File.Exists(Files.GameSave(name, bg: false));
	}

	public static void ReadFileShortData(string path, out int version, out float play_time, out DateTime date_time, out GameType game_type)
	{
		Save save = new Save();
		save.StartReading(path);
		version = save.version;
		GameManager.LoadHeader(save, out play_time, out var _, out date_time, out game_type);
		save.DoneReading();
	}
}
