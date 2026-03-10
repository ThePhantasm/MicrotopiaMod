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
                // Phase 12: Energy Efficiency
                typeof(EnergyDrainPatch),
            };
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
        public static int displayTier = 1;
        public static ConditionalWeakTable<Queen, QueenData> queenData = new();
        public static QueenData GetQueen(Queen q) => queenData.GetOrCreateValue(q);

        // Sentinel hatching state
        public static float sentinelHatchTimer = -1f;  // -1 = idle; >0 = hatching
        public static RadarIslandScanner sentinelSpire = null;  // which spire is hatching
        public const float SENTINEL_HATCH_TIME = 30f;

        public static readonly string[] TrackNames = { "Speed", "Queen", "Mine", "Mold", "Wings", "Sentinel", "Energy" };
        public static readonly string[] TrackCodes = {
            "SPIRE_SPEED", "SPIRE_QUEEN", "SPIRE_MINE", "SPIRE_MOLD", "SPIRE_WINGS", "SPIRE_SENTINEL", "SPIRE_ENERGY"
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
        const string KEY_SPIRE_TRACK = "CSP_SpireTrack";  // selected upgrade track

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
            if (queenTier >= 1) PlayerPrefs.SetInt(KEY_QUEENTIER, queenTier);
            PlayerPrefs.Save();
            Debug.Log($"[Spire] Saved — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Q{ModState.royalMandateLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel}");
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
            Debug.Log($"[Spire] Loaded — P{ModState.prestigeLevel} Spd{ModState.pheromoneLevel} Sentinel×{ModState.sentinelHatched} E{ModState.energyLevel}");
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
}
