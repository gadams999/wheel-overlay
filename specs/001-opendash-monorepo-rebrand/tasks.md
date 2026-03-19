# Tasks: OpenDash Monorepo Rebrand

**Input**: Design documents from `specs/001-opendash-monorepo-rebrand/`
**Branch**: `001-opendash-monorepo-rebrand` | **Date**: 2026-03-18
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.
**Property tests**: Included per Constitution Principle II (mandatory — not optional).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in the same group)
- **[Story]**: Which user story this task belongs to ([US1]–[US6])
- Exact file paths included in every description

---

## Phase 0: Version Bump *(first commit — Constitution §III)*

**Purpose**: Version bump MUST be the first commit on the branch before any implementation begins.

- [x] T000 Bump WheelOverlay version to `0.7.0` in `src/WheelOverlay/WheelOverlay.csproj` — set all three required properties: `<Version>0.7.0</Version>`, `<AssemblyVersion>0.7.0.0</AssemblyVersion>`, `<FileVersion>0.7.0.0</FileVersion>`; commit as `chore(wheel-overlay): bump version to 0.7.0`; this commit MUST precede all other implementation commits on this branch

---

## Phase 1: Setup

**Purpose**: Remove scaffolding and update shared tooling before any user story work begins.

- [x] T001 [P] Delete scaffold file `src/OverlayCore/Placeholder.cs` (no longer needed now that real OverlayCore services exist)
- [x] T002 [P] Delete scaffold file `tests/OverlayCore.Tests/PlaceholderTests.cs` (replaced by real property tests)
- [x] T003 Update `scripts/Validate-PropertyTests.ps1` — add optional `-TestProjectPath` parameter (default: `tests/WheelOverlay.Tests`) so the script can validate any test project; update CI workflow calls to pass `-TestProjectPath` explicitly for both test projects

---

## Phase 2: Foundational (Shared Font Infrastructure)

**Purpose**: Create shared font resources consumed by both WheelOverlay views (US3) and MaterialSettingsWindow (US5). Must complete before those stories begin.

**⚠️ CRITICAL**: US3 (font migration) and US5 (settings UI) cannot begin until this phase is complete.

- [x] T004 [P] Create `src/OverlayCore/Resources/Fonts/SharedFontResources.xaml` — XAML ResourceDictionary defining keys: `OverlayFontFamily` (Segoe UI), `RobotoFontFamily` (Roboto), `MonospaceFontFamily` (Consolas), `OverlayFontSizeSmall` (12.0), `OverlayFontSizeMedium` (16.0), `OverlayFontSizeLarge` (20.0), `OverlayFontSizeXLarge` (28.0), `OverlayFontWeightNormal` (Normal), `OverlayFontWeightBold` (Bold); embed the Roboto font file(s) in `src/OverlayCore/Resources/Fonts/` as `<Resource>` build action so the pack URI resolves correctly; per `contracts/SharedFontResources.md` and FR-025
- [x] T005 [P] Create `src/OverlayCore/Resources/Fonts/FontUtilities.cs` — static class `OpenDash.OverlayCore.Resources.Fonts.FontUtilities` with `GetFontFamily(string? familyName) → FontFamily` (Segoe UI fallback, never null) and `ToFontWeight(string? weightName) → FontWeight` (Normal fallback for null/empty/unrecognized input); per `contracts/SharedFontResources.md`
- [x] T006 Write property test P8 in `tests/OverlayCore.Tests/FontUtilitiesPropertyTests.cs` — comment: `// Feature: OpenDash Monorepo Rebrand, Property 8: FontUtilities returns valid results for all string inputs`; use `#if FAST_TESTS` (10 iterations) / `#else` (100 iterations); property: `GetFontFamily(anyString)` → non-null `FontFamily`; property: `ToFontWeight(validWeightName)` → correct `FontWeight`; verify Segoe UI and Normal fallbacks for null/empty/unrecognized inputs; depends on T005

**Checkpoint**: `dotnet build` succeeds; P8 property tests pass; pack URI `pack://application:,,,/OverlayCore;component/Resources/Fonts/SharedFontResources.xaml` resolves at runtime

