# Requirements Document

## Introduction

This feature restructures the existing WheelOverlay single-purpose repository into a monorepo called "OpenDash-Overlays." The restructuring extracts reusable overlay infrastructure (theme detection, logging, process monitoring, window transparency, config-mode drag, system tray scaffolding) into a shared class library called OverlayCore, while keeping WheelOverlay as the first overlay application. The monorepo supports independent versioning, per-app CI/CD pipelines with path filters, per-app MSI installers, and namespaced git tags. End users see independent applications and are unaware of the shared codebase. Additionally, the existing procedural SettingsWindow is refactored into a Material Design-inspired XAML-based settings framework in OverlayCore, with the About dialog integrated as a settings category. OverlayCore also bundles shared font resources and typography utilities for consistent styling across overlay applications. Tag-based CI/CD release triggers complement the existing path-filtered workflows, giving developers explicit control over when releases are published. User-facing documentation covering usage guides, tips, and how-to content is created and published for each overlay application, starting with WheelOverlay. A global Alt+F6 hotkey is handled in OverlayCore to cycle overlay windows between normal overlay mode and positioning mode, allowing all overlay applications to inherit the mode-cycling behavior.

## Glossary

- **Monorepo**: A single Git repository containing multiple related projects with shared infrastructure
- **OverlayCore**: A .NET class library containing reusable overlay services and utilities shared across overlay applications
- **WheelOverlay**: The existing overlay application for BavarianSimTec Alpha rotary encoders used in sim racing
- **Solution_File**: The top-level `OpenDash-Overlays.sln` file that references all projects in the monorepo
- **ThemeService**: A service that detects the Windows system theme (light/dark), applies matching WPF resource dictionaries, and watches for runtime theme changes
- **LogService**: A file-based logging service with log rotation (truncation at 1 MB)
- **ProcessMonitor**: A WMI-based service that monitors running processes to conditionally show or hide overlay windows
- **WindowTransparencyHelper**: Win32 interop utilities for making WPF windows click-through using WS_EX_TRANSPARENT extended window style
- **ConfigModeBehavior**: The drag-to-reposition pattern where Enter confirms and Escape cancels the overlay position change
- **BaseOverlayWindow**: A set of base window patterns including topmost, no-taskbar, transparent background, and SizeToContent behavior
- **SystemTrayScaffold**: Reusable NotifyIcon setup patterns for system tray integration
- **Path_Filter**: A GitHub Actions workflow configuration that triggers CI/CD jobs only when files in specific directories change
- **Namespaced_Tag**: A git tag prefixed with the app name (e.g., `wheel-overlay/v0.7.0`) to support independent releases per overlay app
- **ProjectReference**: A .csproj reference to another project in the same solution, compiled from source rather than consumed as a NuGet package
- **MaterialDesignSettings**: A XAML-based settings framework in OverlayCore using Material Design-inspired styles, providing a tabbed/categorized settings UI that overlay apps extend with their own settings panels
- **SharedFontResources**: Font resource dictionaries and utilities bundled in OverlayCore for consistent typography across overlay applications
- **Tag_Trigger**: A GitHub Actions workflow trigger that fires when a git tag matching a specific pattern is pushed to the repository
- **UserDocumentation**: Published Markdown-based usage guides, tips, how-to content, and troubleshooting information for each overlay application, stored in the `docs/` directory
- **GlobalHotkey**: A system-wide keyboard shortcut registered via Win32 RegisterHotKey that functions regardless of which application currently has focus
- **OverlayMode**: The normal operating state of an overlay window where the window is topmost, click-through, and displaying its content
- **PositioningMode**: The state where an overlay window disables click-through transparency and enables ConfigModeBehavior drag-to-reposition, allowing the user to move the overlay

## Requirements

### Requirement 1: Monorepo Directory Structure

**User Story:** As a developer, I want the repository organized into a standard monorepo layout, so that I can add new overlay applications alongside existing ones with clear separation of concerns.

