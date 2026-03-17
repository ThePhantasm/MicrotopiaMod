using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ColonySpireMod
{
    [BepInPlugin("com.colonyspire.mod", "Colony Spire Mod", "1.0.0")]
    public class ColonySpirePlugin : BaseUnityPlugin
    {
        void Awake()
        {
            Logger.LogInfo("Colony Spire Mod loading...");
            var harmony = new Harmony("com.colonyspire.mod");
            var patchClasses = new[] {
                typeof(PrestigePatch),
                typeof(SpeedPatch),
                // Queen
                typeof(QueenBuildingUpdatePatch),
                typeof(QueenSetClickUiPatch),
                typeof(QueenInitPatch),
                typeof(SpawnPickupPatch),            // remap T1 larva -> T2/T3 based on queen tier
                // Colony Spire (RadarIslandScanner prefab via updated fods)
                typeof(SpireInitPatch),
                typeof(SpireBuildingUpdatePatch),
                typeof(SpireClickTypePatch),
                // Unlocker hooks — intercept chooser without constructing UnlockRecipeData
                typeof(UnlockerSetUnlockPatch),
                typeof(UnlockerPickUnlockPatch),     // cycle our 6 tracks instead of TechTree lookup
                typeof(UnlockerAnythingToUnlockPatch),
                typeof(UnlockerGetAvailableCountPatch),
                typeof(UnlockerEAvailablePatch),
                typeof(UnlockerCanInsertPatch),
                typeof(UnlockerDoUnlockPatch),
                typeof(UnlockerGatherProgressPatch),
                typeof(SetUnlockNamePatch),
                typeof(SetChangeButtonOverridePatch),  // bypass UIRecipeMenu popup; just cycle tracks
                typeof(SaveOnWritePatch),               // persist mod state across sessions
                typeof(AutoUnlockPatch),
                // Phase 6: Mold Resistance
                typeof(MoldResistancePatch),
                // Phase 7: Mining Speed
                typeof(MiningSpeedPatch),
                // Phase 8: Wing Carry Capacity
                typeof(WingCarryPatch),
                // HUD: larvae/min bar shows tier-adjusted rate + larva type label
                typeof(LarvaRateHudPatch),
                // Phase 11: Gatherer Speed
                typeof(GathererDelayPatch),
                // Phase 12: Energy Efficiency
                // typeof(EnergyDrainPatch),
                // Island Scale
                typeof(GroundInitShapePatch),
                typeof(GroundCreatePatch),
                typeof(UISettingsWorldPatch),
                typeof(UIWorldSettingsInitPatch),
                // Concrete Island Combat
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
                // Trail Color Customization
                typeof(TrailResetMaterialPatch),
                typeof(MainBusColorHotkeyPatch),
            };
            
            // Load settings early so scale defaults are ready
            ModSave.Load();

            foreach (var t in patchClasses)
            {
                try { harmony.CreateClassProcessor(t).Patch(); Logger.LogInfo($"[OK] {t.Name}"); }
                catch (Exception ex) { Logger.LogError($"[FAIL] {t.Name}: {ex.Message}"); }
            }
            Logger.LogInfo("Colony Spire Mod loaded!");
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
        public static float islandScale = 1.0f; // Scale modifier for the first island
        public static ConditionalWeakTable<Queen, QueenData> queenData = new();
        public static QueenData GetQueen(Queen q) => queenData.GetOrCreateValue(q);

        // Sentinel hatching state
        public static float sentinelHatchTimer = -1f;  // -1 = idle; >0 = hatching
        public static RadarIslandScanner sentinelSpire = null;  // which spire is hatching
        public const float SENTINEL_HATCH_TIME = 30f;

        // ----------------------------------------------------------------
        // MAIN BUS TRAIL COLOR — customizable color for TrailType.MAIN
        // Index 0 = vanilla (white), 1-9 = bright color presets
        // ----------------------------------------------------------------
        public static int mainBusColorIndex = 0;
        public static readonly (string name, Color color, Color emission)[] MainBusColors = {
            ("White (Default)",  new Color(1.0f, 1.0f, 1.0f, 1f),    new Color(2.0f, 2.0f, 2.0f, 1f)),     // 0: vanilla
            ("Cyan",             new Color(0.0f, 1.0f, 1.0f, 1f),    new Color(0.0f, 2.0f, 2.0f, 1f)),     // 1
            ("Magenta",          new Color(1.0f, 0.0f, 1.0f, 1f),    new Color(2.0f, 0.0f, 2.0f, 1f)),     // 2
            ("Lime",             new Color(0.2f, 1.0f, 0.0f, 1f),    new Color(0.4f, 2.0f, 0.0f, 1f)),     // 3
            ("Orange",           new Color(1.0f, 0.5f, 0.0f, 1f),    new Color(2.0f, 1.0f, 0.0f, 1f)),     // 4
            ("Hot Pink",         new Color(1.0f, 0.0f, 0.5f, 1f),    new Color(2.0f, 0.0f, 1.0f, 1f)),     // 5
            ("Electric Blue",    new Color(0.0f, 0.5f, 1.0f, 1f),    new Color(0.0f, 1.0f, 2.0f, 1f)),     // 6
            ("Gold",             new Color(1.0f, 0.85f, 0.0f, 1f),   new Color(2.0f, 1.7f, 0.0f, 1f)),     // 7
            ("Spring Green",     new Color(0.0f, 1.0f, 0.5f, 1f),    new Color(0.0f, 2.0f, 1.0f, 1f)),     // 8
            ("Red",              new Color(1.0f, 0.15f, 0.1f, 1f),   new Color(2.0f, 0.3f, 0.2f, 1f)),     // 9
        };
        public static (string name, Color color, Color emission) GetMainBusColor() =>
            MainBusColors[Math.Max(0, Math.Min(mainBusColorIndex, MainBusColors.Length - 1))];
        public static void CycleMainBusColor() {
            mainBusColorIndex = (mainBusColorIndex + 1) % MainBusColors.Length;
            ModSave.SaveMainBusColor();
            RefreshAllMainBusTrails();
            var c = GetMainBusColor();
            Debug.Log($"[Spire] Main Bus color -> {c.name} (index {mainBusColorIndex})");
        }
        public static void RefreshAllMainBusTrails() {
            // Force all placed Main Bus trails to re-apply their material
            try {
                var allTrailsField = AccessTools.Field(typeof(GameManager), "allTrails");
                if (allTrailsField != null) {
                    var allTrails = allTrailsField.GetValue(GameManager.instance);
                    if (allTrails is System.Collections.IEnumerable enumerable) {
                        foreach (var obj in enumerable) {
                            if (obj is Trail trail && trail.trailType == TrailType.MAIN && trail.IsPlaced())
                                trail.ResetMaterial();
                        }
                    }
                }
                // Also try the placed splits which hold trail references
                var allSplitsField = AccessTools.Field(typeof(GameManager), "allSplits");
                if (allSplitsField != null) {
                    var allSplits = allSplitsField.GetValue(GameManager.instance);
                    if (allSplits is System.Collections.IEnumerable splitEnum) {
                        foreach (var obj in splitEnum) {
                            if (obj is Split split) {
                                foreach (var trail in split.connectedTrails) {
                                    if (trail != null && trail.trailType == TrailType.MAIN && trail.IsPlaced())
                                        trail.ResetMaterial();
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) { Debug.Log($"[Spire] RefreshMainBus: {ex.Message}"); }
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

        public static void SaveSettings() {
            PlayerPrefs.SetFloat(KEY_ISLAND_SCALE, ModState.islandScale);
            PlayerPrefs.SetInt(KEY_MAINBUS_COLOR, ModState.mainBusColorIndex);
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
            if (queenTier >= 1) PlayerPrefs.SetInt(KEY_QUEENTIER, queenTier);
            PlayerPrefs.Save();
            Debug.Log($"[Spire] Saved — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Q{ModState.royalMandateLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel} G{ModState.gathererLevel}");
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
            ModState.islandScale       = PlayerPrefs.GetFloat(KEY_ISLAND_SCALE, 1.0f);
            ModState.mainBusColorIndex = PlayerPrefs.GetInt(KEY_MAINBUS_COLOR, 0);
            Debug.Log($"[Spire] Loaded — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel} G{ModState.gathererLevel} Scale={ModState.islandScale} BusColor={ModState.mainBusColorIndex}");
        }

        public static int LoadQueenTier() => PlayerPrefs.GetInt(KEY_QUEENTIER, 1);
    }

    // ================================================================
    // PRESTIGE + SPEED
    // ================================================================
    [HarmonyPatch(typeof(GyneTower), "StartGyne")]
    public static class PrestigePatch {
        [HarmonyPostfix] static void Postfix() { ModState.prestigeLevel++; }
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
            activeTier = qd.larvaOutputTier; // expose to SpawnPickupPatch
            ModState.displayTier = qd.larvaOutputTier;
            if (Input.GetKeyDown(KeyCode.G)) {
                qd.larvaOutputTier = (qd.larvaOutputTier % 3) + 1;
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
            __0.SetTitle(__instance.data.GetTitle() + $" [T{qd.larvaOutputTier}] ★{ModState.prestigeLevel}");
            try {
                var btn = __0.GetButton((UIClickButtonType)50, false);
                if (btn != null) {
                    btn.SetButton(() => {
                        qd.larvaOutputTier = (qd.larvaOutputTier % 3) + 1;
                        ModSave.Save(qd.larvaOutputTier); // persist queen tier
                        __0.SetTitle(__instance.data.GetTitle() + $" [T{qd.larvaOutputTier}] ★{ModState.prestigeLevel}");
                        __0.UpdateButton((UIClickButtonType)50, true, Labels[qd.larvaOutputTier - 1], true);
                    }, (InputAction)0);
                }
                __0.UpdateButton((UIClickButtonType)50, true, Labels[qd.larvaOutputTier - 1], true);
            } catch (Exception ex) { Debug.Log($"[Spire] Queen btn: {ex.Message}"); }
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
                Progress.UnlockBuilding("COLONY_SPIRE", true);
                ModSave.Load();  // restore prestige + track levels
                Debug.Log("[Spire] Building auto-unlocked, state loaded");
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
            if (ModState.wingLevel <= 0 || _data == null) return;
            if (!_data.flying) return;
            var field = AccessTools.Field(typeof(Ant), "carryCapacity");
            int current = (int)field.GetValue(__instance);
            field.SetValue(__instance, current + ModState.wingLevel);
            Debug.Log($"[Spire] WingCarry: {_data.caste} carry {current} -> {current + ModState.wingLevel}");
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
            if (GameManager.instance.GetGroundCount() == 0 && ModState.islandScale != 1.0f) {
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
                    colliders[i].radius *= ModState.islandScale;
                    colliders[i].center *= ModState.islandScale;
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
                __result.transform.localScale = Vector3.one * ModState.islandScale;
                Debug.Log($"[Spire] Scaled initial island to {ModState.islandScale}");
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
                
                var blankSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                var sliderSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                var colorSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                
                SetupSlider(blankSetting, sliderSetting);
                SetupColorDropdown(colorSetting);
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

        public static void SetupColorDropdown(UISettings_Setting colorSetting) {
            if (colorSetting == null) return;
            try {
                colorSetting.InitDropdown(
                    "Main Bus Trail Color",  // header — will be overridden below
                    (List<string> items) => {
                        // Populate dropdown with all available color names
                        foreach (var (name, _, _) in ModState.MainBusColors)
                            items.Add(name);
                    },
                    () => ModState.mainBusColorIndex,  // getter: current index
                    (int index) => {                    // setter: on selection changed
                        ModState.mainBusColorIndex = Math.Max(0, Math.Min(index, ModState.MainBusColors.Length - 1));
                        ModSave.SaveMainBusColor();
                        ModState.RefreshAllMainBusTrails();
                        Debug.Log($"[Spire] Main Bus color -> {ModState.GetMainBusColor().name} (index {ModState.mainBusColorIndex})");
                    }
                );
                // Override the header text directly (the Loc key won't exist)
                var headerField = AccessTools.Field(typeof(UISettings_Setting), "headerText");
                if (headerField != null) {
                    var textObj = headerField.GetValue(colorSetting) as TMPro.TextMeshProUGUI;
                    if (textObj != null) textObj.text = "Main Bus Trail Color";
                }
            } catch (Exception ex) { Debug.Log($"[Spire] ColorDropdown: {ex.Message}"); }
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
                var colorSetting = (UISettings_Setting)addSettingMethod.Invoke(__instance, new object[0]);
                
                UISettingsWorldPatch.SetupSlider(blankSetting, sliderSetting);
                UISettingsWorldPatch.SetupColorDropdown(colorSetting);
            } catch (Exception ex) {
                Debug.Log($"[Spire] UIWorldSettings exception: {ex.Message}");
            }
        }
    }

    // ================================================================
    // MAIN BUS TRAIL COLOR CUSTOMIZATION
    // ================================================================

    // Postfix on Trail.ResetMaterial — after vanilla sets the material, we
    // override the color properties for MAIN trails to the user's chosen color.
    [HarmonyPatch(typeof(Trail), "ResetMaterial")]
    public static class TrailResetMaterialPatch {
        [HarmonyPostfix]
        static void Postfix(Trail __instance) {
            if (ModState.mainBusColorIndex <= 0) return; // 0 = vanilla, skip
            if (__instance.trailType != TrailType.MAIN) return;
            try {
                var (name, color, emission) = ModState.GetMainBusColor();
                // Get the active TrailShapeObject and override its rendered material colors
                var curShapeField = AccessTools.Field(typeof(Trail), "curTrailShapeObject");
                if (curShapeField == null) return;
                var curShape = curShapeField.GetValue(__instance) as TrailShapeObject;
                if (curShape == null) return;

                // For useLineMesh shapes (THICK = the Main Bus default shape),
                // the material is set on quadRenderer and quadRendererArrow via
                // MaterialLibrary.GetTrailMaterial which creates a new Material clone
                // keyed by (rend_nr, _Color, _EmissionColor, offset).
                // We override the material's color properties directly.
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
                // Also override non-useLineMesh renderers (rends/rendsShaded)
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

    // Global hotkey: press V during gameplay to cycle Main Bus color
    [HarmonyPatch(typeof(Gameplay), "Update")]
    public static class MainBusColorHotkeyPatch {
        [HarmonyPostfix]
        static void Postfix() {
            if (Input.GetKeyDown(KeyCode.V)) {
                // Don't cycle if we're in a UI text field or similar
                if (GameManager.instance != null && GameManager.instance.GetStatus() == GameStatus.RUNNING) {
                    ModState.CycleMainBusColor();
                }
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
                
                var dropDisplay = new Dictionary<PickupType, string>
                {
                    { (PickupType)334, "~10" }, // Microchip
                    { (PickupType)327, "~10" }, // Wafer
                    { (PickupType)329, "~10" }, // Biofuel
                    { (PickupType)328, "~10" }  // Royal Jelly
                };
                ui_hover.inventoryGrid.Update("Mega Pinata Rewards", dropDisplay, "");
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
                    var dropDisplay = new Dictionary<PickupType, string>
                    {
                        { (PickupType)334, "~10" }, // Microchip
                        { (PickupType)327, "~10" }, // Wafer
                        { (PickupType)329, "~10" }, // Biofuel
                        { (PickupType)328, "~10" }  // Royal Jelly
                    };
                    clBiome.inventoryGrid.Update("Mega Pinata Rewards", dropDisplay, "");
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
                                // Debug.Log($"[Spire/Corpse] Melee Hit from Ant! Health: {data.health}/{data.maxHealth}");
                                
                                // Visual impact effect at the strike location
                                GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1, _pickup.transform.position);

                                if (data.health <= 0 && !data.isDead)
                                {
                                    data.isDead = true;
                                    Debug.Log("[Spire/Corpse] CORPSE DESTROYED! Mega Pinata imminent!");
                                    ModSpawners.SpawnMegaPinata(corpse.transform.position);
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
}
