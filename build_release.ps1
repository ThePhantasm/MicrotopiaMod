# ============================================================
# Colony Spire Mod - Release Builder
# ============================================================
# Creates a distributable zip that non-programmers can install.
# The zip contains everything needed - no .NET SDK required.
#
# Usage:  .\build_release.ps1
# Output: ColonySpireMod_v{version}.zip in this directory
# ============================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameDir   = (Resolve-Path "$scriptDir\..\..").Path
$version   = "1.1.22"

Write-Host ""
Write-Host "=== Colony Spire Mod - Release Builder ===" -ForegroundColor Cyan
Write-Host ""

# --- Step 1: Build the plugin in Release mode ---
Write-Host "[1/4] Building plugin (Release)..." -ForegroundColor Yellow
$pluginSrcDir = Join-Path $scriptDir "ColonySpirePlugin"
Push-Location $pluginSrcDir
try {
    dotnet build ColonySpirePlugin.csproj -c Release --no-incremental
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed!" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}
$compiledDll = Join-Path $pluginSrcDir "bin\Release\netstandard2.1\ColonySpirePlugin.dll"
if (-not (Test-Path $compiledDll)) {
    Write-Host "ERROR: Compiled DLL not found!" -ForegroundColor Red
    exit 1
}
Write-Host "  Build successful." -ForegroundColor Green

# --- Step 2: Create staging directory ---
Write-Host "[2/4] Staging release files..." -ForegroundColor Yellow
$stagingDir = Join-Path $scriptDir "release_staging"
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item $stagingDir -ItemType Directory | Out-Null

$modDir = Join-Path $stagingDir "ColonySpireMod"
New-Item $modDir -ItemType Directory | Out-Null

# Copy the compiled plugin
Copy-Item $compiledDll $modDir
Write-Host "  Copied: ColonySpirePlugin.dll" -ForegroundColor Gray

# Copy modded prefabs
$moddedPrefabs = Join-Path $scriptDir "prefabs.fods.modded"
if (Test-Path $moddedPrefabs) {
    Copy-Item $moddedPrefabs $modDir
    Write-Host "  Copied: prefabs.fods.modded" -ForegroundColor Gray
}

# Copy BepInEx framework from the game directory
$bepinexSrc = Join-Path $gameDir "BepInEx"
$doorstopSrc = Join-Path $gameDir "doorstop_config.ini"
$winhttpSrc  = Join-Path $gameDir "winhttp.dll"

$bepinexDst = Join-Path $modDir "BepInEx"
New-Item $bepinexDst -ItemType Directory | Out-Null

# Only copy the core BepInEx files (not cache/logs/etc)
$bepinexCoreSrc = Join-Path $bepinexSrc "core"
if (Test-Path $bepinexCoreSrc) {
    Copy-Item $bepinexCoreSrc (Join-Path $bepinexDst "core") -Recurse
    Write-Host "  Copied: BepInEx/core/" -ForegroundColor Gray
}



# Copy doorstop files
if (Test-Path $doorstopSrc) {
    Copy-Item $doorstopSrc $modDir
    Write-Host "  Copied: doorstop_config.ini" -ForegroundColor Gray
}
if (Test-Path $winhttpSrc) {
    Copy-Item $winhttpSrc $modDir
    Write-Host "  Copied: winhttp.dll" -ForegroundColor Gray
}

# Copy README and LICENSE
foreach ($f in @("README.md", "LICENSE")) {
    $src = Join-Path $scriptDir $f
    if (Test-Path $src) { Copy-Item $src $modDir }
}

# --- Step 3: Create the user-friendly installer ---
Write-Host "[3/4] Creating user installer..." -ForegroundColor Yellow

$installerContent = @'
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
'@

$installerPath = Join-Path $modDir "install.ps1"
Set-Content -Path $installerPath -Value $installerContent -Encoding UTF8
Write-Host "  Created: install.ps1" -ForegroundColor Green

# --- Step 4: Create the zip ---
Write-Host "[4/4] Creating release zip..." -ForegroundColor Yellow

$zipName = "ColonySpireMod_v$version.zip"
$zipPath = Join-Path $scriptDir $zipName

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Start-Sleep -Seconds 2 # Wait for AV scan to release lock on new DLL
Compress-Archive -Path "$modDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

# Cleanup staging
Remove-Item $stagingDir -Recurse -Force

$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)

Write-Host ""
Write-Host "=== Release build complete! ===" -ForegroundColor Green
Write-Host "  Output: $zipName ($zipSize MB)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Distribution instructions:" -ForegroundColor White
Write-Host "  1. Share the zip file" -ForegroundColor Gray
Write-Host "  2. User extracts the zip" -ForegroundColor Gray
Write-Host "  3. User right-clicks install.ps1 -> 'Run with PowerShell'" -ForegroundColor Gray
Write-Host "  4. Done! No .NET SDK or coding knowledge needed." -ForegroundColor Gray
Write-Host ""
