using System;
using System.Collections.Generic;
using UnityEngine;

public class Trail : TrailPart
{
	public const float trailInteractionRadius = 5f;

	public const float lineHeight = 0.75f;

	[SerializeField]
	private GameObject mesh;

	public Collider col;

	[SerializeField]
	private ActionPointPin pin;

	[SerializeField]
	private TrailShapeObject[] trailShapeObjects;

	[SerializeField]
	private MeshRenderer quadRenderer;

	[SerializeField]
	private MeshRenderer quadRendererArrow;

	[SerializeField]
	private MeshFilter quadMeshFilter;

	[SerializeField]
	private MeshFilter quadMeshFilterArrow;

	private Mesh quadMesh;

	private float lengthPrev;

	[NonSerialized]
	[Space(10f)]
	public Split splitStart;

	[NonSerialized]
	[Space(10f)]
	public Split splitEnd;

	[NonSerialized]
	public Vector3 posStart;

	[NonSerialized]
	public Vector3 posEnd;

	[NonSerialized]
	public float length;

	[NonSerialized]
	public Vector3 direction;

	[NonSerialized]
	public TrailData data;

	[NonSerialized]
	public TrailType trailType;

	private TrailStatus status;

	[NonSerialized]
	public Ant owner;

	[NonSerialized]
	public ExchangeType commandTrailExchangeType;

	[NonSerialized]
	public bool isInvalid;

	[NonSerialized]
	public int entranceN = -1;

	private bool invisible;

	private TrailShapeObject curTrailShapeObject;

	private int splitStartId;

	private int splitEndId;

	private int ownerId;

	private int[] nearbyObIds;

	[NonSerialized]
	public List<Ant> currentAnts = new List<Ant>();

	[NonSerialized]
	public List<ConnectableObject> nearbyConnectables = new List<ConnectableObject>();

	[NonSerialized]
	public List<ActionPoint> actionPoints = new List<ActionPoint>();

	private float previewActionPointHashPrev;

	[NonSerialized]
	public List<ConnectableObject> nearbyConnectablesTentative = new List<ConnectableObject>();

	[NonSerialized]
	public TrailGate trailGate;

	[NonSerialized]
	public Building building;

	private int buildingId = -1;

	private float offset;

	private bool deleteBasic;

	private const float OBSTACLE_CHECK_RADIUS = 1f;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write((!(splitStart == null)) ? splitStart.linkId : 0);
		if (splitStart == null)
		{
			save.Write(posStart);
		}
		save.Write((!(splitEnd == null)) ? splitEnd.linkId : 0);
		if (splitEnd == null)
		{
			save.Write(posEnd);
		}
		save.Write((!(owner == null)) ? owner.linkId : 0);
		save.Write(entranceN);
		save.Write(invisible);
		for (int num = nearbyConnectables.Count - 1; num >= 0; num--)
		{
			if (nearbyConnectables[num] == null)
			{
				nearbyConnectables.RemoveAt(num);
				Debug.LogError("Removed null object from connectables");
			}
		}
		int[] array = new int[nearbyConnectables.Count];
		int num2 = 0;
		for (int i = 0; i < nearbyConnectables.Count; i++)
		{
			if (!(nearbyConnectables[i] is Pickup))
			{
				array[num2] = nearbyConnectables[i].linkId;
				num2++;
			}
		}
		save.Write(num2);
		for (int j = 0; j < num2; j++)
		{
			save.Write(array[j]);
		}
		switch (trailType)
		{
		case TrailType.GATE_SENSORS:
		case TrailType.GATE_COUNTER:
		case TrailType.GATE_LIFE:
		case TrailType.GATE_CARRY:
		case TrailType.GATE_CASTE:
		case TrailType.GATE_OLD:
		case TrailType.GATE_COUNTER_END:
		case TrailType.GATE_SPEED:
		case TrailType.GATE_TIMER:
		case TrailType.GATE_STOCKPILE:
		case TrailType.GATE_LINK:
			trailGate.Write(save);
			break;
		case TrailType.IN_BUILDING:
		case TrailType.IN_BUILDING_GATE:
			save.Write((!(building == null)) ? building.linkId : 0);
			break;
		case TrailType.COMMAND:
			save.Write((int)commandTrailExchangeType);
			break;
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		splitStartId = save.ReadInt();
		if (splitStartId == 0)
		{
			posStart = save.ReadVector3();
		}
		splitEndId = save.ReadInt();
		if (splitEndId == 0)
		{
			posEnd = save.ReadVector3();
		}
		ownerId = save.ReadInt();
		entranceN = save.ReadInt();
		invisible = save.ReadBool();
		int num = save.ReadInt();
		nearbyObIds = new int[num];
		for (int i = 0; i < num; i++)
		{
			nearbyObIds[i] = save.ReadInt();
		}
		CreateTrailGate(save);
		if (trailType == TrailType.IN_BUILDING_GATE || (save.version >= 37 && trailType == TrailType.IN_BUILDING))
		{
			buildingId = save.ReadInt();
		}
		if (trailType == TrailType.COMMAND && save.version >= 90)
		{
			commandTrailExchangeType = (ExchangeType)save.ReadInt();
		}
	}

	public void LoadLinkSplits()
	{
		if (splitStartId != 0)
		{
			SetSplitStart(GameManager.instance.FindLink<Split>(splitStartId), update_length: false);
		}
		if (splitEndId != 0)
		{
			SetSplitEnd(GameManager.instance.FindLink<Split>(splitEndId), update_length: false);
		}
		TrailStatus trailStatus = TrailStatus.PLACED;
		if (trailType.IsBuildingTrail())
		{
			trailStatus = TrailStatus.PLACED_IN_BUILDING;
		}
		PlaceTrail(trailStatus, null, during_load: true);
	}

