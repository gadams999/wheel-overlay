# Feature Specification: OpenDash Monorepo Rebrand

**Feature Branch**: `001-opendash-monorepo-rebrand`
**Created**: 2026-03-18
**Status**: Draft
**Input**: Migrate .kiro/specs/opendash-monorepo-rebrand into a spec-kit compliant specification

## Clarifications

### Session 2026-03-18

- Q: Is there a target for overlay resource usage during sim racing sessions? → A: <2% CPU, <50MB RAM at idle.
- Q: What is the canonical branch naming convention to document in FR-031? → A: `<type>/<description>` per Constitution §VI (e.g., `feat/add-overlay-repositioning`). The current branch `001-opendash-monorepo-rebrand` is a justified bootstrapping deviation documented in plan.md Complexity Tracking.
- Q: Should the settings UI framework in OverlayCore be a concrete shared WPF Window or an interface/base pattern? → A: OverlayCore contains a concrete `MaterialSettingsWindow` (WPF Window) with side-nav chrome; apps register `ISettingsCategory` panels at startup.
- Q: What concretely satisfies "only the pipeline structure to support discord-notify needs to be established"? → A: A placeholder `.github/workflows/discord-notify-release.yml` with path filters and a namespaced tag trigger (`discord-notify/vX.Y.Z`). No app source code under `src/` is required.
- Q: Does "overlay mode cycling" imply exactly two modes (Normal ↔ Positioning) or more? → A: Two modes for now (Normal ↔ Positioning), but the cycling API in OverlayCore must be designed to support additional modes in the future without breaking changes.

### Session 2026-03-19

- Q: What members must `ISettingsCategory` expose? → A: `string CategoryName`, `int SortOrder`, `FrameworkElement CreateContent()`, `void SaveValues()`, `void LoadValues()` — factory model; `SettingsWindow` calls `CreateContent()` on demand, `LoadValues()` when navigating to a category, and `SaveValues()` on all categories when the user clicks OK or Apply.
- Q: Who is responsible for persisting each category's settings on Save? → A: `ISettingsCategory` exposes `void SaveValues()` and `void LoadValues()`; `MaterialSettingsWindow` coordinates — calling `LoadValues()` when navigating to a category, `SaveValues()` on the outgoing category when navigating away, and `SaveValues()` on all registered categories on OK/Apply. Cancel discards without calling `SaveValues()`. Each category encapsulates its own persistence logic.
- Q: How should the MSI installer handle an existing WheelOverlay installation? → A: In-place upgrade — same `UpgradeCode` across all versions; MSI replaces binaries atomically, APPDATA settings survive untouched.
- Q: Does a change to `src/OverlayCore/` trigger PR checks for all overlay apps or only OverlayCore itself? → A: OverlayCore changes trigger PR checks for all overlay apps that depend on it, in addition to OverlayCore's own checks.
- Q: What must the shared typography resource in OverlayCore contain? → A: Existing WheelOverlay font definitions (family, sizes, weights) extracted into a shared `ResourceDictionary`, plus the Roboto font family. No other new fonts.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add a New Overlay App to the Repository (Priority: P1)

A developer wants to add a second overlay application to the repository. They need a clear structure where they can place the new app alongside WheelOverlay, share existing infrastructure without copying code, and ship the new app independently with its own version number, installer, and release pipeline — without touching or risking WheelOverlay's release process.

**Why this priority**: This is the core value proposition of the entire rebrand. Without this capability, the restructuring has no future payoff. Everything else enables or enriches this story.

**Independent Test**: A developer can create a second overlay app stub under `src/`, wire it to shared infrastructure, confirm it builds, and confirm a version tag push triggers only that app's release pipeline — all without modifying WheelOverlay.

**Acceptance Scenarios**:

1. **Given** a fresh clone of the repository, **When** a developer runs the top-level build command, **Then** all overlay applications and shared infrastructure build successfully in the correct dependency order
2. **Given** a new overlay app is added under `src/`, **When** a version tag for that app is pushed, **Then** only that app's release pipeline triggers — WheelOverlay's pipeline does not run
3. **Given** two overlay apps exist in the repository, **When** a developer changes code in the shared infrastructure layer, **Then** builds for all overlay apps that depend on that infrastructure are triggered

