# Feature Specification: Material Design Settings Window

**Feature Branch**: `overlay-core/v0.1.0`
**Created**: 2026-03-20
**Status**: Draft
**Input**: Refactor the WheelOverlay settings window (built in 001 phase 7) to use MaterialDesignInXamlToolkit instead of hand-rolled native WPF styles

## Background

The settings window delivered in `001-opendash-monorepo-rebrand` phase 7 established the correct structural pattern — side navigation, category registration, `ISettingsCategory` contract — but implemented the visual layer using custom hand-crafted WPF styles. This feature replaces that visual layer with MaterialDesignInXamlToolkit, giving users a polished, consistent Material Design experience and giving developers access to a proven component library for all future category panels.

The structural contract (`ISettingsCategory`, `MaterialSettingsWindow`, category registration at startup) does not change. Only the visual presentation and the controls used inside category panels are in scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Settings Window Looks and Feels Like a Native Material Design App (Priority: P1)

A user opens the WheelOverlay settings window from the system tray. Instead of the basic styled-from-scratch window they see today, the window presents a genuine Material Design experience: a navigation rail or drawer on the left with properly elevated category items, ripple effects on interaction, Material-themed typography, and accent-coloured selected state — matching the quality of any modern Material Design desktop application.

**Why this priority**: This is the core deliverable. Without it, the feature has no value. All other stories depend on the window chrome being upgraded first.

**Independent Test**: Open the settings window. Confirm the window title bar, side navigation list, content area border, OK/Apply/Cancel buttons, and category selection all visually match Material Design specification (elevation, ripple, colour theming).

**Acceptance Scenarios**:

1. **Given** WheelOverlay is running, **When** the user opens settings from the system tray, **Then** the settings window displays with Material Design chrome — including a clearly elevated side navigation panel with category items that show ripple feedback on click and a distinct selected-state accent colour
2. **Given** the settings window is open, **When** the user hovers over and clicks a category item, **Then** a ripple animation plays at the click point and the item transitions to its selected state
3. **Given** the settings window is open, **When** the user views the OK, Apply, and Cancel buttons, **Then** they render as Material Design buttons with correct elevation and colour theming (primary colour for OK/Apply, text-only or outlined for Cancel)
4. **Given** the settings window is open, **When** the user interacts with it, **Then** all typography (headings, labels, body text) uses Material Design type scale rather than default WPF system fonts

---

### User Story 2 — Category Panels Use Consistent Material Design Controls (Priority: P1)

A user navigates to the Display, Appearance, and Advanced settings categories. The form controls they interact with — dropdowns, text inputs, colour pickers, radio buttons — all use Material Design components, matching the window chrome and each other. There is no visual mismatch between the window frame and the content panel.

**Why this priority**: Visual consistency between the window chrome and the category content panels is what makes the upgrade feel complete. A Material Design frame around plain WPF controls defeats the purpose.

**Independent Test**: Navigate to each of the three WheelOverlay categories (Display, Appearance, Advanced). Confirm that every interactive control within each panel uses Material Design component styling — no default WPF control chrome visible.

**Acceptance Scenarios**:

1. **Given** the user navigates to the Display category, **When** they view the layout selector and device picker, **Then** all controls use Material Design styling with proper labels, underlines or outlines, and focus indicators
2. **Given** the user navigates to the Appearance category, **When** they view the font picker and colour inputs, **Then** all controls use Material Design styling and are visually indistinguishable in quality from the navigation panel
3. **Given** the user navigates to the Advanced category, **When** they view the process path configuration, **Then** text input fields use Material Design text field components with floating labels
4. **Given** the user navigates to the About category, **When** they view it, **Then** the version text and GitHub hyperlink use Material Design typography and link styling

---

### User Story 3 — Light and Dark Theme Switching Continues to Work (Priority: P2)

