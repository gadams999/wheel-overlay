# Implementation Plan: SpeakerSight

**Branch**: `speakersight/v0.1.0` | **Date**: 2026-03-31 | **Spec**: `specs/004-speakersight/`
**Input**: Feature specification from `specs/004-speakersight/spec.md`

## Summary

Build `SpeakerSight` v0.1.0 — a WPF always-on-top overlay that connects to the local Discord client via the Discord IPC named-pipe protocol, authenticates via OAuth2 (PKCE), subscribes to `SPEAKING_START`/`SPEAKING_STOP` voice events, and displays active speakers on a click-through overlay window. Mirrors the WheelOverlay monorepo pattern: OverlayCore `ProjectReference`, `ISettingsCategory`-based settings, WiX 4 MSI installer, FsCheck property tests, and MkDocs documentation.

## Technical Context

**Language/Version**: C# 12 / .NET 10.0-windows
**Primary Dependencies**:
- `OverlayCore` (ProjectReference) — LogService, ThemeService, WindowTransparencyHelper, BaseOverlayWindow, MaterialSettingsWindow, ISettingsCategory
- `Meziantou.Framework.Win32.CredentialManager` v1.7.17 — Windows Credential Manager (DPAPI-encrypted token storage)
- `System.IO.Pipes` (BCL) — `NamedPipeClientStream` for Discord IPC
- `System.Text.Json` (BCL) — IPC frame serialization
- `MaterialDesignThemes` v5.3.1 (via OverlayCore) — settings window UI

**Storage**:
- `%APPDATA%\SpeakerSight\settings.json` — overlay preferences (position, opacity, theme, display mode, debounce, grace period, font size)
- `%APPDATA%\SpeakerSight\aliases.json` — ChannelContext + ChannelMember records
- Windows Credential Manager target `"SpeakerSight"` — OAuth2 token bundle (access_token, refresh_token, expiry_utc as JSON)

**Testing**: xUnit 2.x + FsCheck 2.16.6 / FsCheck.Xunit; Xunit.StaFact for WPF STA tests
**Target Platform**: Windows 10/11 desktop (net10.0-windows), WPF + WinForms (NotifyIcon)
**Project Type**: Desktop overlay application
**Performance Goals**: <500 ms speaker appearance latency (SC-002); <2% CPU / <100 MB RAM steady-state (SC-007)
**Constraints**: Click-through by default (FR-011); always-on-top; borderless windowed games only (FR-011a); 8 most-recently-activated speakers displayed, remainder in `+N more` count (FR-006)
**Scale/Scope**: Single voice channel, single guild, single machine; up to 8 simultaneous display entries

**Discord IPC**:
- Named pipe `\\.\pipe\discord-ipc-{0–9}` — probe slots in order; first success wins
- Binary frame: `[opcode: uint32 LE][length: uint32 LE][JSON payload: UTF-8]` — must be written atomically
- OAuth2 scopes: `rpc`, `rpc.voice.read`, `identify` — **requires Discord approval for `rpc` and `rpc.voice.read`** (private beta; 50-slot developer tester whitelist available for dev/test)
- Token exchange uses PKCE + `PUBLIC_OAUTH2_CLIENT` flag (no `client_secret` in binary)
- Events subscribed: `SPEAKING_START`, `SPEAKING_STOP`, `VOICE_STATE_CREATE`, `VOICE_STATE_UPDATE`, `VOICE_STATE_DELETE`, `VOICE_CONNECTION_STATUS`, `VOICE_CHANNEL_SELECT`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Requirement | Status |
|-----------|-------------|--------|
| I — Monorepo / Shared Core | `src/SpeakerSight/` + `ProjectReference` to OverlayCore; namespace `OpenDash.SpeakerSight`; no `<Version>` on OverlayCore | ✅ PASS |
| II — Test-First / PBT | FsCheck property tests for all correctness properties; `#if FAST_TESTS` / `#else` guards; `// Feature: ..., Property N: ...` comments | ✅ PASS |
| III — Per-App Versioning | `<Version>0.1.0</Version>` in `SpeakerSight.csproj`; branch `speakersight/v0.1.0`; version bumped as first commit | ✅ PASS |
| IV — Changelog | `CHANGELOG.md` `[Unreleased]` entry updated before merge | ✅ PASS |
| V — Observability | `LogService.Initialize("SpeakerSight")` first; all failures caught, logged, gracefully degraded | ✅ PASS |
| VI — Branch / Commits | Branch `speakersight/v0.1.0` matches PRIMARY format; spec folder `004-speakersight` permanent | ✅ PASS |
| VII — Documentation | `docs/speakersight/` added; `mkdocs.yml` nav updated; GitHub Actions deploy-docs workflow triggers on push | ✅ PASS |

