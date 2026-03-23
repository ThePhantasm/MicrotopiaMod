# ================================================================
# patch_prefabs.ps1 — Inject custom pickup types into prefabs.fods
# Run from the QueenTierMod directory.
# Idempotent: checks for MOD marker before injecting.
# ================================================================

param(
    [string]$GameDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)

$prefabsPath = Join-Path $GameDir "Microtopia_Data\StreamingAssets\prefabs.fods"

if (-not (Test-Path $prefabsPath)) {
    Write-Host "ERROR: prefabs.fods not found at $prefabsPath" -ForegroundColor Red
    exit 1
}

# ── Create backup if needed ──
$backupPath = "$prefabsPath.backup"
if (-not (Test-Path $backupPath)) {
    Copy-Item $prefabsPath $backupPath -Force
    Write-Host "  Created backup: $backupPath" -ForegroundColor Cyan
}

# ── Load content ──
$content = [System.IO.File]::ReadAllText($prefabsPath)

# ── Idempotency check ──
if ($content.Contains("ENERGY_POD11")) {
    Write-Host "  prefabs.fods already contains ENERGY_POD11, skipping pickup injection." -ForegroundColor Yellow
} else {
    # ────────────────────────────────────────────────────────────────
    # PICKUP INJECTION: Add ENERGY_POD11 after ENERGY_POD10
    # 
    # The pickup data format in the Pickups sheet is:
    #   Col 1: Code (e.g., ENERGY_POD10)
    #   Col 2: Order (numeric, within category)
    #   Col 3: Category (e.g., ENERGY)
    #   Col 4: Title loc key
    #   Col 5: Desc loc key
    #   Col 6: Energy amount (float)
    #   Col 7: Weight (float, 0 for energy pods)
    #   Col 8: State (SOLID/LIQUID/DUST/LIVING)
    #   Col 9-10: empty
    #   Col 11: in_demo flag ("X" or empty)
    #   Col 12: plain name (for debug)
    # ────────────────────────────────────────────────────────────────

    # Build the new row XML for ENERGY_POD11
    $pod11Row = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>ENERGY_POD11</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="float" office:value="11" calcext:value-type="float">
            <text:p>11</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>ENERGY</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>PICKUP_ENERGYPOD11</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>PICKUP_ENERGYPOD11_DESC</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="float" office:value="12500" calcext:value-type="float">
            <text:p>12500</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="float" office:value="0" calcext:value-type="float">
            <text:p>0</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>SOLID</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="2" />
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>X</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>Ultra Energy Pod</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@

    # Find the ENERGY_POD10 row end and insert after it
    # We look for the closing </table:table-row> after ENERGY_POD10
    $pod10Pattern = "<text:p>ENERGY_POD10</text:p>"
    $pod10Idx = $content.IndexOf($pod10Pattern)
    if ($pod10Idx -lt 0) {
        Write-Host "ERROR: Could not find ENERGY_POD10 in prefabs.fods" -ForegroundColor Red
        exit 1
    }

    # Find the end of the ENERGY_POD10 row
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $pod10Idx)
    if ($endIdx -lt 0) {
        Write-Host "ERROR: Could not find row end after ENERGY_POD10" -ForegroundColor Red
        exit 1
    }
    $insertPoint = $endIdx + $rowEnd.Length
    
    $content = $content.Insert($insertPoint, "`n" + $pod11Row)
    Write-Host "  Injected ENERGY_POD11 pickup (energy=12500, state=SOLID)" -ForegroundColor Green
}

