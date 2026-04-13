using System.Collections.Generic;

namespace OpenDash.DiscordChatOverlay.Models;

public class ChannelContext
{
    public string GuildId { get; set; } = string.Empty;
    public string GuildName { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public List<ChannelMember> Members { get; set; } = new();
}
