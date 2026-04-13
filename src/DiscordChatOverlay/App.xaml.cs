using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using OpenDash.DiscordChatOverlay.Models;
using OpenDash.DiscordChatOverlay.Services;
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

        _ipcClient.ConnectionDropped += OnConnectionDropped;
        _ipcClient.AuthRevoked       += OnAuthRevoked;

        _ = ConnectAsync(_appCts.Token);

        LogService.Info("DiscordChatOverlay startup complete.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appCts.Cancel();
        _themeService?.Dispose();
        _ = _ipcClient?.DisposeAsync();
        LogService.Info("DiscordChatOverlay exiting.");
        base.OnExit(e);
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
                var verifier   = _tokenStorage.GeneratePkceVerifier();
                var challenge  = _tokenStorage.GeneratePkceChallenge(verifier);
                var code       = await _ipcClient.SendAuthorize(challenge, ct);
                bundle         = await _tokenStorage.ExchangeCode(code, verifier, GetClientId());
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
            await _ipcClient.Subscribe("VOICE_CHANNEL_SELECT", null, ct);
            await _ipcClient.Subscribe("VOICE_CONNECTION_STATUS", null, ct);

            // Seed initial channel state
            var channelData = await _ipcClient.GetSelectedVoiceChannel(ct);
            if (channelData.HasValue)
            {
                var chName = channelData.Value.TryGetProperty("name", out var n) ? n.GetString() : "?";
                LogService.Info($"App: Currently in channel '{chName}'");
            }
            else
            {
                LogService.Info("App: Not currently in a voice channel.");
            }

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
