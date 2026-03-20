# Research: OpenDash Monorepo Rebrand

**Phase 0 Output** | Branch: `001-opendash-monorepo-rebrand` | Date: 2026-03-18

No unknowns required external research — the kiro design document and existing codebase provide all necessary decisions. This file consolidates findings by topic area.

---

## Decision 1: Global Hotkey Registration Pattern

**Decision**: Use Win32 `RegisterHotKey` / `UnregisterHotKey` with a hidden WPF helper window and `HwndSource.AddHook` to intercept `WM_HOTKEY` (0x0312) messages.

**Rationale**: This is the standard Windows-native approach for system-wide hotkeys in WPF applications. A hidden helper window avoids polluting the main window's WndProc and makes the service self-contained. Alt+F6 maps to `MOD_ALT` (0x0001) + `VK_F6` (0x75), hotkey ID constant 0x0001.

**Alternatives considered**: `InputManager.Current.PreProcessInput` (process-scoped only, not global); raw `WndProc` override on MainWindow (tight coupling). Both rejected for inability to receive hotkey messages when the app lacks focus.

**Failure path**: If `RegisterHotKey` returns false, the service logs `LogService.Error()` with the conflicting app context, returns `false` from `Register()`, and the application continues. System tray menu remains the fallback.

---

## Decision 2: Settings Framework Pattern

**Decision**: `ISettingsCategory` interface + `MaterialSettingsWindow` in OverlayCore. Overlay apps register category implementations at startup. `MaterialSettingsWindow` sorts by `SortOrder` and renders each category's `FrameworkElement` content in a right-side panel with left-side navigation `ListBox`.

**Rationale**: The registration pattern (rather than inheritance) allows overlay apps to add settings panels without subclassing the settings window. OverlayCore ships with `AboutSettingsCategory` (SortOrder=999) always last. WheelOverlay registers Display (1), Appearance (2), Advanced (3).

**Current state**: WheelOverlay has a 1292-line procedural `SettingsWindow.xaml.cs` with three logical categories. Migration path: extract each category into its own XAML UserControl that implements `ISettingsCategory`. Data bindings replace code-behind assignments.

**About window**: The existing `AboutWindow.xaml.cs` will be removed once `AboutSettingsCategory` in OverlayCore replaces it. The About category reads version from the calling assembly's `AssemblyFileVersion` attribute.

**Material Design styling**: Implemented via WPF XAML styles in `OverlayCore/Settings/Styles/MaterialStyles.xaml`. No third-party design toolkit dependency — rounded corners, elevation shadows, accent colors, and smooth transitions are achievable with native WPF `ControlTemplate`, `Style`, and `Storyboard` resources.

---

## Decision 3: Shared Font Resources Pattern

**Decision**: `SharedFontResources.xaml` resource dictionary in OverlayCore, merged into each overlay app's `App.xaml` via pack URI: `pack://application:,,,/OverlayCore;component/Resources/Fonts/SharedFontResources.xaml`. `FontUtilities.cs` provides `GetFontFamily(string)` (Segoe UI fallback) and `ToFontWeight(string)` helpers.

**Rationale**: Pack URIs are the standard WPF mechanism for referencing resources from a referenced assembly. Defining font resources in a shared XAML dictionary allows XAML bindings to reference keys like `{StaticResource OverlayFontFamily}` without per-app duplication.

**WheelOverlay migration**: Existing local font definitions in WheelOverlay XAML files are replaced with shared resource key references. AppSettings `FontFamily` and `FontSize` user-configured values continue to work through `FontUtilities.GetFontFamily()`.

---

## Decision 4: CI/CD Workflow Architecture

**Decision**: Replace single `release.yml` with two workflows:
1. `wheel-overlay-release.yml` — triggered by push to `main` with path filters OR by `wheel-overlay/v*` tags. Validates tag version vs `.csproj` version. Builds, tests, packages MSI, creates GitHub Release.
2. `branch-build-check.yml` — triggered on all PRs; runs `dotnet build` + `dotnet test --configuration FastTests`; validates property test directives.