# ────────────────────────────────────────────────────────────────
# RECIPE INJECTION: Add REAC_ENERGY11 recipe (POD10 ×2 → POD11 ×1)
# Follows the same pattern as REAC_ENERGY10
# ────────────────────────────────────────────────────────────────
if ($content.Contains("REAC_ENERGY11")) {
    Write-Host "  prefabs.fods already contains REAC_ENERGY11, skipping recipe injection." -ForegroundColor Yellow
} else {
    $recipe11Row = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>REAC_ENERGY11</text:p>
          </table:table-cell>
          <table:table-cell table:style-name="ce250" office:value-type="string" calcext:value-type="string">
            <text:p>FACRECIPE_REAC_ENERGY11</text:p>
          </table:table-cell>
          <table:table-cell table:style-name="Default" office:value-type="string" calcext:value-type="string">
            <text:p>ENERGY_POD10 2</text:p>
          </table:table-cell>
          <table:table-cell table:style-name="Default" />
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>ENERGY_POD11 1</text:p>
          </table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>REACTOR_LARGE</text:p>
          </table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="10" calcext:value-type="float">
            <text:p>10</text:p>
          </table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>X</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="53" />
        </table:table-row>
"@

    # Find REAC_ENERGY10 recipe row and insert after it
    $recipe10Pattern = "<text:p>REAC_ENERGY10</text:p>"
    $recipe10Idx = $content.IndexOf($recipe10Pattern)
    if ($recipe10Idx -lt 0) {
        Write-Host "ERROR: Could not find REAC_ENERGY10 in prefabs.fods" -ForegroundColor Red
        exit 1
    }

    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $recipe10Idx)
    $insertPoint = $endIdx + $rowEnd.Length

    $content = $content.Insert($insertPoint, "`n" + $recipe11Row)
    Write-Host "  Injected REAC_ENERGY11 recipe (POD10 x2 -> POD11 x1, 10s, REACTOR_LARGE)" -ForegroundColor Green
}
# ────────────────────────────────────────────────────────────────
# LIQUID METAL PICKUPS: Add LIQUID_IRON and LIQUID_COPPER after BIOFUEL
# These are the intermediate products for the Foundry production chain.
# They use the LIQUID category/state, same as ACID and BIOFUEL.
# ────────────────────────────────────────────────────────────────
if ($content.Contains("LIQUID_IRON")) {
    Write-Host "  prefabs.fods already contains LIQUID_IRON, skipping." -ForegroundColor Yellow
} else {
    $liquidIronRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>LIQUID_IRON</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="float" office:value="604" calcext:value-type="float">
            <text:p>604</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>LIQUID</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>PICKUP_LIQUID_IRON</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>PICKUP_LIQUID_IRON_DESC</text:p>
          </table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="3" calcext:value-type="float">
            <text:p>3</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>LIQUID</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="3" />
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>Liquid Iron</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@

    $liquidCopperRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>LIQUID_COPPER</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="float" office:value="605" calcext:value-type="float">
            <text:p>605</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>LIQUID</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>PICKUP_LIQUID_COPPER</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>PICKUP_LIQUID_COPPER_DESC</text:p>
          </table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="3" calcext:value-type="float">
            <text:p>3</text:p>
          </table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>LIQUID</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="3" />
          <table:table-cell office:value-type="string" calcext:value-type="string">
            <text:p>Liquid Copper</text:p>
          </table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@

    # Insert after BIOFUEL row
    $biofuelPattern = "<text:p>BIOFUEL</text:p>"
    $biofuelIdx = $content.IndexOf($biofuelPattern)
    if ($biofuelIdx -lt 0) {
        Write-Host "ERROR: Could not find BIOFUEL in prefabs.fods" -ForegroundColor Red
        exit 1
    }
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $biofuelIdx)
    $insertPoint = $endIdx + $rowEnd.Length

    $content = $content.Insert($insertPoint, "`n" + $liquidCopperRow)
    $content = $content.Insert($insertPoint, "`n" + $liquidIronRow)
    Write-Host "  Injected LIQUID_IRON pickup (state=LIQUID, weight=3)" -ForegroundColor Green
    Write-Host "  Injected LIQUID_COPPER pickup (state=LIQUID, weight=3)" -ForegroundColor Green
}

