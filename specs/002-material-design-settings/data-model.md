# Data Model: Material Design Settings Window

**Branch**: `wheel-overlay/v0.7.0` | **Spec**: `specs/002-material-design-settings/`

## Entities

### MaterialSettingsWindow (modified)

The concrete WPF window in `src/OverlayCore/Settings/`. Structure is unchanged from `001`; only XAML styling and control choices are replaced.

| Aspect | Before (001) | After (this feature) |
|---|---|---|
| Navigation panel | `ListBox` with `NavListBoxItemStyle` from `MaterialStyles.xaml` | `ListBox` with MDIX `Ripple`-enabled `ItemContainerStyle` on a `ColorZone` |
| Action buttons | `Button` with custom brush styles | `Button` with `MaterialDesignRaisedButton` (OK/Apply) and `MaterialDesignFlatButton` (Cancel) |
| Surface colours | `ThemeBackground`, `ThemeControlBackground` dynamic resources | `MaterialDesignPaper` / `MaterialDesignBackground` dynamic resources (MDIX keys) for MD surfaces; `ThemeBackground` keys retained for overlay rendering compatibility |
| Typography | Default WPF system font | `materialDesign:Typography` attached properties (MD type scale) |
| Resource initialisation | None | Calls `MaterialDesignBootstrap.EnsureInitialized()` in constructor |

**Fields (code-behind — unchanged)**:
- `List<ISettingsCategory> _categories` — sorted by `SortOrder` ascending
- `ISettingsCategory? _currentCategory` — currently displayed panel
- `event SettingsAppliedHandler SettingsApplied`

**Methods (unchanged)**:
- `RegisterCategory(ISettingsCategory)` — adds and re-sorts
- `SaveAll()` — saves current category
- `void OK_Click / Apply_Click / Cancel_Click`

---

### MaterialDesignBootstrap (new — OverlayCore)

Static helper in `src/OverlayCore/Settings/MaterialDesignBootstrap.cs`.

**Responsibility**: Programmatically merge MDIX ResourceDictionaries into `Application.Current.Resources` exactly once, ensuring MD controls render correctly when the settings window is opened for the first time.

**Fields**:
- `static bool _initialized = false`
- `static readonly object _lock = new()`

**Methods**:
- `static void EnsureInitialized()` — idempotent; merges MD theme and defaults dictionaries if not already present. Called by `MaterialSettingsWindow` constructor.

**Merged dictionaries** (in order):
1. `pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml` (base; overwritten at runtime by `PaletteHelper.SetTheme`)
2. `pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml`

**Error handling**: If either merge fails (assembly not found, pack URI invalid), `LogService.Error()` is called and the settings window opens in a degraded-but-functional state using WheelOverlay's existing non-MD styles (constitution Principle V).

---

### ThemeService (modified — OverlayCore)

`src/OverlayCore/Services/ThemeService.cs` — extended to sync MD palette on every theme apply.

**New behaviour in `ApplyTheme(bool isDark)`**:
```
1. (existing) Swap DarkTheme.xaml / LightTheme.xaml in Application.Current.Resources
2. (new) Call PaletteHelper.GetTheme(), SetBaseTheme(isDark ? Dark : Light), SetTheme()
3. (new) If PaletteHelper call fails → LogService.Error(); continue (degraded: WheelOverlay theme applied, MD palette remains previous state)
```

**Fields added**: none (PaletteHelper is instantiated per-call, stateless)

---

### ISettingsCategory (unchanged)

`src/OverlayCore/Settings/ISettingsCategory.cs` — interface contract from `001-opendash-monorepo-rebrand`. No changes.

```csharp
public interface ISettingsCategory
{
    string CategoryName { get; }
    int SortOrder { get; }
    FrameworkElement CreateContent();
    void SaveValues();
    void LoadValues();
}
```

---

### Category Panels (modified — WheelOverlay)

All four panels in `src/WheelOverlay/Settings/` have the same structural change: replace default WPF control construction with MDIX-styled equivalents. No changes to fields, properties, or save/load logic.

#### DisplaySettingsCategory (SortOrder=1)

| Control | Change |
|---|---|
| Device combobox | Add `materialDesign:HintAssist.Hint = "Wheel Device"` |
| Layout picker radio buttons | Apply `Style="{StaticResource MaterialDesignRadioButton}"` |
| Position count combobox | Add `materialDesign:HintAssist.Hint = "Position Count"` |
| Section labels | Add `materialDesign:Typography="Subtitle1"` |
| All buttons (New, Rename, Delete profile) | `Style="{StaticResource MaterialDesignOutlinedButton}"` |

#### AppearanceSettingsCategory (SortOrder=2)

| Control | Change |
|---|---|
| Theme preference combobox | Add `materialDesign:HintAssist.Hint = "Theme"` |
| Colour TextBoxes | Add `materialDesign:HintAssist.Hint`, `materialDesign:HintAssist.IsFloating="True"` |
| Font family combobox | Add `materialDesign:HintAssist.Hint = "Font Family"` |
| Font size slider | Apply MD2 slider style |
| Item spacing slider | Apply MD2 slider style |

#### AdvancedSettingsCategory (SortOrder=3)

| Control | Change |
|---|---|
| Target exe path TextBox | Add `materialDesign:HintAssist.Hint = "Target Executable Path"`, `IsFloating="True"` |
| Opacity slider | Apply MD2 slider style |
| Buttons (Browse, Open Folder, Reset) | `Style="{StaticResource MaterialDesignOutlinedButton}"` |

