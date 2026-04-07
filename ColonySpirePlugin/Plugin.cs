using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ColonySpireMod
{
    [BepInPlugin("com.colonyspire.mod", "Colony Spire Mod", "1.1.10")]
    public class ColonySpirePlugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            Logger.LogInfo("Colony Spire Mod loading...");
            var harmony = new Harmony("com.colonyspire.mod");

            // Load settings early so toggles and scale defaults are ready
            ModSave.Load();

            // Build the patch list based on feature toggles
            var patchClasses = new List<Type>();

            // ── Always-on: Settings UI (must always load so players can toggle features) ──
            patchClasses.AddRange(new[] {
                typeof(UISettingsWorldPatch),
                typeof(UIWorldSettingsInitPatch),
            });

            // ── Colony Spire Mod (master toggle) ──
            if (ModState.enableColonySpire) {
                Logger.LogInfo("[Master] Colony Spire Mod: ENABLED");

                // Core mod infrastructure: Island Scale, Save persistence, Localization, Custom Pickup Types
                patchClasses.AddRange(new[] {
                    typeof(GroundInitShapePatch),
                    typeof(GroundCreatePatch),
                    typeof(ModLocPatch),
                    typeof(IslandScaleSavePatch),
                    typeof(IslandScaleLoadPatch),
                    typeof(PickupTypeParsePatch),
                    typeof(PickupCostParsePatch),
                });

                // ── Prestige System ──
                if (ModState.enablePrestige) {
                    Logger.LogInfo("[Feature] Prestige System: ENABLED");
                    patchClasses.AddRange(new[] {
                        typeof(PrestigePatch),
                        typeof(SpeedPatch),
                        typeof(QueenBuildingUpdatePatch),
                        typeof(QueenSetClickUiPatch),
                        typeof(QueenInitPatch),
                        typeof(SpawnPickupPatch),
                        typeof(SpireInitPatch),
                        typeof(SpireBuildingUpdatePatch),
                        typeof(SpireClickTypePatch),
                        typeof(UnlockerSetUnlockPatch),
                        typeof(UnlockerPickUnlockPatch),
                        typeof(UnlockerAnythingToUnlockPatch),
                        typeof(UnlockerGetAvailableCountPatch),
                        typeof(UnlockerEAvailablePatch),
                        typeof(UnlockerCanInsertPatch),
                        typeof(UnlockerDoUnlockPatch),
                        typeof(UnlockerGatherProgressPatch),
                        typeof(SetUnlockNamePatch),
                        typeof(SetChangeButtonOverridePatch),
                        typeof(SaveOnWritePatch),
                        typeof(AutoUnlockPatch),
                        typeof(MoldResistancePatch),
                        typeof(MiningSpeedPatch),
                        typeof(WingCarryPatch),
                        typeof(GyneTowerPrestigePatch),
                        typeof(LarvaRateHudPatch),
                        typeof(GathererDelayPatch),
                        // Phase 2: Island Furnace
                        typeof(FurnaceCanInsertPatch),
                        typeof(FurnaceOnArrivalPatch),
                        typeof(FurnaceTitlePatch),
                        typeof(FurnaceHoverPatch),
                        // Phase 2.5: Tech Tree Color + Dynamo upgrade
                        typeof(TechTreeColorPatch),
                        typeof(TechTreeBoxInjector),
                        typeof(DynamoProductPatch),
                        // Phase 3: Custom Buildings
                        typeof(BuildingDataGetPatch),
                        typeof(BuildingIconPatch),
                        typeof(CustomBuildingLocPatch),
                    });
                } else {
                    Logger.LogInfo("[Feature] Prestige System: DISABLED");
                }

                // ── Concrete Island Combat ──
                if (ModState.enableCombat) {
                    Logger.LogInfo("[Feature] Concrete Island Combat: ENABLED");
                    patchClasses.AddRange(new[] {
                        typeof(CorpseSetHoverHealthPatch),
                        typeof(CorpseMineDurationPatch),
                        typeof(CorpseAttackPatch),
                        typeof(CorpseHoverHealthPatch),
                        typeof(CorpseSetClickUIHealthPatch),
                        typeof(CorpseUpdateClickUIHealthPatch),
                        typeof(CorpseInitPatch),
                        typeof(ShieldGeneratorTitlePatch),
                        typeof(ShieldGeneratorDescPatch),
                        typeof(RadarTowerInitPatch),
                    });
                } else {
                    Logger.LogInfo("[Feature] Concrete Island Combat: DISABLED");
                }
            } else {
                Logger.LogInfo("[Master] Colony Spire Mod: DISABLED (bugfix-only mode)");
            }

            // ── Vanilla improvements (independent of Colony Spire) ──

            // Divider Save Fix + Counter Gate Ant Leak Fix
            if (ModState.enableDividerFix) {
                Logger.LogInfo("[Improvement] Divider Save Fix: ENABLED");
                patchClasses.AddRange(new[] {
                    typeof(DividerSortFixPatch),
                    typeof(DividerLoadFixPatch),
                    typeof(DividerChooseSafetyPatch),
                    typeof(CounterGateLoadLeakFixPatch),
                });
            } else {
                Logger.LogInfo("[Improvement] Divider Save Fix: DISABLED");
            }
            
            // Game Speed Feature
            if (ModState.enableGameSpeed) {
                Logger.LogInfo("[Improvement] Game Speed: ENABLED");
                patchClasses.Add(typeof(GameSpeedPatch));
            } else {
                Logger.LogInfo("[Improvement] Game Speed: DISABLED");
            }


            // Bridge Save/Load Fix
            if (ModState.enableBridgeFix) {
                Logger.LogInfo("[Improvement] Bridge Save/Load Fix: ENABLED");
                patchClasses.AddRange(new[] {
                    typeof(BridgeInitLoadFixPatch),
                    typeof(BridgeDemolishNullGuardPatch),
                    typeof(BridgeDropPickupNullGuardPatch),
                });
            } else {
                Logger.LogInfo("[Improvement] Bridge Save/Load Fix: DISABLED");
            }

            // Colored Trails
            if (ModState.enableColoredTrails) {
                Logger.LogInfo("[Improvement] Colored Trails: ENABLED");
                patchClasses.AddRange(new[] {
                    typeof(TrailResetMaterialPatch),
                    typeof(TrailColorButtonsPatch),
                    typeof(TrailColorSavePatch),
                    typeof(TrailColorLoadPatch),
                });
            } else {
                Logger.LogInfo("[Improvement] Colored Trails: DISABLED");
            }

            // Stockpile Gates → Battery
            if (ModState.enableBatteryGates) {
                Logger.LogInfo("[Improvement] Stockpile Gate Battery Target: ENABLED");
                patchClasses.AddRange(new[] {
                    typeof(StockpileGateCanAssignPatch),
                    typeof(StockpileGateAssignPatch),
                    typeof(StockpileGateCheckPatch),
                    typeof(StockpileGateAssignLinePatch),
                    typeof(StockpileGateEnumPatch),
                    typeof(StockpileGateHologramPatch),
                    typeof(StockpileGateWritePatch),
                    typeof(StockpileGateReadPatch),
                    typeof(StockpileGateLinksPatch),
                });
            } else {
                Logger.LogInfo("[Improvement] Stockpile Gate Battery Target: DISABLED");
            }

            foreach (var t in patchClasses)
            {
                try { harmony.CreateClassProcessor(t).Patch(); Logger.LogInfo($"[OK] {t.Name}"); }
                catch (Exception ex) { Logger.LogError($"[FAIL] {t.Name}: {ex.Message}"); }
            }
            Logger.LogInfo($"Colony Spire Mod loaded! ({patchClasses.Count} patches)");
        }
    }

    // ================================================================
    // SHARED STATE
    // ================================================================
    public static class ModState
    {
        public static int prestigeLevel = 0;
        public static int pheromoneLevel = 0;
        public static int royalMandateLevel = 0;
        public static int miningLevel = 0;
        public static int carapaceLevel = 0;
        public static int wingLevel = 0;
        public static int sentinelHatched = 0;
        public static int energyLevel = 0;
        public static int gathererLevel = 0;
        public static int displayTier = 1;
        public static float islandScale = 1.0f;
        public static float activeIslandScale = 1.0f;

        // ── Phase 2 Endgame ──
        public static int prestigePoints = 0;
        public static int excavationCores = 0;

        public const int OMNI_ANT_CASTE_ID = 50;
        public const int PRESTIGE_POINTS_PER_OMNI = 10;

        public static int GetPrestigeThreshold() {
            return prestigeLevel switch {
                0 => 20, 1 => 50, 2 => 100, 3 => 200, 4 => 350, 5 => 500,
                _ => 500 + (prestigeLevel - 5) * 200
            };
        }

        public static int GetOmniAntCarry() => 2 + (prestigeLevel / 2);

        public static bool furnaceEnabled => CanT4Endgame;
        public const float FURNACE_ENERGY_PER_ITEM = 5f;
        public const float EXCAVATOR_ENERGY_COST = 50f;
        public const int   EXCAVATOR_CORE_COST   = 5;
        public const float EXCAVATOR_INTERVAL     = 60f;

        public static readonly string[] MineableResources = {
            "BOB_DIRT", "BOB_STONE", "BOB_IRON", "BOB_COPPER",
            "BOB_RESIN", "BOB_SAND", "BOB_CRYSTAL"
        };
        public static ConditionalWeakTable<Queen, QueenData> queenData = new();
        public static QueenData GetQueen(Queen q) => queenData.GetOrCreateValue(q);

        // ── Feature Toggles ──
        public static bool enablePrestige      = true;
        public static bool enableCombat        = true;
        public static bool enableColoredTrails = true;
        public static bool enableBatteryGates  = true;
        public static bool enableDividerFix    = true;
        public static bool enableBridgeFix     = true;
        public static bool enableColonySpire   = true;
        public static bool enableTechTreeColors = true;
        public static bool enableGameSpeed     = true;

        // Sentinel hatching state
        public static float sentinelHatchTimer = -1f;
        public static RadarIslandScanner sentinelSpire = null;
        public const float SENTINEL_HATCH_TIME = 30f;

        // ── Main Bus Trail Color ──
        public static int mainBusColorIndex = 0;
        public static readonly (string name, Color color, Color emission)[] MainBusColors = {
            ("Main Bus",         new Color(1.0f, 1.0f, 1.0f, 1f),    new Color(2.0f, 2.0f, 2.0f, 1f)),
            ("Cyan Bus",         new Color(0.0f, 1.0f, 1.0f, 1f),    new Color(0.0f, 2.0f, 2.0f, 1f)),
            ("Magenta Bus",      new Color(1.0f, 0.0f, 1.0f, 1f),    new Color(2.0f, 0.0f, 2.0f, 1f)),
            ("Lime Bus",         new Color(0.2f, 1.0f, 0.0f, 1f),    new Color(0.4f, 2.0f, 0.0f, 1f)),
            ("Orange Bus",       new Color(1.0f, 0.5f, 0.0f, 1f),    new Color(2.0f, 1.0f, 0.0f, 1f)),
            ("Blue Bus",         new Color(0.0f, 0.5f, 1.0f, 1f),    new Color(0.0f, 1.0f, 2.0f, 1f)),
        };
        public static (string name, Color color, Color emission) GetMainBusColor() =>
            MainBusColors[Math.Max(0, Math.Min(mainBusColorIndex, MainBusColors.Length - 1))];

        public static readonly ConditionalWeakTable<Trail, StrongBox<int>> trailColors = new();
        public static Dictionary<int, int> pendingTrailColors = new();

        public static int GetTrailColorIndex(Trail trail) {
            if (trailColors.TryGetValue(trail, out var box)) return box.Value;
            return -1;
        }

        public static void StampTrailColor(Trail trail, int colorIdx) {
            if (trailColors.TryGetValue(trail, out var box)) {
                box.Value = colorIdx;
            } else {
                trailColors.Add(trail, new StrongBox<int>(colorIdx));
            }
        }

        public static string GetTrailColorPath(string saveName) {
            var saveDir = System.IO.Path.GetDirectoryName(Files.GameSave(saveName, false));
            return System.IO.Path.Combine(saveDir, saveName + ".trailcolors");
        }

        public static string GetIslandScalePath(string saveName) {
            var saveDir = System.IO.Path.GetDirectoryName(Files.GameSave(saveName, false));
            return System.IO.Path.Combine(saveDir, saveName + ".islandscale");
        }

        public static readonly string[] TrackNames = { "Speed", "Queen", "Mine", "Mold", "Wings", "Sentinel", "Energy", "Gather" };
        public static readonly string[] TrackCodes = {
            "SPIRE_SPEED", "SPIRE_QUEEN", "SPIRE_MINE", "SPIRE_MOLD", "SPIRE_WINGS", "SPIRE_SENTINEL", "SPIRE_ENERGY", "SPIRE_GATHER"
        };

        public static (PickupType type, int count)[] GetTrackCost(int track) {
            int lv = GetTrackLevel(track);
            return track switch {
                0 => new[] { ((PickupType)328, lv + 1) },
                1 => new[] { ((PickupType)328, 2), ((PickupType)334, Math.Max(1, lv)) },
                2 => new[] { ((PickupType)328, 2), ((PickupType)211, Math.Max(3, lv * 3)) },
                3 => lv < 2
                    ? new[] { ((PickupType)328, 3 + lv * 2), ((PickupType)250, 5 + lv * 3) }
                    : lv < 4
                        ? new[] { ((PickupType)328, 8 + (lv - 2) * 4), ((PickupType)306, 3 + (lv - 2) * 2) }
                        : new[] { ((PickupType)328, 20), ((PickupType)306, 10), ((PickupType)329, 5) },
                4 => new[] { ((PickupType)328, 3), ((PickupType)336, Math.Max(2, lv * 2)) },
                5 => new[] { ((PickupType)328, 10), ((PickupType)334, 3), ((PickupType)327, 1) },
                6 => new[] { ((PickupType)328, 2), ((PickupType)318, Math.Max(1, lv)) },
                7 => new[] { ((PickupType)328, 2), ((PickupType)250, Math.Max(2, lv * 2)) },
                _ => new[] { ((PickupType)328, 2) }
            };
        }

        static readonly Dictionary<int, int> _spireTrack = new();
        public static int GetSpireTrack(Unlocker u) {
            _spireTrack.TryGetValue(u.GetInstanceID(), out int t);
            return t;
        }
        public static void SetSpireTrack(Unlocker u, int idx) {
            idx = Math.Max(0, Math.Min(TrackNames.Length - 1, idx));
            _spireTrack[u.GetInstanceID()] = idx;
            ModSave.SaveSpireTrack(idx);
            Debug.Log($"[Spire] Track -> {idx}: {TrackNames[idx]}");
        }
        public static int TrackIndexFromCode(string code) {
            for (int i = 0; i < TrackCodes.Length; i++) if (TrackCodes[i] == code) return i;
            return -1;
        }
        public static string GetTrackName(int t) => t >= 0 && t < TrackNames.Length ? TrackNames[t] : "???";
        public static int GetTrackLevel(int t) => t switch {
            0 => pheromoneLevel, 1 => royalMandateLevel, 2 => miningLevel,
            3 => carapaceLevel, 4 => wingLevel,
            5 => sentinelHatched, 6 => energyLevel,
            7 => gathererLevel,
            _ => 0
        };
        public static void UpgradeTrack(int t) {
            switch (t) {
                case 0: pheromoneLevel++; break;
                case 1: royalMandateLevel++; break;
                case 2: miningLevel++; break;
                case 3: if (carapaceLevel < 5) carapaceLevel++; break;
                case 4: if (wingLevel < 5) wingLevel++; break;
                case 5:
                    if (sentinelHatchTimer < 0f) {
                        sentinelHatchTimer = SENTINEL_HATCH_TIME;
                        Debug.Log($"[Spire] Sentinel hatching started! {SENTINEL_HATCH_TIME}s remaining");
                    } else {
                        Debug.Log("[Spire] Sentinel already hatching — wait for it to finish!");
                    }
                    break;
                case 6: energyLevel++; break;
                case 7: if (gathererLevel < 5) gathererLevel++; break;
            }
            if (t != 5) Debug.Log($"[Spire] Upgraded {GetTrackName(t)} -> Lv{GetTrackLevel(t)}");
        }

        // ── Tech Tree Gating ──
        public static bool ModTechResearched(string techCode) {
            try {
                var tech = Tech.Get(techCode, "");
                if (tech == null) return true;
                return tech.GetStatus() == TechStatus.DONE;
            } catch {
                return true;
            }
        }

        public static bool CanQueenT2 => ModTechResearched("MOD_QUEEN_T2");
        public static bool CanQueenT3 => ModTechResearched("MOD_QUEEN_T3");
        public static bool CanColoredTrails => ModTechResearched("MOD_COLORED_TRAILS");
        public static bool CanT4Endgame => ModTechResearched("MOD_T4_ENDGAME");

        public static int MaxQueenTier {
            get {
                if (CanQueenT3) return 3;
                if (CanQueenT2) return 2;
                return 1;
            }
        }
    }
    public class QueenData {
        public int larvaOutputTier = 1;
        public bool initialized = false;
    }
    public static class SpireHelper { public static bool IsSpire(Building b) => b?.data?.code == "COLONY_SPIRE"; }

    // ================================================================
    // SAVE / LOAD
    // ================================================================
    public static class ModSave
    {
        const string KEY_PRESTIGE   = "CSP_Prestige";
        const string KEY_SPEED      = "CSP_Speed";
        const string KEY_QUEEN      = "CSP_Queen";
        const string KEY_MINE       = "CSP_Mine";
        const string KEY_MOLD       = "CSP_Mold";
        const string KEY_WINGS      = "CSP_Wings";
        const string KEY_SENTINEL   = "CSP_Sentinel";
        const string KEY_QUEENTIER  = "CSP_QueenTier";
        const string KEY_ENERGY     = "CSP_Energy";
        const string KEY_GATHER    = "CSP_Gather";
        const string KEY_SPIRE_TRACK = "CSP_SpireTrack";
        const string KEY_ISLAND_SCALE = "CSP_IslandScale";
        const string KEY_MAINBUS_COLOR = "CSP_MainBusColor";
        const string KEY_PRESTIGE_PTS  = "CSP_PrestigePoints";
        const string KEY_EXCAV_CORES   = "CSP_ExcavCores";
        const string KEY_FEAT_PRESTIGE = "CSP_FeatPrestige";
        const string KEY_FEAT_COMBAT   = "CSP_FeatCombat";
        const string KEY_FEAT_TRAILS   = "CSP_FeatTrails";
        const string KEY_FEAT_BATTERY  = "CSP_FeatBattery";
        const string KEY_FEAT_DIVIDER  = "CSP_FeatDividerFix";
        const string KEY_FEAT_BRIDGE   = "CSP_FeatBridgeFix";
        const string KEY_FEAT_TECHCOLORS = "CSP_FeatTechColors";
        const string KEY_FEAT_GAMESPEED = "CSP_FeatGameSpeed";
        const string KEY_FEAT_MASTER   = "CSP_FeatMaster";

        public static void SaveSettings() {
            PlayerPrefs.SetFloat(KEY_ISLAND_SCALE, ModState.islandScale);
            PlayerPrefs.SetInt(KEY_MAINBUS_COLOR, ModState.mainBusColorIndex);
            PlayerPrefs.SetInt(KEY_FEAT_PRESTIGE, ModState.enablePrestige ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_COMBAT,   ModState.enableCombat ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_TRAILS,   ModState.enableColoredTrails ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_BATTERY,  ModState.enableBatteryGates ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_DIVIDER,  ModState.enableDividerFix ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_BRIDGE,   ModState.enableBridgeFix ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_TECHCOLORS, ModState.enableTechTreeColors ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_GAMESPEED, ModState.enableGameSpeed ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_MASTER,   ModState.enableColonySpire ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SaveMainBusColor() {
            PlayerPrefs.SetInt(KEY_MAINBUS_COLOR, ModState.mainBusColorIndex);
            PlayerPrefs.Save();
        }

        public static void SaveSpireTrack(int trackIdx) {
            PlayerPrefs.SetInt(KEY_SPIRE_TRACK, trackIdx);
            PlayerPrefs.Save();
        }
        public static int LoadSpireTrack() => PlayerPrefs.GetInt(KEY_SPIRE_TRACK, 0);

        public static void Save(int queenTier = -1) {
            PlayerPrefs.SetInt(KEY_PRESTIGE, ModState.prestigeLevel);
            PlayerPrefs.SetInt(KEY_SPEED,    ModState.pheromoneLevel);
            PlayerPrefs.SetInt(KEY_QUEEN,    ModState.royalMandateLevel);
            PlayerPrefs.SetInt(KEY_MINE,     ModState.miningLevel);
            PlayerPrefs.SetInt(KEY_MOLD,     ModState.carapaceLevel);
            PlayerPrefs.SetInt(KEY_WINGS,    ModState.wingLevel);
            PlayerPrefs.SetInt(KEY_SENTINEL, ModState.sentinelHatched);
            PlayerPrefs.SetInt(KEY_ENERGY,   ModState.energyLevel);
            PlayerPrefs.SetInt(KEY_GATHER,   ModState.gathererLevel);
            PlayerPrefs.SetInt(KEY_PRESTIGE_PTS, ModState.prestigePoints);
            PlayerPrefs.SetInt(KEY_EXCAV_CORES,  ModState.excavationCores);
            if (queenTier >= 1) PlayerPrefs.SetInt(KEY_QUEENTIER, queenTier);
            PlayerPrefs.Save();
            Debug.Log($"[Spire] Saved — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Q{ModState.royalMandateLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel} G{ModState.gathererLevel} PP={ModState.prestigePoints} Cores={ModState.excavationCores}");
        }

        public static void Load() {
            ModState.prestigeLevel     = PlayerPrefs.GetInt(KEY_PRESTIGE, 0);
            ModState.pheromoneLevel    = PlayerPrefs.GetInt(KEY_SPEED,    0);
            ModState.royalMandateLevel = PlayerPrefs.GetInt(KEY_QUEEN,    0);
            ModState.miningLevel       = PlayerPrefs.GetInt(KEY_MINE,     0);
            ModState.carapaceLevel     = PlayerPrefs.GetInt(KEY_MOLD,     0);
            ModState.wingLevel         = PlayerPrefs.GetInt(KEY_WINGS,    0);
            ModState.sentinelHatched   = PlayerPrefs.GetInt(KEY_SENTINEL, 0);
            ModState.energyLevel       = PlayerPrefs.GetInt(KEY_ENERGY,   0);
            ModState.gathererLevel     = PlayerPrefs.GetInt(KEY_GATHER,   0);
            ModState.prestigePoints    = PlayerPrefs.GetInt(KEY_PRESTIGE_PTS, 0);
            ModState.excavationCores   = PlayerPrefs.GetInt(KEY_EXCAV_CORES,  0);
            ModState.islandScale       = PlayerPrefs.GetFloat(KEY_ISLAND_SCALE, 1.0f);
            ModState.mainBusColorIndex = PlayerPrefs.GetInt(KEY_MAINBUS_COLOR, 0);
            ModState.enablePrestige      = PlayerPrefs.GetInt(KEY_FEAT_PRESTIGE, 1) != 0;
            ModState.enableCombat        = PlayerPrefs.GetInt(KEY_FEAT_COMBAT,   1) != 0;
            ModState.enableColoredTrails = PlayerPrefs.GetInt(KEY_FEAT_TRAILS,   1) != 0;
            ModState.enableBatteryGates  = PlayerPrefs.GetInt(KEY_FEAT_BATTERY,  1) != 0;
            ModState.enableDividerFix    = PlayerPrefs.GetInt(KEY_FEAT_DIVIDER,  1) != 0;
            ModState.enableBridgeFix     = PlayerPrefs.GetInt(KEY_FEAT_BRIDGE,   1) != 0;
            ModState.enableTechTreeColors= PlayerPrefs.GetInt(KEY_FEAT_TECHCOLORS, 1) != 0;
            ModState.enableGameSpeed     = PlayerPrefs.GetInt(KEY_FEAT_GAMESPEED, 1) != 0;
            ModState.enableColonySpire   = PlayerPrefs.GetInt(KEY_FEAT_MASTER,   1) != 0;
            Debug.Log($"[Spire] Loaded — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel} G{ModState.gathererLevel} PP={ModState.prestigePoints} Cores={ModState.excavationCores} Scale={ModState.islandScale}");
            Debug.Log($"[Spire] Features: Prestige={ModState.enablePrestige} Combat={ModState.enableCombat} Trails={ModState.enableColoredTrails} Battery={ModState.enableBatteryGates}");
        }

        public static int LoadQueenTier() => PlayerPrefs.GetInt(KEY_QUEENTIER, 1);
    }

    // ================================================================
    // SETTINGS MENU INJECTION
    // ================================================================
    [HarmonyPatch(typeof(UISettings), "AddWorldSettings")]
    public static class UISettingsWorldPatch {
        [HarmonyPostfix]
        static void Postfix(UISettings __instance) {
            try {
                var addSettingMethod = AccessTools.Method(typeof(UISettings), "AddSetting");
                if (addSettingMethod == null) return;

                // ── Section: Vanilla Fixes & Improvements (always visible) ──
                var blankFixes = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                if (blankFixes != null) blankFixes.InitEmpty();

                SetupFeatureToggle(__instance, addSettingMethod, "Divider Save Fix",
                    "Fix vanilla bug where divider active trail corrupts on save/load",
                    () => ModState.enableDividerFix,
                    v => { ModState.enableDividerFix = v; ModSave.SaveSettings(); });

                SetupFeatureToggle(__instance, addSettingMethod, "Bridge Save/Load Fix",
                    "Fix vanilla bug where bridges corrupt on save/load (position, deletion, materials)",
                    () => ModState.enableBridgeFix,
                    v => { ModState.enableBridgeFix = v; ModSave.SaveSettings(); });

                SetupFeatureToggle(__instance, addSettingMethod, "Colored Trails",
                    "Color variants for Main Bus trails",
                    () => ModState.enableColoredTrails,
                    v => { ModState.enableColoredTrails = v; ModSave.SaveSettings(); });

                SetupFeatureToggle(__instance, addSettingMethod, "Stockpile Gate Battery Target",
                    "Stockpile gates can target battery",
                    () => ModState.enableBatteryGates,
                    v => { ModState.enableBatteryGates = v; ModSave.SaveSettings(); });

                SetupFeatureToggle(__instance, addSettingMethod, "Tech Tree Colors",
                    "Color variants for tech tree halos",
                    () => ModState.enableTechTreeColors,
                    v => { ModState.enableTechTreeColors = v; ModSave.SaveSettings(); });

                SetupFeatureToggle(__instance, addSettingMethod, "Game Speed Buttons",
                    "Add 1x, 2x, 4x buttons to the game HUD",
                    () => ModState.enableGameSpeed,
                    v => { ModState.enableGameSpeed = v; ModSave.SaveSettings(); });

                // ── Section: Colony Spire Mod (master + sub-toggles) ──
                var blankMaster = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                if (blankMaster != null) blankMaster.InitEmpty();

                SetupFeatureToggle(__instance, addSettingMethod, "★ Colony Spire Mod",
                    "Master toggle for all Colony Spire content (requires restart)",
                    () => ModState.enableColonySpire,
                    v => { ModState.enableColonySpire = v; ModSave.SaveSettings(); });

                if (ModState.enableColonySpire) {
                    var blankSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                    var sliderSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                    SetupSlider(blankSetting, sliderSetting);

                    SetupFeatureToggle(__instance, addSettingMethod, "  Prestige System",
                        "Queen tiers, Colony Spire tracks, speed/mining/mold/wing/gatherer upgrades",
                        () => ModState.enablePrestige,
                        v => { ModState.enablePrestige = v; ModSave.SaveSettings(); });

                    SetupFeatureToggle(__instance, addSettingMethod, "  Concrete Island Combat",
                        "Corpse health bars, attack mechanics, shield generators",
                        () => ModState.enableCombat,
                        v => { ModState.enableCombat = v; ModSave.SaveSettings(); });
                }
            } catch (Exception ex) {
                Debug.Log($"[Spire] UISettings exception: {ex.Message}");
            }
        }

        public static void SetupSlider(UISettings_Setting blankSetting, UISettings_Setting sliderSetting) {
            if (blankSetting != null) blankSetting.InitEmpty();
            if (sliderSetting != null) {
                sliderSetting.InitSlider("Initial Island Scale", 0.5f, 1f, 
                    () => ModState.islandScale, 
                    (float v) => {
                        float snapped = Mathf.Round(v * 20f) / 20f;
                        ModState.islandScale = snapped;
                        ModSave.SaveSettings();
                        var valueAfterField = AccessTools.Field(typeof(UISettings_Setting), "valueAfter");
                        if (valueAfterField != null) {
                            var valueAfter = valueAfterField.GetValue(sliderSetting) as UITextImageButton;
                            if (valueAfter != null) {
                                valueAfter.SetText($"{Mathf.Round(snapped * 100f)}%");
                            }
                        }
                    }
                );
                var headerField = AccessTools.Field(typeof(UISettings_Setting), "headerText");
                if (headerField != null) {
                    var textObj = headerField.GetValue(sliderSetting) as TMPro.TextMeshProUGUI;
                    if (textObj != null) textObj.text = "Initial Island Scale";
                }
            }
        }

        public static void SetupFeatureToggle(object settingsInstance, MethodInfo addSettingMethod,
            string title, string description, Func<bool> getter, Action<bool> setter) {
            try {
                var setting = (UISettings_Setting)addSettingMethod.Invoke(settingsInstance, new object[0]);
                if (setting == null) return;
                setting.InitToggle(title, getter, (bool val) => {
                    setter(val);
                    Debug.Log($"[Spire] Feature '{title}' = {val} (requires restart for full effect)");
                });
                var headerField = AccessTools.Field(typeof(UISettings_Setting), "headerText");
                if (headerField != null) {
                    var textObj = headerField.GetValue(setting) as TMPro.TextMeshProUGUI;
                    if (textObj != null) textObj.text = title;
                }
            } catch (Exception ex) { Debug.Log($"[Spire] Toggle '{title}': {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(UIWorldSettings), "Init")]
    public static class UIWorldSettingsInitPatch {
        [HarmonyPostfix]
        static void Postfix(UIWorldSettings __instance) {
            try {
                var addSettingMethod = AccessTools.Method(typeof(UIWorldSettings), "AddSetting");
                if (addSettingMethod == null) return;
                
                var blankSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                var sliderSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                
                UISettingsWorldPatch.SetupSlider(blankSetting, sliderSetting);
            } catch (Exception ex) {
                Debug.Log($"[Spire] UIWorldSettings exception: {ex.Message}");
            }
        }
    }
}
