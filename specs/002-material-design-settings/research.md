# Research: Material Design Settings Window

**Branch**: `wheel-overlay/v0.7.0` | **Spec**: `specs/002-material-design-settings/`

## Decision 1 — MaterialDesignInXamlToolkit Version

**Decision**: Pin `MaterialDesignThemes` (NuGet package ID) v5.3.1 — the assembly is `MaterialDesignThemes.Wpf.dll` (exact version verified at implementation time on NuGet.org)

**Rationale**: The v5.x line is the first that targets `net6.0-windows` and higher cleanly. v4.x had limited modern .NET support. v5.3.1 is the latest stable release as of implementation (2026-03-21), in the v5.x family required for .NET 10 / WPF.

**Alternatives considered**:
- v4.x — dropped; incomplete .NET 10 support and older API surface
- Floating `5.*` range — dropped; spec explicitly requires a pinned version (assumption confirmed in clarifications)
- MD3 opt-in (`MaterialDesignThemes.Wpf` MD3 controls) — dropped; spec requires MD2 style set only

**Action at implementation**: Before writing code, run `dotnet package search MaterialDesignThemes.Wpf` or check NuGet.org to confirm the exact latest stable patch and pin it in `OverlayCore.csproj`.

---

## Decision 2 — Library Integration (OverlayCore as WPF Class Library)

**Decision**: `MaterialDesignThemes.Wpf` is referenced in `OverlayCore.csproj` only. Resource dictionaries are merged **programmatically** inside `MaterialSettingsWindow`'s initialisation path via a static `MaterialDesignBootstrap.EnsureInitialized()` helper, injecting into `Application.Current.Resources` at first call (idempotent).

**Rationale**:
- WPF class libraries do not have `App.xaml`; they cannot self-register ResourceDictionaries declaratively.
- A programmatic merge from within OverlayCore's own code satisfies FR-002 (no direct MDIX reference in WheelOverlay) and FR-003 (resources available to all category panels without per-category setup).
- The `MaterialDesignThemes.Wpf` assembly is transitively available in WheelOverlay's output directory because OverlayCore is a ProjectReference — the DLLs ship with the app.

**Required resource dictionaries** (merged by `MaterialDesignBootstrap.EnsureInitialized()`):
```xml
<!-- 1. Base light OR dark theme (swapped at runtime by PaletteHelper) -->
pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml

<!-- 2. Defaults / typography / elevation -->
pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml
```
The colour palette (primary + accent) is set programmatically via `PaletteHelper` at theme-apply time rather than via static XAML, allowing runtime palette switching without dictionary re-merging.

**Alternatives considered**:
- Declarative merge in `WheelOverlay/App.xaml` — dropped; violates FR-002/FR-003 (consumers would need a direct dependency)
- Generic.xaml WPF assembly resource loading — dropped; WPF auto-loads Generic.xaml only for custom control templates, not application-level brushes

---

## Decision 3 — Navigation Rail (MD2)

**Decision**: Implement the navigation rail as a vertically-oriented `ListBox` with a custom `ItemContainerStyle` that enables the MDIX `Ripple` attached behaviour and applies accent-colour selected-state styling.

**Rationale**: MaterialDesignInXamlToolkit MD2 does not ship a "Navigation Rail" control by that name — that is an MD3 concept. The MD2 equivalent is a styled `ListBox` on a `ColorZone` (for surface elevation). MDIX provides:
- `materialDesign:Ripple.IsEnabled="True"` — ripple on any `ContentControl` / `ListBoxItem`
- `materialDesign:ColorZoneAssist.Mode` — surface colour theming per zone
- `materialDesign:ListBoxItemAssist` — selected indicator colour

This combination delivers the ripple + selected-state accent that FR-004 requires, using only MD2 controls.

**Alternatives considered**:
- `NavigationRailItem` (MD3 opt-in) — dropped; MD3 opt-in is explicitly out of scope per clarifications
- Custom `Button`-based panel — dropped; ListBox provides keyboard navigation (Up/Down arrows) for free, satisfying FR-016

---

## Decision 4 — Runtime Theme Switching (ThemeService → MD Palette)