#### AboutSettingsCategory (SortOrder=999)

| Control | Change |
|---|---|
| App name TextBlock | `materialDesign:Typography="H6"` |
| Version TextBlock | `materialDesign:Typography="Body1"` |
| Description TextBlock | `materialDesign:Typography="Body2"` |
| GitHub link | `Button` with `Style="{StaticResource MaterialDesignFlatButton}"` and hyperlink navigation |

---

### MaterialStyles.xaml (modified — OverlayCore)

`src/OverlayCore/Settings/Styles/MaterialStyles.xaml` — replace custom hand-rolled styles with MD2-based styles.

**Styles kept** (adapted):
- `MaterialSettingsWindowStyle` — window chrome; updated to use MD surface colours

**Styles replaced**:
- `NavListBoxStyle` → `ListBox` + `ColorZone` with `materialDesign:ColorZoneAssist.Mode="SecondaryMid"` applied in XAML
- `NavListBoxItemStyle` → `ListBoxItem` with `materialDesign:Ripple.IsEnabled="True"` and `materialDesign:ListBoxItemAssist` selected colour overrides
- `ContentBorderStyle` → `Border` with `Background="{DynamicResource MaterialDesignPaper}"`

---

## Validation Rules

| Rule | Source |
|---|---|
| `ISettingsCategory.CategoryName` must be non-null and non-empty | ISettingsCategory contract (unchanged) |
| `SortOrder` 999 reserved for About | ISettingsCategory contract (unchanged) |
| Settings JSON must round-trip without data loss | FR-017 |
| `PaletteHelper` must not be called before `MaterialDesignBootstrap.EnsureInitialized()` | Implementation constraint (MDA resources must be merged before PaletteHelper writes to them) |

---

## State Transitions

```
App startup
  └─ MaterialDesignBootstrap.EnsureInitialized()  [OverlayCore, called by MaterialSettingsWindow ctor]
  └─ ThemeService.ApplyTheme(isDark)               [syncs MD palette]

Settings window open
  └─ MaterialSettingsWindow shown
  └─ First category selected → LoadValues()

Category navigation
  └─ Departing category → SaveValues()
  └─ Arriving category → LoadValues()

OK / Apply clicked
  └─ Current category → SaveValues()
  └─ SettingsApplied event raised
  └─ ThemeService.Preference updated (if Appearance category changed theme)
  └─ ThemeService.ApplyTheme() → PaletteHelper.SetTheme() [updates MD palette live]

Cancel clicked
  └─ Current category → LoadValues() (discards unsaved edits)
  └─ Window closed

Theme change (system or manual)
  └─ ThemeService.ApplyTheme(isDark)
  └─ DarkTheme.xaml / LightTheme.xaml swapped
  └─ PaletteHelper.SetTheme() updates MD BaseTheme
  └─ All MD-styled controls re-render via data binding to dynamic resources
```

---

## Property-Based Test Properties

These properties MUST be implemented as FsCheck property tests in `tests/WheelOverlay.Tests/`. Each test MUST carry the required comment and `#if FAST_TESTS` directive per constitution Principle II.

### Property 1 — Navigation Sort Order

```
// Feature: Material-Design-Settings, Property 1: Navigation categories always render in ascending SortOrder
```

**Statement**: For any list of `ISettingsCategory` stubs with arbitrary distinct `SortOrder` values, after registering all of them with `MaterialSettingsWindow`, the internal `_categories` list is sorted in ascending `SortOrder` order regardless of registration order.

**Generator**: `Arb.Generate<List<int>>()` for SortOrder values (distinct, non-negative); construct minimal stub implementations.

**Invariant**: `window._categories.Select(c => c.SortOrder)` is non-decreasing.

---

### Property 2 — Cancel Discards Unsaved Changes

```
// Feature: Material-Design-Settings, Property 2: Cancel restores original settings values
```

**Statement**: For any settings category, after `LoadValues()` captures state S, modifying the in-memory backing model to state S', then calling `LoadValues()` again restores the display to S (not S').

**Generator**: Random `AppSettings`-derived objects with varied field values for each category.

**Invariant**: `category.LoadValues(); mutate(model); category.LoadValues(); observedValue == originalValue`.

---

### Property 3 — Theme Palette Synchronisation (regression guard)

```
// Feature: Material-Design-Settings, Property 3: ThemeService.IsDarkMode reflects the last ApplyTheme argument
```

**Statement**: For any sequence of boolean `isDark` arguments to `ThemeService.ApplyTheme()`, after each call `ThemeService.IsDarkMode == isDark`.

**Generator**: `Arb.Generate<bool list>()` — apply each value in sequence.

**Invariant**: After each application, `IsDarkMode` matches the argument.

---

### Property 4 — Settings Round-Trip (regression guard, originally from 001)

```
// Feature: Material-Design-Settings, Property 4: AppSettings serialise/deserialise round-trip preserves all values
```

**Statement**: For any `AppSettings` instance with random valid field values, `AppSettings.FromJson(settings.ToJson()) == settings`.

**Generator**: FsCheck `Arb<AppSettings>` (existing from 001 tests — must continue to pass, no logic change).

**Invariant**: Structural equality on all serialisable fields.
