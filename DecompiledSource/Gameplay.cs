using System;
using System.Collections.Generic;
using UnityEngine;

public class Gameplay : Singleton
{
	public static Gameplay instance;

	[SerializeField]
	private Texture2D texSelectRect;

	private MouseCursor mouseCursor;

	public static int N_TRAIL_DESTROYED = 0;

	public static bool CAN_PLACE_QUEEN = true;

	public static bool REFRESH_UNLOCKS = false;

	private ClickableObject obUnderMouse;

	private ClickableObject obUnderMouse_old;

	private ClickableObject obHovering;

	private List<ClickableObject> selectedObjects;

	private List<ClickableObject> selectedObjectsPrev;

	[NonSerialized]
	public bool mouseInScene;

	private List<ClickableObject> highlightedObs = new List<ClickableObject>();

	private List<ClickableObject> highlightedObsPrev = new List<ClickableObject>();

	private Vector2 rectSelectStartPos;

	private bool isRectSelecting;

	private UIHoverClickOb uiHoverClickOb;

	[NonSerialized]
	public BuildingGroup currentGroup;

	private LayerMask mouseFindLayers;

	private Activity activity;

	private TrailEditing trailEditing;

	private BuildingEditing buildingEditing;

	private BuildingAssigning buildingAssigning;

	private List<AssignLine> globalAssignLines = new List<AssignLine>();

