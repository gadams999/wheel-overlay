using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using OpenDash.DiscordChatOverlay.Models;
using OpenDash.DiscordChatOverlay.Services;
using OpenDash.DiscordChatOverlay.Settings;
using OpenDash.DiscordChatOverlay.ViewModels;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.DiscordChatOverlay;

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

        if (_settings.ShowOnStartup)
            _mainWindow.Show();

        // Build settings view model with currently available categories
        _settingsViewModel = new SettingsViewModel(_settings, new List<ISettingsCategory>
        {
            new ConnectionSettingsCategory(_ipcClient, _tokenStorage),
            new DisplaySettingsCategory(_settings),
            new AppearanceSettingsCategory(_settings, _mainWindow, _themeService)
        });

        // Hide/show overlay based on channel membership
        _overlayViewModel.PropertyChanged += (_, pe) =>
        {
            if (pe.PropertyName == nameof(OverlayViewModel.IsInChannel))
            {
                if (_overlayViewModel.IsInChannel)
                    _mainWindow.Show();
                else
                    _mainWindow.Hide();
            }
        };

        _ipcClient.ConnectionDropped += OnConnectionDropped;
        _ipcClient.AuthRevoked       += OnAuthRevoked;

        _ = ConnectAsync(_appCts.Token);

        LogService.Info("DiscordChatOverlay startup complete.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appCts.Cancel();
        _settingsWindow?.Close();
        _themeService?.Dispose();
        _voiceService?.Dispose();
        _ = _ipcClient?.DisposeAsync();
        LogService.Info("DiscordChatOverlay exiting.");
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

        _mainWindow?.SuspendClickThrough();

        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            _mainWindow?.RestoreClickThrough();
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
            await _ipcClient!.ConnectAsync(ct);

            var bundle = _tokenStorage!.ReadToken();

            if (bundle == null)
            {
                // First run — full PKCE authorization flow
                LogService.Info("App: No token found; starting authorization flow.");
                var verifier  = _tokenStorage.GeneratePkceVerifier();
                var challenge = _tokenStorage.GeneratePkceChallenge(verifier);
                var code      = await _ipcClient.SendAuthorize(challenge, ct);
                bundle        = await _tokenStorage.ExchangeCode(code, verifier, GetClientId());
                _tokenStorage.WriteToken(bundle);
            }
            else if (_tokenStorage.IsTokenExpiredOrExpiringSoon(bundle))
            {
                LogService.Info("App: Token expiring soon; refreshing.");
                bundle = await _tokenStorage.RefreshToken(bundle.RefreshToken, GetClientId());
                _tokenStorage.WriteToken(bundle);
            }

            await _ipcClient.SendAuthenticate(bundle.AccessToken, ct);

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

    // ── Reconnect loop ─────────────────────────────────────────────────────

    private void OnConnectionDropped(object? sender, EventArgs e)
    {
        LogService.Info("App: Connection dropped; starting reconnect loop.");
        _ = ReconnectLoopAsync(_appCts.Token);
    }

    private void OnAuthRevoked(object? sender, EventArgs e)
    {
        LogService.Info("App: Auth revoked; reconnect loop stopped.");
        // Connection state = Failed — user must re-authorize via settings window
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
                await _ipcClient!.DisposeAsync();
                _ipcClient = new DiscordIpcClient();
                _ipcClient.ConnectionDropped += OnConnectionDropped;
                _ipcClient.AuthRevoked       += OnAuthRevoked;

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

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string GetClientId() => "1488518361783603352";
}
