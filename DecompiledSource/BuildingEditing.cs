using System.Collections.Generic;
using UnityEngine;

public class BuildingEditing
{
	public enum EditAction
	{
		None,
		Moving,
		Rotating,
		DragPlacing
	}

	private List<Building> currentBuildings;

	private List<Building> draggingBuildings = new List<Building>();

	private HashSet<FloorTile> draggingTiles = new HashSet<FloorTile>();

	private BuildMode buildMode;

	private bool didFixedUpdate;

	private List<Building> originalBuildings;

	private List<Trail> originalTrails;

	private List<Vector3> offsetsFromMain;

	private List<float> rotationsFromMain;

	private Blueprint curBlueprint;

	private HashSet<FloorTile> curFloor;

	private float curFloorRadius;

	private bool curFloorNeedsCore;

	private int nTilePositionsOld;

	private bool rotatedLeft;

	private bool rotatedRight;

	private bool dontSnapPrev;

	private (FloorTile, Vector3, float) prevFloorSnapping;

	private List<ClickableObject> assingLineList = new List<ClickableObject>();

	private Building mainBuilding;

	private Vector3 selectionPos;

	private float mainBuildingRot;

	private bool buildingBridge;

	private BuildingConfig configCopy;

	private FloorTile snappingToTile;

	private HashSet<FloorTile> snappingToTileFloor = new HashSet<FloorTile>();

	private List<ClickableObject> obstructions = new List<ClickableObject>();

	private List<ClickableObject> clearWhenPlaced = new List<ClickableObject>();

	private List<Building> invalidBuildings = new List<Building>();

	private Building attachTarget;

	private List<TrailPart> obstructingTrailParts = new List<TrailPart>();

	private Vector3? mousePosition;

	private string hoveringError = "";

	private List<FloorTile> spawnedDraggingTiles = new List<FloorTile>();

	private bool isRelocating
	{
		get
		{
			if (buildMode != BuildMode.RelocateBuildings)
			{
				return buildMode == BuildMode.RelocateFloor;
			}
			return true;
		}
	}

	public EditAction editAction
	{
		get
		{
			if (currentBuildings == null || currentBuildings.Count == 0)
			{
				return EditAction.None;
			}
			switch (currentBuildings[0].currentStatus)
			{
			case BuildingStatus.HOVERING:
			case BuildingStatus.RELOCATE_HOVERING:
				return EditAction.Moving;
			case BuildingStatus.ROTATING:
			case BuildingStatus.RELOCATE_ROTATING:
				return EditAction.Rotating;
			case BuildingStatus.DRAG_PLACING:
				return EditAction.DragPlacing;
			default:
				return EditAction.None;
			}
		}
	}

	public void Start()
	{
		AudioManager.StartTrrr(UISfx3D.BuildingRotate);
	}

	public void Stop()
	{
		StopBuilding();
	}

