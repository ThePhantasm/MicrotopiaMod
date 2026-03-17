using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloorEditing
{
	private static HashSet<Split> splitsToRelocate;

	private static HashSet<Trail> trailsToRelocate;

	private static List<Vector3> splitLocalPositions;

	private static Building refBuilding;

	public static void DeleteSelectedFloor()
	{
		List<FloorTile> selectedFloor = GetSelectedFloor();
		if (selectedFloor != null)
		{
			GetFloorContent(selectedFloor, out var buildings, out var splits, out var floating_trails);
			DeleteFloorAndContent(selectedFloor, buildings, splits, floating_trails);
			Gameplay.instance.DeselectFloorTiles();
			AudioManager.PlayUI(CamController.GetListenerPos(), UISfx3D.BuildingDelete);
		}
	}

	public static void RelocateSelectedFloor()
	{
		List<FloorTile> selectedFloor = GetSelectedFloor();
		if (selectedFloor == null)
		{
			return;
		}
		GetFloorContent(selectedFloor, out var buildings, out var splits, out var floating_trails);
		List<(Vector3, Vector3, TrailType)> list = new List<(Vector3, Vector3, TrailType)>();
		List<Trail> list2 = new List<Trail>();
		for (int i = 0; i < splits.Count; i++)
		{
			Split split = splits[i];
			foreach (Trail connectedTrail in split.connectedTrails)
			{
				if (connectedTrail.IsAction() || !connectedTrail.IsPlaced() || connectedTrail.IsBuilding() || list2.Contains(connectedTrail))
				{
					continue;
				}
				list2.Add(connectedTrail);
				Split otherSplit = connectedTrail.GetOtherSplit(split);
				if (otherSplit.IsInBuilding())
				{
					continue;
				}
				bool flag = connectedTrail.splitStart == split;
				Vector3 vector = split.transform.position;
				Vector3 vector2 = otherSplit.transform.position;
				TrailType item = connectedTrail.trailType;
				if (!splits.Contains(otherSplit))
				{
					Vector3 vector3 = FloorTile.FindEdgePos(selectedFloor, split.transform.position, otherSplit.transform.position, 1f);
					if ((split.transform.position - vector3).sqrMagnitude < 2.25f)
					{
						continue;
					}
					vector2 = vector3;
					if (connectedTrail.IsLogic() && !flag)
					{
						item = TrailType.HAULING;
					}
				}
				if (!flag)
				{
					Vector3 vector4 = vector2;
					Vector3 vector5 = vector;
					vector = vector4;
					vector2 = vector5;
				}
				list.Add((vector, vector2, item));
			}
		}
		foreach (Trail item3 in floating_trails)
		{
			Vector3 position = item3.splitStart.transform.position;
			Vector3 position2 = item3.splitEnd.transform.position;
			Vector3 vector6 = FloorTile.FindEdgePos(selectedFloor, position, position2, 1f);
			Vector3 vector7 = FloorTile.FindEdgePos(selectedFloor, position2, position, 1f);
			if (!((vector7 - vector6).sqrMagnitude < 2.25f))
			{
				TrailType item2 = (item3.IsLogic() ? TrailType.HAULING : item3.trailType);
				list.Add((vector7, vector6, item2));
				list2.Add(item3);
			}
		}
		Gameplay.instance.StartRelocate(buildings, list, list2);
	}

	public static void CreateBlueprintOfSelectedFloor()
	{
		List<FloorTile> selectedFloor = GetSelectedFloor();
		if (selectedFloor != null)
		{
			Blueprint blueprint = BlueprintManager.CreateBlueprint(selectedFloor);
			UIBlueprints uIBlueprints = UIBaseSingleton.Get(UIBlueprints.instance);
			uIBlueprints.Init();
			uIBlueprints.transform.SetAsLastSibling();
			uIBlueprints.SetEditMode(blueprint, new_creation: true);
		}
	}

	public static void DuplicateSelectedFloor()
	{
		List<FloorTile> selectedFloor = GetSelectedFloor();
		if (selectedFloor != null)
		{
			Blueprint blueprint = BlueprintManager.CreateBlueprint(selectedFloor, temporary: true);
			Gameplay.instance.SelectBlueprint(blueprint);
		}
	}

	private static List<FloorTile> GetSelectedFloor()
	{
		List<FloorTile> selectedFloorTiles = Gameplay.instance.GetSelectedFloorTiles();
		if (selectedFloorTiles == null || selectedFloorTiles.Count == 0)
		{
			return null;
		}
		return new List<FloorTile>(selectedFloorTiles);
	}

	private static void GetFloorContent(List<FloorTile> floor, out List<Building> buildings, out List<Split> splits, out List<Trail> floating_trails)
	{
		buildings = new List<Building>();
		splits = new List<Split>();
		floating_trails = new List<Trail>();
		foreach (FloorTile item in floor)
		{
			buildings.Add(item);
			_ = buildings.Count;
			item.GatherContent(ref buildings, ref splits);
		}
		foreach (FloorTile item2 in floor)
		{
			item2.GatherFloatingTrails(splits, ref floating_trails);
		}
	}

	private static void DeleteFloorAndContent(List<FloorTile> floor, List<Building> buildings, List<Split> splits, List<Trail> floating_trails)
	{
		Building.DemolishBuildings(buildings);
		List<Split> list = new List<Split>();
		List<(Trail, Vector3)> list2 = new List<(Trail, Vector3)>();
		float num = 1f;
		for (int i = 0; i < splits.Count; i++)
		{
			Split split = splits[i];
			bool flag = false;
			foreach (Trail connectedTrail in split.connectedTrails)
			{
				if (connectedTrail.IsAction() || !connectedTrail.IsPlaced() || connectedTrail.IsBuilding() || connectedTrail.IsCommandTrail())
				{
					flag = true;
					break;
				}
				Split otherSplit = connectedTrail.GetOtherSplit(split);
				if (otherSplit.IsInBuilding())
				{
					continue;
				}
				_ = connectedTrail.splitStart == split;
				int num2 = splits.IndexOf(otherSplit);
				_ = connectedTrail.trailType;
				if (num2 < 0)
				{
					Vector3 vector = FloorTile.FindEdgePos(floor, split.transform.position, otherSplit.transform.position, 0f - num);
					float sqrMagnitude = (split.transform.position - vector).sqrMagnitude;
					float sqrMagnitude2 = (otherSplit.transform.position - vector).sqrMagnitude;
					if (sqrMagnitude < 2.25f)
					{
						flag = true;
					}
					else if (!(sqrMagnitude2 < 2.25f))
					{
						list2.Add((connectedTrail, vector));
					}
				}
			}
			if (!flag)
			{
				list.Add(split);
			}
		}
		foreach (var (trail, split_pos) in list2)
		{
			trail.NewMidSplit(split_pos);
		}
		foreach (Split item in list)
		{
			item.Delete();
		}
		foreach (Trail floating_trail in floating_trails)
		{
			Vector3 position = floating_trail.splitStart.transform.position;
			Vector3 position2 = floating_trail.splitEnd.transform.position;
			Vector3 vector2 = FloorTile.FindEdgePos(floor, position, position2, 0f - num);
			Vector3 vector3 = FloorTile.FindEdgePos(floor, position2, position, 0f - num);
			if (!((vector3 - vector2).sqrMagnitude < 2.25f))
			{
				floating_trail.NewMidSplit(vector3, out var first_trail, out var second_trail);
				second_trail.NewMidSplit(vector2, out var first_trail2, out first_trail);
				first_trail2.Delete();
			}
		}
	}

	public static void StoreTrailsBeforeRelocating(List<Building> buildings)
	{
		splitsToRelocate = new HashSet<Split>();
		trailsToRelocate = new HashSet<Trail>();
		splitLocalPositions = new List<Vector3>();
		refBuilding = buildings[0];
		List<FloorTile> list = new List<FloorTile>();
		foreach (Building building in buildings)
		{
			if (building is FloorTile item)
			{
				list.Add(item);
			}
		}
		GetFloorContent(list, out var _, out var splits, out var floating_trails);
		List<(Trail, Vector3, Vector3, bool)> list2 = new List<(Trail, Vector3, Vector3, bool)>();
		float num = 1f;
		for (int i = 0; i < splits.Count; i++)
		{
			Split split = splits[i];
			bool flag = split.IsInBuilding();
			int num2 = 0;
			if (!flag)
			{
				foreach (Trail connectedTrail in split.connectedTrails)
				{
					if (connectedTrail.IsAction() || !connectedTrail.IsPlaced() || connectedTrail.IsCommandTrail())
					{
						flag = true;
						break;
					}
					num2 |= (connectedTrail.IsBuilding() ? 1 : 2);
				}
			}
			if (flag)
			{
				continue;
			}
			if (num2 == 3)
			{
				if (buildings.Contains(split.GetBuilding()))
				{
					continue;
				}
				Split split2 = GameManager.instance.NewSplit(split.transform.position);
				List<Trail> list3 = new List<Trail>();
				foreach (Trail connectedTrail2 in split.connectedTrails)
				{
					if (!connectedTrail2.IsBuilding())
					{
						list3.Add(connectedTrail2);
					}
				}
				for (int num3 = list3.Count - 1; num3 >= 0; num3--)
				{
					list3[num3].ReplaceSplit(split, split2);
				}
				split = (splits[i] = split2);
			}
			foreach (Trail connectedTrail3 in split.connectedTrails)
			{
				Split otherSplit = connectedTrail3.GetOtherSplit(split);
				if (otherSplit.IsInBuilding())
				{
					continue;
				}
				bool flag2 = connectedTrail3.splitStart == split;
				int num4 = splits.IndexOf(otherSplit);
				_ = connectedTrail3.trailType;
				if (num4 < 0)
				{
					Vector3 position = split.transform.position;
					Vector3 position2 = otherSplit.transform.position;
					Vector3 vector = FloorTile.FindEdgePos(list, position, position2);
					Vector3 vector2 = num * (position2 - position).normalized;
					Vector3 vector3 = vector - vector2;
					Vector3 vector4 = vector + vector2;
					if (!flag2)
					{
						Vector3 vector5 = vector4;
						Vector3 vector6 = vector3;
						vector3 = vector5;
						vector4 = vector6;
					}
					list2.Add((connectedTrail3, vector3, vector4, flag2));
				}
			}
			AddSplitToRelocate(split);
		}
		Trail trail_to_relocate;
		foreach (var (trail, cut_pos_start_side, cut_pos_end_side, return_start) in list2)
		{
			AddSplitToRelocate(trail.CutForRelocation(cut_pos_start_side, cut_pos_end_side, return_start, out trail_to_relocate));
		}
		foreach (Trail item2 in floating_trails)
		{
			Vector3 position3 = item2.splitStart.transform.position;
			Vector3 position4 = item2.splitEnd.transform.position;
			Vector3 vector7 = FloorTile.FindEdgePos(list, position3, position4);
			Vector3 vector8 = FloorTile.FindEdgePos(list, position4, position3);
			Vector3 vector9 = num * (position4 - position3).normalized;
			Vector3 cut_pos_start_side2 = vector8 - vector9;
			Vector3 vector10 = vector8 + vector9;
			Vector3 vector11 = vector7 - vector9;
			Vector3 cut_pos_end_side2 = vector7 + vector9;
			if (!((vector11 - vector10).sqrMagnitude < 2.25f))
			{
				AddSplitToRelocate(item2.CutForRelocation(cut_pos_start_side2, vector10, return_start: false, out var trail_to_relocate2));
				if (trail_to_relocate2 != null)
				{
					AddSplitToRelocate(trail_to_relocate2.CutForRelocation(vector11, cut_pos_end_side2, return_start: true, out trail_to_relocate));
				}
			}
		}
		foreach (Split item3 in splitsToRelocate)
		{
			foreach (Trail connectedTrail4 in item3.connectedTrails)
			{
				if (!trailsToRelocate.Contains(connectedTrail4))
				{
					trailsToRelocate.Add(connectedTrail4);
				}
			}
		}
	}

	public static bool IsRelocatingTrail(Trail trail)
	{
		if (trailsToRelocate != null)
		{
			return trailsToRelocate.Contains(trail);
		}
		return false;
	}

	private static void AddSplitToRelocate(Split split)
	{
		if (!(split == null) && splitsToRelocate.Add(split))
		{
			splitLocalPositions.Add(refBuilding.transform.InverseTransformPoint(split.transform.position));
		}
	}

	public static IEnumerator CRelocateTrails()
	{
		Transform transform = refBuilding.transform;
		int num = 0;
		foreach (Split item3 in splitsToRelocate)
		{
			Vector3 position = splitLocalPositions[num++];
			item3.transform.position = transform.TransformPoint(position);
		}
		List<(Ant, Trail)> lost_ants = new List<(Ant, Trail)>();
		HashSet<ConnectableObject> connectables = new HashSet<ConnectableObject>();
		foreach (Trail item4 in trailsToRelocate)
		{
			List<Ant> currentAnts = item4.currentAnts;
			for (int num2 = currentAnts.Count - 1; num2 >= 0; num2--)
			{
				Ant ant = currentAnts[num2];
				ant.SetCurrentTrail(null);
				lost_ants.Add((ant, item4));
			}
			item4.SetStartEndPos(item4.splitStart.transform.position, item4.splitEnd.transform.position);
			foreach (ConnectableObject nearbyConnectable in item4.nearbyConnectables)
			{
				connectables.Add(nearbyConnectable);
			}
			foreach (ConnectableObject item5 in item4.FindNearbyConnectables())
			{
				connectables.Add(item5);
			}
		}
		splitLocalPositions = null;
		trailsToRelocate = null;
		splitsToRelocate = null;
		refBuilding = null;
		yield return new WaitForSeconds(0.1f);
		foreach (ConnectableObject item6 in connectables)
		{
			if (item6 != null)
			{
				item6.DirectReconnect();
			}
		}
		foreach (var item7 in lost_ants)
		{
			Ant item = item7.Item1;
			Trail item2 = item7.Item2;
			item.returnToTrails = new List<Trail> { item2 };
			item.TryToReturn();
		}
	}
}
