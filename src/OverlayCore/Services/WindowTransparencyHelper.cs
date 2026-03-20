using System;
using System.Runtime.InteropServices;

namespace OpenDash.OverlayCore.Services;

/// <summary>
/// Win32 interop utilities for making WPF windows click-through using WS_EX_TRANSPARENT extended window style.
/// </summary>
public static class WindowTransparencyHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    /// <summary>
    /// Makes a window click-through by applying the WS_EX_TRANSPARENT extended window style.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    public static void MakeClickThrough(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }

    /// <summary>
    /// Removes click-through behavior from a window by removing the WS_EX_TRANSPARENT extended window style.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    public static void RemoveClickThrough(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
    }

    /// <summary>
    /// Checks if a window has the WS_EX_TRANSPARENT extended window style (is click-through).
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <returns>True if the window is click-through, false otherwise.</returns>
    public static bool IsClickThrough(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        return (extendedStyle & WS_EX_TRANSPARENT) != 0;
    }
}