A user who has selected either the light or dark theme in WheelOverlay opens the settings window. The window appearance — background, surface colours, text contrast, icon tints — automatically matches the currently active theme, exactly as it did before the Material Design upgrade. Switching themes while the app is running updates the settings window on next open.

**Why this priority**: Theme support was a deliberate design requirement in the previous settings window. Regression here would be a visible quality failure for any user running in dark mode.

**Independent Test**: Launch WheelOverlay with dark theme active. Open settings — confirm dark Material Design palette. Close, switch to light theme, reopen — confirm light Material Design palette. All surfaces, text, and interactive states must contrast correctly in both modes.

**Acceptance Scenarios**:

1. **Given** WheelOverlay is configured to use the dark theme, **When** the user opens the settings window, **Then** the window uses a Material Design dark colour scheme with appropriate surface, background, and text contrast
2. **Given** WheelOverlay is configured to use the light theme, **When** the user opens the settings window, **Then** the window uses a Material Design light colour scheme
3. **Given** the app theme changes between sessions, **When** the settings window is opened in the new session, **Then** it reflects the updated theme without residual colours from the previous theme

---

### User Story 4 — No Settings Data Is Lost During the Visual Upgrade (Priority: P1)

A user who has previously saved their display layout, font preferences, colours, and target process path opens settings after the Material Design upgrade is installed. All their saved settings appear exactly as they were. Clicking OK or Apply continues to persist changes correctly. Cancel continues to discard changes.

**Why this priority**: This is a pure visual refactor — the underlying settings model and persistence logic must not regress. Any data loss directly impacts existing users.

**Independent Test**: Set specific values in every settings field, save, close the app, update to the Material Design version, reopen settings. Confirm every field shows the previously saved value. Change a value and cancel — confirm it reverts. Change a value and save — confirm it persists across restart.

**Acceptance Scenarios**:

1. **Given** a user has saved settings in the previous version, **When** they open the settings window after the Material Design upgrade, **Then** all previously saved values are displayed correctly in each category panel
2. **Given** the user changes a setting and clicks Cancel, **When** they reopen the settings window, **Then** the original value is still shown — the cancelled change was not persisted
3. **Given** the user changes a setting and clicks OK or Apply, **When** they restart WheelOverlay, **Then** the changed value persists exactly as entered

---

### Edge Cases

- What happens if the Material Design toolkit's theme resources conflict with WheelOverlay's existing `DarkTheme.xaml` or `LightTheme.xaml` resource dictionaries? The Material Design theme resources must be merged without overriding unrelated WheelOverlay-wide styles.
- What happens if a future `ISettingsCategory` implementation provides a content panel that does not use Material Design controls? The window chrome must still render correctly — individual panel styling is the responsibility of each category author.
- What happens if keyboard navigation breaks after switching from custom ListBox styles to Material Design-themed components? Arrow-key navigation between categories and Tab traversal within panels must be verified explicitly after the migration.
- What happens if the Material Design package version introduces a breaking API change in a future update? The integration must be isolated to `OverlayCore` so only one project needs updating.

## Requirements *(mandatory)*

### Functional Requirements

**Design System Integration**

- **FR-001**: The shared settings infrastructure in OverlayCore MUST use MaterialDesignInXamlToolkit as its design system for all settings window visual components
- **FR-002**: The MaterialDesignInXamlToolkit dependency MUST be declared in OverlayCore only — individual overlay applications MUST NOT need to reference it directly to benefit from Material Design-styled settings
- **FR-003**: MaterialDesignInXamlToolkit theme resource dictionaries MUST be merged at the OverlayCore level so they are available to all settings category content panels without per-category setup

**Window Chrome**

- **FR-004**: The settings window navigation panel MUST use a Material Design-styled list component with ripple interaction feedback and accent-coloured selected state
- **FR-005**: The settings window action buttons (OK, Apply, Cancel) MUST use Material Design button components appropriate to their action weight (primary action vs secondary/dismissal)
- **FR-006**: The settings window layout and surface colours MUST use Material Design elevation and colour system rather than hardcoded brush values

