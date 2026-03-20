using System;
using System.Windows;
using System.Windows.Interop;
using OpenDash.OverlayCore.Services;
using WpfBrush = System.Windows.Media.Brush;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfKey = System.Windows.Input.Key;
using WpfMouseButton = System.Windows.Input.MouseButton;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace OpenDash.OverlayCore.Behaviors;

/// <summary>
/// Encapsulates the Enter-to-confirm / Escape-to-cancel overlay repositioning pattern.
/// Stores original position on Enter(), enables drag, applies semi-transparent background.
/// Exit(true) saves position; Exit(false) restores original.
/// </summary>
public class ConfigModeBehavior
{
    private Window? _window;
    private double _originalLeft;
    private double _originalTop;
    private WpfBrush? _originalBackground;
    private bool _isActive;

    /// <summary>
    /// Event raised when the user confirms the position (Enter key).
    /// </summary>
    public event EventHandler? PositionConfirmed;

    /// <summary>
    /// Event raised when the user cancels the position change (Escape key).
    /// </summary>
    public event EventHandler? PositionCancelled;

    /// <summary>
    /// Gets whether config mode is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Enters config mode for the specified window.
    /// Stores the original position, enables drag, and applies semi-transparent background.
    /// </summary>
    /// <param name="window">The window to enter config mode for.</param>
    public void Enter(Window window)
    {
        if (_isActive)
            return;

        _window = window;
        _isActive = true;

        // Store original position
        _originalLeft = window.Left;
        _originalTop = window.Top;
        _originalBackground = window.Background;

        // Apply semi-transparent gray background (80% opacity)
        window.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 128, 128, 128));

        // Remove click-through to enable dragging
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero)
        {
            WindowTransparencyHelper.RemoveClickThrough(hwnd);
        }

        // Enable dragging
        window.MouseDown += Window_MouseDown;
        window.KeyDown += Window_KeyDown;
    }

    /// <summary>
    /// Handles key down events for Enter (confirm) and Escape (cancel).
    /// </summary>
    /// <param name="e">The key event args.</param>
    public void HandleKeyDown(WpfKeyEventArgs e)
    {
        if (!_isActive)
            return;

        if (e.Key == WpfKey.Enter)
        {
            Exit(confirm: true);
        }
        else if (e.Key == WpfKey.Escape)
        {
            Exit(confirm: false);
        }
    }

    /// <summary>
    /// Exits config mode.
    /// </summary>
    /// <param name="confirm">True to save the new position, false to restore the original position.</param>
    public void Exit(bool confirm)
    {
        if (!_isActive || _window == null)
            return;

        _isActive = false;

        // Restore or save position
        if (!confirm)
        {
            _window.Left = _originalLeft;
            _window.Top = _originalTop;
            PositionCancelled?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            PositionConfirmed?.Invoke(this, EventArgs.Empty);
        }

        // Restore original background
        _window.Background = _originalBackground;

        // Re-apply click-through
        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd != IntPtr.Zero)
        {
            WindowTransparencyHelper.MakeClickThrough(hwnd);
        }

        // Disable dragging
        _window.MouseDown -= Window_MouseDown;
        _window.KeyDown -= Window_KeyDown;

        _window = null;
    }

    private void Window_MouseDown(object sender, WpfMouseButtonEventArgs e)
    {
        if (e.ChangedButton == WpfMouseButton.Left && _isActive && _window != null)
        {
            _window.DragMove();
        }
    }

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        HandleKeyDown(e);
    }
}
