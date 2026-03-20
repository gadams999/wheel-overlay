using System;
using System.Runtime.InteropServices;

namespace OpenDash.OverlayCore.Services;

/// <summary>
/// Registers Alt+F6 as a global hotkey and fires <see cref="ToggleModeRequested"/>
/// when the key combination is pressed system-wide.
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    private const int HOTKEY_ID = 0x0001;
    private const int MOD_ALT   = 0x0001;
    private const int VK_F6     = 0x75;
    private const int WM_HOTKEY = 0x0312;

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _registered = false;
    private bool _disposed = false;

    /// <summary>
    /// Fired on the UI thread when Alt+F6 is pressed system-wide.
    /// Overlay windows subscribe to this to toggle between overlay and positioning mode.
    /// </summary>
    public event EventHandler? ToggleModeRequested;

    /// <summary>
    /// Registers Alt+F6 as a global hotkey for the specified window handle.
    /// Returns <c>true</c> on success.
    /// On failure, logs a descriptive error via <see cref="LogService"/> and returns <c>false</c>.
    /// No exception is thrown — the application continues without the hotkey.
    /// </summary>
    /// <param name="windowHandle">HWND of the window that will receive WM_HOTKEY messages.</param>
    public bool Register(IntPtr windowHandle)
    {
        _hwnd = windowHandle;
        if (RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT, VK_F6))
        {
            _registered = true;
            return true;
        }

        LogService.Error(
            $"Failed to register global hotkey Alt+F6. Another application may be using this key combination. Error code: {Marshal.GetLastWin32Error()}");
        return false;
    }

    /// <summary>
    /// Unregisters the global hotkey. Safe to call even if <see cref="Register"/> returned <c>false</c>.
    /// </summary>
    public void Unregister()
    {
        if (_registered && _hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
            _registered = false;
        }
    }

    /// <summary>
    /// Must be called from the window's WndProc to process WM_HOTKEY messages.
    /// Fires <see cref="ToggleModeRequested"/> when <paramref name="msg"/> equals
    /// <c>WM_HOTKEY</c> and <paramref name="wParam"/> matches the registered hotkey ID.
    /// </summary>
    public void ProcessMessage(int msg, IntPtr wParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleModeRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            Unregister();
            _disposed = true;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
