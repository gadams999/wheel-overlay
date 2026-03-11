namespace OpenDash.OverlayCore.Models;

/// <summary>
/// User preference for application theme.
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// Follow the Windows system theme (light or dark).
    /// </summary>
    System,

    /// <summary>
    /// Always use light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Always use dark theme.
    /// </summary>
    Dark
}
