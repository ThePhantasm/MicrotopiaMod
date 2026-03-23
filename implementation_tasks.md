# Colony Spire Mod — Endgame Overhaul Implementation Tasks

This document translates the `phase2.md` design into actionable, C#/Harmony coding tasks.

## ✅ Step 0: Design
- [x] Finalize core progression loop logic.
- [x] Separate Combat from Prestige, re-routing it to the Infinite Loop.

---

## 🛠️ Phase 1: Foundation (The Omni-Ant & Gyne Tower)

### State & Save/Load
- [x] **ModState Updates:** Add `int prestigePoints` and `int excavationCores` to `ModState.cs`. 
- [x] **Persistence:** Update `ModState.Save()` and `Load()` to serialize both fields.

### T4 Omni-Ant Implementation (`Ant.cs` patches)
- [x] **Caste ID:** Allocated `OMNI_ANT_CASTE_ID = 50` in ModState (past vanilla `_MAX=46`, avoids collision with `DIGGER=10`).
- [x] **Stats Override:** Updated `WingCarryPatch` (Harmony Postfix on `Ant.Fill`). If `caste == 50`:
  - [x] Set `speed` to `2.0x` base.
  - [x] Mining speed handled inherently via the speed multiplier.
  - [x] Set `carryCapacity` to `GetOmniAntCarry()` (base 2, +1 per 2 prestige levels).
  - [x] Enabled `canFly`.
  - [x] Set lifespan (`energy`) to 600 seconds.
- [ ] **Visual Stacking:** Verify `GetPickupLocalPosInStack` successfully renders the dual items without clipping.

### The Gyne Tower & Prestige Upgrades (`GyneTower.cs` patches)
- [x] **Acceptance Gate:** `GyneTowerPrestigePatch` prefix on `CheckIfGateIsSatisfied`. Accepts only T4 (caste 50) ants with `HasStatusEffect(StatusEffect.OLD)`. Rejects all others.
- [x] **Consumption Logic:** When gate accepts an elder T4, the ant is killed and `+10` added to `ModState.prestigePoints`.
- [x] **Launch Threshold Check:** When `prestigePoints >= GetPrestigeThreshold()`, triggers `StartGyne()`, increments prestige level, resets points. Massive visual celebration.
- [x] **UI Work:** Queen title now shows `★{level} [{pts}/{threshold}] 🛡{cores}` inline.

---

## 🛠️ Phase 2: Combat & Infinite Loop

### State & Save/Load
- [x] **ModState Updates:** `excavationCores` added and persists via `CSP_ExcavCores` key.

### Combat Drops (`CorpseAttackPatch.cs`)
- [x] **Loot Interception:** Mega piñata item dropping replaced with excavation core grants.
- [x] **Point Disbursement:** Uses `bob.GetRadius()`. Grants `+1` (small), `+3` (medium), or `+10` (large) `ModState.excavationCores`. Shows celebratory explosion.

### Deep Excavator & Island Furnace
- [x] **Island Furnace (`BatteryBuilding` patch):** `FurnaceCanInsertPatch` and `FurnaceOnArrivalPatch` prefix BatteryBuilding's intake methods. When `prestigeLevel >= 1`, batteries accept ANY material and convert to 5 energy per item. Title/description overridden via `FurnaceTitlePatch`/`FurnaceHoverPatch`.
- [x] **Deep Excavator (`DeepExcavatorBehavior` MonoBehaviour):** Attached to Colony Spire in `SpireInitPatch`. Runs every 60s, consuming 50 island energy + 5 excavation cores to spawn a random resource deposit (`BOB_DIRT`, `BOB_STONE`, `BOB_IRON`, `BOB_COPPER`, `BOB_RESIN`, `BOB_SAND`, `BOB_CRYSTAL`) on the island via `GameManager.SpawnBiomeObject`.

---

## 🛠️ Phase 3: Production Chains & High-End Assembler

### Pickup Config Expansion
- [ ] **Liquid Metals:** Find/assign IDs for `Liquid Iron`, `Liquid Copper`, and `Resin`. Implement mapping rules turning Iron/Copper bars into Liquid quantities.
- [ ] **Circuits & Composites:** Find/assign IDs for `Circuit Board`, `Advanced Compute Unit`, `Alloy Frame`, and `Neural Serum`.
- [ ] **Crystal Update:** Direct Crystal refinement path straight into Microchip crafting lines via `prefabs.fods` tweaks.

### High-End Assembler Building
- [ ] **Building Config:** Define a massive endgame Assembler prefab.
- [ ] **UI Implementation:** Adapt the existing `Spire Track Selector` UI code to build a "Select Active Assembler Recipe" drop-down.
- [ ] **Recipe Intake & Outtake:** Based on chosen selection (e.g., *Adv. Compute Unit*), hardcode the specific `CanInsert` lists allowing ONLY `Circuit Board ×2 + Microchip ×1 + LED ×1`, and producing the unit after a `45s` cycle timer. Apply this structure across all 6 recipes.

---

## 🛠️ Phase 4: Balancing & Polish

### Balance Ramps 
- [x] **Prestige Exponential Function:** Coded `GetPrestigeThreshold()` formula (Level 1 = 20, Level 6 = 500, +200 post-6).
- [x] **Tech Tree Locks:** Added `MOD_T4_ENDGAME` tech node (requires `MOD_COLONY_SPIRE` + `REGULAR_T3 150` + `GYNE_T2 3`). Gates Island Furnace, Deep Excavator, and Dynamo upgrade behind T3 inventor research.
- [x] **Tech Tree Circle Colors:** `TechTreeColorPatch` tints tech circles by cost tier: white=T1, blue-purple=T2, gold=T3, pink/magenta/crimson=Gyne T1/T2/T3.
- [x] **ENERGY_POD11 (Dynamo Upgrade):** `DynamoProductPatch` makes Dynamos produce ENERGY_POD11 (PickupType 111) when T4 Endgame is unlocked. Requires `ENERGY_POD11` row in prefabs.fods with energyAmount set.
- [ ] **prefabs.fods ENERGY_POD11 Row:** Add pickup row for `ENERGY_POD11` (type=111) with high energyAmount (~50). Currently the Dynamo patch sets the product type, but the PickupData row needs to be injected.
- [ ] **Bug/Playtest Session:** Validate that Deep Excavators halt accurately when zero Excavator Cores are available. Validate the UI tracking.
- [ ] **Re-patch TechTree:** Run `patch_techtree.ps1` on a fresh techtree.fods backup to inject `MOD_T4_ENDGAME` (existing saves already patched won't get re-patched due to idempotency check).

