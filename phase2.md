# 🐜 Microtopia Colony Spire Mod — Endgame Overhaul Design (v2)

## Design Philosophy

**The problem isn't "we need a sink" — it's "we need a purpose."** Right now prestige is too easy: hatch a Gyne, launch it, done. There's no scaling, no challenge ramp, no reason to optimise production. This design turns prestige into a Factorio-style escalating mega-project that forces genuine multi-island supply chain mastery.

---

## The Big Picture

```
┌─────────────────────── THE LOOP ─────────────────────────┐
│                                                           │
│  Resources → Furnace → Energy → Excavator → MORE Resources │
│       ↓                                                    │
│  Composites → Bioforge → Super Ant Larvae → Super Ants    │
│       ↓                                    (USE THEM!)    │
│  Combat kills → Prestige Points              ↓ (when old)│
│       ↓                                    Gyne Tower     │
│  Super Ants → SUPER GYNE LAUNCH ← ── ── ── ┘            │
│       ↓                                                   │
│  ★ PRESTIGE ★ (costs scale each time)                     │
│       ↓                                                   │
│  Higher prestige → need MORE of everything → loop harder  │
└───────────────────────────────────────────────────────────┘
```

---

## Part 1: The Infinite Resource Loop

To survive an infinitely scaling endgame, you need infinite resources.

### 1A. Island Furnace (Resource → Energy)
**Purpose:** Convert surplus raw materials into battery energy. This powers the Excavator and other energy-hungry endgame buildings.
- Accepts any material with non-zero value
- Energy yield scales by material rarity (e.g., Stone=2, Biofuel=25)
- Output goes to island battery network

### 1B. Deep Excavator (Energy + Cores → Infinite Deposits)
**Purpose:** Spend energy and **Excavation Cores** to regenerate resource deposits on islands that have been mined out.
- Sinks ~50 energy per cycle from the Furnace AND requires 1 Excavation Core.
- Over 3-5 minutes, "excavates" a new resource node matching the island's biome.
- This creates a permanent, self-sustaining factory loop on every island — provided you keep fighting to supply the cores!

---

## Part 2: The High-End Assembler

### A Single Endgame Production Building

**Purpose:** A new building that serves as the endgame crafting hub. It accepts raw/processed materials and produces composite resources and Super Ant larvae. Think of it as the late-game factory that ties all your supply chains together.