---

## Phase 3: User Story 1 — Add a New Overlay App (Priority: P1) 🎯 MVP

**Goal**: A developer can add a second overlay app stub under `src/`, wire it to OverlayCore, confirm it builds, and confirm pushing a namespaced version tag triggers only that app's CI/CD pipeline — without modifying WheelOverlay.

**Independent Test**: Add stub project `src/DiscordNotify/` with `ProjectReference` to OverlayCore; `dotnet build` succeeds; verify `discord-notify-release.yml` path filters do not include WheelOverlay paths; verify `branch-build-check.yml` runs on PR changes to `src/**`.

### Implementation for User Story 1

- [x] T007 [P] [US1] Create `.github/workflows/discord-notify-release.yml` — placeholder workflow per spec clarification: path filters scoped to `src/DiscordNotify/**`, `src/OverlayCore/**`, `tests/DiscordNotify.Tests/**`; tag trigger `discord-notify/v*`; single job echoing "discord-notify placeholder pipeline — no source implemented yet" and exiting 0; no app source code under `src/` required
- [x] T008 [P] [US1] Update `.github/workflows/branch-build-check.yml` — add `paths` filter to push (branches-ignore: [main]) and pull_request triggers covering `src/**`, `tests/**`, `scripts/**`, `.github/workflows/**`; update CSPROJ path references to `src/WheelOverlay/WheelOverlay.csproj`; pass `-TestProjectPath tests/WheelOverlay.Tests` and `-TestProjectPath tests/OverlayCore.Tests` to `Validate-PropertyTests.ps1` using parameter from T003
- [x] T009 [US1] Create `CONTRIBUTING.md` at repository root — document: branch naming convention `<type>/<description>` (valid prefixes: feat/, fix/, docs/, test/, refactor/, chore/, perf/) with examples; versioning approach (bump `<Version>` in `.csproj` as first commit on branch, SemVer rules); release tag format `{app-name}/vX.Y.Z`; link to `specs/001-opendash-monorepo-rebrand/quickstart.md` for adding a new overlay app; Constitution Check requirement for every PR description

**Checkpoint**: `branch-build-check.yml` triggers correctly on PR with path changes to `src/**`; `discord-notify-release.yml` exists with correct path isolation from WheelOverlay; `CONTRIBUTING.md` covers FR-030 and FR-031 requirements

---

## Phase 4: User Story 2 — Release WheelOverlay Without Disrupting Other Work (Priority: P1)

**Goal**: Pushing `wheel-overlay/v0.7.0` tag triggers only WheelOverlay's pipeline; pipeline validates tag version matches `.csproj`; builds MSI; creates GitHub release with installer attached. Mismatch fails with clear error.

**Independent Test**: Push `wheel-overlay/v0.7.0` tag with matching version in `WheelOverlay.csproj` → pipeline produces MSI and GitHub release. Push with mismatched version → pipeline fails with clear error identifying both versions.

### Implementation for User Story 2

