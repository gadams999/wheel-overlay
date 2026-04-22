using System.Collections.Generic;
using OpenDash.SpeakerSight.Models;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.SpeakerSight.ViewModels;

/// <summary>
/// Coordinator that holds all <see cref="ISettingsCategory"/> instances and provides
/// aggregate save / load / reset-to-defaults operations for the settings window.
/// </summary>
public class SettingsViewModel
{
    private readonly AppSettings               _settings;
    private readonly IReadOnlyList<ISettingsCategory> _categories;

    public IReadOnlyList<ISettingsCategory> Categories => _categories;

    public SettingsViewModel(AppSettings settings, IReadOnlyList<ISettingsCategory> categories)
    {
        _settings   = settings;
        _categories = categories;
    }

    /// <summary>Persists current UI values for every registered category.</summary>
    public void SaveAll()
    {
        foreach (var cat in _categories)
            cat.SaveValues();
    }

    /// <summary>Refreshes every registered category's controls from the current settings model.</summary>
    public void LoadAll()
    {
        foreach (var cat in _categories)
            cat.LoadValues();
    }

    /// <summary>
    /// Resets all settings to their defaults, persists to disk, then refreshes all category controls.
    /// </summary>
    public void ResetToDefaults()
    {
        var defaults = new AppSettings();

        // Copy defaults into the shared settings instance so live references stay valid
        _settings.WindowLeft           = defaults.WindowLeft;
        _settings.WindowTop            = defaults.WindowTop;
        _settings.Opacity              = defaults.Opacity;
        _settings.ThemePreference      = defaults.ThemePreference;
        _settings.DisplayMode          = defaults.DisplayMode;
        _settings.GracePeriodSeconds   = defaults.GracePeriodSeconds;
        _settings.DebounceThresholdMs  = defaults.DebounceThresholdMs;
        _settings.FontSize             = defaults.FontSize;

        _settings.Save();
        LoadAll();
    }
}
