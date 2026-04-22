# Contract: Discord IPC Event Contracts

**Owned by**: `DiscordIpcClient.cs` + `VoiceSessionService.cs` (`OpenDash.SpeakerSight.Services`)

These are the Discord IPC events that `SpeakerSight` subscribes to and the commands it sends. This document serves as the integration contract between `DiscordIpcClient` (transport) and `VoiceSessionService` (consumer).

---

## Frame Format

Every IPC frame is a single atomic write:

```
[opcode: uint32 LE][length: uint32 LE][JSON payload: UTF-8 bytes]
```

| Opcode | Name | Direction |
|--------|------|-----------|
| 0 | HANDSHAKE | Client → Discord |
| 1 | FRAME | Bidirectional |
| 2 | CLOSE | Discord → Client |
| 3 | PING | Discord → Client |
| 4 | PONG | Client → Discord |

---

## Connection Sequence

### 1. HANDSHAKE (opcode 0)
```json
{ "v": 1, "client_id": "<bundled_client_id>" }
```
Discord replies with opcode-1 FRAME / `DISPATCH` `READY`.

### 2. AUTHENTICATE (opcode 1) — subsequent launches
```json
{
  "cmd": "AUTHENTICATE",
  "args": { "access_token": "<stored_access_token>" },
  "nonce": "<uuid>"
}
```
Success response contains `data.user` and `data.scopes`.
Failure response (`evt: "ERROR"`, `data.code: 4006`) → transition to `Failed` state.

### 3. AUTHORIZE (opcode 1) — first launch or after revocation
```json
{
  "cmd": "AUTHORIZE",
  "args": {
    "client_id": "<bundled_client_id>",
    "scopes": ["rpc", "rpc.voice.read", "identify"]
  },
  "nonce": "<uuid>"
}
```
Discord displays in-app consent dialog. Response contains `data.code` (short-lived authorization code).
Follow with PKCE token exchange (HTTPS POST to `discord.com/api/oauth2/token`) to get `access_token` + `refresh_token`.

### 4. GET_SELECTED_VOICE_CHANNEL (opcode 1) — bootstrap after AUTHENTICATE
```json
{ "cmd": "GET_SELECTED_VOICE_CHANNEL", "args": {}, "nonce": "<uuid>" }
```
Response: full channel object with `id`, `name`, `guild_id`, and `voice_states[]` (all current participants), or `data: null` if not in a channel.

---

## Subscriptions

All SUBSCRIBE commands use opcode 1. Send sequentially (wait for echo before next).

### Global (subscribe once after AUTHENTICATE)

```json
{ "cmd": "SUBSCRIBE", "evt": "VOICE_CHANNEL_SELECT",   "args": {},               "nonce": "<uuid>" }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_CONNECTION_STATUS", "args": {},               "nonce": "<uuid>" }
```

### Channel-scoped (re-subscribe on each VOICE_CHANNEL_SELECT)

```json
{ "cmd": "SUBSCRIBE", "evt": "SPEAKING_START",     "args": { "channel_id": "<id>" }, "nonce": "<uuid>" }
{ "cmd": "SUBSCRIBE", "evt": "SPEAKING_STOP",      "args": { "channel_id": "<id>" }, "nonce": "<uuid>" }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_STATE_CREATE", "args": { "channel_id": "<id>" }, "nonce": "<uuid>" }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_STATE_UPDATE", "args": { "channel_id": "<id>" }, "nonce": "<uuid>" }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_STATE_DELETE", "args": { "channel_id": "<id>" }, "nonce": "<uuid>" }
```

Previous channel-scoped subscriptions are implicitly dropped when the pipe is closed (subscriptions are per-connection). On reconnect, re-subscribe after AUTHENTICATE.

---

## Inbound Events (DISPATCH)

### `SPEAKING_START`
```json
{
  "cmd": "DISPATCH",
  "evt": "SPEAKING_START",
  "data": { "user_id": "190320984123768832" }
}
```
**`VoiceSessionService` action**: Start or reset the debounce timer for `user_id`. If `DebounceThresholdMs = 0`, immediately transition to `Active`.

### `SPEAKING_STOP`
```json
{
  "cmd": "DISPATCH",
  "evt": "SPEAKING_STOP",
  "data": { "user_id": "190320984123768832" }
}
```
**`VoiceSessionService` action**: Cancel debounce timer (if running). If participant was `Active`, start grace period timer → `RecentlyActive`. If still in `Debouncing`, return to `Idle` (no visible change).

