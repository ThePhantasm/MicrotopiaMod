# ============================================================
# Microtopia Colony Spire Mod - TechTree Patcher
# ============================================================
# Injects custom research nodes into techtree.fods:
#   1. MOD_QUEEN_T2  - Unlocks T2 Queen Larvae (mid-game)
#   2. MOD_QUEEN_T3  - Unlocks T3 Queen Larvae (late-game)
#   3. MOD_COLORED_TRAILS - Unlocks Colored Trail paths (mid-game)
#   4. MOD_COLONY_SPIRE - Unlocks Colony Spire building (late-game)
# ============================================================

param(
    [string]$GameDir = ""
)

$ErrorActionPreference = "Stop"

if (-not $GameDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $GameDir = (Resolve-Path "$scriptDir\..\..").Path
}

$techTreePath = Join-Path $GameDir "Microtopia_Data\StreamingAssets\techtree.fods"
$backupPath   = "$techTreePath.backup"

if (-not (Test-Path $techTreePath)) {
    Write-Host "ERROR: Cannot find techtree.fods at: $techTreePath" -ForegroundColor Red
    exit 1
}

# Create backup if it doesn't exist
if (-not (Test-Path $backupPath)) {
    Copy-Item $techTreePath $backupPath
    Write-Host "  Created: techtree.fods.backup" -ForegroundColor Green
} else {
    Write-Host "  Backup already exists: techtree.fods.backup (skipped)" -ForegroundColor Gray
}

Write-Host "Patching techtree.fods..." -ForegroundColor Yellow

$content = [System.IO.File]::ReadAllText($techTreePath)

# Check if already patched
if ($content.Contains("MOD_QUEEN_T2")) {
    Write-Host "  TechTree already patched (MOD_QUEEN_T2 found). Skipping." -ForegroundColor Gray
    exit 0
}

# ────────────────────────────────────────────────────────────────
# Build the XML rows to inject into the TechTree sheet.
# Each row follows the column layout:
#   A=Code, B=Required_Tech, C=Title, D=Desc, E=Tech_Type,
#   F=Hidden, G=Cost, H=Task, I=unlock_buildings, J=unlock_recipes,
#   K=unlock_trailtypes, L=unlock_general
# ────────────────────────────────────────────────────────────────