	public void EditingUpdate(Vector3? mouse_position, FloorTile floor_tile)
	{
		mousePosition = mouse_position;
		bool flag = false;
		if (!mousePosition.HasValue)
		{
			for (int i = 0; i < currentBuildings.Count; i++)
			{
				currentBuildings[i].SetObActive(active: false);
			}
			for (int j = 0; j < spawnedDraggingTiles.Count; j++)
			{
				spawnedDraggingTiles[j].SetObActive(active: false);
			}
			Gameplay.instance.Clear3DCursor();
			return;
		}
		switch (editAction)
		{
		case EditAction.Moving:
		{
			SetBuildingsPosRot(mousePosition.Value, mainBuildingRot);
			float num9 = mainBuildingRot;
			bool flag2 = false;
			if (currentBuildings.Count == 1)
			{
				flag2 = SnapToAttachPoint();
			}
			if (!flag2)
			{
				if (snappingToTile != null && curFloor == null && !rotatedLeft && !rotatedRight)
				{
					if (InputManager.rotateBuildingLeft)
					{
						mainBuildingRot -= 90f;
						SetBuildingsRot(mainBuildingRot);
						AudioManager.PlayTrrr(selectionPos, 180f);
						rotatedLeft = true;
					}
					if (InputManager.rotateBuildingRight)
					{
						mainBuildingRot += 90f;
						SetBuildingsRot(mainBuildingRot);
						AudioManager.PlayTrrr(selectionPos, 180f);
						rotatedRight = true;
					}
				}
				else if (!rotatedLeft && !rotatedRight)
				{
					float num10 = 250f * Time.deltaTime * InputManager.buildRotSpeed;
					if (InputManager.rotateBuildingLeft_hold)
					{
						mainBuildingRot -= num10;
						SetBuildingsRot(mainBuildingRot);
						AudioManager.PlayTrrr(selectionPos, Mathf.Abs(num10));
					}
					if (InputManager.rotateBuildingRight_hold)
					{
						mainBuildingRot += num10;
						SetBuildingsRot(mainBuildingRot);
						AudioManager.PlayTrrr(selectionPos, Mathf.Abs(num10));
					}
				}
				if ((!InputManager.rotateBuildingLeft_hold && !InputManager.rotateBuildingRight_hold) || rotatedLeft || rotatedRight)
				{
					SetSnappingToTile(null);
					DoGridSnapping(floor_tile);
				}
				else
				{
					ClearPrevFloorSnapping();
				}
			}
			if (!InputManager.rotateBuildingLeft_hold && rotatedLeft)
			{
				rotatedLeft = false;
			}
			if (!InputManager.rotateBuildingRight_hold && rotatedRight)
			{
				rotatedRight = false;
			}
			if (mainBuildingRot != num9 && curBlueprint != null)
			{
				UpdateBlueprintNeighbors();
			}
			break;
		}
		case EditAction.Rotating:
		{
			Vector3 position = selectionPos;
			Vector3 vector = mousePosition.Value.FloorPosition(position);
			if (Vector3.Distance(vector, position) > 1f)
			{
				Quaternion rotation = Quaternion.LookRotation((vector - position).normalized);
				float y = mainBuilding.transform.localRotation.eulerAngles.y;
				mainBuilding.transform.rotation = rotation;
				SetBuildingsPosRot();
				if (snappingToTile != null && !InputManager.dontSnapHeld)
				{
					snappingToTile.SnapBuilding(mainBuilding.transform, mainBuilding.centerBetweenGridPoints);
				}
				mainBuildingRot = mainBuilding.transform.localRotation.eulerAngles.y;
				SetBuildingsRot(mainBuildingRot);
				AudioManager.PlayTrrr(position, Mathf.Abs(mainBuildingRot - y) * 2f);
			}
			if (buildingBridge && !(mainBuilding as Bridge).UpdateOtherEnd(mousePosition.Value))
			{
				flag = true;
			}
			break;
		}
		case EditAction.DragPlacing:
		{
			List<Vector3> list = new List<Vector3>();
			Vector3 position = mainBuilding.transform.position;
			Vector3 vector = mousePosition.Value.FloorPosition(position);
			float num = 20f;
			DragPlacement dragPlacement = DragPlacement.Square;
			if (mainBuilding is FloorTile floorTile)
			{
				num = floorTile.tileSize;
				dragPlacement = floorTile.dragPlacement;
			}
			Vector3 vector2 = mainBuilding.transform.rotation * Vector3.forward;
			Vector3 vector3 = mainBuilding.transform.rotation * Vector3.right;
			Vector3 lhs = vector - position;
			float num2 = Vector3.Dot(lhs, vector3) / num;
			float num3 = Vector3.Dot(lhs, vector2) / num;
			int num4 = ((num2 >= 0f) ? 1 : (-1));
			int num5 = ((num3 >= 0f) ? 1 : (-1));
			int num6 = Mathf.Abs(Mathf.RoundToInt(num2));
			if (dragPlacement == DragPlacement.LineZ || (dragPlacement == DragPlacement.Cross && Mathf.Abs(num3) > Mathf.Abs(num2)))
			{
				num6 = 0;
			}
			for (int k = 0; k <= num6; k++)
			{
				int num7 = Mathf.Abs(Mathf.RoundToInt(num3));
				if (dragPlacement == DragPlacement.LineX || (dragPlacement == DragPlacement.Cross && Mathf.Abs(num3) < Mathf.Abs(num2)))
				{
					num7 = 0;
				}
				for (int l = 0; l <= num7; l++)
				{
					if (k != 0 || l != 0)
					{
						Vector3 item = position + (float)num4 * num * (float)k * vector3 + (float)num5 * num * (float)l * vector2;
						list.Add(item);
					}
				}
			}
			if (list.Count == nTilePositionsOld)
			{
				break;
			}
			nTilePositionsOld = list.Count;
			draggingBuildings.Clear();
			draggingTiles.Clear();
			if (spawnedDraggingTiles.Count < list.Count)
			{
				int num8 = list.Count - spawnedDraggingTiles.Count;
				for (int m = 0; m < num8; m++)
				{
					Building building = SpawnBuilding(mainBuilding.data.code, mainBuilding.transform.rotation.eulerAngles.y);
					building.SetStatus(BuildingStatus.DRAG_PLACING);
					spawnedDraggingTiles.Add((FloorTile)building);
				}
			}
			draggingBuildings.Add(mainBuilding);
			draggingTiles.Add((FloorTile)mainBuilding);
			for (int n = 0; n < spawnedDraggingTiles.Count; n++)
			{
				if (n >= list.Count)
				{
					spawnedDraggingTiles[n].SetObActive(active: false);
					continue;
				}
				spawnedDraggingTiles[n].SetObActive(active: true);
				spawnedDraggingTiles[n].transform.SetPositionAndRotation(list[n], mainBuilding.transform.rotation);
				draggingTiles.Add(spawnedDraggingTiles[n]);
				draggingBuildings.Add(spawnedDraggingTiles[n]);
			}
			break;
		}
		}
		List<Building> list2 = ((editAction != EditAction.DragPlacing) ? currentBuildings : draggingBuildings);
		if (didFixedUpdate)
		{
			string error = "";
			invalidBuildings.Clear();
			obstructions.Clear();
			clearWhenPlaced.Clear();
			if (flag && !invalidBuildings.Contains(mainBuilding))
			{
				invalidBuildings.Add(mainBuilding);
			}
			Ground ground = null;
			for (int num11 = 0; num11 < list2.Count; num11++)
			{
				Building building2 = list2[num11];
				Ground ground2 = Toolkit.GetGround(building2.transform.position);
				if (ground2 == null)
				{
					if (!invalidBuildings.Contains(building2))
					{
						invalidBuildings.Add(building2);
					}
				}
				else
				{
					if (isRelocating && !Player.crossIslandBuilding && (ground2 != originalBuildings[num11].ground || building2 is Bridge))
					{
						if (!invalidBuildings.Contains(building2))
						{
							invalidBuildings.Add(building2);
						}
						if (ground2 != null)
						{
							error = Loc.GetUI("BUILDING_ERROR_RELOCATE_ISLAND");
						}
					}
					if (ground == null)
					{
						ground = ground2;
					}
					else if (ground2 != ground)
					{
						invalidBuildings.Add(building2);
					}
					if (ground2 != null && !building2.CanBeBuildOnGround(ground2, isRelocating, ref error) && !invalidBuildings.Contains(building2))
					{
						invalidBuildings.Add(building2);
					}
				}
				foreach (ClickableObject item4 in (isRelocating ? originalBuildings[num11] : building2).EAssignedObjects())
				{
					if (building2.GetAssignType() == AssignType.RETRIEVE && item4 is Building building3 && ground2 != building3.ground)
					{
						if (!invalidBuildings.Contains(building2))
						{
							invalidBuildings.Add(building2);
						}
						error = Loc.GetUI("BUILDING_ERROR_DISPENS_ISLAND");
					}
				}
			}
			if (invalidBuildings.Count == 0)
			{
				if (editAction == EditAction.DragPlacing)
				{
					GatherObstructions(draggingBuildings, ref obstructions, ref clearWhenPlaced, ref invalidBuildings);
					Vector3 zero = Vector3.zero;
					foreach (FloorTile draggingTile in draggingTiles)
					{
						zero += draggingTile.transform.position;
					}
					zero /= (float)draggingTiles.Count;
					float radius = Vector3.Distance(zero, mousePosition.Value.FloorPosition(zero));
					GatherFloorObstructions(draggingTiles, draggingBuildings, zero, radius, ref obstructions, ref clearWhenPlaced, ref invalidBuildings);
				}
				else
				{
					if (attachTarget == null)
					{
						GatherObstructions(currentBuildings, ref obstructions, ref clearWhenPlaced, ref invalidBuildings);
					}
					if (curFloor != null)
					{
						if (curFloorNeedsCore)
						{
							bool flag3 = false;
							if (snappingToTile != null)
							{
								foreach (FloorTile item5 in snappingToTile.GatherFloor(only_completed: false))
								{
									if (item5.tileType == TileType.Core && item5.currentStatus == BuildingStatus.COMPLETED)
									{
										flag3 = true;
									}
								}
							}
							if (!flag3)
							{
								error = Loc.GetUI("BUILDING_CONCRETEFLOOR_ADJACENT");
								foreach (FloorTile item6 in curFloor)
								{
									if (item6.tileType == TileType.NeedsCore && !invalidBuildings.Contains(item6))
									{
										invalidBuildings.Add(item6);
									}
								}
							}
						}
						GatherFloorObstructions(curFloor, currentBuildings, selectionPos, curFloorRadius, ref obstructions, ref clearWhenPlaced, ref invalidBuildings);
						HoverMesh hoverMesh = mainBuilding.hoverMesh;
						if (hoverMesh.HasTrails())
						{
							hoverMesh.ResetTrailErrors();
							if (obstructions.Count == 0)
							{
								foreach (var (p, p2, trailError) in hoverMesh.ETrails())
								{
									if (Trail.IsObstructed(p, p2, ref obstructions, escape: false))
									{
										hoverMesh.SetTrailError(trailError);
									}
								}
							}
							hoverMesh.ShowTrailErrors();
						}
					}
				}
			}
			if (error != hoveringError)
			{
				if (error != "")
				{
					UIHover.instance.Init(UIGame.instance);
					UIHover.instance.SetWidth();
					UIHover.instance.SetText(error, Color.red);
				}
				else
				{
					UIHover.instance.Outit(UIGame.instance);
				}
				hoveringError = error;
			}
			foreach (Building item7 in list2)
			{
				item7.hoverMesh.Highlight(!invalidBuildings.Contains(item7));
			}
			for (int num12 = 0; num12 < list2.Count; num12++)
			{
				Building building4 = list2[num12];
				building4.SetObActive(active: true);
				building4.SetVisibleHoverMesh(target: true);
				Building building5 = (isRelocating ? originalBuildings[num12] : building4);
				List<AssignLineData> list3 = new List<AssignLineData>();
				foreach (ClickableObject item8 in building5.EAssignedObjects())
				{
					if (!isRelocating || !(item8 is Building item2) || !originalBuildings.Contains(item2))
					{
						AssignType assignType = building4.GetAssignType();
						Vector3 start_pos = building4.GetAssignLinePos(assignType);
						Vector3 assignLinePos = item8.GetAssignLinePos(assignType);
						AssignLineStatus line_status = AssignLineStatus.WHITE;
						if (assignType == AssignType.CATAPULT && Vector3.Distance(building4.transform.position, item8.transform.position) > building4.AssigningMaxRange())
						{
							start_pos = item8.transform.position + Toolkit.LookVectorNormalized(item8.transform.position, building4.transform.position) * building4.AssigningMaxRange();
							start_pos.y = building4.GetAssignLinePos(assignType).y;
							line_status = AssignLineStatus.RED;
						}
						list3.Add(new AssignLineData(start_pos, assignLinePos, assignType, line_status));
					}
				}
				if (list3.Count > 0)
				{
					building4.ShowAssignLines(list3);
					if (!assingLineList.Contains(building4))
					{
						assingLineList.Add(building4);
					}
				}
				foreach (ClickableObject item9 in building5.EObjectsAssignedToThis())
				{
					if (!isRelocating || ((!(item9 is Building item3) || originalBuildings == null || !originalBuildings.Contains(item3)) && (!(item9 is TrailGate trailGate) || originalTrails == null || !originalTrails.Contains(trailGate.GetOwnerTrail()))))
					{
						item9.ShowAssignLine(building5);
						AssignType assignType2 = item9.GetAssignType();
						Vector3 assignLinePos2 = item9.GetAssignLinePos(assignType2);
						Vector3 end_pos = building4.GetAssignLinePos(assignType2);
						AssignLineStatus line_status2 = AssignLineStatus.WHITE;
						if (Vector3.Distance(building4.transform.position, item9.transform.position) > item9.AssigningMaxRange())
						{
							end_pos = item9.transform.position + Toolkit.LookVectorNormalized(item9.transform.position, building4.transform.position) * item9.AssigningMaxRange();
							end_pos.y = building4.GetAssignLinePos(assignType2).y;
							line_status2 = AssignLineStatus.RED;
						}
						item9.ShowAssignLine(assignLinePos2, end_pos, assignType2, line_status2);
						if (!assingLineList.Contains(item9))
						{
							assingLineList.Add(item9);
						}
					}
				}
			}
		}
		if (attachTarget != null)
		{
			Gameplay.instance.AddHighlight(HighlightType.OUTLINE_WHITE, attachTarget);
		}
		if (originalBuildings != null)
		{
			foreach (Building originalBuilding in originalBuildings)
			{
				Gameplay.instance.AddHighlight(HighlightType.OUTLINE_BLUE, originalBuilding);
			}
		}
		foreach (ClickableObject obstruction in obstructions)
		{
			if (!(obstruction is TrailPart))
			{
				Gameplay.instance.AddHighlight(HighlightType.OUTLINE_RED, obstruction);
			}
		}
		if (obstructions.Count == 0)
		{
			Gameplay.instance.AddHighlight(HighlightType.OUTLINE_YELLOW, clearWhenPlaced);
		}
		ClearObstructingTrails();
		foreach (ClickableObject obstruction2 in obstructions)
		{
			if (obstruction2 is TrailPart trailPart)
			{
				trailPart.SetMaterial(TrailStatus.HOVERING_ERROR);
				obstructingTrailParts.Add(trailPart);
			}
		}
		Gameplay.instance.Clear3DCursor();
		didFixedUpdate = false;
	}