#### Acceptance Criteria

1. THE Solution_File SHALL organize source projects under a `src/` directory containing `src/OverlayCore/` and `src/WheelOverlay/`
2. THE Solution_File SHALL organize test projects under a `tests/` directory containing `tests/OverlayCore.Tests/` and `tests/WheelOverlay.Tests/`
3. THE Solution_File SHALL organize installer projects under an `installers/` directory containing `installers/wheel-overlay/`
4. THE Solution_File SHALL reference all projects (OverlayCore, WheelOverlay, OverlayCore.Tests, WheelOverlay.Tests) and build from the repository root
5. WHEN a developer runs `dotnet build` from the repository root, THE Solution_File SHALL build all projects in the correct dependency order
6. THE Solution_File SHALL place shared assets under a top-level `assets/` directory accessible to all overlay applications
7. THE Solution_File SHALL place per-overlay build scripts under `scripts/{app-name}/` directories (e.g., `scripts/wheel-overlay/`) and shared utility scripts under the top-level `scripts/` directory

### Requirement 2: OverlayCore Shared Class Library

**User Story:** As a developer, I want common overlay infrastructure extracted into a shared library, so that new overlay applications can reuse theme detection, logging, process monitoring, and window management without duplicating code.

#### Acceptance Criteria

1. THE OverlayCore SHALL be a .NET 10.0-windows class library with WPF and WinForms support enabled
2. THE OverlayCore SHALL contain the ThemeService with system theme detection, light/dark switching, resource dictionary swapping, and runtime theme change polling
3. THE OverlayCore SHALL contain the LogService with file-based logging, timestamp formatting, severity levels (Info, Error), and log truncation at 1 MB
4. THE OverlayCore SHALL contain the ProcessMonitor with WMI-based process start/stop detection, executable path matching, filename fallback matching, and target update capability
5. THE OverlayCore SHALL contain the WindowTransparencyHelper providing Win32 interop methods (GetWindowLong, SetWindowLong) for applying and removing WS_EX_TRANSPARENT on WPF windows
6. THE OverlayCore SHALL contain the ConfigModeBehavior providing the Enter-to-confirm and Escape-to-cancel overlay repositioning pattern
7. THE OverlayCore SHALL contain the BaseOverlayWindow patterns for topmost, no-taskbar, transparent-background, SizeToContent window configuration
8. THE OverlayCore SHALL contain the SystemTrayScaffold providing reusable NotifyIcon setup patterns
9. THE OverlayCore SHALL contain shared models including the ThemePreference enum (System, Light, Dark)
10. THE OverlayCore SHALL use the `OpenDash.OverlayCore` root namespace
11. THE OverlayCore SHALL declare its dependency on `System.Management` for WMI-based process monitoring
12. THE OverlayCore SHALL NOT have its own independent version number (the library is consumed via ProjectReference, not as a NuGet package)

### Requirement 3: WheelOverlay Application Migration

**User Story:** As a developer, I want WheelOverlay to reference OverlayCore and retain all existing functionality, so that the restructuring does not break any user-facing behavior.

#### Acceptance Criteria

1. THE WheelOverlay SHALL reference OverlayCore via a ProjectReference to `../OverlayCore/OverlayCore.csproj`
2. THE WheelOverlay SHALL retain all existing functionality: DirectInput polling, rotary position display, 5 layout types (Single, Vertical, Horizontal, Grid, Dial), profiles, and settings management
3. THE WheelOverlay SHALL use the `OpenDash.WheelOverlay` root namespace
4. THE WheelOverlay SHALL maintain its own independent version number in its .csproj file (Version, AssemblyVersion, FileVersion)
5. THE WheelOverlay SHALL continue to store user settings in `%APPDATA%\WheelOverlay\settings.json`
6. THE WheelOverlay SHALL continue to declare its dependency on `Vortice.DirectInput` for hardware input polling
7. THE WheelOverlay SHALL remove duplicated code that has been extracted into OverlayCore and use the shared implementations instead
8. WHEN a user launches WheelOverlay after the restructuring, THE WheelOverlay SHALL load existing settings from `%APPDATA%\WheelOverlay\settings.json` without requiring migration or reconfiguration

