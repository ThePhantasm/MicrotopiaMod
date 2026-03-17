using System.Collections.Generic;
using UnityEngine;

public class Stockpile : Storage
{
	[Header("Stockpile")]
	[Tooltip("Lower number is higher priority for pickups to travel to this storage if transferred to inventory")]
	public int inventoryPriority;

	[SerializeField]
	protected List<PickupState> allowedStates = new List<PickupState>();

	public bool crossIsland;

	[Tooltip("Allow smart dispensers to take from here (can be toggled in-game)")]
	[SerializeField]
	private bool openToSmart = true;

	private bool openToSmart_local;

	[SerializeField]
	private bool updateTopPoint = true;

	[HideInInspector]
	public List<PickupType> allowedPickupTypes = new List<PickupType>();

	private List<PickupType> allowedPickupTypes_draft = new List<PickupType>();

	private bool changeAllowedPickupTypes = true;

	protected PickupState triedInsert;

	private bool triedInsertWhileFull;

	private Transform topPointCopy;

	public override void Write(Save save)
	{
		base.Write(save);
		WriteConfig(save);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		ReadConfig(save);
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write(allowedPickupTypes.Count);
		foreach (PickupType allowedPickupType in allowedPickupTypes)
		{
			save.Write((int)allowedPickupType);
		}
		save.Write(openToSmart_local);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		int num = save.ReadInt();
		allowedPickupTypes.Clear();
		for (int i = 0; i < num; i++)
		{
			allowedPickupTypes.Add((PickupType)save.ReadInt());
		}
		allowedPickupTypes_draft = new List<PickupType>(allowedPickupTypes);
		if (save.GetVersion() >= 75)
		{
			openToSmart_local = save.ReadBool();
		}
		else
		{
			openToSmart_local = openToSmart;
		}
		if (save.GetVersion() < 56)
		{
			save.ReadBool();
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (!during_load)
		{
			openToSmart_local = openToSmart;
		}
		UpdateTopPoint();
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (changeAllowedPickupTypes)
		{
			allowedPickupTypes.Clear();
			if (allowedPickupTypes_draft.Count > 0)
			{
				allowedPickupTypes.AddRange(allowedPickupTypes_draft);
			}
			else
			{
				allowedPickupTypes.Add(PickupType.NONE);
			}
			if (allowedPickupTypes.Contains(PickupType.ANY) && allowedPickupTypes.Count > 1)
			{
				allowedPickupTypes = new List<PickupType> { PickupType.ANY };
			}
			CheckPickupsInside();
			changeAllowedPickupTypes = false;
		}
	}

	public override void OnConfigPaste()
	{
		base.OnConfigPaste();
		CheckPickupsInside();
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (is_selected)
		{
			UpdateTopPoint();
			{
				foreach (ClickableObject item in EObjectsAssignedToThis())
				{
					item.ShowAssignLine(this);
				}
				return;
			}
		}
		foreach (ClickableObject item2 in EObjectsAssignedToThis())
		{
			item2.HideAssignLines();
		}
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN)
		{
			return false;
		}
		ClearBillboard();
		UpdateBillboard(cancel_temporary: true);
		if (!allowedPickupTypes.Contains(_type) && !allowedPickupTypes.Contains(PickupType.ANY) && !allowedPickupTypes.Contains(PickupType.NONE))
		{
			return false;
		}
		triedInsertWhileFull = false;
		triedInsert = PickupState.NONE;
		PickupState state = PickupData.Get(_type).state;
		if (!allowedStates.Contains(state))
		{
			if (show_billboard)
			{
				triedInsert = state;
				UpdateBillboardTempory();
			}
			return false;
		}
		if (data.storageCapacity < 0 && !HasPiles())
		{
			return true;
		}
		if (exchange == ExchangeType.BUILDING_IN)
		{
			if (HasSpaceLeft(_type, PileType.NONE, point, out var _))
			{
				return true;
			}
			if (show_billboard)
			{
				triedInsertWhileFull = true;
				UpdateBillboardTempory();
			}
		}
		return false;
	}

	protected override void PrepareForPickup_Intake(Pickup p, ExchangePoint _point)
	{
		base.PrepareForPickup_Intake(p, _point);
		if (allowedPickupTypes.Contains(PickupType.NONE))
		{
			allowedPickupTypes = new List<PickupType> { p.data.type };
			allowedPickupTypes_draft = new List<PickupType> { p.data.type };
		}
	}