# Helper: build a simple data row in the same style as existing rows
function New-TechRow {
    param(
        [string]$Code,
        [string]$RequiredTech = "",
        [string]$Title = "",
        [string]$Desc = "",
        [string]$TechType = "",
        [string]$Hidden = "",
        [string]$Cost = "",
        [string]$Task = "",
        [string]$UnlockBuildings = "",
        [string]$UnlockRecipes = "",
        [string]$UnlockTrailTypes = "",
        [string]$UnlockGeneral = ""
    )

    # Build cells. Empty cells use the short-form empty element
    function Cell([string]$value, [string]$style = "Default") {
        if ($value) {
            return "     <table:table-cell table:style-name=`"$style`" office:value-type=`"string`" calcext:value-type=`"string`"><text:p>$value</text:p>`n     </table:table-cell>"
        } else {
            return "     <table:table-cell table:style-name=`"$style`"/>"
        }
    }

    $xml = @"
    <table:table-row table:style-name="ro2">
$(Cell $Code "Default")
$(Cell $RequiredTech "ce228")
$(Cell $Title "Default")
$(Cell $Desc "Default")
$(Cell $TechType "Default")
$(Cell $Hidden "ce339")
$(Cell $Cost "ce357")
$(Cell $Task "ce357")
$(Cell $UnlockBuildings "Default")
$(Cell $UnlockRecipes "Default")
     <table:table-cell table:style-name="Default" table:number-columns-repeated="2"/>
     <table:table-cell table:style-name="ce403" table:number-columns-repeated="3"/>
     <table:table-cell table:style-name="Default" table:number-columns-repeated="49"/>
     <table:table-cell table:number-columns-repeated="960"/>
    </table:table-row>
"@
    return $xml
}

# ── Comment row (section header) ──
$commentRow = @"
    <table:table-row table:style-name="ro2">
     <table:table-cell table:style-name="ce219" office:value-type="string" calcext:value-type="string">
      <text:p>// COLONY SPIRE MOD</text:p>
     </table:table-cell>
     <table:table-cell table:style-name="ce265"/>
     <table:table-cell table:number-columns-repeated="1022"/>
    </table:table-row>
"@

# 1. MOD_QUEEN_T2: Unlock T2 larvae on Queen (requires vanilla T2 larvae research)
#    Gated by: Player researched T2 small workers → now Queen can cycle to T2
$row1 = New-TechRow -Code "MOD_QUEEN_T2" `
    -RequiredTech "ANT_SMALLWORKER_T2" `
    -Title "TECHTREE_MOD_QUEEN_T2" `
    -Desc "TECHTREE_MOD_QUEEN_T2_DESC" `
    -TechType "GENERAL" `
    -Cost "REGULAR_T1 80"

# 2. MOD_QUEEN_T3: Unlock T3 larvae on Queen (requires vanilla T3 small worker tech)
#    Gated by: Player researched T3 small workers → late-game Queen upgrade
$row2 = New-TechRow -Code "MOD_QUEEN_T3" `
    -RequiredTech "ANT_WORKERSMALL_T3" `
    -Title "TECHTREE_MOD_QUEEN_T3" `
    -Desc "TECHTREE_MOD_QUEEN_T3_DESC" `
    -TechType "GENERAL" `
    -Cost "REGULAR_T2 100, GYNE_T1 3"

# 3. MOD_COLORED_TRAILS: Unlock colored trail paths (requires MAIN trail tech)
#    Gated by: Player has the Main Bus trail → rewarded with color customization
$row3 = New-TechRow -Code "MOD_COLORED_TRAILS" `
    -RequiredTech "TRAIL_MAIN" `
    -Title "TECHTREE_MOD_COLORED_TRAILS" `
    -Desc "TECHTREE_MOD_COLORED_TRAILS_DESC" `
    -TechType "TRAIL" `
    -Cost "REGULAR_T1 50"

# 4. MOD_COLONY_SPIRE: Unlock Colony Spire building (requires Sentinel research — deep endgame)
#    Gated by: Player can make Sentinels → now unlocks the prestige building
$row4 = New-TechRow -Code "MOD_COLONY_SPIRE" `
    -RequiredTech "ANT_SENTINEL" `
    -Title "TECHTREE_MOD_COLONY_SPIRE" `
    -Desc "TECHTREE_MOD_COLONY_SPIRE_DESC" `
    -TechType "BUILDING" `
    -Cost "REGULAR_T3 200, GYNE_T1 5" `
    -UnlockBuildings "COLONY_SPIRE"

# 5. MOD_T4_ENDGAME: Gate all T4 endgame content behind T3 inventor research
#    Gated by: Colony Spire built + T3 inventor points + T2 Gyne launches
#    Unlocks: Island Furnace, Deep Excavator, ENERGY_POD11 on Dynamos
$row5 = New-TechRow -Code "MOD_T4_ENDGAME" `
    -RequiredTech "MOD_COLONY_SPIRE" `
    -Title "TECHTREE_MOD_T4_ENDGAME" `
    -Desc "TECHTREE_MOD_T4_ENDGAME_DESC" `
    -TechType "GENERAL" `
    -Cost "REGULAR_T3 150, GYNE_T2 3"

$newRows = $commentRow + $row1 + $row2 + $row3 + $row4 + $row5

# ────────────────────────────────────────────────────────────────
# INJECTION: Find the filler rows ("number-rows-repeated=4") that
# come right after the last real data in the TechTree sheet, and 
# insert our new rows just before them.
# ────────────────────────────────────────────────────────────────

# The filler row pattern marks the end of real data
$fillerPattern = '    <table:table-row table:style-name="ro2" table:number-rows-repeated="4">'

if (-not $content.Contains($fillerPattern)) {
    Write-Host "ERROR: Could not find filler row pattern in techtree.fods. File format may have changed." -ForegroundColor Red
    exit 1
}

# Insert our rows just before the filler block
$content = $content.Replace($fillerPattern, "$newRows$fillerPattern")

[System.IO.File]::WriteAllText($techTreePath, $content)

Write-Host "  Injected 5 custom tech nodes into TechTree sheet:" -ForegroundColor Green
Write-Host "    MOD_QUEEN_T2       - Queen T2 Larvae (req ANT_SMALLWORKER_T2, cost 80 T1)" -ForegroundColor White
Write-Host "    MOD_QUEEN_T3       - Queen T3 Larvae (req ANT_WORKERSMALL_T3, cost 100 T2 + 3 Gyne)" -ForegroundColor White
Write-Host "    MOD_COLORED_TRAILS - Colored Trail Paths (req TRAIL_MAIN, cost 50 T1)" -ForegroundColor White
Write-Host "    MOD_COLONY_SPIRE   - Colony Spire (req ANT_SENTINEL, cost 200 T3 + 5 Gyne)" -ForegroundColor White
Write-Host "    MOD_T4_ENDGAME     - T4 Endgame Mastery (req MOD_COLONY_SPIRE, cost 150 T3 + 3 Gyne T2)" -ForegroundColor White
