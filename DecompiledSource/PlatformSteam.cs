using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;

public class PlatformSteam : PlatformBase
{
	private class WorkshopItemInfo
	{
		public PublishedFileId_t publishId;

		public string title;

		public string description;

		public string localPath;

		public DateTime dateCreated;

		public CSteamID creatorUserId;

		public override string ToString()
		{
			return title + ", created " + dateCreated.ToShortDateString() + " " + dateCreated.ToShortTimeString() + " publishid: " + publishId.m_PublishedFileId + (string.IsNullOrEmpty(localPath) ? "" : (", local path " + localPath));
		}
	}

	private enum WorkshopFilter
	{
		NONE,
		ALL,
		MY_UPLOADS,
		MY_SUBSCRIPTIONS
	}

	private enum BusyState
	{
		NONE,
		BUSY,
		FAILED,
		DONE
	}

	public struct UserDetails
	{
		public string name;

		public UserDetails(string _name)
		{
			name = _name;
		}
	}

	private const uint APPID = 2750000u;

	private const uint APPID_DEMO = 2792340u;

	private const uint APPID_PROLOGUE = 3082030u;

	private const uint APPID_PLAYTEST = 3359150u;

	private AppId_t appId;

	private CSteamID steamUserId;

	private string steamUserName;

	private AccountID_t steamAccountId;

	private static Action<PlatformGlobalStats> callbackGetGlobalStats;

	private CallResult<GlobalStatsReceived_t> onGlobalStatsReceivedCallResult;

	private Blueprint currentBlueprint;

	private bool currentBlueprintNew;

	private List<WorkshopItemInfo> subscribedItems;

	private HashSet<ulong> uploadedItemIds;

	private Dictionary<CSteamID, UserDetails> userDetails = new Dictionary<CSteamID, UserDetails>();

	private BusyState busyWorkshopQuery;

	private uint lastQueryResultCount;

	private ulong gotPersona;

	private List<WorkshopItemInfo> requestedItems = new List<WorkshopItemInfo>();

	private Callback<PersonaStateChange_t> personaStateChangedCallback;

	private Callback<DeleteItemResult_t> itemDeletedCallback;

	private CallResult<CreateItemResult_t> createItemCallResult;

	private CallResult<SubmitItemUpdateResult_t> submitItemUpdateCallResult;

	private CallResult<SteamUGCQueryCompleted_t> ugcQueryCompletedCallResult;

