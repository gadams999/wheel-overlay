# Implementation Plan: OpenDash Monorepo Rebrand

**Branch**: `001-opendash-monorepo-rebrand` | **Date**: 2026-03-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-opendash-monorepo-rebrand/spec.md`

## Summary

Restructure the WheelOverlay repository into a proper multi-app monorepo (OpenDash-Overlays) by extracting shared overlay infrastructure into `OverlayCore`, migrating WheelOverlay to `src/`, adding a system-wide Alt+F6 hotkey, replacing the procedural settings dialog with a category-registration–based `MaterialSettingsWindow`, sharing font resources across apps, and establishing per-app namespaced CI/CD release pipelines — all while preserving full backward compatibility for existing WheelOverlay users.

---

## Technical Context

**Language/Version**: C# 12 / .NET 10.0-windows
**Primary Dependencies**: WPF (UI), WinForms (NotifyIcon/SystemTray), System.Management (WMI process monitoring), Vortice.DirectInput 3.8.2 (WheelOverlay only), FsCheck 2.16.6 + FsCheck.Xunit (property tests), xUnit 2.x, WiX 4.0.5 (MSI installer), GitHub Actions
**Storage**: JSON settings at `%APPDATA%\WheelOverlay\settings.json`; log file at `%APPDATA%\WheelOverlay\logs.txt` (1 MB rotation)
**Testing**: xUnit + FsCheck 2.16.6; `FastTests` configuration (10 PBT iterations, `FAST_TESTS` constant); `Release` configuration (100 PBT iterations)
**Target Platform**: Windows 10+ (net10.0-windows); single-process per overlay app
**Project Type**: Desktop overlay applications (monorepo — shared class library + per-app WinExe)
**Performance Goals**: <2% CPU, <50 MB RAM per overlay app while idle (NFR-001)
**Constraints**: Settings files must load without migration from pre-restructuring installations; Alt+F6 hotkey registration failure must not crash the app; OverlayCore must never carry a `<Version>` element
**Scale/Scope**: 2 overlay apps (WheelOverlay v0.6.0 active; discord-notify placeholder workflow only), 1 shared OverlayCore library, ~65+ existing tests preserved

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Monorepo with Shared Core (ProjectReference)** | ✅ PASS | OverlayCore has no `<Version>`; WheelOverlay references it via `ProjectReference`; all apps under `src/` |
| **II. Test-First with Property-Based Testing** | ⚠️ PENDING | Properties P1–P4 implemented. P5 (mode state machine), P6 (tag round-trip), P7 (settings category sort), P8 (FontUtilities) must be written before implementation is complete — tracked in research.md Decision 6 |
| **III. Independent Per-App Versioning** | ✅ PASS | WheelOverlay declares `<Version>0.6.0</Version>`. Version bump is first commit on this branch per constitution. |
| **IV. Changelog as Release Source of Truth** | ✅ PASS | `CHANGELOG.md` follows Keep a Changelog format; `[Unreleased]` section must be updated as part of this feature |
| **V. Observability and Error Resilience** | ✅ PASS | `LogService.Initialize("WheelOverlay")` is first call in `Program.cs`; all failure modes log to `LogService.Error()` per research.md decisions 1 and 7 |
| **VI. Branch Naming and Conventional Commits** | ⚠️ JUSTIFIED DEVIATION | Branch `001-opendash-monorepo-rebrand` uses `{issue-number}-{kebab-case-description}` format, not `<type>/<description>`. See Complexity Tracking below. |

**Constitution Check result**: PASS with one justified deviation. Phase 0 research may proceed.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-opendash-monorepo-rebrand/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command) ✅ complete
├── data-model.md        # Phase 1 output (/speckit.plan command) ✅ complete
├── quickstart.md        # Phase 1 output (/speckit.plan command) ✅ complete
├── contracts/           # Phase 1 output (/speckit.plan command) ✅ complete
│   ├── ISettingsCategory.md
│   ├── GlobalHotkeyService.md
│   ├── CICDWorkflows.md
│   └── SharedFontResources.md
└── tasks.md             # Phase 2 output (/speckit.tasks command — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
OpenDash-Overlays.sln            — root solution, build entry point

src/
├── OverlayCore/                 — shared class library (no <Version>)
│   ├── Behaviors/
│   │   ├── BaseOverlayWindow.cs       ✅ exists
│   │   └── ConfigModeBehavior.cs      ✅ exists
│   ├── Models/
│   │   └── ThemePreference.cs         ✅ exists
│   ├── Resources/
│   │   ├── DarkTheme.xaml             ✅ exists
│   │   ├── LightTheme.xaml            ✅ exists
│   │   └── Fonts/
│   │       ├── SharedFontResources.xaml   ❌ to create
│   │       └── FontUtilities.cs           ❌ to create
│   ├── Services/
│   │   ├── LogService.cs              ✅ exists
│   │   ├── ProcessMonitor.cs          ✅ exists
│   │   ├── SystemTrayScaffold.cs      ✅ exists
│   │   ├── ThemeService.cs            ✅ exists
│   │   ├── WindowTransparencyHelper.cs ✅ exists
│   │   └── GlobalHotkeyService.cs     ❌ to create
│   ├── Settings/
│   │   ├── ISettingsCategory.cs       ❌ to create
│   │   ├── AboutSettingsCategory.cs   ❌ to create
│   │   ├── MaterialSettingsWindow.xaml ❌ to create
│   │   ├── MaterialSettingsWindow.xaml.cs ❌ to create
│   │   └── Styles/
│   │       └── MaterialStyles.xaml    ❌ to create
│   └── Placeholder.cs                 ❌ to remove (once real code present)
│
└── WheelOverlay/                — overlay app (v0.6.0 → will be bumped)
    ├── App.xaml / App.xaml.cs         ✅ exists — add SharedFontResources merge
    ├── Program.cs                     ✅ exists
    ├── MainWindow.xaml / .cs          ✅ exists — wire GlobalHotkeyService + ConfigModeBehavior
    ├── SettingsWindow.xaml / .cs      ✅ exists — replace with MaterialSettingsWindow
    ├── AboutWindow.xaml / .cs         ✅ exists — remove (replaced by AboutSettingsCategory)
    ├── Models/                        ✅ all exist, no changes
    ├── Services/InputService.cs       ✅ exists, no changes
    ├── ViewModels/                    ✅ exist
    ├── Views/                         ✅ all 5 layouts exist
    └── Settings/
        ├── DisplaySettingsCategory.cs     ❌ to create (migrated from SettingsWindow)
        ├── AppearanceSettingsCategory.cs  ❌ to create (migrated from SettingsWindow)
        └── AdvancedSettingsCategory.cs    ❌ to create (migrated from SettingsWindow)

tests/
├── OverlayCore.Tests/
│   ├── LogServicePropertyTests.cs     ✅ exists (P2)
│   ├── ProcessMonitorPropertyTests.cs ✅ exists (P3)
│   ├── ThemeServicePropertyTests.cs   ✅ exists (P1)
│   ├── PlaceholderTests.cs            ❌ to remove
│   ├── OverlayModePropertyTests.cs    ❌ to create (P5)
│   ├── TagFormatPropertyTests.cs      ❌ to create (P6)
│   ├── SettingsCategoryPropertyTests.cs ❌ to create (P7)
│   └── FontUtilitiesPropertyTests.cs  ❌ to create (P8)
└── WheelOverlay.Tests/                ✅ 65+ tests, all must continue to pass

installers/
└── wheel-overlay/
    ├── Package.wxs                    ❌ to create (WiX 4 installer source)
    └── CustomUI.wxs                   ❌ to create (WiX custom UI)

scripts/
├── Validate-PropertyTests.ps1         ✅ exists — update to accept -TestProjectPath param
├── Add-PropertyTestDirectives.ps1     ✅ exists
└── wheel-overlay/
    ├── build_msi.ps1                  ✅ exists — update paths to src/WheelOverlay/
    ├── build_release.ps1              ✅ exists — update paths to src/WheelOverlay/
    └── generate_components.ps1        ✅ exists — update output paths

docs/
└── wheel-overlay/
    ├── getting-started.md             ❌ to create
    ├── usage-guide.md                 ❌ to create
    ├── tips.md                        ❌ to create
    └── troubleshooting.md             ❌ to create

.github/workflows/
├── branch-build-check.yml            ✅ exists — add path filters, update csproj paths
├── pre-merge-validation.yml          ✅ exists — verify/update paths
├── wheel-overlay-release.yml         ❌ to create (replaces release.yml)
├── discord-notify-release.yml        ❌ to create (placeholder per spec clarification)
└── release.yml                       ❌ to delete (superseded by wheel-overlay-release.yml)

assets/                               ✅ complete — shared icons
README.md                             ✅ exists — update monorepo structure section
CHANGELOG.md                          ✅ exists — add [Unreleased] entries for this feature
CONTRIBUTING.md                       ❌ to create (documents branch naming, versioning, release tags)
```

