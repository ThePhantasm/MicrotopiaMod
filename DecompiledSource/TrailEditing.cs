using System.Collections.Generic;
using UnityEngine;

public class TrailEditing
{
	public enum HoverType
	{
		None,
		Ground,
		Interactable,
		TrailPart
	}

	private bool isActive;

	private TrailType currentTrailType;

	private List<Trail> currentTrails;

	private HoverType hoverType;

	private List<TrailPart> hoverTrailParts = new List<TrailPart>();

	private ClickableObject hoverInteractable;

	private Vector3? mousePosition;

	private Vector3 cursorPosition;

	private List<ClickableObject> obstacles = new List<ClickableObject>();

	private bool cursorOnGround;

	private Split draggingSplit;

	private TrailPart clickedTrailPart;

	private Vector2 clickedTrailPartMPos;

	private bool overlapped;

	public const float MIN_TRAIL_LENGTH_GLOBAL = 1.5f;

	public const float TRAIL_SNAP_DIST = 1.5f;

	public const float ERASER_BIG_RADIUS = 26f;

	public static Dictionary<TrailType, TrailGate> globalGates = new Dictionary<TrailType, TrailGate>();

	public List<FloorTile> selectedFloorTiles;

	private static List<Ant> lostAnts = null;

	public void Init()
	{
		globalGates.Clear();
	}

	public void Start()
	{
		isActive = true;
	}

	public void Stop()
	{
		if (currentTrails != null)
		{
			AudioManager.PlayUI(UISfx.TrailDeselect);
		}
		ClickLeftUp(cancel: true);
		SetTrailType(TrailType.NONE);
		isActive = false;
	}

	public void EditingUpdate(Vector3? mouse_position, ref ClickableObject ob_under_mouse, FloorTile floor_tile)
	{
		mousePosition = mouse_position;
		if (mousePosition.HasValue)
		{
			cursorOnGround = Toolkit.IsOnGround(mousePosition.Value);
		}
		else
		{
			cursorOnGround = false;
		}
		UpdateHover(ref ob_under_mouse, floor_tile, (currentTrails != null && currentTrails.Count == 1) ? currentTrails[0] : null, out var invalid_trails);
		HighlightTrails();
		UpdateMouseCursor();
		bool flag = obstacles.Count > 0;
		obstacles.Clear();
		if (currentTrailType == TrailType.FLOOR_DEMOLISH)
		{
			if (hoverInteractable != null)
			{
				obstacles.Add(hoverInteractable);
			}
		}
		else if (currentTrailType == TrailType.FLOOR_SELECTOR)
		{
			if (hoverInteractable != null && !InputManager.selectMultipleHeld)
			{
				Gameplay.instance.AddHighlight(HighlightType.OUTLINE_SELECT, hoverInteractable);
			}
		}
		else if (currentTrails == null)
		{
			if (isActive && hoverType == HoverType.Ground)
			{
				Trail.IsObstructed(cursorPosition, ref obstacles);
			}
		}
		else
		{
			if (currentTrailType == TrailType.COMMAND)
			{
				RemoveDeadCommandTrails();
				if (currentTrails == null || currentTrails.Count == 0)
				{
					SetCurrentTrails(null);
					return;
				}
			}
			EditingUpdateDrawingOrDragging(invalid_trails, flag);
		}
		if (hoverType != HoverType.None && obstacles.Count > 0 && !flag)
		{
			AudioManager.PlayUI(cursorPosition, UISfx3D.TrailObstructed);
		}
		Gameplay.instance.AddHighlight(HighlightType.OUTLINE_RED, obstacles);
		if (selectedFloorTiles != null)
		{
			foreach (FloorTile selectedFloorTile in selectedFloorTiles)
			{
				Gameplay.instance.AddHighlight(HighlightType.OUTLINE_SELECT, selectedFloorTile);
				foreach (Building item in selectedFloorTile.EBuildings())
				{
					Gameplay.instance.AddHighlight(HighlightType.OUTLINE_SELECT, item);
				}
			}
		}
		TrailGate globalGate = GetGlobalGate(currentTrailType);
		if (globalGate != null)
		{
			UIGame.instance.UpdateLogicControl(globalGate);
		}
	}