### `VOICE_CHANNEL_SELECT`
```json
{
  "cmd": "DISPATCH",
  "evt": "VOICE_CHANNEL_SELECT",
  "data": {
    "channel_id": "987654321098765432",   // null if left all channels
    "guild_id": "123456789012345678"
  }
}
```
**`VoiceSessionService` action**:
- Clear all participant state and timers
- If `channel_id` is `null`: hide overlay (no voice channel)
- If `channel_id` non-null: call `GET_SELECTED_VOICE_CHANNEL` to seed state, then re-subscribe channel-scoped events
- `AliasService`: ensure `ChannelContext` record exists for (`guild_id`, `channel_id`)

### `VOICE_STATE_CREATE`
```json
{
  "cmd": "DISPATCH",
  "evt": "VOICE_STATE_CREATE",
  "data": {
    "voice_state": { "mute": false, "deaf": false, "self_mute": false, "self_deaf": false, "suppress": false },
    "user": { "id": "111222333444555666", "username": "NewMember", "avatar": "hash_or_null" },
    "nick": "Server Nick or null"
  }
}
```
**`VoiceSessionService` action**: Add `ParticipantSnapshot` to `VoiceSession.Participants`. `AliasService`: add or update `ChannelMember` entry (update `LastKnownName` if changed).

### `VOICE_STATE_UPDATE`
Same shape as `VOICE_STATE_CREATE`.
**`VoiceSessionService` action**: Replace existing `ParticipantSnapshot`. Update `AliasService` `LastKnownName` if changed.

### `VOICE_STATE_DELETE`
```json
{
  "cmd": "DISPATCH",
  "evt": "VOICE_STATE_DELETE",
  "data": {
    "voice_state": {},
    "user": { "id": "111222333444555666" },
    "nick": null
  }
}
```
**`VoiceSessionService` action**: Remove participant from `VoiceSession.Participants`; cancel any active debounce/grace timers for that `user_id`.

### `VOICE_CONNECTION_STATUS`
```json
{
  "cmd": "DISPATCH",
  "evt": "VOICE_CONNECTION_STATUS",
  "data": {
    "state": "VOICE_CONNECTED",   // or "VOICE_DISCONNECTED", "VOICE_CONNECTING", etc.
    "hostname": "...",
    "pings": []
  }
}
```
**`VoiceSessionService` action**: Informational; log at Debug level. Not used to drive `ConnectionState` (the IPC pipe state drives `ConnectionState`).

### Error Frame
```json
{
  "cmd": "DISPATCH",
  "evt": "ERROR",
  "data": { "code": 4006, "message": "Invalid secret" }
}
```
**`DiscordIpcClient` action**: Emit `AuthRevoked` event → `VoiceSessionService` transitions `ConnectionState` to `Failed`, stops reconnect loop.

---

## C# Event Interface (`DiscordIpcClient` → `VoiceSessionService`)

```csharp
// Events raised by DiscordIpcClient on the ThreadPool
event EventHandler<SpeakingEventArgs>       SpeakingStart;
event EventHandler<SpeakingEventArgs>       SpeakingStop;
event EventHandler<VoiceStateEventArgs>     VoiceStateCreated;
event EventHandler<VoiceStateEventArgs>     VoiceStateUpdated;
event EventHandler<VoiceStateDeletedArgs>   VoiceStateDeleted;
event EventHandler<ChannelSelectEventArgs>  VoiceChannelSelected;
event EventHandler                          AuthRevoked;
event EventHandler                          ConnectionDropped;

// SpeakingEventArgs
public record SpeakingEventArgs(string UserId);

// VoiceStateEventArgs
public record VoiceStateEventArgs(
    string UserId,
    string DisplayName,   // nick ?? username
    string? AvatarHash,   // global avatar hash (user.avatar)
    string? GuildAvatarHash, // guild-specific avatar hash (member.avatar); preferred over AvatarHash
    string? GuildId,      // required to construct guild avatar CDN URL
    bool IsMuted,
    bool IsDeafened);

// VoiceStateDeletedArgs
public record VoiceStateDeletedArgs(string UserId);

// ChannelSelectEventArgs
public record ChannelSelectEventArgs(string? ChannelId, string? GuildId);
```

All event handlers are marshalled to the WPF Dispatcher by `VoiceSessionService` before mutating `ObservableCollection` state.