	private bool updateAssingLines;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init()
	{
		if (mouseCursor != null)
		{
			UnityEngine.Object.Destroy(mouseCursor);
			mouseCursor = null;
		}
		mouseCursor = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(MouseCursor))).GetComponent<MouseCursor>();
		mouseCursor.Clear();
		mouseFindLayers = Toolkit.Mask(Layers.Ants, Layers.Buildings, Layers.Trails, Layers.Sources, Layers.Plants, Layers.Scenery, Layers.Pickups, Layers.BuildingElement, Layers.Splits, Layers.FloorTiles, Layers.Ground, Layers.Corpses, Layers.BigPlants);
		trailEditing = new TrailEditing();
		trailEditing.Init();
		buildingEditing = new BuildingEditing();
		buildingAssigning = new BuildingAssigning();
		selectedObjects = new List<ClickableObject>();
		selectedObjectsPrev = new List<ClickableObject>();
	}

	public void StopPlaying()
	{
		SetActivity(Activity.NONE, force: true);
	}

	public void GameplayFixedUpdate()
	{
		if (activity == Activity.BUILDING)
		{
			buildingEditing.EditingFixedUpdate();
		}
	}

	public void GameplayLateUpdate()
	{
		if (REFRESH_UNLOCKS)
		{
			REFRESH_UNLOCKS = false;
			RefreshUnlocks();
		}
	}

	public void GameplayUpdate()
	{
		mouseInScene = InputManager.MouseInScene();
		Vector3? mouse_position = null;
		Vector3? mouse_position2 = null;
		if (mouseInScene)
		{
			Ray ray = Camera.main.ScreenPointToRay(InputManager.mousePosition);
			if (ray.direction.y < 0f)
			{
				mouse_position = ray.GetPointAtY(0f);
				mouse_position2 = ray.GetPointAtY(GlobalValues.standard.trailHeight).SetY(0f);
			}
		}
		obUnderMouse = null;
		FloorTile floor_tile = null;
		if (mouseInScene && !InputManager.camDragRotate.HasValue)
		{
			Ray ray2 = Camera.main.ScreenPointToRay(InputManager.mousePosition);
			int num = Physics.SphereCastNonAlloc(ray2, 0.5f, Toolkit.raycastHits, 5000f, mouseFindLayers);
			ClickableObject clickableObject = null;
			float num2 = float.MaxValue;
			Layers layers = Layers.IgnoreRaycast;
			float num3 = float.MaxValue;
			for (int i = 0; i < num; i++)
			{
				RaycastHit raycastHit = Toolkit.raycastHits[i];
				Layers layer = (Layers)raycastHit.transform.gameObject.layer;
				if (layer == Layers.FloorTiles)
				{
					FloorTile componentInParent = raycastHit.transform.gameObject.GetComponentInParent<FloorTile>();
					if (componentInParent.currentStatus == BuildingStatus.COMPLETED || trailEditing.GetTrailType() == TrailType.FLOOR_DEMOLISH || trailEditing.GetTrailType() == TrailType.FLOOR_SELECTOR)
					{
						float sqrMagnitude = (ray2.GetPointAtY(0.5f).XZ() - componentInParent.transform.position.XZ()).sqrMagnitude;
						if (sqrMagnitude < num3)
						{
							floor_tile = componentInParent;
							num3 = sqrMagnitude;
						}
					}
				}
				if (PickingOverrides(layers, layer) && layers != Layers.IgnoreRaycast)
				{
					continue;
				}
				float sqrMagnitude2 = (ray2.origin - raycastHit.point).sqrMagnitude;
				if (!(sqrMagnitude2 < num2) && !PickingOverrides(layer, layers))
				{
					continue;
				}
				ClickableObject clickableObject2;
				if (layer == Layers.Ground)
				{
					clickableObject2 = null;
				}
				else
				{
					clickableObject2 = raycastHit.transform.gameObject.GetComponentInParent<ClickableObject>();
					if (clickableObject2 == null || !clickableObject2.IsClickable() || clickableObject2 == buildingEditing.GetCurrentBuilding())
					{
						continue;
					}
				}
				layers = layer;
				clickableObject = clickableObject2;
				num2 = sqrMagnitude2;
			}
			if (clickableObject != null)
			{
				obUnderMouse = clickableObject;
			}
		}
		switch (activity)
		{
		case Activity.TRAIL_EDITING:
			trailEditing.EditingUpdate(mouse_position2, ref obUnderMouse, floor_tile);
			if (!trailEditing.IsDrawingTrail())
			{
				break;
			}
			foreach (Ant item in trailEditing.ECurrentTrailOwners())
			{
				AddHighlight(HighlightType.OUTLINE_WHITE, item);
			}
			break;
		case Activity.BUILDING:
			buildingEditing.EditingUpdate(mouse_position, floor_tile);
			break;
		case Activity.BUILDING_ASSIGNING:
			buildingAssigning.AssigningUpdate(mouse_position, obUnderMouse);
			break;
		default:
			trailEditing.EditingUpdate(mouse_position2, ref obUnderMouse, floor_tile);
			if (trailEditing.IsDragging())
			{
				break;
			}
			if (obUnderMouse != null)
			{
				AddHighlight(HighlightType.INNERGLOW_WHITE_STRONG, obUnderMouse);
				obUnderMouse.GetCurrentBillboard(out var code_desc, out var _, out var col, out var _);
				if (code_desc == "")
				{
					if (obHovering != null)
					{
						UIHover.instance.Outit(obHovering);
						obHovering = null;
					}
				}
				else if (obHovering != obUnderMouse)
				{
					if (obHovering != null)
					{
						UIHover.instance.Outit(obHovering);
					}
					obHovering = obUnderMouse;
					UIHover.instance.Init(obHovering);
					UIHover.instance.SetText(Loc.GetUI(code_desc), col);
				}
			}
			else if (obHovering != null)
			{
				UIHover.instance.Outit(obHovering);
				obHovering = null;
			}
			if (obUnderMouse != obUnderMouse_old)
			{
				if (obUnderMouse_old != null)
				{
					obUnderMouse_old.OnHoverExit();
				}
				if (obUnderMouse != null)
				{
					obUnderMouse.OnHoverEnter();
				}
				obUnderMouse_old = obUnderMouse;
			}
			break;
		}
		if (mouseInScene)
		{
			if (InputManager.selectDown)
			{
				switch (activity)
				{
				case Activity.TRAIL_EDITING:
					trailEditing.ClickLeftDown();
					break;
				case Activity.BUILDING:
					buildingEditing.ClickLeftDown();
					break;
				case Activity.BUILDING_ASSIGNING:
					buildingAssigning.ClickLeftDown();
					break;
				default:
					if (obUnderMouse != null)
					{
						Ant ant = obUnderMouse as Ant;
						if ((object)ant != null)
						{
							switch (ant.moveState)
							{
							default:
								if (ant.ShouldPlayClickAudio())
								{
									AudioManager.PlayUI(mouse_position.Value, UISfx3D.AntSelect);
								}
								if (ant is CargoAnt cargoAnt)
								{
									ant = cargoAnt.centipedeHead;
								}
								if (ant.CanGetCommand())
								{
									SetTrailType(TrailType.COMMAND);
									trailEditing.CreateNewTrailsFromAnts(new List<Ant> { ant });
									Select(ant);
								}
								break;
							case MoveState.Animated:
							case MoveState.Carried:
							case MoveState.Disabled:
							case MoveState.Waiting:
							case MoveState.DeadAndDisabled:
								break;
							}
						}
						else if (obUnderMouse is Building)
						{
							if (obUnderMouse.ShouldPlayClickAudio())
							{
								AudioManager.PlayUI(obUnderMouse.transform.position, UISfx3D.BuildingSelect);
							}
						}
						else if (obUnderMouse is BiomeObject && obUnderMouse.ShouldPlayClickAudio())
						{
							AudioManager.PlayUI(obUnderMouse.transform.position, UISfx3D.BiomeObjectSelect);
						}
						Select(obUnderMouse);
					}
					else
					{
						Select(null);
						trailEditing.ClickLeftDown();
					}
					break;
				}
			}
			if (InputManager.selectHeld)
			{
				switch (activity)
				{
				case Activity.TRAIL_EDITING:
					trailEditing.ClickLeft();
					break;
				default:
					trailEditing.ClickLeft();
					break;
				case Activity.BUILDING:
				case Activity.BUILDING_ASSIGNING:
					break;
				}
			}
			if (InputManager.selectUp && !InputManager.deselectCombined && !InputManager.deselectLoose)
			{
				switch (activity)
				{
				case Activity.TRAIL_EDITING:
					trailEditing.ClickLeftUp();
					break;
				default:
					trailEditing.ClickLeftUp();
					break;
				case Activity.BUILDING:
				case Activity.BUILDING_ASSIGNING:
					break;
				}
			}
			if (InputManager.deselectLoose || (InputManager.deselectCombined && !CamController.instance.IsDraggingCam() && !CamController.instance.IsDragRotatingCam()))
			{
				Select(null);
				if (trailEditing.IsDragging())
				{
					trailEditing.ClickLeftUp(cancel: true);
				}
				switch (activity)
				{
				case Activity.BUILDING:
					buildingEditing.Deselect();
					break;
				case Activity.TRAIL_EDITING:
					trailEditing.Deselect();
					break;
				case Activity.BUILDING_ASSIGNING:
					buildingAssigning.Deselect();
					break;
				default:
					ClearFocus();
					break;
				}
			}
			if (InputManager.pipette && activity == Activity.NONE && Pipette(trailEditing.GetHoveringTrailPart(), obUnderMouse, floor_tile))
			{
				AudioManager.PlayUI(UISfx.BuildingMenuButtonClick);
			}
		}
		MouseCursorType hardwareCursor = MouseCursorType.Normal;
		if (!mouseInScene)
		{
			hardwareCursor = MouseCursorType.Normal;
		}
		else if (InputManager.camDragRotate.HasValue)
		{
			hardwareCursor = MouseCursorType.CameraRotate;
		}
		else if (InputManager.camDragHeldCombined || InputManager.camDragHeldLoose)
		{
			hardwareCursor = MouseCursorType.CameraMove;
		}
		else if (InputManager.pipetteHold)
		{
			hardwareCursor = MouseCursorType.Pipette;
		}
		else if (trailEditing.GetTrailType() != TrailType.NONE && mouseCursor.IsVisible())
		{
			hardwareCursor = trailEditing.GetTrailType() switch
			{
				TrailType.TRAIL_ERASER => MouseCursorType.TrailErase, 
				TrailType.TRAIL_ERASER_BIG => MouseCursorType.TrailErase, 
				TrailType.FLOOR_DEMOLISH => MouseCursorType.TrailErase, 
				TrailType.FLOOR_SELECTOR => MouseCursorType.Normal, 
				_ => MouseCursorType.TrailDraw, 
			};
		}
		else if (buildingEditing.GetEditAction() != BuildingEditing.EditAction.None)
		{
			switch (buildingEditing.GetEditAction())
			{
			case BuildingEditing.EditAction.Moving:
				hardwareCursor = MouseCursorType.BuildingPlace;
				break;
			case BuildingEditing.EditAction.Rotating:
				hardwareCursor = MouseCursorType.BuildingRotate;
				break;
			}
		}
		else if (trailEditing.GetHoverType() == TrailEditing.HoverType.TrailPart || trailEditing.IsDragging())
		{
			hardwareCursor = (InputManager.selectHeld ? MouseCursorType.DragHold : MouseCursorType.Drag);
		}
		UIGlobal.SetHardwareCursor(hardwareCursor);
		AddHighlight(HighlightType.OUTLINE_WHITE, selectedObjects);
		UpdateHighlights();
		UpdateSelected();
		if (updateAssingLines)
		{
			foreach (AssignLine globalAssignLine in globalAssignLines)
			{
				if (globalAssignLine != null && globalAssignLine.isActiveAndEnabled)
				{
					globalAssignLine.UpdateLine();
				}
			}
		}
		if (InputManager.delete)
		{
			if (UIFloorSelection.IsActive())
			{
				UIFloorSelection.instance.OnClickDelete();
			}
			else if (selectedObjects.Count > 0 && selectedObjects[0] is Building)
			{
				Building.DemolishBuildings(new List<Building>(ESelectedBuildings()));
			}
			else if (selectedObjects.Count > 0)
			{
				foreach (ClickableObject item2 in new List<ClickableObject>(selectedObjects))
				{
					item2.OnClickDelete();
				}
				AudioManager.PlayUI(CamController.GetListenerPos(), UISfx3D.BuildingDelete);
			}
		}
		if (InputManager.followAnt)
		{
			if (selectedObjects.Count > 0 && selectedObjects[0] is Building)
			{
				foreach (Building item3 in ESelectedBuildings())
				{
					if (item3.CanTrack())
					{
						UIGame.instance.TrackBuilding(item3);
					}
				}
			}
			else
			{
				List<Transform> list = new List<Transform>();
				foreach (ClickableObject selectedObject in selectedObjects)
				{
					if (selectedObject is Ant ant2)
					{
						list.Add(ant2.transform);
					}
					else if (selectedObject is Pickup pickup && pickup.IsLarva())
					{
						list.Add(pickup.transform);
					}
				}
				int num4 = -1;
				Transform followTarget = CamController.instance.GetFollowTarget();
				if (followTarget != null)
				{
					for (int j = 0; j < list.Count; j++)
					{
						if (followTarget == list[j])
						{
							num4 = j;
						}
					}
				}
				num4++;
				SetActivity(Activity.NONE);
				CamController.instance.ToggleFollow((num4 == list.Count) ? null : list[num4].transform);
			}
		}
		if (InputManager.relocate)
		{
			if (UIFloorSelection.IsActive())
			{
				UIFloorSelection.instance.OnClickRelocate();
			}
			else if (buildingEditing.IsRelocating())
			{
				buildingEditing.Deselect();
			}
			else if (selectedObjects.Count > 0 && selectedObjects[0] is Building)
			{
				StartRelocate(new List<Building>(ESelectedBuildings()));
			}
		}
		if (InputManager.dropPickup)
		{
			foreach (Ant item4 in ESelectedAnts())
			{
				item4.DropPickupsOnGround();
			}
			foreach (Building item5 in ESelectedBuildings())
			{
				if (item5 is Catapult catapult)
				{
					catapult.ClearAssigned();
				}
			}
		}
		if (InputManager.interactBuilding)
		{
			foreach (ClickableObject item6 in ESelectedObs())
			{
				if (item6 is Building building)
				{
					if (building is Stockpile stockpile)
					{
						stockpile.StartSendToOtherStockpile();
						break;
					}
					if (building is DispenserRegular ob)
					{
						StartAssign(ob, AssignType.RETRIEVE);
						break;
					}
					if (building is Catapult ob2)
					{
						StartAssign(ob2, AssignType.CATAPULT);
						break;
					}
					if (building is FlightPad { launchPad: not false } flightPad)
					{
						StartAssign(flightPad, AssignType.FLIGHT);
						break;
					}
				}
				if (item6 is TrailGate_Stockpile ob3)
				{
					StartAssign(ob3, AssignType.GATE);
					break;
				}
			}
		}
		if (InputManager.placeDispenser)
		{
			foreach (Building item7 in ESelectedBuildings())
			{
				if (item7.CanDispense())
				{
					item7.PlaceDispenser();
					break;
				}
			}
		}
		if (InputManager.copySettings)
		{
			if (UIFloorSelection.IsActive())
			{
				UIFloorSelection.instance.OnClickBlueprint();
			}
			else if (selectedObjects.Count == 1 && selectedObjects[0] is TrailGate gate)
			{
				CopySettings(gate);
			}
			else
			{
				using IEnumerator<Building> enumerator4 = ESelectedBuildings().GetEnumerator();
				while (enumerator4.MoveNext() && !CopySettings(enumerator4.Current))
				{
				}
			}
		}
		if (!InputManager.pasteSettings)
		{
			return;
		}
		if (UIFloorSelection.IsActive())
		{
			UIFloorSelection.instance.OnClickDuplicate();
			return;
		}
		if (selectedObjects.Count == 1 && selectedObjects[0] is TrailGate gate2)
		{
			PasteSettings(gate2);
			return;
		}
		List<Building> list2 = new List<Building>();
		foreach (Building item8 in ESelectedBuildings())
		{
			list2.Add(item8);
		}
		PasteSettings(list2);
	}

	public static bool CopySettings(Building building)
	{
		if (!BuildingConfig.CopyToClipboard(building))
		{
			return false;
		}
		AudioManager.PlayUI(building.transform.position, UISfx3D.CopySettings);
		return true;
	}

	public static bool CopySettings(TrailGate gate)
	{
		if (!TrailGate.CopyToClipboard(gate))
		{
			return false;
		}
		AudioManager.PlayUI(gate.transform.position, UISfx3D.CopySettings);
		return true;
	}

	public static bool PasteSettings(List<Building> buildings)
	{
		int num = 0;
		foreach (Building building in buildings)
		{
			if (BuildingConfig.PasteClipboard(building))
			{
				if (num == 0)
				{
					AudioManager.PlayUI(building.transform.position, UISfx3D.PasteSettings);
				}
				num++;
			}
			building.OnConfigPaste();
		}
		if (num == 1)
		{
			instance.UpdateSelected(refresh: true);
		}
		return num > 0;
	}

	public static bool PasteSettings(TrailGate gate)
	{
		if (!TrailGate.PasteClipboard(gate))
		{
			return false;
		}
		gate.OnConfigPaste();
		AudioManager.PlayUI(gate.transform.position, UISfx3D.PasteSettings);
		instance.UpdateSelected(refresh: true);
		return true;
	}

	private bool Pipette(TrailPart trail_part, ClickableObject clickable_object, FloorTile floor_tile)
	{
		if (clickable_object != null && clickable_object is Building building)
		{
			BuildingData buildingData = BuildingData.Get(building.data.code);
			if (buildingData.maxBuildCount <= 0 || GameManager.instance.GetBuildingCount(buildingData.code) < buildingData.maxBuildCount)
			{
				Pipette(building);
				return true;
			}
		}
		TrailGate trailGate = null;
		Trail trail = trail_part as Trail;
		if (trail != null && trail.IsInBuilding())
		{
			trail = null;
		}
		Split split = trail_part as Split;
		if (split != null && split.IsInBuilding())
		{
			split = null;
		}
		if (trail != null)
		{
			trailGate = trail.trailGate;
		}
		else if (split != null)
		{
			foreach (Trail connectedTrail in split.connectedTrails)
			{
				if (connectedTrail.splitStart == split && !connectedTrail.IsInBuilding() && connectedTrail.IsGate())
				{
					trailGate = connectedTrail.trailGate;
				}
			}
		}
		if (trailGate == null && clickable_object != null)
		{
			trailGate = clickable_object as TrailGate;
		}
		if (trailGate != null)
		{
			Pipette(trailGate);
			return true;
		}
		if (trail != null)
		{
			Pipette(trail.trailType);
			return true;
		}
		if (split != null)
		{
			for (int i = 0; i < 2; i++)
			{
				foreach (Trail connectedTrail2 in split.connectedTrails)
				{
					if (!connectedTrail2.IsInBuilding() && (i == 1 || connectedTrail2.splitStart == split))
					{
						Pipette(connectedTrail2.trailType);
						return true;
					}
				}
			}
		}
		if (floor_tile != null)
		{
			Pipette(floor_tile);
			return true;
		}
		return false;
	}

	private void Pipette(Building building)
	{
		UIGame.instance.uiBuildingMenu.OnClickBuildingButton(building.data.code);
		buildingEditing.CopyConfig(building);
		buildingEditing.SetBuildingsRot(building.transform.rotation.eulerAngles.y, external: true);
	}

	private void Pipette(TrailGate gate)
	{
		trailEditing.CopyConfig(gate);
		UIGame.instance.uiBuildingMenu.OnClickTrailButton(gate.GetTrailType());
	}

	private void Pipette(TrailType trail_type)
	{
		UIGame.instance.uiBuildingMenu.OnClickTrailButton(trail_type);
	}

	private void Pipette(FloorTile floor_tile)
	{
		UIGame.instance.uiBuildingMenu.OnClickBuildingButton(floor_tile.data.code);
		buildingEditing.SetBuildingsRot(floor_tile.transform.rotation.eulerAngles.y, external: true);
	}

	public void MapUpdate()
	{
		mouseInScene = InputManager.MouseInScene();
		Vector3? vector = null;
		if (mouseInScene)
		{
			Ray ray = Camera.main.ScreenPointToRay(InputManager.mousePosition);
			if (ray.direction.y < 0f)
			{
				vector = ray.GetPointAtY(0f);
			}
		}
		if (activity != Activity.BUILDING_ASSIGNING)
		{
			return;
		}
		ClickableObject currentOb = buildingAssigning.GetCurrentOb();
		if (vector.HasValue)
		{
			Vector3 vector2 = vector.Value.TargetYPosition(1f);
			if (Vector3.Distance(currentOb.transform.position, vector2) > currentOb.AssigningMaxRange())
			{
				vector2 = currentOb.transform.position.TargetYPosition(1f) + Toolkit.LookVectorNormalized(currentOb.transform.position, vector2) * currentOb.AssigningMaxRange();
			}
			AssignType assignType = currentOb.GetAssignType();
			currentOb.ShowAssignLine(currentOb.GetAssignLinePos(assignType), vector2, assignType, AssignLineStatus.RED);
		}
		else
		{
			currentOb.HideAssignLines();
		}
	}

	public bool DeselectIsRelevant()
	{
		if (activity == Activity.NONE)
		{
			return selectedObjects.Count > 0;
		}
		return true;
	}

	public static bool PickingOverrides(Layers l1, Layers l2)
	{
		if (l1 == Layers.BuildingElement && l2 == Layers.Buildings)
		{
			return true;
		}
		if (Filters.IsActive(Filter.FLOATING_TRAILS) && (l1 == Layers.Trails || l1 == Layers.Splits))
		{
			return true;
		}
		if (l1 == Layers.Ants && l2 == Layers.Plants)
		{
			return true;
		}
		if (l1 == Layers.Trails && l2 == Layers.Plants)
		{
			return true;
		}
		if (l1 == Layers.Trails && l2 == Layers.FloorTiles)
		{
			return true;
		}
		if (l1 == Layers.Trails && l2 == Layers.BuildingElement)
		{
			return true;
		}
		if (l1 != Layers.Ground && l2 == Layers.Corpses)
		{
			return true;
		}
		return false;
	}

	public void GUI(bool active)
	{
		GUIRectSelect(active);
	}

	public void SetActivity(Activity a, bool force = false)
	{
		if (activity != a || force)
		{
			switch (activity)
			{
			case Activity.BUILDING:
				buildingEditing.Stop();
				break;
			case Activity.TRAIL_EDITING:
				trailEditing.Stop();
				break;
			case Activity.BUILDING_ASSIGNING:
				buildingAssigning.Stop();
				break;
			case Activity.NONE:
				trailEditing.Stop();
				break;
			}
			activity = a;
			switch (activity)
			{
			case Activity.BUILDING:
				buildingEditing.Start();
				break;
			case Activity.TRAIL_EDITING:
				trailEditing.Start();
				break;
			case Activity.BUILDING_ASSIGNING:
				buildingAssigning.Start();
				break;
			}
			ClearFocus();
			if (obUnderMouse_old != null)
			{
				obUnderMouse_old.OnHoverExit();
				obUnderMouse_old = null;
			}
		}
	}

	public Activity GetActivity()
	{
		return activity;
	}

	public void Clear3DCursor()
	{
		mouseCursor.Clear();
	}

	public void Set3DCursor(Vector3 pos, Material material, MouseCursorFootMesh foot_type, MouseCursorBodyMesh body_type)
	{
		mouseCursor.Update3DCursor(pos);
		mouseCursor.SetMaterial(material);
		mouseCursor.SetFootMesh(foot_type);
		mouseCursor.SetBodyMesh(body_type);
	}

	public void Click3DCursor()
	{
		mouseCursor.Click3DCursor();
	}

	public void ClickRelease3DCursor()
	{
		mouseCursor.ClickRelease3DCursor();
	}

	public void SetTrailType(TrailPart trail_part)
	{
		SetTrailType(trail_part.GetTrailPartTrailType(TrailType.GATE_SENSORS, TrailType.DIVIDER, TrailType.IN_BUILDING, TrailType.IN_BUILDING_GATE, TrailType.GATE_COUNTER, TrailType.GATE_LINK, TrailType.GATE_LIFE, TrailType.GATE_CARRY, TrailType.GATE_CASTE, TrailType.GATE_OLD, TrailType.GATE_COUNTER_END, TrailType.GATE_SPEED, TrailType.GATE_TIMER, TrailType.GATE_STOCKPILE));
	}

	public void SetTrailType(TrailType _type)
	{
		ClearFocus();
		SetActivity(Activity.TRAIL_EDITING);
		trailEditing.SetTrailType(_type);
		DoRefreshUnlocks();
	}

	public TrailType GetTrailType()
	{
		if (trailEditing != null)
		{
			return trailEditing.GetTrailType();
		}
		return TrailType.NONE;
	}

	public bool IsDrawingTrail()
	{
		if (trailEditing != null)
		{
			return trailEditing.IsDrawingTrail();
		}
		return false;
	}

	public bool IsDrawingCommandTrail(Ant ant)
	{
		if (trailEditing != null)
		{
			return trailEditing.IsDrawingCommandTrail(ant);
		}
		return false;
	}

	public List<FloorTile> GetSelectedFloorTiles()
	{
		if (trailEditing != null)
		{
			return trailEditing.selectedFloorTiles;
		}
		return null;
	}

	public void DeselectFloorTiles()
	{
		if (trailEditing != null)
		{
			trailEditing.selectedFloorTiles.Clear();
			UIFloorSelection.instance.UpdateFloor();
		}
	}

	public Building StartBuilding(string building_code)
	{
		ClearFocus();
		SetActivity(Activity.BUILDING);
		Building result = buildingEditing.StartBuilding(building_code);
		DoRefreshUnlocks();
		Progress.UseBuilding(building_code);
		return result;
	}

	public void StartRelocate(Building building)
	{
		StartRelocate(BuildMode.RelocateBuildings, new List<Building> { building }, null, null);
	}

	public void StartRelocate(List<Building> buildings)
	{
		StartRelocate(BuildMode.RelocateBuildings, buildings, null, null);
	}

	public void StartRelocate(List<Building> buildings, List<(Vector3, Vector3, TrailType)> trail_hover_data, List<Trail> orig_trails)
	{
		StartRelocate(BuildMode.RelocateFloor, buildings, trail_hover_data, orig_trails);
	}

	private void StartRelocate(BuildMode build_mode, List<Building> buildings, List<(Vector3, Vector3, TrailType)> trail_hover_data, List<Trail> orig_trails)
	{
		foreach (Building item in new List<Building>(buildings))
		{
			if (!item.CanRelocate())
			{
				buildings.Remove(item);
			}
		}
		if (buildings.Count != 0)
		{
			ClearFocus();
			SetActivity(Activity.BUILDING);
			buildingEditing.StartRelocate(build_mode, buildings, orig_trails);
			if (trail_hover_data != null)
			{
				buildingEditing.AddHoverTrails(trail_hover_data);
			}
		}
	}

	public void StartAssign(ClickableObject _ob, AssignType _type)
	{
		SetActivity(Activity.BUILDING_ASSIGNING);
		buildingAssigning.SetObject(_ob, _type);
	}

	public void AddHighlight(HighlightType type, ClickableObject ob)
	{
		if (!(ob == null))
		{
			ob.curHighlight = type;
			if (!highlightedObs.Contains(ob))
			{
				highlightedObs.Add(ob);
			}
		}
	}

	public void AddHighlight(HighlightType type, List<ClickableObject> obs)
	{
		foreach (ClickableObject ob in obs)
		{
			AddHighlight(type, ob);
		}
	}

	public void AddHighlight(HighlightType type, List<ConnectableObject> obs)
	{
		foreach (ConnectableObject ob in obs)
		{
			AddHighlight(type, ob);
		}
	}

	public void UpdateHighlights()
	{
		foreach (ClickableObject item in highlightedObsPrev)
		{
			if (!(item == null) && !highlightedObs.Contains(item))
			{
				item.curHighlight = HighlightType.NONE;
				item.UpdateHighlight();
			}
		}
		foreach (ClickableObject highlightedOb in highlightedObs)
		{
			highlightedOb.UpdateHighlight();
		}
		List<ClickableObject> list = highlightedObs;
		List<ClickableObject> list2 = highlightedObsPrev;
		highlightedObsPrev = list;
		highlightedObs = list2;
		highlightedObs.Clear();
	}

	public void Select(ClickableObject ob)
	{
		if (InputManager.selectMultipleHeld && selectedObjects.Count > 0 && (ob == null || ob is Building) && selectedObjects[0] is Building)
		{
			if (ob != null)
			{
				if (selectedObjects.Contains(ob))
				{
					selectedObjects.Remove(ob);
				}
				else
				{
					selectedObjects.Add(ob);
				}
			}
		}
		else
		{
			selectedObjects.Clear();
			if (ob != null)
			{
				selectedObjects.Add(ob);
			}
		}
	}

	public int GetSelectionCount()
	{
		return selectedObjects.Count;
	}

	public void UpdateSelected(bool refresh = false)
	{
		if (uiHoverClickOb == null)
		{
			uiHoverClickOb = UIBaseSingleton.Get(UIHoverClickOb.instance, show: false);
			uiHoverClickOb.Init();
		}
		List<ClickableObject> list = new List<ClickableObject>();
		ClickableObject clickableObject = ((selectedObjectsPrev.Count != 1) ? null : selectedObjectsPrev[0]);
		for (int num = selectedObjects.Count - 1; num >= 0; num--)
		{
			ClickableObject clickableObject2 = selectedObjects[num];
			if (clickableObject2 == null)
			{
				selectedObjects.RemoveAt(num);
			}
			else
			{
				int num2 = selectedObjectsPrev.IndexOf(clickableObject2);
				if (num2 < 0)
				{
					list.Add(clickableObject2);
					clickableObject2.SetTopPoint();
				}
				else
				{
					selectedObjectsPrev.RemoveAt(num2);
				}
			}
		}
		ClickableObject clickableObject3 = null;
		if (selectedObjects.Count == 1)
		{
			clickableObject3 = selectedObjects[0];
		}
		if (clickableObject3 == null || !clickableObject3.OpenUiOnClick())
		{
			uiHoverClickOb.SetSelected(null);
			UIGame.instance.SetSelected(null);
		}
		else
		{
			if (clickableObject3 != clickableObject || refresh)
			{
				if (clickableObject3.GetUiClickType() == UIClickType.OLD)
				{
					uiHoverClickOb.SetSelected(clickableObject3);
				}
				else
				{
					UIGame.instance.SetSelected(clickableObject3, refresh);
				}
			}
			if (clickableObject3.GetUiClickType() == UIClickType.OLD)
			{
				clickableObject3.UpdateHoverUI(uiHoverClickOb);
			}
			else
			{
				clickableObject3.UpdateClickUi(UIGame.instance.uiClick.currentLayout);
			}
		}
		foreach (ClickableObject item in selectedObjectsPrev)
		{
			if (!(item == null))
			{
				item.OnSelected(is_selected: false, was_selected: true);
			}
		}
		selectedObjectsPrev.Clear();
		selectedObjectsPrev.AddRange(selectedObjects);
		foreach (ClickableObject item2 in list)
		{
			item2.OnSelected(is_selected: true, was_selected: false);
		}
	}

	public void ResetUIClickableObject(ClickableObject ob)
	{
		selectedObjectsPrev.Remove(ob);
	}

	public void ClearIfSelected(ClickableObject ob)
	{
		if (selectedObjects.Count > 0)
		{
			selectedObjects.Remove(ob);
			if (selectedObjects.Count == 0)
			{
				ClearFocus();
			}
		}
	}

	public IEnumerable<ClickableObject> ESelectedObs()
	{
		foreach (ClickableObject selectedObject in selectedObjects)
		{
			yield return selectedObject;
		}
	}

	public IEnumerable<Ant> ESelectedAnts()
	{
		foreach (ClickableObject selectedObject in selectedObjects)
		{
			if (selectedObject is Ant ant)
			{
				yield return ant;
			}
		}
	}

	public IEnumerable<Building> ESelectedBuildings()
	{
		foreach (ClickableObject selectedObject in selectedObjects)
		{
			if (selectedObject is Building building)
			{
				yield return building;
			}
		}
	}

	public bool IsSelected(ClickableObject ob)
	{
		return selectedObjects.Contains(ob);
	}

	private void GUIRectSelect(bool active)
	{
		if (!active)
		{
			isRectSelecting = false;
			return;
		}
		bool flag = false;
		if (isRectSelecting)
		{
			flag = (rectSelectStartPos - InputManager.mousePosition).sqrMagnitude > 80f;
		}
		if (InputManager.selectDown && !isRectSelecting && mouseInScene && activity == Activity.NONE && trailEditing.AllowRectSelect() && obUnderMouse == null)
		{
			isRectSelecting = true;
			rectSelectStartPos = InputManager.mousePosition;
		}
		else
		{
			if (isRectSelecting && InputManager.selectHeld)
			{
				if (!flag)
				{
					return;
				}
				Vector2 vector = rectSelectStartPos;
				Vector2 mousePosition = InputManager.mousePosition;
				float num = vector.x;
				float num2 = vector.y;
				float num3 = mousePosition.x;
				float num4 = mousePosition.y;
				if (num > num3)
				{
					float num5 = num3;
					float num6 = num;
					num = num5;
					num3 = num6;
				}
				if (num2 > num4)
				{
					float num7 = num4;
					float num6 = num2;
					num2 = num7;
					num4 = num6;
				}
				Rect rect = Rect.MinMaxRect(num, num2, num3, num4);
				Rect position = Rect.MinMaxRect(num, (float)Screen.height - num2, num3, (float)Screen.height - num4);
				UnityEngine.GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.15f);
				UnityEngine.GUI.DrawTexture(position, texSelectRect);
				float num8 = 2f;
				UnityEngine.GUI.color = new Color(0.6f, 0.6f, 0.6f);
				UnityEngine.GUI.DrawTexture(new Rect(position.xMin, position.yMin, position.width, num8), texSelectRect);
				UnityEngine.GUI.DrawTexture(new Rect(position.xMin, position.yMin, num8, position.height), texSelectRect);
				UnityEngine.GUI.DrawTexture(new Rect(position.xMax - num8, position.yMin, num8, position.height), texSelectRect);
				UnityEngine.GUI.DrawTexture(new Rect(position.xMin, position.yMax - num8, position.width, num8), texSelectRect);
				UnityEngine.GUI.color = Color.white;
				selectedObjects.Clear();
				{
					foreach (Ant item in GameManager.instance.EAnts())
					{
						if (item.IsDead())
						{
							continue;
						}
						Vector2 point = Camera.main.WorldToScreenPoint(item.transform.position);
						if (!rect.Contains(point))
						{
							continue;
						}
						if (item is CargoAnt cargoAnt)
						{
							foreach (CargoAnt item2 in cargoAnt.EAllSubAnts())
							{
								if (!selectedObjects.Contains(item2))
								{
									selectedObjects.Add(item2);
								}
							}
						}
						else if (!selectedObjects.Contains(item))
						{
							selectedObjects.Add(item);
						}
					}
					return;
				}
			}
			if (isRectSelecting && InputManager.selectUp)
			{
				if (flag)
				{
					List<Ant> list = new List<Ant>();
					foreach (Ant item3 in ESelectedAnts())
					{
						if (!item3.CanGetCommand() || (item3 is CargoAnt cargoAnt2 && cargoAnt2.centipedeLeader != null))
						{
							continue;
						}
						switch (item3.moveState)
						{
						case MoveState.Animated:
						case MoveState.Carried:
						case MoveState.Disabled:
						case MoveState.Waiting:
						case MoveState.DeadAndDisabled:
							continue;
						}
						if (!item3.AreCollidersDisabled())
						{
							list.Add(item3);
						}
					}
					if (list.Count > 0)
					{
						Vector3 zero = Vector3.zero;
						foreach (Ant item4 in list)
						{
							zero += item4.transform.position;
						}
						AudioManager.PlayUI(zero / list.Count, UISfx3D.AntSelect);
						SetTrailType(TrailType.COMMAND);
						trailEditing.CreateNewTrailsFromAnts(list);
						selectedObjects.Clear();
						foreach (Ant item5 in list)
						{
							selectedObjects.Add(item5);
						}
					}
				}
				isRectSelecting = false;
			}
			else
			{
				isRectSelecting = false;
			}
		}
	}

	public void ClearFocus()
	{
		DoRefreshUnlocks();
		isRectSelecting = false;
		Select(null);
		UpdateSelected();
		GameManager.instance.hiddenDuringMap = 0;
		if (UIFrame.instance != null)
		{
			UIFrame.instance.SetFrame();
		}
	}

	public static void DoRefreshUnlocks()
	{
		REFRESH_UNLOCKS = true;
	}

	private void RefreshUnlocks()
	{
		SetTaskbar(currentGroup);
		GameManager.instance.RefreshUnlocksBuildings();
	}

	public void SetTaskbar(BuildingGroup _group)
	{
		List<BuildingGroup> unlockedBuildingGroups = Progress.GetUnlockedBuildingGroups();
		if (unlockedBuildingGroups.Count > 0 && !unlockedBuildingGroups.Contains(_group))
		{
			_group = unlockedBuildingGroups[0];
		}
		if (currentGroup != _group)
		{
			currentGroup = _group;
			ClearFocus();
			return;
		}
		string selected_building = "";
		if (buildingEditing != null)
		{
			Building currentBuilding = buildingEditing.GetCurrentBuilding();
			if (currentBuilding != null)
			{
				selected_building = currentBuilding.data.code;
			}
		}
		UIGame.instance.uiBuildingMenu.SetupButtons(currentGroup, GetTrailType(), selected_building);
	}

	public void SetTaskbarEraser()
	{
		if (currentGroup == BuildingGroup.TRAILS || currentGroup == BuildingGroup.LOGIC)
		{
			SetTaskbar(currentGroup);
		}
		else
		{
			SetTaskbar(BuildingGroup.TRAILS);
		}
	}

	public bool ShouldUIBeInteractable()
	{
		if (trailEditing != null && !trailEditing.IsDragging())
		{
			return buildingEditing.editAction != BuildingEditing.EditAction.Rotating;
		}
		return false;
	}

	public void SelectBlueprint(Blueprint blueprint)
	{
		if (blueprint != null)
		{
			ClearFocus();
			SetActivity(Activity.BUILDING);
			buildingEditing.StartBuilding(BuildMode.PlaceBlueprint, blueprint);
		}
	}

	public void ShowAssignLine(Vector3 start_pos, Vector3 end_pos, AssignType line_type, AssignLineStatus line_status = AssignLineStatus.WHITE)
	{
		ShowAssignLines(new List<AssignLineData>
		{
			new AssignLineData(start_pos, end_pos, line_type, line_status)
		});
	}

	public void ShowAssignLines(List<AssignLineData> data)
	{
		if (globalAssignLines.Count < data.Count)
		{
			int num = data.Count - globalAssignLines.Count;
			for (int i = 0; i < num; i++)
			{
				AssignLine component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(AssignLine))).GetComponent<AssignLine>();
				component.Init();
				globalAssignLines.Add(component);
			}
		}
		foreach (AssignLine globalAssignLine in globalAssignLines)
		{
			globalAssignLine.SetObActive(active: false);
		}
		for (int j = 0; j < data.Count; j++)
		{
			globalAssignLines[j].SetLine(data[j].startPos, data[j].endPos, data[j].lineType, data[j].lineStatus);
			globalAssignLines[j].SetObActive(active: true);
		}
		updateAssingLines = data.Count > 0;
	}

	public void HideGlobalAssignLines()
	{
		ShowAssignLines(new List<AssignLineData>());
	}
}
