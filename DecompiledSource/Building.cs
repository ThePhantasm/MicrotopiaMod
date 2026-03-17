using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : PickupContainer
{
	private struct Relocator
	{
		private Vector3 posOrig;

		private Vector3 posNew;

		private Quaternion dRot;

		public Relocator(Vector3 building_orig, Vector3 building_new, Quaternion rot_orig, Quaternion rot_new)
		{
			posOrig = building_orig;
			posNew = building_new;
			dRot = Quaternion.Inverse(rot_orig) * rot_new;
		}

		public Vector3 NewPos(Vector3 pos)
		{
			return posNew + dRot * (pos - posOrig);
		}
	}

	[Header("Building")]
	public GameObject meshBase;

	public GameObject hoverColliderParent;

	public Animator anim;

	public Material hoverMeshMat;

	public bool skipWinding;

	public ExchangePoint[] exchangePoints;

	public List<GameObject> enableOnInit = new List<GameObject>();

	public List<GameObject> enableOnComplete = new List<GameObject>();

	public Vector2 buildRange = Vector2.zero;

	public bool showInventory;

	public bool outdated;

	public List<BuildingTrail> buildingTrails = new List<BuildingTrail>();

	public List<BuildingAttachPoint> buildingAttachPoints = new List<BuildingAttachPoint>();

	[SerializeField]
	private ParticleSystem[] pausableParticleSystems;

	[NonSerialized]
	public BuildingData data;

	[NonSerialized]
	public BuildingStatus currentStatus;

	[NonSerialized]
	public Dictionary<PickupType, int> dicCollectedPickups_build = new Dictionary<PickupType, int>();

	[NonSerialized]
	public Dictionary<PickupType, int> dicCollectedPickups_intake = new Dictionary<PickupType, int>();

	protected List<Pickup> incomingPickups_build = new List<Pickup>();

	protected List<Pickup> incomingPickups_intake = new List<Pickup>();

	[NonSerialized]
	public bool countAsAnt;

	[NonSerialized]
	public Ground ground;

	protected List<PickupCost> costs;

	[NonSerialized]
	public HoverMesh hoverMesh;

	private float currentProgress;

	private float targetProgress;

	[NonSerialized]
	public GameObject meshBuild;

	private Material matBuild;

	private Material matOrig;

	[SerializeField]
	protected EffectArea effectArea;

	[SerializeField]
	protected TriggerArea triggerArea;

	public bool centerBetweenGridPoints;

	protected Building attachedTo;

	private List<int> attachmentsIds = new List<int>();

	protected List<List<Trail>> listSpawnedTrails = new List<List<Trail>>();

	protected List<Trail> enterTrails = new List<Trail>();

	protected List<Trail> gateTrails = new List<Trail>();

	private Coroutine cReconnectToTrails;

	protected bool paused;

	[NonSerialized]
	public Split exitSplit;

	private static int prevPlaceAudioFrame;

	private AudioChannel audioBuilding;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write((int)currentStatus);
		for (int i = 0; i < exchangePoints.Length; i++)
		{
			exchangePoints[i].Write(save);
		}
		save.Write(dicCollectedPickups_build.Count);
		foreach (KeyValuePair<PickupType, int> item in dicCollectedPickups_build)
		{
			save.Write((int)item.Key);
			save.Write(item.Value);
		}
		save.Write(dicCollectedPickups_intake.Count);
		foreach (KeyValuePair<PickupType, int> item2 in dicCollectedPickups_intake)
		{
			save.Write((int)item2.Key);
			save.Write(item2.Value);
		}
		save.Write(listSpawnedTrails.Count);
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			save.Write(listSpawnedTrail.Count);
			foreach (Trail item3 in listSpawnedTrail)
			{
				save.Write(item3.linkId);
			}
		}
		save.Write(paused);
		save.Write(buildingAttachPoints.Count);
		for (int j = 0; j < buildingAttachPoints.Count; j++)
		{
			if (buildingAttachPoints[j].HasAttachment(out var att))
			{
				save.Write(att.linkId);
			}
			else
			{
				save.Write(-1);
			}
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		currentStatus = (BuildingStatus)save.ReadInt();
		for (int i = 0; i < exchangePoints.Length; i++)
		{
			exchangePoints[i].Read(save);
		}
		int num = save.ReadInt();
		for (int j = 0; j < num; j++)
		{
			PickupType pickupType = (PickupType)save.ReadInt();
			int value = save.ReadInt();
			if (CheckPickupType(pickupType, dicCollectedPickups_build))
			{
				dicCollectedPickups_build.Add(pickupType, value);
			}
		}
		num = save.ReadInt();
		for (int k = 0; k < num; k++)
		{
			PickupType pickupType2 = (PickupType)save.ReadInt();
			int value2 = save.ReadInt();
			if (CheckPickupType(pickupType2, dicCollectedPickups_intake))
			{
				dicCollectedPickups_intake.Add(pickupType2, value2);
			}
		}
		num = save.ReadInt();
		for (int l = 0; l < num; l++)
		{
			listSpawnedTrails.Add(new List<Trail>());
			int num2 = save.ReadInt();
			for (int m = 0; m < num2; m++)
			{
				listSpawnedTrails[^1].Add(GameManager.instance.FindLink<Trail>(save.ReadInt()));
			}
		}
		paused = save.ReadBool();
		attachmentsIds.Clear();
		if (save.version < 7)
		{
			return;
		}
		int num3 = save.ReadInt();
		if (num3 != buildingAttachPoints.Count)
		{
			Debug.Log(data.title + " dispenser attach point count is different from previous save.");
		}
		for (int n = 0; n < buildingAttachPoints.Count; n++)
		{
			if (n < num3)
			{
				attachmentsIds.Add(save.ReadInt());
			}
			else
			{
				attachmentsIds.Add(-1);
			}
		}
		if (num3 > buildingAttachPoints.Count)
		{
			int num4 = num3 - buildingAttachPoints.Count;
			for (int num5 = 0; num5 < num4; num5++)
			{
				save.ReadInt();
			}
		}
	}

	public void Write(BlueprintData data)
	{
		data.Write(buildingAttachPoints.Count);
		for (int i = 0; i < buildingAttachPoints.Count; i++)
		{
			buildingAttachPoints[i].HasAttachment(out var att);
			data.Write(att);
		}
		WriteConfig(data);
	}

	public void Read(BlueprintData data)
	{
		int num = data.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Building building = data.ReadBuilding().building;
			if (building != null && i < buildingAttachPoints.Count)
			{
				building.transform.SetPositionAndRotation(buildingAttachPoints[i].GetPosition(), buildingAttachPoints[i].GetRotation());
				SetAttachment(building, buildingAttachPoints[i]);
			}
		}
		ReadConfig(data);
	}

	public virtual void WriteConfig(ISaveContainer save)
	{
	}

	public virtual void ReadConfig(ISaveContainer save)
	{
	}

	private bool CheckPickupType(PickupType _type, Dictionary<PickupType, int> _dic)
	{
		if (_type == PickupType.NONE)
		{
			Debug.LogWarning(base.name + " read: Tried to add PickupType NONE, shouldn't happen");
			return false;
		}
		if (_dic.ContainsKey(_type))
		{
			Debug.LogWarning(base.name + " read: Tried to add PickupType " + _type.ToString() + " while already added, shouldn't happen");
			return false;
		}
		return true;
	}

	public void SetLinkIds(ref int id)
	{
		base.linkId = ++id;
		for (int i = 0; i < exchangePoints.Length; i++)
		{
			exchangePoints[i].linkId = ++id;
		}
	}

	public virtual void LoadLinkBuildings()
	{
		for (int i = 0; i < attachmentsIds.Count; i++)
		{
			int num = attachmentsIds[i];
			if (num != -1)
			{
				Building building = GameManager.instance.FindLink<Building>(num);
				building.transform.SetPositionAndRotation(buildingAttachPoints[i].GetPosition(), buildingAttachPoints[i].GetRotation());
				SetAttachment(building, buildingAttachPoints[i]);
			}
		}
	}

	public virtual void Fill(BuildingData building_data)
	{
		data = building_data;
		costs = data.baseCosts;
	}

	private void Awake()
	{
		if (effectArea.statusEffect == StatusEffect.NONE)
		{
			effectArea = null;
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		GameManager.instance.AddBuilding(this);
		if (!during_load)
		{
			SetStatus(BuildingStatus.HOVERING);
		}
		else
		{
			SetStatus(currentStatus, during_load_or_preplaced: true);
			switch (currentStatus)
			{
			case BuildingStatus.BUILDING:
				ground = Toolkit.GetGround(base.transform.position);
				if (ground != null)
				{
					ground.AddBuilding(this);
				}
				InitBuildProgress(during_load: true);
				currentProgress = targetProgress - 0.0001f;
				break;
			case BuildingStatus.COMPLETED:
				ground = Toolkit.GetGround(base.transform.position);
				if (ground != null)
				{
					ground.AddBuilding(this);
				}
				break;
			}
			FindExitSplit();
			FindEnterTrails();
			FindGateTrails();
		}
		for (int i = 0; i < exchangePoints.Length; i++)
		{
			exchangePoints[i].SetOwner(this);
		}
		if (hoverColliderParent != null)
		{
			Collider[] componentsInChildren = hoverColliderParent.GetComponentsInChildren<Collider>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].enabled = false;
			}
		}
		foreach (GameObject item in enableOnInit)
		{
			item.SetObActive(active: true);
		}
		UpdateBillboard();
		if (anim != null)
		{
			anim.SetBool(ClickableObject.paramSkipWinding, skipWinding);
		}
	}

	public virtual void BuildingUpdate(float dt, bool runWorld)
	{
		if (currentStatus == BuildingStatus.BUILDING)
		{
			UpdateBuildProgress();
		}
		if (showingAssignLines)
		{
			UpdateAssignLines();
		}
		UpdateHologram();
	}

	public virtual void BuildingFixedUpdate(float xdt, bool runWorld)
	{
	}

	public void SetStatus(BuildingStatus _status, bool during_load_or_preplaced = false)
	{
		currentStatus = _status;
		for (int i = 0; i < exchangePoints.Length; i++)
		{
			exchangePoints[i].SetStatus(_status);
		}
		SetHoverMode(_status == BuildingStatus.HOVERING || _status == BuildingStatus.ROTATING || _status == BuildingStatus.RELOCATE_HOVERING || _status == BuildingStatus.RELOCATE_ROTATING || _status == BuildingStatus.DRAG_PLACING);
		if (!during_load_or_preplaced && _status == BuildingStatus.COMPLETED && costs.Count > 0 && !DebugSettings.standard.UnlockEverything() && Time.frameCount != prevPlaceAudioFrame)
		{
			AudioManager.PlayUI(base.transform.position, UISfx3D.BuildingComplete);
			prevPlaceAudioFrame = Time.frameCount;
		}
		if (_status == BuildingStatus.COMPLETED)
		{
			if (!during_load_or_preplaced)
			{
				OnComplete();
			}
			if (data.pollution > 0f)
			{
				ground.AddPollution(data.pollution);
			}
		}
		foreach (GameObject item in enableOnComplete)
		{
			item.SetObActive(_status == BuildingStatus.COMPLETED);
		}
		if (effectArea != null)
		{
			effectArea.SetActive(_status == BuildingStatus.COMPLETED, base.transform.position);
		}
		if (triggerArea != null)
		{
			triggerArea.SetObActive(_status == BuildingStatus.COMPLETED);
		}
	}

	protected virtual void OnComplete()
	{
		CheckAssigned();
		Gameplay.DoRefreshUnlocks();
		Gameplay.instance.ResetUIClickableObject(this);
		UpdateBillboard();
	}

	public void Refresh()
	{
		ExchangePoint[] array = exchangePoints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Refresh();
		}
	}

	public virtual bool NeedsFixedUpdate()
	{
		return false;
	}

	public override Vector3 GetInsertPos(Pickup pickup = null)
	{
		if (currentStatus == BuildingStatus.COMPLETED)
		{
			return base.GetInsertPos();
		}
		return base.transform.position;
	}

	public static void DemolishBuildings(List<Building> buildings, int warning_msg = 0)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < buildings.Count; i++)
		{
			if (!buildings[i].DemolishWarning(out var msg))
			{
				continue;
			}
			num++;
			if (warning_msg < num)
			{
				UIDialogBase uIDialogBase = UIBase.Spawn<UIDialogBase>();
				uIDialogBase.SetText(Loc.GetUI("BUILDING_DEMOLISH_SURE", buildings[i].data.GetTitle()) + "\n\n" + msg);
				uIDialogBase.SetAction(DialogResult.YES, delegate
				{
					DemolishBuildings(buildings, warning_msg + 1);
				});
				uIDialogBase.SetAction(DialogResult.NO, uIDialogBase.StartClose);
				return;
			}
		}
		buildings.Reverse();
		for (int num3 = buildings.Count - 1; num3 >= 0; num3--)
		{
			Building building = buildings[num3];
			if (!building.CanDemolish())
			{
				buildings.RemoveAt(num3);
			}
			else
			{
				List<Ground> list = new List<Ground> { building.ground };
				if (building is Bridge bridge)
				{
					Ground otherGround = bridge.GetOtherGround();
					if (otherGround != building.ground)
					{
						list.Add(otherGround);
					}
				}
				if (Player.crossIslandBuilding)
				{
					foreach (Ground item in GameManager.instance.EGrounds())
					{
						if (!list.Contains(item))
						{
							list.Add(item);
						}
					}
				}
				bool flag = true;
				foreach (KeyValuePair<PickupType, int> item2 in building.GetDicCollectedPickups(BuildingStatus.BUILDING, include_incoming: true).AddDictionary(building.GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: true)))
				{
					int num4 = 0;
					foreach (Ground item3 in list)
					{
						num4 += item3.GetStockpileSpace(item2.Key);
					}
					if (num4 < item2.Value)
					{
						flag = false;
					}
				}
				if (flag)
				{
					buildings.RemoveAt(num3);
					building.Demolish();
					num2++;
				}
			}
		}
		if (num2 > 0)
		{
			AudioManager.PlayUI(CamController.GetListenerPos(), UISfx3D.BuildingDelete);
		}
		if (buildings.Count <= 0)
		{
			return;
		}
		string code = "BUILDING_DEMOLISH_NOSPACE";
		UIDialogBase uIDialogBase2 = UIBase.Spawn<UIDialogBase>();
		if (buildings.Count == 1)
		{
			uIDialogBase2.SetText(Loc.GetUI("BUILDING_DEMOLISH_SURE", buildings[0].data.GetTitle()) + "\n\n" + Loc.GetUI(code));
		}
		else
		{
			uIDialogBase2.SetText(Loc.GetUI("BUILDING_DEMOLISH_SURE_MULTIPLE") + "\n\n" + Loc.GetUI(code));
		}
		uIDialogBase2.SetAction(DialogResult.YES, delegate
		{
			for (int num5 = buildings.Count - 1; num5 >= 0; num5--)
			{
				buildings[num5].Demolish();
			}
			AudioManager.PlayUI(CamController.GetListenerPos(), UISfx3D.BuildingDelete);
		});
		uIDialogBase2.SetAction(DialogResult.NO, uIDialogBase2.StartClose);
	}

	public override void OnClickDelete()
	{
		DemolishBuildings(new List<Building> { this });
	}

	public virtual void Demolish()
	{
		foreach (Pickup item in new List<Pickup>(incomingPickups_build))
		{
			AddPickup(item.type, BuildingStatus.BUILDING);
			item.Delete();
		}
		incomingPickups_build.Clear();
		foreach (Pickup item2 in new List<Pickup>(incomingPickups_intake))
		{
			AddPickup(item2.type, BuildingStatus.COMPLETED);
			item2.Delete();
		}
		incomingPickups_intake.Clear();
		foreach (KeyValuePair<PickupType, int> item3 in dicCollectedPickups_build.AddDictionary(dicCollectedPickups_intake))
		{
			for (int i = 0; i < item3.Value; i++)
			{
				DropPickupOnDemolish(item3.Key);
			}
		}
		if (!(this is Bridge))
		{
			foreach (Trail enterTrail in enterTrails)
			{
				foreach (Ant item4 in EAntsOnBuildingTrails(enterTrail))
				{
					DropAntOnGround(item4);
				}
			}
		}
		Delete();
	}

	protected virtual void DropPickupOnDemolish(PickupType pickup_type)
	{
		Pickup pickup = GameManager.instance.SpawnPickup(pickup_type);
		pickup.transform.SetPositionAndRotation(base.transform.position + new Vector3(UnityEngine.Random.insideUnitCircle.x * GetRadius(), 0f, UnityEngine.Random.insideUnitCircle.y * GetRadius()), Toolkit.RandomYRotation());
		pickup.SetStatus(PickupStatus.ON_GROUND);
		Storage exclude_stockpile = ((this is Storage storage) ? storage : null);
		List<Ground> list = new List<Ground> { ground };
		if (Player.crossIslandBuilding)
		{
			foreach (Ground item in GameManager.instance.EGrounds())
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		foreach (Ground item2 in list)
		{
			if (GameManager.instance.TryExchangePickupToInventory(item2, base.transform.position, pickup, exclude_stockpile))
			{
				break;
			}
		}
	}

	protected void DropAntOnGround(Ant _ant)
	{
		_ant.transform.SetPositionAndRotation(base.transform.position + new Vector3(UnityEngine.Random.insideUnitCircle.x * GetRadius(), 0f, UnityEngine.Random.insideUnitCircle.y * GetRadius()), Toolkit.RandomYRotation());
		_ant.transform.localScale = Vector3.one;
		_ant.SetColliders(target: true);
		_ant.SetMoveState(MoveState.Normal);
	}

	protected override void DoDelete()
	{
		ClearBuildingAudio();
		EraseCollectedPickups(BuildingStatus.BUILDING);
		EraseCollectedPickups(BuildingStatus.COMPLETED);
		for (int num = exchangePoints.Length - 1; num >= 0; num--)
		{
			exchangePoints[num].Delete();
		}
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			foreach (Trail item in new List<Trail>(listSpawnedTrail))
			{
				if (item != null)
				{
					item.Delete();
				}
			}
		}
		if (currentStatus == BuildingStatus.COMPLETED && data.pollution > 0f)
		{
			ground.AddPollution(0f - data.pollution);
		}
		ClearAttachments();
		ClearAttached();
		if (effectArea != null)
		{
			effectArea.SetActive(_active: false);
		}
		if (triggerArea != null)
		{
			triggerArea.SetObActive(active: false);
		}
		GameManager.instance.RemoveBuilding(this);
		GameManager.instance.RemoveBuildingBuilding(this);
		UpdateNavMesh();
		UnityEngine.Object.Destroy(base.gameObject);
		base.DoDelete();
	}

	public virtual bool DemolishWarning(out string msg)
	{
		msg = "";
		return false;
	}

	public virtual void PlaceBuilding()
	{
		SetVisibleHoverMesh(target: true);
		ground = Toolkit.GetGround(base.transform.position);
		if (ground == null)
		{
			Debug.LogError("PlaceBuilding: ground null");
		}
		else
		{
			ground.AddBuilding(this);
		}
		SetStatus(BuildingStatus.BUILDING);
		InitBuildProgress();
		PlaceBuildingTrails();
		HideAssignLines();
		UpdateBuildProgress();
		UpdateBillboard();
		UpdateNavMesh();
	}

	protected void UpdateNavMesh()
	{
		if (ground != null)
		{
			ground.UpdateNavMesh(base.transform.position, GetRadius());
		}
	}

	public void PlacePreplacedBuilding()
	{
		ground = Toolkit.GetGround(base.transform.position);
		if (ground == null)
		{
			Debug.LogError("PlaceBuilding: ground null");
		}
		else
		{
			ground.AddBuilding(this);
		}
		SetStatus(BuildingStatus.COMPLETED, during_load_or_preplaced: true);
		PlaceBuildingTrails();
		UpdateBillboard();
	}

	public void RelocateHover()
	{
		SetStatus(BuildingStatus.RELOCATE_HOVERING);
		ClickableObject[] componentsInChildren = GetComponentsInChildren<ClickableObject>();
		foreach (ClickableObject clickableObject in componentsInChildren)
		{
			if (clickableObject == this)
			{
				continue;
			}
			if (!(clickableObject is Pickup) && !(clickableObject is Ant))
			{
				if (!(clickableObject is ExchangePoint) && !(clickableObject is Dispenser))
				{
					Debug.LogWarning("Building.RelocateHover: not sure what to do with sub object of type " + clickableObject.GetType().Name);
				}
			}
			else
			{
				UnityEngine.Object.Destroy(clickableObject.gameObject);
			}
		}
		base.enabled = false;
	}

	public virtual void Relocate(Vector3 pos, Quaternion rot)
	{
		UpdateNavMesh();
		Relocator relocator = new Relocator(base.transform.position, pos, base.transform.rotation, rot);
		base.transform.SetPositionAndRotation(pos, rot);
		ReconnectToTrails();
		if (effectArea != null)
		{
			effectArea.UpdatePos(pos);
		}
		foreach (Pickup item in incomingPickups_build)
		{
			ExchangeAnimation exchangeAnim = item.GetExchangeAnim();
			Vector3 vector = relocator.NewPos(exchangeAnim.posEnd) - exchangeAnim.posEnd;
			exchangeAnim.posStart += vector;
			exchangeAnim.posEnd += vector;
		}
		foreach (Pickup item2 in incomingPickups_intake)
		{
			ExchangeAnimation exchangeAnim2 = item2.GetExchangeAnim();
			Vector3 vector2 = relocator.NewPos(exchangeAnim2.posEnd) - exchangeAnim2.posEnd;
			exchangeAnim2.posStart += vector2;
			exchangeAnim2.posEnd += vector2;
		}
		List<Split> list = new List<Split>();
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			for (int i = -1; i < listSpawnedTrail.Count; i++)
			{
				Split split = ((i == -1) ? listSpawnedTrail[0].splitStart : listSpawnedTrail[i].splitEnd);
				List<Trail> list2 = new List<Trail>();
				foreach (Trail connectedTrail in split.connectedTrails)
				{
					if (!connectedTrail.trailType.IsBuildingTrail() && !FloorEditing.IsRelocatingTrail(connectedTrail))
					{
						list2.Add(connectedTrail);
					}
				}
				if (list2.Count > 0)
				{
					Split split_new = GameManager.instance.NewSplit(split.transform.position);
					for (int j = 0; j < list2.Count; j++)
					{
						list2[j].ReplaceSplit(split, split_new);
					}
				}
				if (!list.Contains(split))
				{
					list.Add(split);
					split.transform.position = relocator.NewPos(split.transform.position);
				}
			}
		}
		foreach (List<Trail> listSpawnedTrail2 in listSpawnedTrails)
		{
			foreach (Trail item3 in listSpawnedTrail2)
			{
				item3.SetStartEndPos(item3.splitStart.transform.position, item3.splitEnd.transform.position);
			}
		}
		UpdateNavMesh();
		CheckAssigned();
	}

	public override void CheckAssigned()
	{
		List<ClickableObject> list = new List<ClickableObject>();
		foreach (ClickableObject item in EAssignedObjects())
		{
			string error;
			if (this is Catapult)
			{
				if (Vector3.Distance(base.transform.position, item.transform.position) > AssigningMaxRange())
				{
					list.Add(item);
				}
			}
			else if (!CanAssignTo(item, out error))
			{
				list.Add(item);
			}
		}
		foreach (ClickableObject item2 in list)
		{
			Assign(item2, add: false);
		}
	}

	public void ReconnectToTrails()
	{
		if (cReconnectToTrails != null)
		{
			StopCoroutine(cReconnectToTrails);
		}
		cReconnectToTrails = StartCoroutine(CReconnectToTrails());
	}

	private IEnumerator CReconnectToTrails()
	{
		yield return new WaitForSeconds(0.1f);
		for (int i = 0; i < exchangePoints.Length; i++)
		{
			exchangePoints[i].ReconnectToTrails();
		}
		cReconnectToTrails = null;
	}

	public virtual void SetHoverMode(bool hover)
	{
		meshBase.SetObActive(!hover);
		if (hover && hoverMesh == null)
		{
			hoverMesh = HoverMesh.CreateFrom(meshBase, UseHoverCollider() ? hoverColliderParent : null, hoverMeshMat);
		}
		if (hoverMesh != null)
		{
			hoverMesh.SetObActive(hover);
		}
	}

	public bool IsHovering()
	{
		if (hoverMesh != null)
		{
			return hoverMesh.isActiveAndEnabled;
		}
		return false;
	}

	public void SetVisibleHoverMesh(bool target)
	{
		if (hoverMesh != null)
		{
			hoverMesh.SetVisible(target);
		}
		ExchangePoint[] array = exchangePoints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetObActive(attachedTo == null);
		}
	}

	public bool IsPlaced()
	{
		if (currentStatus != BuildingStatus.BUILDING)
		{
			return currentStatus == BuildingStatus.COMPLETED;
		}
		return true;
	}

	protected virtual bool UseHoverCollider()
	{
		return true;
	}

	public virtual bool CanBeBuildOnGround(Ground _ground, bool is_relocating, ref string error)
	{
		return true;
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (!is_selected || currentStatus != BuildingStatus.COMPLETED || data.tutorial == Tutorial.NONE)
		{
			return;
		}
		if (DebugSettings.standard.alwaysPopupTutorials)
		{
			UIGame.instance.SetTutorial(data.tutorial);
			return;
		}
		UIGame.instance.SetTutorial(data.tutorial, delegate
		{
			Gameplay.instance.Select(this);
		});
	}

	protected virtual List<BuildingTrail> GetBuildingTrails()
	{
		return buildingTrails;
	}

	protected virtual void PlaceBuildingTrails()
	{
		List<BuildingTrail> list = GetBuildingTrails();
		if (list.Count == 0)
		{
			return;
		}
		int num = 0;
		Dictionary<Transform, Split> dictionary = new Dictionary<Transform, Split>();
		foreach (BuildingTrail item in list)
		{
			if (item.splitPoints.Count < 2)
			{
				Debug.LogError(base.name + ": building trails not set up correctly");
				return;
			}
			List<Trail> list2 = new List<Trail>();
			for (int i = 0; i < item.splitPoints.Count - 1; i++)
			{
				TrailType type = TrailType.IN_BUILDING;
				foreach (BuildingSplitPointProperties splitProperty in item.splitProperties)
				{
					if (!(item.splitPoints[i] != splitProperty.point) && splitProperty.exchangeType == ExchangeType.GATE)
					{
						type = TrailType.IN_BUILDING_GATE;
					}
				}
				Trail trail = GameManager.instance.NewTrail_Building(type, item.invisible);
				trail.SetBuilding(this);
				list2.Add(trail);
				if (dictionary.ContainsKey(item.splitPoints[i]))
				{
					trail.SetSplitStart(dictionary[item.splitPoints[i]]);
				}
				else if (i == 0)
				{
					Split split = trail.NewStartSplit(item.splitPoints[i].position);
					split.SetInBuilding();
					dictionary.Add(item.splitPoints[i], split);
				}
				else
				{
					trail.SetSplitStart(list2[i - 1].splitEnd);
				}
				if (dictionary.ContainsKey(item.splitPoints[i + 1]))
				{
					trail.SetSplitEnd(dictionary[item.splitPoints[i + 1]]);
				}
				else
				{
					Split split2 = trail.NewEndSplit(item.splitPoints[i + 1].position);
					split2.SetInBuilding();
					dictionary.Add(item.splitPoints[i + 1], split2);
				}
				trail.PlaceTrail(TrailStatus.PLACED_IN_BUILDING);
			}
			foreach (BuildingSplitPointProperties splitProperty2 in item.splitProperties)
			{
				if (!item.splitPoints.Contains(splitProperty2.point))
				{
					continue;
				}
				int num2 = item.splitPoints.IndexOf(splitProperty2.point);
				Trail trail2;
				if (num2 < list2.Count)
				{
					trail2 = list2[num2];
					if (splitProperty2.interactable)
					{
						trail2.splitStart.SetInBuilding(target: false);
					}
				}
				else
				{
					trail2 = list2[num2 - 1];
					if (splitProperty2.interactable)
					{
						trail2.splitEnd.SetInBuilding(target: false);
					}
				}
				if (splitProperty2.exchangeType != ExchangeType.NONE)
				{
					if (splitProperty2.exchangeType == ExchangeType.ENTER)
					{
						trail2.entranceN = num;
						num++;
					}
					SetBuildingTrailActionPoint(trail2, splitProperty2.exchangeType);
				}
			}
			listSpawnedTrails.Add(list2);
		}
		FindEnterTrails();
		FindExitSplit();
		FindGateTrails();
	}

	protected virtual void SetBuildingTrailActionPoint(Trail _trail, ExchangeType _type)
	{
		if (_type != ExchangeType.ENTER && _type != ExchangeType.EXIT && _type != ExchangeType.GATE)
		{
			Debug.LogWarning("Exchange type " + _type.ToString() + " not yet supported for building trail");
		}
		else if (_type == ExchangeType.ENTER)
		{
			_trail.SetNearbyConnectables(new List<ConnectableObject> { this });
			UpdateNearbyActionPoints();
		}
	}

	protected void FindExitSplit()
	{
		exitSplit = null;
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			foreach (Trail item in listSpawnedTrail)
			{
				if (!item.splitEnd.IsInBuilding() && exitSplit == null)
				{
					exitSplit = item.splitEnd;
					exitSplit.SetBillboardListener(this);
				}
			}
		}
	}

	private void FindEnterTrails()
	{
		enterTrails = new List<Trail>();
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			foreach (Trail item in listSpawnedTrail)
			{
				if (!item.splitStart.IsInBuilding() && !enterTrails.Contains(item))
				{
					enterTrails.Add(item);
				}
			}
		}
	}

	private void FindGateTrails()
	{
		gateTrails = new List<Trail>();
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			foreach (Trail item in listSpawnedTrail)
			{
				if (item.IsGate() && !gateTrails.Contains(item))
				{
					gateTrails.Add(item);
				}
			}
		}
	}

	public virtual bool TryUseBuilding(int _entrance, Ant _ant)
	{
		if (_ant.GetCarryingPickupsCount() > 0)
		{
			if (_ant.CanDoExchange(this, ExchangeType.BUILDING_IN, null, out var _))
			{
				_ant.StartExchangePickup(this, ExchangeType.BUILDING_IN);
			}
			return false;
		}
		return true;
	}

	public virtual float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		History.RegisterAntEnd(_ant, repurposed: true);
		if (_ant is CargoAnt cargoAnt)
		{
			foreach (CargoAnt item in cargoAnt.EAllSubAnts())
			{
				item.DeleteCarryingPickups();
				item.Delete();
			}
		}
		else
		{
			_ant.DeleteCarryingPickups();
			_ant.Delete();
		}
		ant_entered = true;
		return 0f;
	}

	public virtual bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		return true;
	}

	protected bool AnyAntsOnBuildingTrails(Trail trail_start)
	{
		using (IEnumerator<Ant> enumerator = EAntsOnBuildingTrails(trail_start).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				_ = enumerator.Current;
				return true;
			}
		}
		return false;
	}

	protected IEnumerable<Ant> EAntsOnBuildingTrails(Trail trail_start)
	{
		Trail trail = trail_start;
		int l = 0;
		while (l < 100)
		{
			l++;
			foreach (Ant currentAnt in trail.currentAnts)
			{
				yield return currentAnt;
			}
			int num = 0;
			Split splitEnd = trail.splitEnd;
			foreach (Trail item in trail.ETrails())
			{
				if (!(item.splitStart != splitEnd) && item.IsBuilding() && num == 0)
				{
					trail = item;
					num++;
				}
			}
			if (num == 0)
			{
				break;
			}
		}
		if (l >= 100)
		{
			Debug.LogError("EAntsOnBuildingTrails: endless loop");
		}
	}

	public List<Ant> GetAntsOnTrails()
	{
		List<Ant> list = new List<Ant>();
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			foreach (Trail item in listSpawnedTrail)
			{
				foreach (Ant currentAnt in item.currentAnts)
				{
					if (!list.Contains(currentAnt))
					{
						list.Add(currentAnt);
					}
				}
			}
		}
		return list;
	}

	public virtual bool CanHaveAttachment(Building _other, Vector3 pos, out BuildingAttachPoint attach_point)
	{
		attach_point = null;
		List<BuildingAttachPoint> list = new List<BuildingAttachPoint>();
		foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
		{
			if (buildingAttachPoint.CanHaveAttached(_other))
			{
				list.Add(buildingAttachPoint);
			}
		}
		if (list.Count > 0)
		{
			float num = float.MaxValue;
			for (int i = 0; i < list.Count; i++)
			{
				float num2 = Vector3.Distance(pos, list[i].GetPosition());
				if (num2 < num)
				{
					num = num2;
					attach_point = list[i];
				}
			}
		}
		if (attach_point != null)
		{
			return true;
		}
		return false;
	}

	public void SetAttachment(Building _other, BuildingAttachPoint attach_point)
	{
		if (attach_point.HasAttachment(out var att))
		{
			if (att == _other)
			{
				return;
			}
			att.ClearAttached();
		}
		attach_point.SetAttachment(_other);
		if (_other != null)
		{
			_other.OnSetAttached(this);
		}
		OnSetAttachment();
		UpdateBillboard();
	}

	public void RemoveAttachment(Building _other)
	{
		foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
		{
			if (buildingAttachPoint.IsAttachment(_other))
			{
				buildingAttachPoint.SetAttachment(null);
			}
		}
		UpdateBillboard();
	}

	protected virtual void OnSetAttachment()
	{
	}

	public void ClearAttachments()
	{
		foreach (BuildingAttachPoint buildingAttachPoint in buildingAttachPoints)
		{
			SetAttachment(null, buildingAttachPoint);
		}
	}

	public virtual void OnSetAttached(Building _target)
	{
		attachedTo = _target;
	}

	public void ClearAttached()
	{
		if (attachedTo != null)
		{
			attachedTo.RemoveAttachment(this);
			OnSetAttached(null);
		}
	}

	public Building GetAttachParent()
	{
		return attachedTo;
	}

	private void InitBuildProgress(bool during_load = false)
	{
		if (!during_load)
		{
			currentProgress = -0.0001f;
			targetProgress = 0f;
		}
		SetBuildMesh(build_active: true);
		if (matBuild != null)
		{
			UnityEngine.Object.Destroy(matBuild);
			matBuild = null;
		}
		foreach (Renderer item in ERenderers())
		{
			if (matBuild == null)
			{
				matOrig = item.sharedMaterial;
				matBuild = UnityEngine.Object.Instantiate(matOrig);
			}
			item.sharedMaterial = matBuild;
		}
		matBuild.SetFloat("_Dissolve", GetBuildValue(0f - currentProgress));
		SetBuildProgress(during_load);
	}

	private IEnumerable<Renderer> ERenderers()
	{
		Renderer[] componentsInChildren = meshBuild.GetComponentsInChildren<Renderer>(includeInactive: false);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!(renderer is ParticleSystemRenderer))
			{
				yield return renderer;
			}
		}
	}

	private void OnDestroy()
	{
		if (matBuild != null)
		{
			UnityEngine.Object.Destroy(matBuild);
			matBuild = null;
		}
	}

	protected virtual void SetBuildMesh(bool build_active)
	{
		if (build_active)
		{
			if (meshBuild == null)
			{
				meshBuild = UnityEngine.Object.Instantiate(meshBase, meshBase.transform.parent);
			}
			if (highlightOverride == meshBase.transform)
			{
				highlightOverride = meshBuild.transform;
			}
		}
		else
		{
			meshBuild.SetObActive(active: false);
			if (highlightOverride == meshBuild.transform)
			{
				highlightOverride = meshBase.transform;
			}
		}
		meshBase.SetObActive(!build_active);
	}

	private void SetBuildProgress(bool during_load = false)
	{
		float num = 0f;
		float num2 = 0f;
		foreach (PickupCost cost in costs)
		{
			if (cost.type != PickupType.NONE)
			{
				num2 += (float)cost.intValue;
				num += (float)GetCollectedAmount(cost.type, BuildingStatus.BUILDING, include_incoming: false);
			}
			else if (cost.category != PickupCategory.NONE)
			{
				num2 += (float)cost.intValue;
				num += (float)GetCollectedAmount(cost.category, BuildingStatus.BUILDING, include_incoming: false);
			}
		}
		if (!during_load && DebugSettings.standard.FreeBuildings())
		{
			num = num2;
		}
		targetProgress = ((num2 == 0f) ? 1f : (num / num2));
		if (num2 == 0f || num / num2 >= 1f)
		{
			GameManager.instance.RemoveBuildingBuilding(this);
			return;
		}
		GameManager.instance.AddBuildingBuilding(this);
		if (!during_load)
		{
			GameManager.instance.CheckBuildingBuildings();
		}
	}

	protected void UpdateBuildProgress()
	{
		if (currentProgress < 1f && currentProgress != targetProgress)
		{
			currentProgress = targetProgress;
			if (matBuild == null)
			{
				Debug.Log("mat build null");
			}
			matBuild.SetFloat("_Dissolve", GetBuildValue(0f - currentProgress));
		}
		if (!(currentProgress >= 1f))
		{
			return;
		}
		foreach (Renderer item in ERenderers())
		{
			item.sharedMaterial = matOrig;
		}
		UnityEngine.Object.Destroy(matBuild);
		matBuild = null;
		SetBuildMesh(build_active: false);
		SetStatus(BuildingStatus.COMPLETED);
	}

	public float GetBuildValue(float progress)
	{
		float num = GetHeight();
		if (buildRange != Vector2.zero)
		{
			num = buildRange.y - buildRange.x;
		}
		return buildRange.x + num * progress;
	}

	public override ExchangeType TrailInteraction(Trail _trail)
	{
		return currentStatus switch
		{
			BuildingStatus.BUILDING => TrailInteraction_Build(_trail), 
			BuildingStatus.COMPLETED => TrailInteraction_Intake(_trail), 
			_ => base.TrailInteraction(_trail), 
		};
	}

	protected virtual ExchangeType TrailInteraction_Build(Trail _trail)
	{
		return ExchangeType.NONE;
	}

	protected virtual ExchangeType TrailInteraction_Intake(Trail _trail)
	{
		return ExchangeType.NONE;
	}

	public override bool CanInsert(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (currentStatus == BuildingStatus.BUILDING)
		{
			if (exchange == ExchangeType.BUILD && !paused)
			{
				if (PickupData.ListContainsPickupType(GetPickupTypesRequiredForBuild(), _type))
				{
					return true;
				}
				foreach (PickupCategory item in GetPickupCategoriesRequiredForBuild())
				{
					if (_type.IsCategory(item))
					{
						return true;
					}
				}
			}
			return false;
		}
		return CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
	}

	protected virtual bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		return false;
	}

	public override void PrepareForPickup(Pickup _pickup, ExchangePoint _point)
	{
		switch (currentStatus)
		{
		case BuildingStatus.BUILDING:
			PrepareForPickup_Build(_pickup);
			break;
		case BuildingStatus.COMPLETED:
			PrepareForPickup_Intake(_pickup, _point);
			break;
		}
	}

	protected virtual void PrepareForPickup_Build(Pickup _pickup)
	{
		if (!incomingPickups_build.Contains(_pickup))
		{
			incomingPickups_build.Add(_pickup);
		}
	}

	protected virtual void PrepareForPickup_Intake(Pickup _pickup, ExchangePoint _point)
	{
		if (!incomingPickups_intake.Contains(_pickup))
		{
			incomingPickups_intake.Add(_pickup);
		}
	}

	public override void OnPickupArrival(Pickup _pickup, ExchangePoint point)
	{
		base.OnPickupArrival(_pickup, point);
		switch (currentStatus)
		{
		case BuildingStatus.BUILDING:
			OnPickupArrival_Build(_pickup, point);
			break;
		case BuildingStatus.COMPLETED:
			OnPickupArrival_Intake(_pickup, point);
			break;
		default:
			Debug.LogError(base.name + " was given a pickup while in status " + currentStatus.ToString() + ", shouldn't happen");
			_pickup.Delete();
			break;
		}
	}

	protected virtual void OnPickupArrival_Build(Pickup _pickup, ExchangePoint point)
	{
		PlayAudioShort(WorldSfx.PickupWooshArrive);
		if (incomingPickups_build.Contains(_pickup))
		{
			incomingPickups_build.Remove(_pickup);
		}
		AddPickup(_pickup.type, BuildingStatus.BUILDING);
		_pickup.Delete();
		SetBuildProgress();
		UpdateBillboard();
	}

	protected virtual void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		if (incomingPickups_intake.Contains(_pickup))
		{
			incomingPickups_intake.Remove(_pickup);
		}
		AddPickup(_pickup.type, BuildingStatus.COMPLETED);
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		RemovePickup(_type, 1, BuildingStatus.COMPLETED);
		return GameManager.instance.SpawnPickup(_type, GetExtractPos(), Quaternion.identity);
	}

	public void AddPickup(PickupType _type, BuildingStatus _status)
	{
		switch (_status)
		{
		case BuildingStatus.BUILDING:
			if (!dicCollectedPickups_build.ContainsKey(_type))
			{
				dicCollectedPickups_build.Add(_type, 0);
			}
			dicCollectedPickups_build[_type]++;
			break;
		case BuildingStatus.COMPLETED:
			if (!dicCollectedPickups_intake.ContainsKey(_type))
			{
				dicCollectedPickups_intake.Add(_type, 0);
				extractablePickupsChanged = true;
			}
			dicCollectedPickups_intake[_type]++;
			break;
		}
	}

	public virtual void DropPickups(PickupType _type, int n = 1, bool try_inventory = false)
	{
		if (GetCollectedAmount(_type, BuildingStatus.COMPLETED, include_incoming: false) == 0)
		{
			Debug.LogWarning(base.name + ": Tried to drop amount " + n + " of pickup " + _type.ToString() + " while not enough collected, shouldn't happen");
		}
		else
		{
			PlayAudioShort(AudioManager.GetDropSfx(_type));
			RemovePickup(_type, n, BuildingStatus.COMPLETED);
			for (int i = 0; i < n; i++)
			{
				Pickup p = GameManager.instance.SpawnPickup(_type, base.transform.position, Toolkit.RandomYRotation());
				DropPickup(p);
			}
		}
	}

	protected void DropPickup(Pickup p)
	{
		Vector3 target_pos = base.transform.position + Toolkit.GetRandomInDonut(GetRadius(), GetRadius() + 2f);
		p.Exchange(target_pos, (GameManager.instance.GetStatus() == GameStatus.PAUSED) ? ExchangeAnimationType.ARC_UNSCALED : ExchangeAnimationType.ARC);
	}

	public IEnumerable<string> EFactoryRecipes()
	{
		foreach (string recipe in data.recipes)
		{
			FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(recipe);
			if ((!DebugSettings.standard.demo || factoryRecipeData.inDemo) && (Progress.HasUnlockedRecipe(recipe) || factoryRecipeData.alwaysUnlocked))
			{
				yield return recipe;
			}
		}
	}

	public int GetCollectedAmount(PickupType _type, BuildingStatus _status, bool include_incoming)
	{
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(_status, include_incoming);
		if (_type == PickupType.ANY)
		{
			int num = 0;
			{
				foreach (KeyValuePair<PickupType, int> item in dicCollectedPickups)
				{
					num += item.Value;
				}
				return num;
			}
		}
		if (!dicCollectedPickups.ContainsKey(_type))
		{
			return 0;
		}
		return dicCollectedPickups[_type];
	}

	public int GetCollectedAmount(PickupCategory _cat, BuildingStatus _status, bool include_incoming)
	{
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(_status, include_incoming);
		int num = 0;
		foreach (KeyValuePair<PickupType, int> item in dicCollectedPickups)
		{
			if (PickupData.Get(item.Key).categories.Contains(_cat))
			{
				num += item.Value;
			}
		}
		return num;
	}

	public Dictionary<PickupType, int> GetDicCollectedPickups(BuildingStatus _status, bool include_incoming)
	{
		Dictionary<PickupType, int> dictionary;
		switch (_status)
		{
		case BuildingStatus.BUILDING:
			dictionary = new Dictionary<PickupType, int>(dicCollectedPickups_build);
			if (!include_incoming)
			{
				break;
			}
			foreach (Pickup item in incomingPickups_build)
			{
				if (!dictionary.ContainsKey(item.type))
				{
					dictionary.Add(item.type, 0);
				}
				dictionary[item.type]++;
			}
			break;
		case BuildingStatus.COMPLETED:
			dictionary = new Dictionary<PickupType, int>(dicCollectedPickups_intake);
			if (!include_incoming)
			{
				break;
			}
			foreach (Pickup item2 in incomingPickups_intake)
			{
				if (!dictionary.ContainsKey(item2.type))
				{
					dictionary.Add(item2.type, 0);
				}
				dictionary[item2.type]++;
			}
			break;
		default:
			Debug.LogError("Don't know dictionary for building status " + _status);
			dictionary = null;
			break;
		}
		return dictionary;
	}

	public List<PickupType> GetCollectedPickupsList(BuildingStatus _status, bool include_incoming)
	{
		List<PickupType> list = new List<PickupType>();
		foreach (KeyValuePair<PickupType, int> dicCollectedPickup in GetDicCollectedPickups(_status, include_incoming))
		{
			if (dicCollectedPickup.Value > 0)
			{
				list.Add(dicCollectedPickup.Key);
			}
		}
		return list;
	}

	public void RemovePickup(PickupType _type, int n, BuildingStatus _status)
	{
		bool flag = false;
		switch (_status)
		{
		case BuildingStatus.BUILDING:
			if (!dicCollectedPickups_build.ContainsKey(_type) || dicCollectedPickups_build[_type] - n < 0)
			{
				flag = true;
				break;
			}
			dicCollectedPickups_build[_type] -= n;
			if (dicCollectedPickups_build[_type] == 0)
			{
				dicCollectedPickups_build.Remove(_type);
			}
			break;
		case BuildingStatus.COMPLETED:
			if (!dicCollectedPickups_intake.ContainsKey(_type) || dicCollectedPickups_intake[_type] - n < 0)
			{
				flag = true;
				break;
			}
			dicCollectedPickups_intake[_type] -= n;
			if (dicCollectedPickups_intake[_type] == 0)
			{
				extractablePickupsChanged = true;
				dicCollectedPickups_intake.Remove(_type);
			}
			break;
		}
		if (flag)
		{
			Debug.LogWarning("Tried removing pickup " + _type.ToString() + " from building " + base.name + " that wasn't added, shouldn't happen");
		}
	}

	public void RemovePickup(PickupCategory _cat, int n, BuildingStatus _status)
	{
		for (int i = 0; i < n; i++)
		{
			List<PickupType> list = new List<PickupType>();
			switch (_status)
			{
			case BuildingStatus.BUILDING:
				foreach (KeyValuePair<PickupType, int> item in dicCollectedPickups_build)
				{
					if (item.Value > 0 && item.Key.IsCategory(_cat))
					{
						list.Add(item.Key);
					}
				}
				break;
			case BuildingStatus.COMPLETED:
				foreach (KeyValuePair<PickupType, int> item2 in dicCollectedPickups_intake)
				{
					if (item2.Value > 0 && item2.Key.IsCategory(_cat))
					{
						list.Add(item2.Key);
					}
				}
				break;
			}
			if (list.Count == 0)
			{
				Debug.LogError("Tried removing pickup catagory " + _cat.ToString() + " from building " + base.name + " that wasn't added, shouldn't happen");
			}
			else
			{
				RemovePickup(list[UnityEngine.Random.Range(0, list.Count)], 1, _status);
			}
		}
	}

	public List<PickupType> GetPickupTypesRequiredForBuild()
	{
		List<PickupType> list = new List<PickupType>();
		foreach (PickupCost cost in costs)
		{
			if (cost.type != PickupType.NONE && GetCollectedAmount(cost.type, BuildingStatus.BUILDING, include_incoming: true) < cost.intValue)
			{
				list.Add(cost.type);
			}
		}
		return list;
	}

	public List<PickupCategory> GetPickupCategoriesRequiredForBuild()
	{
		List<PickupCategory> list = new List<PickupCategory>();
		foreach (PickupCost cost in costs)
		{
			if (cost.category != PickupCategory.NONE && GetCollectedAmount(cost.category, BuildingStatus.BUILDING, include_incoming: true) < cost.intValue)
			{
				list.Add(cost.category);
			}
		}
		return list;
	}

	public void GetDicsRequiredPickups(out Dictionary<PickupType, int> dic_types, out Dictionary<PickupCategory, int> dic_cats)
	{
		dic_types = new Dictionary<PickupType, int>();
		dic_cats = new Dictionary<PickupCategory, int>();
		foreach (PickupCost cost in costs)
		{
			if (cost.type != PickupType.NONE)
			{
				int collectedAmount = GetCollectedAmount(cost.type, BuildingStatus.BUILDING, include_incoming: true);
				if (collectedAmount < cost.intValue)
				{
					dic_types.Add(cost.type, cost.intValue - collectedAmount);
				}
			}
			else if (cost.category != PickupCategory.NONE)
			{
				int collectedAmount2 = GetCollectedAmount(cost.category, BuildingStatus.BUILDING, include_incoming: true);
				if (collectedAmount2 < cost.intValue)
				{
					dic_cats.Add(cost.category, cost.intValue - collectedAmount2);
				}
			}
		}
	}

	public void EraseCollectedPickups(BuildingStatus _status, PickupType _type = PickupType.ANY)
	{
		Dictionary<PickupType, int> dicCollectedPickups = GetDicCollectedPickups(_status, include_incoming: false);
		if (_type == PickupType.ANY)
		{
			foreach (PickupType item in PickupData.EAllPickupTypes())
			{
				if (dicCollectedPickups.ContainsKey(item))
				{
					dicCollectedPickups[item] = 0;
				}
			}
			return;
		}
		if (dicCollectedPickups.ContainsKey(_type))
		{
			dicCollectedPickups[_type] = 0;
		}
	}

	public virtual void PlaceDispenser()
	{
		Gameplay.instance.ClearFocus();
		((Dispenser)Gameplay.instance.StartBuilding("DISPENSER")).Assign(this);
	}

	public virtual bool CanDispense()
	{
		return false;
	}

	public virtual IEnumerable<Animator> EPausableAnimators()
	{
		if (anim != null)
		{
			yield return anim;
		}
	}

	public virtual IEnumerable<ParticleSystem> EPausableParticles()
	{
		for (int i = 0; i < pausableParticleSystems.Length; i++)
		{
			yield return pausableParticleSystems[i];
		}
	}

	protected virtual bool CanPause()
	{
		if (currentStatus == BuildingStatus.BUILDING)
		{
			return true;
		}
		return false;
	}

	protected void SetPause(bool target)
	{
		if (CanPause())
		{
			paused = target;
			UpdateBillboard();
			if (!paused)
			{
				GameManager.instance.CheckBuildingBuildings();
			}
		}
	}

	public bool IsPaused()
	{
		return paused;
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (currentStatus == BuildingStatus.HOVERING || currentStatus == BuildingStatus.RELOCATE_HOVERING || currentStatus == BuildingStatus.ROTATING || currentStatus == BuildingStatus.RELOCATE_ROTATING || currentStatus == BuildingStatus.DRAG_PLACING)
		{
			return BillboardType.DONT_SHOW_BILLBOARD;
		}
		if (outdated)
		{
			code_desc = "BUILDING_OUTDATED";
			col = Color.red;
			return BillboardType.EXCLAMATION_RED;
		}
		if (IsPaused())
		{
			code_desc = "GENERIC_PAUSED";
			col = Color.cyan;
			return BillboardType.PAUSED;
		}
		if (currentStatus == BuildingStatus.BUILDING && ShowProgressBillboard())
		{
			txt_onBillboard = Loc.GetUI("GENERIC_PERCENTAGE", GetProgressText());
			return BillboardType.PROGRESS;
		}
		return BillboardType.NONE;
	}

	protected virtual bool ShowProgressBillboard()
	{
		return true;
	}

	public string GetProgressText()
	{
		return MathF.Floor((targetProgress > 0.999f) ? 100f : (targetProgress * 100f)).ToString("0");
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		ui_hover.SetStatusImage();
		if (CanPause())
		{
			ui_hover.SetPausePlay(delegate
			{
				SetPause(!paused);
			});
		}
		SetHoverBottomButtons_old(ui_hover);
		switch (currentStatus)
		{
		case BuildingStatus.BUILDING:
			SetHoverUI_Build(ui_hover);
			break;
		case BuildingStatus.COMPLETED:
			SetHoverUI_Intake(ui_hover);
			break;
		}
	}

	protected virtual void SetHoverUI_Build(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(data.GetTitle());
		ui_hover.SetInventory();
	}

	protected virtual void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(data.GetTitle());
		if (showInventory)
		{
			ui_hover.SetInventory();
		}
	}

	protected virtual void SetHoverBottomButtons_old(UIHoverClickOb ui_hover)
	{
		ui_hover.SetBottomButtons(CanDemolish() ? new Action(OnClickDelete) : null, delegate
		{
			Gameplay.instance.StartRelocate(this);
		}, null);
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
		ui_hover.UpdateStatusImage(GetCurrentBillboard(out var code_desc, out var _, out var col, out var _));
		ui_hover.SetStatusText(Loc.GetUI(code_desc), col);
		if (CanPause())
		{
			ui_hover.UpdatePausePlay(paused);
		}
		ui_hover.UpdateBottomButtons(CanDemolish());
		switch (currentStatus)
		{
		case BuildingStatus.BUILDING:
			UpdateHoverUI_Build(ui_hover);
			break;
		case BuildingStatus.COMPLETED:
			UpdateHoverUI_Intake(ui_hover);
			break;
		}
	}

	public List<(PickupType, string)> GetBuildProgressPairs()
	{
		List<(PickupType, string)> list = new List<(PickupType, string)>();
		foreach (PickupCost cost in costs)
		{
			int collectedAmount = GetCollectedAmount(cost.type, BuildingStatus.BUILDING, include_incoming: false);
			list.Add((cost.type, $"{collectedAmount} / {cost.intValue}"));
		}
		return list;
	}

	protected virtual void UpdateHoverUI_Build(UIHoverClickOb ui_hover)
	{
		ui_hover.UpdateInfo(Loc.GetUI("BUILDING_PERCENTAGECOMPLETED", GetProgressText()));
		ui_hover.inventoryGrid.Update(Loc.GetUI("BUILDING_BUILDMATERIALS"), GetBuildProgressPairs(), Loc.GetUI("GENERIC_EMPTY"));
	}

	protected virtual void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		if (showInventory)
		{
			ui_hover.inventoryGrid.Update(Loc.GetUI("BUILDING_INVENTORY"), GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false), Loc.GetUI("GENERIC_EMPTY"));
		}
	}

	public override UIClickType GetUiClickType()
	{
		return currentStatus switch
		{
			BuildingStatus.BUILDING => GetUiClickType_Build(), 
			BuildingStatus.COMPLETED => GetUiClickType_Intake(), 
			_ => UIClickType.BUILDING, 
		};
	}

	public virtual UIClickType GetUiClickType_Build()
	{
		return UIClickType.BUILDING;
	}

	public virtual UIClickType GetUiClickType_Intake()
	{
		return UIClickType.OLD;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		ui_click.SetTitle(data.GetTitle());
		ui_click.SetButton(UIClickButtonType.Delete, CanDemolish() ? new Action(OnClickDelete) : null, InputAction.Delete);
		ui_click.SetButtonHover(UIClickButtonType.Delete, "CLICKBOTBUT_HOVER_DEMOLISH");
		ui_click.SetButton(UIClickButtonType.Relocate, CanRelocate() ? ((Action)delegate
		{
			Gameplay.instance.StartRelocate(this);
		}) : null, InputAction.Relocate);
		ui_click.SetButtonHover(UIClickButtonType.Relocate, "CLICKBOTBUT_HOVER_RELOCATE");
		UIClickLayout_Building uIClickLayout_Building = (UIClickLayout_Building)ui_click;
		ButtonWithHotkey button = uIClickLayout_Building.GetButton(UIClickButtonType.TrackBuilding);
		if (button != null)
		{
			button.SetButton(delegate
			{
				UIGame.instance.TrackBuilding(this);
			}, InputAction.FollowAnt);
			button.SetHover("CLICKBOTBUT_HOVER_TRACK");
			button.btButton_better.gameObject.SetObActive(CanTrack());
		}
		switch (currentStatus)
		{
		case BuildingStatus.BUILDING:
			SetClickUi_Build(uIClickLayout_Building);
			break;
		case BuildingStatus.COMPLETED:
			SetClickUi_Intake(uIClickLayout_Building);
			break;
		}
	}

	public virtual void SetClickUi_Build(UIClickLayout_Building ui_building)
	{
		ui_building.SetInventory(target: true);
	}

	public virtual void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		ui_building.SetInfo("");
		ui_building.SetInventory(showInventory);
		ui_building.UpdateButton(UIClickButtonType.Generic1, enabled: false, "", show_button_error: false);
		ui_building.UpdateButton(UIClickButtonType.Generic2, enabled: false, "", show_button_error: false);
		if (CanCopySettings())
		{
			ButtonWithHotkey button = ui_building.GetButton(UIClickButtonType.CopySettings);
			if (button != null)
			{
				button.SetButton(delegate
				{
					Gameplay.CopySettings(this);
				}, InputAction.CopySettings);
				button.SetHover("CLICKBOTBUT_HOVER_COPY_BUILDING");
			}
			button = ui_building.GetButton(UIClickButtonType.PasteSettings);
			if (button != null)
			{
				button.SetButton(delegate
				{
					Gameplay.PasteSettings(new List<Building> { this });
				}, InputAction.PasteSettings);
				button.SetInteractable(BuildingConfig.CanPasteClipboard(this));
				button.SetHover("CLICKBOTBUT_HOVER_PASTE_BUILDING");
			}
		}
		else
		{
			ui_building.UpdateButton(UIClickButtonType.CopySettings, enabled: false, "", show_button_error: false);
			ui_building.UpdateButton(UIClickButtonType.PasteSettings, enabled: false, "", show_button_error: false);
		}
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		switch (currentStatus)
		{
		case BuildingStatus.BUILDING:
			UpdateClickUi_Build(ui_click);
			break;
		case BuildingStatus.COMPLETED:
			UpdateClickUi_Intake(ui_click);
			break;
		}
	}

	public virtual void UpdateClickUi_Build(UIClickLayout ui_click)
	{
		UIClickLayout_Building uIClickLayout_Building = (UIClickLayout_Building)ui_click;
		Dictionary<PickupType, string> dictionary = new Dictionary<PickupType, string>();
		foreach (PickupCost cost in costs)
		{
			int collectedAmount = GetCollectedAmount(cost.type, BuildingStatus.BUILDING, include_incoming: false);
			dictionary.Add(cost.type, $"{collectedAmount} / {cost.intValue}");
		}
		uIClickLayout_Building.SetInfo(Loc.GetUI("BUILDING_PERCENTAGECOMPLETED", GetProgressText()));
		uIClickLayout_Building.inventoryGrid.Update(Loc.GetUI("BUILDING_BUILDMATERIALS"), dictionary, Loc.GetUI("GENERIC_EMPTY"));
	}

	public virtual void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		UIClickLayout_Building uIClickLayout_Building = (UIClickLayout_Building)ui_click;
		if (showInventory)
		{
			uIClickLayout_Building.inventoryGrid.Update(Loc.GetUI("BUILDING_INVENTORY"), GetDicCollectedPickups(BuildingStatus.COMPLETED, include_incoming: false), Loc.GetUI("GENERIC_EMPTY"));
		}
	}

	protected void CountAsAnt(bool caa)
	{
		if (countAsAnt != caa)
		{
			countAsAnt = caa;
			GameManager.instance.UpdateAntCount();
		}
	}

	private IEnumerable<Split> ESpawnedSplits()
	{
		foreach (List<Trail> list in listSpawnedTrails)
		{
			if (list.Count > 0)
			{
				yield return list[0].splitStart;
			}
			foreach (Trail item in list)
			{
				yield return item.splitEnd;
			}
		}
	}

	public int GetBuildingSplitNr(Split split)
	{
		int num = 0;
		foreach (Split item in ESpawnedSplits())
		{
			if (item == split)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public Split GetBuildingSplit(int nr)
	{
		int num = 0;
		foreach (Split item in ESpawnedSplits())
		{
			if (num == nr)
			{
				return item;
			}
			num++;
		}
		return null;
	}

	public virtual bool CanDemolish()
	{
		if (data.noDemolish && currentStatus != BuildingStatus.BUILDING)
		{
			return DebugSettings.standard.DeletableEverything();
		}
		return true;
	}

	public virtual bool CanRelocate()
	{
		return true;
	}

	public virtual bool CanCopySettings()
	{
		return false;
	}

	public virtual bool CanTrack()
	{
		if (currentStatus != BuildingStatus.BUILDING)
		{
			return UIGame.instance.IsTrackingBuilding(this);
		}
		return true;
	}

	public virtual int GetCounterAntCount(int entrance)
	{
		return 0;
	}

	public void ClearEntrances()
	{
		List<Vector3> list = new List<Vector3>();
		ExchangePoint[] array = exchangePoints;
		foreach (ExchangePoint exchangePoint in array)
		{
			list.Add(exchangePoint.transform.position);
		}
		foreach (BuildingTrail buildingTrail in buildingTrails)
		{
			foreach (BuildingSplitPointProperties splitProperty in buildingTrail.splitProperties)
			{
				if (splitProperty.interactable)
				{
					list.Add(splitProperty.point.position);
				}
			}
		}
		List<BiomeObject> list2 = new List<BiomeObject>();
		foreach (Vector3 item in list)
		{
			RaycastHit[] array2 = Physics.RaycastAll(new Vector3(item.x, item.y + 100f, item.z), Vector3.down, 200f);
			foreach (RaycastHit raycastHit in array2)
			{
				BiomeObject componentInParent = raycastHit.collider.GetComponentInParent<BiomeObject>();
				if (componentInParent != null && !list2.Contains(componentInParent))
				{
					list2.Add(componentInParent);
				}
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		Debug.Log("Clearing " + list2.Count + " biome objects that are obstructing buildings");
		foreach (BiomeObject item2 in list2)
		{
			if (item2 != null)
			{
				item2.Delete();
			}
		}
	}

	private void EnsureChannel()
	{
		if (audioBuilding == null)
		{
			audioBuilding = AudioManager.GetBuildingChannel();
			audioBuilding.Lock();
			audioBuilding.Attach(base.transform);
			audioBuilding.InitCulled();
		}
	}

	protected void PlayAudio(AudioLink link, float start_time = 0f)
	{
		EnsureChannel();
		audioBuilding.Play(link, looped: false, start_time);
	}

	protected void PlayAudioShort(WorldSfx sfx)
	{
		AudioClip clip = AudioLinks.standard.GetClip(sfx);
		if (clip != null)
		{
			EnsureChannel();
			audioBuilding.PlayOnce(clip);
		}
	}

	protected void StartLoopAudio(AudioLink link)
	{
		if (link.IsSet())
		{
			EnsureChannel();
			audioBuilding.Play(link, looped: true);
		}
	}

	protected void StopAudio()
	{
		if (audioBuilding != null)
		{
			audioBuilding.Stop();
		}
	}

	protected bool IsPlayingAudio()
	{
		if (audioBuilding != null)
		{
			return audioBuilding.IsPlaying();
		}
		return false;
	}

	protected void ChangeAudioAttachment(Transform tf)
	{
		EnsureChannel();
		audioBuilding.Attach(tf);
	}

	private void ClearBuildingAudio()
	{
		if (audioBuilding != null)
		{
			audioBuilding.Free();
			audioBuilding = null;
		}
	}
}
