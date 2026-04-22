using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Timer = System.Threading.Timer;
using Application = System.Windows.Application;
using OpenDash.SpeakerSight.Models;
using OpenDash.OverlayCore.Services;

namespace OpenDash.SpeakerSight.Services;

/// <summary>
/// Manages the current Discord voice session: tracks participants, runs the per-speaker
/// state machine (debounce → Active → grace-period fade → Silent), and exposes
/// an ObservableCollection of ActiveSpeaker objects for the overlay ViewModel.
/// </summary>
public class VoiceSessionService : INotifyPropertyChanged, IDisposable
{
    private readonly DiscordIpcClient _ipc;
    private readonly AliasService     _alias;
    private readonly AppSettings      _settings;
    private readonly CancellationTokenSource _cts = new();

    private readonly Dictionary<string, SpeakerStateMachine> _machines = new();
    private readonly object _lock = new();
    private string? _lastVoiceConnectionStatus;

    public VoiceSession Session { get; } = new();

    /// <summary>All channel participants as ActiveSpeaker objects (all states).</summary>
    public ObservableCollection<ActiveSpeaker> ActiveSpeakers { get; } = new();

    private ConnectionState _connectionState = ConnectionState.Disconnected;
    public ConnectionState ConnectionState
    {
        get => _connectionState;
        private set
        {
            if (_connectionState == value) return;
            _connectionState = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionState)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public VoiceSessionService(DiscordIpcClient ipc, AliasService alias, AppSettings settings)
    {
        _ipc      = ipc;
        _alias    = alias;
        _settings = settings;

        ipc.SpeakingStart              += OnSpeakingStart;
        ipc.SpeakingStop               += OnSpeakingStop;
        ipc.VoiceStateCreated          += OnVoiceStateCreated;
        ipc.VoiceStateUpdated          += OnVoiceStateUpdated;
        ipc.VoiceStateDeleted          += OnVoiceStateDeleted;
        ipc.VoiceChannelSelected       += OnVoiceChannelSelected;
        ipc.VoiceConnectionStatusChanged += OnVoiceConnectionStatusChanged;
        ipc.ConnectionDropped          += OnConnectionDropped;
        ipc.AuthRevoked                += OnAuthRevoked;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds initial state from the currently selected Discord voice channel (if any).
    /// Call once after a successful AUTHENTICATE.
    /// </summary>
    public async Task SeedInitialChannelAsync(CancellationToken ct)
    {
        try
        {
            var channelData = await _ipc.GetSelectedVoiceChannel(ct);
            if (!channelData.HasValue) return;

            var data      = channelData.Value;
            var channelId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (channelId == null) return;

            var guildId     = data.TryGetProperty("guild_id", out var gEl) && gEl.ValueKind != JsonValueKind.Null
                              ? gEl.GetString() : null;
            var channelName = data.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
            var guildName   = ExtractGuildName(data) ?? _ipc.GetGuildName(guildId);

            lock (_lock)
            {
                Session.ChannelId   = channelId;
                Session.GuildId     = guildId;
                Session.GuildName   = guildName;
                Session.ChannelName = channelName;
            }

            LogService.Info(
                $"VoiceSessionService: already in voice channel at startup — " +
                FormatGuildChannel(guildId, guildName, channelId, channelName));

            await SeedChannelAsync(channelId, guildId, guildName, channelName, data, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogService.Error("VoiceSessionService.SeedInitialChannelAsync failed.", ex);
        }
    }

    /// <summary>Call after successful AUTHENTICATE to mark the session as Connected.</summary>
    public void SetConnected() => ConnectionState = ConnectionState.Connected;

    /// <summary>Call when Discord is not running so the overlay can display a waiting indicator.</summary>
    public void SetWaitingForDiscord() => ConnectionState = ConnectionState.WaitingForDiscord;

    /// <summary>
    /// Re-resolves display names and AvatarVisible for all current ActiveSpeakers from AliasService.
    /// Call after the user saves alias changes in AliasSettingsCategory so the overlay updates immediately.
    /// </summary>
    public void RefreshDisplayNames()
    {
        string? channelId, guildId;
        lock (_lock) { channelId = Session.ChannelId; guildId = Session.GuildId; }

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            foreach (var speaker in ActiveSpeakers)
            {
                speaker.DisplayName   = _alias.Resolve(speaker.UserId, guildId ?? string.Empty, channelId ?? string.Empty, speaker.DisplayName);
                if (guildId != null && channelId != null)
                {
                    var ctx    = _alias.GetContext(guildId, channelId);
                    var member = ctx?.Members.FirstOrDefault(m => m.UserId == speaker.UserId);
                    if (member != null)
                        speaker.AvatarVisible = member.AvatarVisible;
                }
            }
        });
    }

    // ── Internal seeding ───────────────────────────────────────────────────

    private async Task SeedChannelAsync(
        string channelId, string? guildId, string? guildName, string? channelName,
        JsonElement channelData, CancellationToken ct)
    {
        if (guildId != null)
            _alias.UpsertChannelContext(guildId, guildName ?? string.Empty, channelId, channelName ?? channelId);

        LogService.Info(
            $"VoiceSessionService: seeding channel — " +
            FormatGuildChannel(guildId, guildName, channelId, channelName));

        // Subscribe channel-scoped events
        try
        {
            await _ipc.Subscribe("SPEAKING_START",      new { channel_id = channelId }, ct);
            await _ipc.Subscribe("SPEAKING_STOP",       new { channel_id = channelId }, ct);
            await _ipc.Subscribe("VOICE_STATE_CREATE",  new { channel_id = channelId }, ct);
            await _ipc.Subscribe("VOICE_STATE_UPDATE",  new { channel_id = channelId }, ct);
            await _ipc.Subscribe("VOICE_STATE_DELETE",  new { channel_id = channelId }, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            LogService.Error("VoiceSessionService: Failed to subscribe channel-scoped events.", ex);
        }

        // Seed participants from voice_states array
        if (!channelData.TryGetProperty("voice_states", out var vsArr)) return;

        foreach (var vs in vsArr.EnumerateArray())
        {
            var snapshot = ParseVoiceStateElement(vs);
            if (snapshot == null) continue;

            lock (_lock)
            {
                Session.Participants[snapshot.UserId] = snapshot;
                if (guildId != null)
                    _alias.UpsertChannelMember(guildId, channelId, snapshot.UserId, snapshot.DiscordDisplayName);
            }

            var speaker = BuildActiveSpeaker(snapshot, channelId, guildId);
            _ = Application.Current.Dispatcher.BeginInvoke(() => ActiveSpeakers.Add(speaker));

            lock (_lock)
            {
                if (!_machines.ContainsKey(snapshot.UserId))
                    _machines[snapshot.UserId] = new SpeakerStateMachine(speaker, _settings);
            }
        }
    }

    // ── IPC event handlers ─────────────────────────────────────────────────

    private void OnSpeakingStart(object? sender, SpeakingEventArgs e)
    {
        SpeakerStateMachine? m;
        lock (_lock) _machines.TryGetValue(e.UserId, out m);
        m?.OnSpeakingStart();
    }

    private void OnSpeakingStop(object? sender, SpeakingEventArgs e)
    {
        SpeakerStateMachine? m;
        lock (_lock) _machines.TryGetValue(e.UserId, out m);
        m?.OnSpeakingStop();
    }

    private void OnVoiceStateCreated(object? sender, VoiceStateEventArgs e)
    {
        string? channelId, guildId;
        lock (_lock) { channelId = Session.ChannelId; guildId = Session.GuildId; }
        if (channelId == null) return;

        var snapshot = new ParticipantSnapshot
        {
            UserId             = e.UserId,
            DiscordDisplayName = e.DisplayName,
            AvatarHash         = e.AvatarHash,
            IsMuted            = e.IsMuted,
            IsDeafened         = e.IsDeafened
        };

        lock (_lock)
        {
            Session.Participants[e.UserId] = snapshot;
            if (guildId != null)
                _alias.UpsertChannelMember(guildId, channelId, e.UserId, e.DisplayName);
        }

        var speaker = BuildActiveSpeaker(snapshot, channelId, guildId);
        Application.Current.Dispatcher.BeginInvoke(() => ActiveSpeakers.Add(speaker));

        lock (_lock)
        {
            if (!_machines.ContainsKey(e.UserId))
                _machines[e.UserId] = new SpeakerStateMachine(speaker, _settings);
        }
    }

    private void OnVoiceStateUpdated(object? sender, VoiceStateEventArgs e)
    {
        string? channelId, guildId;
        lock (_lock) { channelId = Session.ChannelId; guildId = Session.GuildId; }
        if (channelId == null) return;

        var snapshot = new ParticipantSnapshot
        {
            UserId             = e.UserId,
            DiscordDisplayName = e.DisplayName,
            AvatarHash         = e.AvatarHash,
            IsMuted            = e.IsMuted,
            IsDeafened         = e.IsDeafened
        };
        lock (_lock) { Session.Participants[e.UserId] = snapshot; }

        if (guildId != null)
            _alias.UpsertChannelMember(guildId, channelId, e.UserId, e.DisplayName);

        var resolvedName = _alias.Resolve(e.UserId, guildId ?? string.Empty, channelId, e.DisplayName);
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var existing = ActiveSpeakers.FirstOrDefault(s => s.UserId == e.UserId);
            if (existing != null)
                existing.DisplayName = resolvedName;
        });
    }