---

### User Story 2 - Release WheelOverlay Without Disrupting Other Work (Priority: P1)

A developer wants to publish a new WheelOverlay release. They push a version tag in the namespaced format (e.g., `wheel-overlay/v0.8.0`). The pipeline validates that the tag version matches the declared version in the project configuration, then builds, tests, packages, and publishes the release — all scoped to WheelOverlay and its dependencies only.

**Why this priority**: Independent, reliable releases are a primary outcome of the monorepo restructuring. This story validates the CI/CD pipeline design end-to-end.

**Independent Test**: Push a `wheel-overlay/v0.8.0` tag with matching version in the project config. Observe that the release pipeline runs, validates the version match, produces an installer and zip artifact, and creates a GitHub release — without triggering any other app's pipeline.

**Acceptance Scenarios**:

1. **Given** a version tag `wheel-overlay/v0.8.0` is pushed and the declared project version is `0.8.0`, **When** the pipeline runs, **Then** a GitHub release is created with `wheel-overlay/v0.8.0` as the tag and the MSI installer attached
2. **Given** a version tag `wheel-overlay/v0.8.0` is pushed but the declared project version is `0.7.9`, **When** the pipeline runs, **Then** it fails with a clear error identifying the version mismatch — no release is created
3. **Given** code changes are merged to the main branch that affect only WheelOverlay files, **When** the path-based trigger fires, **Then** the same release pipeline runs as if a tag had been pushed

---

### User Story 3 - WheelOverlay Users Experience No Regression (Priority: P1)

An existing WheelOverlay user installs the updated application after the monorepo restructuring. All their previously saved settings — device selection, layout preferences, overlay position, profiles, and theme — load correctly without any migration step. The overlay behaves exactly as it did before.

**Why this priority**: Backward compatibility is non-negotiable. Any regression here directly impacts existing users and undermines trust in the release.

**Independent Test**: Install WheelOverlay from the new build over an existing installation that has a populated settings file. Launch the app and verify all settings load correctly and the overlay displays as configured.

**Acceptance Scenarios**:

1. **Given** a user has an existing settings file from a pre-restructuring installation, **When** the updated WheelOverlay is launched, **Then** all saved settings are applied without prompting the user to reconfigure anything
2. **Given** WheelOverlay is running, **When** the user opens settings, **Then** all layout types, profiles, theme options, and device configuration are present and functional
3. **Given** the overlay is positioned on screen, **When** the sim racing application is launched, **Then** the overlay appears in the correct previously-saved position

---

### User Story 4 - Reposition the Overlay With a Keyboard Shortcut (Priority: P2)

An end user is in the middle of a sim racing session and wants to move the overlay to a different corner of the screen. They press Alt+F6 from any application — without needing to click the system tray icon. The overlay switches to positioning mode, allowing them to drag it to the new location. Pressing Alt+F6 again (or Enter) confirms the new position and returns the overlay to its normal click-through state.

**Why this priority**: This is the primary new end-user feature. It directly improves usability during sim racing sessions where switching windows to access a context menu is disruptive.

**Independent Test**: With WheelOverlay running in the foreground of any other application, press Alt+F6. Confirm the overlay becomes draggable. Drag it to a new position and press Alt+F6 again. Confirm the overlay returns to click-through mode and remembers the new position after restart.

**Acceptance Scenarios**:

1. **Given** WheelOverlay is in overlay mode, **When** the user presses Alt+F6, **Then** the overlay enters positioning mode (becomes draggable and no longer click-through)
2. **Given** the overlay is in positioning mode, **When** the user presses Alt+F6 again, **Then** the current position is saved and the overlay returns to normal click-through overlay mode
3. **Given** another application already holds the Alt+F6 key combination, **When** WheelOverlay starts, **Then** the shortcut is unavailable but the application continues working normally and the system tray menu remains the fallback for repositioning

