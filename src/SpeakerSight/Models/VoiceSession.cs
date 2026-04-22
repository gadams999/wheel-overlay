using System.Collections.Generic;

namespace OpenDash.SpeakerSight.Models;

public class VoiceSession
{
    public string? ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public string? GuildId { get; set; }
    public string? GuildName { get; set; }
    public Dictionary<string, ParticipantSnapshot> Participants { get; set; } = new();
    public ConnectionState ConnectionState { get; set; } = ConnectionState.Disconnected;
}