**Category Panel Controls**

- **FR-007**: The Display category panel MUST replace all plain WPF controls with Material Design equivalents (combo boxes, radio buttons, labels)
- **FR-008**: The Appearance category panel MUST replace all plain WPF controls with Material Design equivalents, including floating-label text fields for font and size inputs
- **FR-009**: The Advanced category panel MUST replace the process path input with a Material Design text field component with floating label
- **FR-010**: The About category panel MUST use Material Design typography components for the version string and repository link

**Theming**

- **FR-011**: The settings window MUST respond to WheelOverlay's active theme (light or dark) and apply the corresponding Material Design colour palette
- **FR-012**: Switching from one theme to the other MUST result in correct Material Design surface, background, and text colours with no residual colours from the previous theme
- **FR-013**: All Material Design colour choices MUST meet WCAG AA contrast requirements in both light and dark palette modes

**Behavioural Continuity**

- **FR-014**: The `ISettingsCategory` interface contract defined in `001-opendash-monorepo-rebrand` MUST remain unchanged — this feature modifies visual presentation only
- **FR-015**: Category navigation, save-on-navigate, load-on-navigate, OK/Apply/Cancel semantics, and the About category's auto-registration MUST all behave identically to the `001` implementation
- **FR-016**: Keyboard navigation MUST continue to work: Up/Down arrow keys navigate the category list; Tab and Shift-Tab traverse controls within a content panel; Enter activates focused buttons

**No Regression**

- **FR-017**: All settings values MUST persist and reload correctly after the visual migration — no data model or serialisation changes are permitted as part of this feature
- **FR-018**: All automated tests that passed at the end of `001-opendash-monorepo-rebrand` MUST continue to pass after this feature is implemented

### Key Entities

- **MaterialSettingsWindow**: The concrete WPF window in OverlayCore that hosts the side navigation and category content area. Its structure is unchanged from `001`; only its XAML styles and control choices are replaced.
- **ISettingsCategory**: The interface contract through which overlay apps register settings panels. Unchanged — `CategoryName`, `SortOrder`, `CreateContent()`, `LoadValues()`, `SaveValues()`.
- **Material Design Theme**: The active colour palette (light or dark) applied to the settings window. Sourced from WheelOverlay's existing theme selection and mapped to MaterialDesignInXamlToolkit's palette system.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user opening the settings window can identify it as a Material Design application without prompting — confirmed by visual inspection against Material Design specification
- **SC-002**: All interactive controls in all four category panels (Display, Appearance, Advanced, About) use Material Design components — zero plain WPF default-styled controls visible
- **SC-003**: 100% of existing automated tests pass after the migration with no changes to test logic
- **SC-004**: A user with saved settings from the `001` build experiences zero data loss or misconfigured fields when opening the settings window after upgrading
- **SC-005**: The settings window renders correctly in both light and dark themes with no visible colour artefacts from the non-active theme
- **SC-006**: Keyboard-only navigation through all four categories and all controls within each panel is fully functional — no mouse interaction required

## Assumptions

- MaterialDesignInXamlToolkit is the specific design system to use; no other third-party UI toolkit is in scope
- The `ISettingsCategory` structural contract from `001-opendash-monorepo-rebrand` is treated as stable and will not be modified as part of this feature
- WheelOverlay's existing `DarkTheme.xaml` and `LightTheme.xaml` remain the source of truth for which theme is active; this feature maps that choice to MaterialDesignInXamlToolkit's palette system rather than replacing the theme mechanism
- The Roboto font already embedded in OverlayCore (from `001` phase 2) satisfies MaterialDesignInXamlToolkit's typography requirements — no additional font embedding is needed
- Version target: the most recent stable release of MaterialDesignInXamlToolkit compatible with .NET 10 / WPF
- Performance budget from `001` NFR-001 (<2% CPU, <50MB RAM idle) applies equally to this feature — the Material Design library must not cause measurable regression