---

### User Story 5 - Configure WheelOverlay Through a Modern Settings Window (Priority: P2)

A user opens the WheelOverlay settings window from the system tray. Instead of a procedurally built dialog, they see a clean, organized settings window with a side navigation list of categories — Display, Appearance, Advanced, and About. The About section integrates the previously separate About dialog, showing the version and a link to the GitHub repository. Navigation between categories is smooth and keyboard-accessible.

**Why this priority**: Improves user experience and consolidates the About dialog. Also establishes the foundation for all future overlay apps to share a consistent settings UI pattern.

**Independent Test**: Open the settings window via the system tray. Verify all settings categories are present, all existing settings controls are accessible, the About section shows the correct version and GitHub link, and all settings can be saved and loaded without data loss.

**Acceptance Scenarios**:

1. **Given** WheelOverlay is running, **When** the user opens settings, **Then** a window with a side navigation list appears showing Display, Appearance, Advanced, and About categories
2. **Given** the settings window is open on the About category, **When** the user views it, **Then** the application version and a clickable GitHub link are displayed
3. **Given** the settings window is open, **When** the user navigates categories using keyboard arrow keys, **Then** focus moves correctly between categories and their controls

---

### User Story 6 - Find Help and Documentation for WheelOverlay (Priority: P3)

A new WheelOverlay user has installed the app but is unsure how to configure it for their setup. They visit the documentation and find a getting-started guide that walks them through first launch, a usage guide explaining each layout type, tips for getting the best experience during sim racing, and a troubleshooting section for common problems like the DirectInput device not being detected.

**Why this priority**: Documentation reduces support burden and improves onboarding. Lower priority than functional changes but important for user adoption.

**Independent Test**: Follow the getting-started guide from a clean install to a working overlay configuration without any prior knowledge of the app.

**Acceptance Scenarios**:

1. **Given** a user has just installed WheelOverlay, **When** they follow the getting-started guide, **Then** they can complete initial configuration and see the overlay working with their device
2. **Given** a user cannot see the overlay, **When** they consult the troubleshooting guide, **Then** they find the relevant issue and a solution described in plain language
3. **Given** a user wants to try a different layout, **When** they read the usage guide, **Then** each of the five layout types is explained with enough context to choose the right one

---

### Edge Cases

- What happens when a user's existing settings file has fields that were added or removed between versions? The app must load gracefully using defaults for any missing fields.
- What happens when the hotkey registration fails because another application already claims Alt+F6? The app continues operating; the system tray menu remains the fallback.
- What happens when the shared infrastructure changes in a way that breaks a downstream overlay application? The build pipeline should catch this before release.
- What happens when a developer pushes a version tag for an app whose version in the project config doesn't match? The release pipeline must fail with a clear, actionable error.
- What happens when the user's operating system is not running a sim racing application that ProcessMonitor is watching? The overlay should remain visible (fail-open behavior).

## Requirements *(mandatory)*

### Functional Requirements

**Monorepo Structure**

- **FR-001**: The repository MUST organize overlay applications, shared infrastructure, tests, installers, scripts, and documentation into clearly separated top-level directories
- **FR-002**: The repository MUST support building all projects from a single entry point at the repository root
- **FR-003**: The repository MUST support building any single overlay application independently without building other overlay applications
- **FR-004**: Adding a new overlay application MUST follow the same directory and naming conventions as existing overlays, requiring no changes to shared infrastructure

**Shared Overlay Infrastructure**

- **FR-005**: A shared infrastructure library MUST contain reusable services for theme detection, logging, process monitoring, window transparency, and system tray setup
- **FR-006**: The shared library MUST NOT carry its own independent version number — it is always consumed as source alongside the overlay application that depends on it
- **FR-007**: All overlay applications MUST be able to inherit overlay mode cycling behavior without additional configuration by using the shared infrastructure. The cycling API MUST be designed for extensibility so additional modes can be added in the future without breaking existing consumers. The initial implementation supports exactly two modes: Normal and Positioning.

