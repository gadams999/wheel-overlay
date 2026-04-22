# Data Model: SpeakerSight

**Date**: 2026-03-31 | **Branch**: `speakersight/v0.1.0`

Entities are grouped by persistence boundary. All C# types use nullable-enabled, `System.Text.Json` serialization unless noted.

---

## Persisted Entities

### `AppSettings` → `%APPDATA%\SpeakerSight\settings.json`

The single serialized root. `Load()` returns defaults on missing/corrupt file (logs the failure). `Save()` writes atomically via temp file + rename.

| Field | Type | Default | Constraint |
|-------|------|---------|------------|
| `WindowLeft` | `double` | 20.0 | Clamped to screen bounds on load (FR-012) |
| `WindowTop` | `double` | 20.0 | Clamped to screen bounds on load |
| `Opacity` | `int` | 90 | 10–100 inclusive |
| `ThemePreference` | `ThemePreference` enum | `System` | Dark / Light / System |
| `DisplayMode` | `DisplayMode` enum | `SpeakersOnly` | SpeakersOnly / AllMembers |
| `GracePeriodSeconds` | `double` | 2.0 | 0.0–2.0 inclusive |
| `DebounceThresholdMs` | `int` | 200 | 0–1000 inclusive; 0 = disabled |
| `FontSize` | `int` | 14 | 8–32 inclusive |
| `ShowOnStartup` | `bool` | `true` | — |

**Serialization**: `System.Text.Json` with `JsonStringEnumConverter`; case-insensitive property matching. Unknown properties ignored (forward-compat).

**Validation rules**:
- `Opacity` clamped to [10, 100] on deserialization — out-of-range values are corrected silently and logged
- `GracePeriodSeconds` clamped to [0.0, 2.0]
- `DebounceThresholdMs` clamped to [0, 1000]
- `FontSize` clamped to [8, 32]
- Position: if `(WindowLeft, WindowTop)` falls outside all connected monitors → reset to `(20.0, 20.0)` on primary screen

---

### `ChannelContext` + `ChannelMember` → `%APPDATA%\SpeakerSight\aliases.json`

Root is `List<ChannelContext>`. `AliasService` loads on startup; saves after any mutation. Malformed entries are skipped and logged without crashing (FR-014c).

#### `ChannelContext`

| Field | Type | Notes |
|-------|------|-------|
| `GuildId` | `string` | Discord snowflake ID (permanent key component) |
| `GuildName` | `string` | Cached display name; updated on each observation |
| `ChannelId` | `string` | Discord snowflake ID (permanent key component) |
| `ChannelName` | `string` | Cached display name; updated on each observation |
| `Members` | `List<ChannelMember>` | All observed members for this context |

**Key**: (`GuildId`, `ChannelId`) — both snowflake IDs. Created automatically when the user joins a voice channel for the first time (FR-014). Retained indefinitely unless explicitly deleted by the user via settings (FR-014d).

**Deletion rule**: Deleting a `ChannelContext` removes it and all its `Members` permanently. Requires a confirmation prompt in settings (FR-014d). If the user rejoins the same channel, a new `ChannelContext` is created from scratch.

#### `ChannelMember`

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `UserId` | `string` | — | Discord snowflake ID; **permanent key** (FR-014a) |
| `LastKnownName` | `string` | Discord `nick` or `username` | Auto-updated on each observation even if custom name is set |
| `CustomDisplayName` | `string?` | `null` | User-set; full Unicode including inline emoji; `null` = not set |
| `AvatarVisible` | `bool` | `true` | Per-entry avatar visibility toggle (FR-014b) |

**Key**: `UserId` (snowflake ID). Two members in the same channel with the same `LastKnownName` are still distinct entries.

**Name resolution logic** (FR-014b):
```
resolved = member.CustomDisplayName ?? member.LastKnownName ?? rawDiscordName
```

**Validation rules**:
- `UserId` must be non-empty and parseable as a 64-bit unsigned integer (snowflake); entries failing this are skipped and logged
- `LastKnownName` must be non-empty; entries with empty `LastKnownName` and null `CustomDisplayName` are skipped and logged
- `CustomDisplayName` — no length limit enforced by the model; the settings UI caps input at 100 characters

