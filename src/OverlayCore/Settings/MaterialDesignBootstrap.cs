using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
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

    /// <summary>True once EnsureInitialized has successfully merged MDIX resources.</summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Ensures MaterialDesignThemes resources are merged into the application's
    /// resource dictionary. Safe to call multiple times; only the first call
    /// performs work. On failure, logs via LogService.Error and returns without
    /// throwing — the settings window opens in a degraded-but-functional state.
    /// </summary>
    /// <param name="isDark">True to load the dark palette; false for light.</param>
    public static void EnsureInitialized(bool isDark = false)
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                var app = System.Windows.Application.Current;
                if (app == null) return;

                // BundledTheme is the canonical MDIX v5 initializer — it loads the base
                // theme AND defines the primary/secondary palette brushes that control
                // templates (buttons, ComboBoxes) reference. Using raw Light/Dark XAML
                // files without it leaves those brushes undefined, making controls invisible.
                var bundledTheme = new BundledTheme
                {
                    BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light,
                    PrimaryColor = PrimaryColor.BlueGrey,
                    SecondaryColor = SecondaryColor.LightBlue
                };
                app.Resources.MergedDictionaries.Add(bundledTheme);

                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml")
                });

                _initialized = true;
            }
            catch (Exception ex)
            {
                LogService.Error("MaterialDesignBootstrap: Failed to initialize MDIX resources", ex);
            }
        }
    }

    /// <summary>
    /// Swaps the MD base theme (Light ↔ Dark) at runtime via PaletteHelper.
    /// BundledTheme registers the necessary ThemeManager resources that PaletteHelper
    /// requires, so this works correctly only after EnsureInitialized has run.
    /// No-op if not yet initialized.
    /// </summary>
    public static void SwapTheme(bool isDark)
    {
        if (!_initialized) return;

        try
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(theme);
        }
        catch (Exception ex)
        {
            LogService.Error("MaterialDesignBootstrap.SwapTheme: Failed to swap MD theme", ex);
        }
    }
}