	public void LoadLinkAntsAndConnectables(HashSet<ConnectableObject> connectables_near_trails)
	{
		owner = GameManager.instance.FindLink<Ant>(ownerId);
		int num = nearbyObIds.Length;
		List<ConnectableObject> list = new List<ConnectableObject>(num);
		for (int i = 0; i < num; i++)
		{
			ConnectableObject connectableObject = GameManager.instance.FindLink<ConnectableObject>(nearbyObIds[i]);
			if (!(connectableObject is Pickup))
			{
				list.Add(connectableObject);
				if (!connectables_near_trails.Contains(connectableObject))
				{
					connectables_near_trails.Add(connectableObject);
				}
			}
		}
		SetNearbyConnectables(list);
		if (buildingId != -1)
		{
			building = GameManager.instance.FindLink<Building>(buildingId);
		}
		if (trailGate != null)
		{
			trailGate.LoadLinks();
			PositionTransformAtStart(trailGate.transform);
		}
		if (trailType == TrailType.COMMAND && (owner == null || owner.IsDead()))
		{
			Debug.Log("Removing disowned command trail");
			Delete();
		}
	}

	private void OnEnable()
	{
		offset = -100f + 33f * (float)UnityEngine.Random.Range(0, 7);
	}

	public void Fill(TrailData trail_data)
	{
		data = trail_data;
		trailType = data.type;
	}

	public void Init(TrailStatus _status, TrailGate trail_gate, Ant _owner, bool is_action_trail, bool is_invisible, bool during_load = false)
	{
		base.Init(during_load);
		pin.SetObActive(active: false);
		status = _status;
		owner = _owner;
		CopyGateFrom(trail_gate);
		col.enabled = !is_action_trail;
		if (!during_load)
		{
			invisible = is_invisible;
		}
		if (!is_action_trail)
		{
			SetTrailShape(ExchangeType.NONE, IsCommandTrail(), trailType);
		}
	}

	public Vector3 GetNearestPointOnTrail(Vector3 pos, bool snap_end = true)
	{
		Vector3 pos2 = GetPos(GetProgressNear(pos));
		if (snap_end)
		{
			if ((pos2 - posStart).sqrMagnitude < 0.25f)
			{
				pos2 = posStart;
			}
			else if ((pos2 - posEnd).sqrMagnitude < 0.25f)
			{
				pos2 = posEnd;
			}
		}
		return pos2;
	}

	public bool IsPlaced()
	{
		if (status != TrailStatus.PLACED && status != TrailStatus.PLACED_IN_BUILDING)
		{
			return status == TrailStatus.ACTION;
		}
		return true;
	}

	public bool IsBuilding()
	{
		return status == TrailStatus.PLACED_IN_BUILDING;
	}

	public bool IsCommandTrail()
	{
		return trailType == TrailType.COMMAND;
	}

	public bool IsLogic()
	{
		switch (trailType)
		{
		case TrailType.GATE_SENSORS:
		case TrailType.DIVIDER:
		case TrailType.GATE_COUNTER:
		case TrailType.GATE_LIFE:
		case TrailType.GATE_CARRY:
		case TrailType.GATE_CASTE:
		case TrailType.GATE_OLD:
		case TrailType.GATE_COUNTER_END:
		case TrailType.GATE_SPEED:
		case TrailType.GATE_TIMER:
		case TrailType.GATE_STOCKPILE:
		case TrailType.GATE_LINK:
		case TrailType.IN_BUILDING_GATE:
			return true;
		default:
			return false;
		}
	}

	public static bool IsGate(TrailType tt)
	{
		switch (tt)
		{
		case TrailType.GATE_SENSORS:
		case TrailType.GATE_COUNTER:
		case TrailType.GATE_LIFE:
		case TrailType.GATE_CARRY:
		case TrailType.GATE_CASTE:
		case TrailType.GATE_OLD:
		case TrailType.GATE_COUNTER_END:
		case TrailType.GATE_SPEED:
		case TrailType.GATE_TIMER:
		case TrailType.GATE_STOCKPILE:
		case TrailType.GATE_LINK:
		case TrailType.IN_BUILDING_GATE:
			return true;
		default:
			return false;
		}
	}

	public bool IsGate()
	{
		return IsGate(trailType);
	}

	public bool CanConnect()
	{
		if (!IsLogic() && trailType != TrailType.COMMAND && status != TrailStatus.ACTION)
		{
			return status != TrailStatus.PLACED_IN_BUILDING;
		}
		return false;
	}

	public bool IsAction()
	{
		return status == TrailStatus.ACTION;
	}

	public bool IsUsableFor(Ant ant)
	{
		if (!IsPlaced())
		{
			return false;
		}
		if (trailType == TrailType.COMMAND)
		{
			return owner == ant;
		}
		return true;
	}

	public IEnumerable<Trail> ETrails()
	{
		if (splitStart != null)
		{
			foreach (Trail item in splitStart.EPlacedTrails(this))
			{
				yield return item;
			}
		}
		if (!(splitEnd != null))
		{
			yield break;
		}
		foreach (Trail item2 in splitEnd.EPlacedTrails(this))
		{
			yield return item2;
		}
	}

	public override IEnumerable<Trail> ETrails(TrailType of_type)
	{
		foreach (Trail item in ETrails())
		{
			if (of_type == TrailType.NONE || item.trailType == of_type)
			{
				yield return item;
			}
		}
	}

	public bool IsConnectedTo(Trail trail)
	{
		foreach (Trail item in ETrails())
		{
			if (item == trail)
			{
				return true;
			}
		}
		return false;
	}

	public override TrailType GetTrailPartTrailType(params TrailType[] _exclude)
	{
		for (int i = 0; i < _exclude.Length; i++)
		{
			if (trailType == _exclude[i])
			{
				return TrailType.HAULING;
			}
		}
		return trailType;
	}

	private void SetTrailShape(TrailShape _shape)
	{
		curTrailShapeObject = null;
		for (int i = 0; i < trailShapeObjects.Length; i++)
		{
			TrailShapeObject trailShapeObject = trailShapeObjects[i];
			if (trailShapeObject.shape == _shape)
			{
				curTrailShapeObject = trailShapeObject;
				break;
			}
		}
		if (curTrailShapeObject == null)
		{
			Debug.LogError("Trail: don't know how to visualise " + _shape);
			curTrailShapeObject = trailShapeObjects[0];
		}
		curTrailShapeObject.Init(quadRenderer, quadRendererArrow, invisible);
	}

	public void SetPin(ActionPoint ap, bool clickable)
	{
		pin.Init(ap);
		pin.SetClickable(clickable);
		pin.SetObActive(active: true);
	}

