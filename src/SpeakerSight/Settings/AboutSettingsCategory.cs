using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.SpeakerSight.Settings;

public class AboutSettingsCategory : ISettingsCategory
{
    public string CategoryName => "About";
    public int SortOrder => 999;

    public FrameworkElement CreateContent()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionText = version != null
            ? $"v{version.Major}.{version.Minor}.{version.Build}"
            : "v0.1.0";

        var panel = new StackPanel { Margin = new Thickness(16) };

        var title = new TextBlock
        {
            Text       = $"SpeakerSight  {versionText}",
            FontSize   = 16,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 12)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(title);

        var desc = new TextBlock
        {
            Text         = "Displays active Discord voice channel participants as an always-on-top overlay.",
            TextWrapping = TextWrapping.Wrap,
            Margin       = new Thickness(0, 0, 0, 16)
        };
        desc.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(desc);

        var docsBlock = new TextBlock { Margin = new Thickness(0, 0, 0, 8) };
        var docsLink  = new Hyperlink(new Run("Documentation — OpenDash Overlays"))
        {
            NavigateUri = new Uri("https://docs.opendashoverlays.com/speakersight/")
        };
        docsLink.SetResourceReference(Hyperlink.ForegroundProperty, "ThemeAccent");
        docsLink.RequestNavigate += OnRequestNavigate;
        docsBlock.Inlines.Add(docsLink);
        docsBlock.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(docsBlock);

        var note = new TextBlock
        {
            Text         = "Note: The rpc.voice.read scope requires Discord developer whitelist approval for public distribution.",
            TextWrapping = TextWrapping.Wrap,
            FontStyle    = FontStyles.Italic,
            Margin       = new Thickness(0, 12, 0, 0)
        };
        note.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
        panel.Children.Add(note);

        return panel;
    }

    public void SaveValues() { }
    public void LoadValues() { }

    private static void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { /* Silently ignore navigation failures */ }
        e.Handled = true;
    }
}