# ────────────────────────────────────────────────────────────────
# DRIED FIBER: New pickup + drying recipe on MOD_DRYING_RACK
# FIBER ×5 → DRIED_FIBER ×1 (60s)
# ────────────────────────────────────────────────────────────────
if ($content.Contains("DRIED_FIBER")) {
    Write-Host "  prefabs.fods already contains DRIED_FIBER, skipping." -ForegroundColor Yellow
} else {
    $driedFiberRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>DRIED_FIBER</text:p></table:table-cell>
          <table:table-cell office:value-type="float" office:value="104" calcext:value-type="float"><text:p>104</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>CRAFTED</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_DRIED_FIBER</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_DRIED_FIBER_DESC</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="1" calcext:value-type="float"><text:p>1</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>SOLID</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="3" />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>Dried Fiber</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@
    # Insert after FIBER row
    $fiberPattern = "<text:p>FIBER</text:p>"
    $fiberIdx = $content.IndexOf($fiberPattern)
    if ($fiberIdx -lt 0) {
        Write-Host "ERROR: Could not find FIBER in prefabs.fods" -ForegroundColor Red
        exit 1
    }
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $fiberIdx)
    $insertPoint = $endIdx + $rowEnd.Length
    $content = $content.Insert($insertPoint, "`n" + $driedFiberRow)
    Write-Host "  Injected DRIED_FIBER pickup (SOLID, CRAFTED)" -ForegroundColor Green
}

# Drying recipe: FIBER ×5 → DRIED_FIBER ×1 (60s, MOD_DRYING_RACK)
if ($content.Contains("DRY_FIBER")) {
    Write-Host "  prefabs.fods already contains DRY_FIBER recipe, skipping." -ForegroundColor Yellow
} else {
    $dryFiberRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>DRY_FIBER</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FACRECIPE_DRY_FIBER</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FIBER 5</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>DRIED_FIBER 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>MOD_DRYING_RACK</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="60" calcext:value-type="float"><text:p>60</text:p></table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell table:number-columns-repeated="55" />
        </table:table-row>
"@
    # Insert after DISSOLVE_GOLD row (near the other custom recipes)
    $dissolveGoldPattern = "<text:p>DISSOLVE_GOLD</text:p>"
    $dissolveGoldIdx = $content.IndexOf($dissolveGoldPattern)
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $dissolveGoldIdx)
    $insertPoint = $endIdx + $rowEnd.Length
    $content = $content.Insert($insertPoint, "`n" + $dryFiberRow)
    Write-Host "  Injected DRY_FIBER recipe (FIBER x5 -> DRIED_FIBER x1, 60s, MOD_DRYING_RACK)" -ForegroundColor Green
}

# ────────────────────────────────────────────────────────────────
# SMELT RECIPES: IRON_RAW + DRIED_FIBER → LIQUID_IRON (on MOD_LIQUID_SMELTER)
#                 COPPER_RAW + DRIED_FIBER → LIQUID_COPPER (on MOD_LIQUID_SMELTER)
# Uses dried fiber as fuel to melt raw ore into liquid metal.
# ────────────────────────────────────────────────────────────────
if ($content.Contains("MOD_SMELT_IRON")) {
    Write-Host "  prefabs.fods already contains MOD_SMELT_IRON, skipping." -ForegroundColor Yellow
} else {
    $smeltIronRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>MOD_SMELT_IRON</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FACRECIPE_MOD_SMELT_IRON</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>IRON_RAW 2, DRIED_FIBER 2</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LIQUID_IRON 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>MOD_LIQUID_SMELTER</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="30" calcext:value-type="float"><text:p>30</text:p></table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell table:number-columns-repeated="55" />
        </table:table-row>
"@

    $smeltCopperRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>MOD_SMELT_COPPER</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FACRECIPE_MOD_SMELT_COPPER</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>COPPER_RAW 2, DRIED_FIBER 2</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LIQUID_COPPER 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>MOD_LIQUID_SMELTER</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="30" calcext:value-type="float"><text:p>30</text:p></table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell table:number-columns-repeated="55" />
        </table:table-row>
"@

    # Insert after DISSOLVE_GOLD row
    $dissolveGoldPattern = "<text:p>DISSOLVE_GOLD</text:p>"
    $dissolveGoldIdx = $content.IndexOf($dissolveGoldPattern)
    if ($dissolveGoldIdx -lt 0) {
        Write-Host "ERROR: Could not find DISSOLVE_GOLD in prefabs.fods" -ForegroundColor Red
        exit 1
    }
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $dissolveGoldIdx)
    $insertPoint = $endIdx + $rowEnd.Length

    $content = $content.Insert($insertPoint, "`n" + $smeltCopperRow)
    $content = $content.Insert($insertPoint, "`n" + $smeltIronRow)
    Write-Host "  Injected MOD_SMELT_IRON recipe (IRON_RAW x2 + DRIED_FIBER x2 -> LIQUID_IRON x1, 30s, MOD_LIQUID_SMELTER)" -ForegroundColor Green
    Write-Host "  Injected MOD_SMELT_COPPER recipe (COPPER_RAW x2 + DRIED_FIBER x2 -> LIQUID_COPPER x1, 30s, MOD_LIQUID_SMELTER)" -ForegroundColor Green
}

