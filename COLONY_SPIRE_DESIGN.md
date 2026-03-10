# 🐜 Colony Spire Mod — Design & Requirements

## Vision

The Colony Spire is a late-game building that consumes **Royal Jelly** and other expensive resources to provide permanent colony-wide upgrades. It is the endgame resource sink and the only source of Sentinels. Combined with the existing Queen Tier mod (T key to cycle larvae tiers), it creates a Factorio-style scaling loop:

```
Launch Gyne → Prestige ★ → Unlock Colony Spire
→ Feed Royal Jelly → Upgrade tracks → Colony gets stronger
→ Stronger colony → Launch more Gynes → More prestige
→ Higher upgrades → Need more Royal Jelly → Scale production
```

---

## Upgrade Tracks

### 🏃 Track 1: Pheromone Trails (Worker Speed)

All workers move faster. Infinite stacking.

| Level | Effect | Cost |
|-------|--------|------|
| 1 | +5% speed | Royal Jelly × 2 |
| 2 | +10% speed | Royal Jelly × 3 |
| 3 | +15% speed | Royal Jelly × 4 |
| N | +(N×5)% speed | Royal Jelly × (N+1) |

**Hook**: `Ant.GetSpeed()` — multiply return by `(1 + pheromoneLevel * 0.05f)`

### 👑 Track 2: Royal Mandate (Queen Speed)

Queen produces larvae faster. Infinite stacking.

| Level | Effect | Cost |
|-------|--------|------|
| 1 | +10% Queen speed | Royal Jelly × 2 + Microchip × 1 |
| 2 | +20% Queen speed | Royal Jelly × 2 + Microchip × 2 |
| N | +(N×10)% Queen speed | Royal Jelly × 2 + Microchip × N |

**Hook**: `Queen.BuildingUpdate()` — divide timer by `(1 + royalMandateLevel * 0.1f)`

### ⛏️ Track 3: Deep Mining (Mining Speed)

Workers mine resources faster. Infinite stacking.

| Level | Effect | Cost |
|-------|--------|------|
| 1 | +15% mining | Royal Jelly × 2 + Iron Bar × 3 |
| 2 | +30% mining | Royal Jelly × 2 + Iron Bar × 6 |
| N | +(N×15)% mining | Royal Jelly × 2 + Iron Bar × (N×3) |

**Hook**: `AntCasteData.mineSpeed` modifier or mining method patch

### 🦠 Track 4: Hardened Carapace (Mold Resistance)

Reduces mold damage. Capped at 5 — full immunity.

| Level | Effect | Cost |
|-------|--------|------|
| 1 | Mold damage -20% | Royal Jelly × 3 + Resin × 5 |
| 2 | Mold damage -40% | Royal Jelly × 5 + Resin × 8 |
| 3 | Mold damage -60% | Royal Jelly × 8 + Rubber × 3 |
| 4 | Mold damage -80% | Royal Jelly × 12 + Rubber × 5 |
| 5 | **MOLD IMMUNE** | Royal Jelly × 20 + Rubber × 10 + Biofuel × 5 |

**Hook**: Mold/poison damage application method — multiply by `(1 - carapaceLevel * 0.2f)`

### 🪽 Track 5: Wing Strengthening (Flyer Carry Capacity)

Flying units carry more. Capped at 5.

| Level | Effect | Cost |
|-------|--------|------|
| 1 | Flyer carry +1 | Royal Jelly × 3 + Fabric × 2 |
| 2 | Flyer carry +2 | Royal Jelly × 3 + Fabric × 4 |
| N | Flyer carry +N | Royal Jelly × 3 + Fabric × (N×2) |

**Hook**: `Ant.Fill()` — if `data.flying`, add `wingLevel` to `carryCapacity`

### ⚔️ Track 6: Sentinel Hatching (One-Shot Production)

The **only** way to produce Sentinels. Not a level — a repeatable craft.

| Per Sentinel | Cost |
|-------------|------|
| 1 Sentinel | Royal Jelly × 10 + Microchip × 3 + Wafer × 1 |

Production time: **30 seconds** per Sentinel. Blocks all other Spire activity while hatching.

**Hook**: Custom production logic on the Spire building, spawns `AntCaste.SENTINEL` (38)

---

## Requirements

### Phase 0: Foundation ✅

- [x] **R0.1** — Research mold/poison system: StatusEffect.DISEASED=5, lifeDrainFactor field
- [x] **R0.2** — Research RadarTower: uses UIClickType 18 (BUILDING_SMALL), SetClickUi_Intake with Generic1(50) button
- [x] **R0.3** — Research spawn: Queen uses SpawnPickup, GyneTower uses NuptialFlight.SpawnActor
- [x] **R0.4** — Research Progress.Write/Read: uses Save.Write(int)/ReadInt(), version-gated

### Phase 1: Prestige System ✅

- [x] **R1.1** — Added `Progress.prestigeLevel` + 5 upgrade level fields (all static int)
- [x] **R1.2** — Patched `GyneTower.StartGyne()` to increment + Debug.Log
- [x] **R1.3** — Patched `Progress.Write()` to save all 6 fields
- [x] **R1.4** — Patched `Progress.Read()` with version >= 95 guard
- [x] **R1.5** — Queen title shows "Queen [T1] P0" (P = prestige level)

### Phase 2: Worker Speed Bonus (Pheromone Trails) 🟡

Field exists and speed patch applied. No way to increment yet (needs Colony Spire UI).

- [x] **R2.1** — `Progress.pheromoneLevel` added + saved/loaded
- [x] **R2.2** — `Ant.GetSpeed()` patched: `speedMove * (1 + pheromoneLevel * 0.05f)`
- [ ] **R2.3** — Test: verify ants move visibly faster at level 5+