- [x] T010 [US2] Write property test P6 in `tests/OverlayCore.Tests/TagFormatPropertyTests.cs` — comment: `// Feature: OpenDash Monorepo Rebrand, Property 6: Namespaced tag format round-trips correctly`; use `#if FAST_TESTS` / `#else`; property: format `{app-name}/v{major}.{minor}.{patch}` then parse → recovers original app-name and all three version components; property: invalid formats (no prefix, missing `v`, underscore instead of hyphen) do not parse as valid
- [x] T011 [US2] Create `.github/workflows/wheel-overlay-release.yml` — triggers: push to `main` with paths `src/WheelOverlay/**`, `src/OverlayCore/**`, `tests/WheelOverlay.Tests/**`, `tests/OverlayCore.Tests/**`, `installers/wheel-overlay/**`; AND push tags `wheel-overlay/v*`; job 1 `build-and-test`: validate property directives for both test projects (T003 parameter), `dotnet build --configuration Release`, `dotnet test --configuration Release`; job 2 `package-and-release` (needs job 1): parse `<Version>` from `src/WheelOverlay/WheelOverlay.csproj` via PowerShell XML; if tag-triggered validate tag version vs csproj version, fail with `Write-Error` on mismatch; run `scripts/wheel-overlay/build_msi.ps1`; create GitHub release via `softprops/action-gh-release@v1`, attach MSI and zip; release notes from CHANGELOG.md version section; per `contracts/CICDWorkflows.md`
- [x] T012 [US2] Delete `.github/workflows/release.yml` — superseded by `wheel-overlay-release.yml`; only delete after T011 is committed and CI confirms the new workflow is valid
- [x] T013 [P] [US2] Update `scripts/wheel-overlay/build_msi.ps1` — replace all references to old root-relative `WheelOverlay\WheelOverlay.csproj` with `src\WheelOverlay\WheelOverlay.csproj`; update installer source from `.\installer` to `.\installers\wheel-overlay`; confirm `$ErrorActionPreference = "Stop"` present
- [x] T014 [P] [US2] Update `scripts/wheel-overlay/build_release.ps1` — same path corrections as T013: `src\WheelOverlay\WheelOverlay.csproj`, updated publish and output paths; confirm `$ErrorActionPreference = "Stop"` present
- [x] T015 [P] [US2] Update `scripts/wheel-overlay/generate_components.ps1` — update input path to WheelOverlay publish output under `src\WheelOverlay\`; update WiX components output file path to `installers\wheel-overlay\Components.wxs`; confirm `$ErrorActionPreference = "Stop"` present
- [x] T016 [US2] Create `installers/wheel-overlay/Package.wxs` — WiX 4 package definition; `<Package>` with `ProductCode="*"` (auto-generated per build) and a **fixed, stable** `UpgradeCode` GUID (generate once with `[System.Guid]::NewGuid()` and hard-code it — MUST NOT change across versions, per NFR-002); version from `WheelOverlay.csproj`, manufacturer "Gavin Adams"; `<StandardDirectory>` targeting `ProgramFilesFolder`; `<Directory>` `OpenDash\WheelOverlay`; `<Feature>` referencing all component groups from `Components.wxs`; Start Menu and Desktop shortcuts; icon reference; `<MajorUpgrade DowngradeErrorMessage="..."/>` to enforce in-place upgrade and block downgrades
- [x] T017 [US2] Create `installers/wheel-overlay/CustomUI.wxs` — WiX 4 minimal UI extension (WixUI_Minimal bootstrapper or equivalent) supporting simple install/uninstall flow; WiX 4 syntax (not WiX 3 `<Product>`)

**Checkpoint**: `wheel-overlay-release.yml` path filters correctly isolate WheelOverlay from other apps; version validation step fails with clear error on mismatch; `release.yml` deleted; build scripts reference `src/WheelOverlay/`; WiX files present and `build_msi.ps1` succeeds locally

---

## Phase 5: User Story 3 — WheelOverlay Users Experience No Regression (Priority: P1)

**Goal**: After all monorepo changes, an existing user's `%APPDATA%\WheelOverlay\settings.json` loads without any migration step; all 65+ existing tests pass; overlay renders as before with correct fonts.

**Independent Test**: Copy a pre-migration `settings.json` to `%APPDATA%\WheelOverlay\`; launch WheelOverlay from new build; verify all settings applied without prompts; verify overlay appears at saved position with all 5 layouts rendering correctly.

### Implementation for User Story 3

- [ ] T018 [US3] Merge `SharedFontResources.xaml` into `src/WheelOverlay/App.xaml` — add `<ResourceDictionary Source="pack://application:,,,/OverlayCore;component/Resources/Fonts/SharedFontResources.xaml"/>` as first entry in `App.xaml` `MergedDictionaries`; preserve existing DarkTheme.xaml / LightTheme.xaml merges ordered after SharedFontResources; depends on T004
- [ ] T019 [P] [US3] Replace local font definitions in `src/WheelOverlay/Views/DialLayout.xaml` — substitute hardcoded `FontFamily`, `FontSize`, `FontWeight` attribute literals with `{StaticResource OverlayFontFamily}`, `{StaticResource OverlayFontSizeLarge}`, etc.; preserve runtime bindings that read from `AppSettings.FontFamily`/`AppSettings.FontSize` (resolved via `FontUtilities.GetFontFamily()`, not static keys); depends on T018
- [ ] T020 [P] [US3] Replace local font definitions in `src/WheelOverlay/Views/SingleTextLayout.xaml` — same substitution as T019; depends on T018
- [ ] T021 [P] [US3] Replace local font definitions in `src/WheelOverlay/Views/VerticalLayout.xaml` — same substitution as T019; depends on T018
- [ ] T022 [P] [US3] Replace local font definitions in `src/WheelOverlay/Views/HorizontalLayout.xaml` — same substitution as T019; depends on T018
- [ ] T023 [P] [US3] Replace local font definitions in `src/WheelOverlay/Views/GridLayout.xaml` — same substitution as T019; depends on T018
- [ ] T024 [US3] Run `dotnet test --configuration FastTests` and confirm all existing `WheelOverlay.Tests` and `OverlayCore.Tests` pass after font migration; fix any XAML resource lookup failures before proceeding; depends on T019–T023

**Checkpoint**: `dotnet test --configuration FastTests` fully green; all 5 layout views render with shared font keys; settings.json round-trip unchanged (serialization invariant Property 4 still passes)

---

## Phase 6: User Story 4 — Reposition the Overlay With a Keyboard Shortcut (Priority: P2)

**Goal**: Alt+F6 registered globally; pressing it cycles WheelOverlay between normal click-through mode and positioning mode; position saved when exiting positioning mode via hotkey; graceful degradation if key unavailable.

**Independent Test**: With WheelOverlay running behind another full-screen application, press Alt+F6 — overlay becomes draggable (red border, semi-transparent background); drag to new position; press Alt+F6 again — overlay returns to click-through; restart — overlay appears at new position.

### Implementation for User Story 4

- [ ] T025 [US4] Write property test P5 in `tests/OverlayCore.Tests/OverlayModePropertyTests.cs` — comment: `// Feature: OpenDash Monorepo Rebrand, Property 5: Overlay mode state machine alternates correctly`; use `#if FAST_TESTS` / `#else`; property: for any initial mode ∈ {OverlayMode, PositioningMode} and N ≥ 0 toggles, resulting mode = initial XOR (N is odd); property: transitioning PositioningMode → OverlayMode via toggle always triggers confirm (position-save) semantics, not cancel; per `contracts/GlobalHotkeyService.md` state machine
- [ ] T026 [US4] Create `src/OverlayCore/Services/GlobalHotkeyService.cs` — `OpenDash.OverlayCore.Services` namespace; implements `IDisposable`; constants: `HOTKEY_ID = 0x0001`, `MOD_ALT = 0x0001`, `VK_F6 = 0x75`, `WM_HOTKEY = 0x0312`; `Register(IntPtr hwnd) → bool`: calls P/Invoke `RegisterHotKey`; on failure calls `LogService.Error($"Failed to register global hotkey Alt+F6. Another application may be using this key combination. Error code: {Marshal.GetLastWin32Error()}")` and returns false; no exception thrown; `Unregister()`: calls `UnregisterHotKey`, safe to call if Register returned false; `ProcessMessage(int msg, IntPtr wParam)`: fires `ToggleModeRequested` event when `msg == WM_HOTKEY && wParam == HOTKEY_ID`; `Dispose()`: calls `Unregister()`; `public event EventHandler? ToggleModeRequested`; per `contracts/GlobalHotkeyService.md`
- [ ] T027 [US4] Wire `GlobalHotkeyService` in `src/WheelOverlay/MainWindow.xaml.cs` — instantiate in `Loaded` event handler (HWND available); call `_hotkeyService.Register(new WindowInteropHelper(this).Handle)`; subscribe `ToggleModeRequested` to `ToggleOverlayMode()` method; add `HwndSource.AddHook` to route `WM_HOTKEY` messages to `_hotkeyService.ProcessMessage()`; `ToggleOverlayMode()` checks `_configMode` and delegates to `ConfigModeBehavior.Enter(this)` or `ConfigModeBehavior.Exit(confirm: true)`; dispose in `Closed` handler; ensure system tray "Configure overlay position" item also calls `ToggleOverlayMode()`; depends on T026
- [ ] T028 [US4] Verify hotkey graceful degradation in `src/WheelOverlay/MainWindow.xaml.cs` — review `Register()` return value handling: if false, no further hotkey action taken; confirm `LogService.Error()` is called; confirm system tray positioning menu item still functions as fallback; depends on T027

