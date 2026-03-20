# Implementation Plan: OpenDash Monorepo Rebrand

## Overview

This plan restructures the WheelOverlay repository into the OpenDash-Overlays monorepo. Tasks are ordered to establish the foundation (solution, directory structure, OverlayCore) first, then migrate WheelOverlay, add new features (settings framework, hotkey, fonts, docs), update CI/CD, and wire everything together. Each task builds incrementally on the previous ones.

## Tasks

- [x] 1. Create monorepo directory structure and solution file
  - [x] 1.1 Create the top-level directory layout and solution file
    - Create `OpenDash-Overlays.sln` at the repository root
    - Create directories: `src/OverlayCore/`, `src/WheelOverlay/`, `tests/OverlayCore.Tests/`, `tests/WheelOverlay.Tests/`, `installers/wheel-overlay/`, `scripts/wheel-overlay/`, `scripts/`, `docs/wheel-overlay/`, `assets/icons/`, `assets/rotary_knob/`
    - Add solution folders `src` and `tests` to the .sln
    - Move existing `assets/` content (icons, rotary_knob) into the new `assets/` directory
    - Remove the old `WheelOverlay.sln` file
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.6, 1.7, 10.1, 10.2, 10.5_

  - [x] 1.2 Create the OverlayCore class library project
    - Create `src/OverlayCore/OverlayCore.csproj` targeting `net10.0-windows` with `UseWPF`, `UseWindowsForms`, `RootNamespace=OpenDash.OverlayCore`, `Nullable=enable`, `ImplicitUsings=enable`
    - Add `System.Management` NuGet dependency
    - Do NOT add a `<Version>` property (OverlayCore is not independently versioned)
    - Create subdirectories: `Services/`, `Models/`, `Behaviors/`, `Settings/`, `Settings/Styles/`, `Resources/`, `Resources/Fonts/`
    - Add OverlayCore to the solution under the `src` solution folder
    - _Requirements: 2.1, 2.10, 2.11, 2.12, 7.3_

  - [x] 1.3 Create the OverlayCore.Tests project
    - Create `tests/OverlayCore.Tests/OverlayCore.Tests.csproj` referencing OverlayCore, xUnit, FsCheck, FsCheck.Xunit
    - Add `FastTests` build configuration with `FAST_TESTS` define constant
    - Add OverlayCore.Tests to the solution under the `tests` solution folder
    - _Requirements: 4.2, 4.3_

  - [x] 1.4 Configure solution build configurations
    - Add `Debug|Any CPU`, `FastTests|Any CPU`, and `Release|Any CPU` configurations to the solution
    - Ensure all four projects (OverlayCore, WheelOverlay, OverlayCore.Tests, WheelOverlay.Tests) participate in all configurations
    - Verify `dotnet build` from repository root builds all projects in correct dependency order
    - _Requirements: 1.4, 1.5, 10.3, 10.4, 10.6, 10.7_

- [x] 2. Checkpoint - Verify solution structure
  - Ensure `dotnet build` succeeds from the repository root with the new solution file. Ask the user if questions arise.

