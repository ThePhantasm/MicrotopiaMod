# ============================================================
# Microtopia Queen Tier + Colony Spire Mod - Installer
# ============================================================
# This script applies all mod changes to a clean Microtopia install.
#
# Usage:
#   1. Copy the Mod folder into your Microtopia\Mods directory
#   2. Open PowerShell in the game directory
#   3. Run:  .\Mods\[YourModFolder]\install.ps1
#
# What it does:
#   Step 1: Builds the BepInEx plugin (ColonySpirePlugin.dll)
#   Step 2: Copies the plugin to the BepInEx plugins folder
#   Step 3: Copies the modded prefabs.fods into place (factory recipe changes)
#
# Prerequisites:
#   - BepInEx installed in the game directory
#   - .NET SDK installed (for compiling)
# ============================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameDir   = (Resolve-Path "$scriptDir\..\..").Path

Write-Host ""
Write-Host "=== Microtopia Queen Tier + Colony Spire Mod Installer ===" -ForegroundColor Cyan
Write-Host "Game directory: $gameDir" -ForegroundColor Gray
Write-Host ""

# --- Validate game directory ---
$bepinexPath = Join-Path $gameDir "BepInEx\plugins"
$prefabsPath = Join-Path $gameDir "Microtopia_Data\StreamingAssets\prefabs.fods"

if (-not (Test-Path $bepinexPath)) {
    Write-Host "ERROR: Cannot find BepInEx\plugins folder at:" -ForegroundColor Red
    Write-Host "  $bepinexPath" -ForegroundColor Red
    Write-Host "Make sure you have installed BepInEx into the game directory first!" -ForegroundColor Yellow
    exit 1
}

# --- Step 1: Create backup for prefabs --
Write-Host "[1/3] Backing up vanilla files..." -ForegroundColor Yellow

$prefabsBackup = "$prefabsPath.backup"
if (-not (Test-Path $prefabsBackup)) {
    Copy-Item $prefabsPath $prefabsBackup
    Write-Host "  Created: prefabs.fods.backup" -ForegroundColor Green
} else {
    Write-Host "  Backup already exists: prefabs.fods.backup (skipped)" -ForegroundColor Gray
}

# --- Step 2: Build & Install Plugin ---
Write-Host ""
Write-Host "[2/3] Building BepInEx Plugin..." -ForegroundColor Yellow

# Clean up any stale duplicate DLLs in BepInEx subfolders
# BepInEx scans recursively — having the same plugin in both plugins/ and plugins/ColonySpire/
# causes a "Skipping because a newer version exists" conflict and unpredictable loading.
$stalePaths = @(
    (Join-Path $bepinexPath "ColonySpire\ColonySpirePlugin.dll"),
    (Join-Path $bepinexPath "ColonySpire")
)
foreach ($stale in $stalePaths) {
    if (Test-Path $stale) {
        Remove-Item $stale -Recurse -Force
        Write-Host "  Removed stale duplicate: $stale" -ForegroundColor Yellow
    }
}

$pluginSrcDir = Join-Path $scriptDir "ColonySpirePlugin"
Push-Location $pluginSrcDir
try {
    # Build using the specific csproj to avoid hitting Modder.csproj
    dotnet build ColonySpirePlugin.csproj -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
    
    $compiledDll = Join-Path $pluginSrcDir "bin\Release\netstandard2.1\ColonySpirePlugin.dll"
    if (Test-Path $compiledDll) {
        Copy-Item $compiledDll $bepinexPath -Force
        Write-Host "  Plugin installed to BepInEx successfully." -ForegroundColor Green
    } else {
        Write-Host "ERROR: Compiled DLL not found at expected path: $compiledDll" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

# --- Step 3: Copy modded prefabs.fods ---
Write-Host ""
Write-Host "[3/3] Applying prefabs.fods changes (factory recipes)..." -ForegroundColor Yellow

$moddedPrefabs = Join-Path $scriptDir "prefabs.fods.modded"
if (Test-Path $moddedPrefabs) {
    Copy-Item $moddedPrefabs $prefabsPath -Force
    Write-Host "  prefabs.fods updated with mod recipes." -ForegroundColor Green
} else {
    Write-Host "  WARNING: prefabs.fods.modded not found - skipping recipe changes." -ForegroundColor Yellow
}

# --- Done ---
Write-Host ""
Write-Host "=== Installation complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Mod features:" -ForegroundColor Cyan
Write-Host "  - Colony Spire (Radar Tower): press 1-7 to select upgrade track, U to upgrade"
Write-Host "  - Phase 12 Additions: T2 Mining Ants and Spire Energy Efficiency"
Write-Host "  ...and much more!"
Write-Host ""
Write-Host "To safely uninstall, run uninstall.ps1 in your mod folder." -ForegroundColor Gray
Write-Host ""