**Checkpoint**: Alt+F6 toggles overlay mode globally; position persists after restart; registration failure logged with descriptive message and app continues; system tray fallback unaffected

---

## Phase 7: User Story 5 — Configure WheelOverlay Through a Modern Settings Window (Priority: P2)

**Goal**: Settings window with left-side navigation shows Display (1), Appearance (2), Advanced (3), About (999) categories; apps register categories at startup; About replaces separate About dialog; keyboard arrow-key navigation works.

**Independent Test**: Open settings via system tray; verify 4 categories in correct sort order; verify About shows version string and clickable GitHub link; verify all existing settings controls accessible and save correctly; verify up/down arrow keys navigate category list.

### Implementation for User Story 5

- [ ] T029 [US5] Write property test P7 in `tests/OverlayCore.Tests/SettingsCategoryPropertyTests.cs` — comment: `// Feature: OpenDash Monorepo Rebrand, Property 7: Settings categories display in ascending SortOrder`; use `#if FAST_TESTS` / `#else`; property: for any N registered `ISettingsCategory` instances with distinct SortOrder values, the navigation list contains exactly N+1 entries (N app categories + `AboutSettingsCategory` at 999), displayed in ascending SortOrder regardless of registration order; per `contracts/ISettingsCategory.md`
- [ ] T030 [P] [US5] Create `src/OverlayCore/Settings/ISettingsCategory.cs` — `OpenDash.OverlayCore.Settings` namespace; public interface with `string CategoryName { get; }`, `int SortOrder { get; }`, `FrameworkElement CreateContent()`, `void SaveValues()`, `void LoadValues()`; XML doc comments per `contracts/ISettingsCategory.md`
- [ ] T031 [P] [US5] Create `src/OverlayCore/Settings/Styles/MaterialStyles.xaml` — WPF ResourceDictionary with: `ListBox` style for side-navigation (fixed ~180px width, item padding, no visible scrollbar chrome); `ListBoxItem` style with 4px rounded corners, hover and selected-state background using accent color from theme; content area `Border` style; settings `Window` style (MinWidth=680, MinHeight=480); all using native WPF `ControlTemplate` and `Style` — no third-party toolkit dependency
- [ ] T032 [US5] Create `src/OverlayCore/Settings/MaterialSettingsWindow.xaml` and `MaterialSettingsWindow.xaml.cs` — `OpenDash.OverlayCore.Settings` namespace; XAML: two-column `Grid` — left `ListBox` bound to sorted categories collection, right `ContentControl` bound to selected category's `CreateContent()` result; OK/Apply/Cancel buttons; merge `MaterialStyles.xaml` in window resources; code-behind: `RegisterCategory(ISettingsCategory)` adds to internal `ObservableCollection<ISettingsCategory>` sorted by `SortOrder`; constructor auto-registers `AboutSettingsCategory`; `ListBox.SelectionChanged` calls `SaveValues()` on previous, `LoadValues()` on new category; OK and Apply call `SaveValues()` on all; Cancel discards; depends on T030, T031
- [ ] T033 [US5] Create `src/OverlayCore/Settings/AboutSettingsCategory.cs` — implements `ISettingsCategory`; `CategoryName = "About"`, `SortOrder = 999`; `CreateContent()` returns `StackPanel` containing: app name `TextBlock`, version `TextBlock` reading `Assembly.GetEntryAssembly()!.GetName().Version!.ToString(3)`, `TextBlock` with `Hyperlink` to the GitHub repository URL; `SaveValues()` and `LoadValues()` are no-ops; depends on T030
- [ ] T034 [P] [US5] Create `src/WheelOverlay/Settings/DisplaySettingsCategory.cs` — implements `ISettingsCategory`; `CategoryName = "Display"`, `SortOrder = 1`; `CreateContent()` returns a WPF `UserControl` containing all layout-selection controls (radio buttons / combo for DisplayLayout enum) and device-selection controls migrated from `SettingsWindow.xaml.cs`; `LoadValues()` / `SaveValues()` delegate to `SettingsViewModel`; depends on T030
- [ ] T035 [P] [US5] Create `src/WheelOverlay/Settings/AppearanceSettingsCategory.cs` — implements `ISettingsCategory`; `CategoryName = "Appearance"`, `SortOrder = 2`; `CreateContent()` returns `UserControl` containing colors (selected/non-selected text color pickers) and fonts (font-family picker using `FontUtilities.GetFontFamily()`, font-size input) migrated from `SettingsWindow.xaml.cs`; depends on T030, T005
- [ ] T036 [P] [US5] Create `src/WheelOverlay/Settings/AdvancedSettingsCategory.cs` — implements `ISettingsCategory`; `CategoryName = "Advanced"`, `SortOrder = 3`; `CreateContent()` returns `UserControl` containing target process path configuration migrated from `SettingsWindow.xaml.cs`; depends on T030
- [ ] T037 [US5] Wire `MaterialSettingsWindow` in `src/WheelOverlay/App.xaml.cs` — remove old `SettingsWindow` instantiation; create `MaterialSettingsWindow` instance; call `RegisterCategory()` for `DisplaySettingsCategory`, `AppearanceSettingsCategory`, `AdvancedSettingsCategory` (in that order; sort by SortOrder, not registration order); update system tray "Settings" menu item to `settingsWindow.Show()`; depends on T032, T033, T034, T035, T036
- [ ] T038 [US5] Remove `src/WheelOverlay/AboutWindow.xaml` and `src/WheelOverlay/AboutWindow.xaml.cs` — `AboutSettingsCategory` replaces them; remove system tray "About" menu item handler that referenced old window; confirm build clean; depends on T037
- [ ] T039a [US5] Verify keyboard navigation in `MaterialSettingsWindow` — open settings window; confirm Up/Down arrow keys move selection in the category `ListBox`; confirm Tab and Shift-Tab traverse controls within the content panel without requiring mouse; confirm Enter activates focused buttons (OK, Apply, Cancel); no task is complete until all three navigation paths work; depends on T037
- [ ] T039 [US5] Remove `src/WheelOverlay/SettingsWindow.xaml` and `src/WheelOverlay/SettingsWindow.xaml.cs` — all settings controls migrated to category classes T034–T036; run `dotnet test --configuration FastTests` and confirm green before removing; depends on T037, T038

