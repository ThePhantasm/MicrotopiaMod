using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace ColonySpireMod
{


    // ================================================================
    // DIVIDER SAVE/LOAD BUG FIX
    // ================================================================
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
                if (dividerTrails != null && dividerTrails.Count > 0 && dividerI >= dividerTrails.Count && dividerI < 100000) {
                    dividerIField.SetValue(__instance, 0);
                }
            } catch { }
        }
    }

    [HarmonyPatch(typeof(Split), "DividerChoose")]
    public static class DividerChooseSafetyPatch {
        [HarmonyPrefix]
        static bool Prefix(Split __instance, ref Trail next_trail, ref bool __result) {
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
                    dividerIField.SetValue(__instance, 0);
                    dividerI = 0;
                }
                for (int i = 0; i < dividerTrails.Count; i++) {
                    if (!dividerTrails[dividerI].IsGate()) break;
                    dividerI = (dividerI + 1) % dividerTrails.Count;
                }
                dividerIField.SetValue(__instance, dividerI);
            } catch { }
            return true;
        }
    }

    // ================================================================
    // EXACT IMPLEMENTATION OF THE USER'S LANE METADATA TRACKER
    // ================================================================
    public static class DividerLaneTracker {
        public static long NextLaneId = 1;
        private static System.Runtime.CompilerServices.ConditionalWeakTable<Trail, object> laneIds = new System.Runtime.CompilerServices.ConditionalWeakTable<Trail, object>();

        public static string GetSidecarPath(string saveFile) {
            return saveFile + ".dividers";
        }

        public static long GetOrMintLaneId(Trail t) {
            if (t == null) return 0;
            if (laneIds.TryGetValue(t, out object boxed) && boxed is long lid) return lid;
            long newId = NextLaneId++;
            laneIds.Add(t, newId);
            return newId;
        }

        public static long GetLaneId(Trail t) {
            if (t == null) return 0;
            if (laneIds.TryGetValue(t, out object boxed) && boxed is long lid) return lid;
            return 0;
        }
        
        public static void SetLaneId(Trail t, long id) {
            if (t == null) return;
            laneIds.Remove(t);
            laneIds.Add(t, id);
        }

        public static void LoadSidecar(string saveFile, HashSet<Trail> allTrails) {
            try {
                if (string.IsNullOrEmpty(saveFile)) return;
                string path = GetSidecarPath(saveFile);
                if (!System.IO.File.Exists(path)) {
                    Debug.Log("[Spire/Lane Tracker] No sidecar file found. Lanes will mint IDs starting at 1.");
                    NextLaneId = 1;
                    return;
                }
                string[] lines = System.IO.File.ReadAllLines(path);
                int countRestored = 0;
                foreach(var line in lines) {
                    if (line.StartsWith("N:")) {
                        if (long.TryParse(line.Substring(2), out long n)) NextLaneId = n;
                        continue;
                    }
                    string[] parts = line.Split(',');
                    if (parts.Length == 5 && parts[0] == "T") {
                        if (float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y) && float.TryParse(parts[3], out float z) && long.TryParse(parts[4], out long id)) {
                            Vector3 targetPos = new Vector3(x, y, z);
                            foreach(var t in allTrails) {
                                if (t != null && (t.transform.position - targetPos).sqrMagnitude < 0.01f) {
                                    SetLaneId(t, id);
                                    countRestored++;
                                    break;
                                }
                            }
                        }
                    }
                }
                Debug.Log($"[Spire/Lane Tracker] Restored {countRestored} lane IDs from sidecar.");
            } catch (Exception ex) { Debug.LogError($"[Spire/Lane Tracker] Failed to load sidecar: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Split), "UpdatePointer")]
    public static class SplitUpdatePointerHookPatch {
        [HarmonyPrefix]
        static void Prefix(Split __instance) {
            try {
                var divIField = AccessTools.Field(typeof(Split), "dividerI");
                var dividerTrailsCount = (AccessTools.Field(typeof(Split), "dividerTrails").GetValue(__instance) as List<Trail>)?.Count ?? 0;
                if (divIField == null || dividerTrailsCount == 0) return;

                int divI = (int)divIField.GetValue(__instance);
                if (divI >= 0 && divI < dividerTrailsCount) {
                    var trailerList = AccessTools.Field(typeof(Split), "dividerTrails").GetValue(__instance) as List<Trail>;
                    Trail active = trailerList[divI];
                    long targetLaneId = DividerLaneTracker.GetOrMintLaneId(active);
                    ModState.SetDividerTargetLane(__instance, targetLaneId);
                }
            } catch { }
        }
    }

    [HarmonyPatch(typeof(Split), "Choose")]
    public static class SplitChooseHookPatch {
        [HarmonyPostfix]
        static void Postfix(Split __instance, Collider col) {
            try {
                if (col.GetComponent<ActionPointPin>() != null || col.GetComponent<Trail>() != null) {
                    var divIField = AccessTools.Field(typeof(Split), "dividerI");
                    var dividerTrailsCount = (AccessTools.Field(typeof(Split), "dividerTrails").GetValue(__instance) as List<Trail>)?.Count ?? 0;
                    if (divIField == null || dividerTrailsCount == 0) return;
                    
                    int divI = (int)divIField.GetValue(__instance);
                    if (divI >= 0 && divI < dividerTrailsCount) {
                        var trailerList = AccessTools.Field(typeof(Split), "dividerTrails").GetValue(__instance) as List<Trail>;
                        Trail active = trailerList[divI];
                        long targetLaneId = DividerLaneTracker.GetOrMintLaneId(active);
                        ModState.SetDividerTargetLane(__instance, targetLaneId);
                    }
                }
            } catch { }
        }
    }

    [HarmonyPatch(typeof(GameManager), "LoadGameFinished")]
    public static class DividerLoadApplierPatch {
        [HarmonyPostfix]
        static void Postfix(GameManager __instance) {
            try {
                var allTrails = AccessTools.Field(typeof(GameManager), "allTrails").GetValue(__instance) as HashSet<Trail>;
                DividerLaneTracker.LoadSidecar(GlobalGameState.saveFile, allTrails);

                var allSplits = AccessTools.Field(typeof(GameManager), "allSplits").GetValue(__instance) as HashSet<Split>;
                if (allSplits == null) return;
                foreach (Split split in allSplits) {
                    long targetLaneId = ModState.GetDividerTargetLane(split);
                    if (targetLaneId > 0) {
                        var dividerTrails = AccessTools.Field(typeof(Split), "dividerTrails").GetValue(split) as List<Trail>;
                        if (dividerTrails != null) {
                            for (int i = 0; i < dividerTrails.Count; i++) {
                                if (DividerLaneTracker.GetLaneId(dividerTrails[i]) == targetLaneId) {
                                    AccessTools.Field(typeof(Split), "dividerI").SetValue(split, i);
                                    var ptrUpdate = AccessTools.Method(typeof(Split), "UpdatePointer");
                                    ptrUpdate?.Invoke(split, new object[] { false });
                                    break;
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) { Debug.Log($"[Spire/DividerLoadApplier] Post-load apply failed: {ex}"); }
        }
    }

    [HarmonyPatch(typeof(BlueprintManager), "CreateBlueprint")]
    public static class BlueprintCreatePatch {
        [HarmonyPostfix]
        static void Postfix(Blueprint __result) {
            try {
                var curBlueprint = __result;
                if (curBlueprint == null) return;
                
                // ||DIR[bpSplitStart=dirX,dirZ;...]
                string dirPayload = "||DIR[";
                
                var baseRotField = AccessTools.Field(typeof(Blueprint), "baseRot");
                Quaternion baseRot = baseRotField != null ? (Quaternion)baseRotField.GetValue(curBlueprint) : Quaternion.identity;
                Quaternion invBaseRot = Quaternion.Inverse(baseRot);

                for(int i=0; i<curBlueprint.splits.Count; i++) {
                    var bpSplit = curBlueprint.splits[i];
                    if (bpSplit.split != null && bpSplit.split.GetTrailType() == TrailType.DIVIDER) {
                        int divI = (int)AccessTools.Field(typeof(Split), "dividerI").GetValue(bpSplit.split);
                        var divTrails = AccessTools.Field(typeof(Split), "dividerTrails").GetValue(bpSplit.split) as List<Trail>;
                        if (divTrails != null && divI >= 0 && divI < divTrails.Count) {
                            Trail active = divTrails[divI];
                            Vector3 localDir = invBaseRot * active.direction;
                            dirPayload += $"{i}={localDir.x:F3},{localDir.z:F3};";
                        }
                    }
                }
                dirPayload += "]";
                
                // Optional cleanup of legacy tracking format if they edit old blueprints
                if (curBlueprint.description != null && curBlueprint.description.Contains("||SPL[")) {
                    int splStart = curBlueprint.description.IndexOf("||SPL[");
                    int splEnd = curBlueprint.description.IndexOf("]", splStart);
                    if (splEnd > splStart) curBlueprint.description = curBlueprint.description.Remove(splStart, splEnd - splStart + 1);
                }
                if (curBlueprint.description != null && curBlueprint.description.Contains("||TRL[")) {
                    int trlStart = curBlueprint.description.IndexOf("||TRL[");
                    int trlEnd = curBlueprint.description.IndexOf("]", trlStart);
                    if (trlEnd > trlStart) curBlueprint.description = curBlueprint.description.Remove(trlStart, trlEnd - trlStart + 1);
                }

                if (curBlueprint.description == null) curBlueprint.description = "";
                curBlueprint.description += dirPayload;
            } catch (Exception ex) {
                ColonySpirePlugin.Log.LogError($"BlueprintCreatePatch Postfix failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(BuildingEditing), "PlaceBuildings")]
    public static class BlueprintPlaceBuildingsPatch {
        [HarmonyPostfix]
        static void Postfix(BuildingEditing __instance) {
            var buildModeField = AccessTools.Field(typeof(BuildingEditing), "buildMode");
            if (buildModeField == null) return;
            var buildMode = (BuildMode)buildModeField.GetValue(__instance);
            
            if (buildMode == BuildMode.PlaceBlueprint) {
                var curBPRel = AccessTools.Field(typeof(BuildingEditing), "curBlueprint");
                var curBlueprint = curBPRel?.GetValue(__instance) as Blueprint;
                if (curBlueprint == null || curBlueprint.description == null) return;

                try {
                    if (curBlueprint.description.Contains("||DIR[")) {
                        int start = curBlueprint.description.IndexOf("||DIR[") + 6;
                        int end = curBlueprint.description.IndexOf("]", start);
                        if (end > start) {
                            string raw = curBlueprint.description.Substring(start, end - start);
                            var mainBldField = AccessTools.Field(typeof(BuildingEditing), "mainBuilding");
                            Building mainBld = mainBldField != null ? mainBldField.GetValue(__instance) as Building : null;
                            Quaternion worldRot = mainBld != null ? mainBld.transform.localRotation : Quaternion.identity;

                            foreach(var r in raw.Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries)) {
                                var pts = r.Split('=');
                                if (pts.Length == 2 && int.TryParse(pts[0], out int splitId)) {
                                    var coords = pts[1].Split(',');
                                    if (coords.Length == 2 && float.TryParse(coords[0], out float x) && float.TryParse(coords[1], out float z)) {
                                        Vector3 localDir = new Vector3(x, 0, z);
                                        Vector3 worldDir = (worldRot * localDir).normalized;

                                        Split spawnedSplit = curBlueprint.GetSplit(splitId);
                                        if (spawnedSplit != null) {
                                            var dividerTrails = AccessTools.Field(typeof(Split), "dividerTrails").GetValue(spawnedSplit) as List<Trail>;
                                            if (dividerTrails != null && dividerTrails.Count > 0) {
                                                int bestI = 0;
                                                float bestDot = -100f;
                                                for(int i=0; i<dividerTrails.Count; i++) {
                                                    float dot = Vector3.Dot(dividerTrails[i].direction.normalized, worldDir);
                                                    if (dot > bestDot) {
                                                        bestDot = dot;
                                                        bestI = i;
                                                    }
                                                }
                                                AccessTools.Field(typeof(Split), "dividerI").SetValue(spawnedSplit, bestI);
                                                var ptrUpdate = AccessTools.Method(typeof(Split), "UpdatePointer");
                                                ptrUpdate?.Invoke(spawnedSplit, new object[] { true });
                                                long laneId = DividerLaneTracker.GetOrMintLaneId(dividerTrails[bestI]);
                                                ModState.SetDividerTargetLane(spawnedSplit, laneId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    ColonySpirePlugin.Log.LogError($"BlueprintPlaceBuildingsPatch Postfix failed: {ex.Message}");
                }
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
                    
                    float safeDist = Mathf.Min(1.5f, __instance.currentTrail.length * 0.45f);
                    if ((distToEnd <= safeDist || distToStart <= safeDist) && __instance.currentTrail.trailType != TrailType.COMMAND) {
                        __result = false;
                    }
                }
            } catch { }
        }
    }

    // ================================================================
    // LIFE GATE RANGE INCREASE LIMIT FIX
    // Allows Life Gate to be configured up to 6400.
    // ================================================================

    [HarmonyPatch(typeof(UIClickLayout_TrailGateLife), "SetGate")]
    public static class LifeGateMaximumIncreasePatch {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                if (instruction.opcode == OpCodes.Ldc_R4 && (instruction.operand is float f) && f == 600f) {
                    instruction.operand = 6400f;
                }
                yield return instruction;
            }
        }
    }
}