	protected override void OnPickupArrival_Intake(Pickup p, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(p, point);
		if (!allowedPickupTypes.Contains(p.type) && !allowedPickupTypes.Contains(PickupType.ANY))
		{
			DropPickups(p.type);
		}
		Progress.SetCollected(p.type);
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		Pickup result = base.ExtractPickup(_type);
		UpdateTopPoint();
		return result;
	}

	private void CheckPickupsInside()
	{
		foreach (KeyValuePair<PickupType, int> dicCollectedPickup in GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false))
		{
			if (!allowedPickupTypes.Contains(dicCollectedPickup.Key) && dicCollectedPickup.Value > 0 && !allowedPickupTypes.Contains(PickupType.ANY))
			{
				DropPickups(dicCollectedPickup.Key, dicCollectedPickup.Value, try_inventory: true);
			}
		}
	}

	public override bool HasSpaceLeft(PickupType pickup_type, PileType pile_type, ExchangePoint point, out int n)
	{
		if (data.storageCapacity - GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true) <= 0)
		{
			n = 0;
			return false;
		}
		return base.HasSpaceLeft(pickup_type, pile_type, point, out n);
	}

	public bool IsEmpty()
	{
		return GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: true) == 0;
	}

	public override bool CanDispense()
	{
		return true;
	}

	public override IEnumerable<ClickableObject> EObjectsAssignedToThis()
	{
		foreach (Catapult item in GameManager.instance.ECatapults())
		{
			foreach (ClickableObject item2 in item.EAssignedObjects())
			{
				if (item2 == this)
				{
					yield return item;
					break;
				}
			}
		}
		foreach (DispenserRegular item3 in GameManager.instance.EDispensers())
		{
			foreach (ClickableObject item4 in item3.EAssignedObjects())
			{
				if (item4 == this)
				{
					yield return item3;
					break;
				}
			}
		}
		foreach (TrailGate item5 in GameManager.instance.ETrailGates())
		{
			foreach (ClickableObject item6 in item5.EAssignedObjects())
			{
				if (item6 == this)
				{
					yield return item5;
					break;
				}
			}
		}
	}

	public Dictionary<PickupType, int> GetDicExtractables()
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		foreach (PickupType allowedPickupType in allowedPickupTypes)
		{
			dictionary.Add(allowedPickupType, GetCollectedAmount(allowedPickupType, BuildingStatus.COMPLETED, include_incoming: false));
		}
		return dictionary;
	}

	public void StartSendToOtherStockpile()
	{
		Gameplay.instance.StartAssign(this, AssignType.SEND);
	}

	public override bool CanAssignTo(ClickableObject target, out string error)
	{
		if (target is Stockpile stockpile && stockpile != this)
		{
			error = "";
			if (ground != stockpile.ground)
			{
				error = "STOCKPILE_SEND_OTHER_ISLAND";
			}
			List<PickupType> collectedPickupsList = GetCollectedPickupsList(BuildingStatus.COMPLETED, include_incoming: false);
			if (error == "")
			{
				foreach (PickupType item in collectedPickupsList)
				{
					if (!stockpile.allowedStates.Contains(PickupData.Get(item).state))
					{
						error = "STOCKPILE_SEND_WRONG_FILTER";
						break;
					}
				}
			}
			if (error == "" && stockpile.allowedPickupTypes.Count > 0 && !stockpile.allowedPickupTypes.Contains(PickupType.ANY) && !stockpile.allowedPickupTypes.Contains(PickupType.NONE))
			{
				foreach (PickupType item2 in collectedPickupsList)
				{
					if (!stockpile.allowedPickupTypes.Contains(item2))
					{
						error = "STOCKPILE_SEND_WRONG_FILTER";
						break;
					}
				}
			}
			if (error == "")
			{
				bool let_ant_wait = false;
				foreach (PickupType item3 in collectedPickupsList)
				{
					if (!stockpile.CanInsert(item3, ExchangeType.BUILDING_IN, null, ref let_ant_wait))
					{
						error = "STOCKPILE_SEND_NO_SPACE";
						break;
					}
				}
			}
			return true;
		}
		return base.CanAssignTo(target, out error);
	}

	public override void Assign(ClickableObject target, bool add = true)
	{
		if (!(target is Stockpile stockpile))
		{
			return;
		}
		bool let_ant_wait = false;
		foreach (KeyValuePair<PickupType, int> dicCollectedPickup in GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false))
		{
			for (int i = 0; i < dicCollectedPickup.Value; i++)
			{
				if (!stockpile.CanInsert(dicCollectedPickup.Key, ExchangeType.BUILDING_IN, null, ref let_ant_wait))
				{
					break;
				}
				Pickup pickup = ExtractPickup(dicCollectedPickup.Key);
				pickup.Exchange(stockpile, stockpile.GetInsertPos(pickup), ExchangeAnimationType.SHOOT, Random.Range(0f, 0.5f));
			}
		}
	}

	public override AssignType GetAssignType()
	{
		return AssignType.SEND;
	}

	public override void SetAssignLine(bool show)
	{
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetInfo();
		ui_hover.SetIcons(Loc.GetUI("BUILDING_ACCEPTS"), allowedPickupTypes_draft, allowedStates, delegate
		{
			changeAllowedPickupTypes = true;
		});
		ui_hover.SetButtonWithText(delegate
		{
			PlaceDispenser();
		}, clear_on_click: true, Loc.GetUI("BUILDING_PLACEDISPENSER"));
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		ui_hover.UpdateInfo(CapacityInfo());
		ui_hover.UpdateIcons(allowedPickupTypes);
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.STOCKPILE;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		UIClickLayout_Stockpile obj = (UIClickLayout_Stockpile)ui_building;
		obj.SetIcons(Loc.GetUI("BUILDING_ACCEPTS"), allowedPickupTypes_draft, allowedStates, delegate
		{
			changeAllowedPickupTypes = true;
		});
		obj.SetButton(UIClickButtonType.PlaceDispenser, delegate
		{
			PlaceDispenser();
		}, InputAction.PlaceDispenser);
		obj.UpdateButton(UIClickButtonType.PlaceDispenser, enabled: true, Loc.GetUI("BUILDING_PLACEDISPENSER"));
		obj.SetButton(UIClickButtonType.Send, StartSendToOtherStockpile, InputAction.InteractBuilding);
		obj.SetButtonHover(UIClickButtonType.Send, "STOCKPILE_SEND_TO_OTHER");
		obj.SetCheckbox(0, openToSmart_local, delegate(bool is_on)
		{
			openToSmart_local = is_on;
		});
		obj.UpdateCheckbox(0, Progress.HasUnlockedBuilding("DISPENSER_SMART"), Loc.GetUI("STOCKPILE_SMART_CAN_TAKE"));
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		UIClickLayout_Stockpile obj = (UIClickLayout_Stockpile)ui_click;
		obj.SetInfo(CapacityInfo());
		obj.UpdateIcons(allowedPickupTypes);
		obj.UpdateButton(UIClickButtonType.Send, CanSend(), Loc.GetUI("STOCKPILE_SEND"));
	}

	protected virtual string CapacityInfo()
	{
		return Loc.GetUI("BUILDING_STOCKPILE_CAPACITY", data.storageCapacity.ToString());
	}

	public bool CanSend()
	{
		return GetCollectedAmount(PickupType.ANY, BuildingStatus.COMPLETED, include_incoming: false) > 0;
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
		if (allowedPickupTypes.Count == 1 && allowedPickupTypes[0] != PickupType.NONE)
		{
			_pickup = allowedPickupTypes[0];
			return HologramShape.Pickup;
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
		switch (triedInsert)
		{
		case PickupState.LIVING:
			code_desc = "BUILDING_STOCKPILE_LIVING";
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		case PickupState.LIQUID:
			code_desc = "BUILDING_STOCKPILE_FLUID";
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		default:
			if (triedInsertWhileFull)
			{
				code_desc = "BUILDING_STOCKPILE_FULL";
				col = Color.yellow;
				return BillboardType.EXCLAMATION_SMALL;
			}
			code_desc = "";
			col = Color.white;
			return BillboardType.NONE;
		}
	}

	protected override void ClearBillboard()
	{
		triedInsert = PickupState.NONE;
		triedInsertWhileFull = false;
	}

	protected virtual void UpdateTopPoint()
	{
		if (!updateTopPoint)
		{
			return;
		}
		if (topPointCopy == null)
		{
			topPointCopy = new GameObject().transform;
			topPointCopy.parent = topPoint.parent;
			topPointCopy.SetPositionAndRotation(topPoint.position, topPoint.rotation);
		}
		if (allowedPickupTypes.Count == 0 || allowedPickupTypes.Contains(PickupType.NONE))
		{
			topPoint.position = topPointCopy.position;
			return;
		}
		int num = 0;
		foreach (Pile pile in piles)
		{
			if (pile.GetHeight() > num)
			{
				num = pile.GetHeight();
			}
		}
		Vector3 position = topPointCopy.position;
		position.y += PickupData.Get(allowedPickupTypes[0]).GetHeight() * (float)num;
		topPoint.position = position;
	}

	public bool OpenToSmartDispensers()
	{
		return openToSmart_local;
	}

	public override bool CanCopySettings()
	{
		return true;
	}
}
