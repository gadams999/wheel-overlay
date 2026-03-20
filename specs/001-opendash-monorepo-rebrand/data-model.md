# Data Model: OpenDash Monorepo Rebrand

**Phase 1 Output** | Branch: `001-opendash-monorepo-rebrand` | Date: 2026-03-18

This document captures the key entities, their shapes, validation rules, and state transitions introduced or modified by this feature.

---

## Entity 1: ISettingsCategory

The contract that overlay apps implement to register settings panels with `MaterialSettingsWindow`.

```
ISettingsCategory
├── CategoryName : string          — Display label in navigation list. Non-empty, unique per window.
├── SortOrder    : int             — Sort position. Lower = appears earlier. 999 reserved for About.
├── CreateContent() → FrameworkElement  — Returns the WPF panel for this category.
├── SaveValues()    → void         — Persists current UI control values to the settings model.
└── LoadValues()    → void         — Loads current settings model values into UI controls.
```

**Validation rules**:
- `CategoryName` must be non-null and non-empty
- `SortOrder` must be a non-negative integer
- `CreateContent()` must return a non-null `FrameworkElement`
- The `AboutSettingsCategory` (SortOrder=999) is always registered by OverlayCore; overlay apps must not claim SortOrder=999

**Relationships**:
- `MaterialSettingsWindow` contains 1..N `ISettingsCategory` instances
- `AboutSettingsCategory` always appears; WheelOverlay contributes `DisplaySettingsCategory`, `AppearanceSettingsCategory`, `AdvancedSettingsCategory`

---

## Entity 2: GlobalHotkeyService

Manages registration and event dispatch for the Alt+F6 global hotkey.

```
GlobalHotkeyService
├── Register(hwnd: IntPtr) → bool   — Registers hotkey. Returns false on conflict; logs error.
├── Unregister()           → void   — Releases hotkey registration.
├── ProcessMessage(msg: int, wParam: IntPtr) → void  — Called from WndProc for WM_HOTKEY.
├── Dispose()              → void   — Unregisters and cleans up.
└── ToggleModeRequested event       — Fired when Alt+F6 is pressed.
```

**State machine**:
```
[OverlayMode] <──Alt+F6──> [PositioningMode]
     ↑                            │
     └── Alt+F6 or Enter ─────────┘ (saves position)
     └── Escape ──────────────────┘ (restores original position)
```

**Invariants**:
- After N toggle operations, mode = initial XOR (N is odd)
- Transition from PositioningMode → OverlayMode via Alt+F6 MUST save position (equivalent to Enter)
- If registration fails, service remains functional but fires no events

**Win32 constants**:
- Hotkey ID: 0x0001 (arbitrary constant, unique per process)
- MOD_ALT: 0x0001
- VK_F6: 0x75
- WM_HOTKEY: 0x0312

---

## Entity 3: SharedFontResources

XAML resource dictionary keys defined in `OverlayCore/Resources/Fonts/SharedFontResources.xaml`.

```
Resource Keys (FontFamily):
├── OverlayFontFamily    — Default UI font (Segoe UI)
└── MonospaceFontFamily  — Monospace display font (Consolas)

Resource Keys (Double):
├── OverlayFontSizeSmall   — 12.0
├── OverlayFontSizeMedium  — 16.0
├── OverlayFontSizeLarge   — 20.0
└── OverlayFontSizeXLarge  — 28.0

Resource Keys (FontWeight):
├── OverlayFontWeightNormal — FontWeights.Normal
└── OverlayFontWeightBold   — FontWeights.Bold
```

**Companion utilities (FontUtilities.cs)**:
```
FontUtilities
├── GetFontFamily(familyName: string) → FontFamily
│     — Returns FontFamily for name. Falls back to Segoe UI for unrecognized names.
│     — Never returns null.
└── ToFontWeight(weightName: string) → FontWeight
      — Converts "Normal", "Bold", "Light", "SemiBold", etc. to WPF FontWeight.
      — Falls back to FontWeights.Normal for unrecognized names.
```

**Validation rules**:
- `GetFontFamily` must handle null/empty input by returning Segoe UI fallback
- `ToFontWeight` must handle null/empty/unrecognized input by returning Normal fallback
- All resource keys must be available after `SharedFontResources.xaml` is merged

