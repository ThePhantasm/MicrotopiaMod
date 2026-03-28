using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColonySpireMod
{
    // ================================================================
    // LOCALIZATION — inject English strings for custom tech nodes
    // ================================================================
    [HarmonyPatch(typeof(Loc), "GetTechTree")]
    public static class ModLocPatch {
        static readonly Dictionary<string, string> modStrings = new() {
            { "TECHTREE_MOD_QUEEN_T2",            "Queen Larvae T2" },
            { "TECHTREE_MOD_QUEEN_T2_DESC",       "The Queen can now produce T2 Soldier larvae. Press G while viewing the Queen to cycle tiers." },
            { "TECHTREE_MOD_QUEEN_T3",            "Queen Larvae T3" },
            { "TECHTREE_MOD_QUEEN_T3_DESC",       "The Queen can now produce T3 Royal larvae. Press G while viewing the Queen to cycle tiers." },
            { "TECHTREE_MOD_COLORED_TRAILS",      "Colored Trails" },
            { "TECHTREE_MOD_COLORED_TRAILS_DESC", "Unlock vibrant color options for Main Bus trails. Choose from Cyan, Magenta, Lime, Orange, and Blue variants." },
            { "TECHTREE_MOD_COLONY_SPIRE",        "Colony Spire" },
            { "TECHTREE_MOD_COLONY_SPIRE_DESC",   "Unlock the Colony Spire, a powerful endgame building that provides permanent colony-wide upgrades and can hatch Sentinels." },
            { "TECHTREE_MOD_T4_ENDGAME",           "T4 Endgame Mastery" },
            { "TECHTREE_MOD_T4_ENDGAME_DESC",      "Unlock T4 endgame systems: Island Furnace converts any material to energy, Deep Excavator spawns infinite resource deposits, and Dynamos produce ultra-dense energy pods." },
            { "PICKUP_ENERGYPOD11",                "Ultra Energy Pod" },
            { "PICKUP_ENERGYPOD11_DESC",           "An impossibly dense energy sphere. Contains 12,500 units of stored energy." },
            { "FACRECIPE_REAC_ENERGY11",           "Ultra Energy Pod" },
            { "PICKUP_LIQUID_IRON",                "Liquid Iron" },
            { "PICKUP_LIQUID_IRON_DESC",           "Molten iron, smelted using plant fiber as fuel. Used in advanced manufacturing." },
            { "PICKUP_LIQUID_COPPER",              "Liquid Copper" },
            { "PICKUP_LIQUID_COPPER_DESC",         "Molten copper, smelted using plant fiber as fuel. Used in advanced manufacturing." },
            { "FACRECIPE_MOD_SMELT_IRON",          "Smelt Iron" },
            { "FACRECIPE_MOD_SMELT_COPPER",        "Smelt Copper" },
            { "PICKUP_DRIED_FIBER",                "Dried Fiber" },
            { "PICKUP_DRIED_FIBER_DESC",           "Plant fibers dried on a rack. Burns hot enough to smelt raw metal ore." },
            { "FACRECIPE_DRY_FIBER",               "Dry Fiber" },
            { "PICKUP_ALLOY_FRAME",                "Alloy Frame" },
            { "PICKUP_ALLOY_FRAME_DESC",           "A reinforced structural frame forged from liquid iron and iron plates. Essential for advanced ant engineering." },
            { "PICKUP_CIRCUIT_BOARD",              "Circuit Board" },
            { "PICKUP_CIRCUIT_BOARD_DESC",         "A precision circuit etched from copper wire and liquid copper. Controls the neural pathways of advanced ants." },
            { "PICKUP_LARVAE_T4",                  "T4 Omni-Ant Larva" },
            { "PICKUP_LARVAE_T4_DESC",             "An augmented larva enhanced with alloy and circuitry. Will hatch into the ultimate Omni-Ant." },
            { "FACRECIPE_ASSEMBLE_ALLOY",          "Forge Alloy Frame" },
            { "FACRECIPE_ASSEMBLE_CIRCUIT",        "Etch Circuit Board" },
            { "FACRECIPE_ASSEMBLE_T4_LARVA",       "Augment T4 Larva" },
            { "FACRECIPE_GROW_LARVAE_T4",          "Hatch Omni-Ant" },
        };

        [HarmonyPrefix]
        static bool Prefix(string code, ref string __result) {
            if (modStrings.TryGetValue(code, out var text)) {
                __result = text;
                return false;
            }
            return true;
        }
    }

    // ================================================================
    // CUSTOM PICKUP TYPE PARSING
    // ================================================================
    static class ModPickupTypes {
        public const int ENERGY_POD11  = 111;
        public const int LIQUID_IRON   = 600;
        public const int LIQUID_COPPER = 601;
        public const int DRIED_FIBER   = 602;
        public const int ALLOY_FRAME   = 603;
        public const int CIRCUIT_BOARD = 604;
        public const int LARVAE_T4     = 403;

        public static readonly Dictionary<string, PickupType> customTypes = new() {
            { "ENERGY_POD11",  (PickupType)ENERGY_POD11 },
            { "LIQUID_IRON",   (PickupType)LIQUID_IRON },
            { "LIQUID_COPPER", (PickupType)LIQUID_COPPER },
            { "DRIED_FIBER",   (PickupType)DRIED_FIBER },
            { "ALLOY_FRAME",   (PickupType)ALLOY_FRAME },
            { "CIRCUIT_BOARD", (PickupType)CIRCUIT_BOARD },
            { "LARVAE_T4",     (PickupType)LARVAE_T4 },
        };

        public static bool TryParse(string str, out PickupType result) {
            if (string.IsNullOrEmpty(str)) { result = PickupType.NONE; return false; }
            return customTypes.TryGetValue(str.Trim(), out result);
        }
    }

    [HarmonyPatch(typeof(PickupData), "ParsePickupType")]
    public static class PickupTypeParsePatch {
        [HarmonyPrefix]
        static bool Prefix(string str, ref PickupType __result) {
            if (ModPickupTypes.TryParse(str, out var modType)) {
                __result = modType;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PickupCost), "Parse")]
    public static class PickupCostParsePatch {
        [HarmonyPrefix]
        static bool Prefix(PickupCost __instance, string txt, string[] strs) {
            if (strs == null || strs.Length < 2) return true;
            string typeStr = strs[0].Trim();
            if (ModPickupTypes.TryParse(typeStr, out var modType)) {
                __instance.type = modType;
                __instance.category = PickupCategory.NONE;
                if (int.TryParse(strs[1].Trim(), out int amount)) {
                    __instance.intValue = amount;
                }
                return false;
            }
            return true;
        }
    }

    // ================================================================
    // PRESTIGE + SPEED
    // ================================================================
    [HarmonyPatch(typeof(GyneTower), "StartGyne")]
    public static class PrestigePatch {
        [HarmonyPostfix] static void Postfix() {
            Debug.Log($"[Spire] StartGyne fired — prestige level is {ModState.prestigeLevel}");
        }
    }
    [HarmonyPatch(typeof(Ant), "GetSpeed")]
    public static class SpeedPatch {
        [HarmonyPostfix] static void Postfix(ref float __result) {
            if (ModState.pheromoneLevel > 0) __result *= (1f + ModState.pheromoneLevel * 0.05f);
        }
    }

    // ================================================================
    // QUEEN — cycle button + G key + actual larva tier redirect
    // ================================================================
    [HarmonyPatch(typeof(Queen), "BuildingUpdate")]
    public static class QueenBuildingUpdatePatch {
        public static int activeTier = 1;

        [HarmonyPrefix] static void Prefix(Queen __instance) {
            var qd = ModState.GetQueen(__instance);
            int maxTier = ModState.MaxQueenTier;
            if (qd.larvaOutputTier > maxTier) qd.larvaOutputTier = maxTier;
            activeTier = qd.larvaOutputTier;
            ModState.displayTier = qd.larvaOutputTier;
            if (Input.GetKeyDown(KeyCode.G)) {
                qd.larvaOutputTier = (qd.larvaOutputTier % maxTier) + 1;
                activeTier = qd.larvaOutputTier;
            }
        }
        [HarmonyPostfix] static void Postfix() { activeTier = 1; }
    }

    [HarmonyPatch(typeof(GameManager), "SpawnPickup",
        new[] { typeof(PickupType), typeof(Vector3), typeof(Quaternion), typeof(Save) })]
    public static class SpawnPickupPatch {
        [HarmonyPrefix] static void Prefix(ref PickupType _type) {
            if ((int)_type == 400 && QueenBuildingUpdatePatch.activeTier > 1)
                _type = (PickupType)(399 + QueenBuildingUpdatePatch.activeTier);
        }
    }
    [HarmonyPatch(typeof(Queen), "SetClickUi")]
    public static class QueenSetClickUiPatch {
        static readonly string[] Labels = { "T1 Worker", "T2 Soldier", "T3 Royal" };
        [HarmonyPostfix] static void Postfix(Queen __instance, UIClickLayout __0) {
            var qd = ModState.GetQueen(__instance);
            int maxTier = ModState.MaxQueenTier;
            if (qd.larvaOutputTier > maxTier) qd.larvaOutputTier = maxTier;
            string tierInfo = maxTier > 1 ? $" [T{qd.larvaOutputTier}]" : "";
            string prestigeInfo = $" ★{ModState.prestigeLevel} [{ModState.prestigePoints}/{ModState.GetPrestigeThreshold()}] 🛡{ModState.excavationCores}";
            __0.SetTitle(__instance.data.GetTitle() + tierInfo + prestigeInfo);
            try {
                var btn = __0.GetButton((UIClickButtonType)50, false);

                if (btn == null) {
                    btn = CreateQueenTierButton(__0);
                }

                if (btn != null) {
                    if (maxTier <= 1) {
                        __0.UpdateButton((UIClickButtonType)50, false, "", false);
                    } else {
                        btn.SetButton(() => {
                            int mt = ModState.MaxQueenTier;
                            qd.larvaOutputTier = (qd.larvaOutputTier % mt) + 1;
                            ModSave.Save(qd.larvaOutputTier);
                            string ti = $" [T{qd.larvaOutputTier}]";
                            __0.SetTitle(__instance.data.GetTitle() + ti + $" ★{ModState.prestigeLevel}");
                            __0.UpdateButton((UIClickButtonType)50, true, Labels[qd.larvaOutputTier - 1], true);
                        }, (InputAction)0);
                        __0.UpdateButton((UIClickButtonType)50, true, Labels[qd.larvaOutputTier - 1], true);
                    }
                    Debug.Log($"[Spire] Queen tier button active: T{qd.larvaOutputTier}");
                } else {
                    Debug.LogWarning("[Spire] Queen tier button: could not create or find button");
                }
            } catch (Exception ex) { Debug.Log($"[Spire] Queen btn: {ex.Message}"); }
        }

        static ButtonWithHotkey CreateQueenTierButton(UIClickLayout layout) {
            try {
                var listField = AccessTools.Field(typeof(UIClickLayout), "buttonsWithHotkey");
                if (listField == null) return null;
                var buttons = listField.GetValue(layout) as List<ButtonWithHotkey>;
                if (buttons == null || buttons.Count == 0) return null;

                ButtonWithHotkey template = null;
                foreach (var b in buttons) {
                    if (b.btButton_better != null && b.btButton_better.gameObject != null) {
                        template = b;
                        break;
                    }
                }
                if (template == null) template = buttons[0];
                if (template == null) return null;

                GameObject sourceGo = template.btButton_better?.gameObject ?? template.btButton?.gameObject;
                if (sourceGo == null) return null;

                var clonedGo = UnityEngine.Object.Instantiate(sourceGo, sourceGo.transform.parent);
                clonedGo.name = "BtQueenTier_Spire";
                clonedGo.SetActive(true);

                var newBtn = new ButtonWithHotkey();
                newBtn.buttonType = (UIClickButtonType)50;

                var clonedBetter = clonedGo.GetComponent<UITextImageButton>();
                if (clonedBetter != null) {
                    newBtn.btButton_better = clonedBetter;
                } else {
                    var clonedSimple = clonedGo.GetComponent<UIButton>();
                    if (clonedSimple != null) newBtn.btButton = clonedSimple;
                }

                var tmpText = clonedGo.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmpText != null) newBtn.lbButton = tmpText;

                if (newBtn.obHotkey != null) newBtn.obHotkey.SetActive(false);

                buttons.Add(newBtn);
                Debug.Log("[Spire] Dynamically created Queen tier button (cloned from existing layout button)");
                return newBtn;
            } catch (Exception ex) {
                Debug.Log($"[Spire] CreateQueenTierButton failed: {ex.Message}");
                return null;
            }
        }
    }
    [HarmonyPatch(typeof(Queen), "Init")]
    public static class QueenInitPatch {
        [HarmonyPostfix] static void Postfix(Queen __instance) {
            var qd = ModState.GetQueen(__instance);
            if (!qd.initialized) {
                qd.larvaOutputTier = ModSave.LoadQueenTier();
                qd.initialized = true;
                Debug.Log($"[Spire] Queen init: loaded T{qd.larvaOutputTier}");
            }
        }
    }

    // ================================================================
    // COLONY SPIRE — RadarIslandScanner, Unlocker panel
    // ================================================================
    [HarmonyPatch(typeof(RadarIslandScanner), "GetUiClickType_Intake")]
    public static class SpireClickTypePatch {
        [HarmonyPrefix] static bool Prefix(RadarIslandScanner __instance, ref UIClickType __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = (UIClickType)29; return false;
        }
    }

    [HarmonyPatch(typeof(RadarIslandScanner), "Init")]
    public static class SpireInitPatch {
        [HarmonyPostfix] static void Postfix(RadarIslandScanner __instance) {
            if (!SpireHelper.IsSpire(__instance)) return;
            try {
                AccessTools.Field(typeof(Unlocker), "unlockerType").SetValue(__instance, (UnlockerType)1);
                int savedTrack = ModSave.LoadSpireTrack();
                ModState.SetSpireTrack(__instance, savedTrack);
                Debug.Log($"[Spire] Init: restored track {savedTrack} ({ModState.GetTrackName(savedTrack)})");
                AccessTools.Field(typeof(Building), "showInventory")?.SetValue(__instance, true);
                __instance.transform.localScale = new Vector3(1.1f, 1.6f, 1.1f);
                foreach (var r in __instance.GetComponentsInChildren<MeshRenderer>()) {
                    r.material.SetColor("_Color", new Color(0.38f, 0.06f, 0.62f, 1f));
                    r.material.SetColor("_EmissionColor", new Color(1.8f, 1.3f, 0.0f, 1f));
                    r.material.EnableKeyword("_EMISSION");
                }

                if (__instance.gameObject.GetComponent<DeepExcavatorBehavior>() == null) {
                    var excavator = __instance.gameObject.AddComponent<DeepExcavatorBehavior>();
                    excavator.spireBuilding = __instance;
                    Debug.Log("[Spire] Deep Excavator attached to Colony Spire");
                }

                Debug.Log("[Spire] Init done");
            } catch (Exception ex) { Debug.Log($"[Spire] Init failed: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(RadarIslandScanner), "BuildingUpdate")]
    public static class SpireBuildingUpdatePatch {
        [HarmonyPostfix] static void Postfix(RadarIslandScanner __instance, float dt) {
            if (!SpireHelper.IsSpire(__instance)) return;

            for (int k = 0; k < ModState.TrackNames.Length; k++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + k))
                    ModState.SetSpireTrack(__instance, k);

            if (ModState.sentinelHatchTimer > 0f && ModState.sentinelSpire == __instance) {
                ModState.sentinelHatchTimer -= dt;
                if (ModState.sentinelHatchTimer <= 0f) {
                    ModState.sentinelHatchTimer = -1f;
                    ModState.sentinelSpire = null;
                    try {
                        var pos = __instance.transform.position;
                        var rot = UnityEngine.Random.rotation;
                        var sentinel = GameManager.instance.SpawnAnt(
                            (AntCaste)38, pos, rot, null
                        );
                        if (sentinel != null) {
                            AccessTools.Method(typeof(Building), "DropAntOnGround")
                                ?.Invoke(__instance, new object[] { sentinel });
                            ModState.sentinelHatched++;
                            ModSave.Save();
                            Debug.Log($"[Spire] Sentinel #{ModState.sentinelHatched} hatched!");
                        }
                    } catch (Exception ex) {
                        Debug.LogError($"[Spire] Sentinel spawn failed: {ex.Message}");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Unlocker), "SetUnlock")]
    public static class UnlockerSetUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, string _unlock) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int idx = ModState.TrackIndexFromCode(_unlock);
            if (idx >= 0) ModState.SetSpireTrack(__instance, idx);
            AccessTools.Field(typeof(Unlocker), "currentUnlock").SetValue(__instance, null);
            return false;
        }
    }

    [HarmonyPatch(typeof(UIClickLayout_Unlocker), "SetUnlockName")]
    public static class SetUnlockNamePatch {
        [HarmonyPrefix] static bool Prefix(UIClickLayout_Unlocker __instance, Unlocker unlocker) {
            if (!SpireHelper.IsSpire(unlocker)) return true;
            try {
                int idx = ModState.GetSpireTrack(unlocker);
                string label = $"{ModState.GetTrackName(idx)} — Lv{ModState.GetTrackLevel(idx)}";
                var lbResult = AccessTools.Field(typeof(UIClickLayout_Unlocker), "lbUnlockResult")?.GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (lbResult != null) lbResult.text = label;
                var lbWill = AccessTools.Field(typeof(UIClickLayout_Unlocker), "lbWillUnlock")?.GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (lbWill != null) lbWill.text = "Upgrade Track:";
                var sprite = AccessTools.Field(typeof(UIClickLayout_Unlocker), "uiSprite")?.GetValue(__instance) as GameObject;
                if (sprite != null) sprite.SetActive(false);
            } catch (Exception ex) { Debug.Log($"[Spire] SetUnlockName: {ex.Message}"); }
            return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "PickUnlock")]
    public static class UnlockerPickUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int current = ModState.GetSpireTrack(__instance);
            int next = (current + 1) % ModState.TrackCodes.Length;
            ModState.SetSpireTrack(__instance, next);
            AccessTools.Field(typeof(Unlocker), "currentUnlock").SetValue(__instance, null);
            return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "AnythingToUnlock")]
    public static class UnlockerAnythingToUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, ref bool __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = true; return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "GetAvailableBiomeRevealsCount")]
    public static class UnlockerGetAvailableCountPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, ref int __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = ModState.TrackCodes.Length; return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "EAvailableBiomeReveals")]
    public static class UnlockerEAvailablePatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, ref IEnumerable<string> __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = ModState.TrackCodes; return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "CanInsert_Intake")]
    public static class UnlockerCanInsertPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, PickupType _type, ref bool __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            if (ModState.sentinelHatchTimer > 0f) { __result = false; return false; }
            int track = ModState.GetSpireTrack(__instance);
            var costs = ModState.GetTrackCost(track);
            foreach (var (type, count) in costs) {
                if ((int)_type == (int)type) {
                    int have = __instance.GetCollectedAmount(_type, (BuildingStatus)4, true);
                    __result = have < count;
                    return false;
                }
            }
            __result = false; return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "DoUnlock")]
    public static class UnlockerDoUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int track = ModState.GetSpireTrack(__instance);
            if (track == 5 && ModState.sentinelHatchTimer > 0f) {
                Debug.Log("[Spire] Sentinel still hatching, please wait!");
                return false;
            }
            var costs = ModState.GetTrackCost(track);
            foreach (var (type, count) in costs)
                __instance.RemovePickup(type, count, (BuildingStatus)4);
            if (track == 5) ModState.sentinelSpire = __instance as RadarIslandScanner;
            ModState.UpgradeTrack(track);
            return false;
        }
    }

    [HarmonyPatch(typeof(Unlocker), "GatherRecipeProgress")]
    public static class UnlockerGatherProgressPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance,
            ref List<(AntCaste, string)> ant_icons,
            ref List<(PickupType, string)> pickup_icons,
            ref bool go) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int track = ModState.GetSpireTrack(__instance);
            ant_icons = new List<(AntCaste, string)>();
            pickup_icons = new List<(PickupType, string)>();
            if (track == 5 && ModState.sentinelHatchTimer > 0f) {
                int secs = Mathf.CeilToInt(ModState.sentinelHatchTimer);
                pickup_icons.Add(((PickupType)328, $"Hatching... {secs}s"));
                go = false;
                return false;
            }
            var costs = ModState.GetTrackCost(track);
            go = true;
            foreach (var (type, count) in costs) {
                int have = __instance.GetCollectedAmount(type, (BuildingStatus)4, false);
                pickup_icons.Add((type, $"{have}/{count}"));
                if (have < count) go = false;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(UIClickLayout_Unlocker), "SetChangeButton")]
    public static class SetChangeButtonOverridePatch {
        [HarmonyPrefix] static bool Prefix(UIClickLayout_Unlocker __instance, Unlocker unlocker) {
            if (!SpireHelper.IsSpire(unlocker)) return true;
            try {
                var btChange = AccessTools.Field(typeof(UIClickLayout_Unlocker), "btChange")?.GetValue(__instance) as UITextImageButton;
                if (btChange == null) return false;
                btChange.gameObject.SetActive(true);
                btChange.SetButton(() => {
                    int current = ModState.GetSpireTrack(unlocker);
                    int next = (current + 1) % ModState.TrackCodes.Length;
                    ModState.SetSpireTrack(unlocker, next);
                    AccessTools.Method(typeof(UIClickLayout_Unlocker), "SetUnlockName")
                        ?.Invoke(__instance, new object[] { unlocker });
                });
                btChange.SetText($"▶ Change Track");
            } catch (Exception ex) { Debug.Log($"[Spire] SetChangeButton: {ex.Message}"); }
            return false;
        }
    }
    [HarmonyPatch(typeof(Progress), "Read")]
    public static class AutoUnlockPatch {
        [HarmonyPostfix] static void Postfix() {
            try {
                if (ModState.ModTechResearched("MOD_COLONY_SPIRE")) {
                    Progress.UnlockBuilding("COLONY_SPIRE", true);
                    Debug.Log("[Spire] Colony Spire unlocked (tech researched)");
                } else {
                    Debug.Log("[Spire] Colony Spire locked — research MOD_COLONY_SPIRE first");
                }
                ModSave.Load();
            }
            catch (Exception ex) { Debug.Log($"[Spire] Auto-unlock: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Progress), "Write")]
    public static class SaveOnWritePatch {
        [HarmonyPostfix] static void Postfix() {
            try { ModSave.Save(); }
            catch (Exception ex) { Debug.Log($"[Spire] SaveOnWrite: {ex.Message}"); }
        }
    }

    // ================================================================
    // MOLD RESISTANCE, MINING SPEED, WING CARRY, ENERGY, GATHERER, HUD
    // ================================================================
    [HarmonyPatch(typeof(StatusEffects), "CombineEffects")]
    public static class MoldResistancePatch {
        [HarmonyPostfix] static void Postfix(StatusEffects __instance) {
            if (ModState.carapaceLevel <= 0) return;
            float reduction = Math.Max(0f, 1f - ModState.carapaceLevel * 0.2f);
            var field = AccessTools.Field(typeof(StatusEffects), "lifeDrainFactor");
            float current = (float)field.GetValue(__instance);
            if (current > 1f) {
                field.SetValue(__instance, 1f + (current - 1f) * reduction);
            }
        }
    }

    [HarmonyPatch(typeof(BiomeObject), "GetMineDuration")]
    public static class MiningSpeedPatch {
        [HarmonyPostfix] static void Postfix(ref float __result) {
            if (ModState.miningLevel <= 0) return;
            float multiplier = 1f + ModState.miningLevel * 0.15f;
            __result /= multiplier;
        }
    }

    [HarmonyPatch(typeof(Ant), "Fill")]
    public static class WingCarryPatch {
        [HarmonyPostfix] static void Postfix(Ant __instance, AntCasteData _data) {
            if (_data == null) return;

            if ((int)_data.caste == ModState.OMNI_ANT_CASTE_ID) {
                var carryField = AccessTools.Field(typeof(Ant), "carryCapacity");
                var speedField = AccessTools.Field(typeof(Ant), "speed");

                carryField?.SetValue(__instance, ModState.GetOmniAntCarry());

                if (speedField != null) {
                    float baseSpeed = (float)speedField.GetValue(__instance);
                    speedField.SetValue(__instance, baseSpeed * 2.0f);
                }

                __instance.energy = 600f;

                var flyField = AccessTools.Field(typeof(Ant), "canFly");
                flyField?.SetValue(__instance, true);

                Debug.Log($"[Spire] T4 Omni-Ant spawned! Carry={ModState.GetOmniAntCarry()} Speed=2x Lifespan=600s Fly=true");
                return;
            }

            if (ModState.wingLevel <= 0) return;
            if (!_data.flying) return;
            var field = AccessTools.Field(typeof(Ant), "carryCapacity");
            int current = (int)field.GetValue(__instance);
            field.SetValue(__instance, current + ModState.wingLevel);
            Debug.Log($"[Spire] WingCarry: {_data.caste} carry {current} -> {current + ModState.wingLevel}");
        }
    }

    [HarmonyPatch(typeof(GyneTower), "CheckIfGateIsSatisfied")]
    public static class GyneTowerPrestigePatch {
        [HarmonyPrefix] static bool Prefix(GyneTower __instance, Ant ant, ref bool __result) {
            if (ant == null) return true;

            var casteField = AccessTools.Field(typeof(Ant), "caste");
            if (casteField == null) return true;
            int caste = (int)casteField.GetValue(ant);
            if (caste != ModState.OMNI_ANT_CASTE_ID) {
                return true;
            }

            bool isElder = false;
            try {
                isElder = ant.HasStatusEffect(StatusEffect.OLD);
            } catch { }

            if (!isElder) {
                __result = false;
                return false;
            }

            ModState.prestigePoints += ModState.PRESTIGE_POINTS_PER_OMNI;
            Debug.Log($"[Spire] Elder Omni-Ant sacrificed! +{ModState.PRESTIGE_POINTS_PER_OMNI} points. Total: {ModState.prestigePoints}/{ModState.GetPrestigeThreshold()}");

            ant.Die(DeathCause.OLD_AGE);

            if (ModState.prestigePoints >= ModState.GetPrestigeThreshold()) {
                ModState.prestigePoints = 0;
                ModState.prestigeLevel++;
                Debug.Log($"[Spire] ★★★ SUPER GYNE LAUNCH! Prestige now level {ModState.prestigeLevel} ★★★");

                try {
                    AccessTools.Method(typeof(GyneTower), "StartGyne")?.Invoke(__instance, null);
                } catch (Exception ex) {
                    Debug.LogError($"[Spire] StartGyne invoke failed: {ex.Message}");
                }

                var pos = __instance.transform.position;
                for (int i = 0; i < 10; i++) {
                    var offset = UnityEngine.Random.insideUnitSphere * 5f;
                    offset.y = Mathf.Abs(offset.y) + 2f;
                    GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF5, pos + offset);
                }
            }

            ModSave.Save();
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Gatherer), "UseBuilding")]
    public static class GathererDelayPatch {
        [HarmonyPrefix] static void Prefix(Gatherer __instance) {
            if (ModState.gathererLevel <= 0) return;
            float newDelay = Math.Max(0f, 1f - ModState.gathererLevel * 0.2f);
            var field = AccessTools.Field(typeof(Gatherer), "DELAY_INITIAL");
            if (field != null) field.SetValue(__instance, newDelay);
        }
    }

    [HarmonyPatch(typeof(Ant), "GetMaxEnergy")]
    public static class EnergyDrainPatch {
        [HarmonyPostfix] static void Postfix(ref float __result) {
            if (ModState.energyLevel <= 0) return;
            float multiplier = 1f + ModState.energyLevel * 0.05f;
            __result *= multiplier;
        }
    }

    [HarmonyPatch(typeof(UIGame), "UpdateHungerBar")]
    public static class LarvaRateHudPatch {
        static readonly float[] TierDivisors = { 1f, 1f, 3f, 9f };
        static readonly string[] TierLabels  = { "",  "T1", "T2", "T3" };

        [HarmonyPostfix] static void Postfix(UIGame __instance, float larva_rate) {
            int lTier = Math.Max(1, Math.Min(3, ModState.displayTier));
            if (lTier <= 1) return;

            try {
                float adjustedRate = larva_rate / TierDivisors[lTier];
                adjustedRate = Mathf.Round(adjustedRate * 10f) / 10f;

                var lbRate = AccessTools.Field(typeof(UIGame), "lbLarvaRate")
                    .GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (lbRate != null)
                    lbRate.text = $"{adjustedRate} [{TierLabels[lTier]}]";

                var lbUnit = AccessTools.Field(typeof(UIGame), "lbLarvaUnit")
                    .GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (lbUnit != null)
                    lbUnit.text = "/min";
            } catch (Exception ex) {
                Debug.Log($"[Spire] LarvaRateHud: {ex.Message}");
            }
        }
    }

    // ================================================================
    // ISLAND SCALE
    // ================================================================
    [HarmonyPatch(typeof(Ground), "InitShape")]
    public static class GroundInitShapePatch {
        static float[] origRadii;
        static Vector3[] origCenters;
        
        [HarmonyPrefix]
        static void Prefix(Ground __instance) {
            if (GameManager.instance == null) return;
            if (GameManager.instance.GetGroundCount() == 0 && ModState.activeIslandScale != 1.0f) {
                var stField = AccessTools.Field(typeof(Ground), "shapeTransform");
                if (stField == null) return;
                var shapeTransform = stField.GetValue(__instance) as Transform;
                if (shapeTransform == null) return;

                var colliders = shapeTransform.GetComponents<SphereCollider>();
                origRadii = new float[colliders.Length];
                origCenters = new Vector3[colliders.Length];
                for (int i=0; i < colliders.Length; i++) {
                    origRadii[i] = colliders[i].radius;
                    origCenters[i] = colliders[i].center;
                    colliders[i].radius *= ModState.activeIslandScale;
                    colliders[i].center *= ModState.activeIslandScale;
                }
            }
        }
        
        [HarmonyPostfix]
        static void Postfix(Ground __instance) {
            if (origRadii != null) {
                var stField = AccessTools.Field(typeof(Ground), "shapeTransform");
                var shapeTransform = stField?.GetValue(__instance) as Transform;
                if (shapeTransform != null) {
                    var colliders = shapeTransform.GetComponents<SphereCollider>();
                    for (int i=0; i < colliders.Length; i++) {
                        colliders[i].radius = origRadii[i];
                        colliders[i].center = origCenters[i];
                    }
                }
                origRadii = null;
                origCenters = null;
            }
        }
    }

    [HarmonyPatch(typeof(Ground), "Create")]
    public static class GroundCreatePatch {
        [HarmonyPostfix]
        static void Postfix(Ground __result) {
            if (__result != null && GameManager.instance != null && GameManager.instance.GetGroundCount() == 0) {
                __result.transform.localScale = Vector3.one * ModState.activeIslandScale;
                Debug.Log($"[Spire] Scaled initial island to {ModState.activeIslandScale}");
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "SaveGame")]
    public static class IslandScaleSavePatch {
        [HarmonyPostfix]
        static void Postfix(bool __result, string save_name) {
            if (!__result) return;
            try {
                string path = ModState.GetIslandScalePath(save_name);
                System.IO.File.WriteAllText(path, ModState.activeIslandScale.ToString(System.Globalization.CultureInfo.InvariantCulture));
            } catch (Exception ex) {
                Debug.Log($"[Spire] IslandScale save failed: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "KStartLoadGame")]
    public static class IslandScaleLoadPatch {
        [HarmonyPrefix]
        static void Prefix(string save_name) {
            if (string.IsNullOrEmpty(save_name)) {
                ModState.activeIslandScale = ModState.islandScale;
                return;
            }
            try {
                string path = ModState.GetIslandScalePath(save_name);
                if (System.IO.File.Exists(path)) {
                    if (float.TryParse(System.IO.File.ReadAllText(path), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale)) {
                        ModState.activeIslandScale = scale;
                        return;
                    }
                }
                ModState.activeIslandScale = 1.0f;
            } catch (Exception ex) {
                Debug.Log($"[Spire] IslandScale load failed: {ex}");
                ModState.activeIslandScale = 1.0f;
            }
        }
    }
}
