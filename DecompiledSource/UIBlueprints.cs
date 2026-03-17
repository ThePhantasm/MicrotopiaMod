using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBlueprints : UIBaseSingleton
{
	private enum SortType
	{
		Name = 1,
		Bar = 2,
		Date = 3,
		Creator = 4,
		Name_Desc = 11,
		Bar_Desc = 12,
		Date_Desc = 13,
		Creator_Desc = 14
	}

	public static UIBlueprints instance;

	[SerializeField]
	private RectTransform rtScreenshot;

	[SerializeField]
	private TMP_Text lbTitle;

	[SerializeField]
	private TMP_Text lbBlueprintName;

	[SerializeField]
	private TMP_Text lbDetails;

	[SerializeField]
	private TMP_Text lbPlaceButton;

	[SerializeField]
	private TMP_InputField edName;

	[SerializeField]
	private TMP_InputField edDescription;

	[SerializeField]
	private UITextImageButton btDeleteBlueprint;

	[SerializeField]
	private UITextImageButton btEdit;

	[SerializeField]
	private UITextImageButton btAddToBar;

	[SerializeField]
	private UITextImageButton btAddToBar_Grey;

	[SerializeField]
	private UITextImageButton btRemoveFromBar;

	[SerializeField]
	private UITextImageButton btShare;

	[SerializeField]
	private UITextImageButton btSortName;

	[SerializeField]
	private UITextImageButton btSortBar;

	[SerializeField]
	private UITextImageButton btSortDate;

	[SerializeField]
	private UITextImageButton btSortCreator;

	[SerializeField]
	private UIButtonText btBack;

	[SerializeField]
	private UIButtonText btPlace;

	[SerializeField]
	private UIButtonText btSave;

	[SerializeField]
	private RawImage imScreenshot;

	[SerializeField]
	private UIBlueprintButton prefabBtBlueprint;

	[SerializeField]
	private GameObject pfDialogNoButtons;

	private List<UIBlueprintButton> blueprintButtons = new List<UIBlueprintButton>();

	private string currentSelectedCode;

	private Blueprint editBlueprint;

	private bool newCreation;

	private List<SortType> sortOrder;

	private List<string> blueprintCodes;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public override void Show(bool target)
	{
		if (target)
		{
			base.Show(target);
		}
		else
		{
			StartClose();
		}
	}

	public void Init()
	{
		GameManager.instance.SetStatus(GameStatus.MENU);
		UpdateTitleAndBackButton();
		sortOrder = new List<SortType>
		{
			SortType.Date,
			SortType.Creator,
			SortType.Bar
		};
		btSortName.SetButton(delegate
		{
			SortBlueprints(SortType.Name);
		});
		btSortName.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_SORT_NAME"));
		btSortBar.SetButton(delegate
		{
			SortBlueprints(SortType.Bar);
		});
		btSortBar.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_SORT_BAR"));
		btSortDate.SetButton(delegate
		{
			SortBlueprints(SortType.Date);
		});
		btSortDate.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_SORT_DATE"));
		if (Platform.current.hasWorkshop)
		{
			btSortCreator.SetButton(delegate
			{
				SortBlueprints(SortType.Creator);
			});
			btSortCreator.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_SORT_WORKSHOP"));
		}
		else
		{
			btSortCreator.Show(target: false);
		}
		RefreshBlueprints();
		SetSelected(null);
		Show(target: true);
	}

	private void UpdateTitleAndBackButton()
	{
		lbTitle.SetText((editBlueprint != null && newCreation) ? Loc.GetUI("BLUEPRINTS_NEW") : Loc.GetUI("BLUEPRINTS"));
		string text = ((editBlueprint == null) ? Loc.GetUI("GENERIC_BACK") : Loc.GetUI("GENERIC_CANCEL"));
		if (editBlueprint == null || newCreation)
		{
			btBack.Init(delegate
			{
				GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
			}, text);
			return;
		}
		btBack.Init(delegate
		{
			EndEditMode();
			RefreshBlueprints();
			SetSelected(currentSelectedCode);
		}, text);
	}

	private void RefreshBlueprints()
	{
		blueprintCodes = new List<string>();
		BlueprintManager.RefreshBlueprints();
		foreach (string item in BlueprintManager.EBlueprintCodes())
		{
			blueprintCodes.Add(item);
		}
		int count = blueprintCodes.Count;
		prefabBtBlueprint.SetObActive(active: false);
		if (blueprintButtons.Count < count)
		{
			int num = count - blueprintButtons.Count;
			for (int i = 0; i < num; i++)
			{
				UIBlueprintButton component = Object.Instantiate(prefabBtBlueprint, prefabBtBlueprint.transform.parent).GetComponent<UIBlueprintButton>();
				blueprintButtons.Add(component);
			}
		}
		foreach (UIBlueprintButton blueprintButton in blueprintButtons)
		{
			blueprintButton.SetObActive(active: false);
		}
		for (int j = 0; j < count; j++)
		{
			Blueprint blueprint = BlueprintManager.GetBlueprint(blueprintCodes[j]);
			blueprint.UpdateLocked();
			UIBlueprintButton uIBlueprintButton = blueprintButtons[j];
			uIBlueprintButton.Init(blueprint.name, delegate
			{
				SetSelected(blueprint.code);
			});
			uIBlueprintButton.blueprint = blueprint;
			uIBlueprintButton.SetTextColor(Color.white);
			uIBlueprintButton.SetObActive(active: true);
		}
		SortBlueprints();
		RefreshButtonIcons();
	}

	private IEnumerable<UITextImageButton> ESortButtons()
	{
		yield return btSortName;
		yield return btSortBar;
		yield return btSortDate;
		yield return btSortCreator;
	}

	public void SetEditMode(Blueprint blueprint, bool new_creation)
	{
		editBlueprint = blueprint;
		newCreation = new_creation;
		foreach (UITextImageButton item in ESortButtons())
		{
			item.Show(target: false);
		}
		foreach (UIBlueprintButton blueprintButton in blueprintButtons)
		{
			blueprintButton.SetInteractable(target: false);
		}
		btDeleteBlueprint.Show(target: false);
		btEdit.Show(target: false);
		btAddToBar.Show(target: false);
		btAddToBar_Grey.Show(target: false);
		btRemoveFromBar.Show(target: false);
		UpdateShareButton();
		btPlace.SetObActive(active: false);
		btSave.SetObActive(active: true);
		UpdateTitleAndBackButton();
		btSave.Init(delegate
		{
			string text = edName.text.Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = blueprint.name;
			}
			string text2 = edDescription.text.Trim();
			bool flag = blueprint.name != text || blueprint.description != text2;
			blueprint.name = text;
			blueprint.description = text2;
			if (flag || new_creation)
			{
				blueprint.SaveToFile();
			}
			if (new_creation)
			{
				try
				{
					byte[] array = blueprint.iconTexture.EncodeToPNG();
					File.WriteAllBytes(Files.BlueprintImage(blueprint), array);
					Texture2D texture2D = new Texture2D(2, 2);
					texture2D.LoadImage(array);
					blueprint.SetIcon(texture2D);
				}
				catch
				{
					Debug.LogError("Something went wrong saving blueprint image for '" + blueprint.code + "'");
				}
				BlueprintManager.AddBlueprint(blueprint, add_to_bar: true);
			}
			else if (Platform.current.hasWorkshop && blueprint.GetShareType() == BlueprintShareType.Shared && flag && Platform.current.UploadBlueprint(blueprint))
			{
				WaitUntilUploadDone();
			}
			EndEditMode();
			RefreshBlueprints();
			SetSelected(blueprint.code);
		});
		ShowDetails(blueprint);
	}

	private void EndEditMode()
	{
		foreach (UITextImageButton item in ESortButtons())
		{
			item.Show(target: true);
		}
		foreach (UIBlueprintButton blueprintButton in blueprintButtons)
		{
			blueprintButton.SetInteractable(target: true);
		}
		editBlueprint = null;
		newCreation = false;
		UpdateTitleAndBackButton();
	}

	private void SortBlueprints(SortType sort_type)
	{
		if (sortOrder[^1] == sort_type)
		{
			sortOrder[^1] = InvSort(sort_type);
		}
		else if (sortOrder[^1] == InvSort(sort_type))
		{
			sortOrder[^1] = sort_type;
		}
		else
		{
			sortOrder.Remove(sort_type);
			sortOrder.Add(sort_type);
		}
		SortBlueprints();
	}

	private SortType InvSort(SortType sort_type)
	{
		int num = (int)sort_type;
		if (num < 10)
		{
			return (SortType)(num + 10);
		}
		return (SortType)(num - 10);
	}

	private void SortBlueprints()
	{
		foreach (UIBlueprintButton blueprintButton in blueprintButtons)
		{
			blueprintButton.barSortValue = ((!BlueprintManager.GetInBar(blueprintButton.blueprint.code)) ? 1 : 0);
			int creatorSortValue = 0;
			switch (blueprintButton.blueprint.GetShareType())
			{
			case BlueprintShareType.Local:
				creatorSortValue = int.MaxValue;
				break;
			case BlueprintShareType.Shared:
				creatorSortValue = 2147483646;
				break;
			case BlueprintShareType.Subscribed:
				creatorSortValue = (int)(blueprintButton.blueprint.creatorId & 0xFFFFFFFFu);
				break;
			}
			blueprintButton.creatorSortValue = creatorSortValue;
		}
		using (List<SortType>.Enumerator enumerator2 = sortOrder.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				switch (enumerator2.Current)
				{
				case SortType.Name:
					blueprintButtons = blueprintButtons.OrderBy((UIBlueprintButton bb) => bb.blueprint.name).ToList();
					break;
				case SortType.Name_Desc:
					blueprintButtons = blueprintButtons.OrderByDescending((UIBlueprintButton bb) => bb.blueprint.name).ToList();
					break;
				case SortType.Bar:
					blueprintButtons = blueprintButtons.OrderBy((UIBlueprintButton bb) => bb.barSortValue).ToList();
					break;
				case SortType.Bar_Desc:
					blueprintButtons = blueprintButtons.OrderByDescending((UIBlueprintButton bb) => bb.barSortValue).ToList();
					break;
				case SortType.Date:
					blueprintButtons = blueprintButtons.OrderBy((UIBlueprintButton bb) => bb.blueprint.creationDate).ToList();
					break;
				case SortType.Date_Desc:
					blueprintButtons = blueprintButtons.OrderByDescending((UIBlueprintButton bb) => bb.blueprint.creationDate).ToList();
					break;
				case SortType.Creator:
					blueprintButtons = blueprintButtons.OrderBy((UIBlueprintButton bb) => bb.creatorSortValue).ToList();
					break;
				case SortType.Creator_Desc:
					blueprintButtons = blueprintButtons.OrderByDescending((UIBlueprintButton bb) => bb.creatorSortValue).ToList();
					break;
				}
			}
		}
		foreach (UIBlueprintButton blueprintButton2 in blueprintButtons)
		{
			blueprintButton2.transform.SetAsLastSibling();
		}
	}

	private void RefreshButtonIcons()
	{
		foreach (UIBlueprintButton blueprintButton in blueprintButtons)
		{
			blueprintButton.ResetOverlays();
			Blueprint blueprint = blueprintButton.blueprint;
			if (blueprint.locked)
			{
				blueprintButton.AddOverlay(OverlayTypes.LOCKED);
			}
			if (blueprint.code == currentSelectedCode)
			{
				blueprintButton.AddOverlay(OverlayTypes.SELECTED);
			}
			if (BlueprintManager.GetInBar(blueprint.code))
			{
				blueprintButton.AddOverlay(OverlayTypes.TRACKING);
			}
			switch (blueprint.GetShareType())
			{
			case BlueprintShareType.Shared:
				blueprintButton.AddOverlay(OverlayTypes.SHARED);
				break;
			case BlueprintShareType.Subscribed:
				blueprintButton.AddOverlay(OverlayTypes.SUBSCRIBED);
				break;
			}
		}
	}

	private void SetSelected(string code)
	{
		currentSelectedCode = code;
		RefreshButtonIcons();
		btSave.SetObActive(active: false);
		if (currentSelectedCode == null)
		{
			ShowDetails(null);
			btDeleteBlueprint.Show(target: false);
			btAddToBar.SetObActive(active: false);
			btAddToBar_Grey.SetObActive(active: false);
			btRemoveFromBar.SetObActive(active: false);
			btEdit.SetObActive(active: false);
			UpdateShareButton();
			btPlace.SetObActive(active: false);
			return;
		}
		Blueprint blueprint = BlueprintManager.GetBlueprint(currentSelectedCode, need_complete: true);
		if (blueprint == null)
		{
			Debug.Log("Couldn't get complete blueprint " + currentSelectedCode);
			SetSelected(null);
			return;
		}
		blueprint.UpdateLocked(fill_missing_components: true);
		ShowDetails(blueprint);
		btDeleteBlueprint.Show(target: true);
		btEdit.SetObActive(blueprint.GetShareType() != BlueprintShareType.Subscribed);
		UpdateShareButton();
		btPlace.SetObActive(!blueprint.locked);
		BlueprintShareType share_type = blueprint.GetShareType();
		if (share_type == BlueprintShareType.Subscribed)
		{
			btDeleteBlueprint.SetButton(delegate
			{
				BlueprintManager.SetInBar(currentSelectedCode, in_bar: false);
				Platform.current.UnsubscribeBlueprint(blueprint);
				SetSelected(null);
				RefreshBlueprints();
			});
			btDeleteBlueprint.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_UNSUBSCRIBE"));
		}
		else
		{
			btDeleteBlueprint.SetButton(delegate
			{
				UIDialogBase dialog = UIBase.Spawn<UIDialogBase>();
				string text = Loc.GetUI("BLUEPRINTS_DELETE_AYS", blueprint.name);
				if (share_type == BlueprintShareType.Shared)
				{
					text = text + " " + Loc.GetUI("BLUEPRINTS_UNSHARE_AYS");
				}
				dialog.SetText(text);
				dialog.SetAction(DialogResult.YES, delegate
				{
					dialog.StartClose();
					BlueprintManager.SetInBar(currentSelectedCode, in_bar: false);
					string path = Files.BlueprintPath(blueprint);
					if (Directory.Exists(path))
					{
						Directory.Delete(path, recursive: true);
					}
					if (share_type == BlueprintShareType.Shared)
					{
						Platform.current.RemoveUploadedBlueprint(blueprint);
					}
					SetSelected(null);
					RefreshBlueprints();
				});
				dialog.SetAction(DialogResult.NO, dialog.StartClose);
			});
			btDeleteBlueprint.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_DELETE"));
		}
		btEdit.SetButton(delegate
		{
			SetEditMode(blueprint, new_creation: false);
		});
		btEdit.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_EDIT"));
		btShare.SetButton(delegate
		{
			UIDialogBase dialog = UIBase.Spawn<UIDialogBase>();
			dialog.SetText(Loc.GetUI("BLUEPRINTS_SHARE_AYS", blueprint.name));
			dialog.SetAction(DialogResult.YES, delegate
			{
				dialog.StartClose();
				if (Platform.current.UploadBlueprint(blueprint))
				{
					WaitUntilUploadDone();
				}
			});
			dialog.SetAction(DialogResult.NO, dialog.StartClose);
		});
		btShare.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_SHARE"));
		btPlace.Init(delegate
		{
			GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
			Gameplay.instance.SelectBlueprint(blueprint);
		});
		bool flag = BlueprintManager.BarFull();
		bool inBar = BlueprintManager.GetInBar(blueprint.code);
		bool flag2 = !inBar && !flag;
		bool flag3 = !inBar && flag;
		bool flag4 = inBar;
		btAddToBar.SetObActive(flag2);
		if (flag2)
		{
			btAddToBar.SetButton(delegate
			{
				BlueprintManager.SetInBar(currentSelectedCode, in_bar: true);
				SetSelected(currentSelectedCode);
			});
			btAddToBar.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_BAR_ADD"));
		}
		btAddToBar_Grey.SetObActive(flag3);
		if (flag3)
		{
			btAddToBar_Grey.SetInteractable(target: false);
			btAddToBar_Grey.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_BAR_FULL"));
		}
		btRemoveFromBar.SetObActive(flag4);
		if (flag4)
		{
			btRemoveFromBar.SetButton(delegate
			{
				BlueprintManager.SetInBar(currentSelectedCode, in_bar: false);
				SetSelected(currentSelectedCode);
			});
			btRemoveFromBar.SetHoverText(Loc.GetUI("BLUEPRINTS_HOVER_BUTTON_BAR_REMOVE"));
		}
	}

	private void UpdateShareButton()
	{
		if (editBlueprint != null || currentSelectedCode == null || !Platform.current.hasWorkshop || BlueprintManager.GetBlueprint(currentSelectedCode).GetShareType() != BlueprintShareType.Local)
		{
			btShare.Show(target: false);
		}
		else
		{
			btShare.Show(target: true);
		}
	}

	private void ShowDetails(Blueprint blueprint)
	{
		bool flag = blueprint != null;
		bool flag2 = editBlueprint != null;
		rtScreenshot.SetObActive(flag);
		lbBlueprintName.SetObActive(flag && !flag2);
		edName.SetObActive(flag && flag2);
		lbDetails.SetObActive(flag && !flag2);
		edDescription.SetObActive(flag && flag2);
		if (!flag)
		{
			return;
		}
		imScreenshot.texture = blueprint.iconTexture;
		if (flag2)
		{
			edName.SetTextWithoutNotify(blueprint.name);
			edDescription.SetTextWithoutNotify(blueprint.description);
			return;
		}
		lbBlueprintName.text = blueprint.name;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Loc.GetUI("BLUEPRINTS_CREATIONDATE", blueprint.creationDate.ToString("g", Loc.culture)));
		if (blueprint.GetShareType() == BlueprintShareType.Subscribed)
		{
			stringBuilder.AppendLine(Loc.GetUI("BLUEPRINTS_CREATOR", Platform.current.GetUserName(blueprint.creatorId)));
		}
		stringBuilder.AppendLine(Loc.GetUI("BLUEPRINTS_CODE", blueprint.code));
		stringBuilder.AppendLine("<size=8> </size>");
		stringBuilder.AppendLine(blueprint.description);
		if (blueprint.locked)
		{
			stringBuilder.AppendLine("<size=8> </size>");
			stringBuilder.AppendLine("<color=#ff8080ff>" + Loc.GetUI("BLUEPRINTS_MISSING", blueprint.missingComponents) + "</color>");
		}
		lbDetails.text = stringBuilder.ToString();
	}

	private void WaitUntilUploadDone()
	{
		StartKoroutine(KWaitUntilUploadDone());
	}

	private IEnumerator KWaitUntilUploadDone()
	{
		GameManager.instance.SetStatus(GameStatus.UNPAUSABLE);
		UIDialogBase dialog = UIBase.Spawn<UIDialogBase>(pfDialogNoButtons);
		KoroutineId kid = SetFinalizer(delegate
		{
			GameManager.instance.SetStatus(GameStatus.MENU);
			dialog.StartClose();
			UpdateShareButton();
			RefreshButtonIcons();
		});
		try
		{
			int n_dots = 0;
			float timeout = Time.time + 30f;
			string uploading = Loc.GetUI("BLUEPRINTS_UPLOADING");
			while (Platform.current.IsUploadActive())
			{
				n_dots++;
				if (n_dots > 5)
				{
					n_dots = 1;
				}
				string text = "";
				for (int num = 0; num < n_dots; num++)
				{
					text += ".";
				}
				dialog.SetText(uploading + text);
				yield return new WaitForSeconds(0.2f);
				if (Time.time > timeout)
				{
					dialog.SetText("timeout");
					yield return new WaitForSeconds(0.8f);
					break;
				}
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void Update()
	{
		if (InputManager.blueprints && GameManager.instance.GetStatus() != GameStatus.UNPAUSABLE)
		{
			GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
		}
	}

	protected override void Close()
	{
		if (Gameplay.instance.currentGroup == BuildingGroup.BLUEPRINTS)
		{
			Gameplay.instance.SetTaskbar(BuildingGroup.BLUEPRINTS);
		}
		base.Close();
	}
}