**Key properties:**
- Occupies a significant footprint (important building, not a throwaway)
- Has a recipe selector (like the Spire's track selector) to choose what to produce
- Each recipe takes 30-60 seconds and consumes specific materials
- Requires energy (battery power) to operate
- Only one recipe active at a time — forces prioritization or building multiple assemblers
- Unlocked via tech tree (gate behind advanced research)

**Implementation:** Clone an existing factory-type building in `prefabs.fods`, repurpose its Unlocker/intake system. Use the same pattern as the Spire's track selector UI to let players choose recipes.

---

## Part 3: Production Chains (Depth)

### Advanced Materials & Liquid Smelting

To add depth without just requiring "more of the same", we are introducing **Liquid Storage** and **High-End Components**. These form the backbone of the endgame economy.

**1. Liquid Metals Integration**
Instead of directly feeding bars into endgame recipes, Iron and Copper must first be passed through a **Liquid Smelter** and stored in **Liquid Storage**.
- Raw Iron → Iron Bar → Liquid Iron (1 unit = 100 volume)
- Raw Copper → Copper Bar → Liquid Copper (1 unit = 100 volume)

**2. High-End Components**
These are the new "science packs" — proving you've mastered multiple production lines simultaneously.

| Component | Recipe | Required chains |
|---|---|---|
| **Circuit Board** | Liquid Iron (100v) + Liquid Copper (200v) + Resin ×1 | Liquid Metal smelting + Resource extraction |
| **Alloy Frame** | Circuit Board ×1 + Gold ×1 + Concrete ×1 | Electronics + Gold refining + Concrete processing |
| **Neural Serum** | Royal Jelly ×5 + Crystal Dust ×2 + Biofuel ×2 | Queen management + Crystal processing + Bio processing |
| **Advanced Compute Unit** | Circuit Board ×2 + Microchip ×1 + LED ×1 | Advanced electronics + Microchip manufacturing |

**Production chain depths:**
```
Iron Bar → Liquid Iron ───────────────────────┐
Copper Bar → Liquid Copper ───────────────────┤
Resin ────────────────────────────────────────┤→ Circuit Board ─┐
                                              │                 │
Gold Raw → Gold ───────────┐                  │                 ├→ Alloy Frame
Stone → Concrete ──────────┼──────────────────┘                 │
                                                                │
(Electronics chain → Microchip) ──────────────┐                 │
(Electronics chain → LED) ────────────────────┼─────────────────┘
                                              │→ Advanced Compute Unit
```

### All Assembler Recipes

The High-End Assembler handles the heavy lifting, taking these advanced components and turning them into Super Ant larvae:

| Recipe | Inputs | Output | Time |
|---|---|---|---|
| Circuit Board | Liquid Iron + Liquid Copper + Resin | Circuit Board ×1 | 20s |
| Alloy Frame | Circuit Board ×1 + Gold ×1 + Concrete ×1 | Alloy Frame ×1 | 30s |
| Adv. Compute Unit | Circuit Board ×2 + Microchip ×1 + LED ×1 | Adv. Compute Unit ×1| 45s |
| Neural Serum | Royal Jelly ×5 + Crystal Dust ×2 + Biofuel ×2 | Neural Serum ×1 | 45s |
| T4 Omni-Ant Larva| T3 Larva ×1 + Adv. Compute Unit ×1 + Neural Serum ×1 | T4 Larva ×1 | 90s |

> [!NOTE]
> Building multiple Assemblers is encouraged — one for components, one for larvae. This naturally creates demand for more resources and more islands to feed them.

---

## Part 4: Super Ants

### The T4 "Omni-Ant"

Instead of multiple specialized castes, the endgame introduces a single, ultimate worker: the **T4 Omni-Ant**. This ant is basically a supercomputer on legs. It does *everything*.

Produced by combining **T3 Larvae** with **Advanced Compute Units** and **Neural Serum** in the High-End Assembler.

### Stats & Abilities

| Stat | T1 Worker | T2 Soldier | T3 Royal | T4 Omni-Ant |
|---|---|---|---|---|
| Speed | 1.0× | ~1.2× | ~1.4× | **2.0×** |
| Base Carry Capacity | 1 | 1 | 1 | **2** (Upgradeable) |
| Mining Speed | 1.0× | ~1.5× | ~2.0× | **3.0×** |
| Lifespan (energy) | ~120s | ~180s | ~300s | **~600s** (10 mins) |
| Flight | No | No | No | **Yes** |

> [!IMPORTANT]
> **Base Carry is 2, scaling via Prestige.** Unlike lower tiers, the T4 Omni-Ant starts with a multi-carry capacity of 2. Crucially, as you ascend through Prestige ranks, you can permanently upgrade this carry capacity further.

### Carry Capacity: Verified Feasible

From the decompiled source:

```csharp
// Ant.cs line 26
public int carryCapacity = 1;

// Ant.cs line 2571-2573
public bool IsFull()
{
    return carryingPickups.Count >= carryCapacity;
}

// Ant.cs line 2581-2589 — stacks pickups visually
private Vector3 GetPickupLocalPosInStack(int h)
{
    Vector3 localPosition = carryPos.localPosition;
    for (int i = 0; i < h; i++)
        localPosition.y += carryingPickups[i].height;
    return localPosition;
}
```

**The game already supports multi-carry!** `carryCapacity` is a public int on the `Ant` class. We simply set this via Harmony patch when casting the T4 ant, and increment it based on the player's saved Prestige upgrades.

### "Elder" Mechanic — Use Them, Then Sacrifice Them

The critical design tension: T4 Omni-Ants are both your **best workers** and your **prestige currency**.

**Life phases:**
```
 ◆ Young ──────────── ◆ Prime ──────────── ◆ Elder ────── ◆ Death
   0%                   20%                   80%           100%
   Full stats           Full stats            -20% speed    Dies
   Can't sacrifice      Can't sacrifice       CAN sacrifice
```

- **Phase 1-2 (0-80% age):** Full stats. They are amazing workers. Use them!
- **Phase 3 (80-100% age):** The ant gains the `OLD` status effect (already in vanilla). **Only during this phase can they be inserted into the Gyne Tower for Prestige points.**

---

## Part 5: Combat → Deep Excavator Fuel

### Robot Corpses Grant Excavation Cores

Combat is no longer an alternative way to earn prestige. Instead, combat is **the engine that keeps your multi-island economy alive.** Destroying robot corpses drops a vital new currency: **Excavation Cores**. 

The Deep Excavator (Part 1B) requires these Cores to spawn new infinite resource nodes. Without combat, your islands eventually run dry.

**Corpse Tiers (based on size/health):**

| Corpse Type | Health | Excavation Cores Dropped | Equivalent |
|---|---|---|---|
| Small remnant | ~15,000 HP | 1 Core | Keeps 1 island going briefly |
| Medium remnant | ~50,000 HP | 3 Cores | Sustains a small factory |
| Large remnant | ~150,000 HP | 10 Cores | Fuels an empire |

> [!TIP]
> **This tightly integrates combat.** You aren't just fighting for points — you are fighting to secure the raw materials needed to run the High-End Assembler to build your Super Ants. Combat -> Resources -> Omni-Ants -> Prestige.

**Where do points go?** Stored in `ModState.excavationCores`. The Deep Excavator consumes these silently from the global pool when it runs.

---

## Part 6: The Super Gyne (Escalating Prestige)

### How Prestige Works Now

The Gyne Tower becomes a **prestige accumulator** strictly powered by your ultimate endgame workers.

**Prestige uses a unified point system fueled ENTIRELY by Super Ants.** 

| Source | Points Earned |
|---|---|
| Feed 1 T4 Omni-Ant (elder) to Gyne Tower | **10 points** |

**Prestige thresholds (points needed to launch):**

| Prestige Level | Points Needed | Omni-Ants Required |
|---|---|---|
| 1 | 20 | 2 T4 Omni-Ants |
| 2 | 50 | 5 T4 Omni-Ants |
| 3 | 100 | 10 T4 Omni-Ants |
| 4 | 200 | 20 T4 Omni-Ants |
| 5 | 350 | 35 T4 Omni-Ants |
| 6 | 500 | 50 T4 Omni-Ants |
| 7+ | +200 each | **Infinite scaling** |

> [!IMPORTANT]
> **The Escalating Challenge.** Because each Omni-Ant requires massive resources, and infinite resources require Excavation Cores, hitting Level 6 requires a smoothly running combat-and-logistics engine across multiple islands simultaneously. 

### The Super Gyne Launch

When the point threshold is met, the Gyne Tower is ready for a **Super Gyne Launch**:

- Visual: massive particle effect, the tower glows, all accumulated ants "merge" into the Super Gyne
- The Nuptial Flight begins with the Super Gyne (uses existing `NuptialFlight` system)
- On completion: prestige level increments, point counter resets to 0
- **Existing Spire track bonuses persist** — prestige doesn't wipe your upgrades

### Why This Scaling Works

Each prestige level requires more Omni-Ants, which requires:
- More composites → more raw resources → more active islands
- More T2/T3 larvae → Queens running at full capacity
- More raw resources → Excavators running continuously
- Excavators running → **More combat engagement** to secure Cores.

**The demand is multiplicative, not additive.** Going from Prestige 5 to 6 doesn't just need "a few more T4s" — it requires clearing major combat zones to keep your Deep Excavators churning out the metals required for your Microchips.

---

## Implementation Roadmap

### Impl Phase 1: Foundation (can ship as standalone improvement)
1. **Super Ant stats** — Patch `Ant.Fill()` for T4 caste detection → enhanced speed, carry, lifespan
2. **Carry capacity** — Hardcode T4 carry to 2
3. **Gyne Tower rework** — Accept Super Ants (elder-only), add points per ant fed
4. **Prestige threshold** — Escalating points-to-launch requirement

### Impl Phase 2: Combat & Infinite Loop
5. **Island Furnace & Excavator** — Implement resource-to-energy and energy-to-deposit loop.
6. **Combat Cores** — On corpse death, add points to `ModState.excavationCores`; corpse size → point value
7. **Excavator Fuel** — Deep Excavator silently deducts 1 Core per successful node spawn

### Impl Phase 3: Production Chains + Assembler
8. **Composite resources** — New PickupTypes or repurpose unused slots (Liquid Metals, Circuits)
9. **High-End Assembler** — New building with recipe selector for components + T4 larvae
10. **Prestige UI** — Show progress toward next Super Gyne (points accumulated / threshold)

### Impl Phase 4: Balancing
11. **Scaling/balancing pass** — Tune all costs, timers, health pools, prestige curve
12. **Tech tree gating** — Gate the Assembler and T4 behind appropriate research

> [!IMPORTANT]
> **The New Loop is Tighter.** Instead of combat racing alongside production to hit Prestige, combat *fuels* production. Combat -> Resources -> Super Ants -> Prestige. It ensures no part of the game can be ignored!

---

## Future / Optional

### Broadcast Tower / Pheromone Amplifier
- Per-island buildings that consume resources for local or global buffs
- Creates island specialization incentives
