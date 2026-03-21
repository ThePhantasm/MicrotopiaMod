# 🐜 Microtopia – Colony Spire Mod

## Overview

This is a massive gameplay overhaul mod for Microtopia that introduces deep endgame progression through the **Colony Spire** (repurposing the Radar Tower) and an active combat expansion. It shifts late-game focus towards managing specialized logistics, defeating hostile giant robots, and assembling the ultimate colony!

**Royal Jelly as the Core Resource:** The endgame economy centers entirely around Royal Jelly. It is the core currency required to fuel the Spire’s powerful upgrades and hatch Sentinels, meaning you must fully automate and scale its production.

### Major Features

- **The Colony Spire:** The Radar Tower is now the core progression mechanic. Insert scaling resources to level up your entire colony across several different tracks:
  1. **Speed:** Pheromone trail speed bonuses.
  2. **Queen:** Royal mandate bonuses for faster larvae production.
  3. **Mine:** Deep mining efficiency (gather faster).
  4. **Mold:** Carapace resistance against mold and disease.
  5. **Wings:** Increased carry capacity for flying units.
  6. **Gatherer:** Reduced delay before dispatching ants from the Gatherer building.
  7. **Sentinel:** Direct insertion of materials to hatch ultimate defending Sentinels (vanilla recipe removed).
- **Queen Tiers:** Select your Queen and press **G** (or use the UI button) to cycle her output between T1, T2, and T3 larvae. T2 larvae are produced at 1/3 the normal speed, and T3 larvae at 1/9 the normal speed. We also tweaked recipes to allow Gyne production using T2 and T3 larvae!
- **Concrete Island Combat Overhaul:** Giant robot corpses are no longer just passive mining nodes. They have health pools, regenerate, and spawn hostile enemies! Mining them deals "melee damage." Destroying them results in a "Mega-Pinata" burst of endgame resources (Microchips, Wafers, Biofuel, Royal Jelly).

### Minor Features

- **Island Scaling:** Includes the option to scale the size of your initial starting island.
- **Customizable Main Bus Trails:** Added customizable colors for your Main Bus trails to help keep your logistics visually organized.

---

## 🛠️ Installation Guide

This mod requires **BepInEx** to run. 

### Prerequisites
1. **Install BepInEx:** Download the standard `x64` version of BepInEx for your OS and extract it directly into your Microtopia game directory (next to `Microtopia.exe`). Run the game once to generate the `BepInEx/plugins` folder before proceeding.
2. **.NET SDK:** Ensure you have a recent .NET SDK installed on your PC, as this mod compiles its plugin dynamically during installation to match your framework.

### Installing the Mod
1. Download this entire repository.
2. Inside your Microtopia game directory, create a folder called `Mods` if it doesn't already exist.
3. Place the downloaded repository folder into the `Mods` folder (e.g. `Microtopia/Mods/QueenTierMod`).
4. Open a PowerShell terminal.
5. Navigate to the mod folder and run the install script:
   ```powershell
   cd d:\SteamLibrary\steamapps\common\Microtopia\Mods\QueenTierMod
   .\install.ps1
   ```
   *Note: If you get a PowerShell Execution Policy error, run this instead:*
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```

### What the Script Does
The installer does all the heavy lifting for you automatically:
1. It creates a seamless `.backup` of your game's data sheets (`prefabs.fods`).
2. It compiles the C# BepInEx `.dll` plugin directly from the source code and drops it into your `BepInEx/plugins` folder.
3. It splices the custom assets and modifies the `prefabs.fods` data sheet.

### To Uninstall
To safely remove the mod, safely run the `uninstall.ps1` script to restore your `prefabs.fods` from backup and remove the plugin, or remove the `.dll` from `BepInEx/plugins/ColonySpirePlugin.dll` and restore your `prefabs.fods` via Steam's "Verify Integrity of Game Files" feature.

---

## Technical Notes
- **Save Compatibility:** Mod State (Prestige level, Spire upgrades, Queen tiers) is safely saved natively using Unity's `PlayerPrefs` and will persist exactly as you left it across your saves.
- **Dynamic Hooking:** All C# logic acts at runtime using Harmony IL patches (and Mono.Cecil for data sheets), meaning base game methods are carefully wrapped rather than destroyed!

---

## Changelog

### v1.0.1 *(patch)*
- **Fixed:** Island scale setting was incorrectly applied when loading an existing save that was created with a different scale (or no scale at all). The scale is now saved as a per-save sidecar file (`.islandscale`) and loaded back on game load. Old saves without a sidecar default to `1.0` so they are never retroactively resized.

### v1.0.0
- Initial release.
