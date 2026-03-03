# Build and Publish Script for WheelOverlay
$ErrorActionPreference = "Stop"

# Resolve repo root (one level up from scripts/)
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$projectPath = ".\WheelOverlay\WheelOverlay.csproj"
$publishDir = ".\Publish"

Write-Host "Building and Publishing WheelOverlay..." -ForegroundColor Cyan

# Clean previous build
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

# Publish single file executable
dotnet publish $projectPath -c Release -r win-x64 -p:PublishSingleFile=true -p:TreatWarningsAsErrors=true --self-contained false -o $publishDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build Successful!" -ForegroundColor Green
    Write-Host "Executable is located at: $publishDir\WheelOverlay.exe" -ForegroundColor Green
    
    # Create a zip file for distribution
    $zipPath = ".\WheelOverlay_v0.6.0.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath
    Write-Host "Created distribution zip: $zipPath" -ForegroundColor Green
} else {
    Write-Host "Build Failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location
