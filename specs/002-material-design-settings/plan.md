# Implementation Plan: Material Design Settings Window

**Branch**: `wheel-overlay/v0.7.0` | **Date**: 2026-03-21 | **Spec**: `specs/002-material-design-settings/`
**Input**: Feature specification from `specs/002-material-design-settings/spec.md`

## Summary

Replace the hand-crafted WPF styles in WheelOverlay's settings window with MaterialDesignInXamlToolkit (MD2 style set), giving users a polished Material Design experience — ripple navigation, elevated surfaces, floating-label inputs, and MD-typed action buttons — while leaving `ISettingsCategory`, `AppSettings`, and all settings persistence logic untouched.

## Technical Context

**Language/Version**: C# 12 / .NET 10.0-windows
**Primary Dependencies**: MaterialDesignThemes.Wpf v5.x.x (MD2, pinned exact version at implementation time), WPF, OverlayCore (ProjectReference)
**Storage**: JSON at `%APPDATA%\WheelOverlay\settings.json` — unchanged
**Testing**: xUnit, FsCheck 2.16.6 with FsCheck.Xunit
**Target Platform**: Windows 10/11 desktop (WPF)
**Project Type**: Desktop overlay app + shared WPF class library
**Performance Goals**: <2% CPU, <50MB RAM idle (from 001 NFR-001 — must not regress)
**Constraints**: `ISettingsCategory` interface unchanged; `AppSettings` schema unchanged; all 001 tests must continue to pass
**Scale/Scope**: 4 settings category panels, 1 settings window, 1 new static bootstrap helper, 1 modified theme service

## Constitution Check

*GATE: Evaluated before Phase 0 research. Re-evaluated post-design.*

| Principle | Requirement | Status |
|---|---|---|
| I. Monorepo / Shared Core | `MaterialDesignThemes.Wpf` declared in `OverlayCore.csproj` only (FR-002); no `<Version>` on OverlayCore | ✅ PASS — dependency goes in OverlayCore; WheelOverlay gets it transitively |
| II. Test-First / PBT | Design doc defines ≥1 correctness property per testable invariant; properties implemented before PR merge | ✅ PASS — 4 properties defined in data-model.md (2 new, 2 regression guards) |
| III. Per-App Versioning | `WheelOverlay.csproj` version = `0.7.0`; branch = `wheel-overlay/v0.7.0`; version bump was first commit | ✅ PASS — version already 0.7.0 in .csproj |
| IV. Changelog | CHANGELOG.md updated before merge | ⏳ PENDING — to be done during implementation |
| V. Observability | Bootstrap and theme-apply failures logged via `LogService.Error()`; no silent swallows | ✅ PASS — both failure paths documented in contracts and data-model |
| VI. Branch Naming | Branch `wheel-overlay/v0.7.0` matches PRIMARY format `[overlay-name/]vN.N.N`; spec folder `002-material-design-settings` is permanent sequential name | ✅ PASS |

**Complexity Tracking**: No violations. Section not applicable.

## Project Structure

### Documentation (this feature)

```text
specs/002-material-design-settings/
├── spec.md              # Feature specification (input)
├── plan.md              # This file
├── research.md          # Phase 0 output — technology decisions
├── data-model.md        # Phase 1 output — entities, properties, state transitions
├── quickstart.md        # Phase 1 output — developer guide
├── contracts/
│   └── MaterialDesignThemeIntegration.md  # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks — not yet)
```

### Source Code (repository root)

