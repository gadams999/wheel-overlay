using OpenDash.OverlayCore.Services;

namespace OpenDash.OverlayCore.Settings;

/// <summary>
/// Idempotent helper that merges MaterialDesignInXamlToolkit ResourceDictionaries
/// into Application.Current.Resources exactly once.
/// </summary>
public static class MaterialDesignBootstrap
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures MaterialDesignThemes resources are merged into the application's
    /// resource dictionary. Safe to call multiple times; only the first call
    /// performs work. On failure, logs via LogService.Error and returns without
    /// throwing — the settings window opens in a degraded-but-functional state.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                var app = System.Windows.Application.Current;
                if (app == null) return;

                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml")
                });

                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml")
                });

                _initialized = true;
            }
            catch (Exception ex)
            {
                LogService.Error("MaterialDesignBootstrap: Failed to initialize MDIX resources", ex);
            }
        }
    }
}