# ────────────────────────────────────────────────────────────────
# ADVANCED COMPONENT PICKUPS: ALLOY_FRAME, CIRCUIT_BOARD, LARVAE_T4
# ────────────────────────────────────────────────────────────────
if ($content.Contains("ALLOY_FRAME")) {
    Write-Host "  prefabs.fods already contains ALLOY_FRAME, skipping." -ForegroundColor Yellow
} else {
    $alloyRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>ALLOY_FRAME</text:p></table:table-cell>
          <table:table-cell office:value-type="float" office:value="700" calcext:value-type="float"><text:p>700</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>CRAFTED</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_ALLOY_FRAME</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_ALLOY_FRAME_DESC</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="2" calcext:value-type="float"><text:p>2</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>SOLID</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="3" />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>Alloy Frame</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@
    $circuitRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>CIRCUIT_BOARD</text:p></table:table-cell>
          <table:table-cell office:value-type="float" office:value="701" calcext:value-type="float"><text:p>701</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>CRAFTED</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_CIRCUIT_BOARD</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_CIRCUIT_BOARD_DESC</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="1" calcext:value-type="float"><text:p>1</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>SOLID</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="3" />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>Circuit Board</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@
    $larvaeT4Row = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LARVAE_T4</text:p></table:table-cell>
          <table:table-cell office:value-type="float" office:value="702" calcext:value-type="float"><text:p>702</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LARVAE</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_LARVAE_T4</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>PICKUP_LARVAE_T4_DESC</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="1" calcext:value-type="float"><text:p>1</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LIVING</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="3" />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>T4 Omni-Ant Larva</text:p></table:table-cell>
          <table:table-cell table:number-columns-repeated="52" />
        </table:table-row>
"@

    # Insert after LIQUID_COPPER
    $lcPattern = "<text:p>LIQUID_COPPER</text:p>"
    $lcIdx = $content.IndexOf($lcPattern)
    if ($lcIdx -lt 0) { $lcIdx = $content.IndexOf("<text:p>BIOFUEL</text:p>") }
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $lcIdx)
    $insertPoint = $endIdx + $rowEnd.Length
    $content = $content.Insert($insertPoint, "`n" + $larvaeT4Row)
    $content = $content.Insert($insertPoint, "`n" + $circuitRow)
    $content = $content.Insert($insertPoint, "`n" + $alloyRow)
    Write-Host "  Injected ALLOY_FRAME, CIRCUIT_BOARD, LARVAE_T4 pickups" -ForegroundColor Green
}