---

### `TokenBundle` → Windows Credential Manager target `"SpeakerSight"`

Not a file on disk — stored as a JSON string in the `Password` field of a `LocalMachine` Windows credential. Read/written only by `TokenStorageService`.

| Field | Type | Notes |
|-------|------|-------|
| `AccessToken` | `string` | OAuth2 access token; expires per `ExpiryUtc` |
| `RefreshToken` | `string` | Long-lived; invalidated on each use; replaced atomically |
| `ExpiryUtc` | `DateTimeOffset` | `DateTimeOffset.UtcNow + TimeSpan.FromSeconds(expires_in)` |

**Refresh rule**: If `ExpiryUtc - DateTimeOffset.UtcNow < 1 hour` at startup, silently refresh before AUTHENTICATE. On refresh, store the new `TokenBundle` atomically (old bundle remains valid until the write completes).

---

## Runtime-Only Entities (not persisted)

### `VoiceSession`

Represents the current Discord voice channel state. Rebuilt from scratch on every connect and on `VOICE_CHANNEL_SELECT`.

| Field | Type | Notes |
|-------|------|-------|
| `ChannelId` | `string?` | `null` when not in a voice channel |
| `ChannelName` | `string?` | Display name of the current channel |
| `GuildId` | `string?` | — |
| `GuildName` | `string?` | — |
| `Participants` | `Dictionary<string, ParticipantSnapshot>` | Keyed by `user_id`; all members currently in the channel |
| `ConnectionState` | `ConnectionState` | Current IPC state |

### `ParticipantSnapshot`

An immutable snapshot of a channel member at a point in time. Replaced (not mutated) on each `VOICE_STATE_UPDATE`.

| Field | Type | Notes |
|-------|------|-------|
| `UserId` | `string` | Snowflake ID |
| `DiscordDisplayName` | `string` | `nick` if set, else `username` |
| `AvatarHash` | `string?` | Global Discord avatar hash; used as fallback CDN URL |
| `GuildAvatarHash` | `string?` | Guild-specific avatar hash from `member.avatar` IPC field; preferred over `AvatarHash` when set |
| `IsMuted` | `bool` | Self-mute or server-mute |
| `IsDeafened` | `bool` | Self-deaf or server-deaf |

### `ActiveSpeaker`

Derived by `VoiceSessionService` from `ParticipantSnapshot` + alias resolution + current `SpeakerState`. Consumed by `OverlayViewModel`.

| Field | Type | Notes |
|-------|------|-------|
| `UserId` | `string` | Snowflake ID |
| `DisplayName` | `string` | Resolved name (custom → last-known → Discord) |
| `AvatarHash` | `string?` | Global avatar hash; fallback CDN source |
| `GuildAvatarHash` | `string?` | Guild-specific avatar hash; preferred CDN source |
| `GuildId` | `string?` | Required for guild avatar CDN URL construction |
| `AvatarVisible` | `bool` | From `ChannelMember.AvatarVisible`; default `true` |
| `State` | `SpeakerState` | Active / RecentlyActive / Silent |
| `Opacity` | `double` | Current display opacity 0.0–1.0; 1.0 when Active; animated 1.0→0.0 during RecentlyActive; managed by `VoiceSessionService` via `DispatcherTimer` |

### `SpeakerState` (enum)

| Value | Meaning | Overlay appearance |
|-------|---------|-------------------|
| `Active` | Currently speaking (debounce elapsed) | Opacity 1.0; accent color; rendered above RecentlyActive rows |
| `RecentlyActive` | Stopped speaking; grace period / fade running | Opacity animates 1.0→0.0 over `GracePeriodSeconds`; rendered below Active rows |
| `Silent` | Fade complete or never spoke | Opacity 0.0; removed from list (speakers-only) or rendered below fading members (all-members) |

### `ConnectionState` (enum)

