# Contract: settings.json Schema

**File**: `%APPDATA%\SpeakerSight\settings.json`
**Owned by**: `AppSettings.cs` (`OpenDash.SpeakerSight.Models`)
**Serializer**: `System.Text.Json` with `JsonStringEnumConverter`; case-insensitive; unknown properties ignored

---

## Schema

```jsonc
{
  // Overlay window position (device-independent pixels)
  "WindowLeft": 20.0,           // double — clamped to screen bounds on load
  "WindowTop": 20.0,            // double — clamped to screen bounds on load

  // Appearance
  "Opacity": 90,                // int — range [10, 100]
  "ThemePreference": "System",  // string enum: "Dark" | "Light" | "System"
  "FontSize": 14,               // int — range [8, 32]

  // Display behaviour
  "DisplayMode": "SpeakersOnly",    // string enum: "SpeakersOnly" | "AllMembers"
  "GracePeriodSeconds": 2.0,        // double — range [0.0, 2.0]
  "DebounceThresholdMs": 200,       // int — range [0, 1000]; 0 = disabled

  // App
  "ShowOnStartup": true             // bool
}
```

## Example (first run defaults)

```json
{
  "WindowLeft": 20.0,
  "WindowTop": 20.0,
  "Opacity": 90,
  "ThemePreference": "System",
  "FontSize": 14,
  "DisplayMode": "SpeakersOnly",
  "GracePeriodSeconds": 2.0,
  "DebounceThresholdMs": 200,
  "ShowOnStartup": true
}
```

## Validation Rules

| Field | Out-of-range behaviour |
|-------|----------------------|
| `Opacity` | Silently clamped to [10, 100]; logged at Info level |
| `GracePeriodSeconds` | Silently clamped to [0.0, 2.0]; logged at Info level |
| `DebounceThresholdMs` | Silently clamped to [0, 1000]; logged at Info level |
| `FontSize` | Silently clamped to [8, 32]; logged at Info level |
| `WindowLeft`/`WindowTop` | If position outside all monitors → reset to (20.0, 20.0) on primary screen; logged at Warning level |
| Enum values | Unknown string → default value; logged at Warning level |
| Missing file | All defaults used; new file written on first save |
| Malformed JSON | All defaults used; `LogService.Error()` called; corrupt file overwritten on next save |

## Write Contract

`AppSettings.Save()` writes atomically:
1. Serialize to JSON string
2. Write to `{path}.tmp`
3. Rename `{path}.tmp` → `{path}` (atomic on NTFS)

`AppSettings.Load()` returns a new `AppSettings()` (all defaults) on any failure and calls `LogService.Error()`.
