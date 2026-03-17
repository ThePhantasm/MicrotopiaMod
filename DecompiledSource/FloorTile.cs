using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FloorTile : Building
{
	[Header("FloorTile")]
	public float tileSize;

	public TileType tileType;

	public DragPlacement dragPlacement;

	[SerializeField]
	private bool clickable;

	[SerializeField]
	private bool showProgressBillboard = true;

	private FloorTile[] neighbors = new FloorTile[4];

	private int[] neighborLinks = new int[4];

	private const float grid = 5f;

	private int x;

	private int y;

	public override void Read(Save save)
	{
		base.Read(save);
		for (int i = 0; i < 4; i++)
		{
			neighborLinks[i] = save.ReadInt();
		}
		if (save.version < 68)
		{
			x = int.MinValue;
		}
	}

	public override void Write(Save save)
	{
		base.Write(save);
		for (int i = 0; i < 4; i++)
		{
			save.Write((!(neighbors[i] == null)) ? neighbors[i].linkId : 0);
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		for (int i = 0; i < 4; i++)
		{
			neighbors[i] = GameManager.instance.FindLink<FloorTile>(neighborLinks[i]);
		}
	}

	protected override void SetHoverBottomButtons_old(UIHoverClickOb ui_hover)
	{
		ui_hover.SetBottomButtons(OnClickDelete, null, null);
	}

	public override bool IsClickable()
	{
		return clickable;
	}

	public override void SetHoverMode(bool hover)
	{
		base.SetHoverMode(hover);
		if (hoverMesh != null)
		{
			hoverMesh.transform.localPosition = new Vector3(0f, 0.1f, 0f);
		}
	}

	public bool ContainsPos(Vector3 pos, float allowance)
	{
		Vector3 vector = base.transform.InverseTransformPoint(pos);
		float num = 0.5f * tileSize + allowance;
		if (vector.x < 0f - num || vector.x > num || vector.z < 0f - num || vector.z > num)
		{
			return false;
		}
		return true;
	}

	public bool SnapTrail(ref Vector3 pos)
	{
		Vector3 position = base.transform.InverseTransformPoint(pos);
		float num = position.x / tileSize;
		float num2 = position.z / tileSize;
		if (Mathf.Abs(num) > 0.52f || Mathf.Abs(num2) > 0.52f)
		{
			return false;
		}
		if (num < -0.5f)
		{
			num = -0.5f;
		}
		else if (num > 0.5f)
		{
			num = 0.5f;
		}
		if (num2 < -0.5f)
		{
			num2 = -0.5f;
		}
		else if (num2 > 0.5f)
		{
			num2 = 0.5f;
		}
		float num3 = tileSize / 5f;
		position.x = Mathf.Round(num * 5f) * num3;
		position.z = Mathf.Round(num2 * 5f) * num3;
		pos = base.transform.TransformPoint(position);
		pos.y = 0f;
		return true;
	}

	public void SnapBuilding(Transform tf, bool center_between_gridpoints)
	{
		float num = tileSize / 5f;
		Vector3 position = base.transform.InverseTransformPoint(tf.position);
		if (center_between_gridpoints)
		{
			position.x = (Mathf.Round(position.x / num - 0.5f) + 0.5f) * num;
			position.z = (Mathf.Round(position.z / num - 0.5f) + 0.5f) * num;
		}
		else
		{
			position.x = Mathf.Round(position.x / num) * num;
			position.z = Mathf.Round(position.z / num) * num;
		}
		position = base.transform.TransformPoint(position);
		position.y = 0f;
		tf.position = position;
		float num2 = base.transform.localRotation.eulerAngles.y;
		float num3 = Mathf.Round((tf.rotation.eulerAngles.y - num2) / 90f) * 90f + num2;
		tf.localRotation = Quaternion.Euler(0f, num3, 0f);
	}

	private Vector2 GetOffset(TileSide side)
	{
		switch (side)
		{
		case TileSide.Left:
			return Vector2.left;
		case TileSide.Right:
			return Vector2.right;
		case TileSide.Above:
			return Vector2.up;
		case TileSide.Below:
			return Vector2.down;
		default:
			Debug.LogError($"Unknown TileSide {side}");
			return Vector2.right;
		}
	}

	public override void PlaceBuilding()
	{
		base.transform.position = base.transform.position.SetY(Random.Range(-0.01f, 0.01f));
		base.PlaceBuilding();
	}

	private int OtherSide(int i_side)
	{
		return (i_side + 2) % 4;
	}

	public HashSet<FloorTile> GatherFloor(bool only_completed)
	{
		HashSet<FloorTile> hashSet = new HashSet<FloorTile>();
		if (only_completed && currentStatus != BuildingStatus.COMPLETED)
		{
			return hashSet;
		}
		Stack<FloorTile> stack = new Stack<FloorTile>();
		stack.Push(this);
		while (stack.Count > 0)
		{
			FloorTile floorTile = stack.Pop();
			if (hashSet.Contains(floorTile))
			{
				continue;
			}
			hashSet.Add(floorTile);
			for (int i = 0; i < 4; i++)
			{
				FloorTile floorTile2 = floorTile.neighbors[i];
				if (floorTile2 != null && (!only_completed || currentStatus == BuildingStatus.COMPLETED) && !hashSet.Contains(floorTile2))
				{
					stack.Push(floorTile2);
				}
			}
		}
		return hashSet;
	}

	public static bool IsConnected(List<FloorTile> floor)
	{
		if (floor == null || floor.Count == 0)
		{
			return false;
		}
		HashSet<FloorTile> gathered = new HashSet<FloorTile>();
		HashSet<FloorTile> limited_set = floor.ToHashSet();
		floor[0].GatherFloorLimited(ref gathered, limited_set);
		return gathered.Count == floor.Count;
	}

	private void GatherFloorLimited(ref HashSet<FloorTile> gathered, HashSet<FloorTile> limited_set)
	{
		gathered.Add(this);
		for (int i = 0; i < 4; i++)
		{
			FloorTile floorTile = neighbors[i];
			if (floorTile != null && !gathered.Contains(floorTile) && limited_set.Contains(floorTile))
			{
				floorTile.GatherFloorLimited(ref gathered, limited_set);
			}
		}
	}

	public void GatherContent(ref List<Building> buildings, ref List<Split> splits)
	{
		int num = Physics.OverlapBoxNonAlloc(base.transform.position, new Vector3(tileSize * 0.5f, 5f, tileSize * 0.5f), Toolkit.overlapColliders, base.transform.rotation, Toolkit.Mask(Layers.Buildings, Layers.Splits));
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = Toolkit.overlapColliders[i].gameObject;
			Layers layer = (Layers)gameObject.layer;
			switch (layer)
			{
			case Layers.Buildings:
			{
				Building componentInParent2 = gameObject.GetComponentInParent<Building>();
				if (componentInParent2 != null && !buildings.Contains(componentInParent2) && ContainsPos(componentInParent2.transform.position, 1f))
				{
					buildings.Add(componentInParent2);
				}
				break;
			}
			case Layers.Splits:
			{
				Split componentInParent = gameObject.GetComponentInParent<Split>();
				if (componentInParent != null && !splits.Contains(componentInParent))
				{
					splits.Add(componentInParent);
				}
				break;
			}
			default:
				Debug.LogWarning($"FloorTile.GatherContent: unexpected layer {layer}, ob {gameObject.name} ({gameObject.DebugName()})", gameObject);
				break;
			}
		}
	}

	public void GatherFloatingTrails(List<Split> splits, ref List<Trail> floating_trails)
	{
		int num = Physics.OverlapBoxNonAlloc(base.transform.position, new Vector3(tileSize * 0.5f, 5f, tileSize * 0.5f), Toolkit.overlapColliders, base.transform.rotation, Toolkit.Mask(Layers.Trails));
		for (int i = 0; i < num; i++)
		{
			Trail componentInParent = Toolkit.overlapColliders[i].gameObject.GetComponentInParent<Trail>();
			if (!(componentInParent == null) && !componentInParent.IsAction() && componentInParent.IsPlaced() && !componentInParent.IsBuilding() && !componentInParent.IsCommandTrail() && !splits.Contains(componentInParent.splitStart) && !splits.Contains(componentInParent.splitEnd) && !floating_trails.Contains(componentInParent))
			{
				floating_trails.Add(componentInParent);
			}
		}
	}

	public IEnumerable<Building> EBuildings()
	{
		int n_found = Physics.OverlapBoxNonAlloc(base.transform.position, new Vector3(tileSize * 0.5f, 5f, tileSize * 0.5f), Toolkit.overlapColliders, base.transform.rotation, Toolkit.Mask(Layers.Buildings));
		for (int i = 0; i < n_found; i++)
		{
			Building componentInParent = Toolkit.overlapColliders[i].gameObject.GetComponentInParent<Building>();
			if (componentInParent != null && ContainsPos(componentInParent.transform.position, 1f))
			{
				yield return componentInParent;
			}
		}
	}

	public bool CrossesEdge(Vector3 start, Vector3 end, out Vector3 pos)
	{
		Vector2 p = base.transform.InverseTransformPoint(start).XZ();
		Vector2 vector = base.transform.InverseTransformPoint(end).XZ();
		if (p.x == vector.x)
		{
			p.x += 0.01f;
		}
		if (p.y == vector.y)
		{
			p.y += 0.01f;
		}
		float num = tileSize * 0.5f;
		Vector2[] array = new Vector2[4]
		{
			new Vector2(0f - num, num),
			new Vector2(num, num),
			new Vector2(num, 0f - num),
			new Vector2(0f - num, 0f - num)
		};
		float num2 = float.MaxValue;
		Vector2 vector2 = Vector2.zero;
		for (int i = 0; i < 4; i++)
		{
			if (LineIntersectsEdge(p, vector, array[i], array[(i + 1) % 4], out var intersection))
			{
				float sqrMagnitude = (intersection - vector).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					vector2 = intersection;
				}
			}
		}
		if (vector2 == Vector2.zero)
		{
			pos = Vector3.zero;
			return false;
		}
		pos = base.transform.TransformPoint(new Vector3(vector2.x, 0f, vector2.y));
		pos.y = (start.y + end.y) * 0.5f;
		return true;
	}

	private static bool LineIntersectsEdge(Vector2 p1, Vector2 p2, Vector2 edgeStart, Vector2 edgeEnd, out Vector2 intersection)
	{
		Vector2 vector = p2 - p1;
		Vector2 vector2 = edgeEnd - edgeStart;
		float num = vector.x * vector2.y - vector.y * vector2.x;
		float num2 = ((edgeStart - p1).x * vector2.y - (edgeStart - p1).y * vector2.x) / num;
		float num3 = ((edgeStart - p1).x * vector.y - (edgeStart - p1).y * vector.x) / num;
		if (Mathf.Abs(num) > Mathf.Epsilon && num2 >= 0f && num2 <= 1f && num3 >= 0f && num3 <= 1f)
		{
			intersection = p1 + num2 * vector;
			return true;
		}
		intersection = Vector2.zero;
		return false;
	}

	protected override void DoDelete()
	{
		for (int i = 0; i < 4; i++)
		{
			FloorTile floorTile = neighbors[i];
			if (floorTile != null)
			{
				floorTile.neighbors[OtherSide(i)] = null;
				neighbors[i] = null;
			}
		}
		base.DoDelete();
	}

	public IEnumerable<TileEdge> EEdges(bool internal_edges = false)
	{
		if (x == int.MinValue)
		{
			UpdateNeighbors(GatherFloor(only_completed: false));
		}
		float a = GetCorrectedAngle(base.transform);
		for (int s = 0; s < 4; s++)
		{
			TileEdge edge = GetEdge((TileSide)s, a);
			if (internal_edges)
			{
				if (neighbors[s] != null)
				{
					yield return edge;
				}
				else
				{
					yield return edge.Inverse();
				}
			}
			else if (neighbors[s] == null)
			{
				yield return edge;
			}
		}
	}

	private float GetCorrectedAngle(Transform tf)
	{
		float num = (tf.localRotation.eulerAngles.y + 360f) % 90f;
		if (num > 45f)
		{
			num -= 90f;
		}
		return num;
	}

	public TileEdge GetEdge(TileSide side, float a)
	{
		TileEdge result = default(TileEdge);
		result.tile = this;
		Vector2 offset = GetOffset(side);
		Vector3 vector = Quaternion.Euler(0f, a, 0f) * new Vector3(offset.x, 0f, offset.y);
		result.pos = (base.transform.position + 0.5f * tileSize * vector).XZ();
		result.angle = (a + 90f * (float)side) % 360f;
		return result;
	}

	public void UpdateNeighbors(HashSet<FloorTile> floor)
	{
		_UpdateNeighbors(floor, tileSize, GetCorrectedAngle(floor.GetAny().transform));
	}

	private static void _UpdateNeighbors(HashSet<FloorTile> floor, float tile_size, float angle)
	{
		Quaternion quaternion = Quaternion.Euler(0f, 0f - angle, 0f);
		foreach (FloorTile item in floor)
		{
			Vector3 vector = item.transform.position - floor.GetAny().transform.position;
			Vector2 vector2 = (quaternion * vector).XZ();
			item.x = Mathf.RoundToInt(vector2.x / tile_size);
			item.y = Mathf.RoundToInt(vector2.y / tile_size);
			item.ResetNeighbors();
		}
		Vector2Int vector2Int = new Vector2Int(int.MaxValue, int.MaxValue);
		Vector2Int vector2Int2 = new Vector2Int(int.MinValue, int.MinValue);
		foreach (FloorTile item2 in floor)
		{
			if (item2.x < vector2Int.x)
			{
				vector2Int.x = item2.x;
			}
			if (item2.x > vector2Int2.x)
			{
				vector2Int2.x = item2.x;
			}
			if (item2.y < vector2Int.y)
			{
				vector2Int.y = item2.y;
			}
			if (item2.y > vector2Int2.y)
			{
				vector2Int2.y = item2.y;
			}
		}
		int num = 1 + vector2Int2.x - vector2Int.x;
		int num2 = 1 + vector2Int2.y - vector2Int.y;
		FloorTile[,] array = new FloorTile[num, num2];
		foreach (FloorTile item3 in floor)
		{
			array[item3.x - vector2Int.x, item3.y - vector2Int.y] = item3;
		}
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				FloorTile floorTile = array[i, j];
				if (floorTile == null)
				{
					continue;
				}
				if (i < num - 1)
				{
					FloorTile floorTile2 = array[i + 1, j];
					if (floorTile2 != null)
					{
						floorTile.SetNeighbor(TileSide.Right, floorTile2);
					}
				}
				if (j < num2 - 1)
				{
					FloorTile floorTile3 = array[i, j + 1];
					if (floorTile3 != null)
					{
						floorTile.SetNeighbor(TileSide.Above, floorTile3);
					}
				}
			}
		}
	}

	private void SetNeighbor(TileSide side, FloorTile neighbor)
	{
		neighbors[(int)side] = neighbor;
		neighbor.neighbors[OtherSide((int)side)] = this;
	}

	private void ResetNeighbors()
	{
		neighbors[0] = (neighbors[1] = (neighbors[2] = (neighbors[3] = null)));
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public static Vector3 FindEdgePos(List<FloorTile> floor, Vector3 start, Vector3 end, float nudge_dist = 0f)
	{
		Vector3 vector = ((nudge_dist == 0f) ? Vector3.zero : (nudge_dist * (start - end).normalized));
		float num = float.MaxValue;
		Vector3 result = Vector3.zero;
		foreach (FloorTile item in floor)
		{
			if (item.CrossesEdge(start, end, out var pos))
			{
				pos += vector;
				float sqrMagnitude = (end - pos).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = pos;
				}
			}
		}
		return result;
	}

	protected override bool ShowProgressBillboard()
	{
		return showProgressBillboard;
	}
}