**Checkpoint**: Settings window opens with 4 categories in correct order; About shows version and GitHub link; all settings save/load correctly; AboutWindow no longer exists; keyboard arrow-key navigation works between categories

---

## Phase 8: User Story 6 — Find Help and Documentation for WheelOverlay (Priority: P3)

**Goal**: New WheelOverlay user can complete initial setup by following `getting-started.md` without external assistance; troubleshooting guide covers common issues including DirectInput not detected.

**Independent Test**: Follow `docs/wheel-overlay/getting-started.md` from clean install to working overlay configured with a sim racing wheel, without prior knowledge of the app.

### Implementation for User Story 6

- [ ] T040 [P] [US6] Create `docs/wheel-overlay/getting-started.md` — sections: Prerequisites (Windows 10+, compatible DirectInput wheel); Installation (MSI download, run installer); First Launch (system tray icon appears, what to expect); Initial Configuration (open Settings → Display → select device → select layout); Verifying the overlay is visible; First sim session (launch sim, confirm overlay stays visible)
- [ ] T041 [P] [US6] Create `docs/wheel-overlay/usage-guide.md` — sections: Layout Types (Single, Vertical, Horizontal, Grid, Dial — description and use case for each); Profile Management (create, rename, switch, why use profiles); Animation Settings; Grid Configuration (row/column count); Position Count; Smart Text Condensing; Test Mode (`--test-mode` flag); Alt+F6 Hotkey Repositioning; Settings Categories Overview (Display, Appearance, Advanced, About)
- [ ] T042 [P] [US6] Create `docs/wheel-overlay/tips.md` — sections: Optimal Overlay Placement (corner positioning, avoiding apex sight lines); Font Size for High-Refresh Displays; Using Profiles for Different Cars and Games; Power User Tips (system tray quick-access, hotkey workflow for quick repositioning mid-session)
- [ ] T043 [P] [US6] Create `docs/wheel-overlay/troubleshooting.md` — sections: DirectInput Device Not Detected (check device is plugged in, DirectInput driver installed, try running as administrator); Overlay Not Visible (verify system tray icon, check display scaling, check always-on-top behavior); Settings Not Saving (AppData write permissions, log file at `%APPDATA%\WheelOverlay\logs.txt`); Hotkey Not Working (another app holds Alt+F6 — use system tray "Configure overlay position" as fallback); Performance (check CPU/RAM vs NFR-001 targets: <2% CPU, <50MB RAM)