**No violations.** Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/004-speakersight/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   ├── settings-schema.md      ← Phase 1 output
│   ├── aliases-schema.md       ← Phase 1 output
│   └── ipc-event-contracts.md  ← Phase 1 output
└── tasks.md             ← Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
src/SpeakerSight/
├── SpeakerSight.csproj    ← net10.0-windows, Version 0.1.0, refs OverlayCore + Meziantou CredMgr
├── Program.cs                   ← single-instance Mutex, LogService.Initialize, top-level exception handler
├── App.xaml / App.xaml.cs       ← app lifecycle, NotifyIcon tray, ThemeService, settings window, shutdown
├── MainWindow.xaml / .cs        ← always-on-top click-through overlay, hosts SpeakerPanel
├── Converters/
│   ├── AvatarUrlConverter.cs                ← IMultiValueConverter: [GuildId, UserId, GuildAvatarHash, AvatarHash] → CDN URL or null
│   └── BoolToVisibilityConverter.cs         ← true→Visible, false→Hidden (preserves column width)
├── Models/
│   ├── AppSettings.cs           ← JSON-serializable preferences; Load()/Save() to settings.json
│   ├── ChannelContext.cs        ← guild+channel record; list of ChannelMember
│   ├── ChannelMember.cs         ← snowflake ID, last-known name, custom name, avatar toggle
│   ├── VoiceSession.cs          ← current channel name, guild name, participants, connection state
│   ├── ActiveSpeaker.cs         ← display name, SpeakerState enum, debounce/grace timers
│   ├── SpeakerState.cs          ← enum: Active | RecentlyActive | Silent
│   └── ConnectionState.cs       ← enum: Disconnected | Connecting | Connected | Retrying | Failed
├── Services/
│   ├── DiscordIpcClient.cs      ← named-pipe transport, opcode framing, HANDSHAKE/AUTHORIZE/AUTHENTICATE
│   ├── VoiceSessionService.cs   ← SUBSCRIBE management, speaker state machine, debounce, grace period
│   ├── TokenStorageService.cs   ← Credential Manager read/write/delete; PKCE code verifier generation
│   └── AliasService.cs          ← aliases.json load/save; ChannelContext CRUD; name resolution
├── Settings/
│   ├── ConnectionSettingsCategory.cs  ← auth status, re-authorize button, disconnect
│   ├── DisplaySettingsCategory.cs     ← display mode toggle, grace period, debounce threshold
│   ├── AppearanceSettingsCategory.cs  ← position, opacity, theme, font size
│   ├── AliasSettingsCategory.cs       ← ChannelContext list, ChannelMember custom names, delete context
│   └── AboutSettingsCategory.cs       ← version, links
└── ViewModels/
    ├── OverlayViewModel.cs      ← INotifyPropertyChanged; ActiveSpeakers list; connection indicator
    └── SettingsViewModel.cs     ← coordinates category save/load

tests/SpeakerSight.Tests/
├── SpeakerSight.Tests.csproj    ← net10.0-windows, xUnit, FsCheck.Xunit, Xunit.StaFact
├── Infrastructure/
│   ├── UITestBase.cs
│   └── TestConfiguration.cs
├── Services/
│   ├── VoiceSessionServiceTests.cs    ← debounce, grace period, state machine (property tests)
│   └── AliasServiceTests.cs           ← name resolution, JSON round-trip, malformed-entry handling
├── Models/
│   ├── AppSettingsTests.cs            ← serialization round-trip, defaults (property tests)
│   └── ChannelMemberTests.cs          ← snowflake key invariants (property tests)
└── ViewModels/
    └── OverlayViewModelTests.cs       ← speaker cap (8), +N more indicator