### Requirement 4: Test Project Migration

**User Story:** As a developer, I want the existing test suite to continue passing after the restructuring, so that I have confidence the migration did not introduce regressions.

#### Acceptance Criteria

1. THE WheelOverlay.Tests SHALL be relocated to `tests/WheelOverlay.Tests/` and reference the WheelOverlay project at its new path
2. THE OverlayCore.Tests SHALL be a new xUnit test project located at `tests/OverlayCore.Tests/` that references the OverlayCore project
3. THE OverlayCore.Tests SHALL contain tests for the extracted shared services (ThemeService, LogService, ProcessMonitor)
4. WHEN a developer runs `dotnet test` from the repository root, THE Solution_File SHALL execute tests from all test projects
5. THE WheelOverlay.Tests SHALL update namespace references from `WheelOverlay.Services` to `OpenDash.OverlayCore` for extracted services
6. THE WheelOverlay.Tests SHALL continue to use xUnit with FsCheck for property-based testing
7. THE WheelOverlay.Tests SHALL maintain the existing FastTests/Release configuration for controlling property-based test iteration counts

### Requirement 5: Installer Restructuring

**User Story:** As a developer, I want each overlay application to have its own MSI installer in a dedicated directory, so that new overlay apps can ship independent installers.

#### Acceptance Criteria

1. THE WheelOverlay installer SHALL be relocated to `installers/wheel-overlay/` containing the WiX Package.wxs and CustomUI.wxs files
2. THE WheelOverlay installer SHALL continue to produce a WheelOverlay.msi that installs to `Program Files\WheelOverlay`
3. THE WheelOverlay installer SHALL continue to create Start Menu and Desktop shortcuts named "Wheel Overlay"
4. THE WheelOverlay installer SHALL read its version from the WheelOverlay .csproj file
5. WHEN a new overlay application is added to the monorepo, THE installer directory structure SHALL accommodate a new subdirectory under `installers/` for the new application installer

### Requirement 6: CI/CD Pipeline with Path Filters

**User Story:** As a developer, I want CI/CD workflows scoped to individual overlay applications using path filters, so that changes to one overlay do not trigger builds and releases for unrelated overlays.

#### Acceptance Criteria

1. THE WheelOverlay release workflow SHALL trigger only when files change in `src/WheelOverlay/`, `src/OverlayCore/`, `tests/WheelOverlay.Tests/`, `tests/OverlayCore.Tests/`, or `installers/wheel-overlay/`
2. THE WheelOverlay release workflow SHALL read the version from `src/WheelOverlay/WheelOverlay.csproj`
3. THE WheelOverlay release workflow SHALL create a GitHub release with a namespaced tag in the format `wheel-overlay/vX.Y.Z`
4. THE branch build check workflow SHALL trigger for all source and test paths in the monorepo
5. THE pre-merge validation workflow SHALL trigger for all source and test paths in the monorepo
6. WHEN a change affects only OverlayCore, THE WheelOverlay release workflow SHALL still trigger (since WheelOverlay depends on OverlayCore)
7. THE CI/CD workflows SHALL continue to validate property test directives using the existing PowerShell validation script
8. THE CI/CD workflows SHALL continue to support FastTests (10 iterations) for PR builds and Release (100 iterations) for merge builds

### Requirement 7: Independent Versioning

**User Story:** As a developer, I want each overlay application to maintain its own version number independently, so that releasing a new version of one overlay does not require version changes in other overlays.

#### Acceptance Criteria

