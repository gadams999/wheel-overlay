using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using OpenDash.OverlayCore.Models;
using OpenDash.OverlayCore.Services;

namespace OpenDash.DiscordChatOverlay.Models;

public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DiscordChatOverlay",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public double WindowLeft { get; set; } = 20.0;
    public double WindowTop { get; set; } = 20.0;

    private int _opacity = 90;
    public int Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 10, 100);
    }

    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.SpeakersOnly;

    private double _gracePeriodSeconds = 2.0;
    public double GracePeriodSeconds
    {
        get => _gracePeriodSeconds;
        set => _gracePeriodSeconds = Math.Clamp(value, 0.0, 2.0);
    }

    private int _debounceThresholdMs = 200;
    public int DebounceThresholdMs
    {
        get => _debounceThresholdMs;
        set => _debounceThresholdMs = Math.Clamp(value, 0, 1000);
    }

    private int _fontSize = 14;
    public int FontSize
    {
        get => _fontSize;
        set => _fontSize = Math.Clamp(value, 8, 32);
    }

    public bool ShowOnStartup { get; set; } = true;

    /// <summary>
    /// Loads settings from %APPDATA%\DiscordChatOverlay\settings.json.
    /// Returns defaults and logs an error on missing or corrupt file.
    /// Clamps window position to screen bounds after load.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    (settings.WindowLeft, settings.WindowTop) =
                        ScreenBoundsHelper.ClampPosition(settings.WindowLeft, settings.WindowTop);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error("AppSettings.Load: Failed to read settings.json; using defaults.", ex);
        }

        var defaults = new AppSettings();
        (defaults.WindowLeft, defaults.WindowTop) =
            ScreenBoundsHelper.ClampPosition(defaults.WindowLeft, defaults.WindowTop);
        return defaults;
    }

    /// <summary>
    /// Saves settings atomically via temp-file rename.
    /// </summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);

            var tmp = SettingsPath + ".tmp";
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(tmp, json);
            File.Move(tmp, SettingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            LogService.Error("AppSettings.Save: Failed to write settings.json.", ex);
        }
    }
}

/// <summary>
/// Clamps a window position to the nearest monitor's working area.
/// </summary>
public static class ScreenBoundsHelper
{
    public static (double Left, double Top) ClampPosition(double left, double top)
    {
        var pt = new System.Drawing.Point((int)left, (int)top);
        bool onScreen = Screen.AllScreens.Any(s => s.WorkingArea.Contains(pt));
        if (onScreen) return (left, top);

        LogService.Info(
            $"AppSettings: Window position ({left},{top}) is off-screen; resetting to primary screen origin.");
        return (20.0, 20.0);
    }
}