**Checkpoint**: All four documentation files present and complete; new user can follow `getting-started.md` to working overlay; troubleshooting guide covers all scenarios from spec edge cases

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation updates, and release readiness across all user stories.

- [ ] T044a [P] Verify NFR-001 performance — with WheelOverlay running in overlay mode (no sim process detected), idle for 60 seconds; observe CPU and Working Set via `Get-Process WheelOverlay | Select-Object CPU, WorkingSet64` or Task Manager; assert CPU usage < 2% and Working Set < 50 MB; document actual readings as a comment on this task and add them to a `## Performance` subsection in `docs/wheel-overlay/troubleshooting.md` (alongside T043 content)
- [ ] T044 [P] Update `README.md` — add "Repository Structure" section with monorepo directory layout table (src/, tests/, installers/, scripts/, docs/, assets/); add "Adding a New Overlay App" section referencing `specs/001-opendash-monorepo-rebrand/quickstart.md`; add "Applications" subsection listing WheelOverlay v0.7.0 with brief description; update "Version History" summary for this feature's changes; preserve all existing installation and usage instructions
- [ ] T045 [P] Update `CHANGELOG.md` — add entries under `[Unreleased]` per Keep a Changelog format: Added (Alt+F6 global hotkey for overlay repositioning, modern settings window with side navigation, About section in settings window, shared font resources for consistent typography, WheelOverlay user documentation, contributing guide documenting conventions); Changed (settings window redesigned with category-registration pattern); Removed (separate About dialog — now integrated into settings window)
- [ ] T046 Run `scripts/Validate-PropertyTests.ps1 -TestProjectPath tests/WheelOverlay.Tests` and `scripts/Validate-PropertyTests.ps1 -TestProjectPath tests/OverlayCore.Tests` — confirm both pass with no missing `#if FAST_TESTS` directives; depends on T003
- [ ] T047 Run `dotnet test --configuration Release` — confirm all tests pass with 100 PBT iterations (P1–P8 all green); address any flaky property tests; depends on T046
- [ ] T048 Walk through `specs/001-opendash-monorepo-rebrand/quickstart.md` 10-step verification checklist — confirm a new overlay app stub can be added in under 30 minutes of setup time (SC-001); confirm `dotnet build` succeeds from repository root; confirm Constitution Check items satisfied (branch naming, version bump, changelog, test coverage)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (Version Bump)**: No dependencies — T000 MUST be first commit on branch
- **Phase 1 (Setup)**: After Phase 0 — T001 ‖ T002; T003 after
- **Phase 2 (Foundational)**: After Phase 1 — T004 ‖ T005 → T006
- **Phase 3 (US1)**: After Phase 1 — T007 ‖ T008 → T009
- **Phase 4 (US2)**: After Phase 1 — T010 → T011 → T012; T013 ‖ T014 ‖ T015; T016 → T017
- **Phase 5 (US3)**: After Phase 2 (needs T004 SharedFontResources) — T018 → T019 ‖ T020 ‖ T021 ‖ T022 ‖ T023 → T024
- **Phase 6 (US4)**: After Phase 1 — T025 → T026 → T027 → T028
- **Phase 7 (US5)**: After Phase 2 (needs T005 FontUtilities) and T030 — T030 ‖ T031 → T032; T033 ‖ T034 ‖ T035 ‖ T036 → T037 → T038 → T039
- **Phase 8 (US6)**: No blocking dependencies — T040 ‖ T041 ‖ T042 ‖ T043 anytime
- **Phase 9 (Polish)**: After all stories — T044 ‖ T045; T046 → T047 → T048

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 1 — no inter-story dependencies
- **US2 (P1)**: Starts after Phase 1 — no inter-story dependencies (parallel with US1)
- **US3 (P1)**: Starts after Phase 2 — no inter-story dependencies
- **US4 (P2)**: Starts after Phase 1 — no inter-story dependencies (parallel with US1, US2)
- **US5 (P2)**: Starts after Phase 2 and T030 — no other story dependencies
- **US6 (P3)**: No dependencies — can start anytime alongside any phase