	public bool CanDoExchangeType(ExchangeType _exchange)
	{
		if (!Progress.CanDoExchange(_exchange))
		{
			return false;
		}
		if (data.exchangeTypes.Contains(ExchangeType.OWNER))
		{
			if (owner == null)
			{
				Debug.LogError("Tried checking exchange type OWNER while no owner present, shouldn't happen");
			}
			else
			{
				if (commandTrailExchangeType != ExchangeType.NONE)
				{
					return _exchange == commandTrailExchangeType;
				}
				if (_exchange == ExchangeType.PLANT_CUT)
				{
					return false;
				}
				if (owner.data.exchangeTypes.Contains(_exchange) || owner.data.exchangeTypes.Contains(ExchangeType.ANY))
				{
					return true;
				}
			}
		}
		if (data.exchangeTypes.Contains(_exchange) || data.exchangeTypes.Contains(ExchangeType.ANY) || _exchange.EveryAntCanDo())
		{
			return true;
		}
		return false;
	}

	public void SetBuilding(Building _build)
	{
		building = _build;
	}

	public bool IsInBuilding(out Building owner)
	{
		if (building == null)
		{
			owner = null;
			return false;
		}
		owner = building;
		return true;
	}

	public bool IsInBuilding()
	{
		return building != null;
	}

	public HashSet<Trail> GetCounterArea(AreaMode mode, out bool invalid, out List<(Building, int)> counterArea_buildings)
	{
		HashSet<Trail> hashSet = new HashSet<Trail>();
		counterArea_buildings = new List<(Building, int)>();
		List<Trail> gateChain = GetGateChain();
		invalid = false;
		if (IsPlaced())
		{
			Queue<Trail> queue = new Queue<Trail>();
			Queue<Trail> queue2 = new Queue<Trail>();
			queue.Enqueue(this);
			while (queue.Count > 0 || queue2.Count > 0)
			{
				while (queue.Count > 0)
				{
					Trail trail = queue.Dequeue();
					Split split = trail.splitEnd;
					int num = 0;
					foreach (Trail connectedTrail in split.connectedTrails)
					{
						if (connectedTrail.splitStart != split)
						{
							continue;
						}
						num++;
						if (connectedTrail == this)
						{
							invalid = true;
							break;
						}
						if (!connectedTrail.IsPlaced() || hashSet.Contains(connectedTrail))
						{
							continue;
						}
						if (connectedTrail.IsGate() && !connectedTrail.IsInBuilding() && mode != AreaMode.StopNever && !gateChain.Contains(connectedTrail))
						{
							if (mode != AreaMode.StopAtGates && (connectedTrail.trailType != TrailType.GATE_COUNTER_END || (mode != AreaMode.StopAtEnds && mode != AreaMode.StopAtGates_IncludeFlowingBack_StopAtEnds)))
							{
								queue2.Enqueue(connectedTrail);
							}
						}
						else
						{
							hashSet.Add(connectedTrail);
							queue.Enqueue(connectedTrail);
						}
					}
					if (num != 0 || !(trail.building != null))
					{
						continue;
					}
					if (trail.building is Factory factory)
					{
						int item = -1;
						if (factory.antSlots > 0)
						{
							if (trail.actionPoints.Count > 1)
							{
								Debug.Log("Trail has multiple action points, this is not working");
							}
							item = trail.actionPoints[0].GetEntranceN();
						}
						if (!counterArea_buildings.Contains((factory, item)))
						{
							counterArea_buildings.Add((factory, item));
						}
					}
					if (!(trail.building is Gatherer gatherer))
					{
						continue;
					}
					if (!counterArea_buildings.Contains((gatherer, 0)))
					{
						counterArea_buildings.Add((gatherer, 0));
					}
					foreach (Trail item2 in gatherer.ECounterTrails())
					{
						if (!hashSet.Contains(item2))
						{
							hashSet.Add(item2);
							queue.Enqueue(item2);
						}
					}
				}
				while (queue2.Count > 0)
				{
					Trail trail2 = queue2.Dequeue();
					if ((trail2.trailType != TrailType.GATE_COUNTER_END && mode == AreaMode.StopAtEnds) || ConnectsToArea(trail2, hashSet, this, mode))
					{
						hashSet.Add(trail2);
						queue.Enqueue(trail2);
					}
				}
			}
		}
		hashSet.Add(this);
		return hashSet;
	}

	private static bool ConnectsToArea(Trail trail, HashSet<Trail> area, Trail start_trail, AreaMode mode)
	{
		HashSet<Trail> hashSet = new HashSet<Trail> { start_trail };
		Queue<Trail> queue = new Queue<Trail>();
		queue.Enqueue(trail);
		while (queue.Count > 0)
		{
			Split split = queue.Dequeue().splitEnd;
			foreach (Trail connectedTrail in split.connectedTrails)
			{
				if (!(connectedTrail.splitStart != split) && connectedTrail.trailType != TrailType.COMMAND && connectedTrail.IsPlaced() && !hashSet.Contains(connectedTrail) && (mode != AreaMode.OldCalculation || (connectedTrail.trailType != TrailType.GATE_COUNTER && connectedTrail.trailType != TrailType.GATE_COUNTER_END)) && (mode != AreaMode.StopAtGates_IncludeFlowingBack_StopAtEnds || connectedTrail.trailType != TrailType.GATE_COUNTER_END))
				{
					if (area.Contains(connectedTrail))
					{
						return true;
					}
					hashSet.Add(connectedTrail);
					queue.Enqueue(connectedTrail);
				}
			}
		}
		return false;
	}

