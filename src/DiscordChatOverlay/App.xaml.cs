using System.Windows;
using Application = System.Windows.Application;
using OpenDash.DiscordChatOverlay.Models;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.DiscordChatOverlay;

public partial class App : Application
{
    private ThemeService? _themeService;
    private AppSettings? _settings;

    public App()
    {
        LogService.Info("App constructor called.");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LogService.Info("DiscordChatOverlay startup sequence beginning.");

        _settings = AppSettings.Load();

        bool isDark = _settings.ThemePreference == OpenDash.OverlayCore.Models.ThemePreference.Dark ||
                      (_settings.ThemePreference == OpenDash.OverlayCore.Models.ThemePreference.System &&
                       new ThemeService(OpenDash.OverlayCore.Models.ThemePreference.System).DetectSystemTheme());

        MaterialDesignBootstrap.EnsureInitialized(isDark);

        _themeService = new ThemeService(_settings.ThemePreference);
        _themeService.ApplyTheme(_themeService.IsDarkMode);
        _themeService.StartWatching();

        LogService.Info("DiscordChatOverlay startup complete.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _themeService?.Dispose();
        LogService.Info("DiscordChatOverlay exiting.");
        base.OnExit(e);
    }
}