---

## Entity 4: OverlayCore Services (existing, documented for completeness)

These entities were extracted in the completed phase of this feature. Documented here for the data model record.

### ThemeService
```
ThemeService
├── IsDarkMode       : bool          — Current resolved theme state
├── Preference       : ThemePreference — System | Light | Dark
├── ThemeChanged event               — Fires when resolved dark/light state changes
├── DetectSystemTheme() → bool       — Reads Windows registry for system theme
├── ApplyTheme(dark: bool) → void    — Swaps theme resource dictionaries
├── StartWatching() → void           — Begins polling for system theme changes
└── Dispose() → void
```

**Determinism invariant** (Property 1): For any `ThemePreference` and any system theme state, `IsDarkMode` is fully determined: Light→false, Dark→true, System→matches system.

### LogService (static)
```
LogService
├── Initialize(appName: string) → void  — Sets log path to %APPDATA%\{appName}\logs.txt
├── Info(message: string) → void
├── Error(message: string) → void
├── Error(message: string, ex: Exception) → void
└── GetLogPath() → string
```

**Truncation invariant** (Property 2): After each write, file size ≤ 1MB + length of most recent message.

### ProcessMonitor
```
ProcessMonitor
├── TargetApplicationStateChanged event — (sender, isRunning: bool)
├── ProcessMonitor(targetPath: string?, pollInterval: TimeSpan)
├── Start() → void
├── Stop() → void
├── UpdateTarget(targetPath: string?) → void
└── Dispose() → void
```

**Match invariant** (Property 3): Match = full path equality (case-insensitive) OR filename equality (case-insensitive), else no match.

---

## Entity 5: Release Tag Format

```
Tag Format: {app-name}/v{major}.{minor}.{patch}
Examples:
  wheel-overlay/v0.7.0
  discord-notify/v1.0.0

Components:
├── app-name  — lowercase, hyphen-separated, non-empty (e.g., "wheel-overlay")
├── major     — non-negative integer
├── minor     — non-negative integer
└── patch     — non-negative integer
```

**Round-trip invariant** (Property 6): Format then parse recovers original app-name and all three version components. Parsed version string must be comparable to `.csproj <Version>` property for equality validation.

**Validation**: CI/CD tag trigger extracts version via `-replace '{app-name}/v', ''`. Compares to `<Version>` in `.csproj` XML. Fails with error if they differ.

---

## Entity 6: AppSettings (existing, unchanged structure, namespace updated)

```
AppSettings (stored at %APPDATA%\WheelOverlay\settings.json)
├── Layout             : DisplayLayout enum
├── TextLabels         : string[]
├── SelectedTextColor  : string (hex)
├── NonSelectedTextColor : string (hex)
├── FontSize           : int
├── FontFamily         : string
├── MoveOverlayOpacity : int
├── ItemSpacing        : int
├── WindowLeft         : double
├── WindowTop          : double
├── SelectedDeviceName : string
├── ThemePreference    : ThemePreference (System | Light | Dark)
├── Profiles           : List<Profile>
└── SelectedProfileId  : Guid
```

**Serialization invariant** (Property 4): Serialize → deserialize round-trip produces equivalent object. JSON enum serialization uses string format (JsonStringEnumConverter) — namespace change does not affect JSON representation.

**Backward compat rule**: Pre-migration `settings.json` files (with `ThemePreference` serialized as string "System"/"Light"/"Dark") must load without migration. Unknown JSON fields must be silently ignored.

---

## State Transitions: Overlay Window Modes

```
[OverlayMode]
  - Window: Topmost=true, AllowsTransparency=true, click-through (WS_EX_TRANSPARENT)
  - Background: fully transparent
  - ConfigModeBehavior: inactive

Alt+F6 pressed
  ↓
[PositioningMode]
  - Window: click-through removed, ConfigModeBehavior active
  - Background: semi-transparent gray (alpha=204), red border
  - Mouse dragging enabled
  - Keyboard: Enter=save+exit, Escape=cancel+exit

Alt+F6 pressed (or Enter key)
  ↓ save position

[OverlayMode] (restored with new position saved to AppSettings)

Escape key in PositioningMode
  ↓ restore original position (no save)

[OverlayMode] (restored with original position, no settings change)
```