	public void ChangeOwner(Ant new_owner)
	{
		Trail trail = this;
		int i;
		for (i = 0; i < 100; i++)
		{
			Trail trail2 = null;
			if (trail.splitEnd != null)
			{
				foreach (Trail item in trail.splitStart.ETrails(TrailType.COMMAND))
				{
					if (item != trail && item.owner == owner)
					{
						trail2 = item;
					}
				}
			}
			if (trail2 == null)
			{
				break;
			}
			trail = trail2;
		}
		if (i == 100)
		{
			Debug.LogError("DeleteCommandTrailTail: infinite loop (prev)");
			return;
		}
		for (i = 0; i < 100; i++)
		{
			Trail trail3 = null;
			if (trail.splitEnd != null)
			{
				foreach (Trail item2 in trail.splitEnd.ETrails(TrailType.COMMAND))
				{
					if (item2 != trail)
					{
						trail3 = item2;
					}
				}
			}
			trail.owner = new_owner;
			if (trail3 == null)
			{
				break;
			}
			trail = trail3;
		}
		if (i == 100)
		{
			Debug.LogError("DeleteCommandTrailTail: infinite loop (prev)");
		}
	}

	public void PlaceTrail(TrailStatus _status, List<ConnectableObject> obs = null, bool during_load = false)
	{
		status = _status;
		if (status != TrailStatus.PLACED && status != TrailStatus.PLACED_IN_BUILDING)
		{
			return;
		}
		if (splitStart == null || splitEnd == null)
		{
			if (splitStart == null)
			{
				Debug.LogError($"Tried to place trail without start split (pos {posStart:0})");
			}
			if (splitEnd == null)
			{
				Debug.LogError($"Tried to place trail without end split (pos {posEnd:0})");
			}
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		base.transform.position = splitStart.transform.position;
		SetEndPos(splitEnd.transform.position);
		splitStart.UpdateDividerTrails(during_load);
		splitStart.UpdateBillboardSoon();
		if (!during_load)
		{
			for (int num = actionPoints.Count - 1; num >= 0; num--)
			{
				actionPoints[num].Delete();
			}
			actionPoints.Clear();
			if (obs != null)
			{
				SetNearbyConnectables(obs);
				CheckActionPointsUpdate();
			}
			if (status == TrailStatus.PLACED)
			{
				Split.AddChangedSplit(splitStart);
				Split.AddChangedSplit(splitEnd);
			}
		}
		ResetMaterial();
	}

	public void CheckActionPointsUpdate(int dist = 3)
	{
		if (dist < 2)
		{
			foreach (ConnectableObject nearbyConnectable in nearbyConnectables)
			{
				ConnectableObject.ToUpdateAdd(nearbyConnectable);
			}
		}
		if (dist == 1)
		{
			foreach (Trail item in ETrails())
			{
				foreach (ConnectableObject nearbyConnectable2 in item.nearbyConnectables)
				{
					ConnectableObject.ToUpdateAdd(nearbyConnectable2);
				}
			}
			return;
		}
		if (dist <= 1)
		{
			return;
		}
		foreach (Trail item2 in ELinkedTrails(3))
		{
			foreach (ConnectableObject nearbyConnectable3 in item2.nearbyConnectables)
			{
				ConnectableObject.ToUpdateAdd(nearbyConnectable3);
			}
		}
	}

	public TrailGate GetTrailGate()
	{
		return trailGate;
	}

	public void CreateTrailGate(Save save = null)
	{
		if (trailGate == null)
		{
			trailGate = AssetLinks.standard.GetTrailGate(trailType);
			if (trailGate == null)
			{
				return;
			}
			if (save != null)
			{
				trailGate.Read(save);
			}
			trailGate.Init(save != null);
			trailGate.SetOwnerTrail(this);
		}
		if (save == null)
		{
			PositionTransformAtStart(trailGate.transform);
		}
	}

	public void CopyGateFrom(TrailGate other_gate)
	{
		if (!(other_gate == null))
		{
			CreateTrailGate();
			if (trailGate == null || trailGate.GetTrailType() != other_gate.GetTrailType())
			{
				Debug.LogError(string.Format("CopyGateDataFrom: type mismatch {0} <> {1}", (trailGate == null) ? "null" : trailGate.GetTrailType().ToString(), other_gate.GetTrailType()));
			}
			else
			{
				trailGate.CopyFrom(other_gate);
			}
		}
	}

	private void PositionTransformAtStart(Transform tf)
	{
		tf.position = posStart.ZeroPosition();
		Vector3 vector = Toolkit.LookVector(posStart.ZeroPosition(), posEnd.ZeroPosition());
		if (vector != Vector3.zero)
		{
			tf.rotation = Quaternion.LookRotation(vector);
		}
	}

	public IEnumerable<Trail> ELinkedTrails(int dist)
	{
		List<Trail> list = new List<Trail> { this };
		GetLinkedTrails(list, dist);
		foreach (Trail item in list)
		{
			yield return item;
		}
	}

	private void GetLinkedTrails(List<Trail> trails, int dist)
	{
		foreach (Trail item in ETrails())
		{
			switch (item.trailType)
			{
			case TrailType.GATE_SENSORS:
			case TrailType.DIVIDER:
			case TrailType.GATE_COUNTER:
			case TrailType.GATE_LIFE:
			case TrailType.GATE_CARRY:
			case TrailType.GATE_CASTE:
			case TrailType.GATE_OLD:
			case TrailType.GATE_COUNTER_END:
			case TrailType.GATE_SPEED:
			case TrailType.GATE_TIMER:
			case TrailType.GATE_STOCKPILE:
			case TrailType.GATE_LINK:
			case TrailType.IN_BUILDING:
			case TrailType.IN_BUILDING_GATE:
				continue;
			}
			if (!trails.Contains(item))
			{
				trails.Add(item);
				if (dist > 0)
				{
					item.GetLinkedTrails(trails, dist - 1);
				}
			}
		}
	}

	public void SetNearbyConnectables(List<ConnectableObject> obs)
	{
		nearbyConnectables = new List<ConnectableObject>(obs);
		foreach (ConnectableObject ob in obs)
		{
			if (ob != null)
			{
				ob.AddTrail(this);
			}
		}
	}

	public void AddNearbyConnectable(ConnectableObject ob)
	{
		if (!nearbyConnectables.Contains(ob))
		{
			nearbyConnectables.Add(ob);
		}
	}

	public void RemoveFromNearbyConnectables(ConnectableObject ob)
	{
		nearbyConnectables.Remove(ob);
	}

	public IEnumerable<ConnectableObject> EFindNearbyConnectables()
	{
		if (!CanConnect())
		{
			yield break;
		}
		int n = Physics.SphereCastNonAlloc(posStart, 5f, direction, Toolkit.raycastHits, length, Toolkit.Mask(Layers.Sources, Layers.Plants, Layers.Pickups, Layers.Buildings, Layers.BuildingElement, Layers.BigPlants));
		List<Transform> past_transforms = new List<Transform>();
		for (int i = 0; i < n; i++)
		{
			Transform transform = Toolkit.raycastHits[i].transform;
			if (!past_transforms.Contains(transform))
			{
				past_transforms.Add(transform);
				ConnectableObject componentInParent = transform.GetComponentInParent<ConnectableObject>();
				if (componentInParent != null && componentInParent.HasTrailInteraction(this))
				{
					yield return componentInParent;
				}
			}
		}
	}

	public List<ConnectableObject> FindNearbyConnectables()
	{
		List<ConnectableObject> list = new List<ConnectableObject>();
		foreach (ConnectableObject item in EFindNearbyConnectables())
		{
			list.Add(item);
		}
		return list;
	}

	public bool IsObstructed(ref List<ClickableObject> obstructing_objects, bool escape, ClickableObject ignore = null)
	{
		return IsObstructed(posStart, posEnd, ref obstructing_objects, escape, ignore);
	}

	public static bool IsObstructed(Vector3 p1, Vector3 p2, ref List<ClickableObject> obstructing_objects, bool escape, ClickableObject ignore = null)
	{
		bool result = false;
		Vector3 lhs = p2 - p1;
		float magnitude = lhs.magnitude;
		lhs /= magnitude;
		int num = Physics.SphereCastNonAlloc(p1, 1f, lhs, Toolkit.raycastHits, magnitude, Toolkit.Mask(Layers.Default, Layers.Buildings, Layers.Sources, Layers.Plants, Layers.Scenery, Layers.BigPlants));
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = Toolkit.raycastHits[i];
			Collider collider = raycastHit.collider;
			if (!IsObstruction(collider, ref obstructing_objects, ignore))
			{
				continue;
			}
			if (raycastHit.distance == 0f)
			{
				if (!escape || Vector3.Dot(lhs, (collider.gameObject.transform.position.SetY(0f) - p1).normalized) > -0.5f)
				{
					result = true;
				}
			}
			else
			{
				result = true;
			}
		}
		return result;
	}

