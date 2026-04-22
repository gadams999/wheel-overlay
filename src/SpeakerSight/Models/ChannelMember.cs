namespace OpenDash.SpeakerSight.Models;

public class ChannelMember
{
    public string UserId { get; set; } = string.Empty;
    public string LastKnownName { get; set; } = string.Empty;
    public string? CustomDisplayName { get; set; }
    public bool AvatarVisible { get; set; } = true;
}
