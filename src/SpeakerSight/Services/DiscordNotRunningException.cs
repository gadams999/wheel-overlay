namespace OpenDash.SpeakerSight.Services;

/// <summary>
/// Thrown by <see cref="DiscordIpcClient.ConnectAsync"/> when no Discord IPC pipe
/// is found on slots 0–9, indicating Discord is not currently running.
/// </summary>
public sealed class DiscordNotRunningException : Exception
{
    public DiscordNotRunningException() : base("Discord IPC pipe not available.") { }
}