- [x] 3. Extract shared services into OverlayCore
  - [x] 3.1 Extract ThemeService to OverlayCore
    - Move ThemeService to `src/OverlayCore/Services/ThemeService.cs` under namespace `OpenDash.OverlayCore.Services`
    - Move `ThemePreference` enum to `src/OverlayCore/Models/ThemePreference.cs` under namespace `OpenDash.OverlayCore.Models`
    - Move `DarkTheme.xaml` and `LightTheme.xaml` to `src/OverlayCore/Resources/`
    - Update theme resource dictionary URIs to use pack URIs referencing OverlayCore assembly: `pack://application:,,,/OverlayCore;component/Resources/DarkTheme.xaml`
    - _Requirements: 2.2, 2.9, 2.10, 3.7, 8.1_

  - [x] 3.2 Write property test for ThemeService (Property 1)
    - **Property 1: Theme preference resolution is deterministic**
    - Generate random `ThemePreference` values and boolean system theme states
    - Verify: Light → false, Dark → true, System → matches system state
    - File: `tests/OverlayCore.Tests/ThemeServicePropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 2.2**

  - [x] 3.3 Extract LogService to OverlayCore
    - Move LogService to `src/OverlayCore/Services/LogService.cs` under namespace `OpenDash.OverlayCore.Services`
    - Parameterize the log path by app name via `Initialize(string appName)` method
    - Log files stored at `%APPDATA%/{appName}/logs.txt`
    - _Requirements: 2.3, 2.10_

  - [x] 3.4 Write property test for LogService (Property 2)
    - **Property 2: Log file never exceeds 1 MB plus one message**
    - Generate random strings of varying lengths as log messages
    - Verify log file size ≤ 1 MB + length of most recent message after each write
    - File: `tests/OverlayCore.Tests/LogServicePropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 2.3**

  - [x] 3.5 Extract ProcessMonitor to OverlayCore
    - Move ProcessMonitor to `src/OverlayCore/Services/ProcessMonitor.cs` under namespace `OpenDash.OverlayCore.Services`
    - API unchanged: `TargetApplicationStateChanged` event, `Start()`, `Stop()`, `UpdateTarget()`, `Dispose()`
    - _Requirements: 2.4, 2.10_

  - [x] 3.6 Write property test for ProcessMonitor (Property 3)
    - **Property 3: Process matching is consistent with path and filename rules**
    - Generate random file paths and process names with case variations
    - Verify: match by full path (case-insensitive) OR by filename (case-insensitive), else false
    - File: `tests/OverlayCore.Tests/ProcessMonitorPropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 2.4**

  - [x] 3.7 Extract WindowTransparencyHelper to OverlayCore
    - Create `src/OverlayCore/Services/WindowTransparencyHelper.cs` under namespace `OpenDash.OverlayCore.Services`
    - Consolidate Win32 interop (GetWindowLong, SetWindowLong, WS_EX_TRANSPARENT) from MainWindow.xaml.cs
    - Provide `MakeClickThrough()`, `RemoveClickThrough()`, `IsClickThrough()` static methods
    - _Requirements: 2.5, 2.10_

  - [x] 3.8 Extract ConfigModeBehavior to OverlayCore
    - Create `src/OverlayCore/Behaviors/ConfigModeBehavior.cs` under namespace `OpenDash.OverlayCore.Behaviors`
    - Implement Enter-to-confirm / Escape-to-cancel overlay repositioning pattern
    - Store original position on `Enter()`, enable drag, apply semi-transparent background
    - `Exit(true)` saves position; `Exit(false)` restores original
    - _Requirements: 2.6, 2.10_

  - [x] 3.9 Extract BaseOverlayWindow to OverlayCore
    - Create `src/OverlayCore/Behaviors/BaseOverlayWindow.cs` under namespace `OpenDash.OverlayCore.Behaviors`
    - Provide `ApplyOverlayDefaults(Window)` static method: Topmost, no taskbar, transparent background, SizeToContent, AllowsTransparency
    - _Requirements: 2.7, 2.10_

  - [x] 3.10 Extract SystemTrayScaffold to OverlayCore
    - Create `src/OverlayCore/Services/SystemTrayScaffold.cs` under namespace `OpenDash.OverlayCore.Services`
    - Provide reusable NotifyIcon setup: constructor with tooltip and icon, `SetContextMenu()`, `ShowBalloonTip()`, `Dispose()`
    - _Requirements: 2.8, 2.10_

- [x] 4. Checkpoint - Verify OverlayCore builds and extracted service tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Migrate WheelOverlay to reference OverlayCore
  - [x] 5.1 Update WheelOverlay project file and namespace
    - Move WheelOverlay source to `src/WheelOverlay/`
    - Update `WheelOverlay.csproj`: set `RootNamespace=OpenDash.WheelOverlay`, add `ProjectReference` to `../OverlayCore/OverlayCore.csproj`
    - Retain `Vortice.DirectInput` NuGet dependency
    - Retain independent `Version`, `AssemblyVersion`, `FileVersion` properties
    - Add WheelOverlay to the solution under the `src` solution folder
    - _Requirements: 3.1, 3.3, 3.4, 3.6, 8.2_

  - [x] 5.2 Update WheelOverlay namespaces and using directives
    - Change all `namespace WheelOverlay.*` to `namespace OpenDash.WheelOverlay.*`
    - Add `using OpenDash.OverlayCore.Services;`, `using OpenDash.OverlayCore.Models;`, `using OpenDash.OverlayCore.Behaviors;` where needed
    - Remove duplicated code that has been extracted into OverlayCore (ThemeService, LogService, ProcessMonitor, WindowTransparencyHelper inline P/Invoke, ConfigModeBehavior inline logic, SystemTrayScaffold inline setup)
    - Call `LogService.Initialize("WheelOverlay")` at startup to preserve `%APPDATA%\WheelOverlay\logs.txt` path
    - _Requirements: 3.2, 3.5, 3.7, 3.8, 8.2, 8.3_

  - [x] 5.3 Write property test for AppSettings serialization (Property 4)
    - **Property 4: AppSettings JSON serialization round-trip**
    - Generate random AppSettings with profiles, text labels, colors, layouts, theme preferences
    - Verify: serialize to JSON → deserialize back → equivalent object
    - Ensures namespace migration does not break existing `settings.json` files
    - File: `tests/WheelOverlay.Tests/AppSettingsSerializationPropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 3.8**

  - [x] 5.4 Migrate WheelOverlay.Tests
    - Move test project to `tests/WheelOverlay.Tests/`
    - Update `WheelOverlay.Tests.csproj` to reference WheelOverlay at new path
    - Update namespace references from `WheelOverlay.Services` to `OpenDash.OverlayCore.Services` for extracted services
    - Update namespace references from `WheelOverlay.*` to `OpenDash.WheelOverlay.*` for app-specific types
    - Maintain xUnit + FsCheck dependencies and FastTests/Release configuration
    - Add WheelOverlay.Tests to the solution under the `tests` solution folder
    - _Requirements: 4.1, 4.4, 4.5, 4.6, 4.7, 8.4_

