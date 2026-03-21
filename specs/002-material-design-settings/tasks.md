# Tasks: Material Design Settings Window

**Input**: Design documents from `specs/002-material-design-settings/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Property-based tests (FsCheck) are explicitly required by constitution Principle II and the plan's test sequence. Properties 1 and 2 are new; Properties 3 and 4 are regression guards from 001. All four must pass before merge.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared-state dependencies)
- **[Story]**: User story this task belongs to (US1–US4)
- Exact file paths included in all descriptions

---

## Phase 1: Setup

**Purpose**: Pin exact MDIX package version and register it as a dependency so all subsequent phases have a stable build target.

- [ ] T001 Resolve exact latest stable `MaterialDesignThemes.Wpf` v5.1.x version via `dotnet package search MaterialDesignThemes.Wpf` and add pinned `<PackageReference>` to `src/OverlayCore/OverlayCore.csproj` (no `<Version>` on the OverlayCore project itself)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infrastructure that MUST exist before any MD-styled control or runtime theme call can work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. Property tests are written here (before any production code changes) per constitution Principle II and plan.md test sequence.

- [ ] T002 Create `src/OverlayCore/Settings/MaterialDesignBootstrap.cs` — static helper with `_initialized` + `_lock` fields and idempotent `EnsureInitialized()` method that merges `MaterialDesignTheme.Light.xaml` then `MaterialDesignTheme.Defaults.xaml` into `Application.Current.Resources`; on any failure call `LogService.Error()` and return without throwing (data-model.md §MaterialDesignBootstrap, contracts/MaterialDesignThemeIntegration.md)
- [ ] T003 [P] Create `tests/WheelOverlay.Tests/Settings/NavigationSortOrderTests.cs` — FsCheck property test for Property 1 (categories always render in ascending `SortOrder`); must include `// Feature: Material-Design-Settings, Property 1: Navigation categories always render in ascending SortOrder` comment and `#if FAST_TESTS` / `#else` iteration directive; verify test FAILS before implementation (data-model.md §Property 1)
- [ ] T004 [P] Create `tests/WheelOverlay.Tests/Settings/CategoryCancelDiscardTests.cs` — FsCheck property test for Property 2 (cancel restores original settings values); must include `// Feature: Material-Design-Settings, Property 2: Cancel restores original settings values` comment and `#if FAST_TESTS` / `#else` iteration directive; verify test FAILS before implementation (data-model.md §Property 2)
- [ ] T005 [P] Locate the existing Property 3 and Property 4 FsCheck tests in `tests/OverlayCore.Tests/` (or `tests/WheelOverlay.Tests/`) — update each test method's `// Feature:` comment to read `// Feature: Material-Design-Settings, Property 3: ThemeService.IsDarkMode reflects the last ApplyTheme argument` and `// Feature: Material-Design-Settings, Property 4: AppSettings serialise/deserialise round-trip preserves all values` respectively; confirm `#if FAST_TESTS` / `#else` directives are already present (constitution Principle II; data-model.md §Property 3, §Property 4)

**Checkpoint**: Package reference present, bootstrap helper compiled, all four property tests have correct `// Feature: Material-Design-Settings` comments, both new tests failing — user story implementation can now begin

---

## Phase 3: User Story 1 — Settings Window Looks and Feels Like a Native Material Design App (Priority: P1) 🎯 MVP

**Goal**: Replace hand-crafted WPF chrome on `MaterialSettingsWindow` with MD2 navigation rail (ColorZone + Ripple `ListBox`), MD-typed action buttons, and MD surface colours — window looks like a genuine Material Design app.

**Independent Test**: Open the settings window. Confirm the side navigation panel shows ripple feedback on click, accent-coloured selected state, and elevated surface. OK/Apply buttons render as raised MD buttons; Cancel renders flat. Property 1 test passes (sort order invariant).

