# Generate WiX Components.wxs from SpeakerSight Publish directory
$ErrorActionPreference = "Stop"

# Resolve repo root (two levels up from scripts/speakersight/)
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$publishDir = Join-Path $repoRoot "Publish"
$outputFile = Join-Path $repoRoot "installers\speakersight\Components.wxs"

if (-not (Test-Path $publishDir)) {
    Write-Error "Publish directory not found: $publishDir. Run build_release.ps1 first."
    exit 1
}

$files = Get-ChildItem $publishDir -File -Exclude "*.pdb"

Write-Host "Generating component list for $($files.Count) files..." -ForegroundColor Cyan

$components = @()
foreach ($file in $files) {
    $components += @"
      <Component>
        <File Source="`$(var.SourceDir)\$($file.Name)" />
      </Component>
"@
}

$componentXml = $components -join ""

$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="AppComponents" Directory="INSTALLFOLDER">
$componentXml    </ComponentGroup>
  </Fragment>
</Wix>
"@

Set-Content -Path $outputFile -Value $wxsContent -Encoding UTF8
Write-Host "Components.wxs written to: $outputFile" -ForegroundColor Green
Write-Host "Component count: $($files.Count)" -ForegroundColor Green