1. THE WheelOverlay SHALL maintain its version in `src/WheelOverlay/WheelOverlay.csproj` with Version, AssemblyVersion, and FileVersion properties
2. THE WheelOverlay installer SHALL read its version from the WheelOverlay .csproj file and embed the version in the MSI package
3. THE OverlayCore SHALL NOT have an independent version number (the library is versioned implicitly through the consuming application)
4. WHEN a new overlay application is added, THE new application SHALL define its own version independently in its .csproj file
5. THE git tags SHALL use the namespaced format `{app-name}/v{major}.{minor}.{patch}` (e.g., `wheel-overlay/v0.7.0`)

### Requirement 8: Namespace Migration

**User Story:** As a developer, I want consistent namespaces across the monorepo under the OpenDash root, so that the codebase reflects the rebranded project identity.

#### Acceptance Criteria

1. THE OverlayCore SHALL use the root namespace `OpenDash.OverlayCore` with sub-namespaces for Services (`OpenDash.OverlayCore.Services`) and Models (`OpenDash.OverlayCore.Models`)
2. THE WheelOverlay SHALL use the root namespace `OpenDash.WheelOverlay` with sub-namespaces for Models, ViewModels, Views, Converters, and Services
3. WHEN code in WheelOverlay references a type extracted to OverlayCore, THE WheelOverlay source files SHALL use the `OpenDash.OverlayCore` namespace via using directives
4. THE WheelOverlay.Tests SHALL update using directives to reference the new `OpenDash.OverlayCore` and `OpenDash.WheelOverlay` namespaces

### Requirement 9: Per-Overlay Build and Package Scripts

**User Story:** As a developer, I want each overlay application to have its own build and packaging scripts, so that I can build, package, and release each overlay independently without affecting other overlays.

#### Acceptance Criteria

1. EACH overlay application SHALL have its own set of build and packaging scripts located in `scripts/{app-name}/` (e.g., `scripts/wheel-overlay/build_msi.ps1`, `scripts/wheel-overlay/build_release.ps1`)
2. THE WheelOverlay build_msi.ps1 script SHALL reference the WheelOverlay project at `src/WheelOverlay/WheelOverlay.csproj` and the installer at `installers/wheel-overlay/`
3. THE WheelOverlay build_release.ps1 script SHALL produce a zip file named with the WheelOverlay version read from `src/WheelOverlay/WheelOverlay.csproj`
4. THE WheelOverlay generate_components.ps1 script SHALL generate WiX component entries for the WheelOverlay installer at `installers/wheel-overlay/`
5. WHEN a new overlay application is added to the monorepo, THE new application SHALL have its own scripts in `scripts/{app-name}/` following the same conventions
6. SHARED utility scripts (e.g., property test validation) SHALL remain in the top-level `scripts/` directory and be invocable by any overlay's build scripts
7. THE property test validation scripts SHALL accept a test project path parameter so they can scan `tests/WheelOverlay.Tests/`, `tests/OverlayCore.Tests/`, or any future test project
8. WHEN a developer runs an overlay's build script from the repository root, THE script SHALL complete without path-related errors
9. EACH overlay's build script SHALL be self-contained, building only that overlay and its OverlayCore dependency without building other overlay applications

### Requirement 10: Solution File Configuration

**User Story:** As a developer, I want a single root solution file that references all projects in the monorepo, so that I can navigate, build, and debug across all overlay applications and shared code in one IDE session.

#### Acceptance Criteria

1. THE Solution_File SHALL be named `OpenDash-Overlays.sln` and located at the repository root
2. THE Solution_File SHALL use solution folders to organize projects: `src` folder for OverlayCore and all overlay applications, `tests` folder for all test projects
3. THE Solution_File SHALL support Debug, FastTests, and Release build configurations for all projects
4. WHEN a developer opens `OpenDash-Overlays.sln` in Visual Studio, THE Solution_File SHALL display projects organized by solution folders with cross-project navigation and IntelliSense
5. THE old `WheelOverlay.sln` file SHALL be removed after the migration
6. WHEN a developer needs to build only a single overlay, THE developer SHALL use `dotnet build src/{AppName}/{AppName}.csproj` directly rather than requiring a per-overlay .sln file
7. THE Solution_File SHALL automatically resolve OverlayCore as a dependency when building any individual overlay project via `dotnet build` on its .csproj

