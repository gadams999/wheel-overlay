# Requirements Document

## Introduction

Version 0.6.0 of WheelOverlay introduces two major enhancements: a skeuomorphic dial layout that visually mirrors the physical position arrangement of the Bavarian SimTec Alpha wheel, and full dark mode/light mode theming that follows native Windows 10/11 appearance settings. These features improve the overlay's visual fidelity and integrate it more naturally with the user's desktop environment.

## Glossary

- **Overlay**: The transparent, always-on-top WPF window that displays wheel position information to the user.
- **Dial_Layout**: A new display layout mode that arranges position labels in a circular arc mimicking the physical rotary dial on the Bavarian SimTec Alpha wheel.
- **Position**: A numbered slot (1–8) on the rotary dial, each corresponding to a text label configured by the user.
- **Theme_Service**: The application component responsible for detecting the current Windows theme and applying the corresponding visual resources.
- **Light_Mode**: A visual theme using light backgrounds and dark text, matching the Windows light app mode.
- **Dark_Mode**: A visual theme using dark backgrounds and light text, matching the Windows dark app mode.
- **System_Theme**: The app mode preference configured in Windows Settings under Personalization > Colors ("Light", "Dark", or "Custom").
- **Settings_Window**: The configuration dialog where users manage profiles, layouts, appearance, and advanced options.
- **Profile**: A named configuration containing layout mode, text labels, font settings, and other per-profile preferences.
- **Resource_Dictionary**: A WPF mechanism for defining reusable styles, colors, and templates that can be swapped at runtime for theming.

## Requirements

### Requirement 1: Dial Layout Mode

**User Story:** As a sim racer, I want a layout that mirrors the physical positions on my Bavarian SimTec wheel, so that the overlay matches my muscle memory and I can glance at it intuitively.

#### Acceptance Criteria

1. THE Overlay SHALL support a "Dial" option in the DisplayLayout enum alongside the existing Single, Vertical, Horizontal, and Grid modes.
2. WHEN the active profile's layout is set to Dial, THE Overlay SHALL render position labels arranged in a circular arc pattern.
3. THE Dial_Layout SHALL place Position 1 at approximately the 1 o'clock angle on the arc.
4. THE Dial_Layout SHALL place Position 4 at approximately the 5 o'clock angle on the arc.
5. THE Dial_Layout SHALL place Position 5 at approximately the 7 o'clock angle on the arc.
6. THE Dial_Layout SHALL place Position 8 at approximately the 11 o'clock angle on the arc.
7. THE Dial_Layout SHALL distribute positions evenly within the right arc (Positions 1–4, from 1 o'clock to 5 o'clock) and the left arc (Positions 5–8, from 7 o'clock to 11 o'clock).
8. WHEN the selected position changes, THE Dial_Layout SHALL highlight the active position label using the configured selected text color and dim non-active labels using the configured non-selected text color.
9. WHEN the active position has an empty text label, THE Dial_Layout SHALL trigger the same flash animation used by other layout modes.
10. THE Dial_Layout SHALL respect the font size, font family, and text rendering settings from the active profile.

### Requirement 2: Dial Layout in Settings

**User Story:** As a user, I want to select the Dial layout from the settings window, so that I can switch to the skeuomorphic view for my wheel.

#### Acceptance Criteria

1. THE Settings_Window SHALL include "Dial" as a selectable option in the layout mode dropdown for each profile.
2. WHEN the user selects the Dial layout, THE Settings_Window SHALL hide grid-specific configuration controls (rows, columns, suggested dimensions) since they do not apply to the Dial layout.
3. WHEN the user selects the Dial layout and clicks Apply, THE Overlay SHALL switch to the Dial_Layout rendering immediately.
4. THE Settings_Window SHALL persist the Dial layout choice in the profile's JSON settings file.
5. WHEN a settings file containing a Dial layout value is loaded, THE AppSettings SHALL deserialize the Dial layout without error.

### Requirement 3: Dial Layout Positioning Model

**User Story:** As a developer, I want the dial position angles to be defined in a data-driven model, so that positions can be adjusted during development without modifying layout rendering code.

#### Acceptance Criteria

1. THE Dial_Layout SHALL read position angles from a configuration data structure rather than hardcoding angles in the rendering logic.
2. THE configuration data structure SHALL define an angle in degrees for each position index (1–8), where 0 degrees represents 12 o'clock and angles increase clockwise.
3. WHEN the position count in the active profile differs from 8, THE Dial_Layout SHALL distribute positions evenly across the full arc using the available position count.
4. IF the position angle configuration contains fewer entries than the profile's position count, THEN THE Dial_Layout SHALL fall back to even distribution across the arc.

