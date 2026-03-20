using System.Windows;

namespace OpenDash.OverlayCore.Behaviors;

/// <summary>
/// Provides base overlay window configuration patterns.
/// </summary>
public static class BaseOverlayWindow
{
    /// <summary>
    /// Applies standard overlay window properties: Topmost, no taskbar,
    /// transparent background, SizeToContent, AllowsTransparency.
    /// Call from Window.Loaded event.
    /// </summary>
    /// <param name="window">The window to configure as an overlay.</param>
    public static void ApplyOverlayDefaults(Window window)
    {
        // Topmost - always on top
        window.Topmost = true;

        // No taskbar icon
        window.ShowInTaskbar = false;

        // Transparent background
        window.Background = System.Windows.Media.Brushes.Transparent;

        // Size to content
        window.SizeToContent = SizeToContent.WidthAndHeight;

        // Allow transparency
        window.AllowsTransparency = true;

        // No window chrome
        window.WindowStyle = WindowStyle.None;

        // No resize mode
        window.ResizeMode = ResizeMode.NoResize;
    }
}
