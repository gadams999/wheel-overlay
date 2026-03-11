using System;
using System.Windows.Forms;

namespace OpenDash.OverlayCore.Services;

/// <summary>
/// Provides reusable NotifyIcon setup patterns for system tray integration.
/// </summary>
public class SystemTrayScaffold : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the underlying NotifyIcon instance.
    /// </summary>
    public NotifyIcon NotifyIcon { get; }

    /// <summary>
    /// Creates a new SystemTrayScaffold with the specified tooltip and icon.
    /// </summary>
    /// <param name="tooltipText">The tooltip text to display when hovering over the tray icon.</param>
    /// <param name="icon">The icon to display in the system tray.</param>
    public SystemTrayScaffold(string tooltipText, System.Drawing.Icon icon)
    {
        NotifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = tooltipText
        };
    }

    /// <summary>
    /// Sets the context menu for the tray icon.
    /// </summary>
    /// <param name="menu">The context menu to display when right-clicking the tray icon.</param>
    public void SetContextMenu(ContextMenuStrip menu)
    {
        NotifyIcon.ContextMenuStrip = menu;
    }

    /// <summary>
    /// Shows a balloon tip notification from the tray icon.
    /// </summary>
    /// <param name="title">The title of the balloon tip.</param>
    /// <param name="text">The text content of the balloon tip.</param>
    /// <param name="timeout">The timeout in milliseconds before the balloon tip disappears. Default is 3000ms.</param>
    public void ShowBalloonTip(string title, string text, int timeout = 3000)
    {
        NotifyIcon.ShowBalloonTip(timeout, title, text, ToolTipIcon.Info);
    }

    /// <summary>
    /// Disposes the SystemTrayScaffold and removes the tray icon.
    /// </summary>
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
                NotifyIcon.Visible = false;
                NotifyIcon.Dispose();
            }
            _disposed = true;
        }
    }
}