	private static bool IsObstruction(Collider coll, ref List<ClickableObject> obstructing_objects, ClickableObject ignore = null)
	{
		if (coll.isTrigger)
		{
			return false;
		}
		ClickableObject componentInParent = coll.transform.GetComponentInParent<ClickableObject>();
		if (componentInParent == null || (componentInParent is BiomeObject biomeObject && biomeObject.data.trailsPassThrough) || componentInParent == ignore)
		{
			return false;
		}
		if (!obstructing_objects.Contains(componentInParent))
		{
			obstructing_objects.Add(componentInParent);
		}
		return true;
	}

	public static bool IsObstructed(Vector3 pos, ref List<ClickableObject> obstructing_objects)
	{
		bool result = false;
		int num = Physics.OverlapSphereNonAlloc(pos, 1f, Toolkit.overlapColliders, Toolkit.Mask(Layers.Default, Layers.Buildings, Layers.Sources, Layers.Plants, Layers.Scenery, Layers.BigPlants));
		for (int i = 0; i < num; i++)
		{
			if (IsObstruction(Toolkit.overlapColliders[i], ref obstructing_objects))
			{
				result = true;
			}
		}
		return result;
	}

	public bool IsOnGround()
	{
		return IsOnGround(posStart, posEnd, length);
	}

