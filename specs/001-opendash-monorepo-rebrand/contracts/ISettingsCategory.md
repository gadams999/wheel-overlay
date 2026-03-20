# Contract: ISettingsCategory

**Location**: `src/OverlayCore/Settings/ISettingsCategory.cs`
**Namespace**: `OpenDash.OverlayCore.Settings`
**Type**: Public interface
**Consumers**: Overlay applications registering settings panels with `MaterialSettingsWindow`

---

## Interface Definition

```csharp
namespace OpenDash.OverlayCore.Settings;

public interface ISettingsCategory
{
    /// <summary>Display name shown in the navigation list. Must be non-null and non-empty.</summary>
    string CategoryName { get; }

    /// <summary>
    /// Sort order for the navigation list. Lower values appear first.
    /// Value 999 is reserved for the built-in About category.
    /// </summary>
    int SortOrder { get; }

    /// <summary>Creates the WPF content panel for this category. Must return non-null.</summary>
    FrameworkElement CreateContent();

    /// <summary>Persists current UI control values back to the settings model.</summary>
    void SaveValues();

    /// <summary>Loads current settings model values into UI controls.</summary>
    void LoadValues();
}
```

---

## Registration Protocol

Overlay apps register categories before showing the settings window:

```csharp
// In App.xaml.cs or startup code:
var settingsWindow = new MaterialSettingsWindow();
settingsWindow.RegisterCategory(new DisplaySettingsCategory(viewModel));
settingsWindow.RegisterCategory(new AppearanceSettingsCategory(viewModel));
settingsWindow.RegisterCategory(new AdvancedSettingsCategory(viewModel));
// AboutSettingsCategory is auto-registered by MaterialSettingsWindow
settingsWindow.Show();
```

---

## Ordering Invariant

`MaterialSettingsWindow` displays categories sorted ascending by `SortOrder`. `AboutSettingsCategory` (SortOrder=999) always appears last regardless of registration order.

**Contract guarantee** (tested by Property 7): For any set of N registered categories, the navigation list contains exactly N+1 entries (N app categories + About), displayed in ascending `SortOrder`.

---

## WheelOverlay Implementation Summary

| Category | Class | SortOrder | Migrated From |
|----------|-------|-----------|---------------|
| Display | `DisplaySettingsCategory` | 1 | SettingsWindow.xaml.cs layout+device section |
| Appearance | `AppearanceSettingsCategory` | 2 | SettingsWindow.xaml.cs colors+fonts section |
| Advanced | `AdvancedSettingsCategory` | 3 | SettingsWindow.xaml.cs target exe section |
| About | `AboutSettingsCategory` (OverlayCore) | 999 | AboutWindow.xaml.cs |
