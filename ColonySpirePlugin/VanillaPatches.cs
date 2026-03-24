using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace ColonySpireMod
{
    // ================================================================
    // DIVIDER SAVE/LOAD BUG FIX
    // ================================================================
    // Vanilla bug: divider's active trail index (dividerI) gets corrupted
    // during save/load because:
    //   1. allTrails/allSplits are HashSets with non-deterministic iteration
    //   2. dividerTrails is sorted by clock angle relative to the FIRST
    //      trail's direction — but the first trail depends on insertion order
    //   3. The raw index dividerI is saved, but after reload the list order
    //      may differ, so the index points to the wrong trail.
    //
    // Fix strategy:
    //   A) Re-sort dividerTrails using an absolute reference (Vector3.forward)
    //      so the order is deterministic regardless of insertion.
    //   B) Before saving, translate dividerI → trail linkId (a stable ID).
    //   C) After loading, translate linkId → correct index in rebuilt list.
    // ================================================================

    // Part A: Fix the sort to use an absolute reference direction.
    // ALSO handles Part C (linkId→index translation) because during loading,
    // Split.Init() fires when connectedTrails is still empty. The actual
    // dividerTrails list is only populated later when trails call
    // LoadLinkSplits→PlaceTrail→UpdateDividerTrails(during_load=true).
    // So we do the linkId translation HERE, right after the list is built
    // and sorted, when dividerTrails is actually populated.
    [HarmonyPatch(typeof(Split), "UpdateDividerTrails")]
    public static class DividerSortFixPatch {
        [HarmonyPostfix]
        static void Postfix(Split __instance, bool during_load) {
            try {
                var dividerTrailsField = AccessTools.Field(typeof(Split), "dividerTrails");
                var dividerIField = AccessTools.Field(typeof(Split), "dividerI");
                if (dividerTrailsField == null || dividerIField == null) return;
                var dividerTrails = dividerTrailsField.GetValue(__instance) as List<Trail>;
                if (dividerTrails == null || dividerTrails.Count <= 0) return;

                // Step 1: Re-sort using Vector3.forward as the absolute reference
                // direction instead of the first trail's direction (which depends
                // on HashSet insertion order and is non-deterministic across loads)
                if (dividerTrails.Count > 1) {
                    Vector3 refDir = Vector3.forward;
                    dividerTrails.Sort((Trail a, Trail b) => {
                        float angleA = CalculateClockAngle(refDir, a.direction);
                        float angleB = CalculateClockAngle(refDir, b.direction);
                        return angleA.CompareTo(angleB);
                    });
                }

                // Step 2: If loading, translate encoded linkId back to index.
                // We encoded dividerI = linkId + 100000 during save (Part B).
                // Now that dividerTrails is populated and sorted, find the
                // matching trail and set dividerI to its position.
                int dividerI = (int)dividerIField.GetValue(__instance);
                if (during_load && dividerI >= 100000) {
                    int savedLinkId = dividerI - 100000;
                    int newIndex = -1;
                    for (int i = 0; i < dividerTrails.Count; i++) {
                        if (dividerTrails[i].linkId == savedLinkId) {
                            newIndex = i;
                            break;
                        }
                    }
                    if (newIndex >= 0) {
                        dividerIField.SetValue(__instance, newIndex);
                        
                        // Re-invoke UpdatePointer with the corrected dividerI
                        var updatePointer = AccessTools.Method(typeof(Split), "UpdatePointer");
                        updatePointer?.Invoke(__instance, new object[] { true });
                    }
                    // DO NOT reset to 0 here if not found!
                    // Trails are loaded and added sequentially. This method is called repeatedly
                    // as each trail connects. If we reset to 0 when the target trail hasn't been
                    // loaded yet, we corrupt the saved linkId and ruin the restoration.
                }
            } catch (Exception ex) {
                Debug.LogError($"[Spire/DividerFix] Sort/load fix failed: {ex.Message}");
            }
        }

        static float CalculateClockAngle(Vector3 dir1, Vector3 dir2) {
            float angle = Vector3.Angle(dir1, dir2);
            if (Vector3.Cross(dir1, dir2).y >= 0f) return angle;
            return 360f - angle;
        }
    }

    // Part B: Before saving, translate dividerI to the trail's linkId
    // so we persist a stable identifier instead of a fragile index.
    //
    // We store the linkId into dividerI itself (since linkIds are always > 0
    // during save, and normal dividerI values are small indices starting at 0,
    // we mark it by adding a large offset so the load-side can detect it).
    [HarmonyPatch(typeof(Split), "Write")]
    public static class DividerSaveFixPatch {
        // We use a ConditionalWeakTable to stash the REAL dividerI so we can
        // restore it after Write() completes (we don't want to corrupt
        // the live game state).
        static readonly ConditionalWeakTable<Split, StrongBox<int>> _savedDividerI = new();

        [HarmonyPrefix]
        static void Prefix(Split __instance) {
            try {
                var dividerIField = AccessTools.Field(typeof(Split), "dividerI");
                var dividerTrailsField = AccessTools.Field(typeof(Split), "dividerTrails");
                if (dividerIField == null || dividerTrailsField == null) return;

                int dividerI = (int)dividerIField.GetValue(__instance);
                var dividerTrails = dividerTrailsField.GetValue(__instance) as List<Trail>;
                if (dividerTrails == null || dividerTrails.Count == 0) return;

                // Stash original dividerI so we can restore it in Postfix
                if (_savedDividerI.TryGetValue(__instance, out var box))
                    box.Value = dividerI;
                else
                    _savedDividerI.Add(__instance, new StrongBox<int>(dividerI));

                // Replace dividerI with the trail's linkId (shifted by 100000
                // to distinguish from normal small indices on the load side)
                if (dividerI >= 0 && dividerI < dividerTrails.Count) {
                    int linkId = dividerTrails[dividerI].linkId;
                    if (linkId > 0) {
                        dividerIField.SetValue(__instance, linkId + 100000);
                    }
                }
            } catch (Exception ex) {
                Debug.LogError($"[Spire/DividerFix] Save prefix failed: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        static void Postfix(Split __instance) {
            try {
                // Restore the real dividerI so the running game isn't affected
                if (_savedDividerI.TryGetValue(__instance, out var box)) {
                    var dividerIField = AccessTools.Field(typeof(Split), "dividerI");
                    dividerIField?.SetValue(__instance, box.Value);
                }
            } catch { }
        }
    }

    // Part C: No longer needed as a separate patch — the linkId→index
    // translation is now handled in Part A's Postfix on UpdateDividerTrails,
    // which fires at the right time (when dividerTrails is actually populated).
    // Kept as a safety net: if dividerI is still encoded after Init somehow,
    // clamp it.
    [HarmonyPatch(typeof(Split), "Init")]
    public static class DividerLoadFixPatch {
        [HarmonyPostfix]
        static void Postfix(Split __instance) {
            try {
                var dividerIField = AccessTools.Field(typeof(Split), "dividerI");
                var dividerTrailsField = AccessTools.Field(typeof(Split), "dividerTrails");
                if (dividerIField == null || dividerTrailsField == null) return;

                int dividerI = (int)dividerIField.GetValue(__instance);
                var dividerTrails = dividerTrailsField.GetValue(__instance) as List<Trail>;

                // If dividerI is still encoded (>= 100000) but dividerTrails is
                // empty (which is the typical case at Init time), just leave it —
                // Part A will handle it when UpdateDividerTrails fires with data.
                // But if dividerTrails IS populated and dividerI is out of range,
                // clamp it as a safety net.
                if (dividerTrails != null && dividerTrails.Count > 0 && dividerI >= dividerTrails.Count && dividerI < 100000) {
                    dividerIField.SetValue(__instance, 0);
                    Debug.LogWarning($"[Spire/DividerFix] Init safety: clamped dividerI {dividerI} to 0 (count={dividerTrails.Count})");
                }
            } catch (Exception ex) {
                Debug.LogError($"[Spire/DividerFix] Init safety failed: {ex.Message}");
            }
        }
    }

    // Part D: Safety clamp on DividerChoose — if dividerI is out of range,
    // clamp it to 0 instead of crashing with ArgumentOutOfRangeException.
    // This catches any saves where the dividerI got corrupted.
    [HarmonyPatch(typeof(Split), "DividerChoose")]
    public static class DividerChooseSafetyPatch {
        [HarmonyPrefix]
        static bool Prefix(Split __instance, ref Trail next_trail, bool update_count, ref bool __result) {
            try {
                var dividerTrailsField = AccessTools.Field(typeof(Split), "dividerTrails");
                var dividerIField = AccessTools.Field(typeof(Split), "dividerI");
                if (dividerTrailsField == null || dividerIField == null) return true;

                var dividerTrails = dividerTrailsField.GetValue(__instance) as List<Trail>;
                if (dividerTrails == null || dividerTrails.Count == 0) {
                    next_trail = null;
                    __result = false;
                    return false;
                }

                int dividerI = (int)dividerIField.GetValue(__instance);
                if (dividerI < 0 || dividerI >= dividerTrails.Count) {
                    // Clamp to 0 and fix the field
                    dividerIField.SetValue(__instance, 0);
                    Debug.LogWarning($"[Spire/DividerFix] Clamped out-of-range dividerI to 0 (count={dividerTrails.Count})");
                }
            } catch { }
            return true; // let original run (now with safe dividerI)
        }
    }

    // ================================================================
    // MAIN BUS TRAIL COLOR CUSTOMIZATION — SAVE & LOAD
    // ================================================================

    // Postfix on Trail.ResetMaterial — after vanilla sets the material, we
    // override the color properties for MAIN trails using per-trail color.
    // When a trail has no stamped color, we stamp it with the currently selected color.
    [HarmonyPatch(typeof(Trail), "ResetMaterial")]
    public static class TrailResetMaterialPatch {
        [HarmonyPostfix]
        static void Postfix(Trail __instance) {
            if (__instance.trailType != TrailType.MAIN) return;
            // Gate: colored trails require MOD_COLORED_TRAILS research
            if (!ModState.CanColoredTrails) return;
            try {
                // Stamp this trail with a color if it doesn't have one yet
                int colorIdx = ModState.GetTrailColorIndex(__instance);
                if (colorIdx < 0) {
                    // Check if there's a pending color from a loaded save
                    if (__instance.linkId != 0 && ModState.pendingTrailColors.TryGetValue(__instance.linkId, out int pendingColor)) {
                        colorIdx = pendingColor;
                        ModState.pendingTrailColors.Remove(__instance.linkId);
                    } else {
                        // New trail — stamp it with the current selection
                        colorIdx = ModState.mainBusColorIndex;
                    }
                    ModState.StampTrailColor(__instance, colorIdx);
                }

                // Color 0 = vanilla white, no override needed
                if (colorIdx <= 0) return;

                var (name, color, emission) = ModState.MainBusColors[
                    Math.Max(0, Math.Min(colorIdx, ModState.MainBusColors.Length - 1))];

                // Override material colors
                var curShapeField = AccessTools.Field(typeof(Trail), "curTrailShapeObject");
                if (curShapeField == null) return;
                var curShape = curShapeField.GetValue(__instance) as TrailShapeObject;
                if (curShape == null) return;

                var quadRendField = AccessTools.Field(typeof(Trail), "quadRenderer");
                var quadArrowField = AccessTools.Field(typeof(Trail), "quadRendererArrow");
                if (quadRendField != null) {
                    var quadRend = quadRendField.GetValue(__instance) as MeshRenderer;
                    if (quadRend != null && quadRend.material != null) {
                        quadRend.material.SetColor("_Color", color);
                        quadRend.material.SetColor("_EmissionColor", emission);
                    }
                }
                if (quadArrowField != null) {
                    var quadArrow = quadArrowField.GetValue(__instance) as MeshRenderer;
                    if (quadArrow != null && quadArrow.material != null) {
                        quadArrow.material.SetColor("_Color", color);
                        quadArrow.material.SetColor("_EmissionColor", emission);
                    }
                }
                if (curShape.rends != null) {
                    foreach (var r in curShape.rends) {
                        if (r != null && r.gameObject.activeSelf && r.material != null) {
                            r.material.SetColor("_Color", color);
                            r.material.SetColor("_EmissionColor", emission);
                        }
                    }
                }
                if (curShape.rendsShaded != null) {
                    foreach (var r in curShape.rendsShaded) {
                        if (r != null && r.material != null) {
                            r.material.SetColor("_Color", color);
                            r.material.SetColor("_EmissionColor", emission);
                        }
                    }
                }
            } catch (Exception ex) { Debug.Log($"[Spire] TrailColor: {ex.Message}"); }
        }
    }

    // The sub-toolbar (UIToolbarExtra) appears when you click a trail type that
    // has siblings (e.g. MAIN shows NULL+MAIN). It has its own layout and button
    // list, so we can freely add colored Main Bus buttons here without overflow.
    // We hook UIToolbarExtra.Setup(TrailType, Transform) to add extra color buttons.

    [HarmonyPatch(typeof(UIToolbarExtra), "Setup", new[] { typeof(TrailType), typeof(Transform) })]
    public static class TrailColorButtonsPatch {
        [HarmonyPostfix]
        static void Postfix(UIToolbarExtra __instance, TrailType selected_trail) {
            try {
                // Only add color buttons when a MAIN-family trail is selected
                if (selected_trail == TrailType.NONE) return;
                // Gate: colored trails require MOD_COLORED_TRAILS research
                if (!ModState.CanColoredTrails) return;
                TrailData selectedData = TrailData.Get(selected_trail);
                if (selectedData == null) return;

                // Check if this is the MAIN/NULL family by looking at parentType
                // MAIN and NULL share a parentType; we want to inject when either is selected
                TrailType parentType = selectedData.parentType;
                if (parentType == TrailType.NONE) return;

                // Check if MAIN is in this family
                bool hasMain = false;
                foreach (TrailData trail in PrefabData.trails) {
                    if (trail.parentType == parentType && trail.type == TrailType.MAIN) {
                        hasMain = true;
                        break;
                    }
                }
                if (!hasMain) return;

                Debug.Log($"[Spire] TrailColorButtons: UIToolbarExtra.Setup fired for {selected_trail}, parentType={parentType}");

                // Access the spawned buttons list and prefab
                var spawnedField = AccessTools.Field(typeof(UIToolbarExtra), "spawnedButtons");
                var prefabField = AccessTools.Field(typeof(UIToolbarExtra), "prefabButton");
                var uiToolbarField = AccessTools.Field(typeof(UIToolbarExtra), "uiToolbar");
                var spawnedButtons = spawnedField?.GetValue(__instance) as List<UIBuildingButton>;
                var prefab = prefabField?.GetValue(__instance) as UIBuildingButton;
                var uiToolbar = uiToolbarField?.GetValue(__instance) as UIBuildingMenu;

                if (spawnedButtons == null || prefab == null || uiToolbar == null) {
                    Debug.LogError("[Spire] TrailColorButtons: couldn't access UIToolbarExtra fields");
                    return;
                }

                Debug.Log($"[Spire] TrailColorButtons: {spawnedButtons.Count} existing sub-buttons");

                // Add a colored button for each non-default color variant
                for (int i = 1; i < ModState.MainBusColors.Length; i++) {
                    int colorIdx = i;
                    var (colorName, color, emission) = ModState.MainBusColors[colorIdx];

                    // Create or reuse a button
                    UIBuildingButton btn;
                    if (spawnedButtons.Count > 0) {
                        // We need a new button beyond what vanilla created
                        var newGo = UnityEngine.Object.Instantiate(prefab.gameObject, prefab.transform.parent);
                        btn = newGo.GetComponent<UIBuildingButton>();
                        spawnedButtons.Add(btn);
                    } else {
                        continue;
                    }

                    // Get Main Bus trail icon
                    Color iconColor;
                    Sprite trailIcon = AssetLinks.standard.GetTrailIcon(TrailType.MAIN, out iconColor);

                    // Initialize the button like vanilla does
                    btn.Init(colorName, (Action)(() => {
                        ModState.mainBusColorIndex = colorIdx;
                        ModSave.SaveMainBusColor();
                        // Don't refresh existing trails — only new trails get the new color
                        uiToolbar.OnClickTrailButton(TrailType.MAIN);
                        Debug.Log($"[Spire] Trail color set to {colorName} (idx {colorIdx})");
                    }));
                    btn.SetImage(trailIcon);
                    btn.SetImageColor(color);
                    btn.ResetOverlays();
                    if (selected_trail == TrailType.MAIN && colorIdx == ModState.mainBusColorIndex) {
                        btn.AddOverlay(OverlayTypes.SELECTED);
                    }
                    btn.SetInteractable(true);
                    btn.SetObActive(true);
                    btn.SetHotkey("");

                    Debug.Log($"[Spire] Added sub-toolbar color button: {colorName} (idx {colorIdx})");
                }

                // Also tint the existing MAIN button if a non-default color is selected
                // and deselect it if a color variant is what's active
                if (ModState.mainBusColorIndex > 0 && selected_trail == TrailType.MAIN) {
                    // Find the vanilla MAIN button in the sub-toolbar and deselect it
                    // (our colored variant should be selected instead)
                    foreach (var btn in spawnedButtons) {
                        if (btn != null && btn.gameObject.activeSelf && btn.GetText() != null) {
                            // Can't easily identify which is MAIN vs NULL, so skip for now
                        }
                    }
                }

                // Force layout recalculation
                var rtButtons = AccessTools.Field(typeof(UIToolbarExtra), "rtButtons")?.GetValue(__instance) as RectTransform;
                if (rtButtons != null) {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rtButtons);
                }
                // Also resize the background
                var rtBackground = AccessTools.Field(typeof(UIToolbarExtra), "rtBackground")?.GetValue(__instance) as RectTransform;
                if (rtBackground != null && rtButtons != null) {
                    // Match background width to buttons area
                    rtBackground.sizeDelta = new Vector2(rtButtons.rect.width + 20f, rtBackground.sizeDelta.y);
                }

                Debug.Log($"[Spire] TrailColorButtons: SUCCESS — added {ModState.MainBusColors.Length - 1} color buttons to sub-toolbar");
            } catch (Exception ex) {
                Debug.LogError($"[Spire] TrailColorButtons FAILED: {ex}");
            }
        }
    }

    // Save per-trail colors to a sidecar file when the game saves
    [HarmonyPatch(typeof(GameManager), "SaveGame")]
    public static class TrailColorSavePatch {
        [HarmonyPostfix]
        static void Postfix(bool __result, string save_name) {
            if (!__result) return; // save failed, don't write sidecar
            try {
                var allTrailsField = AccessTools.Field(typeof(GameManager), "allTrails");
                if (allTrailsField == null) return;
                var allTrails = allTrailsField.GetValue(GameManager.instance) as HashSet<Trail>;
                if (allTrails == null) return;

                var lines = new List<string>();
                foreach (var trail in allTrails) {
                    if (trail == null || trail.trailType != TrailType.MAIN) continue;
                    if (trail.linkId == 0) continue; // not saved
                    int colorIdx = ModState.GetTrailColorIndex(trail);
                    if (colorIdx > 0) { // only save non-default colors
                        lines.Add($"{trail.linkId}:{colorIdx}");
                    }
                }

                string path = ModState.GetTrailColorPath(save_name);
                if (lines.Count > 0) {
                    System.IO.File.WriteAllLines(path, lines);
                    Debug.Log($"[Spire] TrailColor: saved {lines.Count} colored trails to {path}");
                } else if (System.IO.File.Exists(path)) {
                    System.IO.File.Delete(path); // no colors to save, clean up old file
                }
            } catch (Exception ex) {
                Debug.LogError($"[Spire] TrailColor save failed: {ex}");
            }
        }
    }

    // Load per-trail colors from sidecar file when the game loads
    [HarmonyPatch(typeof(GameManager), "KStartLoadGame")]
    public static class TrailColorLoadPatch {
        [HarmonyPrefix]
        static void Prefix(string save_name) {
            try {
                ModState.pendingTrailColors.Clear();
                string path = ModState.GetTrailColorPath(save_name);
                if (!System.IO.File.Exists(path)) {
                    Debug.Log($"[Spire] TrailColor: no sidecar file for '{save_name}'");
                    return;
                }

                var lines = System.IO.File.ReadAllLines(path);
                foreach (var line in lines) {
                    var parts = line.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int linkId) && int.TryParse(parts[1], out int colorIdx)) {
                        ModState.pendingTrailColors[linkId] = colorIdx;
                    }
                }
                Debug.Log($"[Spire] TrailColor: loaded {ModState.pendingTrailColors.Count} pending colors from {path}");
            } catch (Exception ex) {
                Debug.LogError($"[Spire] TrailColor load failed: {ex}");
            }
        }
    }

    // ================================================================
    // STOCKPILE GATE → BATTERY TARGETING
    // Allows TrailGate_Stockpile to target BatteryBuilding in addition
    // to Stockpile. The battery's storedEnergy is compared against the
    // gate's threshold amount. We store the battery reference in a
    // side-dict because the gate's `stockpile` field is Stockpile-typed.
    // ================================================================
    public static class BatteryGateState
    {
        // Gate instance-ID → BatteryBuilding
        public static readonly Dictionary<int, BatteryBuilding> batteryTargets = new();

        public static BatteryBuilding GetBattery(TrailGate_Stockpile gate)
        {
            batteryTargets.TryGetValue(gate.GetInstanceID(), out var b);
            return b;
        }
        public static void SetBattery(TrailGate_Stockpile gate, BatteryBuilding b)
        {
            if (b != null)
                batteryTargets[gate.GetInstanceID()] = b;
            else
                batteryTargets.Remove(gate.GetInstanceID());
        }
    }

    // 1. CanAssignTo — also accept BatteryBuilding
    [HarmonyPatch(typeof(TrailGate_Stockpile), "CanAssignTo")]
    public static class StockpileGateCanAssignPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, ClickableObject target, ref string error, ref bool __result)
        {
            if (target is BatteryBuilding)
            {
                error = "";
                __result = true;
                return false; // skip original
            }
            return true; // let original handle Stockpile / base
        }
    }

    // 2. Assign — store BatteryBuilding in our side-dict, clear stockpile
    [HarmonyPatch(typeof(TrailGate_Stockpile), "Assign")]
    public static class StockpileGateAssignPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, ClickableObject target, bool add)
        {
            if (target is BatteryBuilding battery)
            {
                if (add)
                {
                    BatteryGateState.SetBattery(__instance, battery);
                    __instance.stockpile = null; // clear stockpile so they don't conflict
                }
                else
                {
                    BatteryGateState.SetBattery(__instance, null);
                }
                // UpdateBillboard via reflection (private method on base)
                AccessTools.Method(typeof(TrailGate_Stockpile), "UpdateBillboard")?.Invoke(__instance, null);
                Debug.Log($"[Spire] Stockpile gate assigned to Battery: {(add ? battery.name : "cleared")}");
                return false;
            }
            // If assigning to a normal Stockpile, clear any battery reference
            if (target is Stockpile && add)
                BatteryGateState.SetBattery(__instance, null);
            return true;
        }
    }

    // 3. CheckIfSatisfied — use storedEnergy when targeting a battery
    [HarmonyPatch(typeof(TrailGate_Stockpile), "CheckIfSatisfied")]
    public static class StockpileGateCheckPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, Ant _ant, bool final, bool chain_satisfied, ref bool __result)
        {
            var battery = BatteryGateState.GetBattery(__instance);
            if (battery == null) return true; // no battery target, use original stockpile logic

            int energyInt = Mathf.RoundToInt(battery.storedEnergy);
            bool flag = __instance.lowerThan ? (energyInt < __instance.amount) : (energyInt > __instance.amount);
            if (final)
            {
                // Show the green/red traffic light via reflection on the base gate
                AccessTools.Method(typeof(TrailGate_Stockpile), "ShowAllowAnt")
                    ?.Invoke(__instance, new object[] { flag, true, chain_satisfied });
            }
            __result = flag;
            return false;
        }
    }

    // 4. SetAssignLine — show line to battery
    [HarmonyPatch(typeof(TrailGate_Stockpile), "SetAssignLine")]
    public static class StockpileGateAssignLinePatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, bool show)
        {
            var battery = BatteryGateState.GetBattery(__instance);
            if (battery == null) return true; // no battery, use original
            if (show)
                __instance.ShowAssignLine(battery, AssignType.GATE);
            else
                __instance.HideAssignLines();
            return false;
        }
    }

    // 5. EAssignedObjects — yield battery instead of stockpile
    [HarmonyPatch(typeof(TrailGate_Stockpile), "EAssignedObjects")]
    public static class StockpileGateEnumPatch
    {
        [HarmonyPostfix]
        static void Postfix(TrailGate_Stockpile __instance, ref IEnumerable<ClickableObject> __result)
        {
            var battery = BatteryGateState.GetBattery(__instance);
            if (battery != null)
            {
                // Replace the result entirely — yield just the battery
                __result = YieldBattery(battery);
            }
        }
        static IEnumerable<ClickableObject> YieldBattery(BatteryBuilding b)
        {
            yield return b;
        }
    }

    // 6. GetHologramShape — show energy icon for battery
    [HarmonyPatch(typeof(TrailGate_Stockpile), "GetHologramShape")]
    public static class StockpileGateHologramPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, ref PickupType _pickup, ref AntCaste _ant, ref HologramShape __result)
        {
            var battery = BatteryGateState.GetBattery(__instance);
            if (battery == null) return true;
            _pickup = PickupType.NONE;
            _ant = AntCaste.NONE;
            // Show a lightning bolt / energy shape — BatteryBuilding doesn't have a hologram
            // so we show the generic QuestionMark but with a pickup hint if possible
            __result = HologramShape.QuestionMark;
            return false;
        }
    }

    // 7. WriteConfig — write battery building reference in place of stockpile
    [HarmonyPatch(typeof(TrailGate_Stockpile), "WriteConfig")]
    public static class StockpileGateWritePatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, ISaveContainer save)
        {
            var battery = BatteryGateState.GetBattery(__instance);
            if (battery == null) return true; // no battery, let original write stockpile
            // Write same format as vanilla: lowerThan, amount, building-reference
            // but substitute the battery for the stockpile slot
            save.Write(__instance.lowerThan);
            save.Write(__instance.amount);
            save.Write((Building)battery);
            Debug.Log($"[Spire] StockpileGate WriteConfig: saved battery {battery.name}");
            return false;
        }
    }

    // 8. ReadConfig — read building reference; if it's a BatteryBuilding, store in side-dict
    [HarmonyPatch(typeof(TrailGate_Stockpile), "ReadConfig")]
    public static class StockpileGateReadPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance, ISaveContainer save)
        {
            // Read same format as vanilla
            __instance.lowerThan = save.ReadBool();
            __instance.amount = save.ReadInt();
            BuildingLink buildingLink = save.ReadBuilding();
            if (buildingLink.postpone)
            {
                // Building not loaded yet — store ID for LoadLinks to resolve
                AccessTools.Field(typeof(TrailGate_Stockpile), "stockpileId")
                    .SetValue(__instance, buildingLink.id);
            }
            else if (buildingLink.building is BatteryBuilding battery)
            {
                BatteryGateState.SetBattery(__instance, battery);
                __instance.stockpile = null;
                Debug.Log($"[Spire] StockpileGate ReadConfig: loaded battery {battery.name}");
            }
            else
            {
                __instance.stockpile = buildingLink.building as Stockpile;
            }
            return false; // skip original
        }
    }

    // 9. LoadLinks — resolve postponed building ID as Stockpile or BatteryBuilding
    [HarmonyPatch(typeof(TrailGate_Stockpile), "LoadLinks")]
    public static class StockpileGateLinksPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TrailGate_Stockpile __instance)
        {
            var idField = AccessTools.Field(typeof(TrailGate_Stockpile), "stockpileId");
            int id = (int)idField.GetValue(__instance);
            if (id == -1) return false; // nothing to link

            // Try Stockpile first (vanilla behavior)
            var stockpile = GameManager.instance.FindLink<Stockpile>(id);
            if (stockpile != null)
            {
                __instance.stockpile = stockpile;
                Debug.Log($"[Spire] StockpileGate LoadLinks: resolved stockpile id={id}");
                return false;
            }

            // Try BatteryBuilding
            var battery = GameManager.instance.FindLink<BatteryBuilding>(id);
            if (battery != null)
            {
                BatteryGateState.SetBattery(__instance, battery);
                __instance.stockpile = null;
                Debug.Log($"[Spire] StockpileGate LoadLinks: resolved battery id={id}");
            }
            else
            {
                Debug.LogWarning($"[Spire] StockpileGate LoadLinks: could not resolve id={id} as Stockpile or Battery");
            }

            return false; // skip original
        }
    }
}