- [ ] T006 [P] [US1] Update `src/OverlayCore/Settings/Styles/MaterialStyles.xaml` — replace `NavListBoxStyle` with a `ListBox` style that applies `materialDesign:ColorZoneAssist.Mode="SecondaryMid"` via a `ColorZone` wrapper; replace `NavListBoxItemStyle` with a `ListBoxItem` `ItemContainerStyle` that sets `materialDesign:Ripple.IsEnabled="True"` and `materialDesign:ListBoxItemAssist` accent-colour selected state; replace `ContentBorderStyle` with `Background="{DynamicResource MaterialDesignPaper}"`; update `MaterialSettingsWindowStyle` window chrome to use `MaterialDesignBackground` brush (data-model.md §MaterialStyles.xaml, research.md §Decision 3)
- [ ] T007 [P] [US1] Update `src/OverlayCore/Settings/MaterialSettingsWindow.xaml.cs` constructor to call `MaterialDesignBootstrap.EnsureInitialized()` as the first statement before any other initialisation (data-model.md §MaterialSettingsWindow, contracts/MaterialDesignThemeIntegration.md §Sequencing constraint)
- [ ] T008 [US1] Update `src/OverlayCore/Settings/MaterialSettingsWindow.xaml` navigation `ListBox` element — wrap in `ColorZone` with `Mode="SecondaryMid"`, apply the updated `NavListBoxItemStyle` referencing MDIX Ripple and ListBoxItemAssist attached properties, and apply MD typography (`materialDesign:Typography`) to category name `TextBlock` items (data-model.md §MaterialSettingsWindow, research.md §Decision 3)
- [ ] T009 [US1] Update OK and Apply `Button` elements in `src/OverlayCore/Settings/MaterialSettingsWindow.xaml` to `Style="{StaticResource MaterialDesignRaisedButton}"` and Cancel `Button` to `Style="{StaticResource MaterialDesignFlatButton}"` (data-model.md §MaterialSettingsWindow, contracts/MaterialDesignThemeIntegration.md §Resource Keys)

**Checkpoint**: Settings window opens with MD chrome. Property 1 test passes. Run `dotnet test --configuration FastTests`.

---

## Phase 4: User Story 2 — Category Panels Use Consistent Material Design Controls (Priority: P1)

**Goal**: All four category panels (Display, Appearance, Advanced, About) replace default WPF controls with MD2 equivalents — floating-label text fields, MD comboboxes, MD radio buttons, MD sliders, MD typography. Save/load logic is untouched.

**Independent Test**: Navigate to each of the four categories. Confirm zero default WPF control chrome visible in any panel. Property 2 test passes (cancel discards invariant).

- [ ] T010 [P] [US2] Update `src/WheelOverlay/Settings/DisplaySettingsCategory.cs` — add `materialDesign:HintAssist.Hint` to device and position-count `ComboBox` controls; apply `Style="{StaticResource MaterialDesignRadioButton}"` to layout picker radio buttons; apply `materialDesign:Typography="Subtitle1"` attached property to section label `TextBlock`s; apply `Style="{StaticResource MaterialDesignOutlinedButton}"` to New/Rename/Delete profile buttons; leave all field assignments and `LoadValues()`/`SaveValues()` logic unchanged (data-model.md §DisplaySettingsCategory, research.md §Decision 5)
- [ ] T011 [P] [US2] Update `src/WheelOverlay/Settings/AppearanceSettingsCategory.cs` — add `materialDesign:HintAssist.Hint` to Theme and Font Family `ComboBox` controls; add `materialDesign:HintAssist.Hint` + `HintAssist.IsFloating="True"` to colour `TextBox` controls; apply `Style="{StaticResource MaterialDesignSlider}"` to font-size and item-spacing `Slider` controls; leave all field assignments and `LoadValues()`/`SaveValues()` logic unchanged (data-model.md §AppearanceSettingsCategory, research.md §Decision 5)
- [ ] T012 [P] [US2] Update `src/WheelOverlay/Settings/AdvancedSettingsCategory.cs` — add `materialDesign:HintAssist.Hint = "Target Executable Path"` and `HintAssist.IsFloating="True"` to the target-exe-path `TextBox`; apply `Style="{StaticResource MaterialDesignSlider}"` to the opacity `Slider`; apply `Style="{StaticResource MaterialDesignOutlinedButton}"` to Browse, Open Folder, and Reset buttons; leave all field assignments and `LoadValues()`/`SaveValues()` logic unchanged (data-model.md §AdvancedSettingsCategory, research.md §Decision 5)
- [ ] T013 [P] [US2] Update `src/WheelOverlay/Settings/AboutSettingsCategory.cs` — apply `materialDesign:Typography="H6"` to app-name `TextBlock`; apply `materialDesign:Typography="Body1"` to version `TextBlock`; apply `materialDesign:Typography="Body2"` to description `TextBlock`; replace `Hyperlink`-in-`TextBlock` GitHub link with a `Button` styled `Style="{StaticResource MaterialDesignFlatButton}"` with click handler that opens the URL; leave all auto-registration and `LoadValues()`/`SaveValues()` logic unchanged (data-model.md §AboutSettingsCategory, research.md §Decision 5)