```text
src/
  OverlayCore/
    OverlayCore.csproj                         # Add MaterialDesignThemes.Wpf reference
    Settings/
      MaterialSettingsWindow.xaml              # Replace NavListBox styles with MD2 ColorZone + Ripple
      MaterialSettingsWindow.xaml.cs           # Add MaterialDesignBootstrap.EnsureInitialized() call
      MaterialDesignBootstrap.cs               # NEW: idempotent MD resource initialiser
      Styles/
        MaterialStyles.xaml                    # Replace NavListBoxStyle, ContentBorderStyle with MD2
    Services/
      ThemeService.cs                          # Add PaletteHelper.SetTheme() in ApplyTheme()
    Resources/
      DarkTheme.xaml                           # Unchanged
      LightTheme.xaml                          # Unchanged

  WheelOverlay/
    Settings/
      DisplaySettingsCategory.cs               # Replace WPF controls with MD2 equivalents
      AppearanceSettingsCategory.cs            # Replace WPF controls with MD2 equivalents
      AdvancedSettingsCategory.cs              # Replace WPF controls with MD2 equivalents
      AboutSettingsCategory.cs                 # Replace WPF controls with MD2 equivalents

tests/
  WheelOverlay.Tests/
    Settings/
      NavigationSortOrderTests.cs              # NEW: Property 1 (category sort order)
      CategoryCancelDiscardTests.cs            # NEW: Property 2 (cancel discards changes)
  OverlayCore.Tests/
    (existing tests — must continue to pass; Property 3 and 4 already covered)
```

**Structure Decision**: Single-project structure within the existing monorepo layout. No new projects, no new solution folders. All changes are scoped to `OverlayCore` (shared styles and bootstrap) and `WheelOverlay` (category panels).

## Phase 0 — Research Summary

All NEEDS CLARIFICATION items are resolved. See `research.md` for full details.

| Question | Decision |
|---|---|
| MDIX version | v5.3.1 (`MaterialDesignThemes` NuGet package), pinned exact patch at implementation time |
| Library integration | Programmatic merge via `MaterialDesignBootstrap.EnsureInitialized()` |
| Navigation Rail in MD2 | Styled `ListBox` + `ColorZone` + `Ripple` attached behaviour |
| Runtime theme switching | `PaletteHelper.GetTheme()` / `SetBaseTheme()` / `SetTheme()` |
| Resource key conflicts | No conflict — MDIX keys (`MaterialDesign*`) distinct from WheelOverlay keys (`Theme*`) |

## Phase 1 — Design Summary

All design artifacts are complete. Key design decisions:

1. **`MaterialDesignBootstrap`** (new static class in OverlayCore) — merges MD dictionaries once into `Application.Current.Resources`; called from `MaterialSettingsWindow` constructor; idempotent; failure-safe with `LogService.Error()`

2. **Navigation rail** — `ListBox` on `ColorZone` (SecondaryMid mode) with `ItemContainerStyle` applying `materialDesign:Ripple.IsEnabled="True"` and `materialDesign:ListBoxItemAssist` accent-colour selected state; keyboard Up/Down navigation preserved

3. **ThemeService extension** — after swapping WheelOverlay theme dictionaries, calls `PaletteHelper.SetTheme()` to sync MD palette to the same light/dark state; failure logged but non-crashing

4. **Category panels** — all control replacements are in C# code-behind (no XAML files for categories); controls get MD styles via `Application.Current.FindResource()` and `HintAssist` attached properties; save/load logic is untouched

5. **`ISettingsCategory` contract** — zero changes; all four existing categories implement the same interface

## Implementation Notes

- **Version pin**: Before writing any code, run `dotnet package search MaterialDesignThemes.Wpf` and record the exact latest stable v5.x version in `OverlayCore.csproj`. Never use a floating range.
- **Test sequence**: Write the two new FsCheck property tests (data-model.md Properties 1 and 2) before modifying the production code. Confirm all four property tests pass (including the two 001 regression guards).
- **Visual verification**: After implementation, manually open the settings window in both light and dark themes and verify all four category panels against the acceptance scenarios in `spec.md`.
- **CHANGELOG**: Add entries under `[Unreleased]` in `CHANGELOG.md` covering: "Upgraded settings window to Material Design 2 visual style with ripple navigation, floating-label inputs, and MD-typed buttons."
