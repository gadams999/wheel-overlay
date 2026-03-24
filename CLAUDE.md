# opendash-overlays Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-21

## Active Technologies
- C# 12 / .NET 10.0-windows + WPF (UI), WinForms (NotifyIcon/SystemTray), System.Management (WMI process monitoring), Vortice.DirectInput 3.8.2 (WheelOverlay only), FsCheck 2.16.6 + FsCheck.Xunit (property tests), xUnit 2.x, WiX 4.0.5 (MSI installer), GitHub Actions (001-opendash-monorepo-rebrand)
- JSON settings at `%APPDATA%\WheelOverlay\settings.json`; log file at `%APPDATA%\WheelOverlay\logs.txt` (1 MB rotation) (001-opendash-monorepo-rebrand)
- MaterialDesignThemes v5.3.1 / MaterialDesignThemes.Wpf assembly (MD2 style set, WheelOverlay settings window) (wheel-overlay/v0.7.0)

- C# 12 / .NET 10.0-windows + WPF (UI), WinForms (NotifyIcon/SystemTray), System.Management (WMI), Vortice.DirectInput (WheelOverlay only), xUnit, FsCheck 2.16.6, WiX 4 (MSI installer), GitHub Actions (001-opendash-monorepo-rebrand)

## Project Structure

```text
OpenDash-Overlays.sln    — root solution, build from here
src/
  OverlayCore/           — shared class library (no <Version>; ProjectReference only)
  WheelOverlay/          — sim racing rotary encoder overlay app (v0.6.0)
tests/
  OverlayCore.Tests/     — property tests for shared services (FsCheck)
  WheelOverlay.Tests/    — unit + property tests for WheelOverlay
installers/wheel-overlay/ — WiX 4 MSI installer
scripts/                 — shared scripts (Validate-PropertyTests.ps1)
scripts/wheel-overlay/   — per-app build scripts
docs/wheel-overlay/      — user documentation
assets/wheel-overlay/    — WheelOverlay icons and source images
```

## Commands

```bash
# Build all projects
dotnet build OpenDash-Overlays.sln

# Run tests (PR mode — 10 PBT iterations)
dotnet test --configuration FastTests

# Run tests (release mode — 100 PBT iterations)
dotnet test --configuration Release

# Validate property test directives (run before committing)
powershell -File scripts/Validate-PropertyTests.ps1

# Build MSI installer
powershell -File scripts/wheel-overlay/build_msi.ps1
```

## Code Style

- Namespaces: `OpenDash.OverlayCore.*` for shared lib, `OpenDash.WheelOverlay.*` for app
- Nullable enabled; implicit usings enabled
- Property tests MUST include comment: `// Feature: {feature}, Property {N}: {title}`
- Property tests MUST use `#if FAST_TESTS / #else` for iteration counts (10/100)
- All service init failures MUST call `LogService.Error()` — never silently swallow
- `LogService.Initialize("{AppName}")` MUST be first call at startup
- OverlayCore MUST NOT have `<Version>` in its .csproj

## Recent Changes
- wheel-overlay/v0.7.0: Added MaterialDesignThemes v5.3.1 (MD2) — visual refactor of settings window; new MaterialDesignBootstrap helper in OverlayCore; ThemeService extended with PaletteHelper sync
- 001-opendash-monorepo-rebrand: Added C# 12 / .NET 10.0-windows + WPF (UI), WinForms (NotifyIcon/SystemTray), System.Management (WMI process monitoring), Vortice.DirectInput 3.8.2 (WheelOverlay only), FsCheck 2.16.6 + FsCheck.Xunit (property tests), xUnit 2.x, WiX 4.0.5 (MSI installer), GitHub Actions

- 001-opendash-monorepo-rebrand: Added C# 12 / .NET 10.0-windows + WPF (UI), WinForms (NotifyIcon/SystemTray), System.Management (WMI), Vortice.DirectInput (WheelOverlay only), xUnit, FsCheck 2.16.6, WiX 4 (MSI installer), GitHub Actions

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
