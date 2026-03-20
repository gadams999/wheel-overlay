using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.WheelOverlay.Settings;

/// <summary>
/// WheelOverlay About panel. Registered at SortOrder=999 by App.xaml.cs.
/// </summary>
public sealed class AboutSettingsCategory : ISettingsCategory
{
    private readonly ThemeService? _themeService;

    public AboutSettingsCategory(ThemeService? themeService = null)
    {
        _themeService = themeService;
    }

    public string CategoryName => "About";
    public int SortOrder => 999;

    public FrameworkElement CreateContent()
    {
        var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

        // App icon (theme-aware)
        var icon = new Image
        {
            Width = 80,
            Height = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        LoadIcon(icon);
        panel.Children.Add(icon);

        // App name
        var appName = new TextBlock
        {
            Text = VersionInfo.ProductName,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        appName.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(appName);

        // Version
        var versionText = new TextBlock
        {
            Text = VersionInfo.GetFullVersionString(),
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        versionText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(versionText);

        // Description
        var description = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        description.Inlines.Add(new Run("A customizable overlay application for sim racing wheels."));
        description.Inlines.Add(new LineBreak());
        description.Inlines.Add(new LineBreak());
        description.Inlines.Add(new Run("Display text labels for different rotary encoder positions on your screen during races."));
        description.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(description);

        // GitHub link
        var linkText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        linkText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        var hyperlink = new Hyperlink
        {
            NavigateUri = new Uri("https://github.com/gadams999/wheel-overlay")
        };
        hyperlink.SetResourceReference(Hyperlink.ForegroundProperty, "ThemeAccent");
        hyperlink.Inlines.Add("GitHub Repository");
        hyperlink.RequestNavigate += OnHyperlinkNavigate;
        linkText.Inlines.Add(hyperlink);
        panel.Children.Add(linkText);

        // Copyright
        var copyright = new TextBlock
        {
            Text = VersionInfo.Copyright,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        copyright.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
        panel.Children.Add(copyright);

        return panel;
    }

    private void LoadIcon(Image image)
    {
        try
        {
            bool isDark = _themeService?.IsDarkMode ?? false;
            var fileName = isDark ? "about_icon_light.png" : "about_icon_dark.png";
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (System.IO.File.Exists(path))
                image.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load about icon", ex);
        }
    }

    private static void OnHyperlinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to open GitHub link", ex);
        }
    }

    public void SaveValues() { /* no-op */ }
    public void LoadValues() { /* no-op */ }
}
