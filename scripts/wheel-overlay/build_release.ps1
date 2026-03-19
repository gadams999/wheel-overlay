# Build and Publish Script for WheelOverlay
$ErrorActionPreference = "Stop"

# Resolve repo root (two levels up from scripts/wheel-overlay/)
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot

$projectPath = ".\src\WheelOverlay\WheelOverlay.csproj"
$publishDir = ".\Publish"

Write-Host "Building and Publishing WheelOverlay..." -ForegroundColor Cyan

# Parse version from csproj
[xml]$csproj = Get-Content $projectPath
$version = $csproj.Project.PropertyGroup.Version

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
    $zipPath = ".\WheelOverlay_v$version.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath
    Write-Host "Created distribution zip: $zipPath" -ForegroundColor Green
} else {
    Write-Host "Build Failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

# --- MSI Installer Build ---
Write-Host ""
Write-Host "Preparing MSI installer build..." -ForegroundColor Cyan

$packageDir = ".\Package"
$installerDir = ".\installers\wheel-overlay"
$assetsDir = ".\assets"

# Ensure Package directory exists
if (-not (Test-Path $packageDir)) {
    New-Item -ItemType Directory -Path $packageDir | Out-Null
}

# Copy installer WiX source files into Package/
Write-Host "Copying installer sources into Package/..." -ForegroundColor Yellow
Copy-Item -Path "$installerDir\*.wxs" -Destination $packageDir -Force
Write-Host "  Copied *.wxs files"

# Copy .wix/ config directory into Package/ (if present)
$wixConfigSrc = "$installerDir\.wix"
$wixConfigDst = "$packageDir\.wix"
if (Test-Path $wixConfigSrc) {
    if (Test-Path $wixConfigDst) {
        Remove-Item $wixConfigDst -Recurse -Force
    }
    Copy-Item -Path $wixConfigSrc -Destination $wixConfigDst -Recurse -Force
    Write-Host "  Copied .wix/ config directory"
}

# Copy app icon from assets into Package/
Copy-Item -Path "$assetsDir\app.ico" -Destination $packageDir -Force -ErrorAction SilentlyContinue
Write-Host "  Copied app.ico from assets/"

Write-Host "Installer sources staged in Package/ successfully." -ForegroundColor Green

Pop-Location
