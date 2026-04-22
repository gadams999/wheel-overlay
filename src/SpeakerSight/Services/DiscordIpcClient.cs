using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenDash.OverlayCore.Services;

namespace OpenDash.SpeakerSight.Services;

// ── Event arg types ────────────────────────────────────────────────────────

public record SpeakingEventArgs(string UserId);

public record VoiceStateEventArgs(
    string UserId,
    string DisplayName,
    string? AvatarHash,
    bool IsMuted,
    bool IsDeafened);

public record VoiceStateDeletedArgs(string UserId);

public record ChannelSelectEventArgs(string? ChannelId, string? GuildId);

// ── Client ─────────────────────────────────────────────────────────────────

public sealed class DiscordIpcClient : IAsyncDisposable
{
    private const string ClientId = "1488518361783603352";

    private NamedPipeClientStream? _pipe;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    // nonce → TaskCompletionSource for awaiting command responses
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<JsonElement>>
        _pending = new();

    // guild ID → guild name, populated from the AUTHENTICATE response
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string>
        _guildNames = new();

    // Completed when Discord sends the READY dispatch after HANDSHAKE
    private TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // ── Public events ──────────────────────────────────────────────────────

    public event EventHandler<SpeakingEventArgs>?     SpeakingStart;
    public event EventHandler<SpeakingEventArgs>?     SpeakingStop;
    public event EventHandler<VoiceStateEventArgs>?   VoiceStateCreated;
    public event EventHandler<VoiceStateEventArgs>?   VoiceStateUpdated;
    public event EventHandler<VoiceStateDeletedArgs>? VoiceStateDeleted;
    public event EventHandler<ChannelSelectEventArgs>? VoiceChannelSelected;
    public event EventHandler<string>?                VoiceConnectionStatusChanged;
    public event EventHandler?                        AuthRevoked;
    public event EventHandler?                        ConnectionDropped;

    // ── Connection ─────────────────────────────────────────────────────────

    /// <summary>
    /// Probes pipe slots 0–9 and connects to the first available one.
    /// Then performs HANDSHAKE and awaits READY.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        for (int slot = 0; slot <= 9; slot++)
        {
            var pipe = new NamedPipeClientStream(".", $"discord-ipc-{slot}",
                PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(500);
                await pipe.ConnectAsync(cts.Token);
                _pipe = pipe;
                LogService.Info($"DiscordIpcClient: Connected on discord-ipc-{slot}.");
                break;
            }
            catch (OperationCanceledException)
            {
                pipe.Dispose();
            }
            catch (Exception ex)
            {
                pipe.Dispose();
                LogService.Info($"DiscordIpcClient: Slot {slot} unavailable: {ex.Message}");
            }
        }

        if (_pipe == null || !_pipe.IsConnected)
        {
            LogService.Info("DiscordIpcClient: No Discord IPC pipe found (slots 0–9) — Discord not running.");
            throw new DiscordNotRunningException();
        }

