# RimTalk-Quests Clean Script
# This script helps clean build cache and symbol cache

# Clean dotnet build artifacts
dotnet clean RimAI.Quests.csproj

# Clean VS Code symbol cache (decompiled files)
$symbolCachePath = "$env:LOCALAPPDATA\Temp\SymbolCache"
if (Test-Path $symbolCachePath) {
    Write-Host "Cleaning symbol cache at: $symbolCachePath" -ForegroundColor Yellow
    Remove-Item -Path $symbolCachePath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Symbol cache cleaned successfully" -ForegroundColor Green
} else {
    Write-Host "Symbol cache not found, skipping..." -ForegroundColor Gray
}

# Restore dependencies
dotnet restore RimAI.Quests.csproj