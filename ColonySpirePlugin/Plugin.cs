using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColonySpireMod
{
    [BepInPlugin("com.colonyspire.mod", "Colony Spire Mod", "1.1.0")]
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

                // Core mod infrastructure: Island Scale, Save persistence, Localization
                patchClasses.AddRange(new[] {
                    typeof(GroundInitShapePatch),
                    typeof(GroundCreatePatch),
                    typeof(ModLocPatch),
                    typeof(IslandScaleSavePatch),
                    typeof(IslandScaleLoadPatch),
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
                        // Phase 3: Custom Buildings
                        typeof(BuildingDataGetPatch),
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

            // Divider Save Fix
            if (ModState.enableDividerFix) {
                Logger.LogInfo("[Improvement] Divider Save Fix: ENABLED");
                patchClasses.AddRange(new[] {
                    typeof(DividerSortFixPatch),
                    typeof(DividerSaveFixPatch),
                    typeof(DividerLoadFixPatch),
                });
            } else {
                Logger.LogInfo("[Improvement] Divider Save Fix: DISABLED");
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
        public static int sentinelHatched = 0;  // Track 6: total sentinels ever hatched
        public static int energyLevel = 0;      // Track 7: Energy efficiency
        public static int gathererLevel = 0;    // Track 8: Gatherer speed (max 5)
        public static int displayTier = 1;
        public static float islandScale = 1.0f; // Scale modifier for the first island (settings UI)
        public static float activeIslandScale = 1.0f; // Scale modifier currently in use

        // ── Phase 2 Endgame ──
        public static int prestigePoints = 0;      // Accumulated prestige points toward next Super Gyne launch
        public static int excavationCores = 0;     // Cores from combat kills, fuel for the Deep Excavator

        // T4 Omni-Ant caste ID — we repurpose an unused AntCaste value
        // The queen produces T3 larvae (caste 402); the High-End Assembler will
        // eventually convert them, but for now we detect T4 via a flag on the ant.
        // For initial implementation, we detect T3 ants that have been "promoted"
        // by checking a tag.  Long-term this becomes a real caste in prefabs.fods.
        public const int OMNI_ANT_CASTE_ID = 10;  // placeholder for T4 caste enum value

        // Points per elder Omni-Ant sacrificed
        public const int PRESTIGE_POINTS_PER_OMNI = 10;

        /// <summary>Returns the prestige points threshold for the current prestige level.</summary>
        public static int GetPrestigeThreshold() {
            return prestigeLevel switch {
                0 => 20,    // Level 0→1
                1 => 50,    // Level 1→2
                2 => 100,   // Level 2→3
                3 => 200,   // Level 3→4
                4 => 350,   // Level 4→5
                5 => 500,   // Level 5→6
                _ => 500 + (prestigeLevel - 5) * 200  // +200 each after 6
            };
        }

        /// <summary>Returns the T4 Omni-Ant carry capacity (base 2, +1 per 2 prestige levels).</summary>
        public static int GetOmniAntCarry() => 2 + (prestigeLevel / 2);

        // Island Furnace: once enabled, all BatteryBuildings accept any material
        public static bool furnaceEnabled => prestigeLevel >= 1;
        public const float FURNACE_ENERGY_PER_ITEM = 5f;

        // Deep Excavator: spawns resource deposits using energy + cores
        public const float EXCAVATOR_ENERGY_COST = 50f;
        public const int   EXCAVATOR_CORE_COST   = 5;
        public const float EXCAVATOR_INTERVAL     = 60f; // seconds between excavation attempts

        // Known mineable resource codes from the game
        public static readonly string[] MineableResources = {
            "BOB_DIRT", "BOB_STONE", "BOB_IRON", "BOB_COPPER",
            "BOB_RESIN", "BOB_SAND", "BOB_CRYSTAL"
        };
        public static ConditionalWeakTable<Queen, QueenData> queenData = new();
        public static QueenData GetQueen(Queen q) => queenData.GetOrCreateValue(q);

        // ----------------------------------------------------------------
        // FEATURE TOGGLES — opt-in/out from settings (all default ON)
        // ----------------------------------------------------------------
        public static bool enablePrestige      = true;   // Prestige system (queen tiers, spire tracks, speed, etc.)
        public static bool enableCombat        = true;   // Concrete island combat (corpse health, shield generators, etc.)
        public static bool enableColoredTrails = true;   // Colored Main Bus trails
        public static bool enableBatteryGates  = true;   // Stockpile gates can target batteries
        public static bool enableDividerFix    = true;   // Fix vanilla divider save/load bug
        public static bool enableColonySpire   = true;   // Master toggle for all Colony Spire mod content

        // Sentinel hatching state
        public static float sentinelHatchTimer = -1f;  // -1 = idle; >0 = hatching
        public static RadarIslandScanner sentinelSpire = null;  // which spire is hatching
        public const float SENTINEL_HATCH_TIME = 30f;

        // ----------------------------------------------------------------
        // MAIN BUS TRAIL COLOR — customizable color for TrailType.MAIN
        // Index 0 = vanilla (white), 1-5 = bright color presets
        // ----------------------------------------------------------------
        public static int mainBusColorIndex = 0;
        public static readonly (string name, Color color, Color emission)[] MainBusColors = {
            ("Main Bus",         new Color(1.0f, 1.0f, 1.0f, 1f),    new Color(2.0f, 2.0f, 2.0f, 1f)),     // 0: vanilla white
            ("Cyan Bus",         new Color(0.0f, 1.0f, 1.0f, 1f),    new Color(0.0f, 2.0f, 2.0f, 1f)),     // 1
            ("Magenta Bus",      new Color(1.0f, 0.0f, 1.0f, 1f),    new Color(2.0f, 0.0f, 2.0f, 1f)),     // 2
            ("Lime Bus",         new Color(0.2f, 1.0f, 0.0f, 1f),    new Color(0.4f, 2.0f, 0.0f, 1f)),     // 3
            ("Orange Bus",       new Color(1.0f, 0.5f, 0.0f, 1f),    new Color(2.0f, 1.0f, 0.0f, 1f)),     // 4
            ("Blue Bus",         new Color(0.0f, 0.5f, 1.0f, 1f),    new Color(0.0f, 1.0f, 2.0f, 1f)),     // 5
        };
        public static (string name, Color color, Color emission) GetMainBusColor() =>
            MainBusColors[Math.Max(0, Math.Min(mainBusColorIndex, MainBusColors.Length - 1))];

        // Per-trail color storage: each Trail instance remembers the color it was drawn with.
        // Uses ConditionalWeakTable so colors are automatically cleaned up when trails are GC'd.
        public static readonly ConditionalWeakTable<Trail, StrongBox<int>> trailColors = new();

        // Pending colors loaded from sidecar file, keyed by linkId.
        // When a trail is first seen by ResetMaterial, we check this map.
        public static Dictionary<int, int> pendingTrailColors = new();

        // Get the color index for a specific trail. Returns -1 if not stamped (use vanilla).
        public static int GetTrailColorIndex(Trail trail) {
            if (trailColors.TryGetValue(trail, out var box)) return box.Value;
            return -1; // not stamped → vanilla color
        }

        // Stamp a trail with the current color. Called when ResetMaterial fires on a new trail.
        public static void StampTrailColor(Trail trail, int colorIdx) {
            if (trailColors.TryGetValue(trail, out var box)) {
                box.Value = colorIdx;
            } else {
                trailColors.Add(trail, new StrongBox<int>(colorIdx));
            }
        }

        // Get the sidecar file path for a given save name
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

        // ----------------------------------------------------------------
        // COST TABLE — per track, per level (level = current track level BEFORE upgrade)
        // Returns the list of (PickupType, count) required to advance from level N.
        // Track 5 (Sentinel) is a flat one-shot cost regardless of level.
        // ----------------------------------------------------------------
        // PickupType constants: ROYAL_JELLY=328, MICROCHIP=334, IRON_BAR=211,
        //   RESIN=250, RUBBER=306, FABRIC=336, WAFER=327, BIOFUEL=329
        public static (PickupType type, int count)[] GetTrackCost(int track) {
            int lv = GetTrackLevel(track);
            return track switch {
                // Track 0: Speed | RJ * (N+1)
                0 => new[] { ((PickupType)328, lv + 1) },
                // Track 1: Queen | RJ*2 + Microchip*(N)
                1 => new[] { ((PickupType)328, 2), ((PickupType)334, Math.Max(1, lv)) },
                // Track 2: Mining | RJ*2 + IronBar*(N*3)
                2 => new[] { ((PickupType)328, 2), ((PickupType)211, Math.Max(3, lv * 3)) },
                // Track 3: Mold | levels 1-2 use Resin, 3-5 use Rubber (+Biofuel at lv5)
                3 => lv < 2
                    ? new[] { ((PickupType)328, 3 + lv * 2), ((PickupType)250, 5 + lv * 3) }
                    : lv < 4
                        ? new[] { ((PickupType)328, 8 + (lv - 2) * 4), ((PickupType)306, 3 + (lv - 2) * 2) }
                        : new[] { ((PickupType)328, 20), ((PickupType)306, 10), ((PickupType)329, 5) },
                // Track 4: Wings | RJ*3 + Fabric*(N*2)
                4 => new[] { ((PickupType)328, 3), ((PickupType)336, Math.Max(2, lv * 2)) },
                // Track 5: Sentinel one-shot | RJ*10 + Microchip*3 + Wafer*1
                5 => new[] { ((PickupType)328, 10), ((PickupType)334, 3), ((PickupType)327, 1) },
                // Track 6: Energy | RJ*2 + Battery(POD3)=318 *(N) 
                6 => new[] { ((PickupType)328, 2), ((PickupType)318, Math.Max(1, lv)) },
                // Track 7: Gather Speed | RJ*2 + Resin*(N*2)  (max 5 levels)
                7 => new[] { ((PickupType)328, 2), ((PickupType)250, Math.Max(2, lv * 2)) },
                _ => new[] { ((PickupType)328, 2) }
            };
        }

        // Per-spire selected track index (keyed by instance ID)
        static readonly Dictionary<int, int> _spireTrack = new();
        public static int GetSpireTrack(Unlocker u) {
            _spireTrack.TryGetValue(u.GetInstanceID(), out int t);
            return t; // 0..6
        }
        public static void SetSpireTrack(Unlocker u, int idx) {
            idx = Math.Max(0, Math.Min(TrackNames.Length - 1, idx));
            _spireTrack[u.GetInstanceID()] = idx;
            ModSave.SaveSpireTrack(idx);  // persist immediately so it survives reload
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
                    // Sentinel track: consume resources (already done in DoUnlock) and start timer
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

        // ----------------------------------------------------------------
        // TECH TREE GATING — check if our custom research nodes are done
        // These tech codes are injected into techtree.fods by patch_techtree.ps1
        //
        // IMPORTANT: The tech tree UI uses Unity prefab boxes — we can't inject
        // new visual nodes.  Our MOD_* techs have no clickable UI box, so they
        // will never be purchased by the player.  Instead we auto-unlock them
        // the moment their prerequisite techs are researched (status == OPEN).
        // The research costs on the nodes serve as documentation only; the real
        // gate is the prerequisite tech chain.
        // ----------------------------------------------------------------
        public static bool ModTechResearched(string techCode) {
            try {
                var tech = Tech.Get(techCode, "");
                if (tech == null) return true; // fail-open if tech not found

                var status = tech.GetStatus();
                if (status == TechStatus.DONE) return true;

                // Auto-unlock: if prerequisites are met (OPEN), unlock it now
                if (status == TechStatus.OPEN) {
                    tech.Unlock(during_load: false);
                    Debug.Log($"[Spire] Auto-unlocked mod tech: {techCode}");
                    return true;
                }

                return false; // prerequisites not yet met
            } catch {
                return true; // fail-open on any error
            }
        }

        public static bool CanQueenT2 => ModTechResearched("MOD_QUEEN_T2");
        public static bool CanQueenT3 => ModTechResearched("MOD_QUEEN_T3");
        public static bool CanColoredTrails => ModTechResearched("MOD_COLORED_TRAILS");

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
        public bool initialized = false;  // flag: have we loaded from PlayerPrefs yet?
    }
    public static class SpireHelper { public static bool IsSpire(Building b) => b?.data?.code == "COLONY_SPIRE"; }

    // ================================================================
    // SAVE / LOAD — PlayerPrefs (persists across sessions, all save slots)
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
        const string KEY_SPIRE_TRACK = "CSP_SpireTrack";  // selected upgrade track
        const string KEY_ISLAND_SCALE = "CSP_IslandScale";
        const string KEY_MAINBUS_COLOR = "CSP_MainBusColor";
        // Phase 2 endgame keys
        const string KEY_PRESTIGE_PTS  = "CSP_PrestigePoints";
        const string KEY_EXCAV_CORES   = "CSP_ExcavCores";
        // Feature toggle keys
        const string KEY_FEAT_PRESTIGE = "CSP_FeatPrestige";
        const string KEY_FEAT_COMBAT   = "CSP_FeatCombat";
        const string KEY_FEAT_TRAILS   = "CSP_FeatTrails";
        const string KEY_FEAT_BATTERY  = "CSP_FeatBattery";
        const string KEY_FEAT_DIVIDER  = "CSP_FeatDividerFix";
        const string KEY_FEAT_MASTER   = "CSP_FeatMaster";

        public static void SaveSettings() {
            PlayerPrefs.SetFloat(KEY_ISLAND_SCALE, ModState.islandScale);
            PlayerPrefs.SetInt(KEY_MAINBUS_COLOR, ModState.mainBusColorIndex);
            PlayerPrefs.SetInt(KEY_FEAT_PRESTIGE, ModState.enablePrestige ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_COMBAT,   ModState.enableCombat ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_TRAILS,   ModState.enableColoredTrails ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_BATTERY,  ModState.enableBatteryGates ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FEAT_DIVIDER,  ModState.enableDividerFix ? 1 : 0);
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
            // Phase 2 endgame
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
            // Phase 2 endgame
            ModState.prestigePoints    = PlayerPrefs.GetInt(KEY_PRESTIGE_PTS, 0);
            ModState.excavationCores   = PlayerPrefs.GetInt(KEY_EXCAV_CORES,  0);
            ModState.islandScale       = PlayerPrefs.GetFloat(KEY_ISLAND_SCALE, 1.0f);
            ModState.mainBusColorIndex = PlayerPrefs.GetInt(KEY_MAINBUS_COLOR, 0);
            // Feature toggles (default = 1 = enabled)
            ModState.enablePrestige      = PlayerPrefs.GetInt(KEY_FEAT_PRESTIGE, 1) != 0;
            ModState.enableCombat        = PlayerPrefs.GetInt(KEY_FEAT_COMBAT,   1) != 0;
            ModState.enableColoredTrails = PlayerPrefs.GetInt(KEY_FEAT_TRAILS,   1) != 0;
            ModState.enableBatteryGates  = PlayerPrefs.GetInt(KEY_FEAT_BATTERY,  1) != 0;
            ModState.enableDividerFix    = PlayerPrefs.GetInt(KEY_FEAT_DIVIDER,  1) != 0;
            ModState.enableColonySpire   = PlayerPrefs.GetInt(KEY_FEAT_MASTER,   1) != 0;
            Debug.Log($"[Spire] Loaded — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel} G{ModState.gathererLevel} PP={ModState.prestigePoints} Cores={ModState.excavationCores} Scale={ModState.islandScale}");
            Debug.Log($"[Spire] Features: Prestige={ModState.enablePrestige} Combat={ModState.enableCombat} Trails={ModState.enableColoredTrails} Battery={ModState.enableBatteryGates}");
        }

        public static int LoadQueenTier() => PlayerPrefs.GetInt(KEY_QUEENTIER, 1);
    }

    // ================================================================
    // LOCALIZATION — inject English strings for our custom tech nodes
    // so they don't show as "?TECHTREE_MOD_QUEEN_T2?"
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
        };

        [HarmonyPrefix]
        static bool Prefix(string code, ref string __result) {
            if (modStrings.TryGetValue(code, out var text)) {
                __result = text;
                return false; // skip original Loc lookup
            }
            return true; // let vanilla handle it
        }
    }

    // ================================================================
    // PRESTIGE + SPEED
    // ================================================================
    // Prestige is now handled by the Gyne Tower accumulator (GyneTowerPrestigePatch).
    // StartGyne still fires naturally when the nuptial flight is triggered there.
    // We keep this patch but make it a no-op so vanilla prestige doesn't double-count.
    [HarmonyPatch(typeof(GyneTower), "StartGyne")]
    public static class PrestigePatch {
        [HarmonyPostfix] static void Postfix() {
            // Phase 2: prestige level is incremented in GyneTowerPrestigePatch instead.
            // This postfix intentionally left empty to prevent double-counting.
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
        public static int activeTier = 1; // set during each queen's update so SpawnPickupPatch can read it

        [HarmonyPrefix] static void Prefix(Queen __instance) {
            var qd = ModState.GetQueen(__instance);
            // Clamp saved tier to max allowed (in case research was un-done or save is from older version)
            int maxTier = ModState.MaxQueenTier;
            if (qd.larvaOutputTier > maxTier) qd.larvaOutputTier = maxTier;
            activeTier = qd.larvaOutputTier; // expose to SpawnPickupPatch
            ModState.displayTier = qd.larvaOutputTier;
            if (Input.GetKeyDown(KeyCode.G)) {
                // Cycle within allowed tiers: 1→2→...→maxTier→1
                qd.larvaOutputTier = (qd.larvaOutputTier % maxTier) + 1;
                activeTier = qd.larvaOutputTier;
            }
        }
        [HarmonyPostfix] static void Postfix() { activeTier = 1; } // reset so other SpawnPickup calls unaffected
    }

    // GameManager.SpawnPickup(PickupType, Vector3, Quaternion, Save) — the overload Queen uses
    // Remaps LARVAE_T1 (400) to LARVAE_T2 (401) or LARVAE_T3 (402) during queen's BuildingUpdate
    [HarmonyPatch(typeof(GameManager), "SpawnPickup",
        new[] { typeof(PickupType), typeof(Vector3), typeof(Quaternion), typeof(Save) })]
    public static class SpawnPickupPatch {
        [HarmonyPrefix] static void Prefix(ref PickupType _type) {
            if ((int)_type == 400 && QueenBuildingUpdatePatch.activeTier > 1)
                _type = (PickupType)(399 + QueenBuildingUpdatePatch.activeTier); // 401=T2, 402=T3
        }
    }
    [HarmonyPatch(typeof(Queen), "SetClickUi")]
    public static class QueenSetClickUiPatch {
        static readonly string[] Labels = { "T1 Worker", "T2 Soldier", "T3 Royal" };
        [HarmonyPostfix] static void Postfix(Queen __instance, UIClickLayout __0) {
            var qd = ModState.GetQueen(__instance);
            int maxTier = ModState.MaxQueenTier;
            // Clamp tier to what's unlocked
            if (qd.larvaOutputTier > maxTier) qd.larvaOutputTier = maxTier;
            string tierInfo = maxTier > 1 ? $" [T{qd.larvaOutputTier}]" : "";
            string prestigeInfo = $" ★{ModState.prestigeLevel} [{ModState.prestigePoints}/{ModState.GetPrestigeThreshold()}] 🛡{ModState.excavationCores}";
            __0.SetTitle(__instance.data.GetTitle() + tierInfo + prestigeInfo);
            try {
                var btn = __0.GetButton((UIClickButtonType)50, false); // Generic1

                if (btn == null) {
                    // Generic1 button doesn't exist in this layout — create one dynamically
                    btn = CreateQueenTierButton(__0);
                }

                if (btn != null) {
                    if (maxTier <= 1) {
                        // Only T1 unlocked — hide the button entirely
                        __0.UpdateButton((UIClickButtonType)50, false, "", false);
                    } else {
                        // Wire click: cycle T1→T2→...→maxTier→T1
                        btn.SetButton(() => {
                            int mt = ModState.MaxQueenTier;
                            qd.larvaOutputTier = (qd.larvaOutputTier % mt) + 1;
                            ModSave.Save(qd.larvaOutputTier);
                            string ti = $" [T{qd.larvaOutputTier}]";
                            __0.SetTitle(__instance.data.GetTitle() + ti + $" ★{ModState.prestigeLevel}");
                            __0.UpdateButton((UIClickButtonType)50, true, Labels[qd.larvaOutputTier - 1], true);
                        }, (InputAction)0);
                        // Show with current tier label
                        __0.UpdateButton((UIClickButtonType)50, true, Labels[qd.larvaOutputTier - 1], true);
                    }
                    Debug.Log($"[Spire] Queen tier button active: T{qd.larvaOutputTier}");
                } else {
                    Debug.LogWarning("[Spire] Queen tier button: could not create or find button");
                }
            } catch (Exception ex) { Debug.Log($"[Spire] Queen btn: {ex.Message}"); }
        }

        /// <summary>
        /// Dynamically creates a Generic1 button by cloning an existing button in the layout.
        /// This handles cases where the BUILDING_SMALL layout doesn't have a Generic1 slot.
        /// </summary>
        static ButtonWithHotkey CreateQueenTierButton(UIClickLayout layout) {
            try {
                // Get the buttonsWithHotkey list via reflection
                var listField = AccessTools.Field(typeof(UIClickLayout), "buttonsWithHotkey");
                if (listField == null) return null;
                var buttons = listField.GetValue(layout) as List<ButtonWithHotkey>;
                if (buttons == null || buttons.Count == 0) return null;

                // Find a template button to clone (prefer one with btButton_better for nice visuals)
                ButtonWithHotkey template = null;
                foreach (var b in buttons) {
                    if (b.btButton_better != null && b.btButton_better.gameObject != null) {
                        template = b;
                        break;
                    }
                }
                if (template == null) template = buttons[0];
                if (template == null) return null;

                // Determine the source GameObject
                GameObject sourceGo = template.btButton_better?.gameObject ?? template.btButton?.gameObject;
                if (sourceGo == null) return null;

                // Clone the button's entire UI hierarchy
                var clonedGo = UnityEngine.Object.Instantiate(sourceGo, sourceGo.transform.parent);
                clonedGo.name = "BtQueenTier_Spire";
                clonedGo.SetActive(true);

                // Build a new ButtonWithHotkey pointing at the cloned components
                var newBtn = new ButtonWithHotkey();
                newBtn.buttonType = (UIClickButtonType)50; // Generic1

                // Wire up btButton_better or btButton depending on what the template had
                var clonedBetter = clonedGo.GetComponent<UITextImageButton>();
                if (clonedBetter != null) {
                    newBtn.btButton_better = clonedBetter;
                } else {
                    var clonedSimple = clonedGo.GetComponent<UIButton>();
                    if (clonedSimple != null) newBtn.btButton = clonedSimple;
                }

                // Try to find TMP_Text on the clone for the label
                var tmpText = clonedGo.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmpText != null) newBtn.lbButton = tmpText;

                // Hide any hotkey indicator on the cloned button
                if (newBtn.obHotkey != null) newBtn.obHotkey.SetActive(false);

                // Add to the layout's button list so GetButton/UpdateButton can find it
                buttons.Add(newBtn);
                Debug.Log("[Spire] Dynamically created Queen tier button (cloned from existing layout button)");
                return newBtn;
            } catch (Exception ex) {
                Debug.Log($"[Spire] CreateQueenTierButton failed: {ex.Message}");
                return null;
            }
        }
    }
    // Queen Init — restore saved tier on first init only (uses initialized flag)
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

    // GetUiClickType_Intake -> 29 (UNLOCKER) so chooser panel activates
    [HarmonyPatch(typeof(RadarIslandScanner), "GetUiClickType_Intake")]
    public static class SpireClickTypePatch {
        [HarmonyPrefix] static bool Prefix(RadarIslandScanner __instance, ref UIClickType __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = (UIClickType)29; return false;
        }
    }

    // Init — set unlockerType=1; load saved track from PlayerPrefs; apply gold+purple visuals
    [HarmonyPatch(typeof(RadarIslandScanner), "Init")]
    public static class SpireInitPatch {
        [HarmonyPostfix] static void Postfix(RadarIslandScanner __instance) {
            if (!SpireHelper.IsSpire(__instance)) return;
            try {
                AccessTools.Field(typeof(Unlocker), "unlockerType").SetValue(__instance, (UnlockerType)1);
                // Restore previously selected track from PlayerPrefs (not instance-ID-based dict)
                int savedTrack = ModSave.LoadSpireTrack();
                ModState.SetSpireTrack(__instance, savedTrack);
                Debug.Log($"[Spire] Init: restored track {savedTrack} ({ModState.GetTrackName(savedTrack)})");
                AccessTools.Field(typeof(Building), "showInventory")?.SetValue(__instance, true);
                // Scale up slightly for presence
                __instance.transform.localScale = new Vector3(1.1f, 1.6f, 1.1f);
                // Gold + purple visuals
                foreach (var r in __instance.GetComponentsInChildren<MeshRenderer>()) {
                    r.material.SetColor("_Color", new Color(0.38f, 0.06f, 0.62f, 1f));
                    r.material.SetColor("_EmissionColor", new Color(1.8f, 1.3f, 0.0f, 1f));
                    r.material.EnableKeyword("_EMISSION");
                }

                // Phase 2: Attach Deep Excavator behavior
                if (__instance.gameObject.GetComponent<DeepExcavatorBehavior>() == null) {
                    var excavator = __instance.gameObject.AddComponent<DeepExcavatorBehavior>();
                    excavator.spireBuilding = __instance;
                    Debug.Log("[Spire] Deep Excavator attached to Colony Spire");
                }

                Debug.Log("[Spire] Init done");
            } catch (Exception ex) { Debug.Log($"[Spire] Init failed: {ex.Message}"); }
        }
    }

    // Keys 1-6 to switch track while hovering; (Update: now Keys 1-7 since there are 7 tracks)
    // also ticks Sentinel hatch timer and spawns on completion.
    [HarmonyPatch(typeof(RadarIslandScanner), "BuildingUpdate")]
    public static class SpireBuildingUpdatePatch {
        [HarmonyPostfix] static void Postfix(RadarIslandScanner __instance, float dt) {
            if (!SpireHelper.IsSpire(__instance)) return;

            // Track selection keys
            for (int k = 0; k < ModState.TrackNames.Length; k++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + k))
                    ModState.SetSpireTrack(__instance, k);

            // Sentinel hatch timer
            if (ModState.sentinelHatchTimer > 0f && ModState.sentinelSpire == __instance) {
                ModState.sentinelHatchTimer -= dt;
                if (ModState.sentinelHatchTimer <= 0f) {
                    ModState.sentinelHatchTimer = -1f;
                    ModState.sentinelSpire = null;
                    try {
                        // Spawn the Sentinel at the Spire's position
                        var pos = __instance.transform.position;
                        var rot = UnityEngine.Random.rotation;
                        var sentinel = GameManager.instance.SpawnAnt(
                            (AntCaste)38,   // SENTINEL
                            pos, rot, null
                        );
                        if (sentinel != null) {
                            // Drop it on the ground near the Spire so it lands properly
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

    // Unlocker.SetUnlock — store track index in our dict, set currentUnlock = null (safe)
    [HarmonyPatch(typeof(Unlocker), "SetUnlock")]
    public static class UnlockerSetUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, string _unlock) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int idx = ModState.TrackIndexFromCode(_unlock);
            if (idx >= 0) ModState.SetSpireTrack(__instance, idx);
            // Leave currentUnlock = null; SetUnlockNamePatch handles the display
            AccessTools.Field(typeof(Unlocker), "currentUnlock").SetValue(__instance, null);
            return false; // skip original DB lookup
        }
    }

    // UIClickLayout_Unlocker.SetUnlockName — show our track name safely (handles null currentUnlock)
    [HarmonyPatch(typeof(UIClickLayout_Unlocker), "SetUnlockName")]
    public static class SetUnlockNamePatch {
        [HarmonyPrefix] static bool Prefix(UIClickLayout_Unlocker __instance, Unlocker unlocker) {
            if (!SpireHelper.IsSpire(unlocker)) return true;
            try {
                int idx = ModState.GetSpireTrack(unlocker);
                string label = $"{ModState.GetTrackName(idx)} — Lv{ModState.GetTrackLevel(idx)}";
                // Set the label text field directly
                var lbResult = AccessTools.Field(typeof(UIClickLayout_Unlocker), "lbUnlockResult")?.GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (lbResult != null) lbResult.text = label;
                var lbWill = AccessTools.Field(typeof(UIClickLayout_Unlocker), "lbWillUnlock")?.GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (lbWill != null) lbWill.text = "Upgrade Track:";
                // Hide the sprite (no icon for our custom tracks)
                var sprite = AccessTools.Field(typeof(UIClickLayout_Unlocker), "uiSprite")?.GetValue(__instance) as GameObject;
                if (sprite != null) sprite.SetActive(false);
            } catch (Exception ex) { Debug.Log($"[Spire] SetUnlockName: {ex.Message}"); }
            return false; // skip original (which would crash on null currentUnlock)
        }
    }

    // Unlocker.PickUnlock — cycle our tracks instead of querying TechTree
    [HarmonyPatch(typeof(Unlocker), "PickUnlock")]
    public static class UnlockerPickUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int current = ModState.GetSpireTrack(__instance);
            int next = (current + 1) % ModState.TrackCodes.Length;
            ModState.SetSpireTrack(__instance, next);
            // Also update currentUnlock field to null (safe — SetUnlockNamePatch handles display)
            AccessTools.Field(typeof(Unlocker), "currentUnlock").SetValue(__instance, null);
            return false; // skip original
        }
    }

    // Unlocker.AnythingToUnlock -> true
    [HarmonyPatch(typeof(Unlocker), "AnythingToUnlock")]
    public static class UnlockerAnythingToUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, ref bool __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = true; return false;
        }
    }

    // Unlocker.GetAvailableBiomeRevealsCount
    [HarmonyPatch(typeof(Unlocker), "GetAvailableBiomeRevealsCount")]
    public static class UnlockerGetAvailableCountPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, ref int __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = ModState.TrackCodes.Length; return false;
        }
    }

    // Unlocker.EAvailableBiomeReveals -> our track codes
    [HarmonyPatch(typeof(Unlocker), "EAvailableBiomeReveals")]
    public static class UnlockerEAvailablePatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, ref IEnumerable<string> __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            __result = ModState.TrackCodes; return false;
        }
    }

    // Unlocker.CanInsert_Intake — accept all required materials for current track.
    // Each material can only be inserted up to its required count.
    [HarmonyPatch(typeof(Unlocker), "CanInsert_Intake")]
    public static class UnlockerCanInsertPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance, PickupType _type, ref bool __result) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            // Block all inserts while a Sentinel is hatching
            if (ModState.sentinelHatchTimer > 0f) { __result = false; return false; }
            int track = ModState.GetSpireTrack(__instance);
            var costs = ModState.GetTrackCost(track);
            // Check if this pickup type is part of the cost
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

    // Unlocker.DoUnlock — consume all required materials, upgrade track (or start Sentinel hatch)
    [HarmonyPatch(typeof(Unlocker), "DoUnlock")]
    public static class UnlockerDoUnlockPatch {
        [HarmonyPrefix] static bool Prefix(Unlocker __instance) {
            if (!SpireHelper.IsSpire(__instance)) return true;
            int track = ModState.GetSpireTrack(__instance);
            // Block if Sentinel already hatching
            if (track == 5 && ModState.sentinelHatchTimer > 0f) {
                Debug.Log("[Spire] Sentinel still hatching, please wait!");
                return false;
            }
            var costs = ModState.GetTrackCost(track);
            // Consume all materials
            foreach (var (type, count) in costs)
                __instance.RemovePickup(type, count, (BuildingStatus)4);
            // Set the spire reference before UpgradeTrack (which starts the timer for track 5)
            if (track == 5) ModState.sentinelSpire = __instance as RadarIslandScanner;
            ModState.UpgradeTrack(track);
            return false;
        }
    }

    // Unlocker.GatherRecipeProgress — show all required materials with have/need counts.
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
            // Sentinel hatching: show timer instead of material requirements
            if (track == 5 && ModState.sentinelHatchTimer > 0f) {
                int secs = Mathf.CeilToInt(ModState.sentinelHatchTimer);
                pickup_icons.Add(((PickupType)328, $"Hatching... {secs}s"));
                go = false; // block further upgrades during hatch
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

    // UIClickLayout_Unlocker.SetChangeButton — bypass UIRecipeMenu (which calls UnlockRecipeData.Get)
    // and wire btChange to directly cycle tracks
    [HarmonyPatch(typeof(UIClickLayout_Unlocker), "SetChangeButton")]
    public static class SetChangeButtonOverridePatch {
        [HarmonyPrefix] static bool Prefix(UIClickLayout_Unlocker __instance, Unlocker unlocker) {
            if (!SpireHelper.IsSpire(unlocker)) return true;
            try {
                // Show btChange
                var btChange = AccessTools.Field(typeof(UIClickLayout_Unlocker), "btChange")?.GetValue(__instance) as UITextImageButton;
                if (btChange == null) return false;
                btChange.gameObject.SetActive(true);
                btChange.SetButton(() => {
                    // Cycle to next track
                    int current = ModState.GetSpireTrack(unlocker);
                    int next = (current + 1) % ModState.TrackCodes.Length;
                    ModState.SetSpireTrack(unlocker, next);
                    // Update the name label immediately
                    AccessTools.Method(typeof(UIClickLayout_Unlocker), "SetUnlockName")
                        ?.Invoke(__instance, new object[] { unlocker });
                });
                btChange.SetText($"▶ Change Track");
            } catch (Exception ex) { Debug.Log($"[Spire] SetChangeButton: {ex.Message}"); }
            return false; // skip original (which would open UIRecipeMenu)
        }
    }
    [HarmonyPatch(typeof(Progress), "Read")]
    public static class AutoUnlockPatch {
        [HarmonyPostfix] static void Postfix() {
            try {
                // Colony Spire is now unlocked via the tech tree (MOD_COLONY_SPIRE node)
                // Only auto-unlock if the tech has been researched
                if (ModState.ModTechResearched("MOD_COLONY_SPIRE")) {
                    Progress.UnlockBuilding("COLONY_SPIRE", true);
                    Debug.Log("[Spire] Colony Spire unlocked (tech researched)");
                } else {
                    Debug.Log("[Spire] Colony Spire locked — research MOD_COLONY_SPIRE first");
                }
                ModSave.Load();  // restore prestige + track levels
            }
            catch (Exception ex) { Debug.Log($"[Spire] Auto-unlock: {ex.Message}"); }
        }
    }

    // Save mod state whenever the game saves
    [HarmonyPatch(typeof(Progress), "Write")]
    public static class SaveOnWritePatch {
        [HarmonyPostfix] static void Postfix() {
            try { ModSave.Save(); }
            catch (Exception ex) { Debug.Log($"[Spire] SaveOnWrite: {ex.Message}"); }
        }
    }

    // ================================================================
    // PHASE 6: MOLD RESISTANCE — Hardened Carapace
    // StatusEffects.CombineEffects resets lifeDrainFactor to 1 then multiplies by
    // each active effect's effectDrainFactor. We postfix-patch it so that after the
    // game has finished accumulating status-effect drain, we multiply the result by
    // our carapace reduction factor.  carapaceLevel 5 = full immunity (factor = 0).
    // ================================================================
    [HarmonyPatch(typeof(StatusEffects), "CombineEffects")]
    public static class MoldResistancePatch {
        [HarmonyPostfix] static void Postfix(StatusEffects __instance) {
            if (ModState.carapaceLevel <= 0) return;
            float reduction = Math.Max(0f, 1f - ModState.carapaceLevel * 0.2f);
            // lifeDrainFactor > 1 means active drain from mold/disease status effects.
            // We scale down the EXCESS above the baseline of 1 to reduce mold damage.
            // If fully immune (reduction==0), clamp the whole factor to 0 (no drain at all).
            var field = AccessTools.Field(typeof(StatusEffects), "lifeDrainFactor");
            float current = (float)field.GetValue(__instance);
            if (current > 1f) {
                // Scale the drain portion: new = 1 + (current - 1) * reduction
                field.SetValue(__instance, 1f + (current - 1f) * reduction);
            }
        }
    }

    // ================================================================
    // PHASE 7: MINING SPEED — Deep Mining
    // BiomeObject.GetMineDuration(float mine_speed) returns:
    //   GlobalValues.baseMineDuration * hardness / mine_speed
    // Smaller result = faster pickup. We divide __result by our mining multiplier.
    // ================================================================
    [HarmonyPatch(typeof(BiomeObject), "GetMineDuration")]
    public static class MiningSpeedPatch {
        [HarmonyPostfix] static void Postfix(ref float __result) {
            if (ModState.miningLevel <= 0) return;
            float multiplier = 1f + ModState.miningLevel * 0.15f;
            __result /= multiplier;
        }
    }

    // ================================================================
    // PHASE 8: WING CARRY CAPACITY — Wing Strengthening
    // ================================================================
    [HarmonyPatch(typeof(Ant), "Fill")]
    public static class WingCarryPatch {
        [HarmonyPostfix] static void Postfix(Ant __instance, AntCasteData _data) {
            if (_data == null) return;

            // ── T4 Omni-Ant Stats ──
            // For now, T4 is detected by caste ID matching OMNI_ANT_CASTE_ID.
            // Until we have a real T4 caste in prefabs.fods, we can test by manually
            // spawning via console: SpawnAnt((AntCaste)10, pos, rot, null)
            if ((int)_data.caste == ModState.OMNI_ANT_CASTE_ID) {
                var carryField = AccessTools.Field(typeof(Ant), "carryCapacity");
                var speedField = AccessTools.Field(typeof(Ant), "speed");

                // Carry: base 2, scaling with prestige
                carryField?.SetValue(__instance, ModState.GetOmniAntCarry());

                // Speed: 2x base
                if (speedField != null) {
                    float baseSpeed = (float)speedField.GetValue(__instance);
                    speedField.SetValue(__instance, baseSpeed * 2.0f);
                }

                // Lifespan: 600 seconds (10 minutes)
                __instance.energy = 600f;

                // Flight: enable
                var flyField = AccessTools.Field(typeof(Ant), "canFly");
                flyField?.SetValue(__instance, true);

                Debug.Log($"[Spire] T4 Omni-Ant spawned! Carry={ModState.GetOmniAntCarry()} Speed=2x Lifespan=600s Fly=true");
                return; // don't also apply wing carry below
            }

            // ── Original Wing Carry Logic (flying ants only) ──
            if (ModState.wingLevel <= 0) return;
            if (!_data.flying) return;
            var field = AccessTools.Field(typeof(Ant), "carryCapacity");
            int current = (int)field.GetValue(__instance);
            field.SetValue(__instance, current + ModState.wingLevel);
            Debug.Log($"[Spire] WingCarry: {_data.caste} carry {current} -> {current + ModState.wingLevel}");
        }
    }

    // ================================================================
    // PHASE 2: T4 OMNI-ANT — Mining Speed Override
    // BiomeObject.GetMineDuration is already patched by MiningSpeedPatch,
    // but T4 ants get an addtional 3x mining speed multiplier.
    // We detect the ant that's mining by checking the calling context.
    // For now, the global mining patch handles T4 implicitly because
    // the ant's speed parameter already incorporates the 2x speed buff.
    // ================================================================

    // ================================================================
    // PHASE 2: GYNE TOWER PRESTIGE ACCUMULATOR
    // Intercepts GyneTower.CheckIfGateIsSatisfied to accept elder
    // Omni-Ants. When an elder T4 ant enters, it's consumed for
    // prestige points. When threshold is met, trigger the nuptial flight.
    // ================================================================
    [HarmonyPatch(typeof(GyneTower), "CheckIfGateIsSatisfied")]
    public static class GyneTowerPrestigePatch {
        [HarmonyPrefix] static bool Prefix(GyneTower __instance, Ant ant, ref bool __result) {
            if (ant == null) return true; // let vanilla handle null

            // Only intercept T4 Omni-Ants
            var casteField = AccessTools.Field(typeof(Ant), "caste");
            if (casteField == null) return true;
            int caste = (int)casteField.GetValue(ant);
            if (caste != ModState.OMNI_ANT_CASTE_ID) {
                // Not a T4 ant — block entry (only T4 elders can prestige)
                __result = false;
                return false;
            }

            // Check elder status (must have OLD status effect)
            // Ant.HasStatusEffect(StatusEffect) is the correct API (see TrailGate_Old.cs)
            bool isElder = false;
            try {
                isElder = ant.HasStatusEffect(StatusEffect.OLD);
            } catch { }

            if (!isElder) {
                // Not elder yet — reject
                __result = false;
                return false;
            }

            // Elder T4 Omni-Ant! Consume it for prestige points.
            ModState.prestigePoints += ModState.PRESTIGE_POINTS_PER_OMNI;
            Debug.Log($"[Spire] Elder Omni-Ant sacrificed! +{ModState.PRESTIGE_POINTS_PER_OMNI} points. Total: {ModState.prestigePoints}/{ModState.GetPrestigeThreshold()}");

            // Kill the ant (consumed by the tower)
            ant.Die(DeathCause.OLD_AGE);

            // Check if we've hit the threshold
            if (ModState.prestigePoints >= ModState.GetPrestigeThreshold()) {
                // SUPER GYNE LAUNCH!
                ModState.prestigePoints = 0;
                ModState.prestigeLevel++;
                Debug.Log($"[Spire] ★★★ SUPER GYNE LAUNCH! Prestige now level {ModState.prestigeLevel} ★★★");

                // Trigger the actual nuptial flight via the tower's StartGyne
                try {
                    AccessTools.Method(typeof(GyneTower), "StartGyne")?.Invoke(__instance, null);
                } catch (Exception ex) {
                    Debug.LogError($"[Spire] StartGyne invoke failed: {ex.Message}");
                }

                // Massive visual celebration
                var pos = __instance.transform.position;
                for (int i = 0; i < 10; i++) {
                    var offset = UnityEngine.Random.insideUnitSphere * 5f;
                    offset.y = Mathf.Abs(offset.y) + 2f;
                    GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF5, pos + offset);
                }
            }

            ModSave.Save();
            __result = false; // we handled it
            return false;
        }
    }

    // ================================================================
    // PHASE 11: GATHERER SPEED — reduces DELAY_INITIAL on Gatherer buildings
    // Gatherer.DELAY_INITIAL defaults to 1.0f; each level subtracts 0.2s (capped at 0).
    // We patch UseBuilding to set the field before the coroutine reads it.
    // ================================================================
    [HarmonyPatch(typeof(Gatherer), "UseBuilding")]
    public static class GathererDelayPatch {
        [HarmonyPrefix] static void Prefix(Gatherer __instance) {
            if (ModState.gathererLevel <= 0) return;
            float newDelay = Math.Max(0f, 1f - ModState.gathererLevel * 0.2f);
            var field = AccessTools.Field(typeof(Gatherer), "DELAY_INITIAL");
            if (field != null) field.SetValue(__instance, newDelay);
        }
    }

    // ================================================================
    // PHASE 12: ENERGY EFFICIENCY
    // Hook: Ant.GetMaxEnergy() to add +10% max energy per level.
    // ================================================================
    [HarmonyPatch(typeof(Ant), "GetMaxEnergy")]
    public static class EnergyDrainPatch {
        [HarmonyPostfix] static void Postfix(ref float __result) {
            if (ModState.energyLevel <= 0) return;
            float multiplier = 1f + ModState.energyLevel * 0.05f;
            __result *= multiplier;
        }
    }

    // ================================================================
    // HUD FIX: Larvae/min bar shows tier-adjusted rate + larva type
    // ================================================================
    [HarmonyPatch(typeof(UIGame), "UpdateHungerBar")]
    public static class LarvaRateHudPatch {
        static readonly float[] TierDivisors = { 1f, 1f, 3f, 9f }; // idx 0 unused; 1=T1, 2=T2, 3=T3
        static readonly string[] TierLabels  = { "",  "T1", "T2", "T3" };

        [HarmonyPostfix] static void Postfix(UIGame __instance, float larva_rate) {
            int lTier = Math.Max(1, Math.Min(3, ModState.displayTier)); // 1,2 or 3
            if (lTier <= 1) return; // T1 = vanilla display, no change needed

            try {
                float adjustedRate = larva_rate / TierDivisors[lTier];
                // Round to 1 decimal place to match vanilla formatting
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
    // INITIAL ISLAND SCALE OVERRIDE
    // ================================================================
    [HarmonyPatch(typeof(Ground), "InitShape")]
    public static class GroundInitShapePatch {
        static float[] origRadii;
        static Vector3[] origCenters;
        
        [HarmonyPrefix]
        static void Prefix(Ground __instance) {
            if (GameManager.instance == null) return;
            // Only apply scale if it's the very first ground (initial island)
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
            // Restore colliders immediately so prefab is left intact
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
            // Target the cloned instance for the local scale
            if (__result != null && GameManager.instance != null && GameManager.instance.GetGroundCount() == 0) {
                __result.transform.localScale = Vector3.one * ModState.activeIslandScale;
                Debug.Log($"[Spire] Scaled initial island to {ModState.activeIslandScale}");
            }
        }
    }

    // ================================================================
    // ISLAND SCALE SAVE/LOAD
    // ================================================================
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
            // save_name is empty when creating a new game
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
                ModState.activeIslandScale = 1.0f; // Vanilla save or created before mod scale feature
            } catch (Exception ex) {
                Debug.Log($"[Spire] IslandScale load failed: {ex}");
                ModState.activeIslandScale = 1.0f;
            }
        }
    }

    // ================================================================
    // SETTINGS MENU INJECTION (Adds slider + color dropdown to UI)
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

                SetupFeatureToggle(__instance, addSettingMethod, "Colored Trails",
                    "Color variants for Main Bus trails",
                    () => ModState.enableColoredTrails,
                    v => { ModState.enableColoredTrails = v; ModSave.SaveSettings(); });

                SetupFeatureToggle(__instance, addSettingMethod, "Stockpile Gate Battery Target",
                    "Stockpile gates can target battery",
                    () => ModState.enableBatteryGates,
                    v => { ModState.enableBatteryGates = v; ModSave.SaveSettings(); });

                // ── Section: Colony Spire Mod (master + sub-toggles) ──
                var blankMaster = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                if (blankMaster != null) blankMaster.InitEmpty();

                SetupFeatureToggle(__instance, addSettingMethod, "★ Colony Spire Mod",
                    "Master toggle for all Colony Spire content (requires restart)",
                    () => ModState.enableColonySpire,
                    v => { ModState.enableColonySpire = v; ModSave.SaveSettings(); });

                // Only show sub-toggles and slider when master is enabled
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
                        // Snap to nearest 5% (0.05 increments)
                        float snapped = Mathf.Round(v * 20f) / 20f;
                        ModState.islandScale = snapped;
                        ModSave.SaveSettings();
                        
                        // Override the percentage text label to show the snapped %
                        var valueAfterField = AccessTools.Field(typeof(UISettings_Setting), "valueAfter");
                        if (valueAfterField != null) {
                            var valueAfter = valueAfterField.GetValue(sliderSetting) as UITextImageButton;
                            if (valueAfter != null) {
                                valueAfter.SetText($"{Mathf.Round(snapped * 100f)}%");
                            }
                        }
                    }
                );
                
                // Override the header directly to avoid missing Loc keys
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
                // Override header text (Loc key won't exist)
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

    // ================================================================
    // MAIN BUS TRAIL COLOR CUSTOMIZATION — SAVE & LOAD
    // ================================================================

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


    // ================================================================
    // CONCRETE ISLAND OVERHAUL (PHASES 1 - 5)
    // ================================================================
    public class HostileCorpseData
    {
        public float health = 50000f;
        public float maxHealth = 50000f;
        public bool isDead = false;
    }

    public static class RobotCorpseManager
    {
        public static ConditionalWeakTable<BiomeObject, HostileCorpseData> corpses = new();
        
        public static bool IsRobotCorpse(BiomeObject bob)
        {
            if (bob == null || bob.data == null) return false;
            
            // Concrete Island boss uses this unique internal code under the hood!
            if (bob.data.code == "BOB_GOLD_DEPOSIT_CONCRETE") return true;
            
            string code = bob.data.code?.ToUpperInvariant() ?? "";
            string title = bob.data.GetTitle()?.ToUpperInvariant() ?? "";
            
            return code.Contains("ROBOT") || code.Contains("CORPSE") || code.Contains("PINATA") || title.Contains("REMNANT") || title.Contains("ROBOT"); 
        }
        
        public static HostileCorpseData GetCorpseData(BiomeObject bob)
        {
            return corpses.GetOrCreateValue(bob);
        }

        public static void EnsureBehavior(BiomeObject bob)
        {
            if (bob != null && bob.gameObject != null && IsRobotCorpse(bob))
            {
                var behavior = bob.gameObject.GetComponent<RobotCorpseBehavior>();
                if (behavior == null)
                {
                    behavior = bob.gameObject.AddComponent<RobotCorpseBehavior>();
                    behavior.biomeObject = bob;
                    bob.data.infinite = true; // Make sure ants don't stop mining
                }
                // Always ensure shield dome is unlocked when the corpse is present
                Progress.UnlockBuilding("BUILD_RADAR_TOWER", during_load: true);
            }
        }
    }

    [HarmonyPatch(typeof(BiomeObject), "SetHoverUI")]
    public static class CorpseSetHoverHealthPatch
    {
        [HarmonyPostfix]
        static void Postfix(BiomeObject __instance, UIHoverClickOb ui_hover)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                RobotCorpseManager.EnsureBehavior(__instance);
                var data = RobotCorpseManager.GetCorpseData(__instance);
                if (!data.isDead)
                {
                    ui_hover.SetHealth("STRUCTURAL INTEGRITY");
                }
            }
        }
    }

    [HarmonyPatch(typeof(BuildingData), "GetTitle")]
    public static class ShieldGeneratorTitlePatch
    {
        [HarmonyPostfix]
        static void Postfix(BuildingData __instance, ref string __result)
        {
            if (__instance.code == "BUILD_RADAR_TOWER")
            {
                __result = "Energy Shield Dome";
            }
        }
    }

    [HarmonyPatch(typeof(BuildingData), "GetDescription")]
    public static class ShieldGeneratorDescPatch
    {
        [HarmonyPostfix]
        static void Postfix(BuildingData __instance, ref string __result)
        {
            if (__instance.code == "BUILD_RADAR_TOWER")
            {
                __result = "Projects a huge defensive Dome that protects ants from robotic zaps. Consumes 50 Electricity from the grid per blocked attack.";
            }
        }
    }

    [HarmonyPatch(typeof(RadarTower), "Init")]
    public static class RadarTowerInitPatch
    {
        [HarmonyPostfix]
        static void Postfix(RadarTower __instance)
        {
            if (__instance.transform.Find("ShieldDomeShape") == null)
            {
                GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = "ShieldDomeShape";
                dome.transform.SetParent(__instance.transform, false);
                dome.transform.localPosition = new Vector3(0, 0f, 0); // At base
                dome.transform.localScale = new Vector3(90f, 90f, 90f); // 45 radius * 2 = 90 diameter
                
                // remove collider so ants don't walk on it or get blocked by it
                var collider = dome.GetComponent<Collider>();
                if (collider != null) GameObject.Destroy(collider);
                
                var renderer = dome.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // Create a transparent blue material using an always-available shader
                    Material mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.2f, 0.6f, 1f, 0.15f); // translucent blue
                    renderer.material = mat;
                }
                
                var behavior = dome.AddComponent<ShieldDomeBehavior>();
                behavior.tower = __instance;
            }
        }
    }
    
    public class ShieldDomeBehavior : MonoBehaviour
    {
        public RadarTower tower;
        private MeshRenderer meshRenderer;

        public void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Update()
        {
            if (tower == null || tower.ground == null) return;
            // Only display the fully powered blue shield if we have enough electricity to intercept an attack
            if (tower.IsPlaced() && tower.currentStatus == BuildingStatus.COMPLETED && tower.ground.GetEnergy(50f) >= 49f)
            {
                if (meshRenderer != null) meshRenderer.enabled = true;
                
                // Slowly rotate it and pulsate slightly or just pulsate its alpha
                if (meshRenderer != null)
                {
                    float alpha = 0.15f + Mathf.Sin(Time.time * 2f) * 0.05f;
                    Color c = meshRenderer.material.color;
                    meshRenderer.material.color = new Color(c.r, c.g, c.b, alpha);
                }
            }
            else
            {
                if (meshRenderer != null) meshRenderer.enabled = false;
            }
        }
    }

    // Phase 1: Make mining faster on corpses so ants "attack" more rapidly.
    [HarmonyPatch(typeof(BiomeObject), "GetMineDuration")]
    public static class CorpseMineDurationPatch
    {
        [HarmonyPrefix]
        static bool Prefix(BiomeObject __instance, float mine_speed, ref float __result)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                RobotCorpseManager.EnsureBehavior(__instance);
                // Ants very rapidly strike the corpse. It's scaled by their mine speed.
                // 0.25f means 4 times a second for a base ant!
                __result = 0.25f / mine_speed;
                return false; 
            }
            return true;
        }
    }

    public static class ModSpawners
    {
        public static void SpawnMegaPinata(Vector3 center)
        {
            Debug.Log("[Spire/Corpse] Spawning Mega Pinata!");
            PickupType[] drops = { 
                (PickupType)334, // Microchip
                (PickupType)327, // Wafer
                (PickupType)329, // Biofuel
                (PickupType)328  // Royal Jelly
            };

            int dropCount = 40; // Massive shower of items
            for (int i = 0; i < dropCount; i++)
            {
                PickupType drop = drops[UnityEngine.Random.Range(0, drops.Length)];
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 2f;
                // Mostly upward offset
                offset.y = Mathf.Abs(offset.y) + 1f;

                try {
                    Pickup p = GameManager.instance.SpawnPickup(drop, center + offset, UnityEngine.Random.rotation);
                    if (p != null) {
                        Rigidbody rb = p.GetComponent<Rigidbody>();
                        if (rb != null) {
                            rb.isKinematic = false;
                            // Add physics force to blast outward
                            rb.AddExplosionForce(500f, center, 10f);
                        }
                    }
                } catch { } // Safety swallow inside physics loop
            }
        }
    }

    [HarmonyPatch(typeof(BiomeObject), "UpdateHoverUI")]
    public static class CorpseHoverHealthPatch
    {
        [HarmonyPostfix]
        static void Postfix(BiomeObject __instance, UIHoverClickOb ui_hover)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                RobotCorpseManager.EnsureBehavior(__instance);
                var data = RobotCorpseManager.GetCorpseData(__instance);
                if (data.isDead) return;
                
                string hpText = $"{Mathf.CeilToInt(data.health)} / {Mathf.CeilToInt(data.maxHealth)} HP";
                float hpRatio = Mathf.Clamp01(data.health / data.maxHealth);
                ui_hover.SetTitle(__instance.data.GetTitle() + $" [{hpText}]");
                ui_hover.UpdateHealth(hpText, hpRatio);
                
                // Phase 2: Show excavation core reward instead of drop table
                float radius = 0f;
                try { radius = __instance.GetRadius(); } catch { }
                int coreReward = radius > 20f ? 10 : radius > 10f ? 3 : 1;
                string sizeLabel = radius > 20f ? "Large" : radius > 10f ? "Medium" : "Small";
                var coreDisplay = new Dictionary<PickupType, string>
                {
                    { (PickupType)328, $"{coreReward} Excavation Cores ({sizeLabel})" }
                };
                ui_hover.inventoryGrid.Update("Combat Reward", coreDisplay, "");
            }
        }
    }

    [HarmonyPatch(typeof(BiomeObject), "SetClickUi")]
    public static class CorpseSetClickUIHealthPatch
    {
        [HarmonyPostfix]
        static void Postfix(BiomeObject __instance, UIClickLayout ui_click)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                RobotCorpseManager.EnsureBehavior(__instance);
                var data = RobotCorpseManager.GetCorpseData(__instance);
                if (!data.isDead)
                {
                    string hpText = $"{Mathf.CeilToInt(data.health)} / {Mathf.CeilToInt(data.maxHealth)} HP";
                    ui_click.SetTitle(__instance.data.GetTitle() + $" [{hpText}]");
                }
            }
        }
    }

    [HarmonyPatch(typeof(BiomeObject), "UpdateClickUi")]
    public static class CorpseUpdateClickUIHealthPatch
    {
        [HarmonyPostfix]
        static void Postfix(BiomeObject __instance, UIClickLayout ui_click)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                RobotCorpseManager.EnsureBehavior(__instance);
                var data = RobotCorpseManager.GetCorpseData(__instance);
                if (data.isDead) return;
                
                string hpText = $"{Mathf.CeilToInt(data.health)} / {Mathf.CeilToInt(data.maxHealth)} HP";
                ui_click.SetTitle(__instance.data.GetTitle() + $" [{hpText}]");
                
                if (ui_click is UIClickLayout_BiomeObject clBiome)
                {
                    // Phase 2: Show excavation core reward
                    float radius = 0f;
                    try { radius = __instance.GetRadius(); } catch { }
                    int coreReward = radius > 20f ? 10 : radius > 10f ? 3 : 1;
                    string sizeLabel = radius > 20f ? "Large" : radius > 10f ? "Medium" : "Small";
                    var coreDisplay = new Dictionary<PickupType, string>
                    {
                        { (PickupType)328, $"{coreReward} Excavation Cores ({sizeLabel})" }
                    };
                    clBiome.inventoryGrid.Update("Combat Reward", coreDisplay, "");
                }
            }
        }
    }

    // Phase 1 & 2: Deal damage when the ant attempts to actually extract. Intercept the pickup insertion so the ant inventory stays empty and it MINEs again.
    [HarmonyPatch(typeof(Ant), "ExchangePickup", new Type[] { typeof(ExchangeType), typeof(Pickup), typeof(PickupContainer) })]
    public static class CorpseAttackPatch
    {
        [HarmonyPrefix]
        static bool Prefix(Ant __instance, ExchangeType exchange_type, Pickup _pickup, ref float __result)
        {
            if (exchange_type == ExchangeType.EXTRACT_INSTANT && _pickup != null)
            {
                var actionField = AccessTools.Field(typeof(Ant), "currentActionPoint");
                if (actionField != null)
                {
                    var action = actionField.GetValue(__instance) as ActionPoint;
                    if (action != null && action.exchangeType == ExchangeType.MINE)
                    {
                        var corpse = action.connectableObject as BiomeObject;
                        if (corpse != null && RobotCorpseManager.IsRobotCorpse(corpse))
                        {
                            var data = RobotCorpseManager.GetCorpseData(corpse);
                            if (!data.isDead) 
                            {
                                data.health -= 50f;
                                
                                // Visual impact effect at the strike location
                                GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1, _pickup.transform.position);

                                if (data.health <= 0 && !data.isDead)
                                {
                                    data.isDead = true;
                                    
                                    // Phase 2: Grant Excavation Cores based on corpse size
                                    float radius = 0f;
                                    try { radius = corpse.GetRadius(); } catch { }
                                    int coreReward = radius > 20f ? 10 : radius > 10f ? 3 : 1;
                                    ModState.excavationCores += coreReward;
                                    Debug.Log($"[Spire/Corpse] CORPSE DESTROYED! +{coreReward} Excavation Cores (total: {ModState.excavationCores})");

                                    // Celebratory explosion (no item drops)
                                    var pos = corpse.transform.position;
                                    for (int i = 0; i < 5; i++) {
                                        var offset = UnityEngine.Random.insideUnitSphere * 3f;
                                        offset.y = Mathf.Abs(offset.y) + 1f;
                                        GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF3, pos + offset);
                                    }

                                    ModSave.Save();
                                    corpse.Delete(); 
                                }
                            }

                            // The ant "mined" successfully, but we intercept and delete the item so the ant doesn't actually grab it.
                            // This forces the ant's inventory to stay empty, making it immediately swing again.
                            _pickup.Delete(); // Destroy the physics object
                            
                            // To keep the Ant locked in eternal combat without wandering back to base, we manually enqueue the Task 
                            // so it instantly pivots around and swings at the object again when its Action phase completes.
                            var queueField = AccessTools.Field(typeof(Ant), "nextActionPoints");
                            if (queueField != null)
                            {
                                var queue = queueField.GetValue(__instance) as Queue<ActionPoint>;
                                if (queue != null && !queue.Contains(action))
                                {
                                    queue.Enqueue(action);
                                }
                            }

                            __result = 1f;    // Hand back 1 so the Ant thinks it succeeded a transfer.
                            return false;     // Skip the vanilla exchange
                        }
                    }
                }
            }
            return true;
        }
    }
    // Phase 3: Regeneration & Hostile Attack Loop
    [HarmonyPatch(typeof(BiomeObject), "Init")]
    public static class CorpseInitPatch
    {
        [HarmonyPostfix]
        static void Postfix(BiomeObject __instance)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                __instance.data.infinite = true; // Ensure ants don't deplete the fake item source and run away
                
                // Force-unlock the Shield Dome building so the player can build it
                // without researching the Radar Tower tech
                Progress.UnlockBuilding("BUILD_RADAR_TOWER", during_load: true);
                
                var radarData = BuildingData.Get("BUILD_RADAR_TOWER");
                if (radarData != null)
                {
                    Debug.Log($"[Spire/Shield] BUILD_RADAR_TOWER found! group={radarData.group}, inBuildMenu={radarData.inBuildMenu}, maxBuildCount={radarData.maxBuildCount}, code={radarData.code}");
                    Debug.Log($"[Spire/Shield] HasUnlocked={Progress.HasUnlockedBuilding("BUILD_RADAR_TOWER")}");
                }
                else
                {
                    Debug.Log("[Spire/Shield] BUILD_RADAR_TOWER NOT FOUND in BuildingData! Dumping all building codes...");
                    foreach (var bd in PrefabData.buildings)
                    {
                        if (bd.code.Contains("RADAR") || bd.code.Contains("radar"))
                            Debug.Log($"[Spire/Shield] Found building: code={bd.code}, group={bd.group}, inBuildMenu={bd.inBuildMenu}");
                    }
                }
                
                if (__instance.gameObject.GetComponent<RobotCorpseBehavior>() == null)
                {
                    var behavior = __instance.gameObject.AddComponent<RobotCorpseBehavior>();
                    behavior.biomeObject = __instance;
                }
            }
        }
    }

    public class RobotCorpseBehavior : MonoBehaviour
    {
        public BiomeObject biomeObject;
        
        private float regenTimer = 0f;
        private float attackTimer = 0f;
        
        public void Start()
        {
            Debug.Log($"[Spire/Corpse] RobotCorpseBehavior attached to {gameObject.name}!");
        }

        public void Update()
        {
            if (biomeObject == null || biomeObject.data == null) return;
            if (GameManager.instance == null || GameManager.instance.GetStatus() != GameStatus.RUNNING) return;
            
            var data = RobotCorpseManager.GetCorpseData(biomeObject);
            if (data == null || data.isDead) return;
            
            float dt = Time.deltaTime;
            
            // 1. Health Regeneration
            regenTimer += dt;
            if (regenTimer >= 1f)
            {
                regenTimer -= 1f;
                // Only regen if not at max health
                if (data.health < data.maxHealth)
                {
                    // 150 health per second regen (was 100)
                    data.health = Mathf.Min(data.health + 150f, data.maxHealth); 
                }
            }
            
            // 2. Hostile Attack Loop (Every 2.5 seconds to cause panic)
            attackTimer += dt;
            if (attackTimer >= 2.5f)
            {
                attackTimer -= 2.5f;
                PerformAoEZap();
            }
        }
        private static GameObject zapPrefabCache;

        private GameObject GetZapPrefab()
        {
            if (zapPrefabCache != null) return zapPrefabCache;
            
            var data = BuildingData.Get("ELECTROLYZER_LARGE");
            if (data == null) data = BuildingData.Get("ELECTROLYZER");
            
            if (data != null && data.prefab != null)
            {
                var e = data.prefab.GetComponent<Electrolyser>();
                if (e != null)
                {
                    var field = AccessTools.Field(typeof(Electrolyser), "pfBeamEffect");
                    if (field != null)
                    {
                        zapPrefabCache = field.GetValue(e) as GameObject;
                    }
                }
            }
            return zapPrefabCache;
        }

        private void PerformAoEZap()
        {
            // The robot corpse releases a sudden short-range electric or radiation pulse
            float zapRadius = biomeObject.GetRadius() + 6f; // A medium sized radius relative to the giant robot
            int maxTargets = 5;   // Zaps up to multiple ants at once
            int targetsHit = 0;
            
            // Get all ants from the GameManager.
            // Using reflection because allAnts is a private field.
            var allAntsField = AccessTools.Field(typeof(GameManager), "allAnts");
            if (allAntsField == null) return;
            
            var allAnts = allAntsField.GetValue(GameManager.instance) as List<Ant>;
            if (allAnts == null) return;
            
            Debug.Log($"[Spire/Corpse] AoE Zap check! Origin: {transform.position}, Radius: {zapRadius}, Target limit: {maxTargets}, Ants count: {allAnts.Count}");
            GameObject prefab = GetZapPrefab();
            
            foreach (var ant in allAnts)
            {
                if (ant == null || ant.IsDead()) continue;
                
                float dist = Vector3.Distance(transform.position, ant.transform.position);
                if (dist <= zapRadius)
                {
                    // Check for an active Shield Generator (Radar Tower) in range!
                    bool shielded = false;
                    foreach (Building tower in GameManager.instance.EBuildings("BUILD_RADAR_TOWER"))
                    {
                        if (Vector3.Distance(ant.transform.position, tower.transform.position) <= 45f)
                        {
                            if (tower.ground != null && tower.ground.GetEnergy(50f) >= 49f)
                            {
                                shielded = true;
                                // Create a visual effect above the tower so the player sees it burning energy
                                GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF3, tower.transform.position + Vector3.up * 4f);
                                break;
                            }
                        }
                    }

                    if (shielded)
                    {
                        // The shield intercepts the hit! Strips barely any lifespan (12 seconds)
                        ant.energy -= 12f;
                        // Different colored explosion showing a shield block deflection
                        GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF5, ant.transform.position);
                    }
                    else
                    {
                        // The robot "zaps" the unprotected ant, stripping 6 minutes off its life span!
                        ant.energy -= 360f;
                        // Normal heavy damage hit
                        GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1, ant.transform.position);
                    }
                    
                    if (ant.energy <= 0)
                    {
                        ant.Die(DeathCause.OLD_AGE); 
                    }
                    
                    if (prefab != null)
                    {
                        var gobj = Instantiate(prefab, null);
                        var lr = gobj.GetComponent<LineRenderer>();
                        if (lr != null)
                        {
                            lr.positionCount = 2; // Make sure it only has 2 points
                            lr.SetPosition(0, transform.position + Vector3.up * 10f); // Fire from near the top of the giant boss
                            lr.SetPosition(1, ant.transform.position + Vector3.up * 0.5f);
                            gobj.AddComponent<ZapVisual>();
                            gobj.SetActive(true);
                        }
                    }
                    
                    targetsHit++;
                    Debug.Log($"[Spire/Corpse] AoE Zap executed on {ant.name}! Distance: {dist}. Shielded: {shielded}");
                    
                    if (targetsHit >= maxTargets) break;
                }
            }
        }
    }

    public class ZapVisual : MonoBehaviour
    {
        private LineRenderer lr;
        private float life = 0.5f;
        
        void Awake()
        {
            lr = GetComponent<LineRenderer>();
        }
        
        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            
            if (lr != null)
            {
                float a = life / 0.5f;
                // Just shrink width
                lr.startWidth = a * 0.5f;
                lr.endWidth = a * 0.5f;
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

    // ================================================================
    // PHASE 2: ISLAND FURNACE — Battery accepts ANY material
    // When furnaceEnabled (prestige >= 1), BatteryBuildings become
    // "Island Furnaces" that accept any material and convert to energy.
    // ================================================================
    [HarmonyPatch(typeof(BatteryBuilding), "CanInsert_Intake")]
    public static class FurnaceCanInsertPatch {
        [HarmonyPrefix]
        static bool Prefix(BatteryBuilding __instance, PickupType _type, ExchangeType exchange,
            ref bool let_ant_wait, ref bool __result) {
            if (!ModState.furnaceEnabled) return true; // vanilla behavior
            if (exchange != ExchangeType.BUILDING_IN) return true;

            // Accept any material — check if there's room for energy
            var capField = AccessTools.Field(typeof(BatteryBuilding), "energyCapacity");
            float cap = (float)(capField?.GetValue(__instance) ?? 100f);
            if (__instance.storedEnergy < cap - 1f) {
                __result = true;
                return false; // skip original
            }
            return true; // full, let vanilla handle rejection
        }
    }

    [HarmonyPatch(typeof(BatteryBuilding), "OnPickupArrival_Intake")]
    public static class FurnaceOnArrivalPatch {
        [HarmonyPrefix]
        static bool Prefix(BatteryBuilding __instance, Pickup _pickup) {
            if (!ModState.furnaceEnabled) return true; // vanilla
            if (_pickup == null) return true;

            // If the item has energyAmount > 0, let vanilla handle it (it's already an energy item)
            if (_pickup.data.energyAmount > 0f) return true;

            // Convert ANY material to energy
            var capField = AccessTools.Field(typeof(BatteryBuilding), "energyCapacity");
            float cap = (float)(capField?.GetValue(__instance) ?? 100f);
            
            // Remove from incoming list
            var incomingField = AccessTools.Field(typeof(Building), "incomingPickups_intake");
            if (incomingField != null) {
                var incoming = incomingField.GetValue(__instance) as List<Pickup>;
                incoming?.Remove(_pickup);
            }

            __instance.storedEnergy = Mathf.Clamp(
                __instance.storedEnergy + ModState.FURNACE_ENERGY_PER_ITEM,
                0f, cap);

            // Visual feedback
            GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1,
                _pickup.transform.position + Vector3.up * 0.5f);
            
            _pickup.Delete();

            // Update visual bars
            AccessTools.Method(typeof(BatteryBuilding), "UpdateVisual")
                ?.Invoke(__instance, null);
            
            return false; // skip vanilla
        }
    }

    // Override title for BatteryBuilding when furnace is enabled
    [HarmonyPatch(typeof(BuildingData), "GetTitle")]
    public static class FurnaceTitlePatch {
        [HarmonyPostfix]
        static void Postfix(BuildingData __instance, ref string __result) {
            if (!ModState.furnaceEnabled) return;
            // Check if this is a battery building (not the shield dome)
            if (__instance.code != null && __instance.code.Contains("BATTERY")) {
                __result = "Island Furnace";
            }
        }
    }

    // Override hover description for BatteryBuilding when furnace is active
    [HarmonyPatch(typeof(BuildingData), "GetDescription")]
    public static class FurnaceHoverPatch {
        [HarmonyPostfix]
        static void Postfix(BuildingData __instance, ref string __result) {
            if (!ModState.furnaceEnabled) return;
            if (__instance.code != null && __instance.code.Contains("BATTERY")) {
                __result = $"Burns any material into {ModState.FURNACE_ENERGY_PER_ITEM} energy. " +
                    $"Feeds the Deep Excavator's infinite resource loop.";
            }
        }
    }

    // ================================================================
    // PHASE 3: OBJ MESH LOADER — Runtime .obj file parser
    // Loads .obj files from the mod's Meshes/ directory and creates
    // Unity Mesh objects that can be applied to building prefabs.
    // ================================================================
    public static class ObjLoader {
        public static Mesh LoadObj(string path) {
            if (!File.Exists(path)) {
                ColonySpirePlugin.Log.LogWarning($"[ObjLoader] File not found: {path}");
                return null;
            }

            var tempVerts = new List<Vector3>();
            var tempNormals = new List<Vector3>();
            var tempUVs = new List<Vector2>();

            var faceVerts = new List<Vector3>();
            var faceNorms = new List<Vector3>();
            var faceUVs = new List<Vector2>();
            var triangles = new List<int>();

            foreach (string rawLine in File.ReadAllLines(path)) {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                string[] parts = line.Split(new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                try {
                    if (parts[0] == "v" && parts.Length >= 4) {
                        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        tempVerts.Add(new Vector3(x, y, z));
                    }
                    else if (parts[0] == "vn" && parts.Length >= 4) {
                        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        tempNormals.Add(new Vector3(x, y, z));
                    }
                    else if (parts[0] == "vt" && parts.Length >= 3) {
                        float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        tempUVs.Add(new Vector2(u, v));
                    }
                    else if (parts[0] == "f") {
                        var faceIndices = new List<int>();
                        for (int i = 1; i < parts.Length; i++) {
                            string[] indices = parts[i].Split('/');
                            int vi = int.Parse(indices[0]) - 1;
                            int ui = indices.Length > 1 && indices[1] != ""
                                ? int.Parse(indices[1]) - 1 : -1;
                            int ni = indices.Length > 2 && indices[2] != ""
                                ? int.Parse(indices[2]) - 1 : -1;

                            int idx = faceVerts.Count;
                            faceVerts.Add(vi >= 0 && vi < tempVerts.Count ? tempVerts[vi] : Vector3.zero);
                            faceNorms.Add(ni >= 0 && ni < tempNormals.Count
                                ? tempNormals[ni] : Vector3.up);
                            faceUVs.Add(ui >= 0 && ui < tempUVs.Count
                                ? tempUVs[ui] : Vector2.zero);
                            faceIndices.Add(idx);
                        }
                        // Fan triangulate (handles tris, quads, n-gons)
                        for (int i = 1; i < faceIndices.Count - 1; i++) {
                            triangles.Add(faceIndices[0]);
                            triangles.Add(faceIndices[i]);
                            triangles.Add(faceIndices[i + 1]);
                        }
                    }
                } catch (Exception ex) {
                    ColonySpirePlugin.Log.LogWarning($"[ObjLoader] Parse error on line '{line}': {ex.Message}");
                }
            }

            if (faceVerts.Count == 0) {
                ColonySpirePlugin.Log.LogError($"[ObjLoader] No vertices found in {path}");
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.name = Path.GetFileNameWithoutExtension(path);
            // CRITICAL: must set UInt32 BEFORE assigning vertices/triangles
            // to support meshes with >65k vertices
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = faceVerts.ToArray();
            mesh.normals = faceNorms.ToArray();
            mesh.uv = faceUVs.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            ColonySpirePlugin.Log.LogInfo($"[ObjLoader] Loaded {Path.GetFileName(path)}: " +
                $"{faceVerts.Count} verts, {triangles.Count / 3} tris");
            return mesh;
        }
    }

    // ================================================================
    // PHASE 3: CUSTOM BUILDING INJECTION
    // Clones existing building prefabs, replaces meshes with AI-generated
    // OBJ files, and registers them in PrefabData.buildings.
    // Uses lazy init via BuildingData.Get postfix.
    // ================================================================
    public static class CustomBuildingInjector {
        private static bool _injected = false;

        // Mesh directory: <game>/Mods/QueenTierMod/Meshes/
        public static string GetMeshDir() {
            string pluginDir = Path.GetDirectoryName(typeof(ColonySpirePlugin).Assembly.Location);
            return Path.Combine(pluginDir, "..", "..", "Mods", "QueenTierMod", "Meshes");
        }

        public static void EnsureInjected() {
            if (_injected) return;
            if (PrefabData.buildings == null || PrefabData.buildings.Count == 0) return;
            _injected = true;

            string meshDir = GetMeshDir();
            ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Mesh directory: {meshDir}");
            ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Directory exists: {Directory.Exists(meshDir)}");

            // --- LIQUID SMELTER ---
            InjectBuilding(
                templateCode: "SMELTER",
                newCode: "MOD_LIQUID_SMELTER",
                meshFile: Path.Combine(meshDir, "liquid_smelter.obj"),
                tintColor: new Color(1f, 0.4f, 0.1f, 1f),
                emissiveColor: new Color(2f, 0.8f, 0f, 1f),
                scaleMult: 3.0f,
                buildCost: "IRON_BAR 8, COPPER_BAR 4",
                buildGroup: BuildingGroup.PRODUCTION
            );

            // --- HIGH-END ASSEMBLER ---
            InjectBuilding(
                templateCode: "COMBINER",
                newCode: "MOD_ASSEMBLER",
                meshFile: Path.Combine(meshDir, "assembler.obj"),
                tintColor: new Color(0.2f, 0.5f, 1f, 1f),
                emissiveColor: new Color(0f, 1f, 2f, 1f),
                scaleMult: 2.0f,
                buildCost: "IRON_BAR 12, MICROCHIP 3, COPPER_BAR 6",
                buildGroup: BuildingGroup.PRODUCTION
            );
        }

        private static void InjectBuilding(string templateCode, string newCode,
            string meshFile, Color tintColor, Color emissiveColor,
            float scaleMult, string buildCost, BuildingGroup buildGroup) {

            // Check if already injected (e.g., on reload)
            foreach (var b in PrefabData.buildings)
                if (b.code == newCode) { ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] {newCode} already registered"); return; }

            BuildingData template = null;
            foreach (var b in PrefabData.buildings)
                if (b.code == templateCode) { template = b; break; }

            if (template == null) {
                ColonySpirePlugin.Log.LogWarning($"[Spire/Buildings] Template '{templateCode}' not found, skipping {newCode}");
                return;
            }

            // 1. Clone the prefab
            var clonedPrefab = UnityEngine.Object.Instantiate(template.prefab);
            clonedPrefab.name = newCode;
            UnityEngine.Object.DontDestroyOnLoad(clonedPrefab);
            clonedPrefab.SetActive(false);

            // 2. Try loading custom mesh from OBJ file
            bool hasMesh = false;
            ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Mesh file: {meshFile}, exists={File.Exists(meshFile)}");
            if (File.Exists(meshFile)) {
                Mesh customMesh = ObjLoader.LoadObj(meshFile);
                if (customMesh != null) {
                    // Get the meshBase (the visual root of the building)
                    var bldgComp = clonedPrefab.GetComponent<Building>();
                    GameObject meshBaseObj = bldgComp?.meshBase ?? clonedPrefab;

                    // CAPTURE a material from existing renderers BEFORE destroying them
                    Material capturedMat = null;
                    string capturedShaderName = "Standard";
                    var existingRenderers = meshBaseObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in existingRenderers) {
                        if (r.sharedMaterial != null) {
                            capturedMat = r.sharedMaterial;
                            capturedShaderName = capturedMat.shader.name;
                            break;
                        }
                    }
                    ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Captured shader: {capturedShaderName}, renderers: {existingRenderers.Length}");

                    // DESTROY ALL children under meshBase - the game re-enables via SetObActive
                    // so we must remove them entirely. Iterate in reverse to avoid index shifting.
                    int destroyedCount = 0;
                    for (int i = meshBaseObj.transform.childCount - 1; i >= 0; i--) {
                        var child = meshBaseObj.transform.GetChild(i).gameObject;
                        UnityEngine.Object.DestroyImmediate(child);
                        destroyedCount++;
                    }
                    ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Destroyed {destroyedCount} children from '{meshBaseObj.name}'");

                    // Also destroy any MeshRenderer/MeshFilter directly on meshBase itself
                    var directMF = meshBaseObj.GetComponent<MeshFilter>();
                    var directMR = meshBaseObj.GetComponent<MeshRenderer>();
                    if (directMF != null) UnityEngine.Object.DestroyImmediate(directMF);
                    if (directMR != null) UnityEngine.Object.DestroyImmediate(directMR);

                    // Create new child with custom mesh, SCALED to building size
                    // OBJ meshes are ~2 Unity units wide; buildings need to be much bigger
                    var customObj = new GameObject("CustomMesh");
                    customObj.transform.SetParent(meshBaseObj.transform, false);
                    customObj.transform.localPosition = Vector3.zero;
                    customObj.transform.localRotation = Quaternion.identity;
                    // scaleMult is the intended building scale (e.g. 1.2 = 20% bigger than template)
                    // We need a base scale factor to bring the ~2-unit OBJ up to building size
                    customObj.transform.localScale = Vector3.one * scaleMult;

                    var mf = customObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = customMesh;

                    var mr = customObj.AddComponent<MeshRenderer>();
                    // Use the captured game material as a base (correct shader + rendering mode)
                    if (capturedMat != null) {
                        mr.sharedMaterial = new Material(capturedMat);
                    } else {
                        mr.sharedMaterial = new Material(Shader.Find("Standard"));
                    }
                    mr.sharedMaterial.SetColor("_Color", tintColor);
                    if (mr.sharedMaterial.HasProperty("_EmissionColor")) {
                        mr.sharedMaterial.SetColor("_EmissionColor", emissiveColor);
                        mr.sharedMaterial.EnableKeyword("_EMISSION");
                    }
                    mr.enabled = true;

                    hasMesh = true;
                    ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Created CustomMesh child under '{meshBaseObj.name}' " +
                        $"verts={customMesh.vertexCount} scale={scaleMult} shader={mr.sharedMaterial.shader.name}");
                }
            }

            // 3. Apply visual styling only for template mesh fallback (custom mesh already styled above)
            if (!hasMesh) {
                foreach (var r in clonedPrefab.GetComponentsInChildren<MeshRenderer>(true)) {
                    if (r.material != null) {
                        r.material = new Material(r.material);
                        r.material.SetColor("_Color", tintColor);
                        if (r.material.HasProperty("_EmissionColor")) {
                            r.material.SetColor("_EmissionColor", emissiveColor);
                            r.material.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }

            // Scale the mesh base (only for template mesh fallback, custom mesh already scaled)
            if (!hasMesh) {
                var bldg = clonedPrefab.GetComponent<Building>();
                if (bldg?.meshBase != null)
                    bldg.meshBase.transform.localScale *= scaleMult;
                else
                    clonedPrefab.transform.localScale *= scaleMult;
            }

            // 4. Create BuildingData
            var newData = new BuildingData {
                code = newCode,
                prefab = clonedPrefab,
                title = newCode + "_TITLE",
                description = newCode + "_DESC",
                group = buildGroup,
                inBuildMenu = true,
                showOrder = template.showOrder + 100,
                maxBuildCount = 0,
                noDemolish = false,
                baseCosts = PickupCost.ParseList(buildCost),
                recipes = new List<string>(),
                autoRecipe = false,
                parentBuilding = "",
                titleParent = "",
                inDemo = false,
            };

            PrefabData.buildings.Add(newData);

            // 5. Force BuildingData dictionary rebuild
            var dicField = AccessTools.Field(typeof(BuildingData), "dicBuildingData");
            dicField?.SetValue(null, null);

            string meshStatus = hasMesh ? "custom mesh" : "template mesh (recolored)";
            ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Registered {newCode} ({meshStatus})");
        }
    }

    // Trigger building injection when BuildingData.Get is first called
    // (guarantees PrefabData.buildings is populated)
    [HarmonyPatch(typeof(BuildingData), "Get", new Type[] { typeof(string) })]
    public static class BuildingDataGetPatch {
        [HarmonyPrefix]
        static void Prefix() {
            CustomBuildingInjector.EnsureInjected();
        }
    }

    // Localization for custom building names
    [HarmonyPatch(typeof(Loc), "GetObject")]
    public static class CustomBuildingLocPatch {
        static readonly Dictionary<string, string> modStrings = new() {
            { "MOD_LIQUID_SMELTER_TITLE",  "Liquid Smelter" },
            { "MOD_LIQUID_SMELTER_DESC",   "Melts iron and copper ore into liquid metal for advanced manufacturing." },
            { "MOD_ASSEMBLER_TITLE",       "High-End Assembler" },
            { "MOD_ASSEMBLER_DESC",        "Crafts advanced components: circuit boards, compute units, and alloy frames." },
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
    // PHASE 2: DEEP EXCAVATOR — Spire-based resource regeneration
    // A MonoBehaviour attached to the Colony Spire that periodically
    // consumes energy (from island batteries) + excavation cores to
    // spawn new resource deposits on the island.
    // ================================================================
    public class DeepExcavatorBehavior : MonoBehaviour {
        public Building spireBuilding;
        private float timer = 0f;
        private float statusTimer = 0f;

        void Update() {
            if (spireBuilding == null) return;
            if (GameManager.instance == null || GameManager.instance.GetStatus() != GameStatus.RUNNING) return;
            if (ModState.prestigeLevel < 1) return; // Need at least prestige 1 to activate

            float dt = Time.deltaTime;
            timer += dt;
            statusTimer += dt;

            // Log status every 30 seconds
            if (statusTimer >= 30f) {
                statusTimer = 0f;
                Debug.Log($"[Spire/Excavator] Status: Cores={ModState.excavationCores} Timer={timer:F0}/{ModState.EXCAVATOR_INTERVAL:F0}s");
            }

            if (timer < ModState.EXCAVATOR_INTERVAL) return;
            timer = 0f;

            // Check if we have enough excavation cores
            if (ModState.excavationCores < ModState.EXCAVATOR_CORE_COST) {
                Debug.Log($"[Spire/Excavator] Not enough cores ({ModState.excavationCores}/{ModState.EXCAVATOR_CORE_COST})");
                return;
            }

            // Check if island has enough energy
            if (spireBuilding.ground == null) return;
            float energyAvailable = spireBuilding.ground.GetEnergy(ModState.EXCAVATOR_ENERGY_COST);
            if (energyAvailable < ModState.EXCAVATOR_ENERGY_COST - 1f) {
                Debug.Log($"[Spire/Excavator] Not enough island energy ({energyAvailable:F0}/{ModState.EXCAVATOR_ENERGY_COST:F0})");
                return;
            }

            // Consume cores
            ModState.excavationCores -= ModState.EXCAVATOR_CORE_COST;

            // Try to spawn a resource deposit on this island
            SpawnResourceDeposit();
            ModSave.Save();
        }

        private void SpawnResourceDeposit() {
            if (spireBuilding.ground == null) return;
            Ground ground = spireBuilding.ground;

            // Pick a random resource type
            string[] candidates = ModState.MineableResources;
            string code = candidates[UnityEngine.Random.Range(0, candidates.Length)];

            // Check if BiomeObjectData exists for this code
            BiomeObjectData bobData = null;
            try { bobData = BiomeObjectData.Get(code); } catch { }
            if (bobData == null || bobData.prefab == null) {
                // Fallback: try a simpler code
                Debug.LogWarning($"[Spire/Excavator] BiomeObjectData not found for '{code}', trying BOB_STONE");
                code = "BOB_STONE";
                try { bobData = BiomeObjectData.Get(code); } catch { }
                if (bobData == null || bobData.prefab == null) {
                    Debug.LogError("[Spire/Excavator] Cannot find any valid BiomeObjectData! Aborting.");
                    return;
                }
            }

            // Find a random spawn position on the island
            Vector3 spawnPos = ground.transform.position;
            for (int attempt = 0; attempt < 10; attempt++) {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 20f;
                offset.y = 0;
                Vector3 candidatePos = ground.transform.position + offset;

                // Raycast downward to find ground surface
                if (Physics.Raycast(candidatePos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f)) {
                    spawnPos = hit.point;
                    break;
                }
            }

            // Spawn the resource node
            float size = UnityEngine.Random.Range(0.8f, 1.5f);
            try {
                var bob = GameManager.instance.SpawnBiomeObject(
                    code, ground, spawnPos, UnityEngine.Random.rotation,
                    ground.transform, size);
                
                if (bob != null) {
                    Debug.Log($"[Spire/Excavator] ★ Spawned {code} at {spawnPos} (size {size:F1})");
                    
                    // Visual celebration
                    for (int i = 0; i < 3; i++) {
                        var offset = UnityEngine.Random.insideUnitSphere * 2f;
                        offset.y = Mathf.Abs(offset.y) + 1f;
                        GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF5, spawnPos + offset);
                    }
                }
            } catch (Exception ex) {
                Debug.LogError($"[Spire/Excavator] Failed to spawn {code}: {ex.Message}");
            }
        }
    }

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

    // Part A: Fix the sort to use an absolute reference direction
    [HarmonyPatch(typeof(Split), "UpdateDividerTrails")]
    public static class DividerSortFixPatch {
        [HarmonyPostfix]
        static void Postfix(Split __instance) {
            try {
                var dividerTrailsField = AccessTools.Field(typeof(Split), "dividerTrails");
                if (dividerTrailsField == null) return;
                var dividerTrails = dividerTrailsField.GetValue(__instance) as List<Trail>;
                if (dividerTrails == null || dividerTrails.Count <= 1) return;

                // Re-sort using Vector3.forward as the absolute reference direction
                // instead of the first trail's direction (which depends on insertion order)
                Vector3 refDir = Vector3.forward;
                dividerTrails.Sort((Trail a, Trail b) => {
                    float angleA = CalculateClockAngle(refDir, a.direction);
                    float angleB = CalculateClockAngle(refDir, b.direction);
                    return angleA.CompareTo(angleB);
                });
            } catch (Exception ex) {
                Debug.LogError($"[Spire/DividerFix] Sort fix failed: {ex.Message}");
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

    // Part C: After loading and Init, translate the encoded linkId back to
    // the correct index in the rebuilt dividerTrails list.
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
                if (dividerTrails == null || dividerTrails.Count == 0) return;

                // Check if this is our encoded linkId (offset >= 100000)
                if (dividerI >= 100000) {
                    int savedLinkId = dividerI - 100000;
                    // Find the trail with this linkId in the sorted dividerTrails
                    int newIndex = -1;
                    for (int i = 0; i < dividerTrails.Count; i++) {
                        if (dividerTrails[i].linkId == savedLinkId) {
                            newIndex = i;
                            break;
                        }
                    }
                    if (newIndex >= 0) {
                        dividerIField.SetValue(__instance, newIndex);
                    } else {
                        // Couldn't find the trail — fall back to 0
                        Debug.LogWarning($"[Spire/DividerFix] Could not find trail with linkId {savedLinkId}, resetting dividerI to 0");
                        dividerIField.SetValue(__instance, 0);
                    }
                }
                // else: loading a save from before the mod, or vanilla save — leave as-is
                // (our Sort fix in Part A already makes the list order deterministic,
                // which helps even without the linkId encoding)

                // Update the pointer visual to match the corrected dividerI
                var updatePointer = AccessTools.Method(typeof(Split), "UpdatePointer");
                updatePointer?.Invoke(__instance, new object[] { true });
            } catch (Exception ex) {
                Debug.LogError($"[Spire/DividerFix] Load fix failed: {ex.Message}");
            }
        }
    }
}
