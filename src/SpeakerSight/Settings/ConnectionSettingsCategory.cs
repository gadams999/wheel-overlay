using System;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using OpenDash.SpeakerSight.Models;
using OpenDash.SpeakerSight.Services;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.SpeakerSight.Settings;

public class ConnectionSettingsCategory : ISettingsCategory
{
    private readonly DiscordIpcClient _ipcClient;
    private readonly TokenStorageService _tokenStorage;

    private TextBlock? _statusText;

    public string CategoryName => "Connection";
    public int SortOrder => 10;

    public ConnectionSettingsCategory(DiscordIpcClient ipcClient, TokenStorageService tokenStorage)
    {
        _ipcClient    = ipcClient;
        _tokenStorage = tokenStorage;
    }

    public FrameworkElement CreateContent()
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        _statusText = new TextBlock
        {
            Text         = GetStatusText(),
            Margin       = new Thickness(0, 0, 0, 16),
            FontSize     = 14,
            TextWrapping = TextWrapping.Wrap
        };
        _statusText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(_statusText);

        var reAuthButton = new Button { Content = "Re-authorize", Margin = new Thickness(0, 0, 0, 8) };
        reAuthButton.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignRaisedButton");
        reAuthButton.Click += OnReAuthorize;
        panel.Children.Add(reAuthButton);

        var disconnectButton = new Button { Content = "Disconnect / Forget Token" };
        disconnectButton.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignOutlinedButton");
        disconnectButton.Click += OnDisconnect;
        panel.Children.Add(disconnectButton);

        return panel;
    }

    public void SaveValues() { /* No persistable fields in this category */ }

    public void LoadValues()
    {
        if (_statusText != null)
            _statusText.Text = GetStatusText();
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private async void OnReAuthorize(object sender, RoutedEventArgs e)
    {
        if (_statusText != null)
            _statusText.Text = "Authorizing…";

        try
        {
            var verifier   = _tokenStorage.GeneratePkceVerifier();
            var challenge  = _tokenStorage.GeneratePkceChallenge(verifier);
            var code       = await _ipcClient.SendAuthorize(challenge);
            var bundle     = await _tokenStorage.ExchangeCode(code, verifier, GetClientId());
            _tokenStorage.WriteToken(bundle);
            await _ipcClient.SendAuthenticate(bundle.AccessToken);

            if (_statusText != null)
                _statusText.Text = "Connected";
        }
        catch (Exception ex)
        {
            LogService.Error("ConnectionSettingsCategory: Re-authorization failed.", ex);
            if (_statusText != null)
                _statusText.Text = "Authorization failed — see log for details.";
        }
    }

    private void OnDisconnect(object sender, RoutedEventArgs e)
    {
        _tokenStorage.DeleteToken();

        if (_statusText != null)
            _statusText.Text = "Disconnected — authorization required";
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private string GetStatusText()
    {
        var bundle = _tokenStorage.ReadToken();
        if (bundle == null)
            return "Status: Disconnected — authorization required";

        if (_tokenStorage.IsTokenExpiredOrExpiringSoon(bundle))
            return "Status: Token expiring soon — will refresh on next launch";

        return "Status: Connected (token present)";
    }

    // Retrieves the client ID from DiscordIpcClient via reflection-free constant access.
    // The constant is internal to DiscordIpcClient; expose a static helper here to avoid
    // duplicating the string literal.
    private static string GetClientId()
    {
        // Returns the same constant used in DiscordIpcClient.
        // Update both if the client ID changes.
        return "1488518361783603352";
    }
}
