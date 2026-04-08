using System;
using System.Collections.Generic;
using System.Reflection;
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
    // Removed DividerSortFixPatch entirely.
    // Vanilla array population natively preserves order via sequential deserialization inside HashSet slots,
    // so sorting it manually violently decoupled the raw physical rotational mappings.

    // Part B: REMOVED.
    // We used to translate dividerI to linkId + 100000 here, but doing so modifying
    // the vanilla save file caused hard crashes (ArgumentOutOfRangeException in DividerChoose)
    // if the user ever uninstalled the mod. The deterministic sort is sufficient to
    // keep indices consistent, so we just let vanilla save the raw deterministically-sorted index.

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
                    dividerI = 0;
                    dividerIField.SetValue(__instance, 0);
                    Debug.LogWarning($"[Spire/DividerFix] Clamped out-of-range dividerI to 0 (count={dividerTrails.Count})");
                }

                // FIX: Fast-forward dividerI if it currently points to a gate
                // This prevents the bug where the first ant is forced down a gated path
                // because dividerI initially started on it.
                for (int i = 0; i < dividerTrails.Count; i++) {
                    if (!dividerTrails[dividerI].IsGate()) break;
                    dividerI = (dividerI + 1) % dividerTrails.Count;
                }
                dividerIField.SetValue(__instance, dividerI);

            } catch { }
            return true; // let original run (now with safe and gate-bypassed dividerI)
        }
    }

    // ================================================================
    // FLAAAWLESS DIVIDER FIX: Local-Rotation Invariant Sorting + Primative Array Tracking
    // ================================================================
    public static class BlueprintState {
        public static Dictionary<int, int> dividerIbySplitIndex = new Dictionary<int, int>();
    }

    [HarmonyPatch(typeof(Blueprint), MethodType.Constructor, new Type[] { typeof(Building), typeof(Vector3) })]
    public static class BlueprintCtorPatch {
        [HarmonyPostfix]
        static void Postfix() {
            BlueprintState.dividerIbySplitIndex.Clear();
        }
    }

    [HarmonyPatch(typeof(Blueprint), "AddSplit", new Type[] { typeof(Split) })]
    public static class BlueprintAddSplitPatch {
        [HarmonyPostfix]
        static void Postfix(int __result, Split split) {
            try {
                if (split != null) {
                    var divField = AccessTools.Field(typeof(Split), "dividerI");
                    if (divField != null) BlueprintState.dividerIbySplitIndex[__result] = (int)divField.GetValue(split);
                }
            } catch { }
        }
    }

    [HarmonyPatch(typeof(BuildingEditing), "PlaceBuildings")]
    public static class BlueprintPlaceBuildingsPatch {
        [HarmonyPostfix]
        static void Postfix(BuildingEditing __instance) {
            try {
                var buildModeField = AccessTools.Field(typeof(BuildingEditing), "buildMode");
                var builderBlueprintField = AccessTools.Field(typeof(BuildingEditing), "curBlueprint");
                if (buildModeField == null || builderBlueprintField == null) return;
                
                var buildMode = (BuildMode)buildModeField.GetValue(__instance);
                if (buildMode != BuildMode.PlaceBlueprint) return;
                
                var curBlueprint = builderBlueprintField.GetValue(__instance) as Blueprint;
                if (curBlueprint == null || curBlueprint.splits == null) return;
                
                var dividerIField = AccessTools.Field(typeof(Split), "dividerI");
                if (dividerIField == null) return;

                // Simple 1-to-1 array map! Works no matter how many times the blueprint objects are cloned in memory!
                for (int i = 0; i < curBlueprint.splits.Count; i++) {
                    if (BlueprintState.dividerIbySplitIndex.TryGetValue(i, out int targetI)) {
                        var spawnedSplit = curBlueprint.splits[i].split;
                        if (spawnedSplit != null) {
                            var trails = AccessTools.Field(typeof(Split), "dividerTrails").GetValue(spawnedSplit) as List<Trail>;
                            if (trails != null && trails.Count > 0) {
                                targetI = Math.Max(0, Math.Min(targetI, trails.Count - 1));
                            } else targetI = 0;

                            dividerIField.SetValue(spawnedSplit, targetI);
                            var pointerUpdate = AccessTools.Method(typeof(Split), "UpdatePointer");
                            if (pointerUpdate != null) pointerUpdate.Invoke(spawnedSplit, new object[] { false });
                        }
                    }
                }
            } catch (Exception ex) {
                Debug.LogError($"[Spire/BlueprintState] PlaceBuildings fix failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Split), "UpdateDividerTrails")]
    public static class DividerSortFixPatch {
        [HarmonyPostfix]
        static void Postfix(Split __instance) {
            try {
                var dividerTrailsField = AccessTools.Field(typeof(Split), "dividerTrails");
                var dividerTrails = dividerTrailsField?.GetValue(__instance) as List<Trail>;
                if (dividerTrails == null || dividerTrails.Count <= 1) return;

                // Pick the trail with the LOWEST linkId as the local reference "Forward".
                // Since pasted blueprints spawn trails sequentially, the lowest linkId trail 
                // in the pasted hub perfectly matches the lowest linkId in the original hub!
                // This means 'refDir' rotates LOCALLY with the blueprint natively!
                Trail refTrail = dividerTrails[0];
                foreach (var t in dividerTrails) {
                    if (t.linkId != 0 && (refTrail.linkId == 0 || t.linkId < refTrail.linkId)) refTrail = t;
                }

                Vector3 getDir(Trail t) {
                    Vector3 pS = t.splitStart != null ? t.splitStart.transform.position : t.transform.position;
                    Vector3 pE = t.splitEnd != null ? t.splitEnd.transform.position : t.transform.position + t.transform.forward;
                    return (pE - pS).normalized;
                }

                Vector3 refDir = getDir(refTrail);
                refDir.y = 0f;

                float GetLocalClock(Vector3 dir) {
                    dir.y = 0f;
                    float angle = Vector3.Angle(refDir, dir);
                    if (Vector3.Cross(refDir, dir).y < 0f) angle = 360f - angle; // Right-handed check
                    return angle;
                }

                dividerTrails.Sort((Trail a, Trail b) => {
                    float angleA = GetLocalClock(getDir(a));
                    float angleB = GetLocalClock(getDir(b));
                    if (Mathf.Abs(angleA - angleB) > 0.001f) return angleA.CompareTo(angleB);
                    
                    float lenA = Vector3.Distance(a.splitStart != null ? a.splitStart.transform.position : a.transform.position, a.splitEnd != null ? a.splitEnd.transform.position : a.transform.position + a.transform.forward);
                    float lenB = Vector3.Distance(b.splitStart != null ? b.splitStart.transform.position : b.transform.position, b.splitEnd != null ? b.splitEnd.transform.position : b.transform.position + b.transform.forward);
                    return lenA.CompareTo(lenB);
                });
            } catch (Exception ex) {
                Debug.LogError($"[Spire/DividerFix] Sort fix failed: {ex.Message}");
            }
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

    // ================================================================
    // COUNTER GATE SAVE/LOAD BUG FIX — ANT LEAK PREVENTION
    // ================================================================
    // Vanilla bug: after loading a save, the TrailGate_Counter's
    // counterArea (the set of trails whose ants are counted) is empty
    // until GateUpdate() runs and rebuilds it. However, Unity's
    // Update() ordering is non-deterministic — an ant's AntUpdate can
    // run BEFORE the gate's GateUpdate on the first frame after load.
    //
    // When that happens:
    //   1. Ant calls ChooseNextTrail → CheckIfTrailGateSatisfied
    //   2. TrailGate_Counter.CheckIfSatisfied iterates counterArea
    //   3. counterArea is empty → nAnts = 0 → gate looks open
    //   4. Ant passes through the full gate!
    //
    // Each save/load cycle leaks exactly one ant through the gate,
    // causing ants to progressively "disappear" from the waiting queue.
    //
    // Fix: Instead of BLOCKING the ant (which breaks complex collider
    // systems by corrupting their routing state), we force-rebuild the
    // counterArea on-the-spot if it hasn't been built yet. This gives
    // CheckIfSatisfied correct data to evaluate with immediately —
    // no blocking, no red flash, no ant leak.
    [HarmonyPatch(typeof(TrailGate_Counter), "CheckIfSatisfied")]
    public static class CounterGateLoadLeakFixPatch
    {
        static readonly FieldInfo _updateAreaField = AccessTools.Field(typeof(TrailGate_Counter), "updateArea");
        static readonly FieldInfo _counterAreaField = AccessTools.Field(typeof(TrailGate_Counter), "counterArea");
        static readonly FieldInfo _areaInvalidField = AccessTools.Field(typeof(TrailGate_Counter), "areaInvalid");
        static readonly FieldInfo _counterAreaBuildingsField = AccessTools.Field(typeof(TrailGate_Counter), "counterArea_buildings");
        static readonly FieldInfo _areaModeField = AccessTools.Field(typeof(TrailGate_Counter), "areaMode");
        static readonly FieldInfo _ownerTrailField = AccessTools.Field(typeof(TrailGate), "ownerTrail");

        [HarmonyPrefix]
        static void Prefix(TrailGate_Counter __instance)
        {
            try
            {
                if (_updateAreaField == null || _ownerTrailField == null) return;

                bool needsUpdate = (bool)_updateAreaField.GetValue(__instance);
                if (!needsUpdate) return;

                // Counter area hasn't been rebuilt yet — force rebuild NOW
                // so CheckIfSatisfied evaluates with correct data.
                var ownerTrail = _ownerTrailField.GetValue(__instance) as Trail;
                if (ownerTrail == null) return;

                var areaMode = (AreaMode)_areaModeField.GetValue(__instance);
                var counterArea = ownerTrail.GetCounterArea(areaMode, out bool areaInvalid, out var counterAreaBuildings);

                _counterAreaField.SetValue(__instance, counterArea);
                _areaInvalidField.SetValue(__instance, areaInvalid);
                _counterAreaBuildingsField.SetValue(__instance, counterAreaBuildings);
                _updateAreaField.SetValue(__instance, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Spire/CounterGateFix] Area rebuild failed: {ex.Message}");
            }
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

    // 10. CopyFrom — copy battery reference when cloning gates
    [HarmonyPatch(typeof(TrailGate_Stockpile), "CopyFrom")]
    public static class StockpileGateCopyPatch
    {
        [HarmonyPostfix]
        static void Postfix(TrailGate_Stockpile __instance, TrailGate other, TrailGate.GateCopyMode copy_mode)
        {
            if (other is TrailGate_Stockpile srcGate)
            {
                var srcBattery = BatteryGateState.GetBattery(srcGate);
                BatteryGateState.SetBattery(__instance, srcBattery);
            }
        }
    }

    // 11. CleanObjectLinks — clear battery reference cleanly
    [HarmonyPatch(typeof(TrailGate_Stockpile), "CleanObjectLinks")]
    public static class StockpileGateCleanLinksPatch
    {
        [HarmonyPostfix]
        static void Postfix(TrailGate_Stockpile __instance)
        {
            BatteryGateState.SetBattery(__instance, null);
        }
    }

    // ================================================================
    // BRIDGE SAVE/LOAD BUG FIX
    // ================================================================
    // Vanilla bug: partially-constructed bridges corrupt on save/load.
    //
    // Root causes:
    //   1. GetGroundOtherEnd() probes a single world-space point to
    //      locate the far island. If Y-rotation precision drifts even
    //      slightly across WriteYRot/ReadYRot, the probe misses.
    //   2. otherGround becomes null → DemolishBuildings crashes with
    //      NullReferenceException → delete button does nothing.
    //   3. Trail Split positions are saved as absolute coords, but
    //      geometry is rebuilt from direction, so paths float.
    //
    // Fix strategy (all in-memory, does NOT touch save format):
    //   A) After Bridge.Recreate on load, if otherGround is null,
    //      retry with a wider ground search and fix midPos/topPoint.
    //   B) Null-guard DemolishBuildings so null otherGround can't crash.
    //   C) Safety postfix on GetGroundOtherEnd to try wider probes.
    // ================================================================

    // Part A: After Bridge.Init (during_load=true), validate and fix
    // otherGround if the probe missed.
    [HarmonyPatch(typeof(Bridge), "Init")]
    public static class BridgeInitLoadFixPatch
    {
        [HarmonyPostfix]
        static void Postfix(Bridge __instance, bool during_load)
        {
            if (!during_load) return;
            try
            {
                var otherGroundField = AccessTools.Field(typeof(Bridge), "otherGround");
                if (otherGroundField == null) return;

                var otherGround = otherGroundField.GetValue(__instance) as Ground;
                if (otherGround != null) return; // probe succeeded, nothing to fix

                // otherGround is null — the direction probe missed.
                // Try to find the ground using a wider search.
                var nPiecesField = AccessTools.Field(typeof(Bridge), "nPieces");
                var pieceLengthField = AccessTools.Field(typeof(Bridge), "pieceLength");
                var connectPointField = AccessTools.Field(typeof(Bridge), "connectPoint");
                var midPosField = AccessTools.Field(typeof(Bridge), "midPos");

                if (nPiecesField == null || pieceLengthField == null || connectPointField == null) return;

                int nPieces = (int)nPiecesField.GetValue(__instance);
                float pieceLength = (float)pieceLengthField.GetValue(__instance);
                Transform connectPoint = connectPointField.GetValue(__instance) as Transform;
                if (connectPoint == null) return;

                // Get the bridge direction (same as GetDir())
                Vector3 dir = __instance.transform.TransformDirection(connectPoint.localPosition).SetY(0f).normalized;
                float cpMag = connectPoint.localPosition.SetY(0f).magnitude;

                // Try multiple probe distances (wider fan) to find the ground
                Ground foundGround = null;
                float baseDistance = (float)nPieces * pieceLength + cpMag * 3f;

                // Try offsets: exact, ±10%, ±20%, ±30%
                float[] offsets = { 0f, 0.1f, -0.1f, 0.2f, -0.2f, 0.3f, -0.3f };
                foreach (float offset in offsets)
                {
                    float dist = baseDistance * (1f + offset);
                    Vector3 probePos = __instance.transform.position + dir * dist;
                    foundGround = Toolkit.GetGround(probePos);
                    if (foundGround != null && foundGround != __instance.ground)
                    {
                        break;
                    }
                    foundGround = null;
                }

                // Also try lateral offsets if straight probes failed
                if (foundGround == null)
                {
                    Vector3 lateral = Vector3.Cross(dir, Vector3.up).normalized;
                    float[] lateralOffsets = { 5f, -5f, 10f, -10f };
                    foreach (float latOff in lateralOffsets)
                    {
                        Vector3 probePos = __instance.transform.position + dir * baseDistance + lateral * latOff;
                        foundGround = Toolkit.GetGround(probePos);
                        if (foundGround != null && foundGround != __instance.ground)
                        {
                            break;
                        }
                        foundGround = null;
                    }
                }

                if (foundGround != null)
                {
                    otherGroundField.SetValue(__instance, foundGround);
                    Debug.Log($"[Spire/BridgeFix] Fixed null otherGround for bridge at {__instance.transform.position}");
                }
                else
                {
                    Debug.LogWarning($"[Spire/BridgeFix] Could not find otherGround for bridge at {__instance.transform.position} — demolish may fail");
                }

                // Also fix midPos (used for topPoint and insert position)
                if (midPosField != null)
                {
                    Vector3 midPos = connectPoint.position + dir * ((float)nPieces * pieceLength * 0.5f);
                    midPosField.SetValue(__instance, midPos);

                    // Update topPoint to match new midPos
                    if (__instance.topPoint != null)
                    {
                        __instance.topPoint.position = new Vector3(midPos.x, __instance.topPoint.position.y, midPos.z);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Spire/BridgeFix] Init postfix failed: {ex.Message}");
            }
        }
    }

    // Part B: Null-guard DemolishBuildings — prevent NullRef when
    // otherGround is null for a corrupted bridge.
    [HarmonyPatch(typeof(Building), "DemolishBuildings")]
    public static class BridgeDemolishNullGuardPatch
    {
        [HarmonyPrefix]
        static void Prefix(List<Building> buildings)
        {
            // Pre-scan for Bridge instances with null otherGround
            // and attempt last-resort ground resolution.
            try
            {
                foreach (var building in buildings)
                {
                    if (building is Bridge bridge)
                    {
                        var otherGroundField = AccessTools.Field(typeof(Bridge), "otherGround");
                        if (otherGroundField == null) continue;

                        var otherGround = otherGroundField.GetValue(bridge) as Ground;
                        if (otherGround == null && bridge.ground != null)
                        {
                            // Last resort: just use the same ground as the start side.
                            // This ensures DemolishBuildings won't crash, and the refunded
                            // materials will go to the starting island's stockpiles.
                            otherGroundField.SetValue(bridge, bridge.ground);
                            Debug.LogWarning($"[Spire/BridgeFix] DemolishBuildings: patched null otherGround → same as start ground for bridge at {bridge.transform.position}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Spire/BridgeFix] DemolishBuildings prefix failed: {ex.Message}");
            }
        }
    }

    // Part C: Null-guard Bridge.DropPickupOnDemolish — if otherGround
    // is null, prevent crash when trying to exchange pickups.
    [HarmonyPatch(typeof(Bridge), "DropPickupOnDemolish")]
    public static class BridgeDropPickupNullGuardPatch
    {
        [HarmonyPrefix]
        static void Prefix(Bridge __instance)
        {
            try
            {
                var otherGroundField = AccessTools.Field(typeof(Bridge), "otherGround");
                if (otherGroundField == null) return;

                var otherGround = otherGroundField.GetValue(__instance) as Ground;
                if (otherGround == null && __instance.ground != null)
                {
                    otherGroundField.SetValue(__instance, __instance.ground);
                    Debug.LogWarning($"[Spire/BridgeFix] DropPickupOnDemolish: patched null otherGround for bridge at {__instance.transform.position}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Spire/BridgeFix] DropPickupOnDemolish prefix failed: {ex.Message}");
            }
        }
    }

    // ================================================================
    // BUG FIX: ANT FREEZING AT OBSCURE INTERSECTIONS
    //   If an ant's energy depleted (burst bulb) and its caste changed
    //   while actively crossing a fork, it could get permanently stuck
    //   due to dropping the fork's state handshake & gate authorization.
    //   This delays death (caste change) by short duration if it's right on a fork.
    // ================================================================

    [HarmonyPatch(typeof(Ant), "MayDie")]
    public static class AntMayDieForkSafetyPatch {
        [HarmonyPostfix]
        static void Postfix(Ant __instance, ref bool __result) {
            try {
                if (__result && __instance.currentTrail != null) {
                    float distToEnd = (1f - __instance.trailProgress) * __instance.currentTrail.length;
                    float distToStart = __instance.trailProgress * __instance.currentTrail.length;
                    
                    // Delay death if it's within 1.5 units of a trail connection
                    // (Unless the trail is a purely linear command trail)
                    if ((distToEnd < 1.5f || distToStart < 1.5f) && __instance.currentTrail.trailType != TrailType.COMMAND) {
                        __result = false;
                    }
                }
            } catch { }
        }
    }
}
