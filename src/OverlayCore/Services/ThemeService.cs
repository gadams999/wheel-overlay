using System;
using Microsoft.Win32;
using OpenDash.OverlayCore.Models;

namespace OpenDash.OverlayCore.Services;

/// <summary>
/// Detects the Windows app theme (light/dark), applies matching WPF resource
/// dictionaries, and watches for runtime theme changes.
/// </summary>
public class ThemeService : IDisposable
{
    private bool _isDarkMode;
    private ThemePreference _preference;
    private bool _disposed;
    private System.Windows.Threading.DispatcherTimer? _pollTimer;

    /// <summary>
    /// Fired when the effective theme changes. Argument is true for dark mode.
    /// </summary>
    public event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// Whether the currently applied theme is dark mode.
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        private set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                ThemeChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// The user's theme preference. Setting this re-evaluates and applies the
    /// effective theme immediately.
    /// </summary>
    public ThemePreference Preference
    {
        get => _preference;
        set
        {
            _preference = value;
            ApplyEffectiveTheme();
        }
    }

    public ThemeService(ThemePreference initialPreference)
    {
        _preference = initialPreference;
        _isDarkMode = ResolveEffectiveTheme();
    }

    /// <summary>
    /// Reads the Windows registry to detect the current system app theme.
    /// Returns true when the system is in dark mode.
    /// </summary>
    public bool DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                // 0 = dark mode, 1 = light mode
                return value == 0;
            }
        }
        catch
        {
            // Registry unreadable — fall through to default
        }

        return false; // default to light mode (not dark)
    }

    /// <summary>
    /// Swaps the theme resource dictionary in Application.Current.Resources.
    /// Uses pack URIs to reference OverlayCore assembly resources.
    /// </summary>
    public void ApplyTheme(bool dark)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        var targetSource = dark
            ? new Uri("pack://application:,,,/OverlayCore;component/Resources/DarkTheme.xaml")
            : new Uri("pack://application:,,,/OverlayCore;component/Resources/LightTheme.xaml");

        var mergedDicts = app.Resources.MergedDictionaries;

        // Find the existing theme dictionary (Light or Dark) and replace it
        for (int i = 0; i < mergedDicts.Count; i++)
        {
            var source = mergedDicts[i].Source;
            if (source != null &&
                (source.OriginalString.Contains("LightTheme.xaml") ||
                 source.OriginalString.Contains("DarkTheme.xaml")))
            {
                mergedDicts[i] = new System.Windows.ResourceDictionary { Source = targetSource };
                return;
            }
        }

        // No existing theme dictionary found — add one
        mergedDicts.Insert(0, new System.Windows.ResourceDictionary { Source = targetSource });
    }

    /// <summary>
    /// Starts a polling timer to detect system theme changes at runtime.
    /// Polls every 2 seconds and re-applies the theme when preference is System.
    /// </summary>
    public void StartWatching()
    {
        if (_pollTimer != null) return;

        _pollTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _pollTimer.Tick += OnPollTimerTick;
        _pollTimer.Start();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Tick -= OnPollTimerTick;
                    _pollTimer = null;
                }
            }
            _disposed = true;
        }
    }

    private void OnPollTimerTick(object? sender, EventArgs e)
    {
        if (_preference != ThemePreference.System) return;

        bool dark = DetectSystemTheme();
        if (dark != _isDarkMode)
        {
            ApplyTheme(dark);
            IsDarkMode = dark;
        }
    }

    /// <summary>
    /// Resolves the effective theme based on the current preference and system state.
    /// Light/Dark preference overrides system detection; System preference follows
    /// the detected Windows theme.
    /// </summary>
    private bool ResolveEffectiveTheme()
    {
        return _preference switch
        {
            ThemePreference.Light => false,
            ThemePreference.Dark => true,
            ThemePreference.System => DetectSystemTheme(),
            _ => DetectSystemTheme()
        };
    }

    private void ApplyEffectiveTheme()
    {
        bool dark = ResolveEffectiveTheme();
        ApplyTheme(dark);
        IsDarkMode = dark;
    }
}