### Requirement 11: Documentation Updates

**User Story:** As a developer, I want the README and contributing documentation updated to reflect the monorepo structure, so that contributors understand how to navigate, build, and add new overlay applications.

#### Acceptance Criteria

1. THE README.md SHALL describe the OpenDash-Overlays monorepo structure and purpose
2. THE README.md SHALL include build instructions referencing `OpenDash-Overlays.sln`
3. THE README.md SHALL document the directory layout (`src/`, `tests/`, `installers/`, `assets/`, `scripts/`)
4. THE README.md SHALL explain how to add a new overlay application to the monorepo
5. THE CONTRIBUTING.md SHALL document the branch naming convention with app-scoped prefixes (e.g., `feat/wheel-overlay/...`, `feat/overlay-core/...`)
6. THE CONTRIBUTING.md SHALL document the namespaced git tag format for releases
7. THE CHANGELOG.md SHALL contain an entry documenting the monorepo restructuring

### Requirement 12: Settings UI Refactor with Material Design in XAML

**User Story:** As a developer, I want the settings UI refactored from procedural code-behind into a Material Design-inspired XAML-based framework in OverlayCore, so that all overlay applications share a consistent, modern settings experience and the About dialog is consolidated into the settings window.

#### Acceptance Criteria

1. THE MaterialDesignSettings SHALL replace the existing procedural code-behind SettingsWindow with XAML templates, styles, and data bindings for all UI elements
2. THE MaterialDesignSettings SHALL use Material Design-inspired XAML styles for controls including buttons, text fields, sliders, combo boxes, toggle switches, and tab headers
3. THE MaterialDesignSettings SHALL present settings organized into categories displayed as tabs or a navigation list within a single settings window
4. THE MaterialDesignSettings SHALL include an "About" category that displays the application version, a clickable GitHub repository link, and the application icon
5. WHEN the MaterialDesignSettings "About" category is implemented, THE separate AboutWindow SHALL be removed from the application
6. THE MaterialDesignSettings SHALL reside in OverlayCore as a base settings framework that overlay applications extend by registering their own settings categories
7. WHEN an overlay application registers a settings category, THE MaterialDesignSettings SHALL display the registered category alongside the shared categories (e.g., About)
8. THE WheelOverlay SHALL register its overlay-specific settings (layout, profiles, DirectInput configuration, theme preferences) as categories in the MaterialDesignSettings framework
9. THE MaterialDesignSettings SHALL support keyboard navigation and focus management across all settings categories and controls

### Requirement 13: Shared Font Resources in OverlayCore

**User Story:** As a developer, I want OverlayCore to bundle shared font resources and typography utilities, so that all overlay applications use consistent fonts and text styling without duplicating font configuration.

#### Acceptance Criteria

1. THE SharedFontResources SHALL include resource dictionaries defining FontFamily, FontSize, and FontWeight resources for common overlay typography (e.g., Segoe UI, monospace fonts)
2. THE SharedFontResources SHALL provide a mechanism for overlay applications to merge the shared font resource dictionaries into their application resources
3. WHEN an overlay application merges the SharedFontResources dictionaries, THE overlay application SHALL be able to reference shared font resources by key in XAML bindings and styles
4. THE SharedFontResources SHALL include font-related utility classes providing FontFamily helpers and FontWeight converters for use in XAML data bindings
5. THE SharedFontResources SHALL support the configurable font size, font weight, and text rendering mode patterns currently used by WheelOverlay
6. THE WheelOverlay SHALL replace its local font definitions with references to the SharedFontResources provided by OverlayCore

### Requirement 14: Tag-Based CI/CD Release Triggers