| Value | Meaning | Tray icon | Overlay indicator |
|-------|---------|-----------|-------------------|
| `Disconnected` | Not yet connected | Default | Hidden |
| `Connecting` | Initial connection in progress | Default | Hidden |
| `Connected` | Authenticated + subscribed | Normal color | Hidden |
| `Retrying` | IPC dropped; exponential backoff | Amber | "⟳ Reconnecting…" |
| `Failed` | Auth revoked or unrecoverable | Red | "✕ Disconnected" |

---

## State Transitions

### Speaker State Machine (per `user_id`)

```
Trigger legend:
  voice_start  = SPEAKING_START event received for user_id
  voice_stop   = SPEAKING_STOP event received for user_id
  debounce_ok  = leading-edge timer fires (continuous activity ≥ DebounceThresholdMs)
  grace_exp    = trailing-edge timer fires (silence ≥ GracePeriodSeconds)

States:        [Idle] → [Debouncing] → [Active] → [RecentlyActive] → [Silent/removed]

[Idle]
  + voice_start (DebounceThresholdMs > 0) → start debounce timer → [Debouncing]
  + voice_start (DebounceThresholdMs = 0) → immediate → [Active]

[Debouncing]
  + debounce_ok  → [Active]
  + voice_stop   → cancel debounce timer → [Idle]   // spike shorter than threshold; no visible change

[Active]
  + voice_stop   → start grace timer → [RecentlyActive]

[RecentlyActive]
  + voice_start  → cancel grace timer → [Active]    // resume during grace period (FR-004b)
  + grace_exp    → [Silent]

[Silent]
  + voice_start  → [Debouncing] or [Active] (same as Idle)
  + member_leave (VOICE_STATE_DELETE) → removed from Participants
```

### Connection State Machine

```
[Disconnected] ──app start──────────────────────────► [Connecting]
[Connecting]   ──pipe open + AUTHENTICATE ok─────────► [Connected]
[Connecting]   ──pipe fail / auth fail (recoverable)─► [Retrying]
[Connected]    ──pipe drop───────────────────────────► [Retrying]
[Retrying]     ──reconnect + re-auth ok──────────────► [Connected]
[Retrying]     ──auth revoked (error 4006)───────────► [Failed]
[Failed]       ──user clicks Re-authorize────────────► [Connecting]
[any]          ──app exit───────────────────────────── terminate cleanly
```

---

## Overlay View Model: Derived Collections

`OverlayViewModel` derives two observable collections from `VoiceSessionService`:

### `ActiveSpeakers: ObservableCollection<ActiveSpeaker>`

- **speakers-only mode**: members with `State = Active` or `State = RecentlyActive`, ordered by most-recently-activated
- **all-members mode**: all `Participants`, `Active`/`RecentlyActive` first, then `Silent`, alphabetical within each group
- **Cap**: maximum 8 items (FR-006). If `Active+RecentlyActive > 8`, overflow members are excluded.

### `OverflowCount: int`

- Total speakers beyond the 8-item cap. `0` when no overflow. Displayed as `+N more` below the list.

### `ConnectionIndicator: string?`

- `null` when `ConnectionState = Connected` (indicator hidden)
- `"⟳ Reconnecting…"` when `Retrying`
- `"✕ Disconnected"` when `Failed`

---

## Relationships Diagram (text)

```
AppSettings (1) ────────────────────────────── settings.json
TokenBundle (0..1) ─────────────────────────── Windows Credential Manager

ChannelContext (0..N) ──────────────────────── aliases.json
  └── ChannelMember (0..N, keyed by UserId)

VoiceSession (runtime, 0..1)
  └── ParticipantSnapshot (0..N, keyed by UserId)
        └── → ActiveSpeaker (resolved, 0..8 displayed)
                  ├── DisplayName  ← AliasService.Resolve(UserId, ChannelContextId)
                  └── AvatarVisible ← ChannelMember.AvatarVisible

OverlayViewModel
  ├── ActiveSpeakers  (max 8)
  ├── OverflowCount
  └── ConnectionIndicator
```
