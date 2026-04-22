using System;
using System.ComponentModel;

namespace OpenDash.SpeakerSight.Models;

public class ActiveSpeaker : INotifyPropertyChanged
{
    public string UserId { get; set; } = string.Empty;

    private string _displayName = string.Empty;
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }
    }

    public string? AvatarHash { get; set; }
    public string? GuildAvatarHash { get; set; }
    public string? GuildId { get; set; }
    public bool AvatarVisible { get; set; } = true;

    public DateTimeOffset LastActivatedUtc { get; set; } = DateTimeOffset.MinValue;

    private SpeakerState _state = SpeakerState.Silent;
    public SpeakerState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
            }
        }
    }

    private double _opacity = 0.0;
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