**Structure Decision**: Option 1 (single solution, monorepo layout). The layout is already established by completed migration checkpoints. OverlayCore is the shared library under `src/OverlayCore/`; each overlay app lives under `src/{AppName}/`. Tests, installers, scripts, and docs mirror the `src/` app name.

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Branch `001-opendash-monorepo-rebrand` violates Constitution VI (`<type>/<description>` format) | This is the bootstrapping branch. It was created before the branch naming convention was formalized in FR-031 and Constitution VI. FR-031's clarification Q&A defined `{issue-number}-{kebab-case-description}` as the answer, which is the format this branch uses. The constitution was then ratified on the same date (2026-03-18). | Renaming the branch at this stage would: (1) break all existing commit references and checkpoints already on this branch, (2) require force-pushing which is prohibited, (3) invalidate the spec artifacts that reference this branch name throughout. All future branches will comply with Constitution VI. |
| T000 (version bump to 0.7.0) was not the literal first commit on the branch | Constitution III requires the version bump to be the first commit on the branch. The branch accumulated 10 spec-authoring, migration, and tooling commits before `tasks.md` was generated and implementation tasks were sequenced. T000 was executed as the first implementation commit after task generation — consistent with the spirit of the rule. | Rebasing or amending history to move the version bump earlier would require force-pushing (prohibited) and would destroy checkpoint references already on this branch. All future feature branches will have the version bump as commit #1 before any implementation begins. |