### Requirement 4: Dark Mode Theme Detection

**User Story:** As a user, I want the overlay and settings window to match my Windows theme, so that the application feels native on my desktop.

#### Acceptance Criteria

1. THE Theme_Service SHALL detect the current Windows app mode (Light or Dark) by reading the registry key `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`.
2. WHEN the application starts, THE Theme_Service SHALL apply the theme matching the current System_Theme.
3. WHEN the Windows theme changes while the application is running, THE Theme_Service SHALL detect the change and update the application theme within 2 seconds.
4. IF the registry key is unreadable or missing, THEN THE Theme_Service SHALL default to Light_Mode.

### Requirement 5: Dark Mode Settings Window

**User Story:** As a user, I want the settings window to follow my Windows theme, so that the configuration dialog is comfortable to use in dark environments.

#### Acceptance Criteria

1. WHILE Dark_Mode is active, THE Settings_Window SHALL render with a dark background, light text, and theme-appropriate control styling.
2. WHILE Light_Mode is active, THE Settings_Window SHALL render with a light background, dark text, and standard control styling.
3. THE Settings_Window SHALL apply theme colors to all interactive controls including buttons, dropdowns, list boxes, text inputs, and scroll bars.
4. WHEN the theme changes while the Settings_Window is open, THE Settings_Window SHALL update its appearance to match the new theme within 2 seconds.

### Requirement 6: Theme Resource Architecture

**User Story:** As a developer, I want theme colors and styles defined in swappable resource dictionaries, so that adding or modifying themes requires minimal code changes.

#### Acceptance Criteria

1. THE Application SHALL define theme-specific colors and styles in separate WPF Resource_Dictionary files (one for Light_Mode, one for Dark_Mode).
2. THE Application SHALL swap the active Resource_Dictionary at runtime when the theme changes.
3. THE Resource_Dictionary files SHALL define named color resources for: background, foreground text, selected text, non-selected text, control background, control border, and drop shadow color.
4. WHEN a new Resource_Dictionary is applied, THE Application SHALL propagate the updated resources to all open windows (Overlay, Settings_Window, AboutWindow) without requiring a restart.

### Requirement 7: Theme Preference Override

**User Story:** As a user, I want the option to override the system theme with a manual choice, so that I can use dark mode for the overlay even if my system is in light mode.

#### Acceptance Criteria

1. THE Settings_Window SHALL provide a theme selection control with three options: "System Default", "Light", and "Dark".
2. WHEN the user selects "System Default", THE Theme_Service SHALL follow the current Windows System_Theme and respond to runtime changes.
3. WHEN the user selects "Light" or "Dark", THE Theme_Service SHALL apply the chosen theme regardless of the System_Theme.
4. THE AppSettings SHALL persist the user's theme preference across application restarts.
5. WHEN a manual theme override is active and the System_Theme changes, THE Theme_Service SHALL retain the user's manual selection.

### Requirement 8: Theme-Appropriate Icons and Graphics

**User Story:** As a user, I want the application icons and graphics to look correct in both light and dark mode, so that the UI is visually consistent and readable regardless of theme.

#### Acceptance Criteria

1. THE Application SHALL provide separate icon variants (light and dark) for the system tray icon.
2. THE Application SHALL provide separate icon or graphic variants for the Settings_Window toolbar icons and any decorative graphics.
3. WHEN the active theme changes, THE Application SHALL swap to the theme-appropriate icon and graphic variants without requiring a restart.
4. THE Dial_Layout SHALL include a rotary knob graphic asset that is visually appropriate for the overlay's transparent background.
5. ALL icon and graphic assets SHALL be manually created and tested by the developer for visual quality in both Light_Mode and Dark_Mode before integration.

### Requirement 9: Settings Serialization for New Properties

**User Story:** As a developer, I want new settings (theme preference, dial layout) to serialize and deserialize correctly, so that user preferences persist across sessions.

#### Acceptance Criteria

1. THE AppSettings SHALL serialize the theme preference ("System", "Light", or "Dark") to the JSON settings file.
2. THE AppSettings SHALL serialize the Dial layout enum value to the JSON settings file.
3. WHEN loading a settings file that predates v0.6.0 and lacks theme or dial properties, THE AppSettings SHALL apply default values (theme: "System", layout: unchanged) without error.
4. FOR ALL valid AppSettings objects, serializing to JSON then deserializing SHALL produce an equivalent AppSettings object (round-trip property).
