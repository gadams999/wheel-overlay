# Quickstart: Adding a New Overlay Application

**Audience**: Developers adding a second (or subsequent) overlay app to the monorepo
**Branch**: `001-opendash-monorepo-rebrand` | **Date**: 2026-03-18

This guide describes the complete steps to add a new overlay application. All steps are required unless marked optional.

---

## Step 1: Create Directory Structure

```bash
# Replace {AppName} with PascalCase (e.g., "DiscordNotify")
# Replace {app-name} with kebab-case (e.g., "discord-notify")

mkdir src/{AppName}/
mkdir tests/{AppName}.Tests/
mkdir installers/{app-name}/
mkdir scripts/{app-name}/
mkdir docs/{app-name}/
```

---

## Step 2: Create the Application Project

Create `src/{AppName}/{AppName}.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>OpenDash.{AppName}</RootNamespace>
    <Version>0.1.0</Version>
    <AssemblyVersion>0.1.0</AssemblyVersion>
    <FileVersion>0.1.0</FileVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\OverlayCore\OverlayCore.csproj" />
  </ItemGroup>
</Project>
```

**Constitution requirement**: Set `Version`, `AssemblyVersion`, and `FileVersion` independently. OverlayCore must NEVER have a `<Version>` element.

---

## Step 3: Create the Test Project

Create `tests/{AppName}.Tests/{AppName}.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <RootNamespace>OpenDash.{AppName}.Tests</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'FastTests'">
    <DefineConstants>FAST_TESTS</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\{AppName}\{AppName}.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="FsCheck" Version="2.16.6" />
    <PackageReference Include="FsCheck.Xunit" Version="2.16.6" />
  </ItemGroup>
</Project>
```

---

## Step 4: Add to Solution

```powershell
# Add application project
dotnet sln OpenDash-Overlays.sln add src/{AppName}/{AppName}.csproj --solution-folder src

# Add test project
dotnet sln OpenDash-Overlays.sln add tests/{AppName}.Tests/{AppName}.Tests.csproj --solution-folder tests
```

Verify `dotnet build` succeeds from repository root.

---

## Step 5: Initialize App Startup

In `src/{AppName}/Program.cs` or `App.xaml.cs`, follow the initialization order from Principle V:

```csharp
// 1. Initialize logging FIRST
LogService.Initialize("{AppName}");

// 2. Initialize other services
var themeService = new ThemeService();
var processMonitor = new ProcessMonitor(targetPath, TimeSpan.FromSeconds(1));

// 3. Register global hotkey (optional — all overlays inherit this from OverlayCore)
var hotkeyService = new GlobalHotkeyService();
hotkeyService.Register(windowHandle);
hotkeyService.ToggleModeRequested += OnToggleModeRequested;

// 4. Register settings categories
var settingsWindow = new MaterialSettingsWindow();
settingsWindow.RegisterCategory(new YourAppSettingsCategory(viewModel));
// AboutSettingsCategory auto-registered

// 5. Merge shared resources in App.xaml
// <ResourceDictionary Source="pack://application:,,,/OverlayCore;component/Resources/Fonts/SharedFontResources.xaml"/>
```

---

## Step 6: Create Build Scripts

Create `scripts/{app-name}/build_msi.ps1`:

```powershell
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$csproj = "$repoRoot\src\{AppName}\{AppName}.csproj"
$installer = "$repoRoot\installers\{app-name}"

# Publish, then build WiX MSI
dotnet publish $csproj --configuration Release --output "$repoRoot\Publish\{AppName}"
# ... WiX build commands
```

Create `scripts/{app-name}/build_release.ps1` similarly.

---

## Step 7: Create WiX Installer

Create `installers/{app-name}/Package.wxs` following the pattern from `installers/wheel-overlay/Package.wxs`. Key sections:
- `<Product>` with GUID and version sourced from `{AppName}.csproj`
- `<Directory>` targeting `Program Files\{AppName}`
- Shortcuts for Start Menu and Desktop

---

## Step 8: Create CI/CD Workflow

Create `.github/workflows/{app-name}-release.yml`:

```yaml
name: {AppName} Release
on:
  push:
    branches: [main]
    paths:
      - 'src/{AppName}/**'
      - 'src/OverlayCore/**'
      - 'tests/{AppName}.Tests/**'
      - 'tests/OverlayCore.Tests/**'
      - 'installers/{app-name}/**'
  push:
    tags:
      - '{app-name}/v*'
```

Follow the `wheel-overlay-release.yml` pattern for build, test, package, and release steps.

---

## Step 9: Create Documentation

Create the following files in `docs/{app-name}/`:
- `getting-started.md` — Installation, first launch, initial configuration
- `usage-guide.md` — Feature-by-feature explanation
- `tips.md` — Best practices
- `troubleshooting.md` — Common issues and solutions

---

## Step 10: Update Repository Documentation

- **README.md**: Add the new overlay to the "Applications" section and directory layout
- **CHANGELOG.md**: Add an `[Unreleased]` entry with `Added: {AppName} overlay application`
- **CONTRIBUTING.md**: No changes needed (conventions already documented)

---

## Verification Checklist

Before opening a PR:
- [ ] `dotnet build` succeeds from repository root
- [ ] `dotnet test --configuration FastTests` passes
- [ ] `.\scripts\Validate-PropertyTests.ps1 -TestProjectPath tests/{AppName}.Tests` passes
- [ ] Version set correctly in all three `.csproj` version properties
- [ ] CHANGELOG.md updated
- [ ] No `<Version>` in OverlayCore.csproj (verify it hasn't been accidentally added)
- [ ] Constitution Check completed in PR description
