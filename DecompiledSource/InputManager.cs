using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class InputManager
{
	public struct TrailShortCut
	{
		public TrailType trailType;

		public InputConfig inputConfig;

		public InputAction inputAction;

		public string locStr;

		public TrailShortCut(TrailType trail_type, string loc_str, InputAction input_action)
		{
			trailType = trail_type;
			locStr = "_obs_" + loc_str;
			inputAction = input_action;
			inputConfig = null;
		}
	}

	public class InputConfig
	{
		public InputAction inputAction;

		private KeyCode keyCode;

		private KeyCode defaultKey;

		private bool mouse0or1;

		public InputConfig(InputAction input_action, KeyCode key_code)
		{
			inputAction = input_action;
			defaultKey = key_code;
			Set(key_code);
		}

		public void Set(KeyCode key_code)
		{
			keyCode = key_code;
			mouse0or1 = keyCode == KeyCode.Mouse0 || keyCode == KeyCode.Mouse1;
		}

		public void ResetToDefault()
		{
			Set(defaultKey);
		}

		public string GetDesc()
		{
			if (keyCode == KeyCode.None)
			{
				return "";
			}
			return InputManager.GetDesc(keyCode);
		}

		public int GetHash()
		{
			return (int)keyCode;
		}

		public bool GetHeld()
		{
			if (mouse0or1)
			{
				if (keyCode == KeyCode.Mouse0)
				{
					return mouse0Held;
				}
				if (keyCode == KeyCode.Mouse1)
				{
					return mouse1Held;
				}
			}
			return Input.GetKey(keyCode);
		}

		public bool GetDown()
		{
			if (mouse0or1)
			{
				if (keyCode == KeyCode.Mouse0)
				{
					return mouse0Down;
				}
				if (keyCode == KeyCode.Mouse1)
				{
					return mouse1Down;
				}
			}
			return Input.GetKeyDown(keyCode);
		}

		public bool GetUp()
		{
			if (mouse0or1)
			{
				if (keyCode == KeyCode.Mouse0)
				{
					return mouse0Up;
				}
				if (keyCode == KeyCode.Mouse1)
				{
					return mouse1Up;
				}
			}
			return Input.GetKeyUp(keyCode);
		}

		public void Write(Save save)
		{
			save.Write((int)keyCode);
		}

		public void Read(Save save)
		{
			Set((KeyCode)save.ReadInt());
		}

		public bool IsKey(KeyCode key_code)
		{
			return keyCode == key_code;
		}

		public KeyCode GetKeyCode()
		{
			return keyCode;
		}
	}

	public static bool camDragDown;

	public static bool camDragHeldCombined;

	public static bool camDragHeldLoose;

	public static bool camDragUp;

	public static bool selectDown;

	public static bool selectHeld;

	public static bool selectUp;

	public static bool deselectCombined;

	public static bool deselectLoose;

	public static bool escape;

	public static bool saveCamTeleportHeld;

	public static bool filterHideUI;

	public static bool filterFloatingTrails;

	public static bool filterHideTrails;

	public static bool filterHideAnts;

	public static bool delete;

	public static bool relocate;

	public static bool followAnt;

	public static bool dropPickup;

	public static bool interactBuilding;

	public static bool stockpileSend;

	public static bool trailDragLock;

	public static bool placeDispenser;

	public static bool pause;

	public static bool quickSave;

	public static bool quickLoad;

	public static bool gameSlower;

	public static bool gameFaster;

	public static bool toggleAntCam;

	public static bool addGround;

	public static bool saveCamPos1;

	public static bool saveCamPos2;

	public static bool camMoveSlower;

	public static bool camMoveFaster;

	public static bool camMoveStart;

	public static bool camMoveAbort;

	public static bool dontSnapHeld;

	public static bool selectMultipleHeld;

	public static bool pipette;

	public static bool pipetteHold;

	public static bool copySettings;

	public static bool pasteSettings;

	public static bool toggleMap;

	public static bool techTree;

	public static bool blueprints;

	public static bool rotateBuildingLeft;

	public static bool rotateBuildingLeft_hold;

	public static bool rotateBuildingLeft_up;

	public static bool rotateBuildingRight;

	public static bool rotateBuildingRight_hold;

	public static bool rotateBuildingRight_up;

	public static float camKeysMoveSpeed = 1f;

	public static float camKeysRotateSpeed = 1f;

	public static float camZoomSpeed = 1f;

	public static float buildRotSpeed = 1f;

	public static TrailType trailTypeQuickSelect;

	public static int buildGroupSelect;

	public static Vector2 camMove;

	public static Vector2? camDragRotate;

	public static float camKeyRotate;

	public static float zoomDelta;

	public static Vector2 deltaMouse;

	public static Vector2 mousePosition;

	private static GameObject selectedUIObject = null;

	public static bool onInputField = false;

	private static Dictionary<InputAction, InputConfig> inputConfigs;

	private static InputConfig inputSelect;

	private static InputConfig inputDeselect;

	private static InputConfig inputCamDrag;

	private static InputConfig inputDeselectOrCamDrag;

	private static InputConfig inputCamRotate;

	private static InputConfig inputCamLeft;

	private static InputConfig inputCamRight;

	private static InputConfig inputCamUp;

	private static InputConfig inputCamDown;

	private static InputConfig inputPause;

	private static InputConfig inputDontSnap;

	private static InputConfig inputSelectMultiple;

	private static InputConfig inputPipette;

	private static InputConfig inputCopySettings;

	private static InputConfig inputPasteSettings;

	private static InputConfig inputCamRotateLeft;

	private static InputConfig inputCamRotateRight;

	private static InputConfig inputCamZoomIn;

	private static InputConfig inputCamZoomOut;

	private static InputConfig inputDelete;

	private static InputConfig inputDropPickup;

	private static InputConfig inputRelocate;

	private static InputConfig inputFollowAnt;

	private static InputConfig inputInteractBuilding;

	private static InputConfig inputStockpileSend;

	private static InputConfig inputTrailDragLock;

	private static InputConfig inputPlaceDispenser;

	private static InputConfig inputBuildingRotateLeft;

	private static InputConfig inputBuildingRotateRight;

	private static InputConfig inputEraser;

	private static InputConfig inputFloorSelector;

	private static InputConfig inputFilterHideUI;

	private static InputConfig inputFilterFloatingTrails;

	private static InputConfig inputFilterHideTrails;

	private static InputConfig inputFilterHideAnts;

	private static InputConfig inputQuickSave;

	private static InputConfig inputQuickLoad;

	private static InputConfig inputPrevGroup;

	private static InputConfig inputNextGroup;

	private static InputConfig inputToggleMap;

	private static InputConfig inputBlueprints;

	private static InputConfig inputTechTree;

	private static bool mouse0Down;

	private static bool mouse0Held;

	private static bool mouse0Up;

	private static bool mouse1Down;

	private static bool mouse1Held;

	private static bool mouse1Up;

	public static List<TrailShortCut> trailShortcuts;

	public static bool Init()
	{
		mousePosition = Input.mousePosition;
		inputConfigs = new Dictionary<InputAction, InputConfig>();
		inputSelect = AddInputConfig(InputAction.Select, KeyCode.Mouse0);
		inputDeselect = AddInputConfig(InputAction.Deselect, KeyCode.None);
		inputCamDrag = AddInputConfig(InputAction.CamDrag, KeyCode.None);
		inputDeselectOrCamDrag = AddInputConfig(InputAction.DeselectOrCamDrag, KeyCode.Mouse1);
		inputCamRotate = AddInputConfig(InputAction.CamRotate, KeyCode.Mouse2);
		inputCamLeft = AddInputConfig(InputAction.CamLeft, KeyCode.A);
		inputCamRight = AddInputConfig(InputAction.CamRight, KeyCode.D);
		inputCamUp = AddInputConfig(InputAction.CamUp, KeyCode.W);
		inputCamDown = AddInputConfig(InputAction.CamDown, KeyCode.S);
		inputPause = AddInputConfig(InputAction.Pause, KeyCode.Space);
		inputDelete = AddInputConfig(InputAction.Delete, KeyCode.Delete);
		inputDontSnap = AddInputConfig(InputAction.DontSnap, KeyCode.LeftControl);
		inputCamRotateLeft = AddInputConfig(InputAction.CamRotateLeft, KeyCode.None);
		inputCamRotateRight = AddInputConfig(InputAction.CamRotateRight, KeyCode.None);
		inputCamZoomIn = AddInputConfig(InputAction.CamZoomIn, KeyCode.Minus);
		inputCamZoomOut = AddInputConfig(InputAction.CamZoomOut, KeyCode.Equals);
		inputDropPickup = AddInputConfig(InputAction.DropPickup, KeyCode.X);
		inputFollowAnt = AddInputConfig(InputAction.FollowAnt, KeyCode.G);
		inputRelocate = AddInputConfig(InputAction.Relocate, KeyCode.F);
		inputInteractBuilding = AddInputConfig(InputAction.InteractBuilding, KeyCode.Z);
		inputPlaceDispenser = AddInputConfig(InputAction.PlaceDispenser, KeyCode.Y);
		inputTrailDragLock = AddInputConfig(InputAction.TrailDragLock, KeyCode.None);
		inputSelectMultiple = AddInputConfig(InputAction.SelectMultiple, KeyCode.LeftShift);
		inputPipette = AddInputConfig(InputAction.Pipette, KeyCode.Mouse3);
		inputCopySettings = AddInputConfig(InputAction.CopySettings, KeyCode.C);
		inputPasteSettings = AddInputConfig(InputAction.PasteSettings, KeyCode.V);
		inputFilterFloatingTrails = AddInputConfig(InputAction.FilterFloatingTrails, KeyCode.F1);
		inputFilterHideTrails = AddInputConfig(InputAction.FilterHideTrails, KeyCode.None);
		inputFilterHideAnts = AddInputConfig(InputAction.FilterHideAnts, KeyCode.None);
		inputFilterHideUI = AddInputConfig(InputAction.FilterHideUI, KeyCode.F2);
		inputQuickSave = AddInputConfig(InputAction.QuickSave, KeyCode.F5);
		inputQuickLoad = AddInputConfig(InputAction.QuickLoad, KeyCode.F9);
		inputPrevGroup = AddInputConfig(InputAction.PrevGroup, KeyCode.LeftBracket);
		inputNextGroup = AddInputConfig(InputAction.NextGroup, KeyCode.RightBracket);
		inputToggleMap = AddInputConfig(InputAction.ToggleMap, KeyCode.M);
		inputBuildingRotateLeft = AddInputConfig(InputAction.BuildingRotateLeft, KeyCode.Q);
		inputBuildingRotateRight = AddInputConfig(InputAction.BuildingRotateRight, KeyCode.E);
		inputTechTree = AddInputConfig(InputAction.TechTree, KeyCode.T);
		inputBlueprints = AddInputConfig(InputAction.BlueprintManager, KeyCode.B);
		trailShortcuts = new List<TrailShortCut>
		{
			new TrailShortCut(TrailType.NULL, "TRAIL_NULL", InputAction.TrailNull),
			new TrailShortCut(TrailType.MAIN, "TRAIL_MAIN", InputAction.TrailMain),
			new TrailShortCut(TrailType.HAULING, "TRAIL_HAULING", InputAction.TrailHauling),
			new TrailShortCut(TrailType.FORAGING, "TRAIL_FORAGING", InputAction.TrailForaging),
			new TrailShortCut(TrailType.PLANT_CUTTING, "TRAIL_PLANTCUTTING", InputAction.TrailPlantCutting),
			new TrailShortCut(TrailType.MINING, "TRAIL_MINING", InputAction.TrailMining),
			new TrailShortCut(TrailType.ELDER, "TRAIL_LAST", InputAction.TrailElder),
			new TrailShortCut(TrailType.GATE_OLD, "TRAIL_GATEOLD", InputAction.TrailGateOld),
			new TrailShortCut(TrailType.DIVIDER, "TRAIL_DIVIDER", InputAction.TrailDivider),
			new TrailShortCut(TrailType.COUNTER_PARENT, "TRAIL_COUNTER_PARENT", InputAction.TrailCounterParent),
			new TrailShortCut(TrailType.GATE_COUNTER, "TRAIL_ASSIGNER", InputAction.TrailGateCounter),
			new TrailShortCut(TrailType.GATE_LINK, "TRAIL_GATELINK", InputAction.TrailGateLink),
			new TrailShortCut(TrailType.GATE_COUNTER_END, "TRAIL_COUNTEREND", InputAction.TrailGateCounterEnd),
			new TrailShortCut(TrailType.GATE, "TRAIL_GATE_GENERIC", InputAction.TrailGate),
			new TrailShortCut(TrailType.GATE_CARRY, "TRAIL_GATECARRY", InputAction.TrailGateCarry),
			new TrailShortCut(TrailType.GATE_CASTE, "TRAIL_GATECASTE", InputAction.TrailGateCaste),
			new TrailShortCut(TrailType.GATE_TIMER, "TRAIL_GATETIMER", InputAction.TrailGateTimer),
			new TrailShortCut(TrailType.GATE_SPEED, "TRAIL_GATESPEED", InputAction.TrailGateSpeed),
			new TrailShortCut(TrailType.GATE_STOCKPILE, "TRAIL_GATESTOCKPILE", InputAction.TrailGateStockpile),
			new TrailShortCut(TrailType.GATE_LIFE, "TRAIL_GATELIFE", InputAction.TrailGateLife)
		};
		for (int i = 0; i < trailShortcuts.Count; i++)
		{
			TrailShortCut value = trailShortcuts[i];
			value.inputConfig = AddInputConfig(value.inputAction, KeyCode.Joystick8Button19);
			trailShortcuts[i] = value;
		}
		inputEraser = AddInputConfig(InputAction.Eraser, KeyCode.R);
		inputFloorSelector = AddInputConfig(InputAction.FloorSelector, KeyCode.H);
		camKeysMoveSpeed = (camKeysRotateSpeed = (camZoomSpeed = (buildRotSpeed = 1f)));
		return true;
	}

	public static void InitPostResources()
	{
		CompleteTrailShortcuts();
	}

	private static void CompleteTrailShortcuts()
	{
		for (int i = 0; i < trailShortcuts.Count; i++)
		{
			TrailShortCut value = trailShortcuts[i];
			if (Progress.TrailCanHaveShortcutKey(value.trailType, out var default_key))
			{
				if (value.inputConfig.IsKey(KeyCode.Joystick8Button19))
				{
					KeyCode key_code = KeyCode.None;
					if (!string.IsNullOrEmpty(default_key))
					{
						if (default_key.Length == 1 && default_key[0] >= '0' && default_key[0] <= '9')
						{
							key_code = (KeyCode)(48 + (default_key[0] - 48));
						}
						else if (default_key.ToLowerInvariant() == "tab")
						{
							key_code = KeyCode.Tab;
						}
						else
						{
							Debug.LogError("Trail shortcut: '" + default_key + "' unknown");
						}
					}
					value.inputConfig.Set(key_code);
				}
			}
			else
			{
				inputConfigs.Remove(value.inputAction);
				value.inputConfig = null;
			}
			trailShortcuts[i] = value;
		}
	}

	public static IEnumerable<(string, InputAction)> ETrailShortcutSettings()
	{
		foreach (TrailShortCut trailShortcut in trailShortcuts)
		{
			if (trailShortcut.inputConfig != null)
			{
				yield return (trailShortcut.locStr, trailShortcut.inputAction);
			}
		}
	}

	public static void ResetToDefault()
	{
		foreach (InputConfig value in inputConfigs.Values)
		{
			value.ResetToDefault();
		}
		CompleteTrailShortcuts();
	}

	public static void WriteConfig(Save save)
	{
		save.Write(inputConfigs.Count);
		foreach (InputConfig value in inputConfigs.Values)
		{
			save.Write((int)value.inputAction);
			value.Write(save);
		}
		save.Write(camKeysMoveSpeed);
		save.Write(camKeysRotateSpeed);
		save.Write(camZoomSpeed);
		save.Write(buildRotSpeed);
	}

	public static void ReadConfig(Save save)
	{
		InputConfig inputConfig = new InputConfig(InputAction.None, KeyCode.None);
		List<InputAction> list = new List<InputAction>();
		HashSet<KeyCode> hashSet = new HashSet<KeyCode>();
		foreach (InputAction key in inputConfigs.Keys)
		{
			list.Add(key);
		}
		List<KeyCode> list2 = new List<KeyCode>
		{
			KeyCode.Q,
			KeyCode.A,
			KeyCode.Z,
			KeyCode.W,
			KeyCode.S,
			KeyCode.X,
			KeyCode.E,
			KeyCode.D,
			KeyCode.C,
			KeyCode.R,
			KeyCode.F,
			KeyCode.V,
			KeyCode.T,
			KeyCode.G,
			KeyCode.B,
			KeyCode.Y,
			KeyCode.H,
			KeyCode.N,
			KeyCode.U,
			KeyCode.J,
			KeyCode.M,
			KeyCode.I,
			KeyCode.K,
			KeyCode.O,
			KeyCode.L,
			KeyCode.P
		};
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			InputAction inputAction = (InputAction)save.ReadInt();
			if (!inputConfigs.TryGetValue(inputAction, out var value))
			{
				value = inputConfig;
			}
			value.Read(save);
			list.Remove(inputAction);
			KeyCode keyCode = value.GetKeyCode();
			if (keyCode != KeyCode.None)
			{
				hashSet.Add(keyCode);
				list2.Remove(keyCode);
			}
		}
		foreach (InputAction item in list)
		{
			if (list2.Count == 0)
			{
				break;
			}
			KeyCode keyCode2 = inputConfigs[item].GetKeyCode();
			if (keyCode2 != KeyCode.None && hashSet.Contains(keyCode2))
			{
				inputConfigs[item].Set(list2[0]);
				list2.RemoveAt(0);
			}
		}
		if (save.version > 42)
		{
			camKeysMoveSpeed = save.ReadFloat();
			camKeysRotateSpeed = save.ReadFloat();
			camZoomSpeed = save.ReadFloat();
		}
		buildRotSpeed = ((save.version < 86) ? 1f : save.ReadFloat());
		if (save.version < 84)
		{
			if (inputConfigs.ContainsKey(InputAction.CamRotateLeft) && inputConfigs[InputAction.CamRotateLeft].IsKey(KeyCode.Q))
			{
				inputCamRotateLeft = AddInputConfig(InputAction.CamRotateLeft, KeyCode.None);
			}
			if (inputConfigs.ContainsKey(InputAction.CamRotateRight) && inputConfigs[InputAction.CamRotateRight].IsKey(KeyCode.E))
			{
				inputCamRotateRight = AddInputConfig(InputAction.CamRotateRight, KeyCode.None);
			}
		}
	}

	private static InputConfig AddInputConfig(InputAction input_action, KeyCode key_code)
	{
		InputConfig inputConfig = new InputConfig(input_action, key_code);
		inputConfigs[input_action] = inputConfig;
		return inputConfig;
	}

	public static void InputUpdate(bool editing = true)
	{
		zoomDelta = Input.mouseScrollDelta.y;
		Vector2 vector = Input.mousePosition;
		deltaMouse = ((mousePosition == Vector2.zero) ? Vector2.zero : (mousePosition - vector));
		mousePosition = vector;
		bool mouseButton = Input.GetMouseButton(0);
		bool mouseButton2 = Input.GetMouseButton(1);
		bool flag = mouse0Held;
		bool flag2 = mouse1Held;
		mouse0Down = mouseButton && !mouseButton2 && !flag;
		mouse0Held = mouseButton && (!mouseButton2 || flag);
		mouse0Up = Input.GetMouseButtonUp(0);
		mouse1Down = mouseButton2 && !mouseButton && !flag2;
		mouse1Held = mouseButton2 && (!mouseButton || flag2);
		mouse1Up = Input.GetMouseButtonUp(1);
		if (UIGlobal.instance != null)
		{
			GameObject currentSelectedGameObject = UIGlobal.eventSystem.currentSelectedGameObject;
			if (currentSelectedGameObject == null || !currentSelectedGameObject.activeInHierarchy)
			{
				onInputField = false;
				selectedUIObject = null;
			}
			else if (currentSelectedGameObject != selectedUIObject)
			{
				onInputField = currentSelectedGameObject.GetComponent<TMP_InputField>() != null || currentSelectedGameObject.GetComponent<InputField>() != null;
				selectedUIObject = currentSelectedGameObject;
			}
		}
		else
		{
			onInputField = false;
		}
		trailTypeQuickSelect = TrailType.NONE;
		buildGroupSelect = 0;
		escape = false;
		gameSlower = (gameFaster = (toggleAntCam = (addGround = (saveCamTeleportHeld = false))));
		if (!onInputField)
		{
			escape = Input.GetKeyDown(KeyCode.Escape);
			gameSlower = Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
			gameFaster = Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus);
			toggleAntCam = Input.GetKeyDown(KeyCode.P);
			addGround = Input.GetKeyDown(KeyCode.N);
			saveCamTeleportHeld = Input.GetKey(KeyCode.LeftShift);
		}
		delete = (relocate = (followAnt = (dropPickup = (interactBuilding = false))));
		camMove = Vector2.zero;
		pause = (dontSnapHeld = (selectMultipleHeld = (pipette = false)));
		camDragRotate = null;
		camKeyRotate = 0f;
		deselectCombined = (deselectLoose = false);
		selectDown = (selectHeld = (selectUp = false));
		if (onInputField)
		{
			return;
		}
		selectHeld = inputSelect.GetHeld();
		selectDown = inputSelect.GetDown();
		selectUp = inputSelect.GetUp();
		deselectCombined = inputDeselectOrCamDrag.GetUp();
		deselectLoose = inputDeselect.GetDown();
		camDragHeldCombined = inputDeselectOrCamDrag.GetHeld();
		camDragHeldLoose = inputCamDrag.GetHeld();
		camDragDown = inputCamDrag.GetDown() || inputDeselectOrCamDrag.GetDown();
		camDragUp = inputCamDrag.GetUp() || inputDeselectOrCamDrag.GetUp();
		camMove.x = (inputCamRight.GetHeld() ? camKeysMoveSpeed : 0f) - (inputCamLeft.GetHeld() ? camKeysMoveSpeed : 0f);
		camMove.y = (inputCamUp.GetHeld() ? camKeysMoveSpeed : 0f) - (inputCamDown.GetHeld() ? camKeysMoveSpeed : 0f);
		if (inputCamRotate.GetHeld() || (Input.GetMouseButton(0) && Input.GetMouseButton(1)))
		{
			camDragRotate = (Player.invertCamera ? new Vector2(deltaMouse.x, 0f - deltaMouse.y) : deltaMouse) * 0.2f;
		}
		camKeyRotate = (inputCamRotateRight.GetHeld() ? 1f : 0f) - (inputCamRotateLeft.GetHeld() ? 1f : 0f);
		if (CamController.instance != null)
		{
			camKeyRotate *= CamController.instance.steerRotateSpeed * camKeysRotateSpeed;
		}
		if (inputSelectMultiple.GetHeld())
		{
			camMove *= 3f;
			camKeyRotate *= 3f;
		}
		zoomDelta += ((inputCamZoomIn.GetHeld() ? 1f : 0f) - (inputCamZoomOut.GetHeld() ? 1f : 0f)) * 0.25f;
		zoomDelta *= camZoomSpeed;
		toggleMap = inputToggleMap.GetDown();
		delete = inputDelete.GetDown();
		relocate = inputRelocate.GetDown();
		followAnt = inputFollowAnt.GetDown();
		dropPickup = inputDropPickup.GetDown();
		interactBuilding = inputInteractBuilding.GetDown();
		trailDragLock = inputTrailDragLock.GetDown();
		placeDispenser = inputPlaceDispenser.GetDown();
		dontSnapHeld = inputDontSnap.GetHeld();
		selectMultipleHeld = inputSelectMultiple.GetHeld();
		pipette = inputPipette.GetUp();
		pipetteHold = inputPipette.GetHeld();
		copySettings = inputCopySettings.GetDown();
		pasteSettings = inputPasteSettings.GetDown();
		pause = inputPause.GetDown();
		techTree = inputTechTree.GetDown();
		blueprints = inputBlueprints.GetDown();
		filterHideUI = inputFilterHideUI.GetDown();
		filterFloatingTrails = inputFilterFloatingTrails.GetDown();
		filterHideTrails = inputFilterHideTrails.GetDown();
		filterHideAnts = inputFilterHideAnts.GetDown();
		quickSave = inputQuickSave.GetDown();
		quickLoad = inputQuickLoad.GetDown();
		rotateBuildingLeft = inputBuildingRotateLeft.GetDown();
		rotateBuildingRight = inputBuildingRotateRight.GetDown();
		rotateBuildingLeft_hold = inputBuildingRotateLeft.GetHeld();
		rotateBuildingRight_hold = inputBuildingRotateRight.GetHeld();
		rotateBuildingLeft_up = inputBuildingRotateLeft.GetUp();
		rotateBuildingRight_up = inputBuildingRotateLeft.GetUp();
		if (!editing)
		{
			return;
		}
		foreach (TrailShortCut trailShortcut in trailShortcuts)
		{
			if (trailShortcut.inputConfig != null && trailShortcut.inputConfig.GetDown())
			{
				trailTypeQuickSelect = trailShortcut.trailType;
			}
		}
		if (inputEraser.GetDown())
		{
			trailTypeQuickSelect = TrailType.TRAIL_ERASER;
		}
		if (inputFloorSelector.GetDown())
		{
			trailTypeQuickSelect = TrailType.FLOOR_SELECTOR;
		}
		buildGroupSelect = (inputPrevGroup.GetDown() ? (-1) : 0) + (inputNextGroup.GetDown() ? 1 : 0);
	}

	public static string GetDesc(InputAction input_action)
	{
		if (input_action == InputAction.None)
		{
			return "";
		}
		return inputConfigs[input_action].GetDesc();
	}

	public static string GetDesc_Scroll()
	{
		return Loc.GetUI("CTRL_SCROLLWHEEL");
	}

	public static string GetDesc_BothMouseButtons()
	{
		return GetDesc(KeyCode.Mouse0) + " + " + GetDesc(KeyCode.Mouse1);
	}

	private static string GetDesc(KeyCode key_code)
	{
		return key_code switch
		{
			KeyCode.Alpha0 => "0", 
			KeyCode.Alpha1 => "1", 
			KeyCode.Alpha2 => "2", 
			KeyCode.Alpha3 => "3", 
			KeyCode.Alpha4 => "4", 
			KeyCode.Alpha5 => "5", 
			KeyCode.Alpha6 => "6", 
			KeyCode.Alpha7 => "7", 
			KeyCode.Alpha8 => "8", 
			KeyCode.Alpha9 => "9", 
			KeyCode.Keypad0 => "K0", 
			KeyCode.Keypad1 => "K1", 
			KeyCode.Keypad2 => "K2", 
			KeyCode.Keypad3 => "K3", 
			KeyCode.Keypad4 => "K4", 
			KeyCode.Keypad5 => "K5", 
			KeyCode.Keypad6 => "K6", 
			KeyCode.Keypad7 => "K7", 
			KeyCode.Keypad8 => "K8", 
			KeyCode.Keypad9 => "K9", 
			KeyCode.KeypadPeriod => "K.", 
			KeyCode.KeypadDivide => "K/", 
			KeyCode.KeypadMultiply => "K*", 
			KeyCode.KeypadMinus => "K-", 
			KeyCode.KeypadPlus => "K+", 
			KeyCode.KeypadEquals => "K=", 
			KeyCode.Exclaim => "!", 
			KeyCode.DoubleQuote => "\"", 
			KeyCode.Hash => "#", 
			KeyCode.Dollar => "$", 
			KeyCode.Percent => "%", 
			KeyCode.Ampersand => "&", 
			KeyCode.Quote => "'", 
			KeyCode.LeftParen => "(", 
			KeyCode.RightParen => ")", 
			KeyCode.Asterisk => "*", 
			KeyCode.Plus => "+", 
			KeyCode.Comma => ",", 
			KeyCode.Minus => "-", 
			KeyCode.Period => ".", 
			KeyCode.Slash => "/", 
			KeyCode.Colon => ":", 
			KeyCode.Semicolon => ";", 
			KeyCode.Less => "<", 
			KeyCode.Equals => "=", 
			KeyCode.Greater => ">", 
			KeyCode.Question => "?", 
			KeyCode.At => "@", 
			KeyCode.LeftBracket => "[", 
			KeyCode.Backslash => "\\", 
			KeyCode.RightBracket => "]", 
			KeyCode.Caret => "^", 
			KeyCode.Underscore => "_", 
			KeyCode.BackQuote => "`", 
			KeyCode.LeftCurlyBracket => "{", 
			KeyCode.Pipe => "|", 
			KeyCode.RightCurlyBracket => "}", 
			KeyCode.Tilde => "~", 
			_ => key_code.ToString(), 
		};
	}

	public static string GetHotkey(TrailType _type)
	{
		switch (_type)
		{
		case TrailType.TRAIL_ERASER:
			return inputEraser.GetDesc();
		case TrailType.FLOOR_SELECTOR:
			return inputFloorSelector.GetDesc();
		default:
			foreach (TrailShortCut trailShortcut in trailShortcuts)
			{
				if (trailShortcut.trailType == _type && trailShortcut.inputConfig != null)
				{
					return trailShortcut.inputConfig.GetDesc();
				}
			}
			return "";
		}
	}

	public static bool Poll(InputAction input_action, bool ignore_left_click)
	{
		KeyCode keyCode = KeyCode.None;
		foreach (KeyCode item in EKeys())
		{
			if (Input.GetKey(item))
			{
				if (item == KeyCode.Mouse0 && ignore_left_click)
				{
					return false;
				}
				keyCode = item;
				break;
			}
		}
		if (keyCode == KeyCode.None)
		{
			return false;
		}
		inputConfigs[input_action].Set(keyCode);
		return true;
	}

	public static void ClearConfig(InputAction input_action)
	{
		inputConfigs[input_action].Set(KeyCode.None);
	}

	public static List<InputAction> GetConfigDuplicates()
	{
		List<InputAction> list = new List<InputAction>();
		foreach (InputConfig value in inputConfigs.Values)
		{
			foreach (InputConfig value2 in inputConfigs.Values)
			{
				if (value != value2 && value.GetHash() == value2.GetHash())
				{
					list.Add(value.inputAction);
				}
			}
		}
		return list;
	}

	public static bool MouseInScene()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			return Application.isFocused;
		}
		return false;
	}

	private static IEnumerable<KeyCode> EKeys()
	{
		yield return KeyCode.Backspace;
		yield return KeyCode.Delete;
		yield return KeyCode.Tab;
		yield return KeyCode.Clear;
		yield return KeyCode.Return;
		yield return KeyCode.Pause;
		yield return KeyCode.Space;
		yield return KeyCode.Keypad0;
		yield return KeyCode.Keypad1;
		yield return KeyCode.Keypad2;
		yield return KeyCode.Keypad3;
		yield return KeyCode.Keypad4;
		yield return KeyCode.Keypad5;
		yield return KeyCode.Keypad6;
		yield return KeyCode.Keypad7;
		yield return KeyCode.Keypad8;
		yield return KeyCode.Keypad9;
		yield return KeyCode.KeypadPeriod;
		yield return KeyCode.KeypadDivide;
		yield return KeyCode.KeypadMultiply;
		yield return KeyCode.KeypadMinus;
		yield return KeyCode.KeypadPlus;
		yield return KeyCode.KeypadEnter;
		yield return KeyCode.KeypadEquals;
		yield return KeyCode.UpArrow;
		yield return KeyCode.DownArrow;
		yield return KeyCode.RightArrow;
		yield return KeyCode.LeftArrow;
		yield return KeyCode.Insert;
		yield return KeyCode.Home;
		yield return KeyCode.End;
		yield return KeyCode.PageUp;
		yield return KeyCode.PageDown;
		yield return KeyCode.F1;
		yield return KeyCode.F2;
		yield return KeyCode.F3;
		yield return KeyCode.F4;
		yield return KeyCode.F5;
		yield return KeyCode.F6;
		yield return KeyCode.F7;
		yield return KeyCode.F8;
		yield return KeyCode.F9;
		yield return KeyCode.F10;
		yield return KeyCode.F11;
		yield return KeyCode.F12;
		yield return KeyCode.F13;
		yield return KeyCode.F14;
		yield return KeyCode.F15;
		yield return KeyCode.Alpha0;
		yield return KeyCode.Alpha1;
		yield return KeyCode.Alpha2;
		yield return KeyCode.Alpha3;
		yield return KeyCode.Alpha4;
		yield return KeyCode.Alpha5;
		yield return KeyCode.Alpha6;
		yield return KeyCode.Alpha7;
		yield return KeyCode.Alpha8;
		yield return KeyCode.Alpha9;
		yield return KeyCode.Exclaim;
		yield return KeyCode.DoubleQuote;
		yield return KeyCode.Hash;
		yield return KeyCode.Dollar;
		yield return KeyCode.Percent;
		yield return KeyCode.Ampersand;
		yield return KeyCode.Quote;
		yield return KeyCode.LeftParen;
		yield return KeyCode.RightParen;
		yield return KeyCode.Asterisk;
		yield return KeyCode.Plus;
		yield return KeyCode.Comma;
		yield return KeyCode.Minus;
		yield return KeyCode.Period;
		yield return KeyCode.Slash;
		yield return KeyCode.Colon;
		yield return KeyCode.Semicolon;
		yield return KeyCode.Less;
		yield return KeyCode.Equals;
		yield return KeyCode.Greater;
		yield return KeyCode.Question;
		yield return KeyCode.At;
		yield return KeyCode.LeftBracket;
		yield return KeyCode.Backslash;
		yield return KeyCode.RightBracket;
		yield return KeyCode.Caret;
		yield return KeyCode.Underscore;
		yield return KeyCode.BackQuote;
		yield return KeyCode.A;
		yield return KeyCode.B;
		yield return KeyCode.C;
		yield return KeyCode.D;
		yield return KeyCode.E;
		yield return KeyCode.F;
		yield return KeyCode.G;
		yield return KeyCode.H;
		yield return KeyCode.I;
		yield return KeyCode.J;
		yield return KeyCode.K;
		yield return KeyCode.L;
		yield return KeyCode.M;
		yield return KeyCode.N;
		yield return KeyCode.O;
		yield return KeyCode.P;
		yield return KeyCode.Q;
		yield return KeyCode.R;
		yield return KeyCode.S;
		yield return KeyCode.T;
		yield return KeyCode.U;
		yield return KeyCode.V;
		yield return KeyCode.W;
		yield return KeyCode.X;
		yield return KeyCode.Y;
		yield return KeyCode.Z;
		yield return KeyCode.LeftCurlyBracket;
		yield return KeyCode.Pipe;
		yield return KeyCode.RightCurlyBracket;
		yield return KeyCode.Tilde;
		yield return KeyCode.Numlock;
		yield return KeyCode.CapsLock;
		yield return KeyCode.ScrollLock;
		yield return KeyCode.RightShift;
		yield return KeyCode.LeftShift;
		yield return KeyCode.RightControl;
		yield return KeyCode.LeftControl;
		yield return KeyCode.RightAlt;
		yield return KeyCode.LeftAlt;
		yield return KeyCode.LeftMeta;
		yield return KeyCode.LeftMeta;
		yield return KeyCode.LeftMeta;
		yield return KeyCode.LeftWindows;
		yield return KeyCode.RightMeta;
		yield return KeyCode.RightMeta;
		yield return KeyCode.RightMeta;
		yield return KeyCode.RightWindows;
		yield return KeyCode.AltGr;
		yield return KeyCode.Help;
		yield return KeyCode.Print;
		yield return KeyCode.SysReq;
		yield return KeyCode.Break;
		yield return KeyCode.Menu;
		yield return KeyCode.Mouse0;
		yield return KeyCode.Mouse1;
		yield return KeyCode.Mouse2;
		yield return KeyCode.Mouse3;
		yield return KeyCode.Mouse4;
		yield return KeyCode.Mouse5;
		yield return KeyCode.Mouse6;
	}
}
