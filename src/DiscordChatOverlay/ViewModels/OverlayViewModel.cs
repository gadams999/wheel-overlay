using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Application = System.Windows.Application;
using OpenDash.DiscordChatOverlay.Models;
using OpenDash.DiscordChatOverlay.Services;

namespace OpenDash.DiscordChatOverlay.ViewModels;

public class OverlayViewModel : INotifyPropertyChanged
{
    private const int Cap = 8;

    private readonly VoiceSessionService _voiceService;
    private readonly AppSettings         _settings;

    public ObservableCollection<ActiveSpeaker> ActiveSpeakers { get; } = new();

    private int _overflowCount;
    public int OverflowCount
    {
        get => _overflowCount;
        private set { if (_overflowCount != value) { _overflowCount = value; OnPropertyChanged(); } }
    }

    private string? _connectionIndicator;
    public string? ConnectionIndicator
    {
        get => _connectionIndicator;
        private set { if (_connectionIndicator != value) { _connectionIndicator = value; OnPropertyChanged(); } }
    }

    private bool _isInChannel;
    public bool IsInChannel
    {
        get => _isInChannel;
        private set { if (_isInChannel != value) { _isInChannel = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public OverlayViewModel(VoiceSessionService voiceService, AppSettings settings)
    {
        _voiceService = voiceService;
        _settings     = settings;

        voiceService.ActiveSpeakers.CollectionChanged += OnSourceCollectionChanged;
        ((INotifyPropertyChanged)voiceService).PropertyChanged += OnVoiceServicePropertyChanged;

        // Subscribe to any speakers already in the collection
        foreach (var s in voiceService.ActiveSpeakers)
            s.PropertyChanged += OnSpeakerPropertyChanged;

        Rebuild();
    }

    // ── Source change handlers ─────────────────────────────────────────────

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ActiveSpeaker s in e.NewItems) s.PropertyChanged += OnSpeakerPropertyChanged;
        if (e.OldItems != null)
            foreach (ActiveSpeaker s in e.OldItems) s.PropertyChanged -= OnSpeakerPropertyChanged;

        Rebuild();
    }

    private void OnSpeakerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActiveSpeaker.State))
            Rebuild();
    }

    private void OnVoiceServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VoiceSessionService.ConnectionState))
        {
            ConnectionIndicator = _voiceService.ConnectionState switch
            {
                ConnectionState.Retrying => "⟳ Reconnecting…",
                ConnectionState.Failed   => "✕ Disconnected",
                _                        => null
            };
            IsInChannel = _voiceService.Session.ChannelId != null;
        }
    }

    // ── Rebuild ────────────────────────────────────────────────────────────

    private void Rebuild()
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(Rebuild));
            return;
        }

        var ordered = BuildDisplayList(_voiceService.ActiveSpeakers, _settings.DisplayMode);

        // Update OverflowCount: excess active/recently-active beyond cap
        var speakingCount = _voiceService.ActiveSpeakers
            .Count(s => s.State == SpeakerState.Active || s.State == SpeakerState.RecentlyActive);
        OverflowCount = Math.Max(0, speakingCount - Cap);

        // Sync ActiveSpeakers in-place to minimise collection churn
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i >= ActiveSpeakers.Count)
                ActiveSpeakers.Add(ordered[i]);
            else if (!ReferenceEquals(ActiveSpeakers[i], ordered[i]))
                ActiveSpeakers[i] = ordered[i];
        }
        while (ActiveSpeakers.Count > ordered.Count)
            ActiveSpeakers.RemoveAt(ActiveSpeakers.Count - 1);

        IsInChannel = _voiceService.Session.ChannelId != null;
    }

    // ── Pure ordering + capping logic (testable) ───────────────────────────

    /// <summary>
    /// Builds a capped (max 8), ordered list from <paramref name="source"/>:
    ///   Active speakers (most-recently-activated first)
    ///   → RecentlyActive (most-recently-activated first)
    ///   → Silent (alphabetical, all-members mode only)
    /// </summary>
    public static IReadOnlyList<ActiveSpeaker> BuildDisplayList(
        IEnumerable<ActiveSpeaker> source, DisplayMode mode)
    {
        var speakers = source.ToList();

        var active        = speakers.Where(s => s.State == SpeakerState.Active)
                                    .OrderByDescending(s => s.LastActivatedUtc)
                                    .ToList();
        var recentlyActive= speakers.Where(s => s.State == SpeakerState.RecentlyActive)
                                    .OrderByDescending(s => s.LastActivatedUtc)
                                    .ToList();
        var silent        = speakers.Where(s => s.State == SpeakerState.Silent)
                                    .OrderBy(s => s.DisplayName)
                                    .ToList();

        var result = new List<ActiveSpeaker>(Cap);

        // Fill with speaking group first (capped)
        foreach (var s in active.Concat(recentlyActive))
        {
            if (result.Count >= Cap) break;
            result.Add(s);
        }

        // In all-members mode, fill remaining slots with silent members
        if (mode == DisplayMode.AllMembers)
        {
            foreach (var s in silent)
            {
                if (result.Count >= Cap) break;
                result.Add(s);
            }
        }

        return result;
    }
}