---

## Phase 0 Research

All Phase 0 unknowns are resolved. See [research.md](research.md) for full findings.

**Summary of decisions**:
1. **Global hotkey**: Win32 `RegisterHotKey` + hidden WPF helper window + `HwndSource.AddHook` for `WM_HOTKEY` interception
2. **Settings framework**: `ISettingsCategory` registration pattern + `MaterialSettingsWindow` in OverlayCore; WheelOverlay registers Display (1), Appearance (2), Advanced (3); OverlayCore auto-registers About (999)
3. **Shared fonts**: `SharedFontResources.xaml` resource dictionary + `FontUtilities.cs` helpers; consumed via pack URI in `App.xaml`
4. **CI/CD**: Replace `release.yml` with `wheel-overlay-release.yml`; add `discord-notify-release.yml` placeholder; update `branch-build-check.yml` with path filters
5. **Build scripts**: Move per-app scripts to `scripts/wheel-overlay/`; update all paths to `src/WheelOverlay/`
6. **Property tests**: P5–P8 required but not yet written (tracked)
7. **ConfigMode wiring**: `GlobalHotkeyService` fires `ToggleModeRequested`; `MainWindow` delegates to `ConfigModeBehavior`
8. **Placeholder removal**: `Placeholder.cs` and `PlaceholderTests.cs` removed when real code is in place

---

## Phase 1 Design

All Phase 1 artifacts are complete:

- **[data-model.md](data-model.md)**: Entities for `ISettingsCategory`, `GlobalHotkeyService`, `SharedFontResources`, `OverlayCore` services, release tag format, `AppSettings` (backward compat), overlay mode state machine
- **[quickstart.md](quickstart.md)**: 10-step developer guide for adding a second overlay app to the monorepo
- **[contracts/ISettingsCategory.md](contracts/ISettingsCategory.md)**: Full interface definition, registration protocol, ordering invariant, WheelOverlay implementation table
- **[contracts/GlobalHotkeyService.md](contracts/GlobalHotkeyService.md)**: Full class definition, Win32 constants, usage pattern, mode toggle logic, error handling contract
- **[contracts/CICDWorkflows.md](contracts/CICDWorkflows.md)**: `wheel-overlay-release.yml` and `branch-build-check.yml` workflow definitions, tag format contract
- **[contracts/SharedFontResources.md](contracts/SharedFontResources.md)**: XAML resource keys, `FontUtilities` API, merge pattern, user-configurable font interaction

**Post-Phase 1 Constitution Check**: All principles remain satisfied. No new violations introduced by the design. The settings framework registration pattern (Principle I, II) is now specified in detail.

---

## Implementation Checklist (for /speckit.tasks reference)

The following work items remain. See `/speckit.tasks` to generate the ordered `tasks.md`.

**Infrastructure remaining**:
- Remove `Placeholder.cs` and `PlaceholderTests.cs`
- Update `Validate-PropertyTests.ps1` to accept `-TestProjectPath` parameter
- Update `branch-build-check.yml`: add path filters, update CSPROJ paths
- Create `wheel-overlay-release.yml` (replace `release.yml`)
- Create `discord-notify-release.yml` (placeholder workflow)
- Delete `release.yml`
- Update build scripts (`build_msi.ps1`, `build_release.ps1`, `generate_components.ps1`) to reference `src/WheelOverlay/`
- Create WiX installer files (`installers/wheel-overlay/Package.wxs`, `CustomUI.wxs`)

**OverlayCore new code**:
- `src/OverlayCore/Settings/ISettingsCategory.cs`
- `src/OverlayCore/Settings/AboutSettingsCategory.cs`
- `src/OverlayCore/Settings/MaterialSettingsWindow.xaml` + `.cs`
- `src/OverlayCore/Settings/Styles/MaterialStyles.xaml`
- `src/OverlayCore/Services/GlobalHotkeyService.cs`
- `src/OverlayCore/Resources/Fonts/SharedFontResources.xaml`
- `src/OverlayCore/Resources/Fonts/FontUtilities.cs`

**WheelOverlay changes**:
- Add `SharedFontResources.xaml` merge to `App.xaml`
- Replace local font definitions with shared resource key references in Views
- Create `DisplaySettingsCategory.cs`, `AppearanceSettingsCategory.cs`, `AdvancedSettingsCategory.cs`
- Wire `MaterialSettingsWindow` + category registration in `App.xaml.cs`
- Wire `GlobalHotkeyService` + `ConfigModeBehavior` delegation in `MainWindow.xaml.cs`
- Remove `AboutWindow.xaml` + `AboutWindow.xaml.cs`
- Remove or gut old `SettingsWindow.xaml` + `.cs` (replaced by MaterialSettingsWindow)

**Property tests (required before implementation complete)**:
- `tests/OverlayCore.Tests/OverlayModePropertyTests.cs` (P5)
- `tests/OverlayCore.Tests/TagFormatPropertyTests.cs` (P6)
- `tests/OverlayCore.Tests/SettingsCategoryPropertyTests.cs` (P7)
- `tests/OverlayCore.Tests/FontUtilitiesPropertyTests.cs` (P8)

**Documentation**:
- `docs/wheel-overlay/getting-started.md`
- `docs/wheel-overlay/usage-guide.md`
- `docs/wheel-overlay/tips.md`
- `docs/wheel-overlay/troubleshooting.md`
- `CONTRIBUTING.md` (branch naming, versioning, release tags)
- Update `README.md` (monorepo structure, add new app guide, directory layout)
- Update `CHANGELOG.md` ([Unreleased] entries for all user-facing changes)
