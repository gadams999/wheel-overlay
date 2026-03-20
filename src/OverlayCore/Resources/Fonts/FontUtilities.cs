using System.Windows;

namespace OpenDash.OverlayCore.Resources.Fonts;

/// <summary>
/// Provides helpers for resolving WPF font types from string values at runtime.
/// Used for user-configurable font settings where static resource keys cannot be used.
/// </summary>
public static class FontUtilities
{
    private static readonly System.Windows.Media.FontFamily _fallbackFontFamily = new("Segoe UI");

    /// <summary>
    /// Returns a WPF <see cref="System.Windows.Media.FontFamily"/> for the given family name.
    /// Falls back to Segoe UI for null, empty, or unrecognized names.
    /// Never returns null.
    /// </summary>
    public static System.Windows.Media.FontFamily GetFontFamily(string? familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
            return _fallbackFontFamily;

        try
        {
            return new System.Windows.Media.FontFamily(familyName);
        }
        catch
        {
            return _fallbackFontFamily;
        }
    }

    /// <summary>
    /// Converts a font weight name ("Normal", "Bold", "Light", "Medium",
    /// "SemiBold", "Black", etc.) to a WPF <see cref="FontWeight"/>.
    /// Falls back to <see cref="FontWeights.Normal"/> for null, empty, or unrecognized names.
    /// </summary>
    public static FontWeight ToFontWeight(string? weightName)
    {
        if (string.IsNullOrWhiteSpace(weightName))
            return FontWeights.Normal;

        return weightName.Trim() switch
        {
            "Thin" => FontWeights.Thin,
            "ExtraLight" or "UltraLight" => FontWeights.ExtraLight,
            "Light" => FontWeights.Light,
            "Normal" or "Regular" => FontWeights.Normal,
            "Medium" => FontWeights.Medium,
            "SemiBold" or "DemiBold" => FontWeights.SemiBold,
            "Bold" => FontWeights.Bold,
            "ExtraBold" or "UltraBold" => FontWeights.ExtraBold,
            "Black" or "Heavy" => FontWeights.Black,
            "ExtraBlack" or "UltraBlack" => FontWeights.ExtraBlack,
            _ => FontWeights.Normal
        };
    }
}