**Decision**: `ThemeService.ApplyTheme(bool isDark)` is extended to call `PaletteHelper.SetTheme()` after swapping the WheelOverlay `DarkTheme.xaml`/`LightTheme.xaml` dictionaries.

```csharp
// Added to ThemeService.ApplyTheme(bool isDark)
var paletteHelper = new PaletteHelper();
ITheme theme = paletteHelper.GetTheme();
theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
paletteHelper.SetTheme(theme);
```

**Rationale**:
- `PaletteHelper` is the v5.x API for runtime theme mutation; it writes into the same `Application.Current.Resources` dictionary that the bootstrap merge populated.
- The existing WheelOverlay theme dictionary (`DarkTheme.xaml`/`LightTheme.xaml`) retains its role as the source-of-truth theme signal; MD palette is a follower, not a replacement.
- No new `ThemePreference` enum values or persistence changes — satisfies FR-017 (no data model changes).

**Alternatives considered**:
- Re-merge different base-theme XAML files (e.g., swap `MaterialDesignTheme.Light.xaml` for `.Dark.xaml`) — dropped; this causes visual artifacts from stale brush references during the swap
- Replace `ThemeService` entirely with `PaletteHelper` — dropped; would remove the Windows-system-theme integration and break existing category panels that use `ThemeBackground`, `ThemeForeground`, etc.

---

## Decision 5 — Category Panel Control Mapping

**Decision**: All four category panels replace plain WPF controls with the MDIX equivalents listed below. UI is constructed in C# (no XAML files for categories — existing pattern is maintained).

| Panel | Control | WPF before | MDIX MD2 after |
|---|---|---|---|
| All | Label / TextBlock | `TextBlock` | `TextBlock` with `materialDesign:Typography` attached property (e.g., `Body1`, `Subtitle1`) |
| Display | Drop-down | `ComboBox` (default) | `ComboBox` with `materialDesign:HintAssist.Hint` floating label |
| Display | Radio-like layout picker | `RadioButton` (default) | `RadioButton` with MD2 style |
| Appearance | Drop-down | `ComboBox` (default) | `ComboBox` with MD2 style |
| Appearance | Font size, spacing | `Slider` (default) | `Slider` with MD2 style |
| Appearance | Colour input | `TextBox` (default) | `TextBox` with `materialDesign:HintAssist.Hint` floating label |
| Advanced | Path text field | `TextBox` (default) | `TextBox` with `materialDesign:HintAssist.Hint` floating label + `IsFloating="True"` |
| Advanced | Opacity slider | `Slider` (default) | `Slider` with MD2 style |
| About | Version text | `TextBlock` | `TextBlock` with `materialDesign:Typography` Body1 |
| About | GitHub link | `Hyperlink` in `TextBlock` | `Button` with `materialDesign:ButtonAssist` link style |
| All | Action buttons | `Button` (default) | `Button` with `Style="{StaticResource MaterialDesignRaisedButton}"` (OK/Apply) and `Style="{StaticResource MaterialDesignFlatButton}"` (Cancel) |

**Alternatives considered**:
- Keep constructing controls in pure XAML files — considered, but rejected to preserve the existing code-only category construction pattern
- Use `MaterialDesignTextBox` wrapper style — not a class in v5.x; the correct approach is attaching `materialDesign:HintAssist` to the standard WPF `TextBox`

---

## Decision 6 — Resource Conflict Avoidance

**Decision**: `MaterialDesignBootstrap.EnsureInitialized()` checks `Application.Current.Resources.MergedDictionaries` before adding MD dictionaries to prevent double-merging. The existing `ThemeBackground`, `ThemeForeground`, etc. keys from `DarkTheme.xaml`/`LightTheme.xaml` are **preserved unchanged** because MDIX uses different key names (`MaterialDesignBackground`, `MaterialDesignBody`, etc.).

**Rationale**: The WheelOverlay overlay rendering pipeline uses `ThemeBackground` / `ThemeAccent` keys. MDIX uses its own `MaterialDesign*` key namespace. There is no key collision by design — both dictionaries coexist.

**Edge case from spec**: "Future `ISettingsCategory` implementations that don't use MD controls" — the window chrome renders correctly regardless because the navigation rail and action buttons are owned entirely by `MaterialSettingsWindow`, not by the category panels.