	private void ClearPrevFloorSnapping()
	{
		prevFloorSnapping.Item1 = null;
	}

	private void DoGridSnapping(FloorTile floor_tile)
	{
		bool dontSnapHeld = InputManager.dontSnapHeld;
		if (!dontSnapHeld)
		{
			if (curFloor != null)
			{
				bool flag = InputManager.deltaMouse == Vector2.zero && InputManager.camMove == Vector2.zero && InputManager.camKeyRotate == 0f && !InputManager.rotateBuildingLeft_hold && !InputManager.rotateBuildingRight_hold && InputManager.zoomDelta == 0f;
				if (dontSnapHeld != dontSnapPrev)
				{
					flag = false;
				}
				if (flag)
				{
					FloorTile item = prevFloorSnapping.Item1;
					if (item != null)
					{
						float item2 = prevFloorSnapping.Item3;
						SetBuildingsPosRot(prevFloorSnapping.Item2, item2);
						SetSnappingToTile(item, get_floor: true);
						mainBuildingRot = item2;
					}
				}
				else
				{
					FloorTile item = SnapFloorTileToFloorTiles(prevFloorSnapping.Item1);
					float y = mainBuilding.transform.localRotation.eulerAngles.y;
					prevFloorSnapping = (item, selectionPos, y);
					SetSnappingToTile(item, get_floor: true);
					mainBuildingRot = y;
				}
			}
			else if (floor_tile != null)
			{
				SetSnappingToTile(floor_tile);
				floor_tile.SnapBuilding(mainBuilding.transform, mainBuilding.centerBetweenGridPoints);
				selectionPos = mainBuilding.transform.position;
				SetBuildingsPosRot();
			}
		}
		dontSnapPrev = dontSnapHeld;
	}