	private void EditingUpdateDrawingOrDragging(List<Trail> extra_invalid_trails, bool was_obstructed)
	{
		Vector3[] array = null;
		if (currentTrailType == TrailType.COMMAND && hoverType == HoverType.Ground && currentTrails.Count > 1)
		{
			array = new Vector3[currentTrails.Count];
			int num = 0;
			foreach (var item2 in GetTargetPosesAroundMouse())
			{
				Vector3 item = item2.Item2;
				array[num++] = item;
			}
		}
		if (IsDragging())
		{
			draggingSplit.transform.position = cursorPosition;
		}
		int num2 = 0;
		foreach (Trail currentTrail in currentTrails)
		{
			if (currentTrail == null)
			{
				Debug.LogError("Tried drawing with null trail in list, shouldn't happen");
				continue;
			}
			ConnectableObject direct_interactable = null;
			if (hoverType != HoverType.None)
			{
				currentTrail.isInvalid = extra_invalid_trails?.Contains(currentTrail) ?? false;
				currentTrail.SetObActive(active: true);
				ClickableObject ignore = null;
				if (IsDragging())
				{
					Vector3 position = draggingSplit.transform.position;
					if (currentTrail.splitStart == draggingSplit)
					{
						currentTrail.SetStartPos(position);
					}
					if (currentTrail.splitEnd == draggingSplit)
					{
						currentTrail.SetEndPos(position);
					}
				}
				else
				{
					Vector3 position2;
					if (currentTrail.owner != null && (currentTrail.splitStart == null || currentTrail.splitStart.TrailCount() < 2))
					{
						position2 = currentTrail.owner.transform.position;
						if (currentTrail.splitStart != null)
						{
							currentTrail.splitStart.transform.position = position2;
						}
					}
					else
					{
						position2 = currentTrail.transform.position;
					}
					Vector3 end = Vector3.zero;
					switch (hoverType)
					{
					case HoverType.Interactable:
					{
						Vector3 vector = mousePosition.Value - position2;
						float magnitude = vector.magnitude;
						vector /= magnitude;
						bool flag = false;
						for (int i = 0; i < 3; i++)
						{
							int num3 = Physics.SphereCastNonAlloc(position2 - (float)i * 1.5f * vector, 1f, vector, Toolkit.raycastHits, magnitude * 2f, Toolkit.Mask((Layers)hoverInteractable.gameObject.layer));
							float num4 = float.MaxValue;
							for (int j = 0; j < num3; j++)
							{
								RaycastHit raycastHit = Toolkit.raycastHits[j];
								float distance = raycastHit.distance;
								if (!(raycastHit.collider.GetComponentInParent<ClickableObject>() != hoverInteractable) && distance != 0f && distance < num4)
								{
									end = raycastHit.point;
									num4 = distance;
									flag = true;
								}
							}
							if (flag)
							{
								break;
							}
						}
						if (flag)
						{
							end -= vector * 2.5f;
						}
						else
						{
							end = hoverInteractable.GetPosNextToOb(currentTrail.transform.position);
						}
						ignore = hoverInteractable;
						direct_interactable = (ConnectableObject)hoverInteractable;
						break;
					}
					case HoverType.TrailPart:
						end = cursorPosition;
						break;
					case HoverType.Ground:
						end = ((currentTrailType == TrailType.COMMAND && currentTrails.Count != 1) ? array[num2++] : cursorPosition);
						break;
					default:
						end = cursorPosition;
						break;
					}
					currentTrail.SetStartEndPos(position2, end);
				}
				if (currentTrail.IsObstructed(ref obstacles, currentTrailType == TrailType.COMMAND, ignore) || !currentTrail.IsOnGround())
				{
					currentTrail.isInvalid = true;
				}
				else if (!currentTrail.isInvalid && !IsDragging() && hoverType != HoverType.Interactable && currentTrail.length < 1.5f)
				{
					currentTrail.isInvalid = true;
				}
				if (currentTrail.isInvalid)
				{
					currentTrail.SetMaterial(TrailStatus.HOVERING_ERROR);
				}
				else
				{
					currentTrail.ResetMaterial();
				}
			}
			else
			{
				currentTrail.SetObActive(active: false);
				currentTrail.isInvalid = true;
			}
			bool no_audio = was_obstructed == (obstacles.Count == 0);
			currentTrail.UpdateActionPointsPreview(direct_interactable, no_audio);
		}
		if (hoverType != HoverType.None)
		{
			AudioManager.PlayTrrr(mousePosition.Value, InputManager.deltaMouse.magnitude);
		}
		foreach (Trail currentTrail2 in currentTrails)
		{
			Gameplay.instance.AddHighlight(HighlightType.OUTLINE_WHITE, currentTrail2.nearbyConnectablesTentative);
		}
	}

	private void HighlightTrails()
	{
		if (hoverType == HoverType.TrailPart)
		{
			bool flag = currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG;
			TrailPart.HighLight(hoverTrailParts, (!flag) ? TrailStatus.HOVERING : TrailStatus.HOVERING_ERROR, !flag);
		}
		else if (TrailGate_Counter.curSelected != null)
		{
			TrailGate_Counter.curSelected.HighLightLoop();
		}
		else
		{
			TrailPart.HighLight();
		}
	}