### Phase 3: Queen Speed Bonus (Royal Mandate) 🟡

Field exists and timer patch applied. No way to increment yet (needs Colony Spire UI).

- [x] **R3.1** — `Progress.royalMandateLevel` added + saved/loaded
- [x] **R3.2** — `Queen.BuildingUpdate()` timer divides by `(1 + royalMandateLevel * 0.1f)`
- [x] **R3.3** — Stacks with T2 (3×) and T3 (9×) tier multiplier

### Phase 4: Remove Vanilla Sentinel Recipe ✅

- [x] **R4.1** — ASSEMBLE_SENTINEL recipe row blanked in `prefabs.fods`
- [x] **R4.2** — Sentinels can only come from Colony Spire now

### Phase 5: Colony Spire Building ✅

Full upgrade UI with real resource costs and Sentinel spawning.

- [x] **R5.1** — RadarTower.selectedTrack field added
- [x] **R5.2** — Colony Spire shows upgrade tracks (keys 1-6 to select)
- [x] **R5.3** — Title shows: "Spire: 1:Speed Lv0"
- [x] **R5.4** — Press U to upgrade selected track
- [x] **R5.5** — Resource cost checking: per-track cost table (RJ + Microchip/IronBar/Resin/Rubber/Fabric/Wafer/Biofuel)
- [x] **R5.6** — Sentinel track initiates 30s hatching timer; spawns `AntCaste.SENTINEL` on completion
- [x] **R5.7** — BUG FIX: Track 6 no longer corrupts prestigeLevel

### Phase 6: Mold Resistance (Hardened Carapace) ✅

- [x] **R6.1** — Research: `StatusEffects.lifeDrainFactor` in `CombineEffects` confirmed as damage path
- [x] **R6.2** — `ModState.carapaceLevel` declared + saved/loaded (PlayerPrefs)
- [x] **R6.3** — Postfix `StatusEffects.CombineEffects`: multiply drain by `max(0, 1 - carapaceLevel * 0.2f)`
- [x] **R6.4** — Capped at level 5 via `ModState.UpgradeTrack`

### Phase 7: Mining Speed (Deep Mining) ✅

- [x] **R7.1** — Research: `BiomeObject.GetMineDuration(float mine_speed)` is the mining hook
- [x] **R7.2** — `ModState.miningLevel` declared + saved/loaded (PlayerPrefs)
- [x] **R7.3** — Postfix `BiomeObject.GetMineDuration`: divide result by `(1 + miningLevel * 0.15f)`

### Phase 8: Flyer Carry Capacity (Wing Strengthening) ✅

- [x] **R8.1** — `ModState.wingLevel` declared + saved/loaded (PlayerPrefs)
- [x] **R8.2** — Postfix `Ant.Fill`: if `data.flying`, add `wingLevel` to `carryCapacity`
- [x] **R8.3** — Capped at level 5 via `ModState.UpgradeTrack`

### Phase 9: Sentinel Hatching ✅

- [x] **R9.1** — Colony Spire accepts Sentinel Hatching resources (RJ×10 + Microchip×3 + Wafer×1)
- [x] **R9.2** — 30-second hatch timer (`ModState.sentinelHatchTimer`) ticks in `SpireBuildingUpdatePatch`
- [x] **R9.3** — Spawns `AntCaste.SENTINEL` (38) at Spire location via `GameManager.SpawnAnt`
- [x] **R9.4** — Blocks inserts and new upgrades while hatching; GatherProgress shows countdown timer

### Phase 10: Polish & Integration

- [x] **R10.1** — All upgrade levels saved/loaded correctly  ✅ done via PlayerPrefs
- [x] **R10.2** — Debug logging for level-ups ✅ done
- [ ] **R10.3** — Queen title shows both tier and prestige: "Queen [T2] ★4"
- [ ] **R10.4** — Update MOD_README.md with full documentation
- [ ] **R10.5** — Test full progression: early → mid → late → Sentinel

---

## Implementation Approach

All changes use the same two techniques from the Queen Tier mod:

| Technique | Used for |
|-----------|----------|
| **Mono.Cecil IL patching** | All runtime behavior (speed, timers, spawning, damage) |
| **prefabs.fods XML editing** | Recipe removal (sentinels), building definitions |

The patcher (`MicrotopiaModder/Program.cs`) will be extended to apply all patches in sequence, always restoring from backup first.

---

## Static Fields on Progress (Saved State)

All upgrade levels are stored as static int fields on the `Progress` class, saved/loaded alongside existing fields like `nupFlightLevel`.

```
Progress.prestigeLevel      — Gynes launched (prestige stars)
Progress.pheromoneLevel      — Track 1: Worker speed
Progress.royalMandateLevel   — Track 2: Queen speed
Progress.miningLevel         — Track 3: Mining speed
Progress.carapaceLevel       — Track 4: Mold resistance (max 5)
Progress.wingLevel           — Track 5: Flyer carry (max 5)
```

---

## Open Questions

1. **Which building becomes the Colony Spire?** Monument is the most "prestige" building thematically. Or we could repurpose the Radar Tower since it already takes resources. Need to investigate what's easiest to mod.

2. **How does the Spire UI work?** The game's click panel is limited (BUILDING_SMALL). We might need the same T-key approach — press keys while Spire is selected to choose upgrade track. Or we find a building type with a richer UI (Factory has recipe selection).

3. **Should prestige be required to BUILD the Spire?** Or is it just the endgame building anyone can build, but it's expensive? Prestige gating adds meaning but more complexity.

4. **Escalating Gyne costs** — do later Gynes cost more? This prevents rushing but adds implementation complexity. Could be Phase 11.
