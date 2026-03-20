using System.Windows;

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