	private void SetSnappingToTile(FloorTile tile, bool get_floor = false)
	{
		if (get_floor && tile != null)
		{
			if (snappingToTile != tile)
			{
				snappingToTileFloor = tile.GatherFloor(only_completed: false);
			}
		}
		else
		{
			snappingToTileFloor.Clear();
		}
		snappingToTile = tile;
	}

	private void SetBuildingsPosRot(Vector3 pos, float rot_y)
	{
		selectionPos = pos;
		mainBuilding.transform.localRotation = Quaternion.Euler(0f, rot_y, 0f);
		SetBuildingsPosRot();
	}

	private void SetBuildingsPosRot()
	{
		if (currentBuildings.Count > 1)
		{
			Quaternion localRotation = mainBuilding.transform.localRotation;
			float y = localRotation.eulerAngles.y;
			for (int i = 0; i < offsetsFromMain.Count; i++)
			{
				currentBuildings[i].transform.position = selectionPos + localRotation * offsetsFromMain[i];
				currentBuildings[i].transform.localRotation = Quaternion.Euler(0f, y + rotationsFromMain[i], 0f);
			}
		}
		else
		{
			mainBuilding.transform.position = selectionPos;
		}
	}

	private void SetBuildingsPos(Vector3 pos)
	{
		selectionPos = pos;
		SetBuildingsPosRot();
	}

	public void SetBuildingsRot(float rot_y, bool external = false)
	{
		if (external)
		{
			mainBuildingRot = rot_y;
		}
		mainBuilding.transform.localRotation = Quaternion.Euler(0f, rot_y, 0f);
		SetBuildingsPosRot();
	}

	public void EditingFixedUpdate()
	{
		if (currentBuildings != null)
		{
			foreach (Building currentBuilding in currentBuildings)
			{
				currentBuilding.hoverMesh.ResetOverlap();
			}
			foreach (FloorTile spawnedDraggingTile in spawnedDraggingTiles)
			{
				spawnedDraggingTile.hoverMesh.ResetOverlap();
			}
		}
		didFixedUpdate = true;
	}

	private void GatherObstructions(List<Building> current_buildings, ref List<ClickableObject> obstructions, ref List<ClickableObject> clear_when_placed, ref List<Building> invalid_buildings)
	{
		foreach (Building current_building in current_buildings)
		{
			if (invalid_buildings.Contains(current_building))
			{
				continue;
			}
			bool flag = false;
			bool flag2 = current_building is FloorTile;
			foreach (Collider item2 in current_building.hoverMesh.EOverlaps())
			{
				Layers layer = (Layers)item2.gameObject.layer;
				if (layer == Layers.Ground || layer == Layers.IgnoreRaycast || (item2.isTrigger && layer != Layers.Trails && layer != Layers.Pickups && layer != Layers.EdgeRocks))
				{
					continue;
				}
				if (flag2)
				{
					if (layer != Layers.Plants && layer != Layers.GroundCover)
					{
						continue;
					}
				}
				else if (layer == Layers.FloorTiles || layer == Layers.GroundCover)
				{
					continue;
				}
				ClickableObject componentInParent = item2.GetComponentInParent<ClickableObject>();
				if (componentInParent == null)
				{
					continue;
				}
				bool flag3 = false;
				if (layer == Layers.Plants || layer == Layers.Pickups || layer == Layers.GroundCover)
				{
					flag3 = true;
				}
				if (layer == Layers.Corpses)
				{
					flag3 = true;
				}
				if (flag3)
				{
					if (!clear_when_placed.Contains(componentInParent))
					{
						clear_when_placed.Add(componentInParent);
					}
				}
				else if (!isRelocating || ((!(componentInParent is Building item) || !originalBuildings.Contains(item)) && (!(componentInParent is Trail trail) || (!trail.IsBuilding() && (buildMode != BuildMode.RelocateFloor || !originalTrails.Contains(trail))))))
				{
					if (!obstructions.Contains(componentInParent))
					{
						obstructions.Add(componentInParent);
					}
					if (!invalid_buildings.Contains(current_building))
					{
						invalid_buildings.Add(current_building);
					}
					flag = true;
				}
			}
			if (!flag && !(current_building is Bridge) && Toolkit.IsOverEdge(current_building.transform.position, current_building.GetRadius()))
			{
				invalid_buildings.Add(current_building);
			}
		}
		if (obstructions.Count > 0 || invalid_buildings.Count > 0)
		{
			clear_when_placed.Clear();
		}
	}