**User Story:** As a developer, I want to trigger releases by pushing git tags in addition to path-based auto-triggers, so that I have explicit control over when a release is published for a specific overlay application.

#### Acceptance Criteria

1. WHEN a git tag matching the pattern `wheel-overlay/v{major}.{minor}.{patch}` is pushed, THE WheelOverlay release workflow SHALL trigger a release build for WheelOverlay
2. WHEN a git tag matching the pattern `discord-notify/v{major}.{minor}.{patch}` is pushed, THE corresponding release workflow SHALL trigger a release build for the Discord Notify application
3. THE Tag_Trigger SHALL extract the version number from the pushed tag and validate that the extracted version matches the Version property in the corresponding application .csproj file
4. IF the version extracted from the tag does not match the .csproj Version property, THEN THE release workflow SHALL fail with a descriptive error message identifying the version mismatch
5. THE Tag_Trigger SHALL coexist with the existing Path_Filter triggers so that releases can be initiated by either a merge to main with matching path changes or by pushing a namespaced tag
6. THE tag format SHALL follow the convention `{app-name}/v{major}.{minor}.{patch}` using hyphenated app names (e.g., `wheel-overlay`, `discord-notify`)
7. WHEN a Tag_Trigger initiates a release, THE release workflow SHALL execute the same build, test, and packaging steps as a Path_Filter-initiated release

### Requirement 15: User Documentation for Overlay Applications

**User Story:** As an end user, I want published documentation covering usage guides, tips, and how-to content for each overlay application, so that I can learn how to set up, configure, and get the most out of the overlays without guessing.

#### Acceptance Criteria

1. THE UserDocumentation SHALL be created and published for each overlay application, starting with WheelOverlay
2. THE UserDocumentation SHALL include a getting-started guide covering installation, first launch, and initial configuration for WheelOverlay
3. THE UserDocumentation SHALL include usage guides explaining each layout type (Single, Vertical, Horizontal, Grid, Dial), profile management, and theme configuration in WheelOverlay
4. THE UserDocumentation SHALL include tips and best-practices content for optimizing overlay positioning, readability, and performance during sim racing sessions
5. THE UserDocumentation SHALL include a troubleshooting section covering common issues such as the overlay not appearing, DirectInput device not detected, and settings not saving
6. WHEN a new overlay application is added to the monorepo, THE UserDocumentation SHALL be extended with a dedicated section for the new overlay application
7. THE UserDocumentation SHALL be stored in a `docs/` directory at the repository root, organized by overlay application (e.g., `docs/wheel-overlay/`)
8. THE UserDocumentation SHALL be written in Markdown format suitable for publishing via GitHub Pages or a similar static site generator

### Requirement 16: Keybind for Cycling Overlay Modes

**User Story:** As an end user, I want a keyboard shortcut to toggle between overlay mode and positioning mode, so that I can quickly reposition the overlay without navigating the system tray menu.

#### Acceptance Criteria

1. WHEN the user presses Alt+F6, THE OverlayCore SHALL cycle the active overlay window between overlay mode and positioning mode
2. WHILE the overlay is in overlay mode, THE OverlayCore SHALL display the overlay in its normal click-through, topmost state
3. WHILE the overlay is in positioning mode, THE OverlayCore SHALL disable click-through transparency and enable the ConfigModeBehavior drag-to-reposition pattern
4. WHEN the overlay transitions from positioning mode back to overlay mode via Alt+F6, THE OverlayCore SHALL confirm the current position (equivalent to pressing Enter in ConfigModeBehavior)
5. THE OverlayCore SHALL register Alt+F6 as a global hotkey so that the keybind functions regardless of which application has focus
6. IF the Alt+F6 hotkey registration fails because another application has reserved the key combination, THEN THE OverlayCore SHALL log a descriptive error and continue operating without the hotkey
7. THE OverlayCore SHALL expose the keybind through a shared service so that all overlay applications inherit the mode-cycling behavior without additional configuration