installers/speakersight/
├── Package.wxs          ← WiX 4; unique UpgradeCode; output: SpeakerSight-v0.1.0.msi
└── CustomUI.wxs

scripts/speakersight/
├── build_msi.ps1
├── build_release.ps1
└── generate_components.ps1

docs/speakersight/
├── index.md             ← overview, feature summary
├── getting-started.md   ← install, first auth, quick tour
├── settings.md          ← all settings fields explained
└── troubleshooting.md   ← common errors, reconnect behaviour

assets/speakersight/
└── app.ico

.github/workflows/
└── speakersight-release.yml   ← tag: speakersight/v*
```

**Structure Decision**: Single-project layout (Option 1 variant). No frontend/backend split; no multi-app service boundary. All Discord IPC logic lives in `Services/DiscordIpcClient` — not a separate project — because the IPC client is app-specific and not shared with OverlayCore.

## Phase 0: Research Findings

See `research.md` for full findings. Key resolved decisions:

| Topic | Decision |
|-------|----------|
| IPC transport | `NamedPipeClientStream`, probe slots `discord-ipc-0` through `discord-ipc-9` |
| RPC library | Custom `DiscordIpcClient` (no NuGet) — no available .NET package covers AUTHORIZE + AUTHENTICATE + voice subscriptions |
| OAuth2 scopes | `rpc`, `rpc.voice.read`, `identify` — **Discord approval gate** for `rpc.voice.read`; 50-slot tester whitelist covers dev/test |
| Client secret handling | PKCE + `PUBLIC_OAUTH2_CLIENT` flag — no `client_secret` in binary |
| Token storage | `Meziantou.Framework.Win32.CredentialManager` v1.7.17 — DPAPI-encrypted, `LocalMachine` persistence |
| Reconnect strategy | Custom exponential backoff with full jitter; cap 64 s; immediate first retry (satisfies SC-006 10 s target) |
| Speaking events | `SPEAKING_START` / `SPEAKING_STOP` dispatched with `user_id`; `VOICE_STATE_*` for member roster |

## Phase 1: Design Artifacts

See `data-model.md`, `contracts/`, `quickstart.md`.

### Key Design Decisions

**Speaker state machine** (per-participant):

```
[no activity] ──voice_start + debounce elapsed──► [Active]
[Active]       ──voice_stop──────────────────────► [RecentlyActive] (grace timer starts)
[RecentlyActive] ──voice_start──────────────────► [Active] (grace timer cancelled)
[RecentlyActive] ──grace timer expires──────────► [Silent / removed]
[any] ──debounce spike < threshold──────────────► no state change (timer resets)
```

**Overlay layout** (FR-014b-layout, FR-004c, FR-016, FR-016a):
- Fixed two-column WPF `Grid`: avatar `Image` column (32 px fixed width; guild avatar preferred, global fallback; CDN async load via `AvatarUrlConverter`; fails silently to blank) + name column (fill)
- Row `Opacity` bound to `ActiveSpeaker.Opacity` (drives smooth 1.0→0.0 fade over grace period)
- `ItemsControl` bound to `OverlayViewModel.ActiveSpeakers` (max 8 items); order: Active (Opacity 1.0) first → RecentlyActive (fading) → Silent (all-members mode only)
- 9th+ active/recently-active speakers: `+N more` `TextBlock` row below the list
- Connection indicator row (top): hidden when Connected; shows "⟳ Reconnecting…" (Retrying) or "✕ Disconnected" (Failed)
- **Overlay dimensions** (FR-016): computed once on startup and on every `FontSize` change. Width = avatar column (32 px) + column gap + name column width, where name column width = 32 × `FormattedText.WidthIncludingTrailingWhitespace` of the character `"W"` at the configured `FontSize` using the overlay's `FontFamily`. Height = 8 × (single-row height at `FontSize` + inter-row spacing). `MainWindow` exposes `RecomputeDimensions(fontSize)` called from `AppearanceSettingsCategory` on slider change and on startup.
- **Preview mode** (FR-016a): `OverlayViewModel` exposes `bool IsPreviewMode`. When `true`, `ActiveSpeakers` is populated with 8 stub `ActiveSpeaker` entries ("Speaker 1"–"Speaker 8"; first 5 in `Active` state at `Opacity=1.0`, last 3 in `RecentlyActive` state at descending opacity 0.7/0.5/0.3) instead of live data. `VoiceSessionService` events are ignored while preview mode is active. `IsPreviewMode` is set to `true` when the settings window opens (in `App.ShowSettings()`) and back to `false` when it closes. Overlay position remains live during preview — position-drag in `AppearanceSettingsCategory` moves the window in real time as per US3 AC1.

**Debounce + grace period** (both in `VoiceSessionService`):
- Leading edge: `System.Threading.Timer` per participant; voice activity starts timer, stop before threshold resets it
- Trailing edge: separate `System.Threading.Timer` per participant; starts on voice stop, cancelled on voice start

**Alias resolution** (FR-014b):
```
Resolve(userId, channelContextId) →
  member = AliasService.Find(channelContextId, userId)
  displayName = member?.CustomDisplayName ?? member?.LastKnownName ?? rawDiscordName
  showAvatar  = member?.AvatarVisible ?? true