- [x] 6. Checkpoint - Verify WheelOverlay builds and all existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Implement GlobalHotkeyService
  - [ ] 7.1 Create GlobalHotkeyService in OverlayCore
    - Create `src/OverlayCore/Services/GlobalHotkeyService.cs` under namespace `OpenDash.OverlayCore.Services`
    - Register Alt+F6 via Win32 `RegisterHotKey` / `UnregisterHotKey`
    - Use hidden WPF helper window with `HwndSource.AddHook` to receive `WM_HOTKEY` (0x0312) messages
    - Expose `ToggleModeRequested` event, `Register(IntPtr)`, `Unregister()`, `ProcessMessage()`, `Dispose()`
    - On registration failure: log descriptive error via LogService, return false, continue without hotkey
    - _Requirements: 16.1, 16.5, 16.6, 16.7_

  - [ ] 7.2 Wire GlobalHotkeyService into WheelOverlay MainWindow
    - Subscribe to `ToggleModeRequested` event in MainWindow
    - Toggle between overlay mode (click-through, topmost) and positioning mode (ConfigModeBehavior drag)
    - When transitioning from positioning → overlay via Alt+F6, confirm position (equivalent to Enter)
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [ ]* 7.3 Write property test for overlay mode state machine (Property 5)
    - **Property 5: Overlay mode state machine alternation**
    - Generate random initial modes and sequences of N toggle operations
    - Verify: even toggles → initial mode, odd toggles → opposite mode; positioning→overlay triggers confirm
    - File: `tests/OverlayCore.Tests/OverlayModePropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 2.6, 16.1, 16.4**

- [ ] 8. Implement Settings UI framework
  - [ ] 8.1 Create ISettingsCategory interface and use https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit MaterialSettingsWindow in OverlayCore
    - Create `src/OverlayCore/Settings/ISettingsCategory.cs` with `CategoryName`, `SortOrder`, `CreateContent()`, `SaveValues()`, `LoadValues()`
    - Create `src/OverlayCore/Settings/MaterialSettingsWindow.xaml` and `.xaml.cs` with left-side navigation list and right-side content area
    - Create `src/OverlayCore/Settings/Styles/MaterialStyles.xaml` with Material Design-inspired styles (rounded corners, elevation shadows, accent colors, smooth transitions)
    - Support keyboard navigation (Tab, arrow keys, Escape to close)
    - _Requirements: 12.1, 12.2, 12.3, 12.6, 12.9_

  - [ ] 8.2 Create AboutSettingsCategory in OverlayCore
    - Create `src/OverlayCore/Settings/AboutSettingsCategory.xaml` and `.xaml.cs`
    - Display application icon (theme-aware), version string (from calling assembly's AssemblyVersion), clickable GitHub link, close button
    - Set `SortOrder = 999` so it always appears last
    - _Requirements: 12.4, 12.5_

  - [ ]* 8.3 Write property test for settings category ordering (Property 7)
    - **Property 7: Settings categories are displayed in sort order and all registered categories appear**
    - Generate random sets of ISettingsCategory implementations with random sort orders
    - Verify: navigation list contains exactly the registered categories in ascending SortOrder; About (999) always last
    - File: `tests/OverlayCore.Tests/SettingsCategoryPropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 12.3, 12.7**

  - [ ] 8.4 Create WheelOverlay settings categories
    - Create `src/WheelOverlay/Settings/DisplaySettingsCategory.xaml(.cs)` (SortOrder=1): layout, profiles, DirectInput config
    - Create `src/WheelOverlay/Settings/AppearanceSettingsCategory.xaml(.cs)` (SortOrder=2): theme, colors, fonts
    - Create `src/WheelOverlay/Settings/AdvancedSettingsCategory.xaml(.cs)` (SortOrder=3): advanced options
    - Register all three categories plus About in MaterialSettingsWindow
    - Remove the old separate `SettingsWindow` and `AboutWindow` from WheelOverlay
    - _Requirements: 12.7, 12.8_