### Parallel Execution Map

```
Phase 0:  T000  (first commit — version bump)

Phase 1:  T001 ‖ T002  →  T003

Phase 2:  T004 ‖ T005  →  T006

Parallel workstreams after Phase 1 (US1, US2, US4, US6 can all start):
  Stream A (US1):  T007 ‖ T008  →  T009
  Stream B (US2):  T010  →  T011  →  T012
                   T013 ‖ T014 ‖ T015  (parallel script updates)
                   T016  →  T017
  Stream C (US4):  T025  →  T026  →  T027  →  T028
  Stream D (US6):  T040 ‖ T041 ‖ T042 ‖ T043

After Phase 2 (US3 and US5 start):
  Stream E (US3):  T018  →  T019 ‖ T020 ‖ T021 ‖ T022 ‖ T023  →  T024
  Stream F (US5):  T029
                   T030 ‖ T031  →  T032
                   T033 ‖ T034 ‖ T035 ‖ T036  →  T037  →  T038  →  T039

Phase 9 (Polish, all stories done):
                   T044 ‖ T045  →  T046  →  T047  →  T048
```

---

## Implementation Strategy

### MVP First (User Stories 1–3 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational fonts
3. Complete Phase 3 (US1): Multi-app CI/CD structure
4. Complete Phase 4 (US2): WheelOverlay release pipeline
5. Complete Phase 5 (US3): No-regression verification
6. **STOP and VALIDATE**: All P1 user stories complete — core monorepo rebrand deliverable
7. Release as v0.7.0 if desired before tackling P2/P3 work

