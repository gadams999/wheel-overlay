using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using OpenDash.SpeakerSight.Models;
using OpenDash.SpeakerSight.Services;
using OpenDash.SpeakerSight.Settings;
using OpenDash.SpeakerSight.ViewModels;
using OpenDash.OverlayCore.Resources.Fonts;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;
using WinForms = System.Windows.Forms;

namespace OpenDash.SpeakerSight;

public partial class App : Application
{
    // Exponential backoff slots (seconds): 0, 2, 4, 8, 16, 32, 64
    private static readonly int[] BackoffSlots = { 0, 2, 4, 8, 16, 32, 64 };

    private ThemeService?           _themeService;
    private AppSettings?            _settings;
    private TokenStorageService?    _tokenStorage;
    private DiscordIpcClient?       _ipcClient;
    private AliasService?           _aliasService;
    private VoiceSessionService?    _voiceService;
    private OverlayViewModel?       _overlayViewModel;
    private MainWindow?             _mainWindow;
    private SettingsViewModel?      _settingsViewModel;
    private MaterialSettingsWindow? _settingsWindow;
    private CancellationTokenSource _appCts = new();
    private int                     _retryAttempt;

    private WinForms.NotifyIcon?    _notifyIcon;
    private System.Drawing.Icon?    _iconDefault;
    private System.Drawing.Icon?    _iconAmber;
    private System.Drawing.Icon?    _iconRed;