- [ ] 9. Implement shared font resources
  - [ ] 9.1 Create SharedFontResources and FontUtilities in OverlayCore
    - Create `src/OverlayCore/Resources/Fonts/SharedFontResources.xaml` with FontFamily, FontSize, FontWeight resource keys
    - Create `src/OverlayCore/Resources/Fonts/FontUtilities.cs` with `GetFontFamily(string)` (fallback to Segoe UI) and `ToFontWeight(string)` helpers
    - _Requirements: 13.1, 13.4, 13.5_

  - [ ] 9.2 Integrate SharedFontResources into WheelOverlay
    - Merge `SharedFontResources.xaml` into WheelOverlay's `App.xaml` via pack URI: `pack://application:,,,/OverlayCore;component/Resources/Fonts/SharedFontResources.xaml`
    - Replace WheelOverlay's local font definitions with references to shared font resource keys
    - _Requirements: 13.2, 13.3, 13.6_

  - [ ]* 9.3 Write property test for FontUtilities (Property 8)
    - **Property 8: FontUtilities helpers return valid results for all valid inputs**
    - Generate random non-empty font family name strings and valid font weight names
    - Verify: `GetFontFamily()` returns non-null FontFamily (Segoe UI fallback); `ToFontWeight()` returns correct FontWeight
    - File: `tests/OverlayCore.Tests/FontUtilitiesPropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 13.4**

- [ ] 10. Checkpoint - Verify settings framework, hotkey, and font resources work
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Restructure installers and build scripts
  - [ ] 11.1 Migrate WheelOverlay installer
    - Move `Package.wxs` and `CustomUI.wxs` to `installers/wheel-overlay/`
    - Update paths in WiX files to reference `src/WheelOverlay/` publish output
    - Verify installer still produces `WheelOverlay.msi` installing to `Program Files\WheelOverlay` with Start Menu and Desktop shortcuts
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [ ] 11.2 Migrate and update build scripts
    - Move `build_msi.ps1`, `build_release.ps1`, `generate_components.ps1` to `scripts/wheel-overlay/`
    - Update `build_msi.ps1` to reference `src/WheelOverlay/WheelOverlay.csproj` and `installers/wheel-overlay/`
    - Update `build_release.ps1` to read version from `src/WheelOverlay/WheelOverlay.csproj` and produce correctly named zip
    - Update `generate_components.ps1` to target `installers/wheel-overlay/`
    - Move shared scripts (`Validate-PropertyTests.ps1`, `Add-PropertyTestDirectives.ps1`) to top-level `scripts/`
    - Update shared scripts to accept a test project path parameter
    - All scripts resolve paths relative to repository root via `$repoRoot = Split-Path -Parent $PSScriptRoot`
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.6, 9.7, 9.8, 9.9_

- [ ] 12. Update CI/CD workflows
  - [ ] 12.1 Create wheel-overlay-release.yml workflow
    - Create `.github/workflows/wheel-overlay-release.yml` with path filters for `src/WheelOverlay/**`, `src/OverlayCore/**`, `tests/WheelOverlay.Tests/**`, `tests/OverlayCore.Tests/**`, `installers/wheel-overlay/**`
    - Add tag trigger for `wheel-overlay/v*` pattern
    - Add version extraction step: extract version from tag, compare to `src/WheelOverlay/WheelOverlay.csproj` Version, fail on mismatch
    - Create GitHub release with namespaced tag `wheel-overlay/vX.Y.Z`
    - Execute same build, test, and packaging steps for both path-triggered and tag-triggered releases
    - _Requirements: 6.1, 6.2, 6.3, 6.6, 6.7, 6.8, 7.2, 7.5, 14.1, 14.3, 14.4, 14.5, 14.6, 14.7_

  - [ ]* 12.2 Write property test for tag format round-trip (Property 6)
    - **Property 6: Namespaced tag format round-trip and version extraction**
    - Generate random app names (lowercase+hyphens, non-empty) and semver triples (non-negative integers)
    - Verify: format as `{app-name}/v{major}.{minor}.{patch}` → parse back → recover original app name and version
    - File: `tests/OverlayCore.Tests/TagFormatPropertyTests.cs`
    - Include `#if FAST_TESTS` directive for iteration count control
    - **Validates: Requirements 7.5, 14.3, 14.6**

  - [ ] 12.3 Create branch-check.yml workflow
    - Create `.github/workflows/branch-check.yml` triggering on PRs for all source and test paths
    - Run `dotnet build` and `dotnet test` with `FastTests` configuration (10 PBT iterations)
    - Validate property test directives using shared PowerShell validation script
    - _Requirements: 6.4, 6.5, 6.7, 6.8_

  - [ ] 12.4 Remove old CI/CD workflow files
    - Remove the old `release.yml` workflow that referenced the single-project structure
    - _Requirements: 6.1_

- [ ] 13. Create user documentation
  - [ ] 13.1 Create WheelOverlay user documentation
    - Create `docs/wheel-overlay/getting-started.md`: installation, first launch, initial configuration
    - Create `docs/wheel-overlay/usage-guide.md`: layout types (Single, Vertical, Horizontal, Grid, Dial), profile management, theme configuration
    - Create `docs/wheel-overlay/tips.md`: positioning, readability, performance tips for sim racing
    - Create `docs/wheel-overlay/troubleshooting.md`: overlay not appearing, DirectInput device not detected, settings not saving
    - All documentation in Markdown suitable for GitHub Pages
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.7, 15.8_

- [ ] 14. Update repository documentation
  - [ ] 14.1 Update README.md
    - Describe OpenDash-Overlays monorepo structure and purpose
    - Include build instructions referencing `OpenDash-Overlays.sln`
    - Document directory layout (`src/`, `tests/`, `installers/`, `assets/`, `scripts/`, `docs/`)
    - Explain how to add a new overlay application to the monorepo
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [ ] 14.2 Create CONTRIBUTING.md
    - Document branch naming convention with app-scoped prefixes (e.g., `feat/wheel-overlay/...`, `feat/overlay-core/...`)
    - Document namespaced git tag format for releases (`{app-name}/v{major}.{minor}.{patch}`)
    - _Requirements: 11.5, 11.6_

  - [ ] 14.3 Update CHANGELOG.md
    - Add entry documenting the monorepo restructuring, namespace migration, OverlayCore extraction, settings UI refactor, global hotkey, shared fonts, CI/CD changes, and documentation additions
    - _Requirements: 11.7_

- [ ] 15. Final integration and wiring
  - [ ] 15.1 Wire all components together in WheelOverlay App startup
    - Ensure `App.xaml.cs` / `Program.cs` initializes: `LogService.Initialize("WheelOverlay")`, ThemeService, ProcessMonitor, GlobalHotkeyService, SystemTrayScaffold, MaterialSettingsWindow with registered categories
    - Merge OverlayCore resource dictionaries (themes, fonts, material styles) in `App.xaml`
    - Verify MainWindow uses BaseOverlayWindow, ConfigModeBehavior, WindowTransparencyHelper from OverlayCore
    - Verify existing settings load from `%APPDATA%\WheelOverlay\settings.json` without migration
    - _Requirements: 3.2, 3.5, 3.7, 3.8, 16.1, 16.7_

  - [ ]* 15.2 Write unit tests for integration points
    - Test settings backward compatibility: load pre-migration `settings.json` fixture and verify deserialization
    - Test About category content: verify version text, GitHub link, and icon presence
    - Test hotkey registration failure: simulate failure and verify error logging without crash
    - Test namespace verification: grep source files for old `WheelOverlay.Services` namespace (should find none)
    - _Requirements: 3.8, 12.4, 16.6_

- [ ] 16. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after major milestones
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples, edge cases, and integration points
- The implementation language is C# with WPF, matching the existing codebase and design document