        // Start background read loop
        _readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token), _readCts.Token);

        await SendHandshake(ct);
    }

    // ── Auth commands ──────────────────────────────────────────────────────

    /// <summary>
    /// Sends AUTHORIZE with a PKCE code challenge and awaits the authorization code from Discord's consent dialog.
    /// </summary>
    public async Task<string> SendAuthorize(string codeChallenge, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            cmd   = "AUTHORIZE",
            args  = new
            {
                client_id             = ClientId,
                scopes                = new[] { "rpc", "rpc.voice.read", "identify", "guilds" },
                code_challenge        = codeChallenge,
                code_challenge_method = "S256",
            },
            nonce = nonce
        });

        var tcs = RegisterPending(nonce);
        await WriteFrameAsync(1, payload, ct);

        var response = await tcs.Task.WaitAsync(ct);
        return response.GetProperty("data").GetProperty("code").GetString()!;
    }

    /// <summary>
    /// Sends AUTHENTICATE with the given access token.
    /// Returns the set of scopes granted by Discord.
    /// Fires AuthRevoked if error code 4006 is received.
    /// </summary>
    public async Task<IReadOnlySet<string>> SendAuthenticate(string accessToken, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            cmd   = "AUTHENTICATE",
            args  = new { access_token = accessToken },
            nonce = nonce
        });

        var tcs = RegisterPending(nonce);
        await WriteFrameAsync(1, payload, ct);

        var response = await tcs.Task.WaitAsync(ct);

        var grantedScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // AUTHENTICATE response includes data.guilds and data.scopes
        try
        {
            if (response.TryGetProperty("data", out var data))
            {
                // Cache guild names
                if (data.TryGetProperty("guilds", out var guilds) &&
                    guilds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var g in guilds.EnumerateArray())
                    {
                        var id   = g.TryGetProperty("id",   out var idEl)   ? idEl.GetString()   : null;
                        var name = g.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                        if (id != null && name != null)
                            _guildNames[id] = name;
                    }
                    if (_guildNames.Count > 0)
                        LogService.Info($"DiscordIpcClient: cached {_guildNames.Count} guild name(s) from AUTHENTICATE response.");
                }

                // Collect granted scopes
                if (data.TryGetProperty("scopes", out var scopes) &&
                    scopes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in scopes.EnumerateArray())
                    {
                        var scope = s.GetString();
                        if (scope != null) grantedScopes.Add(scope);
                    }
                    LogService.Info($"DiscordIpcClient: granted scopes = [{string.Join(", ", grantedScopes)}]");
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error("DiscordIpcClient: Failed to parse AUTHENTICATE response.", ex);
        }

        return grantedScopes;
    }

    /// <summary>Returns the guild name for the given guild ID, or null if not cached.</summary>
    public string? GetGuildName(string? guildId) =>
        guildId != null && _guildNames.TryGetValue(guildId, out var name) ? name : null;

    /// <summary>
    /// Sends GET_GUILDS and caches the returned guild names.
    /// Requires the guilds scope; call after a successful AUTHENTICATE.
    /// </summary>
    public async Task FetchAndCacheGuildsAsync(CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            cmd   = "GET_GUILDS",
            args  = new { },
            nonce = nonce
        });

        var tcs = RegisterPending(nonce);
        await WriteFrameAsync(1, payload, ct);
        var response = await tcs.Task.WaitAsync(ct);

        try
        {
            if (response.TryGetProperty("data", out var data) &&
                data.TryGetProperty("guilds", out var guilds) &&
                guilds.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var g in guilds.EnumerateArray())
                {
                    var id   = g.TryGetProperty("id",   out var idEl)   ? idEl.GetString()   : null;
                    var name = g.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                    if (id != null && name != null)
                    {
                        _guildNames[id] = name;
                        count++;
                    }
                }
                LogService.Info($"DiscordIpcClient: cached {count} guild name(s) from GET_GUILDS.");
            }
        }
        catch (Exception ex)
        {
            LogService.Error("DiscordIpcClient: Failed to parse GET_GUILDS response.", ex);
        }
    }

    // ── Subscription ───────────────────────────────────────────────────────

    public async Task Subscribe(string evt, object? args = null, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            cmd   = "SUBSCRIBE",
            evt   = evt,
            args  = args ?? new { },
            nonce = nonce
        });

        var tcs = RegisterPending(nonce);
        await WriteFrameAsync(1, payload, ct);
        await tcs.Task.WaitAsync(ct);
    }

    public async Task Unsubscribe(string evt, object? args = null, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            cmd   = "UNSUBSCRIBE",
            evt   = evt,
            args  = args ?? new { },
            nonce = nonce
        });

        var tcs = RegisterPending(nonce);
        await WriteFrameAsync(1, payload, ct);
        await tcs.Task.WaitAsync(ct);
    }

    // ── State query ────────────────────────────────────────────────────────

    /// <summary>Returns full channel object or null if not in a channel.</summary>
    public async Task<JsonElement?> GetSelectedVoiceChannel(CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            cmd   = "GET_SELECTED_VOICE_CHANNEL",
            args  = new { },
            nonce = nonce
        });

        var tcs = RegisterPending(nonce);
        await WriteFrameAsync(1, payload, ct);
        var response = await tcs.Task.WaitAsync(ct);

        if (response.TryGetProperty("data", out var data) &&
            data.ValueKind != JsonValueKind.Null)
            return data;

        return null;
    }

    // ── Internals ──────────────────────────────────────────────────────────

    /// <summary>
    /// Waits until Discord sends the READY dispatch following a successful HANDSHAKE.
    /// Must be awaited before sending AUTHENTICATE or any other command.
    /// </summary>
    public Task WaitForReadyAsync(CancellationToken ct) =>
        _readyTcs.Task.WaitAsync(ct);

    private async Task SendHandshake(CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { v = 1, client_id = ClientId });
        await WriteFrameAsync(0, payload, ct);
        // READY dispatch arrives asynchronously in the read loop and sets _readyTcs
    }

    private async Task WriteFrameAsync(uint opcode, string json, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var frame = new byte[8 + body.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0, 4), opcode);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4, 4), (uint)body.Length);
        body.CopyTo(frame, 8);
        await _pipe!.WriteAsync(frame, ct);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            var header = new byte[8];
            while (!ct.IsCancellationRequested && _pipe!.IsConnected)
            {
                // Read 8-byte header
                int read = 0;
                while (read < 8)
                {
                    int n = await _pipe.ReadAsync(header.AsMemory(read, 8 - read), ct);
                    if (n == 0) { FireConnectionDropped(); return; }
                    read += n;
                }

                uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
                uint length = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4));

                // Read payload
                var body = new byte[length];
                int bodyRead = 0;
                while (bodyRead < (int)length)
                {
                    int n = await _pipe.ReadAsync(body.AsMemory(bodyRead, (int)length - bodyRead), ct);
                    if (n == 0) { FireConnectionDropped(); return; }
                    bodyRead += n;
                }

                DispatchFrame(opcode, body);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (IOException)               { FireConnectionDropped(); }
        catch (Exception ex)
        {
            LogService.Error("DiscordIpcClient: Read loop error.", ex);
            FireConnectionDropped();
        }
    }

    private void DispatchFrame(uint opcode, byte[] body)
    {
        switch (opcode)
        {
            case 0: // HANDSHAKE — ignore (we sent it)
                break;

            case 1: // FRAME
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    RouteFrame(doc.RootElement.Clone());
                }
                catch (Exception ex)
                {
                    LogService.Error("DiscordIpcClient: Failed to parse frame JSON.", ex);
                }
                break;

            case 2: // CLOSE
                LogService.Info("DiscordIpcClient: Received CLOSE from Discord.");
                FireConnectionDropped();
                break;

            case 3: // PING — reply with PONG
                _ = WriteFrameAsync(4, "{}", CancellationToken.None);
                break;

            case 4: // PONG — ignore
                break;

            default:
                LogService.Info($"DiscordIpcClient: Unknown opcode {opcode}.");
                break;
        }
    }

    private void RouteFrame(JsonElement frame)
    {
        var cmd = frame.TryGetProperty("cmd", out var cmdEl) ? cmdEl.GetString() : null;
        var evt = frame.TryGetProperty("evt", out var evtEl) ? evtEl.GetString() : null;
        var nonce = frame.TryGetProperty("nonce", out var nonceEl) ? nonceEl.GetString() : null;

        // Error frame — check for auth revocation
        if (evt == "ERROR")
        {
            if (frame.TryGetProperty("data", out var errData) &&
                errData.TryGetProperty("code", out var codeEl) &&
                codeEl.GetInt32() == 4006)
            {
                LogService.Info("DiscordIpcClient: Auth revoked (4006).");
                Task.Run(() => AuthRevoked?.Invoke(this, EventArgs.Empty));
            }
            // Resolve pending nonce if any
            if (nonce != null && _pending.TryRemove(nonce, out var errTcs))
                errTcs.TrySetException(new InvalidOperationException($"Discord error frame (evt=ERROR)."));
            return;
        }

        // Resolve pending nonce-matched command response
        if (nonce != null && _pending.TryRemove(nonce, out var tcs))
        {
            tcs.TrySetResult(frame);
        }

        // Route DISPATCH events
        if (cmd == "DISPATCH" && evt != null)
        {
            var data = frame.TryGetProperty("data", out var dataEl) ? dataEl : default;
            RouteDispatch(evt, data);
        }
    }

    private void RouteDispatch(string evt, JsonElement data)
    {
        try
        {
            switch (evt)
            {
                case "READY":
                {
                    LogService.Info("DiscordIpcClient: READY received.");
                    _readyTcs.TrySetResult();
                    break;
                }

                case "SPEAKING_START":
                {
                    var userId = data.GetProperty("user_id").GetString()!;
                    Task.Run(() => SpeakingStart?.Invoke(this, new SpeakingEventArgs(userId)));
                    break;
                }
                case "SPEAKING_STOP":
                {
                    var userId = data.GetProperty("user_id").GetString()!;
                    Task.Run(() => SpeakingStop?.Invoke(this, new SpeakingEventArgs(userId)));
                    break;
                }
                case "VOICE_STATE_CREATE":
                {
                    var args = ParseVoiceStateArgs(data);
                    Task.Run(() => VoiceStateCreated?.Invoke(this, args));
                    break;
                }
                case "VOICE_STATE_UPDATE":
                {
                    var args = ParseVoiceStateArgs(data);
                    Task.Run(() => VoiceStateUpdated?.Invoke(this, args));
                    break;
                }
                case "VOICE_STATE_DELETE":
                {
                    var userId = data.GetProperty("user").GetProperty("id").GetString()!;
                    Task.Run(() => VoiceStateDeleted?.Invoke(this, new VoiceStateDeletedArgs(userId)));
                    break;
                }
                case "VOICE_CHANNEL_SELECT":
                {
                    string? channelId = null, guildId = null;
                    if (data.TryGetProperty("channel_id", out var cId) && cId.ValueKind != JsonValueKind.Null)
                        channelId = cId.GetString();
                    if (data.TryGetProperty("guild_id", out var gId) && gId.ValueKind != JsonValueKind.Null)
                        guildId = gId.GetString();
                    Task.Run(() => VoiceChannelSelected?.Invoke(this, new ChannelSelectEventArgs(channelId, guildId)));
                    break;
                }
                case "VOICE_CONNECTION_STATUS":
                {
                    var state = data.TryGetProperty("state", out var s) ? s.GetString() ?? "?" : "?";
                    Task.Run(() => VoiceConnectionStatusChanged?.Invoke(this, state));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"DiscordIpcClient: Error routing DISPATCH evt={evt}.", ex);
        }
    }

    private static VoiceStateEventArgs ParseVoiceStateArgs(JsonElement data)
    {
        var user = data.GetProperty("user");
        var userId = user.GetProperty("id").GetString()!;
        string? avatarHash = null;
        if (user.TryGetProperty("avatar", out var av) && av.ValueKind != JsonValueKind.Null)
            avatarHash = av.GetString();

        // Display name: nick ?? username
        string displayName = userId;
        if (data.TryGetProperty("nick", out var nickEl) && nickEl.ValueKind != JsonValueKind.Null)
            displayName = nickEl.GetString() ?? userId;
        else if (user.TryGetProperty("username", out var unEl))
            displayName = unEl.GetString() ?? userId;

        bool isMuted = false, isDeafened = false;
        if (data.TryGetProperty("voice_state", out var vs))
        {
            if (vs.TryGetProperty("mute", out var m)) isMuted = m.GetBoolean();
            else if (vs.TryGetProperty("self_mute", out var sm)) isMuted = sm.GetBoolean();
            if (vs.TryGetProperty("deaf", out var d)) isDeafened = d.GetBoolean();
            else if (vs.TryGetProperty("self_deaf", out var sd)) isDeafened = sd.GetBoolean();
        }

        return new VoiceStateEventArgs(userId, displayName, avatarHash, isMuted, isDeafened);
    }

    private void FireConnectionDropped()
    {
        // Cancel all pending awaiters
        foreach (var kv in _pending)
            kv.Value.TrySetCanceled();
        _pending.Clear();

        Task.Run(() => ConnectionDropped?.Invoke(this, EventArgs.Empty));
    }

    private TaskCompletionSource<JsonElement> RegisterPending(string nonce)
    {
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[nonce] = tcs;
        return tcs;
    }

    // ── Reset ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tears down the current connection and resets all internal state so that
    /// <see cref="ConnectAsync"/> can be called again on this same instance.
    /// Any in-flight pending commands are cancelled.
    /// </summary>
    public async Task ResetForReconnectAsync()
    {
        _readCts?.Cancel();
        if (_readTask != null)
        {
            try { await _readTask; }
            catch (OperationCanceledException) { }
        }

        _pipe?.Dispose();
        _readCts?.Dispose();

        _pipe     = null;
        _readCts  = null;
        _readTask = null;
        _readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        foreach (var kv in _pending)
            kv.Value.TrySetCanceled();
        _pending.Clear();

        _guildNames.Clear();
    }

    // ── IAsyncDisposable ───────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _readCts?.Cancel();
        if (_readTask != null)
        {
            try { await _readTask; }
            catch (OperationCanceledException) { }
        }
        _pipe?.Dispose();
        _readCts?.Dispose();
    }
}