**WheelOverlay Compatibility**

- **FR-008**: WheelOverlay MUST retain all existing functionality: rotary input polling, five layout types, profiles, theme selection, and settings persistence
- **FR-009**: WheelOverlay MUST load existing user settings files from their current location without requiring any migration step
- **FR-010**: WheelOverlay MUST use shared infrastructure services in place of its own duplicated implementations, with no user-facing behavioral change

**Independent Versioning and Releases**

- **FR-011**: Each overlay application MUST maintain its own independent version number
- **FR-012**: Release tags MUST use the namespaced format `{app-name}/vX.Y.Z` to scope releases to individual applications
- **FR-013**: A release pipeline MUST validate that the version in the release tag matches the version declared in the application's project configuration before proceeding

**CI/CD Pipelines**

- **FR-014**: Each overlay application MUST have a dedicated release pipeline that triggers only when files relevant to that application change
- **FR-015**: Release pipelines MUST support two trigger mechanisms: automatic triggers on relevant file changes to the main branch, and explicit triggers via namespaced version tags
- **FR-016**: Pull request validation pipelines MUST run for all source and test paths across the monorepo. A change to `src/OverlayCore/` MUST trigger PR checks for every overlay application that depends on it (in addition to OverlayCore's own checks), since OverlayCore is consumed as source and a change there can break any downstream app.
- **FR-017**: Property-based tests MUST run with a reduced iteration count on pull requests and full iteration count on release builds

**Settings UI Framework**

- **FR-018**: The shared `SettingsWindow` in OverlayCore MUST present settings organized into named categories accessible via a side navigation list. Overlay applications populate the window by registering `ISettingsCategory` implementations at startup; no per-app WPF window duplication is permitted. `ISettingsCategory` MUST expose exactly five members: `string CategoryName` (display label in the nav list), `int SortOrder` (ascending sort order; lower value = higher position), `FrameworkElement CreateContent()` (factory method called by `SettingsWindow` on demand to create the category's content panel), `void LoadValues()` (called by `SettingsWindow` when the user navigates to the category), and `void SaveValues()` (called by `SettingsWindow` on the outgoing category when the user navigates away, and on all registered categories when the user clicks OK or Apply). The About category is always present and always last (highest `SortOrder` value).
- **FR-019**: The settings window MUST include an About category displaying the application version and a link to the project repository
- **FR-020**: The About category MUST replace the existing separate About dialog — there MUST NOT be two separate About surfaces
- **FR-021**: The settings window MUST support keyboard navigation across all categories and controls

**Global Mode Cycling Hotkey**

- **FR-022**: WheelOverlay MUST register a system-wide keyboard shortcut (Alt+F6) that cycles the overlay between normal click-through mode and drag-to-reposition mode
- **FR-023**: When cycling from positioning mode back to normal mode via the hotkey, the current position MUST be saved (equivalent to confirming the drag)
- **FR-024**: If the hotkey cannot be registered because another application holds the key combination, WheelOverlay MUST log a descriptive error and continue operating normally without the hotkey

**Shared Visual Resources**

- **FR-025**: A shared `ResourceDictionary` of typography definitions MUST be available in OverlayCore for all overlay applications to consume. It MUST include the Roboto font family plus all font families, sizes, and weights currently defined locally in WheelOverlay — extracted without modification. No additional fonts beyond these are in scope.
- **FR-026**: WheelOverlay MUST replace its local font definitions with references to the shared typography resources

**User Documentation**

- **FR-027**: Published documentation MUST exist for WheelOverlay covering getting started, usage of all layout types and profiles, tips for sim racing use, and troubleshooting common issues
- **FR-028**: Documentation MUST be stored in the repository and suitable for publication via a static site generator. Minimum compatibility criteria: each file MUST include a YAML frontmatter block with at least a `title` key; body content MUST use standard Markdown without raw HTML blocks; file names MUST be lowercase with words separated by hyphens
- **FR-029**: When a new overlay application is added with source code under `src/`, its documentation MUST be added in a dedicated section following the same structure as WheelOverlay's documentation (placeholder-only apps with no source are exempt until source is implemented)

**Developer Documentation**

- **FR-030**: The repository README MUST describe the monorepo structure, build instructions, directory layout, and how to add a new overlay application
- **FR-031**: A contributing guide MUST document the branch naming convention (`<type>/<description>` per Constitution §VI; valid prefixes: `feat/`, `fix/`, `docs/`, `test/`, `refactor/`, `chore/`, `perf/`), versioning approach, and release tag format

### Key Entities

- **Overlay Application**: A standalone application that displays an always-on-top, click-through information overlay. Has its own version, installer, release pipeline, settings, and documentation. Depends on OverlayCore for shared behavior.
- **Shared Infrastructure (OverlayCore)**: A library containing services shared across all overlay applications — theme detection, logging, process monitoring, window transparency management, system tray scaffolding, settings UI framework, and typography resources. Not independently versioned.
- **Settings Category**: A named panel of related settings that an overlay application registers with `MaterialSettingsWindow` by implementing `ISettingsCategory`. Exposes the five members defined in FR-018. Displayed in a side navigation list sorted ascending by `SortOrder`; the About category is always last (highest `SortOrder` value).
- **Overlay Mode**: The normal operating state where the overlay is always on top, click-through, and displaying its content. One of exactly two modes in the initial implementation; the cycling API is extensible for future modes.
- **Positioning Mode**: A temporary state where the overlay is draggable by the user, with click-through disabled, allowing repositioning. Exiting this mode saves the position.
- **Release Tag**: A namespaced git tag in the format `{app-name}/vX.Y.Z` that explicitly triggers a release pipeline for a specific overlay application.

## Non-Functional Requirements

- **NFR-001**: Each overlay application MUST consume less than 2% CPU and less than 50MB RAM while idle (overlay visible, no user interaction, no active sim racing process detected)
- **NFR-002**: The WheelOverlay MSI installer MUST use a fixed `UpgradeCode` (consistent across all versions) so that installing a new version performs an in-place upgrade — replacing application binaries without touching user files in `%APPDATA%\WheelOverlay\`. This directly satisfies SC-004 (zero reconfigure prompts on upgrade).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can add a new overlay application stub to the repository and trigger an independent test build in under 30 minutes of setup time
- **SC-002**: Pushing a valid `wheel-overlay/vX.Y.Z` release tag results in a published GitHub release with installer artifact within the existing CI/CD pipeline execution time
- **SC-003**: 100% of existing WheelOverlay automated tests pass after the restructuring with no changes to test logic
- **SC-004**: A user with an existing settings file from a pre-restructuring WheelOverlay installation experiences zero prompts to reconfigure on first launch of the updated version
- **SC-005**: A user can reposition the overlay using only the keyboard (Alt+F6 to enter positioning mode, drag, Alt+F6 to confirm) without interacting with the system tray
- **SC-006**: The settings window presents all existing configuration options accessible in 3 clicks or fewer from the system tray icon
- **SC-007**: A new user can complete initial WheelOverlay setup by following the getting-started documentation alone, without external assistance

## Assumptions

- The repository targets Windows only; no cross-platform overlay support is in scope
- "Shared infrastructure" is consumed by overlay applications by compiling from source alongside the overlay, not as a separately packaged and published library
- Existing user settings files are stored at `%APPDATA%\WheelOverlay\settings.json`; this path must be preserved exactly after the restructuring
- The Alt+F6 key combination is chosen for the global hotkey; if a user's system has a conflict, they must use the system tray menu as the fallback (no in-app hotkey remapping is in scope)
- The second overlay application mentioned in CI/CD requirements (discord-notify) does not need to be fully implemented as part of this feature. A placeholder `.github/workflows/discord-notify-release.yml` with path filters and a namespaced tag trigger (`discord-notify/vX.Y.Z`) is the required deliverable — no app source code under `src/` is needed
- Material Design-inspired styles are implemented using existing WPF XAML capabilities; no third-party design toolkit dependency is assumed unless already present