```

**Settings persistence** — two files, both in `%APPDATA%\SpeakerSight\`:
- `settings.json` — `AppSettings` (System.Text.Json, round-trip property tests)
- `aliases.json` — `List<ChannelContext>` (System.Text.Json, malformed-entry skip+log)

**IPC reconnect lifecycle**:
1. App start → `DiscordIpcClient.ConnectAsync()` (probes slots 0–9)
2. HANDSHAKE → READY
3. Read token from Credential Manager → AUTHENTICATE (or AUTHORIZE if no token / token expired without valid refresh)
4. SUBSCRIBE to `VOICE_CHANNEL_SELECT` + `VOICE_CONNECTION_STATUS`
5. `GET_SELECTED_VOICE_CHANNEL` → seed initial state
6. On voice channel join → SUBSCRIBE to channel-scoped events
7. On pipe drop → fire `ConnectionDropped` → `VoiceSessionService` clears state, `App` starts reconnect loop
8. On auth revoked (error 4006) → transition to `Failed` state, stop retry loop, show re-auth prompt

## Property Tests (FsCheck — required by Constitution II)

| Property | Test Class | `FAST_TESTS` | `Release` |
|----------|-----------|-------------|-----------|
| `AppSettings` serialization round-trip preserves all fields | `AppSettingsTests` | 10 | 100 |
| `AppSettings` defaults satisfy all range constraints (GracePeriodSeconds in [0.0,2.0]) | `AppSettingsTests` | 10 | 100 |
| `ChannelMember` with arbitrary Unicode custom name serializes and deserializes identically | `ChannelMemberTests` | 10 | 100 |
| `AliasService.Resolve` returns custom name when set, falls back to last-known name, then raw | `AliasServiceTests` | 10 | 100 |
| `AliasService` skips and logs malformed entries without throwing | `AliasServiceTests` | 10 | 100 |
| Debounce: events shorter than threshold produce no state transition | `VoiceSessionServiceTests` | 10 | 100 |
| Debounce: events ≥ threshold produce `Active` state within 500 ms | `VoiceSessionServiceTests` | 10 | 100 |
| Opacity fade monotone: `ActiveSpeaker.Opacity` strictly decreases each tick during grace period, never increases | `VoiceSessionServiceTests` | 10 | 100 |
| Grace period resumption: resuming speech transitions to `Active` and restores `Opacity` to 1.0 | `VoiceSessionServiceTests` | 10 | 100 |
| Speaker cap: `ActiveSpeakers` list never exceeds 8 items regardless of event count | `OverlayViewModelTests` | 10 | 100 |
| Out-of-bounds position correction always results in a position within connected monitor bounds | `AppSettingsTests` | 10 | 100 |

## Pre-Launch Gate

**Discord `rpc.voice.read` approval** is required before public release. Development and testing use Discord's 50-slot developer tester whitelist (added via the Developer Portal). This is not a build blocker but is a **distribution blocker** — the MSI can be built and tested privately before approval is granted.
