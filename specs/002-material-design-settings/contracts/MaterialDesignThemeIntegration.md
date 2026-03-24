# Contract: MaterialDesign Theme Integration

**Branch**: `wheel-overlay/v0.7.0` | **Spec**: `specs/002-material-design-settings/`

## Purpose

Defines how OverlayCore initialises MaterialDesignInXamlToolkit resources and how `ThemeService` keeps the MD palette in sync with WheelOverlay's active theme. This is the only new integration contract introduced by this feature.

## ISettingsCategory

Unchanged from `001-opendash-monorepo-rebrand`. See `specs/001-opendash-monorepo-rebrand/contracts/ISettingsCategory.md`.

---

## MaterialDesignBootstrap.EnsureInitialized()

**Location**: `src/OverlayCore/Settings/MaterialDesignBootstrap.cs`
**Caller**: `MaterialSettingsWindow` constructor (called exactly once before any MD control is created)
**Thread safety**: guarded by `static readonly object _lock`

### Contract

```
Pre-conditions:
  - Application.Current must be non-null (WPF application is running)
  - MaterialDesignThemes.Wpf assembly must be present in the output directory

Post-conditions (success):
  - Application.Current.Resources.MergedDictionaries contains:
      1. MaterialDesignTheme.Light.xaml (base; overwritten immediately by ThemeService.ApplyTheme)
      2. MaterialDesignTheme.Defaults.xaml
  - MD control styles (MaterialDesignRaisedButton, MaterialDesignSlider, etc.) are resolvable
    from Application.Current.FindResource()
  - Method is idempotent: calling again is a no-op

Post-conditions (failure — assembly not found or pack URI invalid):
  - LogService.Error() is called with diagnostic context
  - No exception is thrown
  - Application continues without MD resources (degraded: default WPF chrome visible)
```

### Resource Keys Made Available

After initialisation, the following dynamic resource keys are resolvable application-wide:

| Key | Type | Purpose |
|---|---|---|
| `MaterialDesignRaisedButton` | `Style` | Primary action button (OK, Apply) |
| `MaterialDesignFlatButton` | `Style` | Dismissal button (Cancel), link buttons |
| `MaterialDesignOutlinedButton` | `Style` | Secondary action button |
| `MaterialDesignRadioButton` | `Style` | Radio button |
| `MaterialDesignSlider` | `Style` | Slider |
| `MaterialDesignPaper` | `SolidColorBrush` | Card / content surface background |
| `MaterialDesignBackground` | `SolidColorBrush` | Window background |
| `MaterialDesignBody` | `SolidColorBrush` | Primary text colour |
| `MaterialDesignToolBarBackground` | `SolidColorBrush` | Navigation rail surface |

---

## ThemeService.ApplyTheme(bool isDark) — Extended Contract

**Location**: `src/OverlayCore/Services/ThemeService.cs`

### Extended post-conditions (additions to the 001 contract)

```
In addition to the 001 behaviour (swap DarkTheme.xaml / LightTheme.xaml):

Success path:
  - PaletteHelper.GetTheme() returns the current MD ITheme
  - ITheme.SetBaseTheme(BaseTheme.Dark | BaseTheme.Light) updates the palette
  - PaletteHelper.SetTheme(theme) writes updated palette to Application.Current.Resources
  - All MD controls re-render via their dynamic resource bindings
  - ThemeService.IsDarkMode == isDark

Failure path (PaletteHelper throws):
  - LogService.Error() is called with the exception context
  - WheelOverlay theme dictionaries are still swapped (partial success)
  - IsDarkMode is still updated
  - MD controls may retain the previous palette (visual inconsistency, non-crashing)
```

### Sequencing constraint

`MaterialDesignBootstrap.EnsureInitialized()` MUST be called before `ThemeService.ApplyTheme()` is invoked. The `MaterialSettingsWindow` constructor guarantees this by calling `EnsureInitialized()` first; startup code in `App.xaml.cs` calls `ApplyTheme()` after creating the settings window.

---

## Non-Contracts (explicitly excluded)

The following are NOT part of this feature's contracts and are governed by the 001 spec:

- `ISettingsCategory` interface — unchanged
- `AppSettings` JSON schema — unchanged (FR-017)
- `SettingsApplied` event signature — unchanged
- `RegisterCategory()` sort behaviour — unchanged