### Incremental Delivery

1. Phase 1 + Phase 2 → Infrastructure ready
2. Phase 3 (US1) → Independent multi-app CI/CD ✓
3. Phase 4 (US2) → WheelOverlay release pipeline ✓
4. Phase 5 (US3) → No regression confirmed ✓ → **Ship P1 milestone**
5. Phase 6 (US4) → Alt+F6 hotkey ✓
6. Phase 7 (US5) → Modern settings window ✓ → **Ship P2 milestone**
7. Phase 8 (US6) → Documentation ✓
8. Phase 9 → Full release validation ✓ → **Ship v0.7.0**

### Single Developer Sequence

Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2) → Phase 5 (US3) → Phase 6 (US4) → Phase 7 (US5) → Phase 8 (US6) → Phase 9

Interleave US6 documentation during implementation phases as a context switch (no blocking dependencies).

---

## Notes

- **[P]** tasks = different files, no interdependencies — safe to parallelize within the same group
- Each **[USN]** label maps the task to a specific user story in spec.md for traceability
- Property tests T006, T010, T025, T029 are constitution-mandatory per Principle II — not optional
- Every property test file requires `#if FAST_TESTS / #else` iteration directives
- Every property test comment must be: `// Feature: OpenDash Monorepo Rebrand, Property N: {title}`
- Run `Validate-PropertyTests.ps1` (T003 update) after writing each property test
- US5 is the most complex phase — keep `SettingsWindow.xaml.cs` alive until T037 is confirmed working (T039 is last)
- All PowerShell scripts must include `$ErrorActionPreference = "Stop"` per Constitution V
- Commit after each task or logical group using Conventional Commits format: `type(scope): description`
- US3 regression check (T024) must be green before merging any US5 changes
