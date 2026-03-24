# ============================================================
# Colony Spire Mod - Easy Installer
# ============================================================
# 
# HOW TO USE:
#   1. Right-click this file and select "Run with PowerShell"
#      OR open PowerShell in this folder and type: .\install.ps1
#   2. When prompted, enter your Microtopia game folder path
#      (e.g. D:\SteamLibrary\steamapps\common\Microtopia)
#   3. That's it! Launch the game.
#
# To find your game folder:
#   Steam -> Right-click Microtopia -> Manage -> Browse Local Files
# ============================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Colony Spire Mod Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- Auto-detect or prompt for game directory ---
$defaultPaths = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Microtopia",
    "C:\Program Files\Steam\steamapps\common\Microtopia",
    "D:\SteamLibrary\steamapps\common\Microtopia",
    "D:\Steam\steamapps\common\Microtopia",
    "E:\SteamLibrary\steamapps\common\Microtopia"
)

$gameDir = $null
foreach ($p in $defaultPaths) {
    if (Test-Path (Join-Path $p "Microtopia.exe")) {
        $gameDir = $p
        Write-Host "Auto-detected game at: $gameDir" -ForegroundColor Green
        break
    }
}

if (-not $gameDir) {
    Write-Host "Could not auto-detect the game folder." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To find it: Steam -> Right-click Microtopia -> Manage -> Browse Local Files" -ForegroundColor Gray
    Write-Host ""
    $gameDir = Read-Host "Enter your Microtopia game folder path"
    $gameDir = $gameDir.Trim('"').Trim("'")
}

# Validate
$exePath = Join-Path $gameDir "Microtopia.exe"
if (-not (Test-Path $exePath)) {
    Write-Host ""
    Write-Host "ERROR: Microtopia.exe not found in '$gameDir'" -ForegroundColor Red
    Write-Host "Please make sure you entered the correct path." -ForegroundColor Red
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Installing to: $gameDir" -ForegroundColor Cyan
Write-Host ""

# --- Install BepInEx (mod framework) ---
Write-Host "[1/3] Installing BepInEx mod framework..." -ForegroundColor Yellow

$bepinexDst = Join-Path $gameDir "BepInEx"
if (-not (Test-Path (Join-Path $bepinexDst "core"))) {
    Copy-Item (Join-Path $scriptDir "BepInEx") $bepinexDst -Recurse -Force
    Write-Host "  BepInEx installed." -ForegroundColor Green
} else {
    Write-Host "  BepInEx already present (skipped)." -ForegroundColor Gray
}

# Copy doorstop loader files
foreach ($f in @("doorstop_config.ini", "winhttp.dll")) {
    $src = Join-Path $scriptDir $f
    $dst = Join-Path $gameDir $f
    if ((Test-Path $src) -and -not (Test-Path $dst)) {
        Copy-Item $src $dst -Force
        Write-Host "  Copied: $f" -ForegroundColor Green
    }
}

# --- Install plugin ---
Write-Host "[2/3] Installing Colony Spire plugin..." -ForegroundColor Yellow

$pluginsDst = Join-Path $bepinexDst "plugins"
if (-not (Test-Path $pluginsDst)) { New-Item $pluginsDst -ItemType Directory | Out-Null }

Copy-Item (Join-Path $scriptDir "ColonySpirePlugin.dll") $pluginsDst -Force
Write-Host "  ColonySpirePlugin.dll installed." -ForegroundColor Green

# --- Install modded prefabs ---
Write-Host "[3/3] Installing modded game data..." -ForegroundColor Yellow

$prefabsPath = Join-Path $gameDir "Microtopia_Data\StreamingAssets\prefabs.fods"
$prefabsBackup = "$prefabsPath.backup"
$moddedPrefabs = Join-Path $scriptDir "prefabs.fods.modded"

if (Test-Path $moddedPrefabs) {
    # Backup original
    if (-not (Test-Path $prefabsBackup)) {
        Copy-Item $prefabsPath $prefabsBackup
        Write-Host "  Backed up original prefabs.fods" -ForegroundColor Gray
    }
    Copy-Item $moddedPrefabs $prefabsPath -Force
    Write-Host "  Game data updated." -ForegroundColor Green
}

# --- Done ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Installation complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Launch Microtopia and enjoy!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Features (all can be toggled in World Settings):" -ForegroundColor White
Write-Host "  - Prestige System: Queen tiers, Colony Spire upgrades" -ForegroundColor Gray
Write-Host "  - Concrete Island Combat: Corpse health & shield generators" -ForegroundColor Gray
Write-Host "  - Colored Trails: Color variants for Main Bus trails" -ForegroundColor Gray
Write-Host "  - Battery Gates: Stockpile gates can target batteries" -ForegroundColor Gray
Write-Host ""
Write-Host "To uninstall: restore prefabs.fods.backup and remove" -ForegroundColor Gray
Write-Host "BepInEx/plugins/ColonySpirePlugin.dll" -ForegroundColor Gray
Write-Host ""
Read-Host "Press Enter to exit"