	private static bool IsOnGround(Vector3 start, Vector3 end, float l)
	{
		Vector3 vector = end - start;
		int num = Mathf.Max(Mathf.FloorToInt(l) / 4, 2);
		for (int i = 0; i < num; i++)
		{
			if (!Toolkit.IsOnGround(start + vector * ((float)i / (float)num)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsOnGround(Vector3 start, Vector3 end)
	{
		return IsOnGround(start, end, (end - start).magnitude);
	}

	public void SetEndPos(Vector3 end)
	{
		SetStartEndPos(posStart, end);
	}

	public void SetStartPos(Vector3 start, bool only_visual = false)
	{
		SetStartEndPos(start, posEnd, only_visual);
	}

	public void SetStartEndPos(Vector3 start, Vector3 end, bool only_visual = false)
	{
		base.transform.position = start;
		if (!only_visual)
		{
			posStart = start;
			posEnd = end;
		}
		base.transform.LookAt(end);
		float z = Vector3.Distance(start, end);
		mesh.transform.localScale = new Vector3(mesh.transform.localScale.x, mesh.transform.localScale.y, z);
		mesh.transform.position = (start + end) / 2f;
		if (!only_visual)
		{
			SetLength(z);
		}
		if (!curTrailShapeObject.useLineMesh)
		{
			curTrailShapeObject.SetLine(end, start);
		}
		if (trailGate != null)
		{
			PositionTransformAtStart(trailGate.transform);
		}
	}

	public void SetTrailShape(ExchangeType action_exchange, bool command_trail = false, TrailType _type = TrailType.NONE)
	{
		switch (action_exchange)
		{
		case ExchangeType.BUILDING_IN:
		case ExchangeType.ENTER:
			SetTrailShape(TrailShape.INSERT);
			break;
		case ExchangeType.BUILDING_OUT:
		case ExchangeType.FORAGE:
			SetTrailShape(TrailShape.EXTRACT);
			break;
		default:
			SetTrailShape(TrailShape.EXTRACT);
			break;
		case ExchangeType.NONE:
			if (command_trail)
			{
				SetTrailShape(TrailShape.DOTTED);
				break;
			}
			switch (_type)
			{
			case TrailType.GATE_SENSORS:
			case TrailType.GATE_COUNTER:
			case TrailType.GATE_LIFE:
			case TrailType.GATE_CARRY:
			case TrailType.GATE_CASTE:
			case TrailType.GATE_OLD:
			case TrailType.GATE_SPEED:
			case TrailType.GATE_TIMER:
			case TrailType.GATE_STOCKPILE:
			case TrailType.GATE_LINK:
				SetTrailShape(TrailShape.GATE);
				break;
			case TrailType.DIVIDER:
			case TrailType.GATE_COUNTER_END:
				SetTrailShape(TrailShape.DOUBLE);
				break;
			case TrailType.ELDER:
			case TrailType.NULL:
				SetTrailShape(TrailShape.MULTIPLE0);
				break;
			case TrailType.MAIN:
				SetTrailShape(TrailShape.THICK);
				break;
			case TrailType.IN_BUILDING:
			case TrailType.IN_BUILDING_GATE:
				SetTrailShape(TrailShape.STRAIGHT);
				break;
			default:
				SetTrailShape(TrailShape.REGULAR);
				break;
			}
			break;
		}
	}

	public void SetTrailShapeDirect(TrailShape trail_shape)
	{
		SetTrailShape(trail_shape);
	}

	public Split NewStartSplit(Vector3 pos)
	{
		Split split = GameManager.instance.NewSplit(pos);
		SetSplitStart(split);
		return split;
	}

	public Split NewEndSplit(Vector3 pos)
	{
		Split split = GameManager.instance.NewSplit(pos);
		SetSplitEnd(split);
		return split;
	}

	public Split NewMidSplit(Vector3 split_pos)
	{
		Trail first_trail;
		Trail second_trail;
		return NewMidSplit(split_pos, out first_trail, out second_trail);
	}

	public Split NewMidSplit(Vector3 split_pos, out Trail first_trail, out Trail second_trail)
	{
		float progressNear = GetProgressNear(split_pos);
		Split split = GameManager.instance.NewSplit(GetPos(progressNear));
		CreateCutTrails(split, split, out first_trail, out second_trail);
		MoveAnts(new List<Trail> { first_trail, second_trail });
		Delete();
		return split;
	}

	private void CreateCutTrails(Split s1, Split s2, out Trail t1, out Trail t2)
	{
		t1 = (t2 = null);
		if (s1 != null)
		{
			t1 = GameManager.instance.NewTrail(trailType, trailGate);
			t1.SetSplitStart(splitStart);
			t1.SetSplitEnd(s1);
			t1.PlaceTrail(TrailStatus.PLACED, t1.FindNearbyConnectables());
		}
		if (s2 != null)
		{
			TrailType type = trailType;
			if (IsLogic())
			{
				type = ((trailType == TrailType.GATE_OLD) ? TrailType.ELDER : TrailType.NULL);
			}
			t2 = GameManager.instance.NewTrail(type);
			t2.SetSplitStart(s2);
			t2.SetSplitEnd(splitEnd);
			t2.PlaceTrail(TrailStatus.PLACED, t2.FindNearbyConnectables());
		}
	}

	public Split CutForRelocation(Vector3 cut_pos_start_side, Vector3 cut_pos_end_side, bool return_start, out Trail trail_to_relocate)
	{
		cut_pos_start_side = GetPos(GetProgressNear(cut_pos_start_side));
		cut_pos_end_side = GetPos(GetProgressNear(cut_pos_end_side));
		Split split = null;
		Split split2 = null;
		if ((posStart - cut_pos_start_side).sqrMagnitude > 2.25f)
		{
			split = GameManager.instance.NewSplit(cut_pos_start_side);
		}
		if ((posEnd - cut_pos_end_side).sqrMagnitude > 2.25f)
		{
			split2 = GameManager.instance.NewSplit(cut_pos_end_side);
		}
		CreateCutTrails(split, split2, out var t, out var t2);
		if (!return_start)
		{
			Trail trail = t2;
			Trail trail2 = t;
			t = trail;
			t2 = trail2;
		}
		MoveAnts(new List<Trail> { t, t2 });
		Delete();
		trail_to_relocate = t;
		if (!return_start)
		{
			return split2;
		}
		return split;
	}

	public void ReplaceSplit(Split split_old, Split split_new)
	{
		if (splitStart == split_old)
		{
			SetSplitStart(split_new);
		}
		if (splitEnd == split_old)
		{
			SetSplitEnd(split_new);
		}
	}

	public void SetSplitStart(Split split, bool update_length = true)
	{
		if (splitStart != null)
		{
			splitStart.RemoveTrail(this);
		}
		splitStart = split;
		splitStart.AddTrail(this);
		base.transform.position = (posStart = splitStart.transform.position);
		if (update_length)
		{
			SetLength((posEnd - posStart).magnitude);
		}
	}

	public void SetSplitEnd(Split split, bool update_length = true)
	{
		if (splitEnd != null)
		{
			splitEnd.RemoveTrail(this);
		}
		splitEnd = split;
		splitEnd.AddTrail(this);
		posEnd = splitEnd.transform.position;
		if (update_length)
		{
			SetLength((posEnd - posStart).magnitude);
		}
	}

	private void SetLength(float l)
	{
		if (l == 0f)
		{
			length = 0.0001f;
			direction = new Vector3(1f, 0f, 0f);
		}
		else
		{
			length = l;
			direction = (posEnd - posStart) / l;
		}
		if (curTrailShapeObject.useLineMesh)
		{
			SetMeshLength(length);
		}
	}

	private void SetMeshLength(float length)
	{
		if (status == TrailStatus.PLACED)
		{
			if (quadMesh != null)
			{
				UnityEngine.Object.Destroy(quadMesh);
			}
			length = ((length < 1f) ? 1f : ((length < 5f) ? length.RoundAt(0.5f) : ((length < 20f) ? length.RoundAt(1f) : ((!(length < 40f)) ? length.RoundAt(10f) : length.RoundAt(5f)))));
			Mesh sharedMesh = MaterialLibrary.GetQuadMesh(length * curTrailShapeObject.tileFactor);
			quadMeshFilter.sharedMesh = sharedMesh;
			if (curTrailShapeObject.withArrow)
			{
				quadMeshFilterArrow.sharedMesh = sharedMesh;
			}
		}
		else
		{
			if (!(Mathf.Abs(length - lengthPrev) > 0.05f))
			{
				return;
			}
			float tiling = length * curTrailShapeObject.tileFactor;
			if (quadMesh == null)
			{
				quadMesh = MaterialLibrary.GetNewQuadMesh(tiling);
				quadMeshFilter.sharedMesh = quadMesh;
				if (curTrailShapeObject.withArrow)
				{
					quadMeshFilterArrow.sharedMesh = quadMesh;
				}
			}
			else
			{
				quadMesh.uv = MaterialLibrary.GetQuadUv(tiling);
			}
			lengthPrev = length;
		}
	}

	public Split GetOtherSplit(Split split)
	{
		if (!(split == splitStart))
		{
			return splitStart;
		}
		return splitEnd;
	}

	public void UpdateActionPointsPreview(ConnectableObject direct_interactable, bool no_audio, bool no_others = false)
	{
		nearbyConnectablesTentative.Clear();
		if (!isInvalid && !no_others)
		{
			foreach (ConnectableObject item2 in EFindNearbyConnectables())
			{
				if (!nearbyConnectablesTentative.Contains(item2))
				{
					nearbyConnectablesTentative.Add(item2);
				}
			}
		}
		if (direct_interactable != null)
		{
			nearbyConnectablesTentative.Add(direct_interactable);
		}
		float num = 0f;
		foreach (ConnectableObject item3 in nearbyConnectablesTentative)
		{
			num += item3.transform.position.x + item3.transform.position.z;
		}
		if (num != previewActionPointHashPrev)
		{
			int count = actionPoints.Count;
			for (int num2 = actionPoints.Count - 1; num2 >= 0; num2--)
			{
				actionPoints[num2].Delete();
			}
			actionPoints.Clear();
			foreach (ConnectableObject item4 in nearbyConnectablesTentative)
			{
				ActionPoint item = new ActionPoint(this, item4);
				actionPoints.Add(item);
			}
			previewActionPointHashPrev = num;
			if (!no_audio)
			{
				Vector3 pos = (posStart + posEnd) * 0.5f;
				if (actionPoints.Count > count)
				{
					AudioManager.PlayUI(pos, UISfx3D.TrailActionPointAppear);
				}
				else if (actionPoints.Count < count)
				{
					AudioManager.PlayUI(pos, UISfx3D.TrailActionPointDisappear);
				}
			}
		}
		foreach (ActionPoint actionPoint in actionPoints)
		{
			actionPoint.UpdatePosition();
			actionPoint.UpdatePin(clickable: false);
		}
	}

	public void SetActionPoint(ConnectableObject ob, bool active)
	{
		foreach (ActionPoint actionPoint2 in actionPoints)
		{
			if (actionPoint2.connectableObject == ob)
			{
				if (!active)
				{
					actionPoints.Remove(actionPoint2);
					actionPoint2.Delete();
				}
				return;
			}
		}
		if (!active)
		{
			return;
		}
		ActionPoint actionPoint = new ActionPoint(this, ob, trailType.IsBuildingTrail());
		actionPoints.Add(actionPoint);
		actionPoint.UpdatePosition();
		actionPoint.UpdatePin(clickable: false);
		foreach (Ant currentAnt in currentAnts)
		{
			currentAnt.RefreshAntActionPoints();
		}
	}

	public float GetProgressNear(Vector3 pos)
	{
		if (posEnd == posStart)
		{
			return 1f;
		}
		Vector3 rhs = posEnd - posStart;
		return Mathf.Clamp01(Vector3.Dot(pos - posStart, rhs) / rhs.sqrMagnitude);
	}

	public float GetProgressNearUnclamped(Vector3 pos)
	{
		if (posEnd == posStart)
		{
			return 1f;
		}
		Vector3 rhs = posEnd - posStart;
		return Vector3.Dot(pos - posStart, rhs) / rhs.sqrMagnitude;
	}

	public Vector3 GetPos(float progress)
	{
		return posStart + progress * (posEnd - posStart);
	}

	public void MoveAnts(List<Trail> alternatives)
	{
		foreach (Ant item in new List<Ant>(currentAnts))
		{
			Vector3 position = item.transform.position;
			foreach (Trail alternative in alternatives)
			{
				if (!(alternative == null))
				{
					float progressNearUnclamped = alternative.GetProgressNearUnclamped(position);
					if (!(progressNearUnclamped < 0f) && !(progressNearUnclamped > 1f))
					{
						item.SetCurrentTrail(alternative);
						break;
					}
				}
			}
		}
	}

	public override void DoHighlight(TrailStatus _status, bool include_trails_for_splits = true, bool also_building = false)
	{
		if (_status == TrailStatus.HOVERING || _status == TrailStatus.HOVERING_ERROR)
		{
			SetMaterial(_status, also_building);
		}
		else
		{
			ResetMaterial();
		}
	}

	public override void ResetMaterial()
	{
		if (trailType == TrailType.COMMAND && commandTrailExchangeType != ExchangeType.NONE)
		{
			SetMaterial(Gatherer.ExchangeTypeToCommandTrailType(commandTrailExchangeType), IsPlaced());
		}
		else
		{
			SetMaterial(trailType, IsPlaced());
		}
	}

	public override void SetMaterial(TrailStatus _status, bool also_building = false)
	{
		if (!also_building && status == TrailStatus.PLACED_IN_BUILDING)
		{
			ResetMaterial();
		}
		else
		{
			base.SetMaterial(_status);
		}
	}

	public override void SetMaterial(Material mat)
	{
		curTrailShapeObject.SetMaterial(mat, quadRenderer, quadRendererArrow, offset);
		foreach (ActionPoint actionPoint in actionPoints)
		{
			if (actionPoint.actionTrail != null)
			{
				actionPoint.actionTrail.SetMaterial(mat);
			}
		}
		if (trailGate != null && trailGate.gameObject.activeSelf)
		{
			trailGate.SetMaterial(mat);
		}
	}

	public void EnterTrail(Ant ant)
	{
		if (currentAnts.Contains(ant))
		{
			Debug.LogError(ant.name + " was added to " + base.name + " a second time, shouldn't happen");
		}
		else
		{
			currentAnts.Add(ant);
		}
	}

	public void ExitTrail(Ant ant)
	{
		if (!currentAnts.Contains(ant))
		{
			Debug.LogError(ant.name + " tried to leave " + base.name + " while not being added to it, shouldn't happen");
		}
		else
		{
			currentAnts.Remove(ant);
		}
	}

	public bool CheckIfTrailGateSatisfied(Ant ant, bool final, out string warning)
	{
		warning = "";
		switch (trailType)
		{
		case TrailType.IN_BUILDING_GATE:
			if (building == null)
			{
				Debug.LogError("Tried checking " + trailType.ToString() + " without assigned building, shouldn't happen");
				return false;
			}
			if (building.currentStatus != BuildingStatus.COMPLETED)
			{
				return false;
			}
			return building.CheckIfGateIsSatisfied(ant, this, out warning);
		case TrailType.GATE_SENSORS:
		case TrailType.GATE_COUNTER:
		case TrailType.GATE_LIFE:
		case TrailType.GATE_CARRY:
		case TrailType.GATE_CASTE:
		case TrailType.GATE_OLD:
		case TrailType.GATE_COUNTER_END:
		case TrailType.GATE_SPEED:
		case TrailType.GATE_TIMER:
		case TrailType.GATE_STOCKPILE:
		case TrailType.GATE_LINK:
		{
			if (trailGate == null)
			{
				Debug.LogError("Tried checking " + trailType.ToString() + " without trail gate, shouldn't happen");
				return false;
			}
			bool flag = trailGate.CheckIfChainSatisfied(ant, final);
			return trailGate.CheckIfSatisfied(ant, final, flag) && flag;
		}
		default:
			return true;
		}
	}

	public void DoEnterGate(Ant _ant)
	{
		if (trailGate != null)
		{
			trailGate.EnterGate(_ant);
		}
	}

	public List<Trail> GetGateChain()
	{
		List<Trail> list = new List<Trail>();
		if (!IsGate() || !IsPlaced())
		{
			return list;
		}
		list.Add(this);
		Trail trail = this;
		int num = 1;
		for (int i = 0; i < num; i++)
		{
			if (!trail.IsPlaced() || trail.splitEnd.TrailCount(placed: true) != 2)
			{
				continue;
			}
			foreach (Trail item in trail.splitEnd.EPlacedTrails())
			{
				if (item != trail && item.IsGate() && !item.IsInBuilding() && item.GetTrailGate() != null && !list.Contains(item))
				{
					list.Add(item);
					trail = item;
					num++;
				}
			}
		}
		return list;
	}

	public bool IsStartOfChain()
	{
		if (!IsGate() || !IsPlaced() || splitEnd.TrailCount(placed: true) != 2)
		{
			return false;
		}
		foreach (Trail connectedTrail in splitEnd.connectedTrails)
		{
			if (connectedTrail.splitStart == splitEnd)
			{
				return connectedTrail.IsGate();
			}
		}
		return false;
	}

	protected override void DoDelete()
	{
		for (int num = currentAnts.Count - 1; num >= 0; num--)
		{
			Ant ant = currentAnts[num];
			if (ant == null)
			{
				Debug.LogError("Ant was null, shouldn't happen");
			}
			else
			{
				TrailEditing.AntLostTrail(ant);
				ant.SetCurrentTrail(null);
			}
		}
		foreach (ConnectableObject nearbyConnectable in nearbyConnectables)
		{
			nearbyConnectable.RemoveTrail(this);
		}
		if (!deleteBasic)
		{
			CheckActionPointsUpdate();
		}
		if (Gameplay.instance.IsDrawingTrail() && trailType == TrailType.COMMAND && !IsPlaced())
		{
			Gameplay.instance.SetActivity(Activity.NONE);
		}
		if (quadMesh != null)
		{
			UnityEngine.Object.Destroy(quadMesh);
		}
		for (int num2 = actionPoints.Count - 1; num2 >= 0; num2--)
		{
			actionPoints[num2].Delete();
		}
		if (deleteBasic)
		{
			if (splitStart != null)
			{
				splitStart.RemoveTrailBasic(this);
			}
			if (splitEnd != null)
			{
				splitEnd.RemoveTrailBasic(this);
			}
		}
		else
		{
			if (splitStart != null)
			{
				splitStart.RemoveTrail(this);
			}
			if (splitEnd != null)
			{
				splitEnd.RemoveTrail(this);
			}
		}
		if (status == TrailStatus.PLACED)
		{
			if (splitStart != null)
			{
				Split.AddChangedSplit(splitStart);
			}
			if (splitEnd != null)
			{
				Split.AddChangedSplit(splitEnd);
			}
		}
		GameManager.instance.RemoveTrail(this);
		if (trailGate != null)
		{
			trailGate.Delete();
		}
		if (!(this == null))
		{
			UnityEngine.Object.Destroy(base.gameObject);
			base.DoDelete();
		}
	}

	public void DeleteBasic()
	{
		deleteBasic = true;
		DoDelete();
	}

	public void DeleteGate()
	{
		if (trailGate != null)
		{
			trailGate.Delete();
			trailGate = null;
		}
	}

	public void DeleteCommandTrailTail()
	{
		Trail trail = this;
		int i;
		for (i = 0; i < 100; i++)
		{
			Trail trail2 = null;
			if (trail.splitEnd != null)
			{
				foreach (Trail item in trail.splitStart.ETrails(TrailType.COMMAND))
				{
					if (item != trail && item.owner == owner)
					{
						trail2 = item;
					}
				}
			}
			if (trail2 == null)
			{
				break;
			}
			trail = trail2;
		}
		if (i == 100)
		{
			Debug.LogError("DeleteCommandTrailTail: infinite loop (prev)");
			return;
		}
		for (i = 0; i < 100; i++)
		{
			if (trail.currentAnts.Count > 0)
			{
				break;
			}
			Trail trail3 = null;
			if (trail.splitEnd != null)
			{
				foreach (Trail item2 in trail.splitEnd.ETrails(TrailType.COMMAND))
				{
					if (item2 != trail && item2.owner == owner)
					{
						trail3 = item2;
					}
				}
			}
			trail.Delete();
			if (trail3 == null)
			{
				break;
			}
			trail = trail3;
		}
		if (i == 100)
		{
			Debug.LogError("DeleteCommandTrailTail: infinite loop (prev)");
		}
	}

	public MeshRenderer GetBaseRenderer()
	{
		return quadRenderer;
	}

	public override string ToString()
	{
		return $"{base.name} ({trailType}, {status})";
	}
}
