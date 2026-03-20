# Contract: SharedFontResources

**Location**: `src/OverlayCore/Resources/Fonts/`
**Files**:
- `SharedFontResources.xaml` — XAML resource dictionary with font keys
- `FontUtilities.cs` — C# helpers for runtime font resolution

**Namespace**: `OpenDash.OverlayCore.Resources.Fonts`
**Consumers**: All overlay applications merging the resource dictionary in `App.xaml`

---

## XAML Resource Dictionary Keys

### FontFamily Keys

| Key | Default Value | Usage |
|-----|---------------|-------|
| `OverlayFontFamily` | `Segoe UI` | Default UI font for overlay labels |
| `MonospaceFontFamily` | `Consolas` | Monospace font for numeric displays |

### FontSize Keys (Double)

| Key | Default Value | Usage |
|-----|---------------|-------|
| `OverlayFontSizeSmall` | `12.0` | Small supplementary text |
| `OverlayFontSizeMedium` | `16.0` | Standard overlay label text |
| `OverlayFontSizeLarge` | `20.0` | Prominent position values |
| `OverlayFontSizeXLarge` | `28.0` | Large dial/single-text display |

### FontWeight Keys

| Key | Default Value | Usage |
|-----|---------------|-------|
| `OverlayFontWeightNormal` | `Normal` | Default weight |
| `OverlayFontWeightBold` | `Bold` | Emphasis text |

---

## Merging in App.xaml

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/OverlayCore;component/Resources/Fonts/SharedFontResources.xaml"/>
            <!-- App-specific resources follow -->
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## FontUtilities API

```csharp
namespace OpenDash.OverlayCore.Resources.Fonts;

public static class FontUtilities
{
    /// <summary>
    /// Returns a WPF FontFamily for the given family name.
    /// Falls back to Segoe UI for null, empty, or unrecognized names.
    /// Never returns null.
    /// </summary>
    public static FontFamily GetFontFamily(string? familyName);

    /// <summary>
    /// Converts a font weight name ("Normal", "Bold", "Light", "Medium",
    /// "SemiBold", "Black", etc.) to a WPF FontWeight.
    /// Falls back to FontWeights.Normal for null, empty, or unrecognized names.
    /// </summary>
    public static FontWeight ToFontWeight(string? weightName);
}
```

---

## Using Shared Keys in XAML

```xml
<!-- Reference shared font family resource -->
<TextBlock FontFamily="{StaticResource OverlayFontFamily}"
           FontSize="{StaticResource OverlayFontSizeLarge}"
           FontWeight="{StaticResource OverlayFontWeightBold}" />
```

---

## User-Configurable Fonts (WheelOverlay)

WheelOverlay allows users to configure `FontFamily` and `FontSize` via `AppSettings`. These are resolved at runtime using `FontUtilities.GetFontFamily(settings.FontFamily)` rather than using static resource keys, since they are user-controlled values. The static keys provide defaults; user overrides take precedence.

---

## Property Test Coverage

**Property 8**: FontUtilities helpers return valid results for all valid inputs
- `GetFontFamily(anyString)` → non-null `FontFamily` (Segoe UI fallback for unrecognized)
- `ToFontWeight(validWeightName)` → correct `FontWeight` value
- Test file: `tests/OverlayCore.Tests/FontUtilitiesPropertyTests.cs`