	private void UpdateHover(ref ClickableObject ob_under_mouse, FloorTile floor_tile, Trail current_trail, out List<Trail> invalid_trails)
	{
		hoverType = HoverType.None;
		hoverTrailParts.Clear();
		hoverInteractable = null;
		invalid_trails = null;
		if (!mousePosition.HasValue)
		{
			cursorPosition = Vector3.zero;
			return;
		}
		Vector3 vector = (cursorPosition = mousePosition.Value);
		TrailPart trailPart;
		if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG)
		{
			float distance = ((currentTrailType == TrailType.TRAIL_ERASER_BIG) ? 26f : 1.5f);
			FindTrailPartsToErase(cursorPosition, ref hoverTrailParts, distance);
			trailPart = ((hoverTrailParts.Count == 0) ? null : hoverTrailParts[0]);
		}
		else if (currentTrailType == TrailType.FLOOR_DEMOLISH || currentTrailType == TrailType.FLOOR_SELECTOR)
		{
			trailPart = null;
		}
		else
		{
			trailPart = FindTrailPart(ref cursorPosition, zoom_based: true, current_trail);
			if (trailPart == null && floor_tile != null && !InputManager.dontSnapHeld && currentTrailType != TrailType.COMMAND && floor_tile.SnapTrail(ref cursorPosition))
			{
				trailPart = FindTrailPart(ref cursorPosition, zoom_based: false, current_trail);
			}
		}
		if (currentTrails != null && trailPart != null && trailPart is Split b && ((!IsDragging()) ? (!CheckMergeEffectsForCurrentTrails(b, out invalid_trails, only_check: true)) : (!CheckMergeEffects(draggingSplit, b, out invalid_trails, only_check: true))))
		{
			trailPart = null;
		}
		if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG)
		{
			cursorPosition = vector;
			for (int num = hoverTrailParts.Count - 1; num >= 0; num--)
			{
				TrailPart trailPart2 = hoverTrailParts[num];
				if (trailPart2 != null && trailPart2 is Split split)
				{
					foreach (Trail connectedTrail in split.connectedTrails)
					{
						if (connectedTrail.IsBuilding())
						{
							hoverTrailParts.RemoveAt(num);
							break;
						}
					}
				}
			}
		}
		if (ob_under_mouse != null && IsObInteractable(ob_under_mouse) && currentTrails.Count == 1)
		{
			hoverType = HoverType.Interactable;
			hoverInteractable = ob_under_mouse;
		}
		if (trailPart != null && ob_under_mouse != null && Gameplay.PickingOverrides(Layers.Trails, (Layers)ob_under_mouse.gameObject.layer))
		{
			ob_under_mouse = null;
		}
		if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG)
		{
			if (trailPart != null)
			{
				hoverType = HoverType.TrailPart;
			}
		}
		else if (currentTrailType == TrailType.FLOOR_DEMOLISH || currentTrailType == TrailType.FLOOR_SELECTOR)
		{
			hoverType = HoverType.None;
			hoverInteractable = floor_tile;
		}
		else if (trailPart != null && (ob_under_mouse == null || isActive) && (!Player.disableTrailDragging || !InDragMode()))
		{
			Split split2 = trailPart as Split;
			if (hoverType != HoverType.Interactable || currentTrailType != TrailType.NONE || split2 != null)
			{
				bool flag = false;
				if (split2 != null && !IsDragging() && InDragMode() && !CanDrag(split2))
				{
					flag = true;
				}
				if (!flag)
				{
					hoverType = HoverType.TrailPart;
					hoverTrailParts.Add(trailPart);
				}
			}
		}
		if (hoverType == HoverType.None)
		{
			hoverType = HoverType.Ground;
		}
	}

	private TrailPart FindTrailPart(ref Vector3 pos, bool zoom_based, Trail current_trail)
	{
		float radius = (zoom_based ? (1.5f * Mathf.Max(1f, CamController.instance.GetZoomFactor())) : 1.5f);
		TrailPart trailPart = null;
		int num = Physics.OverlapSphereNonAlloc(pos, radius, Toolkit.overlapColliders, Toolkit.Mask(Layers.Splits));
		float num2 = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			Transform transform = Toolkit.overlapColliders[i].transform;
			Split component = transform.GetComponent<Split>();
			if (component.deleted || component.GetTrailType() == TrailType.COMMAND || component.IsInBuilding())
			{
				continue;
			}
			if (IsDragging())
			{
				if (component == draggingSplit || component == clickedTrailPart)
				{
					continue;
				}
			}
			else if (current_trail != null && (component == current_trail.splitStart || component == current_trail.splitEnd))
			{
				continue;
			}
			float sqrMagnitude = (transform.transform.position - pos).sqrMagnitude;
			if (sqrMagnitude < num2)
			{
				trailPart = component;
				num2 = sqrMagnitude;
				pos = transform.transform.position;
			}
		}
		if (trailPart == null)
		{
			num = Physics.OverlapSphereNonAlloc(pos, radius, Toolkit.overlapColliders, Toolkit.Mask(Layers.Trails));
			num2 = float.MaxValue;
			for (int j = 0; j < num; j++)
			{
				Trail componentInParent = Toolkit.overlapColliders[j].transform.GetComponentInParent<Trail>();
				if (!(componentInParent == null) && !componentInParent.deleted && !componentInParent.IsCommandTrail() && !componentInParent.IsBuilding() && !(componentInParent == current_trail) && (!IsDragging() || (!(componentInParent.splitStart == draggingSplit) && !(componentInParent.splitEnd == draggingSplit) && !(componentInParent.splitStart == clickedTrailPart) && !(componentInParent.splitEnd == clickedTrailPart))))
				{
					Vector3 nearestPointOnTrail = componentInParent.GetNearestPointOnTrail(pos);
					float sqrMagnitude2 = (nearestPointOnTrail - pos).sqrMagnitude;
					if (sqrMagnitude2 < num2)
					{
						trailPart = componentInParent;
						num2 = sqrMagnitude2;
						pos = nearestPointOnTrail;
					}
				}
			}
		}
		return trailPart;
	}

	private void FindTrailPartsToErase(Vector3 pos, ref List<TrailPart> list, float distance)
	{
		int num = Physics.OverlapSphereNonAlloc(pos, distance, Toolkit.overlapColliders, Toolkit.Mask(Layers.Trails, Layers.Splits));
		for (int i = 0; i < num; i++)
		{
			Transform transform = Toolkit.overlapColliders[i].transform;
			switch ((Layers)transform.gameObject.layer)
			{
			case Layers.Splits:
			{
				Split component = transform.GetComponent<Split>();
				if (!(component == null) && !component.deleted && component.GetTrailType() != TrailType.COMMAND && !component.IsInBuilding())
				{
					list.Add(component);
				}
				break;
			}
			case Layers.Trails:
			{
				Trail componentInParent = transform.GetComponentInParent<Trail>();
				if (!(componentInParent == null) && !componentInParent.deleted && !componentInParent.IsCommandTrail() && !componentInParent.IsBuilding())
				{
					list.Add(componentInParent);
				}
				break;
			}
			}
		}
	}

	public HoverType GetHoverType()
	{
		return hoverType;
	}

	public TrailPart GetHoveringTrailPart()
	{
		if (hoverTrailParts.Count == 0)
		{
			return null;
		}
		return hoverTrailParts[0];
	}

	public void ClickLeftDown()
	{
		clickedTrailPart = null;
		if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG || currentTrailType == TrailType.FLOOR_DEMOLISH || currentTrailType == TrailType.FLOOR_SELECTOR)
		{
			Gameplay.instance.Click3DCursor();
		}
		else
		{
			if (AnyCurrentTrailInvalid())
			{
				return;
			}
			TrailPart trailPart = null;
			if (hoverType == HoverType.TrailPart)
			{
				if (hoverTrailParts.Count != 1)
				{
					Debug.LogError($"Odd, {hoverTrailParts.Count} hovertrailparts");
					return;
				}
				trailPart = hoverTrailParts[0];
			}
			if (currentTrails == null)
			{
				switch (hoverType)
				{
				case HoverType.TrailPart:
					clickedTrailPart = trailPart;
					clickedTrailPartMPos = InputManager.mousePosition;
					if (!InDragMode())
					{
						StartNewTrailFromSelected();
						Gameplay.instance.Click3DCursor();
					}
					break;
				case HoverType.Ground:
					if (currentTrailType != TrailType.NONE && obstacles.Count == 0 && cursorOnGround)
					{
						if (currentTrailType == TrailType.COMMAND)
						{
							Debug.LogError("huh");
						}
						CreateNewTrail(GameManager.instance.NewSplit(cursorPosition));
						Gameplay.instance.Click3DCursor();
					}
					break;
				}
				return;
			}
			bool flag = false;
			switch (hoverType)
			{
			case HoverType.Interactable:
			{
				Trail trail2 = currentTrails[0];
				trail2.NewEndSplit(trail2.posEnd);
				flag = true;
				break;
			}
			case HoverType.TrailPart:
			{
				Split split = GameManager.instance.NewSplit(cursorPosition);
				foreach (Trail currentTrail in currentTrails)
				{
					currentTrail.SetSplitEnd(split);
				}
				if (currentTrailType == TrailType.COMMAND)
				{
					foreach (Trail currentTrail2 in currentTrails)
					{
						trailPart.AddCommandTrailLink(currentTrail2);
					}
					break;
				}
				overlapped = false;
				MergeSplitOnTrailPart(trailPart, split);
				if (currentTrailType == TrailType.DIVIDER)
				{
					flag = true;
				}
				if (overlapped)
				{
					flag = true;
				}
				break;
			}
			case HoverType.Ground:
				if (currentTrails.Count == 1)
				{
					currentTrails[0].NewEndSplit(cursorPosition);
				}
				else
				{
					foreach (var (trail, pos) in GetTargetPosesAroundMouse())
					{
						trail.NewEndSplit(pos);
					}
				}
				flag = true;
				break;
			}
			AudioManager.PlayUI(mousePosition.Value, flag ? UISfx3D.TrailPlace : UISfx3D.TrailPlaceClosed);
			List<Trail> list = new List<Trail>();
			foreach (Trail currentTrail3 in currentTrails)
			{
				bool flag2 = false;
				if (currentTrailType == TrailType.COMMAND && currentTrail3.owner != null && !currentTrail3.owner.CanGetCommand())
				{
					flag2 = true;
					flag = false;
				}
				if (!flag2)
				{
					currentTrail3.PlaceTrail(TrailStatus.PLACED, currentTrail3.nearbyConnectablesTentative);
					TrailGate globalGate = GetGlobalGate(currentTrail3.trailType);
					if (globalGate != null)
					{
						currentTrail3.CopyGateFrom(globalGate);
					}
				}
				if (currentTrailType == TrailType.COMMAND)
				{
					if (!flag2)
					{
						LetOwnerAntContinue(currentTrail3);
					}
					if (UIHoverClickOb.instance != null && UIHoverClickOb.instance.IsShown())
					{
						UIHoverClickOb.instance.Show(target: false);
					}
				}
				if (flag)
				{
					TrailType trailType = currentTrail3.trailType;
					switch (trailType)
					{
					case TrailType.GATE_SENSORS:
					case TrailType.GATE_COUNTER:
					case TrailType.GATE_LIFE:
					case TrailType.GATE_CARRY:
					case TrailType.GATE_CASTE:
					case TrailType.GATE_COUNTER_END:
					case TrailType.GATE_SPEED:
					case TrailType.GATE_TIMER:
					case TrailType.GATE_STOCKPILE:
					case TrailType.GATE_LINK:
						trailType = TrailType.NULL;
						break;
					case TrailType.GATE_OLD:
						trailType = ((!Progress.HasUnlocked(TrailType.ELDER)) ? TrailType.NULL : TrailType.ELDER);
						break;
					}
					Split start_split = ((trailType == TrailType.DIVIDER) ? currentTrail3.splitStart : currentTrail3.splitEnd);
					Trail item = CreateContinuedTrail(start_split, trailType, currentTrail3.owner);
					list.Add(item);
				}
			}
			SetCurrentTrails(list);
			Gameplay.instance.Click3DCursor();
			if (currentTrails == null && currentTrailType == TrailType.COMMAND)
			{
				Gameplay.instance.SetActivity(Activity.NONE);
			}
		}
	}

	public void ClickLeft()
	{
		if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG)
		{
			if (hoverType != HoverType.TrailPart || hoverTrailParts.Count <= 0)
			{
				return;
			}
			AudioManager.PlayUI(hoverTrailParts[0].transform.position, UISfx3D.TrailDelete);
			{
				foreach (TrailPart hoverTrailPart in hoverTrailParts)
				{
					hoverTrailPart.Delete();
				}
				return;
			}
		}
		if (currentTrailType == TrailType.FLOOR_DEMOLISH)
		{
			if (hoverInteractable != null)
			{
				hoverInteractable.OnClickDelete();
			}
		}
		else if (currentTrailType == TrailType.FLOOR_SELECTOR)
		{
			if (!(hoverInteractable != null) || !(hoverInteractable is FloorTile item))
			{
				return;
			}
			if (InputManager.selectMultipleHeld)
			{
				if (selectedFloorTiles.Remove(item) && UIFloorSelection.instance != null)
				{
					UIFloorSelection.instance.UpdateFloor();
				}
			}
			else if (!selectedFloorTiles.Contains(item))
			{
				selectedFloorTiles.Add(item);
				if (UIFloorSelection.instance != null)
				{
					UIFloorSelection.instance.UpdateFloor();
				}
			}
		}
		else if (clickedTrailPart != null && !IsDragging() && InDragMode() && !Player.disableTrailDragging && (InputManager.mousePosition - clickedTrailPartMPos).sqrMagnitude > 25f)
		{
			StartDrag();
		}
	}

	public void ClickLeftUp(bool cancel = false)
	{
		if (IsDragging())
		{
			if (cancel || AnyCurrentTrailInvalid())
			{
				draggingSplit.Delete();
			}
			else
			{
				Split split = clickedTrailPart as Split;
				Trail trail = clickedTrailPart as Trail;
				List<(Trail, Vector3, bool)> list = new List<(Trail, Vector3, bool)>();
				if (split != null)
				{
					foreach (Trail item in split.ECommandTrailLinks())
					{
						draggingSplit.AddCommandTrailLink(item);
						list.Add((item, draggingSplit.transform.position, false));
					}
					foreach (Trail connectedTrail in split.connectedTrails)
					{
						foreach (Trail item2 in connectedTrail.ECommandTrailLinks())
						{
							Vector3 position = ((connectedTrail.splitStart == split) ? draggingSplit : connectedTrail.splitStart).transform.position;
							Vector3 position2 = ((connectedTrail.splitEnd == split) ? draggingSplit : connectedTrail.splitEnd).transform.position;
							Vector3 logicalPointOnLinePieceForCommandTrail = GetLogicalPointOnLinePieceForCommandTrail(item2, position, position2);
							Toolkit.GetNearestPosOnLinePiece(position, position2, item2.posEnd);
							list.Add((item2, logicalPointOnLinePieceForCommandTrail, true));
						}
					}
				}
				else
				{
					foreach (Trail item3 in trail.ECommandTrailLinks())
					{
						Vector3 vector = GetLogicalPointOnLinePieceForCommandTrail(item3, trail.posStart, draggingSplit.transform.position);
						Vector3 logicalPointOnLinePieceForCommandTrail2 = GetLogicalPointOnLinePieceForCommandTrail(item3, trail.posEnd, draggingSplit.transform.position);
						if ((logicalPointOnLinePieceForCommandTrail2 - item3.posEnd).sqrMagnitude < (vector - item3.posEnd).sqrMagnitude)
						{
							vector = logicalPointOnLinePieceForCommandTrail2;
						}
						list.Add((item3, vector, true));
					}
				}
				StartLostAntsCheck();
				if (split != null)
				{
					split.Delete();
				}
				else
				{
					trail.Delete();
				}
				if (hoverType == HoverType.TrailPart && hoverTrailParts.Count == 1)
				{
					MergeSplitOnTrailPart(hoverTrailParts[0], draggingSplit);
				}
				if (currentTrails != null)
				{
					foreach (Trail currentTrail in currentTrails)
					{
						currentTrail.PlaceTrail(TrailStatus.PLACED, currentTrail.nearbyConnectablesTentative);
					}
				}
				foreach (var (trail2, vector2, flag) in list)
				{
					trail2.SetEndPos(vector2);
					foreach (Ant currentAnt in trail2.currentAnts)
					{
						currentAnt.CommandTrailUpdated();
					}
					if (!flag)
					{
						continue;
					}
					foreach (Trail item4 in Toolkit.EFindTrailsNear(vector2, 0.1f))
					{
						item4.AddCommandTrailLink(trail2);
					}
				}
				EndLostAntsCheck(currentTrails);
			}
			SetCurrentTrails(null);
			SetDragging(null);
		}
		clickedTrailPart = null;
		Gameplay.instance.ClickRelease3DCursor();
	}

	public static void StartLostAntsCheck()
	{
		if (lostAnts != null)
		{
			Debug.LogWarning("Nested lost ant checks");
		}
		lostAnts = new List<Ant>();
	}

	public static void EndLostAntsCheck(List<Trail> new_trails)
	{
		if (lostAnts.Count > 0)
		{
			foreach (Ant lostAnt in lostAnts)
			{
				lostAnt.returnToTrails = new_trails;
				lostAnt.TryToReturn();
			}
		}
		lostAnts = null;
	}

	public static void EndLostAntsCheck(Trail new_trail)
	{
		EndLostAntsCheck((lostAnts.Count == 0) ? null : new List<Trail> { new_trail });
	}

	private Vector3 GetLogicalPointOnLinePieceForCommandTrail(Trail command_trail, Vector3 p1, Vector3 p2)
	{
		Vector3 posStart = command_trail.posStart;
		Vector3 posEnd = command_trail.posEnd;
		float x = posStart.x;
		float x2 = posEnd.x;
		float x3 = p1.x;
		float x4 = p2.x;
		float z = posStart.z;
		float z2 = posEnd.z;
		float z3 = p1.z;
		float z4 = p2.z;
		float num = (x - x2) * (z3 - z4) - (z - z2) * (x3 - x4);
		if (Mathf.Abs(num) > Mathf.Epsilon)
		{
			float num2 = ((x - x3) * (z3 - z4) - (z - z3) * (x3 - x4)) / num;
			float num3 = ((x - x3) * (z - z2) - (z - z3) * (x - x2)) / num;
			Vector3 result = new Vector3(x + num2 * (x2 - x), 0f, z + num2 * (z2 - z));
			if (num3 >= 0f && num3 <= 1f)
			{
				return result;
			}
		}
		return Toolkit.GetNearestPosOnLinePiece(p1, p2, command_trail.posEnd);
	}

	public static void AntLostTrail(Ant ant)
	{
		if (lostAnts != null)
		{
			lostAnts.Add(ant);
		}
	}

	public void SetTrailType(TrailType _type)
	{
		if (currentTrailType == _type)
		{
			return;
		}
		TrailGate globalGate = GetGlobalGate(currentTrailType);
		if (globalGate != null)
		{
			globalGate.CleanObjectLinks();
		}
		currentTrailType = _type;
		SetCurrentTrails(null);
		if (_type == TrailType.FLOOR_SELECTOR)
		{
			selectedFloorTiles = new List<FloorTile>();
			UIFloorSelection uIFloorSelection = UIBaseSingleton.Get(UIFloorSelection.instance);
			uIFloorSelection.Init();
			uIFloorSelection.UpdateFloor();
		}
		else
		{
			if (UIFloorSelection.instance != null)
			{
				UIFloorSelection.instance.Show(target: false);
			}
			selectedFloorTiles = null;
		}
		if (_type == TrailType.NONE || _type == TrailType.COMMAND)
		{
			UIFrame.instance.SetFrame();
			UIGame.instance.SetLogicControl(null);
			return;
		}
		TrailData trailData = TrailData.Get(_type);
		switch (_type)
		{
		case TrailType.FLOOR_DEMOLISH:
			Gameplay.instance.SetTaskbar(BuildingGroup.FOUNDATION);
			break;
		case TrailType.FLOOR_SELECTOR:
			Gameplay.instance.SetTaskbar(BuildingGroup.BLUEPRINTS);
			break;
		default:
			if (trailData.eraser)
			{
				Gameplay.instance.SetTaskbarEraser();
			}
			else if (Progress.GetUnlockedTrailsInBuildMenu().Count < GlobalValues.standard.maxBuildMenuItemCount)
			{
				Gameplay.instance.SetTaskbar(BuildingGroup.TRAILS);
			}
			else if (trailData.trailPages.Contains(0))
			{
				Gameplay.instance.SetTaskbar(BuildingGroup.TRAILS);
			}
			else if (trailData.trailPages.Contains(1))
			{
				Gameplay.instance.SetTaskbar(BuildingGroup.LOGIC);
			}
			break;
		}
		UIFrame.instance.SetFrame(AssetLinks.standard.GetTrailMaterial(_type).color);
		if (trailData.logic)
		{
			UIGame.instance.SetLogicControl(GetGlobalGate(_type));
		}
		else
		{
			UIGame.instance.SetLogicControl(null);
		}
	}

	public static TrailGate GetGlobalGate(TrailType trail_type)
	{
		if (!globalGates.TryGetValue(trail_type, out var value))
		{
			value = AssetLinks.standard.GetTrailGate(trail_type);
			if (value != null)
			{
				value.SetObActive(active: false);
			}
			globalGates.Add(trail_type, value);
		}
		return value;
	}

	public TrailType GetTrailType()
	{
		return currentTrailType;
	}

	private bool IsObInteractable(ClickableObject ob_under_mouse)
	{
		if (currentTrails == null)
		{
			return false;
		}
		if (!currentTrails[0].data.snapToConnectable)
		{
			return false;
		}
		if (!(ob_under_mouse is ConnectableObject connectableObject))
		{
			return false;
		}
		if (connectableObject.TrailInteraction(currentTrails[0]) == ExchangeType.NONE)
		{
			return false;
		}
		if (connectableObject is ExchangePoint)
		{
			return currentTrailType == TrailType.COMMAND;
		}
		return true;
	}

	private void SetCurrentTrails(List<Trail> new_trails)
	{
		if (new_trails != null && new_trails.Count == 0)
		{
			new_trails = null;
		}
		if (currentTrails != null)
		{
			foreach (Trail currentTrail in currentTrails)
			{
				if (!currentTrail.IsPlaced() && (new_trails == null || !new_trails.Contains(currentTrail)))
				{
					currentTrail.Delete();
				}
			}
		}
		currentTrails = new_trails;
		if (currentTrails == null)
		{
			AudioManager.EndTrrr();
		}
		else
		{
			AudioManager.StartTrrr(UISfx3D.TrailDrawing);
		}
	}

	public void Deselect()
	{
		if (currentTrails != null)
		{
			AudioManager.PlayUI(UISfx.TrailDeselect);
			SetCurrentTrails(null);
			if (currentTrailType == TrailType.COMMAND)
			{
				Gameplay.instance.SetActivity(Activity.NONE);
			}
		}
		else
		{
			Gameplay.instance.SetActivity(Activity.NONE);
		}
	}

	private IEnumerable<(Trail, Vector3)> GetTargetPosesAroundMouse()
	{
		int count = currentTrails.Count;
		Vector3 target = mousePosition.Value;
		if (count == 2)
		{
			Vector2 vector = currentTrails[0].posStart.XZ();
			Vector2 vector2 = currentTrails[1].posStart.XZ();
			Vector2 vector3 = (vector + vector2) * 0.5f;
			float f = 0f - Mathf.Atan2(target.z - vector3.y, target.x - vector3.x);
			Vector2 vector4 = new Vector2(Mathf.Sin(f) * 2f, Mathf.Cos(f) * 2f);
			Vector2 vector5 = new Vector2(target.x + vector4.x, target.z + vector4.y);
			Vector2 t2 = new Vector2(target.x - vector4.x, target.z - vector4.y);
			float num = Vector2.Dot(vector5 - vector, t2 - vector2);
			float num2 = Vector2.Dot(vector5 - vector2, t2 - vector);
			if (num < num2)
			{
				Vector2 vector6 = t2;
				Vector2 vector7 = vector5;
				vector5 = vector6;
				t2 = vector7;
			}
			yield return (currentTrails[0], new Vector3(vector5.x, target.y, vector5.y));
			yield return (currentTrails[1], new Vector3(t2.x, target.y, t2.y));
			yield break;
		}
		int i = 1;
		float a = 0f;
		foreach (Trail currentTrail in currentTrails)
		{
			float radius = 3f * Mathf.Sqrt(i);
			Vector3 item = new Vector3(target.x + Mathf.Sin(a) * radius, target.y, target.z + Mathf.Cos(a) * radius);
			float rnd = currentTrail.owner.randomValue;
			yield return (currentTrail, item);
			a += (5f + rnd * 3f) / radius;
			i++;
		}
	}

	public bool AllowRectSelect()
	{
		return clickedTrailPart == null;
	}

	private bool AnyCurrentTrailInvalid(bool dont_include_too_short = false)
	{
		if (currentTrails == null)
		{
			return false;
		}
		foreach (Trail currentTrail in currentTrails)
		{
			if (currentTrail.isInvalid)
			{
				if (dont_include_too_short && currentTrail.length < 1.5f)
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private void StartNewTrailFromSelected()
	{
		Split split = clickedTrailPart as Split;
		if (split == null)
		{
			split = (clickedTrailPart as Trail).NewMidSplit(cursorPosition);
		}
		if (currentTrailType == TrailType.NONE)
		{
			Gameplay.instance.SetTrailType(split);
		}
		CreateNewTrail(split);
		clickedTrailPart = null;
	}

	private void CreateNewTrail(Split start_split)
	{
		AudioManager.PlayUI(cursorPosition, UISfx3D.TrailPlace);
		Trail trail = GameManager.instance.NewTrail(currentTrailType, GetGlobalGate(currentTrailType));
		trail.SetSplitStart(start_split);
		trail.SetEndPos(mousePosition.Value);
		SetCurrentTrails(new List<Trail> { trail });
	}

	private Trail CreateContinuedTrail(Split start_split, TrailType trail_type, Ant ant)
	{
		Trail trail = GameManager.instance.NewTrail(trail_type, null, ant);
		trail.SetSplitStart(start_split);
		trail.SetEndPos(mousePosition.Value);
		return trail;
	}

	public bool IsDrawingTrail()
	{
		return currentTrails != null;
	}

	public bool IsDrawingCommandTrail(Ant ant)
	{
		if (currentTrails == null)
		{
			return false;
		}
		foreach (Trail currentTrail in currentTrails)
		{
			if (currentTrail.owner == ant)
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerable<(Trail, Split)> ECurrentTrailsOtherSplits()
	{
		foreach (Trail currentTrail in currentTrails)
		{
			yield return (currentTrail, currentTrail.splitStart);
		}
	}

	private bool CanDrag(Split split)
	{
		foreach (Trail connectedTrail in split.connectedTrails)
		{
			if (connectedTrail.IsBuilding())
			{
				return false;
			}
		}
		return true;
	}

	private void StartDrag()
	{
		Split split = clickedTrailPart as Split;
		if (split != null && !CanDrag(split))
		{
			return;
		}
		List<Trail> list = new List<Trail>();
		SetDragging(GameManager.instance.NewSplit(mousePosition.Value));
		if (split != null)
		{
			foreach (Trail connectedTrail in split.connectedTrails)
			{
				Trail trail = GameManager.instance.NewTrail(connectedTrail.trailType, connectedTrail.trailGate, connectedTrail.owner);
				trail.SetSplitStart((connectedTrail.splitStart == split) ? draggingSplit : connectedTrail.splitStart);
				trail.SetSplitEnd((connectedTrail.splitEnd == split) ? draggingSplit : connectedTrail.splitEnd);
				list.Add(trail);
			}
		}
		else
		{
			Trail trail2 = clickedTrailPart as Trail;
			Trail trail3 = GameManager.instance.NewTrail(trail2.trailType, trail2.trailGate, trail2.owner);
			trail3.SetSplitStart(trail2.splitStart);
			trail3.SetSplitEnd(draggingSplit);
			list.Add(trail3);
			Trail trail4 = GameManager.instance.NewTrail(trail2.IsLogic() ? TrailType.NULL : trail2.trailType, null, trail2.owner);
			trail4.SetSplitStart(draggingSplit);
			trail4.SetSplitEnd(trail2.splitEnd);
			list.Add(trail4);
		}
		SetCurrentTrails(list);
	}

	public bool IsDragging()
	{
		return draggingSplit != null;
	}

	private bool InDragMode()
	{
		return Gameplay.instance.GetActivity() == Activity.NONE;
	}

	private void SetDragging(Split split)
	{
		draggingSplit = split;
	}

	private void MergeSplitOnTrailPart(TrailPart trail_part, Split split)
	{
		Split split2 = trail_part as Split;
		if (split2 == null)
		{
			split2 = (trail_part as Trail).NewMidSplit(cursorPosition);
		}
		split = MergeSplits(split, split2);
		if (split == null)
		{
			Debug.LogWarning("Shouldn't be allowed to merge");
		}
		else if (split.TrailCount() == 0)
		{
			split.Delete();
		}
	}

	private bool CheckMergeEffects(Split a, Split b, out List<Trail> out_trails, bool only_check)
	{
		return CheckMergeEffects(a.EOtherSplits(), b, out out_trails, only_check);
	}

	private bool CheckMergeEffectsForCurrentTrails(Split b, out List<Trail> out_trails, bool only_check)
	{
		return CheckMergeEffects(ECurrentTrailsOtherSplits(), b, out out_trails, only_check);
	}

	private bool CheckMergeEffects(IEnumerable<(Trail, Split)> connections, Split b, out List<Trail> out_trails, bool only_check)
	{
		out_trails = (only_check ? null : new List<Trail>());
		bool result = true;
		foreach (var (item, split) in connections)
		{
			if (split == b)
			{
				if (!only_check)
				{
					out_trails.Add(item);
				}
				continue;
			}
			foreach (var item3 in b.EOtherSplits())
			{
				var (item2, _) = item3;
				if (item3.Item2 == split)
				{
					overlapped = true;
					if (!only_check)
					{
						out_trails.Add(item2);
					}
				}
			}
		}
		return result;
	}

	private Split MergeSplits(Split a, Split b)
	{
		if (!CheckMergeEffects(a, b, out var out_trails, only_check: false))
		{
			return null;
		}
		if (out_trails.Count > 0)
		{
			List<Trail> list = new List<Trail>(a.connectedTrails);
			foreach (Trail connectedTrail in b.connectedTrails)
			{
				if (!list.Contains(connectedTrail) && !out_trails.Contains(connectedTrail))
				{
					list.Add(connectedTrail);
				}
			}
			foreach (Trail item in out_trails)
			{
				item.MoveAnts(list);
				List<Ant> currentAnts = item.currentAnts;
				for (int num = currentAnts.Count - 1; num >= 0; num--)
				{
					currentAnts[num].SetCurrentTrail(null);
				}
				item.DeleteBasic();
			}
		}
		foreach (Trail connectedTrail2 in a.connectedTrails)
		{
			if (connectedTrail2.splitStart == a)
			{
				connectedTrail2.splitStart = b;
				b.AddTrail(connectedTrail2);
			}
			if (connectedTrail2.splitEnd == a)
			{
				connectedTrail2.splitEnd = b;
				b.AddTrail(connectedTrail2);
			}
		}
		a.DeleteButKeepTrails();
		using (List<Trail>.Enumerator enumerator = b.connectedTrails.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				enumerator.Current.CheckActionPointsUpdate();
			}
		}
		return b;
	}

	public void CreateNewTrailsFromAnts(List<Ant> ants)
	{
		List<Trail> list = new List<Trail>();
		foreach (Ant ant in ants)
		{
			UIHover.instance.Outit(ant);
			if (currentTrails != null)
			{
				bool flag = false;
				foreach (Trail currentTrail in currentTrails)
				{
					if (currentTrail.owner == ant)
					{
						list.Add(currentTrail);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
			}
			Trail trail = GameManager.instance.NewTrail(TrailType.COMMAND, null, ant);
			trail.NewStartSplit(ant.transform.position);
			trail.SetStartEndPos(ant.transform.position, ant.transform.position);
			list.Add(trail);
		}
		SetCurrentTrails(list);
	}

	private void LetOwnerAntContinue(Trail trail)
	{
		Ant owner = trail.owner;
		if (owner.currentTrail == null || owner.waitingAtEnd || trail.splitStart == null || trail.splitStart.TrailCount() < 2)
		{
			owner.waitingAtEnd = false;
			owner.GetOnNewTrail(trail);
		}
		if (owner is CargoAnt cargoAnt)
		{
			{
				foreach (CargoAnt item in cargoAnt.EAllSubAnts())
				{
					item.ResetNextTrail();
				}
				return;
			}
		}
		owner.ResetNextTrail();
	}

	public IEnumerable<Ant> ECurrentTrailOwners()
	{
		if (currentTrails == null)
		{
			yield break;
		}
		foreach (Trail currentTrail in currentTrails)
		{
			Ant owner = currentTrail.owner;
			if (!(owner != null))
			{
				continue;
			}
			if (owner is CargoAnt cargoAnt)
			{
				foreach (CargoAnt item in cargoAnt.EAllSubAnts())
				{
					yield return item;
				}
			}
			else
			{
				yield return owner;
			}
		}
	}

	private void RemoveDeadCommandTrails()
	{
		List<Trail> list = null;
		for (int num = currentTrails.Count - 1; num >= 0; num--)
		{
			Trail trail = currentTrails[num];
			if (trail == null)
			{
				currentTrails.RemoveAt(num);
			}
			else if (trail.trailType == TrailType.COMMAND && (trail.owner == null || trail.owner.IsDead()))
			{
				currentTrails.RemoveAt(num);
				if (list == null)
				{
					list = new List<Trail>();
				}
				if (!list.Contains(trail))
				{
					list.Add(trail);
				}
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Trail item in list)
		{
			item.Delete();
		}
	}

	private void UpdateMouseCursor()
	{
		if (mousePosition.HasValue && (isActive || hoverType == HoverType.TrailPart) && !InputManager.camDragHeldCombined && !InputManager.camDragHeldLoose && !InputManager.camDragRotate.HasValue)
		{
			Material material = null;
			if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG || currentTrailType == TrailType.FLOOR_DEMOLISH)
			{
				material = AssetLinks.standard.GetTrailMaterial(TrailType.TRAIL_ERASER, lit_up: true, cursor: true);
			}
			else if (currentTrailType == TrailType.FLOOR_SELECTOR)
			{
				material = AssetLinks.standard.GetTrailMaterial(TrailType.FLOOR_SELECTOR, lit_up: true, cursor: true);
			}
			else if (AnyCurrentTrailInvalid(dont_include_too_short: true) || obstacles.Count > 0 || !cursorOnGround)
			{
				material = AssetLinks.standard.GetTrailMaterial(TrailStatus.HOVERING_ERROR, lit_up: true, cursor: true);
			}
			else if (currentTrailType != TrailType.NONE)
			{
				material = AssetLinks.standard.GetTrailMaterial(currentTrailType, lit_up: true, cursor: true);
			}
			else if (hoverType == HoverType.TrailPart && hoverTrailParts.Count == 1)
			{
				material = AssetLinks.standard.GetTrailMaterial(hoverTrailParts[0].GetTrailPartTrailType(TrailType.GATE_SENSORS), lit_up: true, cursor: true);
			}
			MouseCursorFootMesh foot_type;
			switch (currentTrailType)
			{
			case TrailType.TRAIL_ERASER:
				foot_type = MouseCursorFootMesh.CYLINDER;
				break;
			case TrailType.TRAIL_ERASER_BIG:
				foot_type = MouseCursorFootMesh.CYLINDER_BIG;
				break;
			case TrailType.FLOOR_DEMOLISH:
			case TrailType.FLOOR_SELECTOR:
				foot_type = MouseCursorFootMesh.SQUARE;
				break;
			default:
				foot_type = MouseCursorFootMesh.SPHERE;
				break;
			}
			MouseCursorBodyMesh body_type = MouseCursorBodyMesh.NONE;
			if (currentTrailType != TrailType.COMMAND)
			{
				if (currentTrailType == TrailType.TRAIL_ERASER || currentTrailType == TrailType.TRAIL_ERASER_BIG)
				{
					body_type = MouseCursorBodyMesh.ERASER;
				}
				else if (currentTrailType == TrailType.FLOOR_DEMOLISH)
				{
					body_type = MouseCursorBodyMesh.BLOCK_ERASER;
				}
				else if (currentTrailType == TrailType.FLOOR_SELECTOR)
				{
					body_type = MouseCursorBodyMesh.PYRAMID;
				}
				else if (currentTrails == null || currentTrails.Count == 1)
				{
					body_type = ((currentTrailType == TrailType.NONE) ? MouseCursorBodyMesh.PYRAMID : MouseCursorBodyMesh.PENCIL);
				}
			}
			Gameplay.instance.Set3DCursor(cursorPosition, material, foot_type, body_type);
		}
		else
		{
			Gameplay.instance.Clear3DCursor();
		}
	}

	public void CopyConfig(TrailGate gate)
	{
		TrailGate globalGate = GetGlobalGate(gate.GetTrailType());
		if (globalGate != null)
		{
			globalGate.CopyFrom(gate, TrailGate.GateCopyMode.Settings);
		}
	}
}