**Checkpoint**: All four panels show MD controls. Property 2 test passes. Run `dotnet test --configuration FastTests`.

---

## Phase 5: User Story 3 — Light and Dark Theme Switching Continues to Work (Priority: P2)

**Goal**: `ThemeService.ApplyTheme()` syncs the MD palette to match WheelOverlay's active theme so MD controls render with the correct light/dark colour scheme on every theme change.

**Independent Test**: Launch with dark theme — settings window shows MD dark palette. Switch to light — settings window shows MD light palette. No residual colours from the previous theme. Property 3 regression guard passes.

- [ ] T014 [US3] Extend `src/OverlayCore/Services/ThemeService.cs` `ApplyTheme(bool isDark)` method — after the existing `DarkTheme.xaml`/`LightTheme.xaml` dictionary swap, add: `var paletteHelper = new PaletteHelper(); var theme = paletteHelper.GetTheme(); theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light); paletteHelper.SetTheme(theme);` wrapped in try/catch that calls `LogService.Error()` on failure and continues without re-throwing; `IsDarkMode` update must still occur on the failure path (data-model.md §ThemeService, contracts/MaterialDesignThemeIntegration.md §ThemeService.ApplyTheme, research.md §Decision 4)

**Checkpoint**: Both light and dark themes render correctly in the settings window. Property 3 passes. Run `dotnet test --configuration FastTests`.

---

## Phase 6: User Story 4 — No Settings Data Is Lost During the Visual Upgrade (Priority: P1)

**Goal**: Confirm that all four category panels' persistence logic (save/load/cancel) is structurally unchanged and that the Property 2 and Property 4 regression-guard tests pass.

**Independent Test**: Set specific values in every settings field, save, close, reopen — all values present. Change a value and cancel — original value restored. Property 2 and Property 4 tests pass. All existing 001 tests continue to pass.

- [ ] T015 [US4] Review all four category panel files (`DisplaySettingsCategory.cs`, `AppearanceSettingsCategory.cs`, `AdvancedSettingsCategory.cs`, `AboutSettingsCategory.cs`) in `src/WheelOverlay/Settings/` — run `git diff main...HEAD -- src/WheelOverlay/Settings/*SettingsCategory.cs` and confirm only `HintAssist`, `Style`, and `Typography` setter lines appear; no field assignments, conditional branches, or `LoadValues()`/`SaveValues()` body changes permitted (FR-017)
- [ ] T016 [US4] Run `dotnet test --configuration FastTests` and confirm: Property 2 (cancel discards) passes, Property 3 (theme palette sync) passes, Property 4 (AppSettings round-trip) passes, and all 001 baseline tests pass with no test-logic changes (data-model.md §Property 3, §Property 4, spec.md SC-003)

**Checkpoint**: Full test suite green. All four user stories verified independently.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Changelog entry, full-iteration PBT validation, script hygiene check, performance budget verification, and manual visual sign-off per plan.md implementation notes.

