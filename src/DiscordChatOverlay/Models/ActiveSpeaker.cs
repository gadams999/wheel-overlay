using System.ComponentModel;

namespace OpenDash.DiscordChatOverlay.Models;

public class ActiveSpeaker : INotifyPropertyChanged
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarHash { get; set; }
    public string? GuildAvatarHash { get; set; }
    public string? GuildId { get; set; }
    public bool AvatarVisible { get; set; } = true;
    public SpeakerState State { get; set; } = SpeakerState.Silent;

    private double _opacity = 1.0;
    public double Opacity
    {
        get => _opacity;
        set
        {
            if (_opacity != value)
            {
                _opacity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Opacity)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