	private bool SnapToAttachPoint()
	{
		attachTarget = null;
		BuildingAttachPoint buildingAttachPoint = null;
		float num = float.MaxValue;
		float radius = mainBuilding.GetRadius();
		radius *= radius;
		foreach (Collider item in mainBuilding.hoverMesh.EOverlaps())
		{
			Layers layer = (Layers)item.gameObject.layer;
			if (layer != Layers.Buildings && layer != Layers.BuildingElement)
			{
				continue;
			}
			Building componentInParent = item.GetComponentInParent<Building>();
			if (!(componentInParent == null) && componentInParent.CanHaveAttachment(mainBuilding, mousePosition.Value, out var attach_point))
			{
				float sqrMagnitude = (mousePosition.Value - attach_point.GetPosition()).sqrMagnitude;
				if (sqrMagnitude < num && sqrMagnitude < radius * 4f)
				{
					num = sqrMagnitude;
					attachTarget = componentInParent;
					buildingAttachPoint = attach_point;
				}
			}
		}
		if (attachTarget == null)
		{
			return false;
		}
		SetBuildingsPosRot(buildingAttachPoint.GetPosition(), buildingAttachPoint.GetRotation().eulerAngles.y);
		return true;
	}

	private void SetCurFloor(bool main_is_floor)
	{
		if (main_is_floor)
		{
			SetBuildingsPosRot();
			curFloor = new HashSet<FloorTile>();
			Vector2 zero = Vector2.zero;
			foreach (Building currentBuilding in currentBuildings)
			{
				if (currentBuilding is FloorTile floorTile)
				{
					curFloor.Add(floorTile);
					zero += floorTile.transform.position.XZ();
				}
			}
			if (curFloor.Count == 0)
			{
				curFloorRadius = 0.1f;
				return;
			}
			zero /= (float)curFloor.Count;
			float num = float.MinValue;
			bool flag = false;
			bool flag2 = false;
			foreach (FloorTile item in curFloor)
			{
				float sqrMagnitude = (zero - item.transform.position.XZ()).sqrMagnitude;
				if (sqrMagnitude > num)
				{
					num = sqrMagnitude;
				}
				if (item.tileType == TileType.Core)
				{
					flag = true;
				}
				if (item.tileType == TileType.NeedsCore)
				{
					flag2 = true;
				}
			}
			curFloorNeedsCore = flag2 && !flag;
			curFloorRadius = Mathf.Sqrt(num) + curFloor.GetAny().tileSize;
		}
		else
		{
			curFloor = null;
		}
	}

	private FloorTile SnapFloorTileToFloorTiles(FloorTile cur_snap_tile)
	{
		if (!FindSnappingEdges(cur_snap_tile, out var my_edge, out var other_edge))
		{
			return null;
		}
		float num = (other_edge.angle + 180f) % 360f - my_edge.angle;
		if (num < 180f)
		{
			num -= 360f;
		}
		SetBuildingsRot(mainBuilding.transform.localRotation.eulerAngles.y + num);
		Vector3 vector = selectionPos;
		Vector3 vector2 = my_edge.pos.To3D() - vector;
		Vector3 v = vector + Quaternion.Euler(0f, num, 0f) * vector2;
		Vector2 vector3 = other_edge.pos - v.XZ();
		vector.x += vector3.x;
		vector.z += vector3.y;
		SetBuildingsPos(vector);
		return other_edge.tile;
	}