	public override IEnumerator KInit(Action<string> callback, Action<float> func_progress)
	{
		string fatal_error = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			callback(fatal_error);
		});
		try
		{
			func_progress(0f);
			hasWorkshop = true;
			if (!Packsize.Test())
			{
				fatal_error = "[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.";
				yield break;
			}
			if (!DllCheck.Test())
			{
				fatal_error = "[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.";
				yield break;
			}
			try
			{
				if (DebugSettings.standard.playtest)
				{
					appId = (AppId_t)3359150u;
				}
				else if (DebugSettings.standard.prologue)
				{
					appId = (AppId_t)3082030u;
				}
				else if (DebugSettings.standard.demo)
				{
					appId = (AppId_t)2792340u;
				}
				else
				{
					appId = (AppId_t)2750000u;
				}
				if (SteamAPI.RestartAppIfNecessary(appId))
				{
					Application.Quit();
					fatal_error = "SteamAPI.RestartAppIfNecessary -> Restarting...";
					yield break;
				}
			}
			catch (DllNotFoundException ex)
			{
				fatal_error = "Could not load Steam library\n" + ex;
				yield break;
			}
			if (!SteamAPI.Init())
			{
				fatal_error = "Steam initialization failed";
				yield break;
			}
			steamUserId = SteamUser.GetSteamID();
			steamUserName = SteamFriends.GetPersonaName();
			steamAccountId = steamUserId.GetAccountID();
			onGlobalStatsReceivedCallResult = CallResult<GlobalStatsReceived_t>.Create(OnGlobalStatsReceived);
			SteamUserStats.RequestCurrentStats();
			func_progress(0.5f);
			inited = true;
			yield return StartKoroutine(KGatherWorkshopItems());
			func_progress(1f);
			Debug.Log("Steam initialized");
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	protected override void Process(float dt)
	{
		SteamAPI.RunCallbacks();
		base.Process(dt);
	}

	public override void Outit()
	{
		SteamAPI.Shutdown();
	}

	public override string GetPlayerFileDir()
	{
		if (DebugSettings.standard.demo && !DebugSettings.standard.prologue)
		{
			return Directory.GetParent(Application.dataPath).FullName;
		}
		string text = Path.Combine(Application.persistentDataPath, steamUserId.m_SteamID.ToString());
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public override Language GetDefaultLanguage()
	{
		Language language;
		switch (SteamApps.GetCurrentGameLanguage().ToLowerInvariant())
		{
		case "english":
			language = Language.ENGLISH;
			break;
		case "french":
			language = Language.FRENCH;
			break;
		case "german":
			language = Language.GERMAN;
			break;
		case "japanese":
			language = Language.JAPANESE;
			break;
		case "chinese":
		case "schinese":
		case "tchinese":
			language = Language.CHINESE_SIMPLIFIED;
			break;
		case "russian":
			language = Language.RUSSIAN;
			break;
		case "dutch":
			language = Language.DUTCH;
			break;
		case "koreana":
		case "korean":
			language = Language.KOREAN;
			break;
		case "polish":
			language = Language.POLISH;
			break;
		default:
			language = base.GetDefaultLanguage();
			break;
		}
		Language language2 = language;
		if (!Loc.AllowedLanguage(language2))
		{
			language2 = base.GetDefaultLanguage();
		}
		return language2;
	}

	public override string GetUserName()
	{
		return steamUserName;
	}

	public override ulong GetUserId()
	{
		return steamUserId.m_SteamID;
	}

	protected override void UpdateGynesFlownReal(int v)
	{
		SteamUserStats.SetStat("gynes_flown", v);
		SteamUserStats.StoreStats();
	}

	public override bool GetGlobalStats(Action<PlatformGlobalStats> callback_result)
	{
		SteamAPICall_t hAPICall = SteamUserStats.RequestGlobalStats(14);
		onGlobalStatsReceivedCallResult.Set(hAPICall);
		if (callbackGetGlobalStats != null)
		{
			Debug.LogWarning("PlatformSteam: Requesting global stats while previous request wasn't finished");
		}
		callbackGetGlobalStats = callback_result;
		return true;
	}

	private void OnGlobalStatsReceived(GlobalStatsReceived_t pCallback, bool failure)
	{
		SteamUserStats.GetGlobalStat("gynes_flown", out long pData);
		PlatformGlobalStats obj = new PlatformGlobalStats
		{
			valid = !failure
		};
		if (!failure)
		{
			obj.totalGynesFlown = (int)pData;
		}
		callbackGetGlobalStats(obj);
		callbackGetGlobalStats = null;
	}

	protected override void GainAchievementReal(Achievement achievement)
	{
		string text = "ACH_" + achievement.ToString().ToUpperInvariant();
		SteamUserStats.GetAchievement(text, out var pbAchieved);
		if (!pbAchieved)
		{
			if (!SteamUserStats.SetAchievement(text))
			{
				Debug.LogWarning("SetAchievement(" + text + ") returned false");
			}
			else
			{
				Debug.Log($"Achieved {achievement}");
			}
			SteamUserStats.StoreStats();
		}
	}

	private IEnumerator KGatherWorkshopItems()
	{
		KoroutineId kid = SetFinalizer(delegate
		{
		});
		try
		{
			personaStateChangedCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChanged);
			itemDeletedCallback = Callback<DeleteItemResult_t>.Create(OnItemDeleted);
			createItemCallResult = CallResult<CreateItemResult_t>.Create(OnCreatedItem);
			submitItemUpdateCallResult = CallResult<SubmitItemUpdateResult_t>.Create(OnItemUpdateSubmitted);
			ugcQueryCompletedCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);
			subscribedItems = new List<WorkshopItemInfo>();
			yield return StartKoroutine(KRequestWorkshopItems(WorkshopFilter.MY_SUBSCRIPTIONS));
			foreach (WorkshopItemInfo requestedItem in requestedItems)
			{
				if (!string.IsNullOrEmpty(requestedItem.localPath))
				{
					subscribedItems.Add(requestedItem);
				}
				CSteamID creator_id = requestedItem.creatorUserId;
				if (!userDetails.TryGetValue(creator_id, out var value))
				{
					yield return StartKoroutine(KGetUserData(requestedItem.creatorUserId));
					value = userDetails[creator_id];
				}
			}
			uploadedItemIds = new HashSet<ulong>();
			yield return StartKoroutine(KRequestWorkshopItems(WorkshopFilter.MY_UPLOADS));
			foreach (WorkshopItemInfo requestedItem2 in requestedItems)
			{
				uploadedItemIds.Add(requestedItem2.publishId.m_PublishedFileId);
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private IEnumerator KRequestWorkshopItems(WorkshopFilter filter)
	{
		KoroutineId kid = SetFinalizer(delegate
		{
			if (busyWorkshopQuery == BusyState.FAILED)
			{
				requestedItems.Clear();
			}
		});
		try
		{
			if (busyWorkshopQuery == BusyState.BUSY)
			{
				Debug.LogError("KGatherWorkshopItems: querystatus is " + busyWorkshopQuery);
				yield break;
			}
			requestedItems.Clear();
			uint page = 1u;
			bool flag = false;
			while (!flag)
			{
				busyWorkshopQuery = BusyState.BUSY;
				UGCQueryHandle_t handle;
				switch (filter)
				{
				case WorkshopFilter.NONE:
				case WorkshopFilter.ALL:
					handle = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByPublicationDate, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse, appId, appId, page);
					break;
				case WorkshopFilter.MY_UPLOADS:
					handle = SteamUGC.CreateQueryUserUGCRequest(steamAccountId, EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderDesc, appId, appId, page);
					break;
				case WorkshopFilter.MY_SUBSCRIPTIONS:
					handle = SteamUGC.CreateQueryUserUGCRequest(steamAccountId, EUserUGCList.k_EUserUGCList_Subscribed, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderDesc, appId, appId, page);
					break;
				default:
					Debug.LogError("KRequestWorkshopItems: don't know " + filter);
					busyWorkshopQuery = BusyState.FAILED;
					yield break;
				}
				SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(handle);
				ugcQueryCompletedCallResult.Set(hAPICall);
				while (busyWorkshopQuery == BusyState.BUSY)
				{
					yield return null;
				}
				if (busyWorkshopQuery == BusyState.FAILED)
				{
					yield break;
				}
				page++;
				flag = lastQueryResultCount < 50;
			}
			busyWorkshopQuery = BusyState.DONE;
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t info, bool failed)
	{
		UGCQueryHandle_t handle = info.m_handle;
		if (info.m_eResult != EResult.k_EResultOK || failed)
		{
			Debug.LogError("Steam OnUGCQueryCompleted: " + info.m_eResult.ToString() + ", failed " + failed);
			if (handle != UGCQueryHandle_t.Invalid)
			{
				SteamUGC.ReleaseQueryUGCRequest(handle);
			}
			busyWorkshopQuery = BusyState.FAILED;
			return;
		}
		if (handle == UGCQueryHandle_t.Invalid)
		{
			Debug.LogError("Steam OnUGCQueryCompleted: handle is invalid");
			busyWorkshopQuery = BusyState.FAILED;
			return;
		}
		uint num = (lastQueryResultCount = info.m_unTotalMatchingResults);
		SteamUGCDetails_t pDetails;
		for (uint num2 = 0u; num2 < num && SteamUGC.GetQueryUGCResult(handle, num2, out pDetails); num2++)
		{
			WorkshopItemInfo workshopItemInfo = new WorkshopItemInfo
			{
				publishId = pDetails.m_nPublishedFileId,
				title = pDetails.m_rgchTitle,
				description = pDetails.m_rgchDescription,
				dateCreated = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(pDetails.m_rtimeCreated),
				creatorUserId = new CSteamID(pDetails.m_ulSteamIDOwner)
			};
			if ((SteamUGC.GetItemState(pDetails.m_nPublishedFileId) & 4) != 0)
			{
				SteamUGC.GetItemInstallInfo(pDetails.m_nPublishedFileId, out var _, out workshopItemInfo.localPath, 1000u, out var _);
			}
			requestedItems.Add(workshopItemInfo);
		}
		SteamUGC.ReleaseQueryUGCRequest(handle);
		busyWorkshopQuery = BusyState.DONE;
	}

	private IEnumerator KGetUserData(CSteamID steam_id)
	{
		KoroutineId kid = SetFinalizer(delegate
		{
		});
		try
		{
			gotPersona = 0uL;
			if (SteamFriends.RequestUserInformation(steam_id, bRequireNameOnly: false))
			{
				while (gotPersona == 0L)
				{
					yield return null;
				}
			}
			UserDetails value = default(UserDetails);
			value.name = SteamFriends.GetFriendPersonaName(steam_id);
			userDetails[steam_id] = value;
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void OnPersonaStateChanged(PersonaStateChange_t info)
	{
		gotPersona = info.m_ulSteamID;
	}

	public override IEnumerable<string> ESubscribedBlueprintPaths()
	{
		foreach (WorkshopItemInfo subscribedItem in subscribedItems)
		{
			yield return subscribedItem.localPath;
		}
	}

	public override bool UploadBlueprint(Blueprint blueprint)
	{
		if (currentBlueprint != null)
		{
			Debug.LogError("Steam UploadBlueprint: already busy with upload");
			return false;
		}
		currentBlueprint = blueprint;
		switch (blueprint.GetShareType())
		{
		case BlueprintShareType.Local:
			Debug.Log("Sending blueprint '" + blueprint.name + "' to Steam Workshop");
			currentBlueprintNew = true;
			createItemCallResult.Set(SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeFirst));
			break;
		case BlueprintShareType.Shared:
		{
			currentBlueprintNew = false;
			UGCUpdateHandle_t uGCUpdateHandle_t = SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(blueprint.publishId));
			SetTitleAndDesc(uGCUpdateHandle_t);
			submitItemUpdateCallResult.Set(SteamUGC.SubmitItemUpdate(uGCUpdateHandle_t, null));
			break;
		}
		case BlueprintShareType.Subscribed:
			Debug.LogError("Can't upload changes to someone elses blueprint");
			break;
		}
		return true;
	}

	private void SetTitleAndDesc(UGCUpdateHandle_t h_update)
	{
		string text = currentBlueprint.name;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "Blueprint";
		}
		SteamUGC.SetItemTitle(h_update, text);
		string text2 = currentBlueprint.description;
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "-";
		}
		SteamUGC.SetItemDescription(h_update, text2);
	}

	public override bool IsUploadActive()
	{
		return currentBlueprint != null;
	}

	public override bool BlueprintIsNotUploaded(Blueprint blueprint)
	{
		return !uploadedItemIds.Contains(blueprint.publishId);
	}

	public override void RemoveUploadedBlueprint(Blueprint blueprint)
	{
		ulong publishId = blueprint.publishId;
		if (publishId != 0L)
		{
			SteamUGC.DeleteItem(new PublishedFileId_t(publishId));
			uploadedItemIds.Remove(publishId);
		}
	}

	public override void UnsubscribeBlueprint(Blueprint blueprint)
	{
		ulong publishId = blueprint.publishId;
		if (publishId == 0L)
		{
			return;
		}
		PublishedFileId_t publishedFileId_t = new PublishedFileId_t(publishId);
		SteamUGC.UnsubscribeItem(publishedFileId_t);
		for (int num = subscribedItems.Count - 1; num >= 0; num--)
		{
			WorkshopItemInfo workshopItemInfo = subscribedItems[num];
			if (workshopItemInfo != null && workshopItemInfo.publishId == publishedFileId_t)
			{
				subscribedItems.RemoveAt(num);
			}
		}
	}

	private void EndBlueprintUpload()
	{
		currentBlueprint = null;
	}

	private void OnCreatedItem(CreateItemResult_t info, bool failed)
	{
		if (currentBlueprint == null)
		{
			Debug.LogError("Steam OnCreatedItem: item is null");
			return;
		}
		if (info.m_eResult != EResult.k_EResultOK || failed)
		{
			Debug.LogError("Steam OnCreatedItem: " + info.m_eResult.ToString() + ", failed " + failed);
			EndBlueprintUpload();
			return;
		}
		PublishedFileId_t nPublishedFileId = info.m_nPublishedFileId;
		currentBlueprint.publishId = nPublishedFileId.m_PublishedFileId;
		currentBlueprint.creatorId = steamUserId.m_SteamID;
		currentBlueprint.SaveToFile();
		string pszContentFolder = Files.BlueprintPath(currentBlueprint).Replace('/', Path.DirectorySeparatorChar);
		string pszPreviewFile = Files.BlueprintImage(currentBlueprint).Replace('/', Path.DirectorySeparatorChar);
		if (info.m_bUserNeedsToAcceptWorkshopLegalAgreement)
		{
			SteamFriends.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{nPublishedFileId}");
		}
		UGCUpdateHandle_t uGCUpdateHandle_t = SteamUGC.StartItemUpdate(appId, nPublishedFileId);
		SetTitleAndDesc(uGCUpdateHandle_t);
		SteamUGC.SetItemVisibility(uGCUpdateHandle_t, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
		SteamUGC.SetItemContent(uGCUpdateHandle_t, pszContentFolder);
		SteamUGC.SetItemPreview(uGCUpdateHandle_t, pszPreviewFile);
		submitItemUpdateCallResult.Set(SteamUGC.SubmitItemUpdate(uGCUpdateHandle_t, null));
	}

	private void OnItemUpdateSubmitted(SubmitItemUpdateResult_t info, bool failed)
	{
		if (currentBlueprint == null)
		{
			Debug.LogError("Steam OnItemUpdateSubmitted: item is null");
			return;
		}
		if (info.m_eResult != EResult.k_EResultOK || failed)
		{
			Debug.LogError("Steam OnItemUpdateSubmitted: " + info.m_eResult.ToString() + ", failed " + failed);
			if (currentBlueprintNew)
			{
				currentBlueprint.publishId = 0uL;
				currentBlueprint.uploadDate = default(DateTime);
				currentBlueprint.creatorId = 0uL;
				currentBlueprint.SaveToFile();
			}
		}
		else
		{
			Debug.Log("Blueprint '" + currentBlueprint.name + "' (code " + currentBlueprint.code + ") uploaded to workshop");
			currentBlueprint.uploadDate = DateTime.Now;
			uploadedItemIds.Add(info.m_nPublishedFileId.m_PublishedFileId);
		}
		EndBlueprintUpload();
	}

	private IEnumerator CLogWorkshopItems(string header, WorkshopFilter filter)
	{
		yield return StartKoroutine(KRequestWorkshopItems(filter));
		string text = header + ": " + requestedItems.Count + "\n";
		foreach (WorkshopItemInfo requestedItem in requestedItems)
		{
			text = text + " - " + requestedItem.ToString() + "\n";
		}
		Debug.Log(text);
	}

	private IEnumerator KDeleteWorkshopItems(WorkshopFilter filter)
	{
		KoroutineId kid = SetFinalizer(delegate
		{
		});
		try
		{
			yield return StartKoroutine(KRequestWorkshopItems(filter));
			foreach (WorkshopItemInfo requestedItem in requestedItems)
			{
				Debug.Log("DELETING workshop item " + requestedItem.ToString());
				SteamUGC.DeleteItem(requestedItem.publishId);
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void OnItemDeleted(DeleteItemResult_t info)
	{
		PublishedFileId_t nPublishedFileId = info.m_nPublishedFileId;
		Debug.Log("Item deletion " + nPublishedFileId.ToString() + ": " + info.m_eResult);
	}

	public override string GetUserName(ulong id)
	{
		if (!userDetails.TryGetValue(new CSteamID(id), out var value))
		{
			return "?";
		}
		return value.name;
	}
}