    private void OnVoiceStateDeleted(object? sender, VoiceStateDeletedArgs e)
    {
        SpeakerStateMachine? m;
        lock (_lock)
        {
            Session.Participants.Remove(e.UserId);
            _machines.TryGetValue(e.UserId, out m);
            _machines.Remove(e.UserId);
        }
        m?.Cancel();

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var existing = ActiveSpeakers.FirstOrDefault(s => s.UserId == e.UserId);
            if (existing != null) ActiveSpeakers.Remove(existing);
        });
    }

    private void OnVoiceChannelSelected(object? sender, ChannelSelectEventArgs e)
    {
        // Capture old channel for leave-log before overwriting session
        string? oldChannelId, oldChannelName, oldGuildId, oldGuildName;
        lock (_lock)
        {
            oldChannelId   = Session.ChannelId;
            oldChannelName = Session.ChannelName;
            oldGuildId     = Session.GuildId;
            oldGuildName   = Session.GuildName;
        }

        if (oldChannelId != null && e.ChannelId != oldChannelId)
            LogService.Info(
                $"VoiceSessionService: left voice channel — " +
                FormatGuildChannel(oldGuildId, oldGuildName, oldChannelId, oldChannelName));

        // Update session state BEFORE clearing so ViewModel sees the new ChannelId
        lock (_lock) { Session.ChannelId = e.ChannelId; Session.GuildId = e.GuildId; Session.GuildName = null; }

        ClearAllParticipants();

        if (e.ChannelId == null)
        {
            LogService.Info("VoiceSessionService: left voice channel — no longer in any channel.");
            return;
        }

        LogService.Info(
            $"VoiceSessionService: joining voice channel — " +
            FormatGuildChannel(e.GuildId, guildName: null, e.ChannelId, channelName: null));

        _ = Task.Run(async () =>
        {
            try
            {
                var ct          = _cts.Token;
                var channelData = await _ipc.GetSelectedVoiceChannel(ct);
                if (!channelData.HasValue) return;

                var channelName = channelData.Value.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                var guildName   = ExtractGuildName(channelData.Value) ?? _ipc.GetGuildName(e.GuildId);

                lock (_lock) { Session.ChannelName = channelName; Session.GuildName = guildName; }

                LogService.Info(
                    $"VoiceSessionService: joined voice channel — " +
                    FormatGuildChannel(e.GuildId, guildName, e.ChannelId, channelName));

                await SeedChannelAsync(e.ChannelId, e.GuildId, guildName, channelName, channelData.Value, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogService.Error("VoiceSessionService: VoiceChannelSelected seeding failed.", ex);
            }
        });
    }

    private void OnConnectionDropped(object? sender, EventArgs e)
    {
        lock (_lock) { Session.ChannelId = null; Session.GuildId = null; Session.GuildName = null; }
        ClearAllParticipants();
        ConnectionState = ConnectionState.Retrying;
    }

    private void OnAuthRevoked(object? sender, EventArgs e)
    {
        lock (_lock) { Session.ChannelId = null; Session.GuildId = null; Session.GuildName = null; }
        ClearAllParticipants();
        ConnectionState = ConnectionState.Failed;
    }

    private void OnVoiceConnectionStatusChanged(object? sender, string state)
    {
        // Suppress repeated identical states (Discord sends VOICE_CONNECTED every ~5s as a keepalive)
        if (state == _lastVoiceConnectionStatus) return;
        _lastVoiceConnectionStatus = state;

        string? channelId, channelName, guildId, guildName;
        lock (_lock)
        {
            channelId   = Session.ChannelId;
            channelName = Session.ChannelName;
            guildId     = Session.GuildId;
            guildName   = Session.GuildName ?? _ipc.GetGuildName(guildId);
        }

        if (state == "VOICE_CONNECTED")
            LogService.Info(
                $"VoiceSessionService: voice connected — " +
                FormatGuildChannel(guildId, guildName, channelId, channelName));
        else if (state is "DISCONNECTED" or "VOICE_DISCONNECTED")
            LogService.Info(
                $"VoiceSessionService: voice disconnected — " +
                FormatGuildChannel(guildId, guildName, channelId, channelName));
        else
            LogService.Info($"VoiceSessionService: voice connection status = {state}");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void ClearAllParticipants()
    {
        List<SpeakerStateMachine> machines;
        lock (_lock)
        {
            machines = new List<SpeakerStateMachine>(_machines.Values);
            _machines.Clear();
            Session.Participants.Clear();
        }
        foreach (var m in machines) m.Cancel();
        Application.Current.Dispatcher.BeginInvoke(() => ActiveSpeakers.Clear());
    }

    private ActiveSpeaker BuildActiveSpeaker(ParticipantSnapshot snapshot, string? channelId, string? guildId)
    {
        var displayName = _alias.Resolve(
            snapshot.UserId, guildId ?? string.Empty, channelId ?? string.Empty, snapshot.DiscordDisplayName);

        bool avatarVisible = true;
        if (guildId != null && channelId != null)
        {
            var ctx    = _alias.GetContext(guildId, channelId);
            var member = ctx?.Members.FirstOrDefault(m => m.UserId == snapshot.UserId);
            if (member != null) avatarVisible = member.AvatarVisible;
        }

        return new ActiveSpeaker
        {
            UserId         = snapshot.UserId,
            DisplayName    = displayName,
            AvatarHash     = snapshot.AvatarHash,
            GuildAvatarHash= snapshot.GuildAvatarHash,
            GuildId        = guildId,
            AvatarVisible  = avatarVisible,
            State          = SpeakerState.Silent,
            Opacity        = 0.0
        };
    }

    private static ParticipantSnapshot? ParseVoiceStateElement(JsonElement vs)
    {
        if (!vs.TryGetProperty("user", out var user)) return null;
        var userId = user.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(userId)) return null;

        string? avatarHash = null;
        if (user.TryGetProperty("avatar", out var av) && av.ValueKind != JsonValueKind.Null)
            avatarHash = av.GetString();

        string displayName = userId!;
        if (vs.TryGetProperty("nick", out var nickEl) && nickEl.ValueKind != JsonValueKind.Null
            && !string.IsNullOrEmpty(nickEl.GetString()))
            displayName = nickEl.GetString()!;
        else if (user.TryGetProperty("username", out var unEl) && !string.IsNullOrEmpty(unEl.GetString()))
            displayName = unEl.GetString()!;

        bool isMuted = false, isDeafened = false;
        if (vs.TryGetProperty("voice_state", out var vst))
        {
            if (vst.TryGetProperty("mute", out var m))           isMuted     = m.GetBoolean();
            else if (vst.TryGetProperty("self_mute", out var sm)) isMuted     = sm.GetBoolean();
            if (vst.TryGetProperty("deaf", out var d))           isDeafened  = d.GetBoolean();
            else if (vst.TryGetProperty("self_deaf", out var sd)) isDeafened  = sd.GetBoolean();
        }

        return new ParticipantSnapshot
        {
            UserId             = userId!,
            DiscordDisplayName = displayName,
            AvatarHash         = avatarHash,
            IsMuted            = isMuted,
            IsDeafened         = isDeafened
        };
    }

    /// <summary>
    /// Tries to extract the guild name from a channel response object.
    /// Discord IPC returns a <c>guild</c> sub-object with <c>id</c> and <c>name</c>.
    /// </summary>
    private static string? ExtractGuildName(JsonElement channelData)
    {
        if (channelData.TryGetProperty("guild", out var guildEl)
            && guildEl.ValueKind == JsonValueKind.Object
            && guildEl.TryGetProperty("name", out var nameEl)
            && nameEl.ValueKind != JsonValueKind.Null)
            return nameEl.GetString();
        return null;
    }

    /// <summary>
    /// Formats a guild+channel pair as <c>guild="name" (id) channel="name" (id)</c>.
    /// Falls back gracefully when names or IDs are unavailable.
    /// </summary>
    private static string FormatGuildChannel(
        string? guildId, string? guildName, string? channelId, string? channelName)
    {
        var guild = guildId != null
            ? (guildName != null ? $"guild=\"{guildName}\" ({guildId})" : $"guild=({guildId})")
            : "guild=none";
        var channel = channelId != null
            ? (channelName != null ? $"channel=\"{channelName}\" ({channelId})" : $"channel=({channelId})")
            : "channel=none";
        return $"{guild} {channel}";
    }

    // ── IDisposable ────────────────────────────────────────────────────────

    public void Dispose()
    {
        _cts.Cancel();

        // Unwire IPC events to prevent callbacks after disposal
        _ipc.SpeakingStart               -= OnSpeakingStart;
        _ipc.SpeakingStop                -= OnSpeakingStop;
        _ipc.VoiceStateCreated           -= OnVoiceStateCreated;
        _ipc.VoiceStateUpdated           -= OnVoiceStateUpdated;
        _ipc.VoiceStateDeleted           -= OnVoiceStateDeleted;
        _ipc.VoiceChannelSelected        -= OnVoiceChannelSelected;
        _ipc.VoiceConnectionStatusChanged -= OnVoiceConnectionStatusChanged;
        _ipc.ConnectionDropped           -= OnConnectionDropped;
        _ipc.AuthRevoked                 -= OnAuthRevoked;

        List<SpeakerStateMachine> machines;
        lock (_lock)
        {
            machines = new List<SpeakerStateMachine>(_machines.Values);
            _machines.Clear();
        }
        foreach (var m in machines) m.Cancel();
        _cts.Dispose();
    }

    // ── Inner class: SpeakerStateMachine ───────────────────────────────────

    private enum MachineState { Idle, Debouncing, Active, RecentlyActive, Silent }

    private sealed class SpeakerStateMachine
    {
        private MachineState     _state = MachineState.Idle;
        private readonly ActiveSpeaker _speaker;
        private readonly AppSettings   _settings;
        private Timer?           _debounceTimer;
        private DispatcherTimer? _fadeTimer;
        private readonly object  _lock = new();

        public SpeakerStateMachine(ActiveSpeaker speaker, AppSettings settings)
        {
            _speaker  = speaker;
            _settings = settings;
        }

        public void OnSpeakingStart()
        {
            Action? uiAction = null;

            lock (_lock)
            {
                switch (_state)
                {
                    case MachineState.RecentlyActive:
                        // Interrupt fade — go directly to Active
                        _state = MachineState.Active;
                        uiAction = () =>
                        {
                            _fadeTimer?.Stop();
                            _fadeTimer = null;
                            _speaker.Opacity           = 1.0;
                            _speaker.State             = SpeakerState.Active;
                            _speaker.LastActivatedUtc  = DateTimeOffset.UtcNow;
                        };
                        break;

                    case MachineState.Idle:
                    case MachineState.Silent:
                        if (_settings.DebounceThresholdMs > 0)
                        {
                            _state = MachineState.Debouncing;
                            _debounceTimer?.Dispose();
                            _debounceTimer = new Timer(OnDebounceElapsed, null,
                                _settings.DebounceThresholdMs, Timeout.Infinite);
                        }
                        else
                        {
                            _state = MachineState.Active;
                            uiAction = () =>
                            {
                                _speaker.Opacity          = 1.0;
                                _speaker.State            = SpeakerState.Active;
                                _speaker.LastActivatedUtc = DateTimeOffset.UtcNow;
                            };
                        }
                        break;

                    case MachineState.Debouncing:
                        // Reset debounce window
                        _debounceTimer?.Change(_settings.DebounceThresholdMs, Timeout.Infinite);
                        break;

                    case MachineState.Active:
                        break; // already active
                }
            }

            if (uiAction != null)
                Application.Current.Dispatcher.BeginInvoke(uiAction);
        }

        public void OnSpeakingStop()
        {
            bool startFade = false;

            lock (_lock)
            {
                switch (_state)
                {
                    case MachineState.Debouncing:
                        _debounceTimer?.Dispose();
                        _debounceTimer = null;
                        _state = MachineState.Idle;
                        break;

                    case MachineState.Active:
                        _state     = MachineState.RecentlyActive;
                        startFade  = true;
                        break;
                }
            }

            if (startFade) StartFade();
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                _state = MachineState.Idle;
                // Fade timer self-terminates on next tick because _state != RecentlyActive
            }
        }

        // ── Private helpers ────────────────────────────────────────────────

        private void OnDebounceElapsed(object? _)
        {
            Action? uiAction = null;

            lock (_lock)
            {
                if (_state != MachineState.Debouncing) return;
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                _state = MachineState.Active;
                uiAction = () =>
                {
                    _speaker.Opacity          = 1.0;
                    _speaker.State            = SpeakerState.Active;
                    _speaker.LastActivatedUtc = DateTimeOffset.UtcNow;
                };
            }

            if (uiAction != null)
                Application.Current.Dispatcher.BeginInvoke(uiAction);
        }

        private void StartFade()
        {
            if (_settings.GracePeriodSeconds <= 0)
            {
                lock (_lock) { _state = MachineState.Silent; }
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _speaker.Opacity = 0.0;
                    _speaker.State   = SpeakerState.Silent;
                });
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                // Re-check state on UI thread before starting timer
                lock (_lock)
                {
                    if (_state != MachineState.RecentlyActive) return;
                }

                _speaker.State = SpeakerState.RecentlyActive;

                double decrement = 1.0 / (_settings.GracePeriodSeconds / 0.033);

                _fadeTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _fadeTimer.Tick += OnFadeTick;
                _fadeTimer.Tag   = decrement;
                _fadeTimer.Start();
            });
        }

        private void OnFadeTick(object? sender, EventArgs e)
        {
            // Running on UI thread
            bool isStillFading;
            lock (_lock) { isStillFading = _state == MachineState.RecentlyActive; }

            if (!isStillFading)
            {
                _fadeTimer?.Stop();
                _fadeTimer = null;
                return;
            }

            double decrement = _fadeTimer?.Tag is double d ? d : 0.033;
            _speaker.Opacity = Math.Max(0.0, _speaker.Opacity - decrement);

            if (_speaker.Opacity <= 0.0)
            {
                _fadeTimer?.Stop();
                _fadeTimer = null;
                lock (_lock) { _state = MachineState.Silent; }
                _speaker.State = SpeakerState.Silent;
            }
        }
    }
}