    public App()
    {
        LogService.Info("App constructor called.");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LogService.Info("SpeakerSight startup sequence beginning.");

        _settings = AppSettings.Load();

        bool isDark = _settings.ThemePreference == OpenDash.OverlayCore.Models.ThemePreference.Dark ||
                      (_settings.ThemePreference == OpenDash.OverlayCore.Models.ThemePreference.System &&
                       new ThemeService(OpenDash.OverlayCore.Models.ThemePreference.System).DetectSystemTheme());

        MaterialDesignBootstrap.EnsureInitialized(isDark);

        _themeService = new ThemeService(_settings.ThemePreference);
        _themeService.ApplyTheme(_themeService.IsDarkMode);
        _themeService.StartWatching();

        _tokenStorage = new TokenStorageService();
        _ipcClient    = new DiscordIpcClient();

        _aliasService = new AliasService();
        _aliasService.Load();

        _voiceService     = new VoiceSessionService(_ipcClient, _aliasService, _settings);
        _overlayViewModel = new OverlayViewModel(_voiceService, _settings);

        _mainWindow = new MainWindow(_overlayViewModel)
        {
            Left = _settings.WindowLeft,
            Top  = _settings.WindowTop
        };

        // Apply saved opacity (AppSettings.Opacity is 10–100; WPF Opacity is 0.0–1.0)
        _mainWindow.Opacity = _settings.Opacity / 100.0;

        // Apply saved font family and size
        _mainWindow.FontFamily = FontUtilities.GetFontFamily(_settings.FontFamily);
        _mainWindow.FontSize   = _settings.FontSize;

        // Apply saved font weight
        _mainWindow.FontWeight = _settings.FontBold ? FontWeights.Bold : FontWeights.Normal;

        // Apply saved font color
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings.FontColor);
            _mainWindow.Foreground = new System.Windows.Media.SolidColorBrush(color);
        }
        catch
        {
            _mainWindow.Foreground = System.Windows.Media.Brushes.White;
        }

        // Apply saved background color
        try
        {
            var bgColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings.BackgroundColor);
            _mainWindow.SpeakerBackground = new System.Windows.Media.SolidColorBrush(bgColor);
        }
        catch
        {
            _mainWindow.SpeakerBackground = System.Windows.Media.Brushes.Transparent;
        }

        // Apply saved speaker spacing
        _mainWindow.SpeakerSpacing = _settings.SpeakerSpacing;

        // Build settings view model with currently available categories
        _settingsViewModel = new SettingsViewModel(_settings, new List<ISettingsCategory>
        {
            new ConnectionSettingsCategory(_ipcClient, _tokenStorage),
            new DisplaySettingsCategory(_settings),
            new AppearanceSettingsCategory(_settings, _mainWindow, _themeService),
            new AliasSettingsCategory(_aliasService, _voiceService),
            new AboutSettingsCategory()
        });

        _mainWindow.Show();

        _overlayViewModel.ReAuthorizationRequested += OnReAuthorizationRequested;

        _ipcClient.ConnectionDropped += OnConnectionDropped;
        _ipcClient.AuthRevoked       += OnAuthRevoked;

        SetupTrayIcon();

        _ = ConnectAsync(_appCts.Token);

        LogService.Info("SpeakerSight startup complete.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appCts.Cancel();
        _settingsWindow?.Close();
        _themeService?.Dispose();
        _voiceService?.Dispose();
        _ = _ipcClient?.DisposeAsync();

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        _iconAmber?.Dispose();
        _iconRed?.Dispose();
        _iconDefault?.Dispose();

        LogService.Info("SpeakerSight exiting.");
        base.OnExit(e);
    }

    /// <summary>
    /// Opens the settings window (or brings it to the foreground if already open).
    /// Suspends click-through on the overlay while settings are open so the user
    /// can interact with the position drag fields.
    /// </summary>
    public void ShowSettings()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        bool isDark = _themeService?.IsDarkMode ?? false;
        _settingsWindow = new MaterialSettingsWindow(isDark);

        if (_settingsViewModel != null)
        {
            foreach (var cat in _settingsViewModel.Categories)
                _settingsWindow.RegisterCategory(cat);
        }

        _mainWindow?.EnableDragMode();

        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            _mainWindow?.DisableDragMode();

            // Persist any position change made by dragging
            if (_mainWindow != null && _settings != null)
            {
                _settings.WindowLeft = _mainWindow.Left;
                _settings.WindowTop  = _mainWindow.Top;
                _settings.Save();
            }
        };

        _settingsWindow.Show();
    }

    // ── Connection sequence ────────────────────────────────────────────────

    /// <summary>
    /// Connects to Discord IPC, authenticates (or authorizes on first run),
    /// subscribes to global events, and seeds initial channel state.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct)
    {
        try
        {
            // Phase 1: wait for Discord IPC pipe — polls every 5 s if Discord is not running
            bool waitLogged = false;
            while (true)
            {
                try
                {
                    await _ipcClient!.ConnectAsync(ct);
                    break;
                }
                catch (DiscordNotRunningException)
                {
                    if (!waitLogged)
                    {
                        waitLogged = true;
                        LogService.Info("App: Discord not running — retrying every 5 s.");
                        _voiceService!.SetWaitingForDiscord();
                    }
                    // _pipe/_readCts/_readTask are all null (never set on a failed connect),
                    // so ConnectAsync can be retried on the same instance — no dispose/recreate needed.
                    await Task.Delay(5_000, ct);
                }
            }

            LogService.Info("App: waiting for Discord READY...");
            await _ipcClient.WaitForReadyAsync(ct);

            var bundle = _tokenStorage!.ReadToken();

            if (bundle == null)
            {
                bundle = await AuthorizeAsync(ct);
            }
            else if (_tokenStorage.IsTokenExpiredOrExpiringSoon(bundle))
            {
                LogService.Info("App: Token expiring soon; refreshing.");
                bundle = await _tokenStorage.RefreshToken(bundle.RefreshToken, GetClientId());
                _tokenStorage.WriteToken(bundle);
            }

            LogService.Info("App: sending AUTHENTICATE...");
            var grantedScopes = await _ipcClient!.SendAuthenticate(bundle.AccessToken, ct);

            // If the stored token pre-dates a scope addition, silently re-authorize
            if (!grantedScopes.Contains("guilds"))
            {
                LogService.Info("App: Token missing 'guilds' scope — re-authorizing to upgrade.");
                _tokenStorage.DeleteToken();
                bundle = await AuthorizeAsync(ct);
                await _ipcClient.SendAuthenticate(bundle.AccessToken, ct);
            }

            await _ipcClient.FetchAndCacheGuildsAsync(ct);

            // Subscribe global events
            await _ipcClient.Subscribe("VOICE_CHANNEL_SELECT",    null, ct);
            await _ipcClient.Subscribe("VOICE_CONNECTION_STATUS", null, ct);

            _voiceService!.SetConnected();

            // Seed initial channel state
            await _voiceService.SeedInitialChannelAsync(ct);

            _retryAttempt = 0;
            LogService.Info("App: Discord connection established.");
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            LogService.Error("App: Connection sequence failed.", ex);
        }
    }

    private async Task<TokenBundle> AuthorizeAsync(CancellationToken ct)
    {
        LogService.Info("App: No token found (or scope upgrade required); starting authorization flow.");
        var verifier  = _tokenStorage!.GeneratePkceVerifier();
        var challenge = _tokenStorage.GeneratePkceChallenge(verifier);
        var code      = await _ipcClient!.SendAuthorize(challenge, ct);
        var bundle    = await _tokenStorage.ExchangeCode(code, verifier, GetClientId());
        _tokenStorage.WriteToken(bundle);
        return bundle;
    }

    // ── Reconnect loop ─────────────────────────────────────────────────────

    private void OnConnectionDropped(object? sender, EventArgs e)
    {
        LogService.Info("App: Connection dropped; starting reconnect loop.");
        _ = ReconnectLoopAsync(_appCts.Token);
    }

    private void OnAuthRevoked(object? sender, EventArgs e)
    {
        LogService.Info("App: Auth revoked; waiting for user to re-authorize via overlay banner.");
        // Connection state = Failed — overlay banner prompts the user; no automatic retry
    }

    private void OnReAuthorizationRequested(object? sender, EventArgs e)
    {
        LogService.Info("App: Re-authorization requested by user — deleting token and reconnecting.");
        _tokenStorage?.DeleteToken();
        _ = ReconnectLoopAsync(_appCts.Token);
    }

    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            int slot    = Math.Min(_retryAttempt, BackoffSlots.Length - 1);
            int baseMs  = BackoffSlots[slot] * 1000;
            // Full jitter: random in [0, baseMs]
            int delayMs = baseMs == 0 ? 0 : Random.Shared.Next(0, baseMs);

            if (delayMs > 0)
            {
                LogService.Info($"App: Reconnect attempt {_retryAttempt + 1} — waiting {delayMs} ms.");
                await Task.Delay(delayMs, ct);
            }

            _retryAttempt++;

            try
            {
                await _ipcClient!.ResetForReconnectAsync();

                await ConnectAsync(ct);
                return; // success
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                LogService.Error($"App: Reconnect attempt {_retryAttempt} failed.", ex);
            }
        }
    }

    // ── System tray ────────────────────────────────────────────────────────

    private void SetupTrayIcon()
    {
        try
        {
            var resourceInfo = GetResourceStream(new Uri("pack://application:,,,/app.ico"));
            _iconDefault = resourceInfo?.Stream != null
                ? new System.Drawing.Icon(resourceInfo.Stream)
                : System.Drawing.SystemIcons.Application;
        }
        catch (Exception ex)
        {
            LogService.Error("App: Failed to load tray icon from resources.", ex);
            _iconDefault = System.Drawing.SystemIcons.Application;
        }

        _iconAmber = CreateDotIcon(_iconDefault, System.Drawing.Color.FromArgb(255, 176, 0));
        _iconRed   = CreateDotIcon(_iconDefault, System.Drawing.Color.FromArgb(220, 50,  47));

        var menu         = new WinForms.ContextMenuStrip();
        var showItem     = new WinForms.ToolStripMenuItem("Show Overlay");
        var hideItem     = new WinForms.ToolStripMenuItem("Hide Overlay");
        var settingsItem = new WinForms.ToolStripMenuItem("Settings");
        var exitItem     = new WinForms.ToolStripMenuItem("Exit");

        showItem.Click     += (_, _) => { _mainWindow?.Show(); _mainWindow?.Activate(); };
        hideItem.Click     += (_, _) => _mainWindow?.Hide();
        settingsItem.Click += (_, _) => ShowSettings();
        exitItem.Click     += (_, _) =>
        {
            if (_notifyIcon != null) { _notifyIcon.Visible = false; _notifyIcon.Dispose(); _notifyIcon = null; }
            Current.Shutdown();
        };

        menu.Items.Add(showItem);
        menu.Items.Add(hideItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon             = _iconDefault,
            ContextMenuStrip = menu,
            Text             = "SpeakerSight",
            Visible          = true
        };

        _notifyIcon.DoubleClick += (_, _) =>
        {
            if (_mainWindow?.IsVisible == true)
                _mainWindow.Hide();
            else
            {
                _mainWindow?.Show();
                _mainWindow?.Activate();
            }
        };

        // Drive tray icon color from connection state
        if (_overlayViewModel != null)
        {
            _overlayViewModel.PropertyChanged += (_, pe) =>
            {
                if (pe.PropertyName == nameof(OverlayViewModel.ConnectionIndicator))
                    UpdateTrayIcon(_overlayViewModel.ConnectionIndicator);
                else if (pe.PropertyName == nameof(OverlayViewModel.IsAuthRequired))
                {
                    // Auth-required uses a red icon; clearing it falls back to ConnectionIndicator
                    if (_overlayViewModel.IsAuthRequired)
                    {
                        if (_notifyIcon != null) _notifyIcon.Icon = _iconRed ?? _iconDefault;
                    }
                    else
                    {
                        UpdateTrayIcon(_overlayViewModel.ConnectionIndicator);
                    }
                }
            };
        }
    }

    private void UpdateTrayIcon(string? connectionIndicator)
    {
        if (_notifyIcon == null) return;

        _notifyIcon.Icon = connectionIndicator switch
        {
            { } s when s.Contains("Waiting")      => _iconAmber ?? _iconDefault,
            { } s when s.Contains("Reconnecting") => _iconAmber ?? _iconDefault,
            { } s when s.Contains("Disconnected") => _iconRed   ?? _iconDefault,
            _                                      => _iconDefault
        };
    }

    private static System.Drawing.Icon? CreateDotIcon(System.Drawing.Icon baseIcon, System.Drawing.Color dotColor)
    {
        try
        {
            using var bmp = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.DrawIcon(baseIcon, new System.Drawing.Rectangle(0, 0, 16, 16));
                using var brush = new System.Drawing.SolidBrush(dotColor);
                g.FillEllipse(brush, 10, 10, 5, 5);
            }
            var hIcon = bmp.GetHicon();
            var icon  = System.Drawing.Icon.FromHandle(hIcon);
            return (System.Drawing.Icon)icon.Clone(); // clone so we own the handle's copy
        }
        catch (Exception ex)
        {
            LogService.Error($"App: Could not create indicator icon — {ex.Message}");
            return null;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string GetClientId() => "1488518361783603352";
}
