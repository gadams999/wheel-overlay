namespace OpenDash.SpeakerSight.Models;

public class ParticipantSnapshot
{
    public string UserId { get; set; } = string.Empty;
    public string DiscordDisplayName { get; set; } = string.Empty;
    public string? AvatarHash { get; set; }
    public string? GuildAvatarHash { get; set; }
    public bool IsMuted { get; set; }
    public bool IsDeafened { get; set; }
}