	private bool FindSnappingEdges(FloorTile cur_snap_tile, out TileEdge my_edge, out TileEdge other_edge)
	{
		my_edge = default(TileEdge);
		other_edge = default(TileEdge);
		List<TileEdge> my_edges = new List<TileEdge>();
		foreach (FloorTile item in curFloor)
		{
			foreach (TileEdge item2 in item.EEdges())
			{
				my_edges.Add(item2);
			}
		}
		if (my_edges.Count == 0)
		{
			Debug.LogError("SnapFloorTileToFloorTiles: No edges??");
			return false;
		}
		List<FloorTile> list = new List<FloorTile>();
		int num = Physics.OverlapSphereNonAlloc(selectionPos, curFloorRadius, Toolkit.overlapColliders, Toolkit.Mask(Layers.FloorTiles));
		for (int i = 0; i < num; i++)
		{
			FloorTile componentInParent = Toolkit.overlapColliders[i].GetComponentInParent<FloorTile>();
			if (!currentBuildings.Contains(componentInParent) && (!isRelocating || !originalBuildings.Contains(componentInParent)))
			{
				list.Add(componentInParent);
			}
		}
		float num2 = 25f;
		for (int j = 0; j < 2; j++)
		{
			foreach (TileEdge item3 in my_edges)
			{
				_ = item3;
				if (list.Count > 1 && list.Contains(cur_snap_tile) && GetNearbyEdge(cur_snap_tile, ref my_edges, j == 1, num2 * 3f, ref my_edge, ref other_edge))
				{
					return true;
				}
				foreach (FloorTile item4 in list)
				{
					if ((!(item4 == cur_snap_tile) || list.Count <= 1) && GetNearbyEdge(item4, ref my_edges, j == 1, num2, ref my_edge, ref other_edge))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool GetNearbyEdge(FloorTile other_tile, ref List<TileEdge> my_edges, bool internal_edges, float min_dist_sq, ref TileEdge my_edge, ref TileEdge other_edge)
	{
		bool result = false;
		foreach (TileEdge item in other_tile.EEdges(internal_edges))
		{
			foreach (TileEdge my_edge2 in my_edges)
			{
				float sqrMagnitude = (my_edge2.pos - item.pos).sqrMagnitude;
				if (sqrMagnitude < min_dist_sq)
				{
					float num = Mathf.Abs(my_edge2.angle - item.angle);
					if (Mathf.Abs(180f - num) < 45f)
					{
						my_edge = my_edge2;
						other_edge = item;
						min_dist_sq = sqrMagnitude;
						result = true;
					}
				}
			}
		}
		return result;
	}

	private void GatherFloorObstructions(HashSet<FloorTile> floor, List<Building> current_buildings, Vector3 center, float radius, ref List<ClickableObject> obstructions, ref List<ClickableObject> clear_when_placed, ref List<Building> invalid_buildings)
	{
		if (floor.Count <= 0)
		{
			return;
		}
		float tileSize = floor.GetAny().tileSize;
		float num = tileSize * tileSize * 0.8f;
		int num2 = Physics.OverlapSphereNonAlloc(center, radius, Toolkit.overlapColliders, Toolkit.Mask(Layers.FloorTiles));
		for (int i = 0; i < num2; i++)
		{
			FloorTile componentInParent = Toolkit.overlapColliders[i].GetComponentInParent<FloorTile>();
			if (!(componentInParent != null) || !componentInParent.isActiveAndEnabled || current_buildings.Contains(componentInParent) || (isRelocating && originalBuildings.Contains(componentInParent)))
			{
				continue;
			}
			bool flag = snappingToTileFloor.Contains(componentInParent);
			foreach (FloorTile item in floor)
			{
				if (!((item.transform.position - componentInParent.transform.position).XZ().sqrMagnitude < num))
				{
					continue;
				}
				if (flag)
				{
					if (!clear_when_placed.Contains(componentInParent))
					{
						clear_when_placed.Add(componentInParent);
					}
					continue;
				}
				if (!obstructions.Contains(componentInParent))
				{
					obstructions.Add(componentInParent);
				}
				if (!invalid_buildings.Contains(item))
				{
					invalid_buildings.Add(item);
				}
			}
		}
	}

	public void ClickLeftDown()
	{
		if (!mousePosition.HasValue)
		{
			return;
		}
		bool flag = false;
		if (attachTarget != null && attachTarget.CanHaveAttachment(mainBuilding, mousePosition.Value, out var attach_point))
		{
			attachTarget.SetAttachment(isRelocating ? originalBuildings[0] : mainBuilding, attach_point);
			flag = true;
		}
		else
		{
			nTilePositionsOld = -1;
			switch (editAction)
			{
			case EditAction.Moving:
				if (buildingBridge)
				{
					(mainBuilding as Bridge).SetPlacingOtherEnd(placing_other_end: true);
					foreach (Building currentBuilding in currentBuildings)
					{
						currentBuilding.SetStatus(BuildingStatus.ROTATING);
					}
					break;
				}
				if ((snappingToTile != null && curFloor != null) || Player.buildRotMode == BuildingRotationSetting.NO || (Player.buildRotMode == BuildingRotationSetting.NOT_ON_GRID && snappingToTile != null))
				{
					if (buildMode == BuildMode.PlaceFloor && currentBuildings.Count == 1 && mainBuilding is FloorTile { dragPlacement: not DragPlacement.None })
					{
						mainBuilding.SetStatus(BuildingStatus.DRAG_PLACING);
					}
					else
					{
						flag = true;
					}
					break;
				}
				foreach (Building currentBuilding2 in currentBuildings)
				{
					currentBuilding2.SetStatus(isRelocating ? BuildingStatus.RELOCATE_ROTATING : BuildingStatus.ROTATING);
				}
				break;
			case EditAction.Rotating:
				if (buildMode == BuildMode.PlaceFloor && currentBuildings.Count == 1 && mainBuilding is FloorTile { dragPlacement: not DragPlacement.None })
				{
					mainBuilding.SetStatus(BuildingStatus.DRAG_PLACING);
				}
				else
				{
					flag = true;
				}
				break;
			case EditAction.DragPlacing:
				flag = true;
				break;
			}
		}
		if (flag && obstructions.Count == 0 && invalidBuildings.Count == 0)
		{
			PlaceBuildings();
		}
	}

	private void PlaceBuildings()
	{
		HashSet<FloorTile> hashSet = null;
		FloorTile floorTile = null;
		if (mainBuilding is FloorTile floorTile2)
		{
			floorTile = floorTile2;
			hashSet = ((snappingToTile == null) ? new HashSet<FloorTile>() : snappingToTile.GatherFloor(only_completed: false));
			if (isRelocating)
			{
				foreach (Building originalBuilding in originalBuildings)
				{
					if (originalBuilding is FloorTile item)
					{
						hashSet.Add(item);
					}
				}
			}
			else
			{
				if (!hashSet.Contains(floorTile))
				{
					hashSet.Add(floorTile);
				}
				foreach (FloorTile item3 in curFloor)
				{
					if (!hashSet.Contains(item3))
					{
						hashSet.Add(item3);
					}
				}
			}
		}
		foreach (ClickableObject item4 in clearWhenPlaced)
		{
			if (floorTile != null && item4 is FloorTile item2 && hashSet.Contains(item2))
			{
				hashSet.Remove(item2);
			}
			if (item4 is Building building)
			{
				building.Demolish();
			}
			else
			{
				item4.Delete();
			}
		}
		AudioManager.EndTrrr();
		switch (buildMode)
		{
		case BuildMode.PlaceBuilding:
		case BuildMode.PlaceFloor:
		{
			mainBuilding.PlaceBuilding();
			bool flag = false;
			if (configCopy != null)
			{
				configCopy.ApplyTo(mainBuilding);
				flag = true;
			}
			mainBuilding.CheckAssigned();
			for (int num2 = spawnedDraggingTiles.Count - 1; num2 >= 0; num2--)
			{
				FloorTile floorTile3 = spawnedDraggingTiles[num2];
				if (floorTile3.gameObject.activeSelf)
				{
					floorTile3.PlaceBuilding();
					if (!hashSet.Contains(floorTile3))
					{
						hashSet.Add(floorTile3);
					}
					spawnedDraggingTiles.RemoveAt(num2);
				}
			}
			ClearDraggingTiles();
			if (floorTile != null)
			{
				floorTile.UpdateNeighbors(hashSet);
			}
			AudioManager.PlayUI(mousePosition.Value, UISfx3D.BuildingPlace);
			Building building4 = mainBuilding;
			if (building4.data.maxBuildCount > 0 && GameManager.instance.GetBuildingCount(building4.data.code) >= building4.data.maxBuildCount)
			{
				Gameplay.instance.SetActivity(Activity.NONE);
				break;
			}
			Building building5 = StartBuilding(building4);
			if (flag)
			{
				configCopy = BuildingConfig.GetConfig(building4);
			}
			{
				foreach (ClickableObject item5 in building4.EAssignedObjects())
				{
					building5.Assign(item5);
				}
				break;
			}
		}
		case BuildMode.PlaceBlueprint:
		{
			foreach (Building currentBuilding in currentBuildings)
			{
				currentBuilding.PlaceBuilding();
				curBlueprint.RetrieveData(currentBuilding);
			}
			floorTile.UpdateNeighbors(hashSet);
			foreach (BlueprintSplit split4 in curBlueprint.splits)
			{
				Split split = split4.FindBuildingSplit();
				if (split == null)
				{
					Vector3 pos = selectionPos + mainBuilding.transform.localRotation * split4.pos;
					split = GameManager.instance.NewSplit(pos);
				}
				split4.split = split;
			}
			foreach (BlueprintTrail trail2 in curBlueprint.trails)
			{
				Split split2 = curBlueprint.GetSplit(trail2.splitIdStart);
				Split split3 = curBlueprint.GetSplit(trail2.splitIdEnd);
				if (split2 == null || split3 == null)
				{
					Debug.LogError("Placing blueprint trail: invalid split");
					continue;
				}
				Trail trail = GameManager.instance.NewTrail(trail2.trailType, TrailEditing.GetGlobalGate(trail2.trailType));
				trail.SetSplitStart(split2, update_length: false);
				trail.SetSplitEnd(split3);
				trail.PlaceTrail(TrailStatus.PLACED, trail.FindNearbyConnectables());
				trail2.RetrieveData(trail);
			}
			foreach (Building currentBuilding2 in currentBuildings)
			{
				currentBuilding2.ReconnectToTrails();
			}
			AudioManager.PlayUI(mousePosition.Value, UISfx3D.BuildingPlace);
			float y = mainBuilding.transform.localRotation.eulerAngles.y;
			StartBuilding(BuildMode.PlaceBlueprint, curBlueprint, y);
			break;
		}
		case BuildMode.RelocateBuildings:
		case BuildMode.RelocateFloor:
		{
			if (currentBuildings.Count != originalBuildings.Count)
			{
				Debug.LogError("BuildingEditing: count mismatch on relocate");
				break;
			}
			if (originalTrails != null)
			{
				FloorEditing.StoreTrailsBeforeRelocating(originalBuildings);
			}
			for (int num = currentBuildings.Count - 1; num >= 0; num--)
			{
				Building building2 = currentBuildings[num];
				HandleAttachmentsOnRelocate(originalBuildings[num]);
				originalBuildings[num].Relocate(building2.transform.position, building2.transform.rotation);
				building2.Delete();
			}
			for (int i = 0; i < originalBuildings.Count; i++)
			{
				foreach (ClickableObject item6 in originalBuildings[i].EObjectsAssignedToThis())
				{
					if (item6 is Building building3)
					{
						building3.CheckAssigned();
					}
				}
			}
			if (originalTrails != null)
			{
				GameManager.instance.StartCoroutine(FloorEditing.CRelocateTrails());
			}
			if (hashSet != null && hashSet.Count > 0)
			{
				hashSet.GetAny().UpdateNeighbors(hashSet);
			}
			AudioManager.PlayUI(mousePosition.Value, UISfx3D.BuildingPlace);
			Gameplay.instance.SetActivity(Activity.NONE);
			break;
		}
		default:
			Debug.LogError($"Placing: Unhandled build mode: {buildMode}");
			break;
		}
	}

	public void CopyConfig(Building building)
	{
		configCopy = BuildingConfig.GetConfig(building);
	}

	private void HandleAttachmentsOnRelocate(Building building)
	{
		List<Building> list = new List<Building>();
		foreach (BuildingAttachPoint buildingAttachPoint in building.buildingAttachPoints)
		{
			if (buildingAttachPoint.HasAttachment(out var att) && !originalBuildings.Contains(att))
			{
				list.Add(att);
			}
		}
		Building attachParent = building.GetAttachParent();
		if (attachParent != null && !originalBuildings.Contains(attachParent) && attachParent != attachTarget)
		{
			list.Add(building);
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			list[num].ClearAttached();
		}
	}

	public Building GetCurrentBuilding()
	{
		return mainBuilding;
	}

	public EditAction GetEditAction()
	{
		return editAction;
	}

	private Building StartBuilding(Building base_on_building)
	{
		return StartBuilding(base_on_building.data.code, base_on_building.transform.localRotation.eulerAngles.y);
	}

	public Building StartBuilding(string building_code, float rot = 0f)
	{
		StopBuilding();
		Building building = SpawnBuilding(building_code, rot);
		currentBuildings = new List<Building> { building };
		buildMode = ((!(building is FloorTile)) ? BuildMode.PlaceBuilding : BuildMode.PlaceFloor);
		curBlueprint = null;
		originalTrails = null;
		CurrentBuildingsChanged();
		return building;
	}

	public void StartBuilding(BuildMode build_mode, Blueprint blueprint, float rot = 0f)
	{
		StopBuilding();
		buildMode = build_mode;
		curBlueprint = blueprint;
		currentBuildings = new List<Building>();
		offsetsFromMain = new List<Vector3>();
		rotationsFromMain = new List<float>();
		foreach (BlueprintBuilding building2 in curBlueprint.buildings)
		{
			Building building = SpawnBuilding(building2.code);
			building2.SetBuilding(building);
			currentBuildings.Add(building);
			offsetsFromMain.Add(building2.pos.To3D());
			rotationsFromMain.Add(building2.rot);
		}
		CurrentBuildingsChanged();
		Vector3 offset = selectionPos - mainBuilding.transform.position;
		mainBuilding.hoverMesh.AddTrails(blueprint.GetTrailHoverData(), mainBuilding, offset);
		if (rot != 0f)
		{
			mainBuilding.transform.rotation = Quaternion.Euler(0f, rot, 0f);
			mainBuildingRot = rot;
			SetBuildingsPosRot();
		}
		UpdateBlueprintNeighbors();
	}

	public void AddHoverTrails(List<(Vector3, Vector3, TrailType)> trail_hover_data)
	{
		mainBuilding.hoverMesh.AddTrails(trail_hover_data, null, Vector3.zero);
	}

	private void UpdateBlueprintNeighbors()
	{
		if (curFloor != null && curFloor.Count > 0)
		{
			curFloor.GetAny().UpdateNeighbors(curFloor);
		}
	}

	public static Building SpawnBuilding(string building_code, float rot = 0f)
	{
		BuildingData buildingData = BuildingData.Get(building_code);
		Building component = Object.Instantiate(buildingData.prefab, new Vector3(0f, -5000f, 0f), Quaternion.Euler(0f, rot, 0f)).GetComponent<Building>();
		component.Fill(buildingData);
		component.Init();
		return component;
	}

	public void StartRelocate(BuildMode build_mode, List<Building> buildings, List<Trail> orig_trails)
	{
		buildMode = build_mode;
		if (buildings.Count > 1)
		{
			Vector3 center = buildings.GetCenter();
			float num = float.MaxValue;
			int num2 = 0;
			for (int i = 0; i < buildings.Count; i++)
			{
				if (build_mode != BuildMode.RelocateFloor || buildings[i] is FloorTile)
				{
					float sqrMagnitude = (buildings[i].transform.position - center).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						num2 = i;
					}
				}
			}
			if (num2 > 0)
			{
				int index = num2;
				Building building = buildings[0];
				Building building2 = buildings[num2];
				Building building3 = (buildings[index] = building);
				building3 = (buildings[0] = building2);
			}
			selectionPos = ((build_mode == BuildMode.RelocateBuildings) ? buildings[0].transform.position : center);
		}
		else
		{
			selectionPos = buildings[0].transform.position;
		}
		currentBuildings = new List<Building>();
		originalBuildings = new List<Building>(buildings);
		originalTrails = ((orig_trails == null) ? null : new List<Trail>(orig_trails));
		offsetsFromMain = new List<Vector3>();
		rotationsFromMain = new List<float>();
		curBlueprint = null;
		foreach (Building building6 in buildings)
		{
			foreach (BuildingAttachPoint buildingAttachPoint in building6.buildingAttachPoints)
			{
				if (buildingAttachPoint.HasAttachment(out var att) && !originalBuildings.Contains(att))
				{
					originalBuildings.Add(att);
				}
			}
		}
		Quaternion localRotation = originalBuildings[0].transform.localRotation;
		foreach (Building originalBuilding in originalBuildings)
		{
			offsetsFromMain.Add(Quaternion.Inverse(localRotation) * (originalBuilding.transform.position - selectionPos));
			rotationsFromMain.Add(originalBuilding.transform.localRotation.eulerAngles.y - localRotation.eulerAngles.y);
			bool flag = false;
			if (originalBuilding.meshBuild != null && originalBuilding.meshBuild.activeSelf)
			{
				originalBuilding.meshBuild.SetObActive(active: false);
				flag = true;
			}
			originalBuilding.ForceDisableBillboard();
			Vector3 position = ((orig_trails != null) ? originalBuilding.transform.position : new Vector3(0f, -5000f, 0f));
			Building component = Object.Instantiate(originalBuilding, position, originalBuilding.transform.rotation).GetComponent<Building>();
			currentBuildings.Add(component);
			originalBuilding.UpdateBillboard();
			if (flag)
			{
				originalBuilding.meshBuild.SetObActive(active: true);
			}
			if (component.hoverMesh == null)
			{
				HoverMesh componentInChildren = component.GetComponentInChildren<HoverMesh>(includeInactive: true);
				if (componentInChildren != null)
				{
					Object.Destroy(componentInChildren.gameObject);
				}
			}
			component.RelocateHover();
			component.Fill(originalBuilding.data);
		}
		CurrentBuildingsChanged();
	}

	public void StopBuilding()
	{
		if (originalBuildings != null)
		{
			originalBuildings = null;
		}
		foreach (ClickableObject assingLine in assingLineList)
		{
			assingLine.HideAssignLines();
		}
		assingLineList.Clear();
		originalTrails = null;
		curBlueprint = null;
		if (currentBuildings != null)
		{
			for (int num = currentBuildings.Count - 1; num >= 0; num--)
			{
				if (!currentBuildings[num].IsPlaced())
				{
					currentBuildings[num].Delete();
				}
			}
			currentBuildings = null;
			CurrentBuildingsChanged();
		}
		ClearDraggingTiles();
		ClearObstructingTrails();
		buildMode = BuildMode.None;
	}

	private void ClearDraggingTiles()
	{
		foreach (FloorTile spawnedDraggingTile in spawnedDraggingTiles)
		{
			spawnedDraggingTile.Delete();
		}
		spawnedDraggingTiles.Clear();
		draggingBuildings.Clear();
		draggingTiles.Clear();
	}

	private void CurrentBuildingsChanged()
	{
		if (currentBuildings == null)
		{
			mainBuilding = null;
			buildingBridge = false;
			curFloor = null;
			SetCurFloor(main_is_floor: false);
		}
		else
		{
			mainBuilding = currentBuildings[0];
			mainBuildingRot = mainBuilding.transform.localRotation.eulerAngles.y;
			buildingBridge = mainBuilding is Bridge;
			SetCurFloor(mainBuilding is FloorTile);
		}
		SetSnappingToTile(null);
		ClearPrevFloorSnapping();
		configCopy = null;
	}

	private void ClearObstructingTrails()
	{
		foreach (TrailPart obstructingTrailPart in obstructingTrailParts)
		{
			if (obstructingTrailPart != null)
			{
				obstructingTrailPart.ResetMaterial();
			}
		}
		obstructingTrailParts.Clear();
	}

	public bool IsRelocating()
	{
		return isRelocating;
	}

	public void Deselect()
	{
		if (currentBuildings != null)
		{
			EditAction editAction = this.editAction;
			for (int num = currentBuildings.Count - 1; num >= 0; num--)
			{
				switch (editAction)
				{
				case EditAction.Moving:
					currentBuildings[num].Delete();
					break;
				case EditAction.Rotating:
					currentBuildings[num].SetStatus((!isRelocating) ? BuildingStatus.HOVERING : BuildingStatus.RELOCATE_HOVERING);
					break;
				case EditAction.DragPlacing:
					ClearDraggingTiles();
					currentBuildings[num].SetStatus(BuildingStatus.HOVERING);
					break;
				}
			}
			switch (editAction)
			{
			case EditAction.Moving:
				AudioManager.PlayUI(mousePosition.Value, UISfx3D.BuildingDelete);
				Gameplay.instance.SetActivity(Activity.NONE);
				break;
			case EditAction.Rotating:
				if (buildingBridge)
				{
					(mainBuilding as Bridge).SetPlacingOtherEnd(placing_other_end: false);
				}
				if (curBlueprint != null)
				{
					UpdateBlueprintNeighbors();
				}
				break;
			}
		}
		if (hoveringError != "")
		{
			hoveringError = "";
			UIHover.instance.Outit(UIGame.instance);
		}
	}
}