**Tag version validation**: Extract version from tag with `-replace 'wheel-overlay/v', ''`; read `Version` from `src/WheelOverlay/WheelOverlay.csproj` using PowerShell XML parsing. Fail with `Write-Error` identifying both versions if they diverge.

**Path filters for WheelOverlay**: `src/WheelOverlay/**`, `src/OverlayCore/**`, `tests/WheelOverlay.Tests/**`, `tests/OverlayCore.Tests/**`, `installers/wheel-overlay/**`. OverlayCore changes trigger WheelOverlay release because WheelOverlay depends on OverlayCore.

**Current state of existing workflows**:
- `release.yml`: references `WheelOverlay/WheelOverlay.csproj` (wrong path), needs full replacement
- `branch-build-check.yml`: no path filters (runs for any file change), needs path filter addition
- `pre-merge-validation.yml`: status unknown — likely needs path filter updates

---

## Decision 5: Build Script Migration

**Decision**: Move `build_msi.ps1`, `build_release.ps1`, `generate_components.ps1` from root `scripts/` to `scripts/wheel-overlay/`. Update all path references from `.\WheelOverlay\WheelOverlay.csproj` to `.\src\WheelOverlay\WheelOverlay.csproj` and from root-relative `.\Package`, `.\installer` to `installers\wheel-overlay\`. All scripts resolve root via `$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)` (one level up from `scripts/wheel-overlay/`).

**Shared scripts remain in root**: `Validate-PropertyTests.ps1`, `Add-PropertyTestDirectives.ps1` stay in `scripts/` and are updated to accept a `-TestProjectPath` parameter to scan any test project, not just the WheelOverlay root.

**Installer WiX files**: `Package.wxs` and `CustomUI.wxs` move from current location to `installers/wheel-overlay/`. Output paths updated to reference `src/WheelOverlay/` publish artifacts.

---

## Decision 6: Property Test Coverage for Remaining Properties

Properties 1–4 are already implemented. Remaining:

| Property | Test File | Status |
|----------|-----------|--------|
| P5: Overlay mode state machine alternation | `tests/OverlayCore.Tests/OverlayModePropertyTests.cs` | ❌ Missing |
| P6: Namespaced tag format round-trip | `tests/OverlayCore.Tests/TagFormatPropertyTests.cs` | ❌ Missing |
| P7: Settings categories in sort order | `tests/OverlayCore.Tests/SettingsCategoryPropertyTests.cs` | ❌ Missing |
| P8: FontUtilities valid results for all inputs | `tests/OverlayCore.Tests/FontUtilitiesPropertyTests.cs` | ❌ Missing |

Per Principle II (constitution), property tests MUST be written before implementation is complete. All four tests must be added as part of this feature.

---

## Decision 7: MainWindow ConfigMode Wiring

**Current state**: `MainWindow.xaml.cs` has inline config mode logic (~60 lines) that stores position, toggles transparency, enables dragging, and handles Enter/Escape keystrokes. It does NOT use OverlayCore's `ConfigModeBehavior` yet.

**Migration path**: Introduce `GlobalHotkeyService` that fires `ToggleModeRequested`. In `MainWindow`, subscribe to the event and delegate to `ConfigModeBehavior.Enter(window)` / `ConfigModeBehavior.Exit(confirm: true/false)`. The inline `ConfigMode` property logic should remain coordinating the state flags, but delegate position saving, transparency, and keyboard handling to the OverlayCore types.

**Alt+F6 vs. system tray menu**: Both must work. System tray "Configure overlay position" menu item calls the same toggle path as the hotkey. This ensures backward compatibility for users who have not learned the hotkey.

---

## Decision 8: Placeholder Removal

`src/OverlayCore/Placeholder.cs` and `tests/OverlayCore.Tests/PlaceholderTests.cs` are scaffolding files. They are removed when real service files and tests are added to those projects (OverlayCore already has real services from the migration — Placeholder.cs can be removed now; PlaceholderTests.cs can be removed once the first real OverlayCore test is written, which has already happened with the property tests).

**Action**: Remove both placeholder files early in implementation.
