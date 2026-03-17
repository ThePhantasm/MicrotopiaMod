using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Factory : Storage
{
	[Serializable]
	public class UnlockReqForProcessRecipe
	{
		public GeneralUnlocks unlock;

		public string recipe = "";
	}

	[Header("Factory")]
	[SerializeField]
	private List<GameObject> enableDuringProcess = new List<GameObject>();

	public float processAnimationDuration = 1f;

	public bool needEmptyOutputToWork;

	public bool needStoredRecipeToWork;

	public bool showRecipeIngredients = true;

	public bool spitOutProduct = true;

	public bool antShouldForProduct = true;

	public float antWaitBeforeMoving;

	public List<UnlockReqForProcessRecipe> unlocksReqForProcessRecipe;

	protected bool isProcessing = true;

	protected string storedRecipe = "";

	private List<Pickup> takenFromAttachment = new List<Pickup>();

	private List<PickupType> storedProducts = new List<PickupType>();

	protected FactoryRecipeData processingRecipe;

	protected float processTime;

	private bool willProcess;

	private bool playerMustChooseRecipe;

	protected bool allowChangeRecipe = true;

	[Header("Ant Factory")]
	public int antSlots;

	public bool showAntsInside;

	[NonSerialized]
	public List<Trail> productTrails = new List<Trail>();

	[NonSerialized]
	public List<Ant> antsInside = new List<Ant>();

	[NonSerialized]
	public List<Ant> antsWaitingToExit = new List<Ant>();

	[NonSerialized]
	public List<(Ant, float)> antsWaitingToMove = new List<(Ant, float)>();

	private List<int> antsInside_links = new List<int>();

	private List<int> antsWaitingToExit_links = new List<int>();

	private List<int> antsWaitingToMove_links = new List<int>();

	private int collectedCentipedeLength;

	private float checkRecipeTimer;

	private bool noIngredientsInAntFactory;

	private bool buildingNeedsPickups;

	[SerializeField]
	[Tooltip("Audio played once when activation starts (use for 'warm up' before loop, or if activation uses a single audio instead of looping)")]
	private AudioLink audioWindUp;

	[SerializeField]
	[Tooltip("Audio played looping during activation (after windup is done, if that is set)")]
	private AudioLink audioActiveLoop;

	[SerializeField]
	[Tooltip("Audio played once when activation ends")]
	private AudioLink audioWindDown;

	private bool waitWithLoopAudio;

	[SerializeField]
	private Transform antsParent;

	[SerializeField]
	private bool cheat;

	private float updateSometimesTimer;

	[SerializeField]
	protected PickupType requiresDispenserPickup;

	[SerializeField]
	private Transform dispenserBillboardParent;

	[Tooltip("If dispenser is attached, require enough pickups in dispenser before accepting ingredients from ants")]
	[SerializeField]
	private bool requirePickupsPipedIn;

	private List<TrailGate_Link> passOnGates = new List<TrailGate_Link>();

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(isProcessing);
		save.Write(takenFromAttachment.Count);
		if (takenFromAttachment.Count > 0)
		{
			foreach (Pickup item in takenFromAttachment)
			{
				save.Write(item.linkId);
			}
		}
		save.Write(storedProducts.Count);
		if (storedProducts.Count > 0)
		{
			foreach (PickupType storedProduct in storedProducts)
			{
				save.Write((int)storedProduct);
			}
		}
		save.Write((processingRecipe == null) ? "" : processingRecipe.code);
		save.Write(processTime);
		WriteConfig(save);
		save.Write(productTrails.Count);
		for (int i = 0; i < productTrails.Count; i++)
		{
			save.Write(productTrails[i].linkId);
		}
		if (antSlots == 0)
		{
			save.Write(antsInside.Count);
			foreach (Ant item2 in antsInside)
			{
				save.Write((!(item2 == null)) ? item2.linkId : 0);
			}
		}
		else
		{
			if (antsInside.Count != antSlots)
			{
				Debug.LogError($"AntsInside mismatch; count {antsInside.Count} but slots {antSlots}");
			}
			for (int j = 0; j < antSlots; j++)
			{
				Ant ant = ((j < antsInside.Count) ? antsInside[j] : null);
				save.Write((!(ant == null)) ? ant.linkId : 0);
			}
		}
		save.Write(antsWaitingToExit.Count);
		foreach (Ant item3 in antsWaitingToExit)
		{
			save.Write(item3.linkId);
		}
		save.Write(antsWaitingToMove.Count);
		foreach (var item4 in antsWaitingToMove)
		{
			save.Write(item4.Item1.linkId);
			save.Write(item4.Item2);
		}
		save.Write(collectedCentipedeLength);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		bool target = save.ReadBool();
		takenFromAttachment = new List<Pickup>();
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Pickup pickup = GameManager.instance.FindLink<Pickup>(save.ReadInt());
			if (pickup == null)
			{
				Debug.LogError("Pickup returned null while loading, shouldn't happen.");
				continue;
			}
			pickup.SetStatus(PickupStatus.IN_CONTAINER);
			pickup.SetObActive(active: false);
			takenFromAttachment.Add(pickup);
		}
		storedProducts = new List<PickupType>();
		num = save.ReadInt();
		for (int j = 0; j < num; j++)
		{
			storedProducts.Add((PickupType)save.ReadInt());
		}
		string text = save.ReadString();
		processingRecipe = ((text == "") ? null : FactoryRecipeData.Get(text));
		processTime = save.ReadFloat();
		SetProcess(target, on_init: true);
		ReadConfig(save);
		if (save.version < 7)
		{
			if (buildingAttachPoints.Count > 0)
			{
				Debug.LogWarning("Building " + data.GetTitle() + " might be out of date, please demolish and build again");
			}
			if (save.version < 6)
			{
				save.ReadInt();
			}
			else
			{
				int num2 = save.ReadInt();
				for (int k = 0; k < num2; k++)
				{
					save.ReadInt();
				}
			}
		}
		productTrails = new List<Trail>();
		if (save.version < 5)
		{
			int num3 = save.ReadInt();
			if (num3 != 0)
			{
				productTrails.Add(GameManager.instance.FindLink<Trail>(num3));
			}
		}
		else
		{
			num = save.ReadInt();
			for (int l = 0; l < num; l++)
			{
				productTrails.Add(GameManager.instance.FindLink<Trail>(save.ReadInt()));
			}
		}
		antsInside_links = new List<int>();
		antsWaitingToExit_links = new List<int>();
		antsWaitingToMove_links = new List<int>();
		if (save.version < 19)
		{
			num = save.ReadInt();
			for (int m = 0; m < num; m++)
			{
				save.ReadInt();
			}
		}
		if (save.version >= 54 || antSlots != 0)
		{
			num = ((antSlots == 0) ? save.ReadInt() : antSlots);
			for (int n = 0; n < num; n++)
			{
				int num4 = save.ReadInt();
				if (num4 == -1)
				{
					num4 = 0;
				}
				antsInside_links.Add(num4);
			}
		}
		num = save.ReadInt();
		for (int num5 = 0; num5 < num; num5++)
		{
			antsWaitingToExit_links.Add(save.ReadInt());
		}
		if (save.version >= 16)
		{
			num = save.ReadInt();
			for (int num6 = 0; num6 < num; num6++)
			{
				antsWaitingToMove_links.Add(save.ReadInt());
				float num7 = ((save.version > 16) ? save.ReadFloat() : 0f);
				if (num7 > 60f)
				{
					num7 = 0f;
				}
				antsWaitingToMove.Add((null, num7));
			}
		}
		collectedCentipedeLength = save.ReadInt();
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write((storedRecipe == null) ? "" : storedRecipe);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		string text = save.ReadString();
		if (save.GetSaveType() == SaveType.GameSave)
		{
			storedRecipe = text;
		}
		else if (text != "" && Progress.HasUnlockedRecipe(text))
		{
			SetStoredRecipe(text);
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		antsInside.Clear();
		antsWaitingToExit.Clear();
		foreach (int antsInside_link in antsInside_links)
		{
			Ant ant = GameManager.instance.FindLink<Ant>(antsInside_link);
			if (ant != null || antSlots > 0)
			{
				antsInside.Add(ant);
				if (ant != null)
				{
					ParentAntIfNeeded(ant);
				}
			}
		}
		foreach (int antsWaitingToExit_link in antsWaitingToExit_links)
		{
			antsWaitingToExit.Add(GameManager.instance.FindLink<Ant>(antsWaitingToExit_link));
		}
		for (int i = 0; i < antsWaitingToMove_links.Count; i++)
		{
			float item = antsWaitingToMove[i].Item2;
			antsWaitingToMove[i] = (GameManager.instance.FindLink<Ant>(antsWaitingToMove_links[i]), item);
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (!during_load)
		{
			SetProcess(target: false, on_init: true);
			antsInside = new List<Ant>();
			for (int i = 0; i < antSlots; i++)
			{
				antsInside.Add(null);
			}
		}
		buildingNeedsPickups = false;
		foreach (string recipe in data.recipes)
		{
			if (FactoryRecipeData.Get(recipe).costsPickup.Count > 0)
			{
				buildingNeedsPickups = true;
				break;
			}
		}
	}

	public override IEnumerable<ParticleSystem> EPausableParticles()
	{
		foreach (ParticleSystem item in base.EPausableParticles())
		{
			yield return item;
		}
		foreach (GameObject item2 in enableDuringProcess)
		{
			if (item2.TryGetComponent<ParticleSystem>(out var component))
			{
				yield return component;
			}
		}
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld)
		{
			return;
		}
		bool flag = false;
		foreach (Ant item in antsInside)
		{
			if (item != null)
			{
				flag = true;
				break;
			}
		}
		if (flag && processingRecipe == null)
		{
			checkRecipeTimer -= dt;
			if (checkRecipeTimer < 0f)
			{
				SetProcessingRecipe();
				noIngredientsInAntFactory = processingRecipe == null && buildingNeedsPickups;
				checkRecipeTimer = 0.5f;
			}
		}
		EnableSpawnedAnt();
		if (antsWaitingToMove.Count > 0)
		{
			List<(Ant, float)> list = new List<(Ant, float)>();
			for (int i = 0; i < antsWaitingToMove.Count; i++)
			{
				if (antsWaitingToMove[i].Item1 == null)
				{
					Debug.LogError("AntsWaitingToMove was null, shouldn't happen");
					list.Add(antsWaitingToMove[i]);
					continue;
				}
				float num = Mathf.Max(antsWaitingToMove[i].Item2 - dt, 0f);
				if (num == 0f && SpawnedAntCanMove())
				{
					antsWaitingToMove[i].Item1.SetMoveState(MoveState.Normal);
					list.Add(antsWaitingToMove[i]);
				}
				else
				{
					antsWaitingToMove[i] = (antsWaitingToMove[i].Item1, num);
				}
			}
			foreach (var item2 in list)
			{
				antsWaitingToMove.Remove(item2);
				exitSplit.UpdateBillboardSoon();
			}
			UpdateBillboard();
		}
		if (buildingAttachPoints.Count > 0 && processingRecipe == null)
		{
			updateSometimesTimer += dt;
			if (updateSometimesTimer > 0.5f)
			{
				updateSometimesTimer = 0f;
				bool flag2 = false;
				foreach (KeyValuePair<PickupType, int> item3 in dicCollectedPickups_intake)
				{
					if (item3.Value > 0)
					{
						flag2 = true;
						break;
					}
				}
				if (flag2)
				{
					SetProcessingRecipe();
				}
			}
		}
		SetProcess(Process(dt));
		UpdateFactoryAudio();
	}

	public void EnableSpawnedAnt()
	{
		if (antsWaitingToExit.Count <= 0)
		{
			return;
		}
		List<Trail> list = new List<Trail>();
		foreach (Trail productTrail in productTrails)
		{
			if (productTrail.currentAnts.Count == 0)
			{
				list.Add(productTrail);
			}
		}
		if (list.Count > 0)
		{
			Trail trail = list[UnityEngine.Random.Range(0, list.Count)];
			Ant ant = antsWaitingToExit[0];
			ant.transform.SetPositionAndRotation(trail.posStart, Quaternion.LookRotation(trail.direction));
			ant.GetOnNewTrail(trail);
			antsWaitingToExit.RemoveAt(0);
			if (SpawnedAntCanMove() && antWaitBeforeMoving == 0f)
			{
				ant.SetMoveState(MoveState.Normal);
				return;
			}
			antsWaitingToMove.Add((ant, antWaitBeforeMoving));
			ant.SetMoveState(MoveState.Waiting);
			exitSplit.UpdateBillboardSoon();
		}
	}

	protected virtual bool SpawnedAntCanMove()
	{
		if (exitSplit != null)
		{
			foreach (Trail connectedTrail in exitSplit.connectedTrails)
			{
				if (connectedTrail.splitStart == exitSplit && connectedTrail.IsPlaced())
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public override void Demolish()
	{
		foreach (PickupType storedProduct in storedProducts)
		{
			AddPickup(storedProduct, BuildingStatus.COMPLETED);
		}
		foreach (Pickup item in new List<Pickup>(takenFromAttachment))
		{
			item.Delete();
		}
		foreach (Ant item2 in antsInside)
		{
			if (!(item2 != null))
			{
				continue;
			}
			if (AntsDieInside())
			{
				History.RegisterAntEnd(item2, repurposed: false);
				item2.Delete();
				continue;
			}
			if (item2.transform.parent != null)
			{
				item2.transform.parent = null;
			}
			DropAntOnGround(item2);
		}
		antsInside.Clear();
		foreach (Ant item3 in antsWaitingToExit)
		{
			DropAntOnGround(item3);
		}
		antsWaitingToExit.Clear();
		foreach (var item4 in antsWaitingToMove)
		{
			DropAntOnGround(item4.Item1);
		}
		antsWaitingToMove.Clear();
		base.Demolish();
	}

	protected virtual bool AntsDieInside()
	{
		return true;
	}

	protected override void DoDelete()
	{
		if (antsInside.Count > 0 || antsWaitingToExit.Count > 0 || antsWaitingToMove.Count > 0)
		{
			bool flag = false;
			if (antsInside.Count > 0)
			{
				flag = true;
				foreach (Ant item in antsInside)
				{
					if (item != null)
					{
						flag = false;
					}
				}
			}
			if (!flag)
			{
				Debug.LogWarning($"{base.name}: Deleting with ants inside ({antsInside.Count}, {antsWaitingToExit.Count}, {antsWaitingToMove.Count}), shouldnt happen");
			}
		}
		base.DoDelete();
	}

	protected override bool CanPause()
	{
		return true;
	}

	protected override ExchangeType TrailInteraction_Intake(Trail _trail)
	{
		return base.TrailInteraction_Intake(_trail);
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (isProcessing)
		{
			return false;
		}
		if (IsPaused() || exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		if (requiresDispenserPickup != PickupType.NONE && requiresDispenserPickup != PickupType.ANY && _type == requiresDispenserPickup)
		{
			return false;
		}
		int n;
		bool flag = HasSpaceLeft(_type, PileType.INPUT, point, out n);
		if (HasPiles(PileType.INPUT) && !flag)
		{
			return false;
		}
		if (OutputFull() && !spitOutProduct)
		{
			return false;
		}
		if (antsWaitingToMove.Count > 0)
		{
			return false;
		}
		if (requirePickupsPipedIn)
		{
			Dictionary<PickupType, int> dicAvailablePickups = GetDicAvailablePickups(include_incoming: false);
			List<string> list = new List<string>();
			if (storedRecipe != "")
			{
				list.Add(storedRecipe);
			}
			else
			{
				list.AddRange(EFactoryRecipes());
			}
			foreach (string item in list)
			{
				foreach (PickupCost item2 in FactoryRecipeData.Get(item).costsPickup)
				{
					bool flag2 = false;
					if (_type == item2.type && item2.intValue == 1)
					{
						flag2 = true;
					}
					else if (dicAvailablePickups.ContainsKey(item2.type) && dicAvailablePickups[item2.type] >= item2.intValue)
					{
						flag2 = true;
					}
					if (!flag2)
					{
						return false;
					}
				}
			}
			return true;
		}
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(include_incoming: true);
		if (!dicCollectedPickups.ContainsKey(_type))
		{
			dicCollectedPickups.Add(_type, 0);
		}
		dicCollectedPickups[_type]++;
		Dictionary<PickupType, int> dicAvailablePickups2 = GetDicAvailablePickups(include_incoming: true);
		if (GetPotentialRecipes(dicCollectedPickups.AddDictionary(dicAvailablePickups2), GetCollectedAntCastes_list(include_incoming: true), dicAvailablePickups2.ToList()).Count > 0)
		{
			return true;
		}
		return false;
	}

	protected override void PrepareForPickup_Intake(Pickup _pickup, ExchangePoint _point)
	{
		base.PrepareForPickup_Intake(_pickup, _point);
		if (OutputFull() && spitOutProduct)
		{
			foreach (PickupType item in new List<PickupType>(storedProducts))
			{
				storedProducts.Remove(item);
				extractablePickupsChanged = true;
				Pickup pickup = ((!HasPiles(PileType.OUTPUT)) ? GameManager.instance.SpawnPickup(item, base.transform.position, base.transform.rotation) : TakeFromPiles(item, PileType.OUTPUT));
				if (pickup != null)
				{
					DropPickup(pickup);
				}
			}
		}
		if (_point != null)
		{
			foreach (Pile pile in piles)
			{
				if (pile.assignedExchangePoint == _point && pile.IsEmpty())
				{
					pile.ReservePile(_pickup.type);
				}
			}
		}
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(include_incoming: true);
		Dictionary<PickupType, int> dicAvailablePickups = GetDicAvailablePickups(include_incoming: true);
		if (GetCompletedRecipes(dicCollectedPickups.AddDictionary(dicAvailablePickups)).Count > 0)
		{
			willProcess = true;
		}
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		if (CamController.instance.GetFollowTarget() == _pickup.transform)
		{
			CamController.instance.SetFollowFactoryIngredient(this);
		}
		if (incomingPickups_intake.Contains(_pickup))
		{
			incomingPickups_intake.Remove(_pickup);
		}
		AddPickup(_pickup.type, BuildingStatus.COMPLETED);
		if (HasPiles(PileType.INPUT))
		{
			AddToPiles(_pickup, PileType.INPUT, point, content_priority: true);
		}
		else
		{
			_pickup.Delete();
		}
		GameManager.instance.UpdatePickupInventory();
		if (processingRecipe == null)
		{
			SetProcessingRecipe();
		}
	}

	protected Dictionary<PickupType, int> GetDicCollectedPickups(bool include_incoming)
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>(dicCollectedPickups_intake);
		if (include_incoming)
		{
			foreach (Pickup item in incomingPickups_intake)
			{
				if (!dictionary.ContainsKey(item.type))
				{
					dictionary.Add(item.type, 0);
				}
				dictionary[item.type]++;
			}
		}
		return dictionary;
	}

	protected Dictionary<PickupType, int> GetDicAvailablePickups(bool include_incoming)
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
		{
			if (buildingAttachPoint.HasDispenser(out var dis))
			{
				dictionary.AddDictionary(dis.GetDicAvailablePickups(include_incoming));
			}
		}
		return dictionary;
	}

	public void SetStoredRecipe(string _recipe)
	{
		if (!(GetStoredRecipe() == _recipe))
		{
			storedRecipe = _recipe;
			ResetProcess();
			if (storedRecipe != "")
			{
				DropUnwantedIngredients(FactoryRecipeData.Get(storedRecipe));
			}
			SetProcessingRecipe();
		}
	}

	protected virtual void DropUnwantedIngredients(FactoryRecipeData recipe)
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		foreach (Pile pile in piles)
		{
			if (pile.pileType != PileType.INPUT)
			{
				continue;
			}
			foreach (Pickup pickup in pile.GetPickups())
			{
				if (!dictionary.ContainsKey(pickup.type))
				{
					dictionary.Add(pickup.type, 0);
				}
				dictionary[pickup.type]++;
			}
		}
		foreach (KeyValuePair<PickupType, int> item in dictionary)
		{
			int num = (dicCollectedPickups_intake.ContainsKey(item.Key) ? dicCollectedPickups_intake[item.Key] : 0);
			if (item.Value != num)
			{
				Debug.LogError(base.name + " collected pickups/piles mismatch; " + item.Value + " collected: " + num + ", found in piles: " + item.Value);
				dicCollectedPickups_intake[item.Key] = item.Value;
			}
		}
		foreach (KeyValuePair<PickupType, int> item2 in new Dictionary<PickupType, int>(dicCollectedPickups_intake))
		{
			bool flag = true;
			int num2 = 0;
			foreach (PickupCost item3 in recipe.costsPickup)
			{
				if (item3.type != PickupType.NONE)
				{
					if (item3.type == item2.Key)
					{
						flag = false;
						if (item2.Value > item3.intValue)
						{
							num2 += item2.Value - item3.intValue;
						}
					}
				}
				else if (item3.category != PickupCategory.NONE && item2.Key.IsCategory(item3.category))
				{
					flag = false;
					if (item2.Value > item3.intValue)
					{
						num2 += item2.Value - item3.intValue;
					}
				}
			}
			if (flag)
			{
				num2 = item2.Value;
			}
			DropPickups(item2.Key, num2);
		}
		List<Ant> list = new List<Ant>();
		foreach (Ant item4 in antsInside)
		{
			if (item4 != null)
			{
				list.Add(item4);
			}
		}
		foreach (AntCaste item5 in recipe.costsAnt.ToEnumList())
		{
			foreach (Ant item6 in list)
			{
				if (item6.caste == item5)
				{
					list.Remove(item6);
					break;
				}
			}
		}
		foreach (Ant item7 in list)
		{
			if (antSlots == 0)
			{
				antsInside.Remove(item7);
			}
			else
			{
				for (int i = 0; i < antsInside.Count; i++)
				{
					if (item7 == antsInside[i])
					{
						antsInside[i] = null;
						break;
					}
				}
			}
			if (AntsDieInside())
			{
				History.RegisterAntEnd(item7, repurposed: false);
				item7.Delete();
			}
			else
			{
				antsWaitingToExit.Add(item7);
			}
		}
	}

	public string GetStoredRecipe()
	{
		return storedRecipe;
	}

	public void SetProcessingRecipe()
	{
		playerMustChooseRecipe = false;
		noIngredientsInAntFactory = false;
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(include_incoming: false);
		Dictionary<PickupType, int> dicAvailablePickups = GetDicAvailablePickups(include_incoming: false);
		Dictionary<PickupType, int> collected_pickups = dicCollectedPickups.AddDictionary(dicAvailablePickups);
		List<string> completedRecipes = GetCompletedRecipes(collected_pickups);
		if (completedRecipes.Count > 0)
		{
			playerMustChooseRecipe = CheckIfNeedStoredRecipe(completedRecipes, GetPotentialRecipes(collected_pickups, GetCollectedAntCastes_list(include_incoming: false), dicAvailablePickups.ToList()));
			if (!playerMustChooseRecipe)
			{
				processingRecipe = FactoryRecipeData.Get(completedRecipes[0]);
			}
			if (processingRecipe != null)
			{
				foreach (PickupCost item in processingRecipe.costsPickup)
				{
					int num = item.intValue;
					Dictionary<PickupType, int> dicCollectedPickups2 = GetDicCollectedPickups(include_incoming: false);
					if (dicCollectedPickups2.ContainsKey(item.type))
					{
						num -= dicCollectedPickups2[item.type];
					}
					if (num <= 0)
					{
						continue;
					}
					foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
					{
						if (!buildingAttachPoint.HasDispenser(out var dis))
						{
							continue;
						}
						Dictionary<PickupType, int> dicAvailablePickups2 = dis.GetDicAvailablePickups(include_incoming: false);
						if (dicAvailablePickups2.ContainsKey(item.type) && dicAvailablePickups2[item.type] >= num)
						{
							for (int i = 0; i < num; i++)
							{
								Pickup pickup = dis.ExtractPickup(item.type);
								pickup.SetStatus(PickupStatus.IN_CONTAINER);
								pickup.SetObActive(active: false);
								takenFromAttachment.Add(pickup);
							}
							break;
						}
					}
				}
			}
			SetProcess(Process(0f));
		}
		StartCoroutine(CUpdateBillboardDelayed(0.2f));
	}

	public List<string> GetPotentialRecipes(Dictionary<PickupType, int> collected_pickups, List<AntCaste> collected_ants, List<PickupType> exclude_from_excess)
	{
		if (!data.autoRecipe && storedRecipe == "")
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		foreach (string item in EFactoryRecipes())
		{
			if (storedRecipe != "" && item != storedRecipe)
			{
				continue;
			}
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(item);
			bool flag = false;
			if (factoryRecipeData.costsAnt.Count == 0)
			{
				flag = collected_ants != null && collected_ants.Count > 0;
			}
			else if (collected_ants == null)
			{
				flag = true;
			}
			else
			{
				List<AntCaste> list2 = new List<AntCaste>(factoryRecipeData.costsAnt.ToEnumList());
				foreach (AntCaste collected_ant in collected_ants)
				{
					if (list2.Contains(collected_ant))
					{
						list2.Remove(collected_ant);
						continue;
					}
					flag = true;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			bool flag2 = false;
			foreach (KeyValuePair<PickupType, int> collected_pickup in collected_pickups)
			{
				if (collected_pickup.Key == PickupType.NONE || collected_pickup.Value == 0)
				{
					continue;
				}
				PickupCost pickupCost = null;
				foreach (PickupCost item2 in factoryRecipeData.costsPickup)
				{
					if (item2.type == collected_pickup.Key)
					{
						pickupCost = item2;
						break;
					}
				}
				if (pickupCost == null)
				{
					flag2 = true;
				}
				else if (!exclude_from_excess.Contains(collected_pickup.Key) && collected_pickup.Value > pickupCost.intValue)
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public List<string> GetCompletedRecipes(Dictionary<PickupType, int> collected_pickups)
	{
		if (DebugSettings.standard.freeRecipes && storedRecipe != "")
		{
			return new List<string> { storedRecipe };
		}
		List<string> list = new List<string>();
		foreach (string item in EFactoryRecipes())
		{
			if ((storedRecipe != "" || needStoredRecipeToWork) && item != storedRecipe)
			{
				continue;
			}
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(item);
			if (factoryRecipeData.costsAnt.Count > 0)
			{
				List<AntCaste> collectedAntCastes_list = GetCollectedAntCastes_list(include_incoming: false);
				if (collectedAntCastes_list == null || collectedAntCastes_list.Count < factoryRecipeData.costsAnt.Count())
				{
					continue;
				}
				List<AntCaste> list2 = new List<AntCaste>(collectedAntCastes_list);
				bool flag = false;
				foreach (AntCasteAmount item2 in factoryRecipeData.costsAnt)
				{
					for (int i = 0; i < item2.intValue; i++)
					{
						if (list2.Contains(item2.type))
						{
							list2.Remove(item2.type);
							continue;
						}
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
			}
			bool flag2 = false;
			foreach (PickupCost item3 in factoryRecipeData.costsPickup)
			{
				if (item3.type != PickupType.NONE && (!collected_pickups.ContainsKey(item3.type) || collected_pickups[item3.type] < item3.intValue))
				{
					flag2 = true;
					break;
				}
				if (item3.category == PickupCategory.NONE)
				{
					continue;
				}
				int num = 0;
				foreach (KeyValuePair<PickupType, int> collected_pickup in collected_pickups)
				{
					if (collected_pickup.Key.IsCategory(item3.category))
					{
						num += collected_pickup.Value;
					}
				}
				if (num < item3.intValue)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				list.Add(item);
			}
		}
		return list;
	}

	protected virtual List<AntCaste> GetCollectedAntCastes_list(bool include_incoming)
	{
		List<AntCaste> list = new List<AntCaste>();
		foreach (Ant item in antsInside)
		{
			if (item != null)
			{
				list.Add(item.caste);
			}
		}
		if (include_incoming)
		{
			foreach (Trail enterTrail in enterTrails)
			{
				foreach (Ant item2 in EAntsOnBuildingTrails(enterTrail))
				{
					if (!list.Contains(item2.caste))
					{
						list.Add(item2.caste);
					}
				}
			}
		}
		return list;
	}

	protected virtual Dictionary<AntCaste, int> GetCollectedAntCastes_dic(bool include_incoming)
	{
		Dictionary<AntCaste, int> dictionary = new Dictionary<AntCaste, int>();
		foreach (Ant item in antsInside)
		{
			if (item != null)
			{
				if (!dictionary.ContainsKey(item.caste))
				{
					dictionary.Add(item.caste, 0);
				}
				dictionary[item.caste]++;
			}
		}
		if (!include_incoming)
		{
			return dictionary;
		}
		List<Ant> list = new List<Ant>(antsInside);
		foreach (Trail enterTrail in enterTrails)
		{
			foreach (Ant item2 in EAntsOnBuildingTrails(enterTrail))
			{
				if (!list.Contains(item2))
				{
					list.Add(item2);
					if (!dictionary.ContainsKey(item2.caste))
					{
						dictionary.Add(item2.caste, 0);
					}
					dictionary[item2.caste]++;
				}
			}
		}
		return dictionary;
	}

	private bool CheckIfNeedStoredRecipe(List<string> completed_recipes, List<string> potential_recipes)
	{
		if (storedRecipe == "" && completed_recipes.Count > 0 && potential_recipes.Count > 1)
		{
			return true;
		}
		return false;
	}

	protected virtual void SetProcess(bool target, bool on_init = false)
	{
		if (isProcessing == target && !on_init)
		{
			return;
		}
		isProcessing = target;
		if (!on_init)
		{
			if (isProcessing)
			{
				if (audioWindUp.IsSet())
				{
					PlayAudio(audioWindUp);
					if (audioActiveLoop.IsSet())
					{
						waitWithLoopAudio = true;
					}
				}
				else
				{
					StartLoopAudio(audioActiveLoop);
				}
			}
			else
			{
				waitWithLoopAudio = false;
				StopAudio();
				PlayAudio(audioWindDown);
			}
		}
		else if (isProcessing)
		{
			if (audioWindUp.IsSet() && processTime < audioWindUp.GetLength())
			{
				PlayAudio(audioWindUp, processTime);
				if (audioActiveLoop.IsSet())
				{
					waitWithLoopAudio = true;
				}
			}
			else
			{
				StartLoopAudio(audioActiveLoop);
			}
		}
		if (anim != null)
		{
			anim.SetBool(ClickableObject.paramDoAction, isProcessing);
		}
		foreach (GameObject item in enableDuringProcess)
		{
			item.SetObActive(isProcessing);
		}
	}

	private void UpdateFactoryAudio()
	{
		if (waitWithLoopAudio && !IsPlayingAudio())
		{
			waitWithLoopAudio = false;
			StartLoopAudio(audioActiveLoop);
		}
	}

	private void ResetProcess()
	{
		processTime = 0f;
		processingRecipe = null;
		willProcess = false;
	}

	public virtual bool Process(float dt)
	{
		if (processingRecipe == null)
		{
			return false;
		}
		foreach (UnlockReqForProcessRecipe item in unlocksReqForProcessRecipe)
		{
			if (item.recipe == processingRecipe.code && !Progress.HasUnlocked(item.unlock))
			{
				return false;
			}
		}
		foreach (PickupCost productPickup in processingRecipe.productPickups)
		{
			HasSpaceLeft(productPickup.type, PileType.OUTPUT, null, out var n);
			if (productPickup.type != PickupType.NONE && n < productPickup.intValue)
			{
				return true;
			}
		}
		if (antsWaitingToExit.Count > 0)
		{
			return true;
		}
		processTime += dt;
		float processDuration = GetProcessDuration();
		if (processTime >= processDuration)
		{
			BuildProduct(processingRecipe, DebugSettings.standard.freeRecipes && storedRecipe != "");
			ResetProcess();
			SetProcessingRecipe();
		}
		return true;
	}

	protected virtual void BuildProduct(FactoryRecipeData recipe, bool free)
	{
		bool flag = CamController.instance.GetFollowFactoryIngredient() == this;
		List<TrailGate_Link> list = new List<TrailGate_Link>();
		if (!free)
		{
			foreach (PickupCost item in recipe.costsPickup)
			{
				if (item.type == PickupType.NONE)
				{
					continue;
				}
				int num = item.intValue;
				int num2;
				if (GetDicCollectedPickups(include_incoming: false).ContainsKey(item.type))
				{
					num2 = num;
					for (int i = 0; i < num2; i++)
					{
						if (GetDicCollectedPickups(include_incoming: false)[item.type] == 0)
						{
							break;
						}
						RemovePickup(item.type, 1, BuildingStatus.COMPLETED);
						num--;
						if (HasPiles(PileType.INPUT))
						{
							Pickup pickup = TakeFromPiles(item.type, PileType.INPUT);
							if (pickup == null)
							{
								Debug.LogError(base.name + ": Tried deleting " + item.type.ToString() + " from pile that wasn't present, shouldn't happen");
							}
							else
							{
								pickup.Delete();
							}
						}
					}
				}
				if (num <= 0)
				{
					continue;
				}
				num2 = num;
				for (int j = 0; j < num2; j++)
				{
					foreach (Pickup item2 in new List<Pickup>(takenFromAttachment))
					{
						if (item2.type == item.type)
						{
							takenFromAttachment.Remove(item2);
							item2.Delete();
							num--;
						}
					}
				}
				if (num > 0)
				{
					Debug.LogError(base.name + ": Couldn't delete " + num + " amount of cost " + item.type.ToString() + " for recipe " + recipe.code + ", shouldn't happen");
				}
			}
			foreach (AntCaste item3 in processingRecipe.costsAnt.ToEnumList())
			{
				List<Ant> list2 = new List<Ant>(antsInside);
				for (int k = 0; k < list2.Count; k++)
				{
					Ant ant = list2[k];
					if (!(ant != null) || ant.caste != item3)
					{
						continue;
					}
					if (CamController.instance.GetFollowTarget() == ant.transform)
					{
						flag = true;
					}
					if (ant.HasStatusEffect(StatusEffect.OLD))
					{
						Progress.nOldAntsUpgraded++;
					}
					if (antSlots == 0)
					{
						antsInside.Remove(ant);
					}
					else
					{
						antsInside[k] = null;
					}
					History.RegisterAntEnd(ant, repurposed: true);
					foreach (TrailGate_Link linkedGate in GameManager.instance.GetLinkedGates(ant))
					{
						if (!list.Contains(linkedGate))
						{
							list.Add(linkedGate);
						}
					}
					ant.Delete();
					break;
				}
			}
		}
		if (recipe.productPickups.Count > 0)
		{
			foreach (PickupCost productPickup in recipe.productPickups)
			{
				if (productPickup.type == PickupType.NONE)
				{
					continue;
				}
				for (int l = 0; l < productPickup.intValue; l++)
				{
					storedProducts.Add(productPickup.type);
					extractablePickupsChanged = true;
					if (HasPiles(PileType.OUTPUT))
					{
						Pickup pickup2 = GameManager.instance.SpawnPickup(productPickup.type);
						pickup2.SetStatus(PickupStatus.IN_CONTAINER, base.transform);
						AddToPiles(pickup2, PileType.OUTPUT, null, content_priority: true);
					}
				}
				Progress.AddPickupManufactured(productPickup.type, productPickup.intValue);
			}
		}
		if (recipe.productAnts.Count <= 0)
		{
			return;
		}
		if (productTrails.Count == 0)
		{
			Debug.LogError(base.name + ": Tried spawning ant without exit trail, shouldn't happen");
			return;
		}
		foreach (AntCasteAmount productAnt in recipe.productAnts)
		{
			if (productAnt.type == AntCaste.NONE)
			{
				continue;
			}
			for (int m = 0; m < productAnt.intValue; m++)
			{
				Ant ant2 = SpawnProductAnt(productAnt.type, list);
				if (flag)
				{
					StartCoroutine(CFollowOutput(ant2));
					flag = false;
				}
			}
			Progress.AddAntCasteMade(productAnt.type, productAnt.intValue);
		}
	}

	private IEnumerator CFollowOutput(Ant ant)
	{
		yield return null;
		if (ant != null)
		{
			CamController.instance.SetFollowTarget(ant.transform);
		}
	}

	public float GetProcessDuration()
	{
		return Mathf.Clamp(DebugSettings.standard.instantFactories ? 0f : processingRecipe.processTime, processAnimationDuration, float.MaxValue);
	}

	public string GetProcessingRecipe()
	{
		if (storedRecipe != "")
		{
			return storedRecipe;
		}
		if (processingRecipe == null)
		{
			return "";
		}
		return processingRecipe.code;
	}

	public float GetProcessTime()
	{
		return processTime;
	}

	public int GetNAntsInside()
	{
		return antsInside.Count;
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (storedProducts.Count == 0)
		{
			if (!IsPaused() && antShouldForProduct && (willProcess || isProcessing || playerMustChooseRecipe))
			{
				let_ant_wait = true;
				return false;
			}
		}
		else if ((uint)(exchange - 6) <= 1u)
		{
			return true;
		}
		return false;
	}

	public override List<PickupType> GetExtractablePickupsInternal()
	{
		return storedProducts;
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		if (!storedProducts.Contains(_type))
		{
			Debug.LogError(base.name + ": Tried extracting product " + _type.ToString() + " while not present, shouldn't happen");
			return null;
		}
		storedProducts.Remove(_type);
		extractablePickupsChanged = true;
		if (HasPiles(PileType.OUTPUT))
		{
			return TakeFromPiles(_type, PileType.OUTPUT);
		}
		return GameManager.instance.SpawnPickup(_type, base.transform.position, base.transform.rotation);
	}

	protected bool OutputFull()
	{
		if (needEmptyOutputToWork)
		{
			return storedProducts.Count > 0;
		}
		return false;
	}

	protected virtual Ant SpawnProductAnt(AntCaste ant_caste, List<TrailGate_Link> link_gates)
	{
		Ant result;
		if (ant_caste == AntCaste.CARGO_TRAIN)
		{
			List<CargoAnt> list = new List<CargoAnt>();
			if (collectedCentipedeLength == 0)
			{
				collectedCentipedeLength = 1;
			}
			for (int i = 0; i < collectedCentipedeLength + 1; i++)
			{
				list.Add((CargoAnt)SpawnSingleAnt(ant_caste, link_gates));
			}
			for (int j = 0; j < list.Count; j++)
			{
				CargoAnt cargoAnt = list[j];
				CargoAnt leader = null;
				if (j == 0)
				{
					cargoAnt.SetTotalEnergy(cargoAnt.data.energy + cargoAnt.data.energyExtra * (float)list.Count);
				}
				else
				{
					leader = list[j - 1];
				}
				cargoAnt.SetCentipedeConnection(list[0], leader, (j == list.Count - 1) ? null : list[j + 1]);
			}
			result = list[0];
		}
		else
		{
			result = SpawnSingleAnt(ant_caste, link_gates);
		}
		collectedCentipedeLength = 0;
		return result;
	}

	private Ant SpawnSingleAnt(AntCaste ant_caste, List<TrailGate_Link> link_gates)
	{
		Ant ant = GameManager.instance.SpawnAnt(ant_caste, base.transform.position, base.transform.rotation);
		ant.SetMoveState(MoveState.Disabled);
		antsWaitingToExit.Add(ant);
		foreach (TrailGate_Link link_gate in link_gates)
		{
			link_gate.Assign(ant);
		}
		return ant;
	}

	protected override void SetBuildingTrailActionPoint(Trail _trail, ExchangeType _type)
	{
		base.SetBuildingTrailActionPoint(_trail, _type);
		if (_type == ExchangeType.EXIT)
		{
			productTrails.Add(_trail);
		}
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		if (!base.TryUseBuilding(_entrance, _ant))
		{
			return false;
		}
		if (antSlots > 0 && antsInside[_entrance] != null)
		{
			return false;
		}
		foreach (Trail productTrail in productTrails)
		{
			if (productTrail.currentAnts.Count > 0)
			{
				return false;
			}
		}
		return true;
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		if (storedRecipe != "")
		{
			Dictionary<AntCaste, int> dictionary = antsInside.ToDictionary();
			Dictionary<AntCaste, int> dictionary2 = FactoryRecipeData.Get(storedRecipe).costsAnt.ToDictionary();
			if (!dictionary2.ContainsKey(_ant.caste) || (dictionary.ContainsKey(_ant.caste) && dictionary[_ant.caste] >= dictionary2[_ant.caste]))
			{
				Debug.LogWarning("Launching invalid ant");
				Vector3 force = Vector3.Lerp(-_ant.transform.forward, _ant.transform.up, 0.5f) * 75f;
				_ant.StartLaunch(force, LaunchCause.NONE);
				ant_entered = false;
				return 0f;
			}
		}
		if (_ant.caste == AntCaste.CARGO_TRAIN)
		{
			collectedCentipedeLength = 0;
			foreach (CargoAnt item in ((CargoAnt)_ant).EAllSubAnts())
			{
				_ = item;
				collectedCentipedeLength++;
			}
		}
		if (antSlots > 0)
		{
			antsInside[_entrance] = _ant;
		}
		else
		{
			antsInside.Add(_ant);
		}
		if (processingRecipe == null)
		{
			SetProcessingRecipe();
		}
		ant_entered = false;
		if (!showAntsInside)
		{
			if (_ant is CargoAnt cargoAnt)
			{
				foreach (CargoAnt item2 in cargoAnt.EAllSubAnts())
				{
					item2.SetCurrentTrail(null);
					_ant.SetMoveState(AntsDieInside() ? MoveState.DeadAndDisabled : MoveState.Disabled);
				}
			}
			else
			{
				_ant.SetCurrentTrail(null);
				_ant.SetMoveState(AntsDieInside() ? MoveState.DeadAndDisabled : MoveState.Disabled);
			}
			if (AntsDieInside())
			{
				GameManager.instance.UpdateAntCount();
			}
			ant_entered = true;
		}
		else
		{
			_ant.SetCurrentTrail(null);
			_ant.SetMoveState(MoveState.Waiting);
		}
		ParentAntIfNeeded(_ant);
		return 0f;
	}

	private void ParentAntIfNeeded(Ant ant)
	{
		if (showAntsInside && antsParent != null)
		{
			ant.transform.parent = antsParent;
			ant.SetColliders(target: false);
			if ((ant.transform.position - antsParent.position).sqrMagnitude > 100f)
			{
				ant.transform.localPosition = Vector3.zero;
			}
		}
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (ant.GetCarryingPickupsCount() > 0)
		{
			warning = "ANT_CANT_ENTER_CARRY";
			return false;
		}
		if (antsWaitingToMove.Count > 0)
		{
			return false;
		}
		if (antSlots > 0)
		{
			int index = ((!(trail == listSpawnedTrails[0][0])) ? ((trail == listSpawnedTrails[1][0]) ? 1 : 0) : 0);
			if (antsInside[index] != null)
			{
				return false;
			}
			if (AnyAntsOnBuildingTrails(trail))
			{
				return false;
			}
		}
		if (storedRecipe != "" && !FactoryRecipeData.Get(storedRecipe).costsAnt.ToDictionary().ContainsKey(ant.caste))
		{
			warning = "ANT_CANT_ENTER_WRONG_CASTE";
			return false;
		}
		List<AntCaste> list = new List<AntCaste>();
		foreach (Ant item in antsInside)
		{
			if (item != null)
			{
				list.Add(item.caste);
			}
		}
		List<Ant> list2 = new List<Ant>();
		foreach (Trail gateTrail in gateTrails)
		{
			foreach (Ant item2 in EAntsOnBuildingTrails(gateTrail))
			{
				if (!list2.Contains(item2))
				{
					list2.Add(item2);
				}
			}
		}
		foreach (Ant item3 in list2)
		{
			list.Add(item3.caste);
		}
		list.Add(ant.caste);
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(include_incoming: true);
		Dictionary<PickupType, int> dicAvailablePickups = GetDicAvailablePickups(include_incoming: true);
		return GetPotentialRecipes(dicCollectedPickups.AddDictionary(dicAvailablePickups), list, dicAvailablePickups.ToList()).Count > 0;
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetRecipe(this, showRecipeIngredients);
		if (antSlots > 0)
		{
			ui_hover.SetSlots();
		}
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		ui_hover.UpdateRecipe(this);
		if (antSlots <= 0)
		{
			return;
		}
		List<string> list = new List<string>();
		if (antSlots == 2)
		{
			list.Add(Loc.GetUI("BUILDING_COMBINER_LEFT"));
			list.Add(Loc.GetUI("BUILDING_COMBINER_RIGHT"));
		}
		List<AntCaste> list2 = new List<AntCaste>();
		foreach (Ant item in antsInside)
		{
			if (item == null)
			{
				list2.Add(AntCaste.NONE);
			}
			else
			{
				list2.Add(item.caste);
			}
		}
		ui_hover.UpdateSlots("", antSlots, list2, list);
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.FACTORY;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		((UIClickLayout_Factory)ui_building).SetRecipe(this, showRecipeIngredients, allowChangeRecipe);
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		((UIClickLayout_Factory)ui_click).UpdateRecipe(this, allowChangeRecipe);
	}

	protected override bool HasHologram()
	{
		return true;
	}

	public override HologramShape GetHologramShape(out PickupType _pickup, out AntCaste _ant)
	{
		_pickup = PickupType.NONE;
		_ant = AntCaste.NONE;
		if (GetCurrentBillboard(out var _, out var _, out var _, out var _) != BillboardType.NONE)
		{
			return HologramShape.None;
		}
		FactoryRecipeData factoryRecipeData = (isProcessing ? processingRecipe : FactoryRecipeData.Get(GetStoredRecipe()));
		if (factoryRecipeData != null)
		{
			if (factoryRecipeData.productAnts.Count > 0)
			{
				_ant = factoryRecipeData.productAnts[0].type;
				return HologramShape.Ant;
			}
			if (factoryRecipeData.productPickups.Count > 0)
			{
				_pickup = factoryRecipeData.productPickups[0].type;
				return HologramShape.Pickup;
			}
			return base.GetHologramShape(out _pickup, out _ant);
		}
		return HologramShape.QuestionMark;
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (requiresDispenserPickup != PickupType.NONE)
		{
			foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
			{
				if (buildingAttachPoint.type != AttachType.DISPENSER)
				{
					continue;
				}
				if (!buildingAttachPoint.HasDispenser(out var dis))
				{
					switch (requiresDispenserPickup)
					{
					case PickupType.ANY:
						code_desc = "BUILDING_NEEDSDISPENSER";
						break;
					case PickupType.ACID:
						code_desc = "BUILDING_NEEDSDISPENSER_ACID";
						break;
					default:
						code_desc = "BUILDING_NEEDSDISPENSER";
						Debug.LogWarning("Don't know how to handle pickuptype " + requiresDispenserPickup);
						break;
					}
					col = Color.yellow;
					parent = dispenserBillboardParent;
					return BillboardType.ARROW;
				}
				if (requiresDispenserPickup != PickupType.ANY && !dis.GetAllowedPickups().Contains(requiresDispenserPickup))
				{
					if (requiresDispenserPickup == PickupType.ACID)
					{
						code_desc = "BUILDING_WRONGDISPENSER_ACID";
					}
					else
					{
						code_desc = "???";
						Debug.LogWarning("Don't know how to handle pickuptype " + requiresDispenserPickup);
					}
					col = Color.red;
					parent = dispenserBillboardParent;
					return BillboardType.CROSS_SMALL;
				}
			}
		}
		if (playerMustChooseRecipe)
		{
			code_desc = "BUILDING_CHOOSEPRODUCTION";
			col = Color.yellow;
			return BillboardType.QUESTION;
		}
		if (noIngredientsInAntFactory)
		{
			code_desc = "BUILDING_FACTORY_NOINGREDIENTSFORANT";
			col = Color.yellow;
			return BillboardType.EXCLAMATION;
		}
		if (antsWaitingToMove.Count > 0 && !SpawnedAntCanMove())
		{
			code_desc = "BUILDING_FACTORYNEEDSCONNECTION";
			col = Color.yellow;
			return BillboardType.INVISIBLE;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	public override bool CanCopySettings()
	{
		return true;
	}

	public void GatherRecipeProgress(UIIconItem icon_item, bool show_ingredients, out List<(AntCaste, string)> ant_icons, out List<(PickupType, string)> pickup_icons, out string text, out string status, out string progress_text, out float progress_value)
	{
		string text2 = GetProcessingRecipe();
		Dictionary<AntCaste, int> collectedAntCastes_dic = GetCollectedAntCastes_dic(include_incoming: false);
		Dictionary<PickupType, int> dictionary = GetDicCollectedPickups(include_incoming: false).AddDictionary(GetDicAvailablePickups(include_incoming: false));
		bool flag = storedProducts.Count > 0;
		progress_value = 0f;
		ant_icons = null;
		pickup_icons = null;
		if (text2 == "")
		{
			text = Loc.GetUI("BUILDING_NORECIPE");
			icon_item.Init(PickupType.NONE);
			if (show_ingredients)
			{
				ant_icons = new List<(AntCaste, string)>();
				if (collectedAntCastes_dic != null)
				{
					foreach (KeyValuePair<AntCaste, int> item in collectedAntCastes_dic)
					{
						ant_icons.Add((item.Key, item.Value.ToString()));
					}
				}
				ant_icons = new List<(AntCaste, string)>();
				if (dictionary != null)
				{
					pickup_icons = new List<(PickupType, string)>();
					foreach (KeyValuePair<PickupType, int> item2 in dictionary)
					{
						pickup_icons.Add((item2.Key, item2.Value.ToString()));
					}
				}
			}
			if (flag)
			{
				status = Loc.GetUI("BUILDING_WAITINGFORPICKUP");
			}
			else
			{
				status = Loc.GetUI("BUILDING_DECIDEONEARRIVAL");
			}
			progress_text = "";
			return;
		}
		FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(text2);
		text = factoryRecipeData.GetTitle();
		icon_item.Init(factoryRecipeData);
		if (show_ingredients)
		{
			ant_icons = new List<(AntCaste, string)>();
			if (factoryRecipeData.costsAnt.Count() > 0)
			{
				foreach (AntCasteAmount item3 in factoryRecipeData.costsAnt)
				{
					if (!collectedAntCastes_dic.TryGetValue(item3.type, out var value))
					{
						value = 0;
					}
					ant_icons.Add((item3.type, $"{value} / {item3.intValue}"));
				}
			}
			pickup_icons = new List<(PickupType, string)>();
			Dictionary<PickupType, int> dictionary2 = factoryRecipeData.costsPickup.ToDictionary();
			foreach (KeyValuePair<PickupType, int> item4 in dictionary2)
			{
				PickupType key = item4.Key;
				if (!dictionary.TryGetValue(key, out var value2))
				{
					value2 = 0;
				}
				pickup_icons.Add((key, $"{value2} / {dictionary2[key]}"));
			}
		}
		if (processTime == 0f)
		{
			if (flag)
			{
				status = Loc.GetUI("BUILDING_WAITINGFORPICKUP");
			}
			else
			{
				status = Loc.GetUI("BUILDING_WAITINGFORINGREDIENTS");
			}
			progress_text = "";
		}
		else
		{
			status = "";
			float num = Mathf.Clamp(processTime, 0f, factoryRecipeData.processTime);
			progress_text = (factoryRecipeData.processTime - num).Unit(PhysUnit.TIME_MINUTES);
			progress_value = (factoryRecipeData.processTime - num) / factoryRecipeData.processTime;
		}
	}

	public override bool CanTrack()
	{
		return true;
	}

	public override int GetCounterAntCount(int entrance)
	{
		int num = 0;
		if (antSlots == 0)
		{
			num += antsInside.Count;
		}
		else if (antsInside[entrance] != null)
		{
			num++;
		}
		return num + antsWaitingToExit.Count;
	}
}
