using System.Collections;
using System.Collections.Generic;
using DTT.Utils.Extensions;
using UnityEngine;

public class Unlocker : Building
{
	[Header("Unlocker")]
	public UnlockerType unlockerType;

	private UnlockRecipeData currentUnlock;

	private Coroutine cReveal;

	protected bool isRevealing;

	private List<Ant> antsInside = new List<Ant>();

	private List<int> antsInside_links = new List<int>();

	private bool doUnlock;

	private List<IslandUnlocks> allUnlocks;

	public override void Write(Save save)
	{
		base.Write(save);
		if (currentUnlock == null)
		{
			save.Write("");
		}
		else
		{
			save.Write(currentUnlock.code);
		}
		save.Write(antsInside.Count);
		foreach (Ant item in antsInside)
		{
			save.Write(item.linkId);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		string text = save.ReadString();
		if (!text.IsNullOrEmpty())
		{
			SetUnlock(text);
		}
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			antsInside_links.Add(save.ReadInt());
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		antsInside.Clear();
		foreach (int antsInside_link in antsInside_links)
		{
			Ant ant = GameManager.instance.FindLink<Ant>(antsInside_link);
			if (ant != null)
			{
				antsInside.Add(ant);
			}
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (during_load)
		{
			return;
		}
		UnlockerType unlockerType = this.unlockerType;
		if (unlockerType != UnlockerType.IslandReveal)
		{
			_ = 2;
			return;
		}
		using IEnumerator<string> enumerator = EAvailableBiomeReveals().GetEnumerator();
		if (enumerator.MoveNext())
		{
			string current = enumerator.Current;
			SetUnlock(current);
		}
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (doUnlock)
		{
			doUnlock = false;
			DoUnlock();
		}
	}

	public void PickUnlock(int tier)
	{
		List<string> list = new List<string>();
		foreach (Unlocker item in GameManager.instance.EUnlockers())
		{
			list.Add(item.GetUnlockCode());
		}
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		foreach (KeyValuePair<int, List<string>> dicUnlockTier in TechTree.dicUnlockTiers)
		{
			if (list2.Count > 0 || list3.Count > 0)
			{
				break;
			}
			if (dicUnlockTier.Key > tier)
			{
				continue;
			}
			foreach (string item2 in dicUnlockTier.Value)
			{
				if (!list.Contains(item2))
				{
					UnlockRecipeData unlockRecipeData = UnlockRecipeData.Get(item2);
					bool num = unlockRecipeData.reqUnlock.IsNullOrEmpty() || list.Contains(unlockRecipeData.reqUnlock);
					bool flag = unlockRecipeData.reqBuilding.IsNullOrEmpty() || Progress.HasUnlockedBuilding(unlockRecipeData.reqBuilding);
					if (num && flag)
					{
						list2.Add(item2);
					}
					else
					{
						list3.Add(item2);
					}
				}
			}
		}
		if (list2.Count > 0)
		{
			string unlock = list2[Random.Range(0, list2.Count)];
			SetUnlock(unlock);
		}
		else if (list3.Count > 0)
		{
			string unlock2 = list3[Random.Range(0, list3.Count)];
			SetUnlock(unlock2);
		}
		else
		{
			Delete();
		}
	}

	public void SetUnlock(string _unlock)
	{
		currentUnlock = UnlockRecipeData.Get(_unlock);
	}

	public UnlockRecipeData GetCurrentUnlock()
	{
		return currentUnlock;
	}

	public string GetUnlockCode()
	{
		if (currentUnlock == null)
		{
			return "";
		}
		return currentUnlock.code;
	}

	public IEnumerable<string> EAvailableBiomeReveals()
	{
		switch (unlockerType)
		{
		case UnlockerType.IslandReveal:
			foreach (IslandUnlocks item in AllIslandUnlocks())
			{
				if (!Progress.HasUnlocked(item.generalUnlock) && item.generalUnlock != GeneralUnlocks.RADAR_DESERT && !DebugSettings.standard.UnlockEverything())
				{
					continue;
				}
				foreach (string unlockRecipe in item.unlockRecipes)
				{
					if (!UnlockRecipeData.Get(unlockRecipe).IsCompleted())
					{
						yield return unlockRecipe;
						break;
					}
				}
			}
			break;
		case UnlockerType.Unlocker:
			Debug.LogError("Shoudln't call GetAvailableBiomeReveals on Unlocker type Unlocker");
			break;
		}
	}

	public int GetAvailableBiomeRevealsCount()
	{
		int num = 0;
		foreach (string item in EAvailableBiomeReveals())
		{
			_ = item;
			num++;
		}
		return num;
	}

	private void DoUnlock()
	{
		if (currentUnlock == null || currentUnlock.IsCompleted())
		{
			return;
		}
		if (!DebugSettings.standard.freeUnlocks)
		{
			foreach (PickupCost item in currentUnlock.costsPickup)
			{
				if (GetCollectedAmount(item.type, BuildingStatus.COMPLETED, include_incoming: true) < item.intValue)
				{
					Debug.LogError("Tried to unlock " + currentUnlock.code + " but not enough " + item.type);
					return;
				}
			}
			foreach (PickupCost item2 in currentUnlock.costsPickup)
			{
				RemovePickup(item2.type, item2.intValue, BuildingStatus.COMPLETED);
			}
			foreach (Ant item3 in new List<Ant>(antsInside))
			{
				History.RegisterAntEnd(item3, repurposed: false);
				GameManager.instance.GetLinkedGates(item3);
				item3.Delete();
			}
			antsInside.Clear();
		}
		currentUnlock.Unlock(during_load: false);
		switch (unlockerType)
		{
		case UnlockerType.IslandReveal:
			if (currentUnlock.unlockIsland != "")
			{
				cReveal = StartCoroutine(CRevealIslandDelay(currentUnlock.unlockIsland));
				Debug.Log("Revealing island");
			}
			if (currentUnlock.nextUnlock != "")
			{
				SetUnlock(currentUnlock.nextUnlock);
			}
			else
			{
				if (currentUnlock.repeatable)
				{
					break;
				}
				using IEnumerator<string> enumerator3 = EAvailableBiomeReveals().GetEnumerator();
				if (enumerator3.MoveNext())
				{
					string current4 = enumerator3.Current;
					SetUnlock(current4);
				}
				break;
			}
			break;
		case UnlockerType.Unlocker:
			if (GameManager.instance.GetStatus() == GameStatus.PAUSED)
			{
				GameManager.instance.SetStatus(GameStatus.RUNNING);
			}
			cReveal = StartCoroutine(CRevealUnlockDelay());
			break;
		}
	}

	private IEnumerator CRevealIslandDelay(string biome)
	{
		isRevealing = true;
		if (anim != null)
		{
			anim.SetBool("DoAction", value: true);
			yield return new WaitForSeconds(3f);
		}
		GameManager.instance.AddBiome(biome);
		isRevealing = false;
	}

	private IEnumerator CRevealUnlockDelay()
	{
		if (anim != null)
		{
			yield return new WaitForSeconds(0.5f);
			anim.SetTrigger("Do Unlock");
			yield return new WaitForSeconds(1.5f);
		}
		else
		{
			yield return new WaitForSeconds(0.5f);
		}
		UIDialogNewBuilding uIDialogNewBuilding = UIBase.Spawn<UIDialogNewBuilding>();
		uIDialogNewBuilding.SetText(Loc.GetUI("UNLOCKER_STORY"));
		uIDialogNewBuilding.SetAction(DialogResult.OK, uIDialogNewBuilding.StartClose);
		if (currentUnlock.unlockBuildings.Count > 0)
		{
			BuildingData buildingData = BuildingData.Get(currentUnlock.unlockBuildings[0]);
			uIDialogNewBuilding.SetDialogUnlock(buildingData.GetTitle(), buildingData.GetDescription(), buildingData.GetIcon());
		}
		else if (currentUnlock.unlockRecipes.Count > 0)
		{
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(currentUnlock.unlockRecipes[0]);
			if (factoryRecipeData.productAnts.Count > 0)
			{
				AntCasteData antCasteData = AntCasteData.Get(factoryRecipeData.productAnts[0].type);
				uIDialogNewBuilding.SetDialogUnlock(antCasteData.GetTitle(), antCasteData.GetDescription(), antCasteData.GetIcon());
			}
			else if (factoryRecipeData.productPickups.Count > 0)
			{
				PickupData pickupData = PickupData.Get(factoryRecipeData.productPickups[0].type);
				uIDialogNewBuilding.SetDialogUnlock(pickupData.GetTitle(), pickupData.GetDescription(), pickupData.GetIcon());
			}
		}
	}

	public override bool CanRelocate()
	{
		if (unlockerType == UnlockerType.Unlocker && currentUnlock != null && !currentUnlock.IsCompleted())
		{
			return false;
		}
		return base.CanRelocate();
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (currentUnlock == null || currentUnlock.IsCompleted())
		{
			return false;
		}
		int num = currentUnlock.costsPickup.FindIndex((PickupCost c) => c.type == _type);
		if (num == -1)
		{
			return false;
		}
		if (GetCollectedAmount(_type, BuildingStatus.COMPLETED, include_incoming: true) >= currentUnlock.costsPickup[num].intValue)
		{
			return false;
		}
		return true;
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(_pickup, point);
		_pickup.Delete();
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		if (!antsInside.Contains(_ant))
		{
			antsInside.Add(_ant);
		}
		_ant.SetCurrentTrail(null);
		_ant.SetMoveState(MoveState.DeadAndDisabled);
		_ant.transform.position += Random.insideUnitSphere * 5f;
		_ant.transform.position.SetY(_ant.transform.position.y + 2.5f);
		GameManager.instance.UpdateAntCount();
		ant_entered = true;
		return 0f;
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (ant.GetCarryingPickupsCount() > 0)
		{
			warning = "ANT_CANT_ENTER_CARRY";
			return false;
		}
		if (currentUnlock == null || currentUnlock.IsCompleted())
		{
			return false;
		}
		if (!currentUnlock.costsAnt.ToDictionary().ContainsKey(ant.caste))
		{
			warning = "ANT_CANT_ENTER_WRONG_CASTE";
			return false;
		}
		int num = 0;
		foreach (Ant item in antsInside)
		{
			if (item.caste == ant.caste)
			{
				num++;
			}
		}
		foreach (Trail gateTrail in gateTrails)
		{
			foreach (Ant item2 in EAntsOnBuildingTrails(gateTrail))
			{
				if (item2.caste == ant.caste)
				{
					num++;
				}
			}
		}
		currentUnlock.costsAnt.ToDictionary().TryGetValue(ant.caste, out var value);
		return num < value;
	}

	public override UIClickType GetUiClickType_Intake()
	{
		if (!AnythingToUnlock())
		{
			return UIClickType.BUILDING_SMALL;
		}
		return UIClickType.UNLOCKER;
	}

	public bool AnythingToUnlock()
	{
		switch (unlockerType)
		{
		case UnlockerType.IslandReveal:
			if (GetAvailableBiomeRevealsCount() > 0)
			{
				return !isRevealing;
			}
			return false;
		case UnlockerType.Unlocker:
			return !currentUnlock.IsCompleted();
		default:
			return false;
		}
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		if (GetUiClickType() == UIClickType.UNLOCKER)
		{
			UIClickLayout_Unlocker obj = (UIClickLayout_Unlocker)ui_building;
			obj.SetUnlocker(this, delegate
			{
				doUnlock = true;
				Gameplay.instance.Select(null);
			});
			obj.inventoryGrid.SetDesiredConstraints(3);
		}
		else if (unlockerType == UnlockerType.Unlocker)
		{
			ui_building.SetInfo(Loc.GetUI("UNLOCKER_UNLOCK_NOTHING"));
		}
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (GetUiClickType() == UIClickType.UNLOCKER)
		{
			UIClickLayout_Unlocker obj = (UIClickLayout_Unlocker)ui_click;
			obj.SetInventory(target: true);
			GatherRecipeProgress(out var ant_icons, out var pickup_icons, out var go);
			obj.inventoryGrid.Update(Loc.GetUI("UNLOCKER_REQUIRES"), pickup_icons, ant_icons, Loc.GetUI("GENERIC_NOTHING"));
			obj.SetUnlockButton(go || DebugSettings.standard.freeUnlocks);
		}
	}

	public void GatherRecipeProgress(out List<(AntCaste, string)> ant_icons, out List<(PickupType, string)> pickup_icons, out bool go)
	{
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false);
		pickup_icons = new List<(PickupType, string)>();
		ant_icons = new List<(AntCaste, string)>();
		go = true;
		foreach (PickupCost item in currentUnlock.costsPickup)
		{
			PickupType type = item.type;
			int num = (dicCollectedPickups.ContainsKey(type) ? dicCollectedPickups[type] : 0);
			pickup_icons.Add((type, $"{num} / {item.intValue}"));
			if (num < item.intValue)
			{
				go = false;
			}
		}
		foreach (KeyValuePair<AntCaste, int> item2 in currentUnlock.costsAnt.ToDictionary())
		{
			AntCaste key = item2.Key;
			Dictionary<AntCaste, int> dictionary = antsInside.ToDictionary();
			int num2 = (dictionary.ContainsKey(key) ? dictionary[key] : 0);
			ant_icons.Add((key, $"{num2} / {item2.Value}"));
			if (num2 < item2.Value)
			{
				go = false;
			}
		}
	}

	public int GetNAntsInside()
	{
		return antsInside.Count;
	}

	public List<IslandUnlocks> AllIslandUnlocks()
	{
		if (allUnlocks != null)
		{
			return allUnlocks;
		}
		allUnlocks = new List<IslandUnlocks>();
		if (WorldSettings.sandbox)
		{
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_BLUE, new List<string> { "REVEALBIOME_BLUE_SANDBOX" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_DESERT, new List<string> { "REVEALBIOME_DESERT_SANDBOX" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_JUNGLE, new List<string> { "REVEALBIOME_JUNGLE_SANDBOX" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_TOXIC, new List<string> { "REVEALBIOME_TOXIC_SANDBOX" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_CONCRETE, new List<string> { "REVEALBIOME_CONCRETE_SANDBOX" }));
		}
		else
		{
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_BLUE, new List<string> { "REVEALBIOME_BLUE1", "REVEALBIOME_BLUE2", "REVEALBIOME_BLUE3", "REVEALBIOME_BLUE4", "REVEALBIOME_BLUE5", "REVEALBIOME_BLUE6" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_DESERT, new List<string> { "REVEALBIOME_DESERT1", "REVEALBIOME_DESERT2", "REVEALBIOME_DESERT3", "REVEALBIOME_DESERT4", "REVEALBIOME_DESERT5", "REVEALBIOME_DESERT6" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_JUNGLE, new List<string> { "REVEALBIOME_JUNGLE1", "REVEALBIOME_JUNGLE2", "REVEALBIOME_JUNGLE3", "REVEALBIOME_JUNGLE4", "REVEALBIOME_JUNGLE5", "REVEALBIOME_JUNGLE6" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_TOXIC, new List<string> { "REVEALBIOME_TOXIC1", "REVEALBIOME_TOXIC2", "REVEALBIOME_TOXIC3", "REVEALBIOME_TOXIC4", "REVEALBIOME_TOXIC5", "REVEALBIOME_TOXIC6" }));
			allUnlocks.Add(new IslandUnlocks(GeneralUnlocks.RADAR_CONCRETE, new List<string> { "REVEALBIOME_CONCRETE1", "REVEALBIOME_CONCRETE2", "REVEALBIOME_CONCRETE3", "REVEALBIOME_CONCRETE4", "REVEALBIOME_CONCRETE5", "REVEALBIOME_CONCRETE6" }));
		}
		return allUnlocks;
	}

	public void GetUnlockInfo(out string verb, out string result, out Sprite sprite)
	{
		verb = (result = null);
		switch (unlockerType)
		{
		case UnlockerType.IslandReveal:
			verb = Loc.GetUI("UNLOCKER_WILL_REVEAL");
			switch (currentUnlock.unlockIsland)
			{
			case "BiomeBlue2":
				result = Loc.GetObject("BIOME_BLUE");
				break;
			case "BiomeScrapara":
				result = Loc.GetObject("BIOME_DESERT");
				break;
			case "BiomeGreen":
				result = Loc.GetObject("BIOME_JUNGLE");
				break;
			case "BiomeToxicwaste":
				result = Loc.GetObject("BIOME_TOXIC");
				break;
			case "BiomeConcrete":
				result = Loc.GetObject("BIOME_CONCRETE");
				break;
			default:
				result = "???";
				break;
			}
			break;
		case UnlockerType.Unlocker:
			verb = Loc.GetUI("UNLOCKER_WILL_UNLOCK");
			if (currentUnlock.unlockBuildings.Count > 0)
			{
				result = BuildingData.Get(currentUnlock.unlockBuildings[0]).GetTitle();
			}
			else if (currentUnlock.unlockRecipes.Count > 0)
			{
				result = FactoryRecipeData.Get(currentUnlock.unlockRecipes[0]).GetTitle();
			}
			break;
		}
		sprite = currentUnlock.GetIcon();
	}

	public override bool CanTrack()
	{
		if (!base.CanTrack())
		{
			return AnythingToUnlock();
		}
		return true;
	}
}
