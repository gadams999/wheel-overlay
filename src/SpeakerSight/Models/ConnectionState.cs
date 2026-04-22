namespace OpenDash.SpeakerSight.Models;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Retrying,
    WaitingForDiscord,
    Failed
}
