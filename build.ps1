# RimTalk-Quests Build Script
# This script helps build the mod for RimWorld

param(
    [string]$GameVersion = "1.6",
    [string]$Configuration = "Debug",
    [string]$RimWorldPath = "D:\SteamLibrary\steamapps\common\RimWorld",
    [switch]$UseNuGet = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RimTalk-Quests Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Use provided path or environment variable
if ([string]::IsNullOrEmpty($RimWorldPath)) {
    $RimWorldPath = $env:RIMWORLD_DIR
}

# Check if RimWorld path is set (only required when not using NuGet)
if ([string]::IsNullOrEmpty($RimWorldPath) -and -not $UseNuGet) {
    Write-Host "ERROR: RimWorld path not set." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please specify path: .\build.ps1 -RimWorldPath 'C:\Path\To\RimWorld'" -ForegroundColor Yellow
    Write-Host "Or use NuGet mode: .\build.ps1 -UseNuGet" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Validate paths when using local DLLs
if (-not $UseNuGet) {
    if (-not (Test-Path $RimWorldPath)) {
        Write-Host "ERROR: RimWorld directory not found at: $RimWorldPath" -ForegroundColor Red
        exit 1
    }

    Write-Host "RimWorld Path: $RimWorldPath" -ForegroundColor Green
    Write-Host "Game Version:  $GameVersion" -ForegroundColor Green
    Write-Host "Configuration: $Configuration" -ForegroundColor Green
} else {
    Write-Host "Build Mode:    NuGet packages (no local DLLs)" -ForegroundColor Yellow
    Write-Host "Game Version:  $GameVersion" -ForegroundColor Green
    Write-Host "Configuration: $Configuration" -ForegroundColor Green
}

Write-Host ""

# Clean old build artifacts
Write-Host "Cleaning old build artifacts..." -ForegroundColor Cyan
Remove-Item "obj" -Recurse -ErrorAction SilentlyContinue
Remove-Item "$GameVersion\Assemblies\*.dll" -ErrorAction SilentlyContinue

# Build the project
Write-Host "Building project..." -ForegroundColor Cyan

if ($UseNuGet) {
    # Build with NuGet packages (no local DLLs required)
    $buildArgs = @(
        "build",
        "Source\RimAI.Quests.csproj",
        "/p:GameVersion=$GameVersion",
        "/p:Configuration=$Configuration",
        "/p:UseLocalDlls=false"
    )
} else {
    # Build with local DLLs (full static analysis)
    $buildArgs = @(
        "build",
        "Source\RimAI.Quests.csproj",
        "/p:GameVersion=$GameVersion",
        "/p:Configuration=$Configuration",
        "/p:RimWorldDir=$RimWorldPath",
        "/p:UseLocalDlls=true"
    )
}

& dotnet $buildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Build Successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Output: $GameVersion\Assemblies\RimAI.Quests.dll" -ForegroundColor Green
    
    $modDestPath = Join-Path $RimWorldPath "Mods\RimAI.Quests"
    if (Test-Path $modDestPath) {
        Write-Host "Deployed to: $modDestPath" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit $LASTEXITCODE
}