- [ ] T017 [P] Update `CHANGELOG.md` — add entry under `[Unreleased]` section: "Upgraded settings window to Material Design 2 visual style with ripple navigation, floating-label inputs, and MD-typed buttons" (plan.md §Implementation Notes, constitution Principle IV)
- [ ] T018 Run `dotnet test --configuration Release` — confirm all four FsCheck properties pass at 100-iteration depth with no failures or shrinks (plan.md §Implementation Notes, CLAUDE.md §Commands)
- [ ] T019 [P] Run `powershell -File scripts/Validate-PropertyTests.ps1` from repo root — confirm zero violations on the two new property test files (`NavigationSortOrderTests.cs`, `CategoryCancelDiscardTests.cs`) and no regressions in existing test files (CLAUDE.md §Commands)
- [ ] T020 [P] Manual visual verification: open the settings window in both light and dark themes; navigate all four category panels; confirm ripple animation plays on navigation clicks; confirm all controls match MD2 specification; confirm keyboard Up/Down navigation between categories and Tab traversal within panels still work; for WCAG AA contrast spot-check — verify with Windows Accessibility Insights or Colour Contrast Analyser that navigation rail text on `SecondaryMid` background, body text on `MaterialDesignPaper`, and button labels on raised-button surface all meet minimum 4.5:1 ratio in both light and dark palettes (spec.md §Acceptance Scenarios, FR-013, FR-016, plan.md §Implementation Notes)
- [ ] T021 [P] Performance spot-check: launch WheelOverlay with the settings window open, leave idle for 30 seconds, and confirm CPU usage stays below 2% and working-set RAM stays below 50 MB via Task Manager or `Get-Process WheelOverlay | Select-Object CPU, WorkingSet` — verify MDIX library load has not caused measurable regression against NFR-001 (spec.md §Assumptions ¶6, plan.md §Technical Context)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 completion
- **US2 (Phase 4)**: Depends on Phase 3 completion (window chrome must be initialised first — MaterialDesignBootstrap must be called before any MD resource lookup in category panels)
- **US3 (Phase 5)**: Depends on Phase 2 completion (PaletteHelper requires MDIX resources to be merged first)
- **US4 (Phase 6)**: Depends on Phases 3, 4, and 5 (verifies combined output)
- **Polish (Phase 7)**: Depends on Phase 6 sign-off

### User Story Dependencies

- **US1 (P1)**: Requires Phase 2 only — first story to implement
- **US2 (P1)**: Requires US1 (MDIX resources must be loaded via EnsureInitialized before category panel controls can resolve MD styles)
- **US3 (P2)**: Requires Phase 2 only — can be implemented in parallel with US1/US2 by a second developer
- **US4 (P1)**: Verification story — requires US1, US2, US3 complete

### Within Each Phase

- Property tests (T003, T004) MUST be written and confirmed failing before T006–T014 are started
- T005 (comment updates) can be done in parallel with T003/T004
- T002 (bootstrap) must compile before T007 (window calls it)
- T006 (styles) should be complete before T008 (XAML references the styles)
- T008 before T009 (same XAML file — sequential edits)
- T010–T013 are fully parallel (separate files)

### Parallel Opportunities

- T003, T004, T005 (Phase 2): parallel — separate files
- T006 and T007 (Phase 3): parallel — separate files
- T010, T011, T012, T013 (Phase 4): fully parallel — one file each
- T017, T019, T020, T021 (Phase 7): parallel — separate concerns

---

## Parallel Example: Phase 4 (US2 Category Panels)

```
# All four category panels can be updated simultaneously:
Task T010: DisplaySettingsCategory.cs    ← Developer A
Task T011: AppearanceSettingsCategory.cs ← Developer B
Task T012: AdvancedSettingsCategory.cs   ← Developer C
Task T013: AboutSettingsCategory.cs      ← Developer D
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T005)
3. Complete Phase 3: US1 (T006–T009)
4. **STOP and VALIDATE**: Window chrome looks like MD — visual inspection + Property 1 test pass
5. Demo / share screenshot before proceeding to US2

### Incremental Delivery

1. Setup + Foundational → stable build baseline
2. US1 → MD window chrome → demo
3. US2 → MD category panels → demo (full visual parity)
4. US3 → theme switching sync → demo (dark mode verified)
5. US4 + Polish → full test pass + changelog → PR-ready

### Parallel Team Strategy

- Developer A: US1 window chrome (T006–T009)
- Developer B: US3 ThemeService (T014) ← can start after Phase 2
- Once US1 done → Developer A + B + C tackle US2 in parallel (T010–T013)

---

## Notes

- `[P]` tasks operate on different files with no shared-state dependency at the time of execution
- Property tests MUST fail before implementation and pass after — do not skip the failure check
- `ISettingsCategory`, `AppSettings`, and all `LoadValues()`/`SaveValues()` logic are frozen for this feature — any accidental change must be reverted
- `OverlayCore.csproj` must NOT gain a `<Version>` element (constitution Principle I)
- Use `Application.Current.FindResource()` in C# code-behind (not `TryFindResource`) so missing keys throw visibly during development
- Commit after each phase checkpoint to maintain a bisectable history
