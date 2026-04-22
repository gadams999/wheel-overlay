# Build and Publish Script for SpeakerSight
# Full release build: clean, build, test (Release), build MSI, sign if cert available
$ErrorActionPreference = "Stop"

# Resolve repo root (two levels up from scripts/speakersight/)
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot

$projectPath = ".\src\SpeakerSight\SpeakerSight.csproj"
$publishDir = ".\Publish"

Write-Host "=== SpeakerSight Release Build ===" -ForegroundColor Cyan
Write-Host ""

# Parse version from csproj
[xml]$csproj = Get-Content $projectPath
$version = $csproj.Project.PropertyGroup.Version
Write-Host "Version: $version" -ForegroundColor White
Write-Host ""

# Step 1: Run tests (Release configuration — 100 PBT iterations)
Write-Host "[1/4] Running tests (Release)..." -ForegroundColor Yellow
dotnet test --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed! Aborting release build." -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "Tests passed" -ForegroundColor Green
Write-Host ""

# Step 2: Publish
Write-Host "[2/4] Publishing application..." -ForegroundColor Yellow

# Clean previous build
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:TreatWarningsAsErrors=true -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "Application published to: $publishDir\SpeakerSight.exe" -ForegroundColor Green
Write-Host ""

# Step 3: Build MSI
Write-Host "[3/4] Building MSI installer..." -ForegroundColor Yellow
powershell -File "$PSScriptRoot\build_msi.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "MSI build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "MSI built: .\Package\SpeakerSight-v$version.msi" -ForegroundColor Green
Write-Host ""

# Step 4: Sign (if certificate available)
Write-Host "[4/4] Signing binaries..." -ForegroundColor Yellow
$signtool = Get-Command signtool -ErrorAction SilentlyContinue
if ($signtool) {
    Write-Host "  signtool found — skipping (no cert configured)" -ForegroundColor Gray
    Write-Host "  To sign: signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 Package\SpeakerSight-v$version.msi" -ForegroundColor Gray
} else {
    Write-Host "  signtool not found — skipping code signing" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Release Build Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Artifacts:" -ForegroundColor Cyan
Write-Host "  MSI:         .\Package\SpeakerSight-v$version.msi" -ForegroundColor White
Write-Host "  Executable:  .\Publish\SpeakerSight.exe" -ForegroundColor White
Write-Host ""

Pop-Location
