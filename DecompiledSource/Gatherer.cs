using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gatherer : Building
{
	private struct GatherTarget
	{
		public enum GatherTargetType
		{
			Normal,
			Pickup,
			Circum
		}

		public ConnectableObject ob;

		public Vector2 pos;

		public float prio;

		public GatherTargetType type;

		public BiomeObject.Circum circum;

		public static GatherTarget none;

		public GatherTarget(ConnectableObject _ob, Vector2 _pos, float _dist_sq)
		{
			ob = _ob;
			pos = _pos;
			prio = _dist_sq * UnityEngine.Random.Range(1f, 1.8f);
			type = GatherTargetType.Normal;
			circum = default(BiomeObject.Circum);
		}
	}

	private enum ReservedContext
	{
		Local,
		Global
	}

	[Header("Gatherer")]
	[SerializeField]
	private Transform returnBuildingTrail;

	[SerializeField]
	private GameObject pfRadiusIndicator;

	[SerializeField]
	private Material matIndicatorHauling;

	[SerializeField]
	private Material matIndicatorForaging;

	[SerializeField]
	private Material matIndicatorMining;

	[SerializeField]
	private Material matIndicatorPlantCutting;

	private ExchangeType gatherType = ExchangeType.PICKUP;

	private PickupType pickupFilter = PickupType.ANY;

	private PickupType forageFilter = PickupType.ANY;

	private PickupType miningFilter = PickupType.ANY;

	private PickupType cutFilter = PickupType.ANY;

	private ExchangeType shownGatherType;

	private PickupType shownFilter;

	private float shownRadius01;

	private static List<PickupType> pickupPickups;

	private static List<PickupType> foragePickups;

	private static List<PickupType> minePickups;

	private static List<PickupType> cutPickups;

	private GameObject obRadiusIndicator;

	private LineRenderer radiusLineRenderer;

	private Coroutine cCreatePathForAnt;

	private Ant innerAnt;

	private List<Trail> busyTrails;

	private List<Trail> commandTrails = new List<Trail>();

	private static List<(GatherTarget, float)> globalReservedTargets = new List<(GatherTarget, float)>();

	private List<(GatherTarget, float)> myReservedTargets = new List<(GatherTarget, float)>();

	private static int lastUpdateFrame;

	private float DELAY_INITIAL = 1f;

	public const float NAV_SQUARE_SIZE = 12f;

	private float DRAW_SPEED = 100f;

	private const float MIN_RADIUS = 50f;

	private const float MAX_RADIUS = 500f;

	private bool cantFindMaterial;

	private bool cantDoCaste;

	private bool isPlacing;

	private bool isHovering;

	private bool isSelected;

	public float searchRadius { get; private set; }

	public PickupType curFilter
	{
		get
		{
			switch (gatherType)
			{
			case ExchangeType.PICKUP:
				return pickupFilter;
			case ExchangeType.FORAGE:
				return forageFilter;
			case ExchangeType.MINE:
				return miningFilter;
			case ExchangeType.PLANT_CUT:
				return cutFilter;
			default:
				Debug.LogError($"Unexpected gatherType {gatherType}");
				return PickupType.ANY;
			}
		}
		private set
		{
			switch (gatherType)
			{
			case ExchangeType.PICKUP:
				pickupFilter = value;
				break;
			case ExchangeType.FORAGE:
				forageFilter = value;
				break;
			case ExchangeType.MINE:
				miningFilter = value;
				break;
			case ExchangeType.PLANT_CUT:
				cutFilter = value;
				break;
			default:
				Debug.LogError($"Unexpected gatherType {gatherType}");
				break;
			}
		}
	}

	public override void Write(Save save)
	{
		base.Write(save);
		WriteConfig(save);
		CleanCommandTrails();
		save.Write(commandTrails.Count);
		foreach (Trail commandTrail in commandTrails)
		{
			save.Write(commandTrail.linkId);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		ReadConfig(save);
		if (save.version < 92)
		{
			return;
		}
		commandTrails.Clear();
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			int num2 = save.ReadInt();
			if (num2 != 0)
			{
				commandTrails.Add(GameManager.instance.FindLink<Trail>(num2));
			}
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write((int)gatherType);
		save.Write((int)pickupFilter);
		save.Write((int)forageFilter);
		save.Write((int)miningFilter);
		save.Write((int)cutFilter);
		save.Write(searchRadius);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		gatherType = (ExchangeType)save.ReadInt();
		pickupFilter = (forageFilter = (miningFilter = (cutFilter = PickupType.ANY)));
		if (save.GetVersion() >= 92)
		{
			int num = save.ReadInt();
			if (num != 0)
			{
				pickupFilter = (PickupType)num;
				forageFilter = (PickupType)save.ReadInt();
				miningFilter = (PickupType)save.ReadInt();
				cutFilter = (PickupType)save.ReadInt();
			}
		}
		if (save.GetVersion() >= 94)
		{
			searchRadius = save.ReadFloat();
		}
	}

	public override void BuildingUpdate(float dt, bool run_world)
	{
		base.BuildingUpdate(dt, run_world);
		if (run_world)
		{
			UpdateReserved(ReservedContext.Local, dt, run_world);
			if (Time.frameCount > lastUpdateFrame)
			{
				UpdateReserved(ReservedContext.Global, dt, run_world);
				lastUpdateFrame = Time.frameCount;
			}
		}
	}

	private void UpdateRadiusIndicator()
	{
		bool flag = isPlacing || isHovering || isSelected;
		if (searchRadius == 0f || !flag)
		{
			if (obRadiusIndicator != null)
			{
				UnityEngine.Object.Destroy(obRadiusIndicator);
			}
			obRadiusIndicator = null;
			radiusLineRenderer = null;
			return;
		}
		if (obRadiusIndicator == null)
		{
			obRadiusIndicator = UnityEngine.Object.Instantiate(pfRadiusIndicator, base.transform.position, Quaternion.identity, base.transform);
		}
		if (radiusLineRenderer == null)
		{
			radiusLineRenderer = obRadiusIndicator.GetComponentInChildren<LineRenderer>();
		}
		int num = 50;
		radiusLineRenderer.positionCount = num;
		float num2 = searchRadius;
		for (int i = 0; i < num; i++)
		{
			float f = MathF.PI * 2f * (float)i / (float)num;
			radiusLineRenderer.SetPosition(i, new Vector3(Mathf.Cos(f) * num2, 0.5f, Mathf.Sin(f) * num2));
		}
		switch (gatherType)
		{
		case ExchangeType.PICKUP:
			radiusLineRenderer.sharedMaterial = matIndicatorHauling;
			break;
		case ExchangeType.FORAGE:
			radiusLineRenderer.sharedMaterial = matIndicatorForaging;
			break;
		case ExchangeType.MINE:
			radiusLineRenderer.sharedMaterial = matIndicatorMining;
			break;
		case ExchangeType.PLANT_CUT:
			radiusLineRenderer.sharedMaterial = matIndicatorPlantCutting;
			break;
		default:
			radiusLineRenderer.sharedMaterial = matIndicatorHauling;
			break;
		}
	}

	public override void SetHoverMode(bool hover)
	{
		base.SetHoverMode(hover);
		isPlacing = hover;
		UpdateRadiusIndicator();
	}

	public override void OnHoverEnter()
	{
		base.OnHoverEnter();
		isHovering = true;
		UpdateRadiusIndicator();
	}

	public override void OnHoverExit()
	{
		base.OnHoverExit();
		isHovering = false;
		UpdateRadiusIndicator();
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		isSelected = is_selected;
		if (is_selected != was_selected)
		{
			UpdateRadiusIndicator();
		}
	}

	private void AddReserved(ReservedContext context, GatherTarget target, float dur)
	{
		((context == ReservedContext.Global) ? globalReservedTargets : myReservedTargets).Add((target, dur));
	}

	private void UpdateReserved(ReservedContext context, float dt, bool run_world)
	{
		List<(GatherTarget, float)> list = ((context == ReservedContext.Global) ? globalReservedTargets : myReservedTargets);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			var (item, num2) = list[num];
			if (item.ob == null || num2 < 0f)
			{
				list.RemoveAt(num);
			}
			else if (run_world)
			{
				list[num] = (item, num2 - dt);
			}
		}
	}

	private bool IsReserved(GatherTarget t)
	{
		if (!IsReserved(ReservedContext.Local, t))
		{
			return IsReserved(ReservedContext.Global, t);
		}
		return true;
	}

	private bool IsReserved(ReservedContext context, GatherTarget t)
	{
		foreach (var item2 in (context == ReservedContext.Global) ? globalReservedTargets : myReservedTargets)
		{
			GatherTarget item = item2.Item1;
			if (!(t.ob != item.ob) && (t.type != GatherTarget.GatherTargetType.Circum || !(t.circum != item.circum)))
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerable<GatherTarget> EGatherOptions()
	{
		Vector2 vector = base.transform.position.XZ();
		List<GatherTarget> options = new List<GatherTarget>();
		float num = searchRadius * searchRadius;
		bool let_ant_wait = false;
		bool flag = gatherType == ExchangeType.MINE;
		PickupType pickupType = curFilter;
		if (gatherType == ExchangeType.PICKUP || gatherType == ExchangeType.FORAGE)
		{
			foreach (Pickup item2 in ground.EPickupsOnGround())
			{
				if (pickupType == PickupType.ANY || item2.type == pickupType)
				{
					Vector2 vector2 = item2.transform.position.XZ();
					float sqrMagnitude = (vector2 - vector).sqrMagnitude;
					if ((!(num > 0f) || !(sqrMagnitude > num)) && (gatherType != ExchangeType.FORAGE || item2.CanForage()))
					{
						GatherTarget gatherTarget = new GatherTarget(item2, vector2, sqrMagnitude);
						gatherTarget.type = GatherTarget.GatherTargetType.Pickup;
						GatherTarget item = gatherTarget;
						options.Add(item);
					}
				}
			}
		}
		if (gatherType == ExchangeType.PICKUP || gatherType == ExchangeType.MINE || gatherType == ExchangeType.PLANT_CUT)
		{
			foreach (BiomeObject item3 in GameManager.instance.EBiomeObjects(ground))
			{
				if (item3.gameObject.layer == 26 || item3.gameObject.layer == 12)
				{
					continue;
				}
				Vector2 vector3 = item3.transform.position.XZ();
				float num2 = 0f;
				if (!flag)
				{
					num2 = (vector3 - vector).sqrMagnitude;
					if (num > 0f && num2 > num)
					{
						continue;
					}
				}
				if (item3.CanExtract(gatherType, ref let_ant_wait) && (pickupType == PickupType.ANY || item3.HasExtractablePickup(gatherType, pickupType)))
				{
					if (flag)
					{
						FillFromCircums(item3, vector, ref options, num);
					}
					else
					{
						options.Add(new GatherTarget(item3, vector3, num2));
					}
				}
			}
		}
		if (gatherType == ExchangeType.MINE || gatherType == ExchangeType.FORAGE || gatherType == ExchangeType.PLANT_CUT)
		{
			foreach (Plant item4 in ground.ecology.EPlants())
			{
				Vector2 vector4 = item4.transform.position.XZ();
				float num3 = 0f;
				if (!flag)
				{
					num3 = (vector4 - vector).sqrMagnitude;
					if (num > 0f && num3 > num)
					{
						continue;
					}
				}
				if (item4.CanExtract(gatherType, ref let_ant_wait) && (pickupType == PickupType.ANY || item4.HasExtractablePickup(gatherType, pickupType)))
				{
					if (gatherType == ExchangeType.MINE)
					{
						FillFromCircums(item4, vector, ref options, num);
					}
					else
					{
						options.Add(new GatherTarget(item4, vector4, num3));
					}
				}
			}
		}
		options.Sort((GatherTarget a, GatherTarget b) => a.prio.CompareTo(b.prio));
		foreach (GatherTarget item5 in options)
		{
			yield return item5;
		}
	}

	private void FillFromCircums(BiomeObject bob, Vector2 orig_pos, ref List<GatherTarget> options, float max_range_sq)
	{
		foreach (BiomeObject.Circum item2 in bob.ECircums())
		{
			float sqrMagnitude = (item2.pos - orig_pos).sqrMagnitude;
			if (!(max_range_sq > 0f) || !(sqrMagnitude > max_range_sq))
			{
				float num = 0.5f - 0.5f * Vector2.Dot((item2.pos - orig_pos).normalized, item2.dir);
				num *= bob.GetRadius();
				sqrMagnitude += num * num;
				GatherTarget gatherTarget = new GatherTarget(bob, item2.pos, sqrMagnitude);
				gatherTarget.circum = item2;
				gatherTarget.type = GatherTarget.GatherTargetType.Circum;
				GatherTarget item = gatherTarget;
				options.Add(item);
			}
		}
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		return true;
	}

	public override float UseBuilding(int entrance, Ant ant, out bool ant_entered)
	{
		ant_entered = true;
		StartCoroutine(CCreatePathForAnt(ant));
		UpdateBillboard();
		return 0f;
	}

	private IEnumerator CCreatePathForAnt(Ant ant)
	{
		while (cCreatePathForAnt != null)
		{
			yield return null;
		}
		cCreatePathForAnt = StartCoroutine(CCreatePathForAnt2(ant));
	}

	private IEnumerator CCreatePathForAnt2(Ant ant)
	{
		innerAnt = ant;
		ant.SetMoveState(MoveState.Waiting);
		busyTrails = new List<Trail>();
		GatherTarget gather_target = GatherTarget.none;
		Vector2 start_pos = ant.transform.position.XZ();
		Vector2 return_pos = returnBuildingTrail.position.XZ();
		List<Vector2> path = null;
		int path_target_index = 0;
		cantDoCaste = !ant.data.exchangeTypes.Contains(gatherType);
		bool skip = cantDoCaste || ant.IsFull();
		int n_fails = 0;
		if (!skip)
		{
			foreach (GatherTarget item in EGatherOptions())
			{
				if (IsReserved(item))
				{
					continue;
				}
				Vector2 target_pos;
				if (item.type == GatherTarget.GatherTargetType.Circum)
				{
					if (BlockedByAnt(item.pos))
					{
						continue;
					}
					target_pos = item.circum.pos;
				}
				else
				{
					target_pos = GetInteractPos(item, start_pos);
				}
				path = PathFind(ant.transform.position.XZ(), target_pos);
				if (path == null)
				{
					AddReserved(ReservedContext.Local, item, 60f);
					n_fails++;
					yield return null;
					continue;
				}
				gather_target = item;
				path_target_index = path.Count - 1;
				break;
			}
		}
		cantFindMaterial = !skip && gather_target.ob == null;
		if (path == null)
		{
			path = new List<Vector2> { start_pos, return_pos };
		}
		else
		{
			if ((gatherType == ExchangeType.PICKUP && gather_target.type == GatherTarget.GatherTargetType.Pickup) || gatherType == ExchangeType.FORAGE || gatherType == ExchangeType.MINE)
			{
				float num = 0f;
				for (int i = 0; i < path.Count - 1; i++)
				{
					num += (path[i] - path[i + 1]).magnitude;
				}
				float num2 = num / ant.GetSpeed() + 3f;
				if (gatherType == ExchangeType.MINE)
				{
					num2 += 10f;
				}
				AddReserved(ReservedContext.Global, gather_target, num2);
			}
			for (int num3 = path.Count - 2; num3 >= 1; num3--)
			{
				path.Add(path[num3]);
			}
			for (int j = 1; j < path.Count - 1 && j != path_target_index; j++)
			{
				Vector2 vector = path[j];
				Vector2 vector2 = ((vector - path[j - 1]).normalized + (path[j + 1] - vector).normalized) * 0.5f;
				Vector2 vector3 = new Vector2(vector2.y, 0f - vector2.x) * 0.8f;
				path[j] = vector + vector3;
				path[^j] = vector - vector3;
			}
			path.Add(return_pos);
		}
		if (!skip)
		{
			yield return new WaitForSeconds(DELAY_INITIAL);
			while (!GameManager.instance.runWorld)
			{
				yield return null;
			}
		}
		Split split = GameManager.instance.NewSplit(start_pos.To3D());
		Trail trail_start = null;
		Vector2 prev_pos = start_pos;
		for (int k = 1; k < path.Count; k++)
		{
			Trail trail = GameManager.instance.NewTrail(TrailType.COMMAND, null, ant);
			trail.commandTrailExchangeType = gatherType;
			if (trail_start == null)
			{
				trail_start = trail;
			}
			trail.SetSplitStart(split);
			Vector2 pos = path[k];
			float length = (pos - prev_pos).magnitude;
			Vector2 dir = (pos - prev_pos) / length;
			Vector3 pos2 = (prev_pos + dir * 1f).To3D();
			split = trail.NewEndSplit(pos2);
			busyTrails.Add(trail);
			for (float f = 1f; f < length; f += Time.deltaTime * DRAW_SPEED)
			{
				pos2 = (prev_pos + dir * f).To3D();
				split.transform.position = pos2;
				trail.SetEndPos(pos2);
				yield return null;
				while (!GameManager.instance.runWorld)
				{
					yield return null;
				}
			}
			pos2 = pos.To3D();
			split.transform.position = pos2;
			trail.SetEndPos(pos2);
			prev_pos = pos;
		}
		for (int l = 1; l < path.Count; l++)
		{
			List<ConnectableObject> obs = null;
			if (gather_target.ob != null && l == path_target_index)
			{
				obs = new List<ConnectableObject> { gather_target.ob };
			}
			busyTrails[l - 1].PlaceTrail(TrailStatus.PLACED, obs);
		}
		ant.SetMoveState(MoveState.Normal);
		ant.SetCurrentTrail(trail_start, 0f);
		innerAnt = null;
		CleanCommandTrails();
		foreach (Trail busyTrail in busyTrails)
		{
			commandTrails.Add(busyTrail);
		}
		busyTrails = null;
		cCreatePathForAnt = null;
	}

	private void CleanCommandTrails()
	{
		for (int num = commandTrails.Count - 1; num >= 0; num--)
		{
			if (commandTrails[num] == null)
			{
				commandTrails.RemoveAt(num);
			}
		}
	}

	private void AbortPathCreation()
	{
		if (cCreatePathForAnt == null)
		{
			return;
		}
		if (innerAnt != null)
		{
			Vector3 position = innerAnt.transform.position;
			innerAnt.transform.SetParent(null);
			innerAnt.SetCurrentTrail(null);
			innerAnt.transform.position = position;
			innerAnt.StartLaunch(new Vector3(0f, 20f, 0f), LaunchCause.LOST_FLOOR);
			innerAnt = null;
		}
		if (busyTrails != null)
		{
			List<Split> list = new List<Split>();
			if (busyTrails.Count > 0)
			{
				list.Add(busyTrails[0].splitStart);
			}
			foreach (Trail busyTrail in busyTrails)
			{
				list.Add(busyTrail.splitEnd);
			}
			foreach (Trail busyTrail2 in busyTrails)
			{
				busyTrail2.DeleteBasic();
			}
			foreach (Split item in list)
			{
				item.Delete();
			}
			busyTrails = null;
		}
		StopCoroutine(cCreatePathForAnt);
		cCreatePathForAnt = null;
	}

	public override void Relocate(Vector3 pos, Quaternion rot)
	{
		AbortPathCreation();
		commandTrails.Clear();
		base.Relocate(pos, rot);
	}

	protected override void DoDelete()
	{
		AbortPathCreation();
		base.DoDelete();
	}

	private bool BlockedByAnt(Vector2 pos)
	{
		return Physics.OverlapSphereNonAlloc(pos.To3D(), 2f, Toolkit.overlapColliders, Toolkit.Mask(Layers.Ants)) > 0;
	}

	private static Vector2 GetInteractPos(GatherTarget target, Vector2 orig)
	{
		Vector2 vector = target.pos;
		Vector2 normalized = (target.pos - orig).normalized;
		ConnectableObject ob = target.ob;
		float num = ob.GetRadius() * 1.5f + 10f;
		Vector3 origin = (target.pos - normalized * num).To3D();
		bool flag = false;
		int num2 = Physics.SphereCastNonAlloc(origin, 1f, normalized.To3D(), Toolkit.raycastHits, num, Toolkit.Mask((Layers)ob.gameObject.layer));
		float num3 = float.MaxValue;
		for (int i = 0; i < num2; i++)
		{
			RaycastHit raycastHit = Toolkit.raycastHits[i];
			float distance = raycastHit.distance;
			if (raycastHit.collider.GetComponentInParent<ClickableObject>() == ob && distance > 0f && distance < num3)
			{
				vector = raycastHit.point.XZ();
				num3 = distance;
				flag = true;
			}
		}
		if (flag)
		{
			return vector - normalized * 2.5f;
		}
		return ob.GetPosNextToOb(origin).XZ();
	}

	private List<Vector2> PathFind(Vector2 start_pos, Vector2 target_pos)
	{
		if (IsFree(start_pos, target_pos, check_ground: true))
		{
			List<Vector2> list = new List<Vector2> { start_pos };
			float magnitude = (target_pos - start_pos).magnitude;
			if (magnitude > 30f)
			{
				Vector2 vector = (target_pos - start_pos) / magnitude;
				list.Add(start_pos + vector * 10f);
				list.Add(target_pos - vector * 10f);
			}
			else if (magnitude > 20f)
			{
				list.Add((start_pos + target_pos) * 0.5f);
			}
			list.Add(target_pos);
			return list;
		}
		ground.NavReset();
		NavPoint nearestNavPoint = ground.GetNearestNavPoint(start_pos);
		NavPoint nearestFreeNavPoint = GetNearestFreeNavPoint(target_pos);
		if (nearestNavPoint == null || nearestFreeNavPoint == null)
		{
			return null;
		}
		HashSet<NavPoint> hashSet = new HashSet<NavPoint> { nearestNavPoint };
		nearestNavPoint.gScore = 0f;
		nearestNavPoint.fScore = GetCost(nearestNavPoint, nearestFreeNavPoint);
		float num = Mathf.Max(250f, nearestNavPoint.fScore * 3.3f);
		while (hashSet.Count > 0)
		{
			bool flag = false;
			NavPoint navPoint = null;
			float num2 = 1E+09f;
			foreach (NavPoint item in hashSet)
			{
				float fScore = item.fScore;
				if (fScore < num2)
				{
					num2 = fScore;
					navPoint = item;
					flag = true;
				}
			}
			if (!flag)
			{
				Debug.LogError("Nav error");
				return null;
			}
			if (navPoint == nearestFreeNavPoint)
			{
				List<NavPoint> path = ReconstructPath(navPoint, nearestNavPoint);
				Shorten(ref path, start_pos, target_pos);
				List<Vector2> list2 = new List<Vector2>();
				for (int i = 0; i < path.Count; i++)
				{
					list2.Add(path[i].pos);
				}
				if ((nearestNavPoint.pos - start_pos).magnitude > 1.5f)
				{
					list2.Insert(0, start_pos);
				}
				if ((nearestFreeNavPoint.pos - target_pos).magnitude > 1.5f)
				{
					list2.Add(target_pos);
				}
				return list2;
			}
			hashSet.Remove(navPoint);
			NavPoint[] neighbours = navPoint.neighbours;
			int num3 = navPoint.neighbourLinks;
			NavPoint navPoint2 = null;
			for (int j = 0; j < neighbours.Length; j++)
			{
				bool flag2;
				if ((num3 & 1) != 0)
				{
					flag2 = (num3 & 2) == 0;
					if (flag2)
					{
						navPoint2 = neighbours[j];
					}
				}
				else
				{
					navPoint2 = neighbours[j];
					flag2 = IsFree(navPoint.pos, navPoint2.pos);
					if (flag2)
					{
						navPoint.neighbourLinks |= 1 << j * 2;
					}
					else
					{
						navPoint.neighbourLinks |= 3 << j * 2;
					}
				}
				if (flag2)
				{
					float num4 = navPoint.gScore + GetCost(navPoint, navPoint2);
					if (!(num4 >= navPoint2.gScore))
					{
						float num5 = num4 + GetCost(navPoint2, nearestFreeNavPoint);
						if (num5 < num)
						{
							navPoint2.gScore = num4;
							navPoint2.fScore = num5;
							navPoint2.prevPoint = navPoint;
							hashSet.Add(navPoint2);
						}
					}
				}
				num3 >>= 2;
			}
		}
		return null;
	}

	private float GetCost(NavPoint a, NavPoint b)
	{
		float num = a.pos.x - b.pos.x;
		float num2 = a.pos.y - b.pos.y;
		return (float)Math.Sqrt(num * num + num2 * num2);
	}

	private bool IsFree(Vector2 p1, Vector2 p2, bool check_ground = false)
	{
		Vector2 vector = p2 - p1;
		float magnitude = vector.magnitude;
		int num = Physics.RaycastNonAlloc(p1.To3D().SetY(0.5f), (vector / magnitude).To3D(), Toolkit.raycastHits, magnitude, Toolkit.Mask(Layers.Default, Layers.Buildings, Layers.Sources, Layers.Plants, Layers.Scenery, Layers.BigPlants), QueryTriggerInteraction.Ignore);
		HashSet<ClickableObject> hashSet = new HashSet<ClickableObject>();
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = Toolkit.raycastHits[i];
			ClickableObject componentInParent = raycastHit.transform.GetComponentInParent<ClickableObject>();
			if (!hashSet.Contains(componentInParent))
			{
				hashSet.Add(componentInParent);
				if (!(componentInParent == null) && (!(componentInParent is BiomeObject biomeObject) || !biomeObject.data.trailsPassThrough) && !(componentInParent == this))
				{
					return false;
				}
			}
		}
		if (check_ground && !Trail.IsOnGround(p1.To3D(), p2.To3D()))
		{
			return false;
		}
		return true;
	}

	private NavPoint GetNearestFreeNavPoint(Vector2 target_pos)
	{
		NavPoint nearestNavPoint = ground.GetNearestNavPoint(target_pos);
		if (nearestNavPoint == null)
		{
			return null;
		}
		if (IsFree(target_pos, nearestNavPoint.pos))
		{
			return nearestNavPoint;
		}
		Vector2 vector = ground.navSquareX.XZ();
		Vector2 vector2 = ground.navSquareY.XZ();
		if ((target_pos - (nearestNavPoint.pos + vector)).sqrMagnitude > (target_pos - (nearestNavPoint.pos - vector)).sqrMagnitude)
		{
			vector = -vector;
		}
		if ((target_pos - (nearestNavPoint.pos + vector2)).sqrMagnitude > (target_pos - (nearestNavPoint.pos - vector2)).sqrMagnitude)
		{
			vector2 = -vector2;
		}
		NavPoint neighbourWithOffset = nearestNavPoint.GetNeighbourWithOffset(vector);
		if (neighbourWithOffset != null && IsFree(target_pos, neighbourWithOffset.pos))
		{
			return neighbourWithOffset;
		}
		neighbourWithOffset = nearestNavPoint.GetNeighbourWithOffset(vector2);
		if (neighbourWithOffset != null && IsFree(target_pos, neighbourWithOffset.pos))
		{
			return neighbourWithOffset;
		}
		neighbourWithOffset = nearestNavPoint.GetNeighbourWithOffset(vector + vector2);
		if (neighbourWithOffset != null && IsFree(target_pos, neighbourWithOffset.pos))
		{
			return neighbourWithOffset;
		}
		return null;
	}

	private List<NavPoint> ReconstructPath(NavPoint target_point, NavPoint start_point)
	{
		NavPoint navPoint = target_point;
		List<NavPoint> list = new List<NavPoint> { navPoint };
		for (navPoint = navPoint.prevPoint; navPoint != start_point; navPoint = navPoint.prevPoint)
		{
			list.Add(navPoint);
		}
		list.Add(navPoint);
		list.Reverse();
		return list;
	}

	private void Shorten(ref List<NavPoint> path, Vector2 start_pos, Vector2 end_pos)
	{
		List<NavPoint> list = new List<NavPoint>();
		float num = 10000f;
		bool flag;
		do
		{
			int num2 = path.Count - 1;
			flag = false;
			while (num2 > 1)
			{
				if (!list.Contains(path[num2 - 1]))
				{
					Vector2 pos = path[num2].pos;
					Vector2 pos2 = path[num2 - 2].pos;
					if ((pos - pos2).sqrMagnitude < num && IsFree(pos, pos2, check_ground: true))
					{
						path.RemoveAt(num2 - 1);
						flag = true;
					}
					else
					{
						list.Add(path[num2 - 1]);
					}
				}
				num2--;
			}
		}
		while (flag);
		Vector2 vector = NearestPosOn(end_pos, path[^2].pos, path[^1].pos);
		if (IsFree(vector, end_pos))
		{
			path[^1].pos = vector;
		}
		path[0].pos = NearestPosOn(start_pos, path[0].pos, path[1].pos);
	}

	private Vector2 NearestPosOn(Vector2 point, Vector2 start, Vector2 end)
	{
		float num = 0f;
		Vector2 vector = end - start;
		if (end != start)
		{
			num = Mathf.Clamp01(Vector2.Dot(point - start, vector) / vector.sqrMagnitude);
		}
		return start + num * vector;
	}

	public List<PickupType> GetPossiblePickups()
	{
		if (pickupPickups == null)
		{
			pickupPickups = new List<PickupType> { PickupType.NONE };
			foragePickups = new List<PickupType>();
			minePickups = new List<PickupType>();
			cutPickups = new List<PickupType>();
			foreach (PickupData pickup in PrefabData.pickups)
			{
				pickupPickups.Add(pickup.type);
			}
			HashSet<PickupType> hashSet = new HashSet<PickupType> { PickupType.NONE };
			HashSet<PickupType> hashSet2 = new HashSet<PickupType> { PickupType.NONE };
			HashSet<PickupType> hashSet3 = new HashSet<PickupType> { PickupType.NONE };
			foreach (BiomeObjectData biomeObject in PrefabData.biomeObjects)
			{
				if (biomeObject.exchangeTypes.Contains(ExchangeType.FORAGE))
				{
					hashSet.Add(biomeObject.fruit);
				}
				if (biomeObject.exchangeTypes.Contains(ExchangeType.MINE))
				{
					foreach (PickupCost pickup2 in biomeObject.pickups)
					{
						hashSet2.Add(pickup2.type);
					}
				}
				if (!biomeObject.exchangeTypes.Contains(ExchangeType.PLANT_CUT))
				{
					continue;
				}
				foreach (PickupCost pickup3 in biomeObject.pickups)
				{
					hashSet3.Add(pickup3.type);
				}
			}
			foreach (PickupType pickupPickup in pickupPickups)
			{
				if (hashSet.Contains(pickupPickup))
				{
					foragePickups.Add(pickupPickup);
				}
				if (hashSet2.Contains(pickupPickup))
				{
					minePickups.Add(pickupPickup);
				}
				if (hashSet3.Contains(pickupPickup))
				{
					cutPickups.Add(pickupPickup);
				}
			}
		}
		return gatherType switch
		{
			ExchangeType.PICKUP => pickupPickups, 
			ExchangeType.FORAGE => foragePickups, 
			ExchangeType.MINE => minePickups, 
			ExchangeType.PLANT_CUT => cutPickups, 
			_ => new List<PickupType>(), 
		};
	}

	public void ChangeGatherType()
	{
		gatherType++;
		if (gatherType > ExchangeType.MINE)
		{
			gatherType = ExchangeType.PICKUP;
		}
		ClearBillboard();
		UpdateBillboard();
	}

	public void ChangeFilter(PickupType pickup_type)
	{
		curFilter = pickup_type;
		ClearBillboard();
		UpdateBillboard();
	}

	public static TrailType ExchangeTypeToCommandTrailType(ExchangeType et)
	{
		return et switch
		{
			ExchangeType.PICKUP => TrailType.COMMAND, 
			ExchangeType.FORAGE => TrailType.COMMAND_FORAGING, 
			ExchangeType.MINE => TrailType.COMMAND_MINING, 
			ExchangeType.PLANT_CUT => TrailType.COMMAND_PLANT_CUTTING, 
			_ => TrailType.HAULING, 
		};
	}

	public IEnumerable<Trail> ECounterTrails()
	{
		if (exitSplit == null)
		{
			yield break;
		}
		foreach (Trail connectedTrail in exitSplit.connectedTrails)
		{
			yield return connectedTrail;
		}
	}

	public override int GetCounterAntCount(int entrance)
	{
		int num = 0;
		foreach (Trail commandTrail in commandTrails)
		{
			num += commandTrail.currentAnts.Count;
		}
		return num;
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (cantFindMaterial)
		{
			code_desc = "GATHERER_WARNING_FILTER" + ((searchRadius == 0f) ? "_ISLAND" : "_RANGE");
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		if (cantDoCaste)
		{
			code_desc = "GATHERER_WARNING_CASTE";
			col = Color.yellow;
			return BillboardType.EXCLAMATION_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	protected override void ClearBillboard()
	{
		cantFindMaterial = false;
		cantDoCaste = false;
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.GATHERER;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		((UIClickLayout_Gatherer)ui_building).SetupForGatherer(this);
		shownGatherType = ExchangeType.NONE;
		shownFilter = PickupType.NONE;
		shownRadius01 = -1f;
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		ui_click.UpdateButton(UIClickButtonType.Generic1, enabled: true);
		if (shownGatherType != gatherType)
		{
			shownGatherType = gatherType;
			TrailType tt = ExchangeTypeToTrailType(gatherType);
			((UIClickLayout_Gatherer)ui_click).ChangeGatherType(tt);
			UpdateRadiusIndicator();
		}
		if (shownFilter != curFilter)
		{
			shownFilter = curFilter;
			((UIClickLayout_Gatherer)ui_click).ChangeFilter(curFilter);
		}
		float num = ((searchRadius == 0f) ? 1f : Mathf.Clamp01(Mathf.InverseLerp(50f, 500f, searchRadius)));
		if (shownRadius01 != num)
		{
			shownRadius01 = num;
			((UIClickLayout_Gatherer)ui_click).ChangeRadius01(num);
		}
	}

	public override bool CanCopySettings()
	{
		return true;
	}

	public void SetSearchRadius01(float f)
	{
		searchRadius = ((f > 0.99f) ? 0f : Mathf.Lerp(50f, 500f, f / 0.99f));
		UpdateRadiusIndicator();
	}

	protected override bool HasHologram()
	{
		if (curFilter != PickupType.ANY)
		{
			return curFilter != PickupType.NONE;
		}
		return false;
	}

	public override HologramShape GetHologramShape(out PickupType _pickup, out AntCaste _ant)
	{
		_pickup = PickupType.NONE;
		_ant = AntCaste.NONE;
		if (GetCurrentBillboard(out var _, out var _, out var _, out var _) != BillboardType.NONE)
		{
			return HologramShape.None;
		}
		if (HasHologram())
		{
			_pickup = curFilter;
			return HologramShape.Pickup;
		}
		return HologramShape.QuestionMark;
	}

	public static TrailType ExchangeTypeToTrailType(ExchangeType et)
	{
		return et switch
		{
			ExchangeType.PICKUP => TrailType.HAULING, 
			ExchangeType.FORAGE => TrailType.FORAGING, 
			ExchangeType.MINE => TrailType.MINING, 
			ExchangeType.PLANT_CUT => TrailType.PLANT_CUTTING, 
			_ => TrailType.HAULING, 
		};
	}
}