# ────────────────────────────────────────────────────────────────
# ASSEMBLER RECIPES on COMBINER2:
#   ASSEMBLE_ALLOY:    IRON_PLATE x2 + LIQUID_IRON x1 → ALLOY_FRAME x1 (45s)
#   ASSEMBLE_CIRCUIT:  COPPER_WIRE x2 + LIQUID_COPPER x1 → CIRCUIT_BOARD x1 (45s)
#   ASSEMBLE_T4_LARVA: LARVAE_T3 x1 + ALLOY_FRAME x1 + CIRCUIT_BOARD x1 → LARVAE_T4 x1 (90s)
# ────────────────────────────────────────────────────────────────
if ($content.Contains("ASSEMBLE_ALLOY")) {
    Write-Host "  prefabs.fods already contains ASSEMBLE_ALLOY, skipping." -ForegroundColor Yellow
} else {
    $assembleAlloyRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>ASSEMBLE_ALLOY</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FACRECIPE_ASSEMBLE_ALLOY</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>IRON_PLATE 2, LIQUID_IRON 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>ALLOY_FRAME 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>COMBINER2</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="45" calcext:value-type="float"><text:p>45</text:p></table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell table:number-columns-repeated="55" />
        </table:table-row>
"@
    $assembleCircuitRow = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>ASSEMBLE_CIRCUIT</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FACRECIPE_ASSEMBLE_CIRCUIT</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>COPPER_WIRE 2, LIQUID_COPPER 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>CIRCUIT_BOARD 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>COMBINER2</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="45" calcext:value-type="float"><text:p>45</text:p></table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell table:number-columns-repeated="55" />
        </table:table-row>
"@
    $assembleT4Row = @"
        <table:table-row table:style-name="ro2">
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>ASSEMBLE_T4_LARVA</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>FACRECIPE_ASSEMBLE_T4_LARVA</text:p></table:table-cell>
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LARVAE_T3 1, ALLOY_FRAME 1, CIRCUIT_BOARD 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>LARVAE_T4 1</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="string" calcext:value-type="string"><text:p>COMBINER2</text:p></table:table-cell>
          <table:table-cell />
          <table:table-cell office:value-type="float" office:value="90" calcext:value-type="float"><text:p>90</text:p></table:table-cell>
          <table:table-cell table:style-name="ce286" />
          <table:table-cell table:number-columns-repeated="55" />
        </table:table-row>
"@
    # Find last COMBINER2 recipe
    $lastCombIdx = $content.LastIndexOf("<text:p>COMBINER, COMBINER2</text:p>")
    if ($lastCombIdx -lt 0) { $lastCombIdx = $content.LastIndexOf("<text:p>COMBINER2</text:p>") }
    $rowEnd = "</table:table-row>"
    $endIdx = $content.IndexOf($rowEnd, $lastCombIdx)
    $insertPoint = $endIdx + $rowEnd.Length
    $content = $content.Insert($insertPoint, "`n" + $assembleT4Row)
    $content = $content.Insert($insertPoint, "`n" + $assembleCircuitRow)
    $content = $content.Insert($insertPoint, "`n" + $assembleAlloyRow)
    Write-Host "  Injected ASSEMBLE_ALLOY recipe (45s, COMBINER2)" -ForegroundColor Green
    Write-Host "  Injected ASSEMBLE_CIRCUIT recipe (45s, COMBINER2)" -ForegroundColor Green
    Write-Host "  Injected ASSEMBLE_T4_LARVA recipe (90s, COMBINER2)" -ForegroundColor Green
}

# ── Write output ──
[System.IO.File]::WriteAllText($prefabsPath, $content)

# ── Also write a .modded copy for reference ──
$moddedPath = Join-Path $PSScriptRoot "prefabs.fods.modded"
Copy-Item $prefabsPath $moddedPath -Force

Write-Host ""
Write-Host "  prefabs.fods patched successfully!" -ForegroundColor Green
Write-Host "  Energy:     POD8(750) -> POD9(2000) -> POD10(5000) -> POD11(12500)" -ForegroundColor White
Write-Host "  Smelter:    IRON_RAW + DRIED_FIBER -> LIQUID_IRON  |  COPPER_RAW + DRIED_FIBER -> LIQUID_COPPER" -ForegroundColor White
Write-Host "  Assembler:  ALLOY_FRAME + CIRCUIT_BOARD + LARVAE_T3 -> LARVAE_T4 (T4 Omni-Ant)" -ForegroundColor White
