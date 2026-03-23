# ============================================================
# Microtopia Queen Tier + Colony Spire Mod — Uninstaller
# ============================================================
# Removes the BepInEx plugin and restores vanilla prefabs.fods
# from backups created by install.ps1.
#
# Usage:  .\Mods\QueenTierMod\uninstall.ps1
# ============================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameDir   = (Resolve-Path "$scriptDir\..\..").Path

Write-Host ""
Write-Host "=== Microtopia Mod Uninstaller ===" -ForegroundColor Cyan
Write-Host "Game directory: $gameDir" -ForegroundColor Gray
Write-Host ""

$bepinexDll  = Join-Path $gameDir "BepInEx\plugins\ColonySpirePlugin.dll"
$prefabsPath = Join-Path $gameDir "Microtopia_Data\StreamingAssets\prefabs.fods"
$prefabsBackup = "$prefabsPath.backup"

if (Test-Path $bepinexDll) {
    Remove-Item $bepinexDll -Force
    Write-Host "  Removed: BepInEx\plugins\ColonySpirePlugin.dll" -ForegroundColor Green
} else {
    Write-Host "  Plugin DLL not found (already removed)" -ForegroundColor Yellow
}

if (Test-Path $prefabsBackup) {
    Copy-Item $prefabsBackup $prefabsPath -Force
    Write-Host "  Restored: prefabs.fods" -ForegroundColor Green
} else {
    Write-Host "  No backup found for prefabs.fods. Use Steam -> Verify to reset." -ForegroundColor Yellow
}

$techTreePath = Join-Path $gameDir "Microtopia_Data\StreamingAssets\techtree.fods"
$techTreeBackup = "$techTreePath.backup"
if (Test-Path $techTreeBackup) {
    Copy-Item $techTreeBackup $techTreePath -Force
    Write-Host "  Restored: techtree.fods" -ForegroundColor Green
} else {
    Write-Host "  No backup found for techtree.fods. Use Steam -> Verify to reset." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Uninstallation complete ===" -ForegroundColor Green
Write-Host ""
