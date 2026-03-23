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
                    bob.data.infinite = true;
                }
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
                dome.transform.localPosition = new Vector3(0, 0f, 0);
                dome.transform.localScale = new Vector3(90f, 90f, 90f);
                
                var collider = dome.GetComponent<Collider>();
                if (collider != null) GameObject.Destroy(collider);
                
                var renderer = dome.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.2f, 0.6f, 1f, 0.15f);
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
            if (tower.IsPlaced() && tower.currentStatus == BuildingStatus.COMPLETED && tower.ground.GetEnergy(50f) >= 49f)
            {
                if (meshRenderer != null) meshRenderer.enabled = true;
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

    [HarmonyPatch(typeof(BiomeObject), "GetMineDuration")]
    public static class CorpseMineDurationPatch
    {
        [HarmonyPrefix]
        static bool Prefix(BiomeObject __instance, float mine_speed, ref float __result)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                RobotCorpseManager.EnsureBehavior(__instance);
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
                (PickupType)334, (PickupType)327, (PickupType)329, (PickupType)328
            };

            int dropCount = 40;
            for (int i = 0; i < dropCount; i++)
            {
                PickupType drop = drops[UnityEngine.Random.Range(0, drops.Length)];
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 2f;
                offset.y = Mathf.Abs(offset.y) + 1f;

                try {
                    Pickup p = GameManager.instance.SpawnPickup(drop, center + offset, UnityEngine.Random.rotation);
                    if (p != null) {
                        Rigidbody rb = p.GetComponent<Rigidbody>();
                        if (rb != null) {
                            rb.isKinematic = false;
                            rb.AddExplosionForce(500f, center, 10f);
                        }
                    }
                } catch { }
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
                                GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1, _pickup.transform.position);

                                if (data.health <= 0 && !data.isDead)
                                {
                                    data.isDead = true;
                                    float radius = 0f;
                                    try { radius = corpse.GetRadius(); } catch { }
                                    int coreReward = radius > 20f ? 10 : radius > 10f ? 3 : 1;
                                    ModState.excavationCores += coreReward;
                                    Debug.Log($"[Spire/Corpse] CORPSE DESTROYED! +{coreReward} Excavation Cores (total: {ModState.excavationCores})");

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

                            _pickup.Delete();
                            
                            var queueField = AccessTools.Field(typeof(Ant), "nextActionPoints");
                            if (queueField != null)
                            {
                                var queue = queueField.GetValue(__instance) as Queue<ActionPoint>;
                                if (queue != null && !queue.Contains(action))
                                {
                                    queue.Enqueue(action);
                                }
                            }

                            __result = 1f;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BiomeObject), "Init")]
    public static class CorpseInitPatch
    {
        [HarmonyPostfix]
        static void Postfix(BiomeObject __instance)
        {
            if (RobotCorpseManager.IsRobotCorpse(__instance))
            {
                __instance.data.infinite = true;
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
            
            regenTimer += dt;
            if (regenTimer >= 1f)
            {
                regenTimer -= 1f;
                if (data.health < data.maxHealth)
                {
                    data.health = Mathf.Min(data.health + 150f, data.maxHealth); 
                }
            }
            
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
                    if (field != null) zapPrefabCache = field.GetValue(e) as GameObject;
                }
            }
            return zapPrefabCache;
        }

        private void PerformAoEZap()
        {
            float zapRadius = biomeObject.GetRadius() + 6f;
            int maxTargets = 5;
            int targetsHit = 0;
            
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
                    bool shielded = false;
                    foreach (Building tower in GameManager.instance.EBuildings("BUILD_RADAR_TOWER"))
                    {
                        if (Vector3.Distance(ant.transform.position, tower.transform.position) <= 45f)
                        {
                            if (tower.ground != null && tower.ground.GetEnergy(50f) >= 49f)
                            {
                                shielded = true;
                                GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF3, tower.transform.position + Vector3.up * 4f);
                                break;
                            }
                        }
                    }

                    if (shielded)
                    {
                        ant.energy -= 12f;
                        GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF5, ant.transform.position);
                    }
                    else
                    {
                        ant.energy -= 360f;
                        GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1, ant.transform.position);
                    }
                    
                    if (ant.energy <= 0) ant.Die(DeathCause.OLD_AGE);
                    
                    if (prefab != null)
                    {
                        var gobj = Instantiate(prefab, null);
                        var lr = gobj.GetComponent<LineRenderer>();
                        if (lr != null)
                        {
                            lr.positionCount = 2;
                            lr.SetPosition(0, transform.position + Vector3.up * 10f);
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
        
        void Awake() { lr = GetComponent<LineRenderer>(); }
        
        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (lr != null)
            {
                float a = life / 0.5f;
                lr.startWidth = a * 0.5f;
                lr.endWidth = a * 0.5f;
            }
        }
    }

    // ================================================================
    // ISLAND FURNACE — Battery accepts ANY material
    // ================================================================
    [HarmonyPatch(typeof(BatteryBuilding), "CanInsert_Intake")]
    public static class FurnaceCanInsertPatch {
        [HarmonyPrefix]
        static bool Prefix(BatteryBuilding __instance, PickupType _type, ExchangeType exchange,
            ref bool let_ant_wait, ref bool __result) {
            if (!ModState.furnaceEnabled) return true;
            if (exchange != ExchangeType.BUILDING_IN) return true;
            var capField = AccessTools.Field(typeof(BatteryBuilding), "energyCapacity");
            float cap = (float)(capField?.GetValue(__instance) ?? 100f);
            if (__instance.storedEnergy < cap - 1f) {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BatteryBuilding), "OnPickupArrival_Intake")]
    public static class FurnaceOnArrivalPatch {
        [HarmonyPrefix]
        static bool Prefix(BatteryBuilding __instance, Pickup _pickup) {
            if (!ModState.furnaceEnabled) return true;
            if (_pickup == null) return true;
            if (_pickup.data.energyAmount > 0f) return true;

            var capField = AccessTools.Field(typeof(BatteryBuilding), "energyCapacity");
            float cap = (float)(capField?.GetValue(__instance) ?? 100f);
            
            var incomingField = AccessTools.Field(typeof(Building), "incomingPickups_intake");
            if (incomingField != null) {
                var incoming = incomingField.GetValue(__instance) as List<Pickup>;
                incoming?.Remove(_pickup);
            }

            __instance.storedEnergy = Mathf.Clamp(
                __instance.storedEnergy + ModState.FURNACE_ENERGY_PER_ITEM, 0f, cap);

            GameManager.instance.SpawnExplosion(ExplosionType.ENERGY_POOF1,
                _pickup.transform.position + Vector3.up * 0.5f);
            _pickup.Delete();

            AccessTools.Method(typeof(BatteryBuilding), "UpdateVisual")?.Invoke(__instance, null);
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildingData), "GetTitle")]
    public static class FurnaceTitlePatch {
        [HarmonyPostfix]
        static void Postfix(BuildingData __instance, ref string __result) {
            if (!ModState.furnaceEnabled) return;
            if (__instance.code != null && __instance.code.Contains("BATTERY")) {
                __result = "Island Furnace";
            }
        }
    }

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
    // OBJ MESH LOADER
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
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                try {
                    if (parts[0] == "v" && parts.Length >= 4) {
                        tempVerts.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                    }
                    else if (parts[0] == "vn" && parts.Length >= 4) {
                        tempNormals.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                    }
                    else if (parts[0] == "vt" && parts.Length >= 3) {
                        tempUVs.Add(new Vector2(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture)));
                    }
                    else if (parts[0] == "f") {
                        var faceIndices = new List<int>();
                        for (int i = 1; i < parts.Length; i++) {
                            string[] indices = parts[i].Split('/');
                            int vi = int.Parse(indices[0]) - 1;
                            int ui = indices.Length > 1 && indices[1] != "" ? int.Parse(indices[1]) - 1 : -1;
                            int ni = indices.Length > 2 && indices[2] != "" ? int.Parse(indices[2]) - 1 : -1;
                            int idx = faceVerts.Count;
                            faceVerts.Add(vi >= 0 && vi < tempVerts.Count ? tempVerts[vi] : Vector3.zero);
                            faceNorms.Add(ni >= 0 && ni < tempNormals.Count ? tempNormals[ni] : Vector3.up);
                            faceUVs.Add(ui >= 0 && ui < tempUVs.Count ? tempUVs[ui] : Vector2.zero);
                            faceIndices.Add(idx);
                        }
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
    // CUSTOM BUILDING INJECTION
    // ================================================================
    public static class CustomBuildingInjector {
        private static bool _injected = false;
        // Maps custom building prefab names → template prefab names for icon fallback
        public static readonly Dictionary<string, string> iconFallbackMap = new();

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

            InjectBuilding("SMELTER", "MOD_DRYING_RACK",
                Path.Combine(meshDir, "drying_rack.obj"),
                new Color(0.7f, 0.5f, 0.25f, 1f), new Color(0.3f, 0.2f, 0f, 1f),
                8.0f, "FIBER 10, RESIN 5, SCREW 5", BuildingGroup.PRODUCTION);

            InjectBuilding("SMELTER", "MOD_LIQUID_SMELTER",
                Path.Combine(meshDir, "liquid_smelter.obj"),
                new Color(1f, 0.4f, 0.1f, 1f), new Color(2f, 0.8f, 0f, 1f),
                10.0f, "IRON_BAR 8, COPPER_BAR 4", BuildingGroup.PRODUCTION);

            InjectBuilding("COMBINER", "MOD_ASSEMBLER",
                Path.Combine(meshDir, "assembler.obj"),
                new Color(0.2f, 0.5f, 1f, 1f), new Color(0f, 1f, 2f, 1f),
                10.0f, "IRON_BAR 12, MICROCHIP 3, COPPER_BAR 6", BuildingGroup.PRODUCTION);

            InjectRecipesIntoBuildings();
        }

        static void InjectRecipesIntoBuildings() {
            FactoryRecipeData.Get("");

            const string GROW_T4_CODE = "GROW_LARVAE_T4";
            if (!FactoryRecipeData.dicFactoryRecipe.ContainsKey(GROW_T4_CODE)) {
                var growT4 = new FactoryRecipeData {
                    code = GROW_T4_CODE,
                    title = "FACRECIPE_GROW_LARVAE_T4",
                    costsPickup = new List<PickupCost> { new PickupCost((PickupType)ModPickupTypes.LARVAE_T4, 1) },
                    costsAntOld = new List<AntCaste>(),
                    costsAnt = new List<AntCasteAmount>(),
                    productPickups = new List<PickupCost>(),
                    productAnts = new List<AntCasteAmount> { new AntCasteAmount((AntCaste)ModState.OMNI_ANT_CASTE_ID, 1) },
                    energyCost = 0f,
                    processTime = 20f,
                    alwaysUnlocked = true,
                    inDemo = false,
                    buildings = new List<string> { "MOD_ASSEMBLER" }
                };
                FactoryRecipeData.dicFactoryRecipe[GROW_T4_CODE] = growT4;
                PrefabData.factoryRecipes.Add(growT4);
                var assemblerData = BuildingData.Get("MOD_ASSEMBLER");
                if (assemblerData != null && !assemblerData.recipes.Contains(GROW_T4_CODE))
                    assemblerData.recipes.Add(GROW_T4_CODE);
                ColonySpirePlugin.Log.LogInfo($"[Spire/Recipes] Injected {GROW_T4_CODE} on MOD_ASSEMBLER");
            }

            string[] assemblerRecipes = { "ASSEMBLE_ALLOY", "ASSEMBLE_CIRCUIT", "ASSEMBLE_T4_LARVA" };
            foreach (var rCode in assemblerRecipes) {
                if (FactoryRecipeData.dicFactoryRecipe.ContainsKey(rCode)) {
                    var recipe = FactoryRecipeData.dicFactoryRecipe[rCode];
                    recipe.buildings.Remove("COMBINER2");
                    if (!recipe.buildings.Contains("MOD_ASSEMBLER")) recipe.buildings.Add("MOD_ASSEMBLER");
                    var assemblerData = BuildingData.Get("MOD_ASSEMBLER");
                    if (assemblerData != null && !assemblerData.recipes.Contains(rCode))
                        assemblerData.recipes.Add(rCode);
                }
            }

            string[] smeltRecipes = { "MOD_SMELT_IRON", "MOD_SMELT_COPPER" };
            foreach (var rCode in smeltRecipes) {
                if (FactoryRecipeData.dicFactoryRecipe.ContainsKey(rCode)) {
                    var recipe = FactoryRecipeData.dicFactoryRecipe[rCode];
                    if (!recipe.buildings.Contains("MOD_LIQUID_SMELTER")) recipe.buildings.Add("MOD_LIQUID_SMELTER");
                    var smelterData = BuildingData.Get("MOD_LIQUID_SMELTER");
                    if (smelterData != null && !smelterData.recipes.Contains(rCode))
                        smelterData.recipes.Add(rCode);
                }
            }

            string[] dryRecipes = { "DRY_FIBER" };
            foreach (var rCode in dryRecipes) {
                if (FactoryRecipeData.dicFactoryRecipe.ContainsKey(rCode)) {
                    var recipe = FactoryRecipeData.dicFactoryRecipe[rCode];
                    if (!recipe.buildings.Contains("MOD_DRYING_RACK")) recipe.buildings.Add("MOD_DRYING_RACK");
                    var rackData = BuildingData.Get("MOD_DRYING_RACK");
                    if (rackData != null && !rackData.recipes.Contains(rCode))
                        rackData.recipes.Add(rCode);
                }
            }
        }

        private static void InjectBuilding(string templateCode, string newCode,
            string meshFile, Color tintColor, Color emissiveColor,
            float scaleMult, string buildCost, BuildingGroup buildGroup) {

            foreach (var b in PrefabData.buildings)
                if (b.code == newCode) { ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] {newCode} already registered"); return; }

            BuildingData template = null;
            foreach (var b in PrefabData.buildings)
                if (b.code == templateCode) { template = b; break; }

            if (template == null) {
                ColonySpirePlugin.Log.LogWarning($"[Spire/Buildings] Template '{templateCode}' not found, skipping {newCode}");
                return;
            }

            var clonedPrefab = UnityEngine.Object.Instantiate(template.prefab);
            clonedPrefab.name = newCode;
            UnityEngine.Object.DontDestroyOnLoad(clonedPrefab);
            clonedPrefab.SetActive(false);

            bool hasMesh = false;
            if (File.Exists(meshFile)) {
                Mesh customMesh = ObjLoader.LoadObj(meshFile);
                if (customMesh != null) {
                    var bldgComp = clonedPrefab.GetComponent<Building>();
                    GameObject meshBaseObj = bldgComp?.meshBase ?? clonedPrefab;

                    Material capturedMat = null;
                    var existingRenderers = meshBaseObj.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in existingRenderers) {
                        if (r.sharedMaterial != null) { capturedMat = r.sharedMaterial; break; }
                    }

                    for (int i = meshBaseObj.transform.childCount - 1; i >= 0; i--)
                        UnityEngine.Object.DestroyImmediate(meshBaseObj.transform.GetChild(i).gameObject);

                    var directMF = meshBaseObj.GetComponent<MeshFilter>();
                    var directMR = meshBaseObj.GetComponent<MeshRenderer>();
                    if (directMF != null) UnityEngine.Object.DestroyImmediate(directMF);
                    if (directMR != null) UnityEngine.Object.DestroyImmediate(directMR);

                    var customObj = new GameObject("CustomMesh");
                    customObj.transform.SetParent(meshBaseObj.transform, false);
                    customObj.transform.localPosition = Vector3.zero;
                    customObj.transform.localRotation = Quaternion.identity;
                    customObj.transform.localScale = Vector3.one * scaleMult;

                    var mf = customObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = customMesh;
                    var mr = customObj.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = capturedMat != null ? new Material(capturedMat) : new Material(Shader.Find("Standard"));
                    mr.sharedMaterial.SetColor("_Color", tintColor);
                    if (mr.sharedMaterial.HasProperty("_EmissionColor")) {
                        mr.sharedMaterial.SetColor("_EmissionColor", emissiveColor);
                        mr.sharedMaterial.EnableKeyword("_EMISSION");
                    }
                    mr.enabled = true;
                    hasMesh = true;
                }
            }

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
                var bldg = clonedPrefab.GetComponent<Building>();
                if (bldg?.meshBase != null) bldg.meshBase.transform.localScale *= scaleMult;
                else clonedPrefab.transform.localScale *= scaleMult;
            }

            var newData = new BuildingData {
                code = newCode, prefab = clonedPrefab,
                title = newCode + "_TITLE", description = newCode + "_DESC",
                group = buildGroup, inBuildMenu = true,
                showOrder = template.showOrder + 100, maxBuildCount = 0,
                noDemolish = false, baseCosts = PickupCost.ParseList(buildCost),
                recipes = new List<string>(), autoRecipe = false,
                parentBuilding = "", titleParent = "", inDemo = false,
            };

            PrefabData.buildings.Add(newData);
            var dicField = AccessTools.Field(typeof(BuildingData), "dicBuildingData");
            dicField?.SetValue(null, null);

            // Register icon fallback: our custom prefab → template prefab name
            iconFallbackMap[newCode] = templateCode;
            ColonySpirePlugin.Log.LogInfo($"[Spire/Buildings] Icon fallback: {newCode} → {templateCode}");
        }
    }

    [HarmonyPatch(typeof(BuildingData), "Get", new Type[] { typeof(string) })]
    public static class BuildingDataGetPatch {
        [HarmonyPrefix]
        static void Prefix() { CustomBuildingInjector.EnsureInjected(); }
    }

    // Patch GetIcon() to return the template building's icon for our custom buildings
    [HarmonyPatch(typeof(BuildingData), "GetIcon")]
    public static class BuildingIconPatch {
        [HarmonyPostfix]
        static void Postfix(BuildingData __instance, ref Sprite __result) {
            // If vanilla found an icon, keep it
            if (__result != null) return;
            // Check if this is one of our custom buildings
            if (CustomBuildingInjector.iconFallbackMap.TryGetValue(__instance.code, out string templateCode)) {
                __result = Resources.Load<Sprite>("Building Icons/" + templateCode);
            }
        }
    }

    [HarmonyPatch(typeof(Loc), "GetObject")]
    public static class CustomBuildingLocPatch {
        static readonly Dictionary<string, string> modStrings = new() {
            { "MOD_DRYING_RACK_TITLE",     "Drying Rack" },
            { "MOD_DRYING_RACK_DESC",      "Hangs plant fibers to dry in the sun. Dried fiber burns hot enough to smelt metal." },
            { "MOD_LIQUID_SMELTER_TITLE",  "Liquid Smelter" },
            { "MOD_LIQUID_SMELTER_DESC",   "Melts iron and copper ore into liquid metal for advanced manufacturing." },
            { "MOD_ASSEMBLER_TITLE",       "High-End Assembler" },
            { "MOD_ASSEMBLER_DESC",        "Crafts advanced components: circuit boards, compute units, and alloy frames." },
        };

        [HarmonyPrefix]
        static bool Prefix(string code, ref string __result) {
            if (modStrings.TryGetValue(code, out var text)) { __result = text; return false; }
            return true;
        }
    }

    // ================================================================
    // DEEP EXCAVATOR
    // ================================================================
    public class DeepExcavatorBehavior : MonoBehaviour {
        public Building spireBuilding;
        private float timer = 0f;
        private float statusTimer = 0f;

        void Update() {
            if (spireBuilding == null) return;
            if (GameManager.instance == null || GameManager.instance.GetStatus() != GameStatus.RUNNING) return;
            if (!ModState.CanT4Endgame) return;

            float dt = Time.deltaTime;
            timer += dt;
            statusTimer += dt;

            if (statusTimer >= 30f) {
                statusTimer = 0f;
                Debug.Log($"[Spire/Excavator] Status: Cores={ModState.excavationCores} Timer={timer:F0}/{ModState.EXCAVATOR_INTERVAL:F0}s");
            }

            if (timer < ModState.EXCAVATOR_INTERVAL) return;
            timer = 0f;

            if (ModState.excavationCores < ModState.EXCAVATOR_CORE_COST) return;
            if (spireBuilding.ground == null) return;
            float energyAvailable = spireBuilding.ground.GetEnergy(ModState.EXCAVATOR_ENERGY_COST);
            if (energyAvailable < ModState.EXCAVATOR_ENERGY_COST - 1f) return;

            ModState.excavationCores -= ModState.EXCAVATOR_CORE_COST;
            SpawnResourceDeposit();
            ModSave.Save();
        }

        private void SpawnResourceDeposit() {
            if (spireBuilding.ground == null) return;
            Ground ground = spireBuilding.ground;
            string[] candidates = ModState.MineableResources;
            string code = candidates[UnityEngine.Random.Range(0, candidates.Length)];

            BiomeObjectData bobData = null;
            try { bobData = BiomeObjectData.Get(code); } catch { }
            if (bobData == null || bobData.prefab == null) {
                code = "BOB_STONE";
                try { bobData = BiomeObjectData.Get(code); } catch { }
                if (bobData == null || bobData.prefab == null) return;
            }

            Vector3 spawnPos = ground.transform.position;
            for (int attempt = 0; attempt < 10; attempt++) {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 20f;
                offset.y = 0;
                if (Physics.Raycast(ground.transform.position + offset + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f)) {
                    spawnPos = hit.point;
                    break;
                }
            }

            float size = UnityEngine.Random.Range(0.8f, 1.5f);
            try {
                var bob = GameManager.instance.SpawnBiomeObject(code, ground, spawnPos, UnityEngine.Random.rotation, ground.transform, size);
                if (bob != null) {
                    Debug.Log($"[Spire/Excavator] ★ Spawned {code} at {spawnPos} (size {size:F1})");
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
    // TECH TREE BOX INJECTOR
    // ================================================================
    [HarmonyPatch(typeof(UITechTreeTree), "Init")]
    public static class TechTreeBoxInjector {
        static readonly (string code, string prereq, Vector2 offset)[] modTechs = {
            ("MOD_QUEEN_T2",       "ANT_SMALLWORKER_T2",   new Vector2(120, -60)),
            ("MOD_QUEEN_T3",       "ANT_WORKERSMALL_T3",   new Vector2(120, -60)),
            ("MOD_COLORED_TRAILS", "TRAIL_MAIN",           new Vector2(120, 60)),
            ("MOD_COLONY_SPIRE",   "ANT_SENTINEL",         new Vector2(120, -60)),
            ("MOD_T4_ENDGAME",     "MOD_COLONY_SPIRE",     new Vector2(120, 0)),
        };

        static bool injected = false;

        [HarmonyPostfix]
        static void Postfix(UITechTreeTree __instance, TechTreeType _type, bool first_time, Action on_tech_unlock) {
            if (!first_time) return;
            if (injected) return;

            try {
                if (__instance.listBoxes.Count == 0) return;

                var existingBoxes = new Dictionary<string, UITechTreeBox>();
                foreach (var box in __instance.listBoxes) {
                    if (!string.IsNullOrEmpty(box.techCode) && !existingBoxes.ContainsKey(box.techCode))
                        existingBoxes[box.techCode] = box;
                }

                var dicBoxesField = AccessTools.Field(typeof(UITechTreeTree), "dicBoxes");
                var dicBoxes = dicBoxesField?.GetValue(__instance) as Dictionary<string, UITechTreeBox>;
                if (dicBoxes == null) {
                    dicBoxes = new Dictionary<string, UITechTreeBox>();
                    foreach (var b in __instance.listBoxes) {
                        if (!dicBoxes.ContainsKey(b.techCode)) dicBoxes[b.techCode] = b;
                    }
                    dicBoxesField?.SetValue(__instance, dicBoxes);
                }

                var template = __instance.listBoxes[__instance.listBoxes.Count - 1];

                foreach (var (code, prereq, offset) in modTechs) {
                    if (existingBoxes.ContainsKey(code) || dicBoxes.ContainsKey(code)) continue;
                    var tech = Tech.Get(code, "");
                    if (tech == null) continue;

                    UITechTreeBox prereqBox = null;
                    if (!existingBoxes.TryGetValue(prereq, out prereqBox))
                        dicBoxes.TryGetValue(prereq, out prereqBox);

                    var newBoxGO = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
                    var newBox = newBoxGO.GetComponent<UITechTreeBox>();
                    if (newBox == null) { UnityEngine.Object.Destroy(newBoxGO); continue; }

                    newBox.techCode = code;
                    newBox.requiredTechs = new List<string>();
                    if (tech.requiredTechs != null) newBox.requiredTechs.AddRange(tech.requiredTechs);

                    var floatField = AccessTools.Field(typeof(UITechTreeBox), "floatingAround");
                    floatField?.SetValue(newBox, false);

                    var rt = newBoxGO.GetComponent<RectTransform>();
                    if (rt != null && prereqBox != null) {
                        var prereqRT = prereqBox.GetComponent<RectTransform>();
                        if (prereqRT != null) rt.anchoredPosition = prereqRT.anchoredPosition + offset;
                    }

                    __instance.listBoxes.Add(newBox);
                    dicBoxes[code] = newBox;
                    existingBoxes[code] = newBox;

                    newBox.Init(delegate {
                        var t = Tech.Get(code, "");
                        if (t != null && t.GetStatus() == TechStatus.OPEN) {
                            newBox.DoOnClickVisual();
                            foreach (var cost in t.costs) Progress.RemoveInventorPoints(cost.type, cost.amount);
                            TechTree.GiveTech(t.code);
                            foreach (var lb in __instance.listBoxes) lb.SetInteractable();
                            newBox.UpdateBox();
                            on_tech_unlock();
                        }
                    });
                    newBox.UpdateBox();

                    if (__instance.prefabTechTreeLine != null) {
                        foreach (var reqTech in newBox.requiredTechs) {
                            if (dicBoxes.ContainsKey(reqTech)) {
                                var line = UnityEngine.Object.Instantiate(
                                    __instance.prefabTechTreeLine, __instance.lineParent
                                ).GetComponent<UITechTreeLine>();
                                line.SetObActive(true);
                                line.InitMaterial();
                                __instance.spawnedTechTreeLines.Add(line);
                            }
                        }
                    }

                    newBoxGO.SetActive(true);
                    Debug.Log($"[Spire] Injected tech tree box: {code}");
                }

                __instance.SetLinesProgress(instant: true, editor: false);
                injected = true;
            } catch (Exception ex) {
                Debug.LogError($"[Spire] TechTreeBoxInjector failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    // ================================================================
    // TECH TREE COLOR
    // ================================================================
    [HarmonyPatch(typeof(UITechTreeBoxShape), "UpdateBox", new Type[] { typeof(TechStatus) })]
    public static class TechTreeColorPatch {
        static readonly Color ColorT2      = new Color(0.55f, 0.40f, 1.0f, 1f);
        static readonly Color ColorT3      = new Color(1.0f,  0.85f, 0.2f, 1f);
        static readonly Color ColorGyneT1  = new Color(1.0f,  0.5f,  0.7f, 1f);
        static readonly Color ColorGyneT2  = new Color(0.9f,  0.2f,  0.6f, 1f);
        static readonly Color ColorGyneT3  = new Color(0.7f,  0.1f,  0.2f, 1f);

        [HarmonyPostfix]
        static void Postfix(UITechTreeBoxShape __instance, TechStatus _status) {
            try {
                if (_status == TechStatus.NONE) return;
                var box = __instance.GetComponentInParent<UITechTreeBox>();
                if (box == null) return;
                var techField = AccessTools.Field(typeof(UITechTreeBox), "tech");
                if (techField == null) return;
                var tech = techField.GetValue(box) as Tech;
                if (tech == null || tech.costs == null || tech.costs.Count == 0) return;

                int highestInventorTier = 0;
                int highestGyneTier = 0;
                foreach (var cost in tech.costs) {
                    if (cost.amount <= 0) continue;
                    switch (cost.type) {
                        case InventorPoints.REGULAR_T1: highestInventorTier = Math.Max(highestInventorTier, 1); break;
                        case InventorPoints.REGULAR_T2: highestInventorTier = Math.Max(highestInventorTier, 2); break;
                        case InventorPoints.REGULAR_T3: highestInventorTier = Math.Max(highestInventorTier, 3); break;
                        case InventorPoints.GYNE_T1:    highestGyneTier = Math.Max(highestGyneTier, 1); break;
                        case InventorPoints.GYNE_T2:    highestGyneTier = Math.Max(highestGyneTier, 2); break;
                        case InventorPoints.GYNE_T3:    highestGyneTier = Math.Max(highestGyneTier, 3); break;
                    }
                }

                Color? tintColor = null;
                if (highestGyneTier >= 3)       tintColor = ColorGyneT3;
                else if (highestGyneTier >= 2)  tintColor = ColorGyneT2;
                else if (highestGyneTier >= 1)  tintColor = ColorGyneT1;
                else if (highestInventorTier >= 3) tintColor = ColorT3;
                else if (highestInventorTier >= 2) tintColor = ColorT2;

                if (tintColor == null) return;

                var images = __instance.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images) {
                    float a = img.color.a;
                    if (a < 0.01f) continue;
                    img.color = new Color(tintColor.Value.r, tintColor.Value.g, tintColor.Value.b, a);
                }
            } catch (Exception ex) {
                Debug.Log($"[Spire] TechTreeColor: {ex.Message}");
            }
        }
    }

    // ================================================================
    // DYNAMO UPGRADE
    // ================================================================
    [HarmonyPatch(typeof(Dynamo), "UseBuilding")]
    public static class DynamoProductPatch {
        [HarmonyPrefix]
        static void Prefix(Dynamo __instance) {
            if (!ModState.CanT4Endgame) return;
            try {
                var productField = AccessTools.Field(typeof(Dynamo), "product");
                if (productField == null) return;
                productField.SetValue(__instance, (PickupType)111);
            } catch (Exception ex) {
                Debug.Log($"[Spire] DynamoProduct: {ex.Message}");
            }
        }
    }
}
