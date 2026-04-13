# Tasks: Discord Chat Overlay

**Input**: Design documents from `specs/004-discord-chat-overlay/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Property tests are included — required by Constitution Principle II (FsCheck / `// Feature: ..., Property N: ...` directive mandatory).

**Organization**: Tasks grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US6)
- Exact file paths are included in every description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project scaffold — directories, project files, solution registration, CI workflow

- [x] T001 Create directory structure: `src/DiscordChatOverlay/`, `tests/DiscordChatOverlay.Tests/Infrastructure/`, `tests/DiscordChatOverlay.Tests/Models/`, `tests/DiscordChatOverlay.Tests/Services/`, `tests/DiscordChatOverlay.Tests/ViewModels/`, `installers/discord-chat-overlay/`, `scripts/discord-chat-overlay/`, `docs/discord-chat-overlay/`, `assets/discord-chat-overlay/`
- [x] T002 Create `src/DiscordChatOverlay/DiscordChatOverlay.csproj` targeting `net10.0-windows`, `<Version>0.1.0</Version>`, `<UseWPF>true</UseWPF>`, `<UseWindowsForms>true</UseWindowsForms>`, `ProjectReference` to `../../src/OverlayCore/OverlayCore.csproj`, `PackageReference` for `Meziantou.Framework.Win32.CredentialManager` v1.7.17; enable nullable and implicit usings; namespace root `OpenDash.DiscordChatOverlay`
- [x] T003 [P] Create `tests/DiscordChatOverlay.Tests/DiscordChatOverlay.Tests.csproj` targeting `net10.0-windows`, `<UseWPF>true</UseWPF>`, `PackageReference` for xUnit 2.x, `FsCheck` 2.16.6, `FsCheck.Xunit`, `Xunit.StaFact`; `ProjectReference` to `DiscordChatOverlay.csproj`; `FastTests` configuration with `<DefineConstants>FAST_TESTS</DefineConstants>`
- [x] T004 Register `src/DiscordChatOverlay/DiscordChatOverlay.csproj` and `tests/DiscordChatOverlay.Tests/DiscordChatOverlay.Tests.csproj` as projects in `OpenDash-Overlays.sln` (add project entries + build configuration mappings for Debug/Release/FastTests)
- [x] T005 [P] Add placeholder icon `assets/discord-chat-overlay/app.ico` (copy or adapt from `assets/wheel-overlay/` to use as starting point; final icon can be updated before release)
- [x] T006 [P] Create `.github/workflows/discord-chat-overlay-release.yml` that triggers on tag pattern `discord-chat-overlay/v*`; build steps: `dotnet build`, `dotnet test --configuration Release`, `powershell -File scripts/discord-chat-overlay/build_msi.ps1`; upload `DiscordChatOverlay-v0.1.0.msi` as release asset; mirror the structure of the existing WheelOverlay release workflow
- [x] T007 [P] Update `CHANGELOG.md` — add `[Unreleased]` section entry for DiscordChatOverlay v0.1.0 describing: Discord IPC voice overlay, OAuth2 PKCE auth, Windows Credential Manager token storage, always-on-top click-through WPF overlay, WiX 4 MSI installer

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure required before any user story can be implemented: app bootstrap, all model types, AliasService (consumed by VoiceSessionService in US2), and test infrastructure

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T008 Create `src/DiscordChatOverlay/Program.cs` — single-instance Mutex guard (named `"DiscordChatOverlay_SingleInstance"`; exit immediately if already held); `LogService.Initialize("DiscordChatOverlay")` as the absolute first call; `Application.Run(new App())` wrapped in top-level try/catch that calls `LogService.Error()` before re-throwing; `[STAThread]` attribute
- [x] T009 Create `src/DiscordChatOverlay/App.xaml` (WPF `Application` with merged `MaterialDesignThemes` resource dictionaries per OverlayCore `MaterialDesignBootstrap` pattern) and `src/DiscordChatOverlay/App.xaml.cs` skeleton (`Application` subclass; `OnStartup` override placeholder; `OnExit` override placeholder; `ThemeService` initialization via OverlayCore; `AppSettings` load on startup; `ShutdownMode = ShutdownMode.OnExplicitShutdown`)
- [x] T010 [P] Create `src/DiscordChatOverlay/Models/AppSettings.cs` — all fields per `contracts/settings-schema.md`: `WindowLeft` (double, 20.0), `WindowTop` (double, 20.0), `Opacity` (int, 90, clamped [10,100]), `ThemePreference` (enum, System), `DisplayMode` (enum, SpeakersOnly), `GracePeriodSeconds` (double, 2.0, clamped [0.0,2.0]), `DebounceThresholdMs` (int, 200, clamped [0,1000]), `FontSize` (int, 14, clamped [8,32]), `ShowOnStartup` (bool, true); `Load()` static method: deserialize from `%APPDATA%\DiscordChatOverlay\settings.json` with `JsonStringEnumConverter`; returns defaults + `LogService.Error()` on missing/corrupt file; calls `ScreenBoundsHelper.ClampPosition()` after load; `Save()` atomic write via temp file + rename (write to `settings.json.tmp`, then `File.Move` overwrite); `ScreenBoundsHelper.ClampPosition(left, top)` static helper using `Screen.AllScreens.Any(s => s.WorkingArea.Contains(...))`, resets to `(20.0, 20.0)` on primary screen if out-of-bounds, logs at Warning
- [x] T011 [P] Create enum files in `src/DiscordChatOverlay/Models/`: `SpeakerState.cs` (Active, RecentlyActive, Silent), `ConnectionState.cs` (Disconnected, Connecting, Connected, Retrying, Failed), `ThemePreference.cs` (Dark, Light, System), `DisplayMode.cs` (SpeakersOnly, AllMembers)
- [x] T012 [P] Create `src/DiscordChatOverlay/Models/ChannelContext.cs` (properties: `GuildId string`, `GuildName string`, `ChannelId string`, `ChannelName string`, `Members List<ChannelMember>`; serializable with `System.Text.Json`) and `src/DiscordChatOverlay/Models/ChannelMember.cs` (properties: `UserId string`, `LastKnownName string`, `CustomDisplayName string?`, `AvatarVisible bool` default true; serializable) — per `contracts/aliases-schema.md`
- [x] T013 [P] Create `src/DiscordChatOverlay/Models/VoiceSession.cs` (properties: `ChannelId string?`, `ChannelName string?`, `GuildId string?`, `GuildName string?`, `Participants Dictionary<string, ParticipantSnapshot>`, `ConnectionState ConnectionState`) and `src/DiscordChatOverlay/Models/ParticipantSnapshot.cs` (properties: `UserId string`, `DiscordDisplayName string`, `AvatarHash string?` (global avatar hash), `GuildAvatarHash string?` (guild-specific avatar hash from `member.avatar` IPC field; preferred over `AvatarHash`), `IsMuted bool`, `IsDeafened bool`) — runtime-only, no persistence
- [x] T014 [P] Create `src/DiscordChatOverlay/Models/ActiveSpeaker.cs` (properties: `UserId string`, `DisplayName string`, `AvatarHash string?` (global), `GuildAvatarHash string?` (guild-specific; preferred), `GuildId string?` (needed for guild avatar CDN URL), `AvatarVisible bool`, `State SpeakerState`, `Opacity double` (default 1.0; `INotifyPropertyChanged`; managed by `VoiceSessionService` fade timer)) — runtime-only, consumed by `OverlayViewModel`
- [x] T015 Create `src/DiscordChatOverlay/Services/AliasService.cs` — owns `%APPDATA%\DiscordChatOverlay\aliases.json`; `Load()`: deserialize `List<ChannelContext>` with `System.Text.Json`; per `contracts/aliases-schema.md` validation: skip ChannelContext if GuildId/ChannelId empty or non-uint64 (log error), skip ChannelMember if UserId non-uint64 or both LastKnownName empty + CustomDisplayName null (log error), treat null Members list as empty; returns empty list on missing file or malformed JSON (log error); `Save()`: atomic temp-file-rename write; `UpsertChannelContext(guildId, guildName, channelId, channelName)`: create or update cached names, save; `UpsertChannelMember(guildId, channelId, userId, discordDisplayName)`: add entry if absent, always update LastKnownName if changed, preserve CustomDisplayName and AvatarVisible, save; `DeleteChannelContext(guildId, channelId)`: remove context + all its members, save; `Resolve(userId, guildId, channelId) → string`: returns `member.CustomDisplayName ?? member.LastKnownName ?? rawDiscordName`; `GetContext(guildId, channelId) → ChannelContext?`
- [x] T016 [P] Create `tests/DiscordChatOverlay.Tests/Infrastructure/TestConfiguration.cs` (static class with `const int Iterations = #if FAST_TESTS 10 #else 100 #endif` and FsCheck `Configuration` instance) and `tests/DiscordChatOverlay.Tests/Infrastructure/UITestBase.cs` (base class with `[StaFact]` / `[StaTheory]` usage guidance for WPF STA thread tests via Xunit.StaFact)

**Checkpoint**: Foundation ready — all model types exist, AliasService functional, app bootstraps. User story phases can now begin.

---

## Phase 3: User Story 1 — Connect and Authenticate with Discord (Priority: P1) 🎯 MVP

**Goal**: App connects to local Discord client via named-pipe IPC, completes OAuth2 PKCE authorization on first run, persists tokens to Windows Credential Manager, reconnects automatically on subsequent launches.

**Independent Test**: Launch the app with Discord running → follow the in-app Discord authorization dialog → verify tray icon shows connected state (normal color) and `ConnectionSettingsCategory` shows "Connected" status. Then relaunch: verify no authorization prompt appears (token reused). Revoke access inside Discord → verify app shows re-authorization prompt.

### Implementation for User Story 1

- [x] T017 [US1] Create `src/DiscordChatOverlay/Services/TokenStorageService.cs` — Credential Manager target `"DiscordChatOverlay"`; `ReadToken() → TokenBundle?`: call `CredentialManager.ReadCredential("DiscordChatOverlay")`; deserialize JSON `{AccessToken, RefreshToken, ExpiryUtc}`; return null if absent; `WriteToken(TokenBundle)`: serialize to JSON, call `CredentialManager.WriteCredential(... CredentialPersistence.LocalMachine)`; `DeleteToken()`: `CredentialManager.DeleteCredential("DiscordChatOverlay")`; `IsTokenExpiredOrExpiringSoon(TokenBundle) → bool`: `ExpiryUtc - UtcNow < 1 hour`; `GeneratePkceVerifier() → string`: 32 cryptographically random bytes, base64url-encoded; `GeneratePkceChallenge(verifier) → string`: base64url(SHA-256(verifier)); `ExchangeCode(code, verifier, clientId) → TokenBundle`: HTTP POST to `https://discord.com/api/oauth2/token` with `grant_type=authorization_code`, `code`, `code_verifier`, `client_id`, `redirect_uri`; parse `access_token`, `refresh_token`, `expires_in`; `RefreshToken(refreshToken, clientId) → TokenBundle`: HTTP POST to `https://discord.com/api/oauth2/token` with `grant_type=refresh_token`; all HTTP failures logged via `LogService.Error()`
- [x] T018 [US1] Create `src/DiscordChatOverlay/Services/DiscordIpcClient.cs` — `private const string ClientId = "YOUR_CLIENT_ID_HERE"` placeholder; async pipe probe loop: try `NamedPipeClientStream(".", "discord-ipc-{N}", InOut, Asynchronous)` for slots 0–9, `ConnectAsync(timeout: 500ms, ct)`, stop at first success; binary frame encode: `[uint32 LE opcode][uint32 LE length][UTF-8 JSON]` written as single `WriteAsync` call (atomic); binary frame decode: `ReadAsync` opcode + length header, then payload bytes; `System.Text.Json` payload serialize/deserialize; background `Task` read loop: on each frame dispatch `JsonElement` to internal router; opcode routing: 0=HANDSHAKE, 1=FRAME (route by `cmd`/`evt`), 2=CLOSE (fire `ConnectionDropped`), 3=PING (reply opcode-4 PONG), 4=PONG (ignore); `SendHandshake()`: write opcode-0 `{"v":1,"client_id":"..."}`, await READY DISPATCH; `SendAuthorize() → string code`: write `{"cmd":"AUTHORIZE","args":{"client_id":"...","scopes":["rpc","rpc.voice.read","identify"]},"nonce":"<guid>"}`, await `AUTHORIZE` response `data.code`; `SendAuthenticate(accessToken)`: write `{"cmd":"AUTHENTICATE","args":{"access_token":"..."},"nonce":"<guid>"}`, await response; on error code 4006 fire `AuthRevoked`; `Subscribe(evt, args)` / `Unsubscribe(evt, args)`: write SUBSCRIBE/UNSUBSCRIBE FRAME; `GetSelectedVoiceChannel() → JsonElement?`: write GET_SELECTED_VOICE_CHANNEL FRAME, await nonce-matched response; C# event interface per `contracts/ipc-event-contracts.md`: `event EventHandler<SpeakingEventArgs> SpeakingStart`, `SpeakingStop`, `VoiceStateCreated`, `VoiceStateUpdated`, `VoiceStateDeleted`, `VoiceChannelSelected`, `AuthRevoked`, `ConnectionDropped`; all events raised on ThreadPool; `DisposeAsync()` closes pipe gracefully
- [x] T019 [US1] Create `src/DiscordChatOverlay/Settings/ConnectionSettingsCategory.cs` implementing `ISettingsCategory` from OverlayCore — displays current connection status ("Connected" / "Retrying (attempt N)" / "Disconnected — authorization required"); Re-authorize button: calls `DiscordIpcClient.SendAuthorize()` + `TokenStorageService.ExchangeCode()` + `TokenStorageService.WriteToken()` then re-authenticate; Disconnect button: calls `TokenStorageService.DeleteToken()`, disconnects IPC pipe, transitions `ConnectionState` to Disconnected, updates status display
- [x] T020 [US1] Wire startup connection sequence in `App.xaml.cs` `OnStartup`: instantiate `TokenStorageService`, `DiscordIpcClient`, `VoiceSessionService` (stub if not yet implemented); call `ConnectAsync()`: probe pipe → HANDSHAKE → read token from `TokenStorageService`; if token present and not expiring: `SendAuthenticate(accessToken)` → SUBSCRIBE globals (`VOICE_CHANNEL_SELECT`, `VOICE_CONNECTION_STATUS`) → `GetSelectedVoiceChannel()` (seed initial state); if token expiring: `RefreshToken()` then proceed; if no token: `SendAuthorize()` → `ExchangeCode()` → `WriteToken()` → proceed; set `ConnectionState = Connected` on success; implement exponential backoff reconnect loop per research.md R-007: slots `[0s, 2s, 4s, 8s, 16s, 32s, 64s]`, full-jitter delay, immediate first retry; on `ConnectionDropped`: start retry loop, set `ConnectionState = Retrying`; on `AuthRevoked`: set `ConnectionState = Failed`, stop loop; on reconnect success: re-AUTHENTICATE + re-SUBSCRIBE all events, set `ConnectionState = Connected`

### Property Tests for User Story 1

- [x] T021 [P] [US1] Create `tests/DiscordChatOverlay.Tests/Models/AppSettingsTests.cs` — three FsCheck property tests using `#if FAST_TESTS / #else` iteration guards:
  - `// Feature: Discord Chat Overlay, Property 1: AppSettings serialization round-trip` — arbitrary `AppSettings` serializes to JSON and deserializes back with all fields identical (using `FsCheck.Gen` for in-range values)
  - `// Feature: Discord Chat Overlay, Property 2: AppSettings defaults satisfy all range constraints` — default `AppSettings()` instance: Opacity in [10,100], GracePeriodSeconds in [0.0,2.0], DebounceThresholdMs in [0,1000], FontSize in [8,32]
  - `// Feature: Discord Chat Overlay, Property 3: out-of-bounds position correction always yields position within monitor bounds` — arbitrary (left, top) outside all `Screen.AllScreens` working areas → `ScreenBoundsHelper.ClampPosition(left, top)` returns position within `Screen.PrimaryScreen.WorkingArea`

**Checkpoint**: US1 complete — app connects to Discord, authenticates, persists token, reconnects automatically. No overlay display required for this checkpoint.

---

## Phase 4: User Story 2 — Display Active Speakers in a Voice Channel (Priority: P2)

**Goal**: While in a Discord voice channel the overlay shows active speakers in real time with grace-period fade, 8-speaker cap + overflow count, connection status indicator, and display mode (speakers-only / all-members).

**Independent Test**: Join a voice channel with another participant → have them speak → verify their name appears on the overlay within 500 ms → they stop → verify name dims (RecentlyActive) then disappears after grace period (default 2 s) → they speak again during grace period → verify immediate return to full intensity. Verify overlay hides when not in any channel. No settings window or alias configuration required.

### Implementation for User Story 2

- [x] T022 [US2] Create `src/DiscordChatOverlay/Services/VoiceSessionService.cs` — holds `VoiceSession` (current channel state); per-participant `Dictionary<string, SpeakerStateMachine>` (each machine owns a debounce `System.Threading.Timer` and a grace `System.Threading.Timer`); state machine per data-model.md: `SPEAKING_START` with `DebounceThresholdMs > 0` → start/reset debounce timer → [Debouncing]; `SPEAKING_START` with `DebounceThresholdMs = 0` → immediate [Active]; debounce timer fires → [Active] (set `ActiveSpeaker.Opacity = 1.0`); `SPEAKING_STOP` while Debouncing → cancel debounce → [Idle] (no visible change); `SPEAKING_STOP` while Active → transition to [RecentlyActive]: start a `DispatcherTimer` that fires every 33 ms (≈30 fps) and decrements `ActiveSpeaker.Opacity` by `(1.0 / (GracePeriodSeconds / 0.033))` per tick until Opacity ≤ 0; when Opacity reaches 0 stop the timer and transition to [Silent]; `SPEAKING_START` while RecentlyActive → cancel fade timer → set `Opacity = 1.0` → [Active] (FR-004b); grace timer fires (Opacity reaches 0) → [Silent]; wire to `DiscordIpcClient` events: `SpeakingStart`, `SpeakingStop`, `VoiceStateCreated` (add `ParticipantSnapshot` to `VoiceSession.Participants`; call `AliasService.UpsertChannelMember()`), `VoiceStateUpdated` (replace snapshot; update `AliasService.LastKnownName`), `VoiceStateDeleted` (remove participant; cancel timers), `VoiceChannelSelected` (clear all participants + timers; if `ChannelId` null hide overlay; if non-null call `GetSelectedVoiceChannel()` to seed then re-subscribe channel-scoped events; call `AliasService.UpsertChannelContext()`), `ConnectionDropped` (set `ConnectionState = Retrying`; clear participant state; signal `App` to start reconnect loop), `AuthRevoked` (set `ConnectionState = Failed`); `BuildActiveSpeaker(userId, participantSnapshot) → ActiveSpeaker`: call `AliasService.Resolve()` for `DisplayName`; copy `AvatarHash`, `GuildAvatarHash`, and `GuildId` from `participantSnapshot`; lookup `AliasService` for `AvatarVisible`; all `ObservableCollection` mutations marshalled via `Application.Current.Dispatcher.Invoke()`; exposes `ObservableCollection<ActiveSpeaker> ActiveSpeakers` and `ConnectionState ConnectionState` properties; `ConnectionState` changes fire `INotifyPropertyChanged`
- [x] T023 [US2] Create `src/DiscordChatOverlay/ViewModels/OverlayViewModel.cs` — `INotifyPropertyChanged`; subscribes to `VoiceSessionService`; maintains `ObservableCollection<ActiveSpeaker> ActiveSpeakers` capped at 8 items; **ordering (FR-004c)**: Active speakers (Opacity == 1.0, State == Active) always first ordered by most-recently-activated; RecentlyActive (fading) members below, ordered by most-recently-activated; Silent members last, alphabetical — speakers-only mode excludes Silent entirely; all-members mode includes Silent below the fading group; `int OverflowCount` = total Active+RecentlyActive speakers beyond cap (shown as `+N more`); `string? ConnectionIndicator`: null when Connected, `"⟳ Reconnecting…"` when Retrying, `"✕ Disconnected"` when Failed (FR-010b); `bool IsInChannel`: true when `VoiceSession.ChannelId != null`; reacts to `VoiceSessionService.ConnectionState` changes
- [x] T024 [US2] Create `src/DiscordChatOverlay/MainWindow.xaml` and `MainWindow.xaml.cs` — WPF window; `WindowStyle="None"`, `AllowsTransparency="True"`, `Background="Transparent"`, `Topmost="True"`; click-through via `WS_EX_TRANSPARENT | WS_EX_LAYERED` applied in code-behind on `SourceInitialized` using `SetWindowLong`; click-through suspended while settings position drag is active (expose `SuspendClickThrough()` / `RestoreClickThrough()` methods called from `AppearanceSettingsCategory`); XAML layout: outer `Grid` with `RowDefinition` for connection indicator + `RowDefinition` for speaker list; connection indicator `TextBlock` bound to `OverlayViewModel.ConnectionIndicator` (visible only when non-null); `ItemsControl` bound to `OverlayViewModel.ActiveSpeakers`; item template: outer row `Grid` with `Opacity` bound to `ActiveSpeaker.Opacity` (drives the fade animation); inner two-column `Grid` with `ColumnDefinition Width="32"` (avatar `Image` control: `Source` bound via `MultiBinding` with `AvatarUrlConverter` — 4 bindings in order: `GuildId`, `UserId`, `GuildAvatarHash`, `AvatarHash` (matches `IMultiValueConverter` parameter order defined in T025); `Visibility` bound via `BoolToVisibilityConverter` on `AvatarVisible` — use `Hidden` not `Collapsed` to preserve column width per FR-014b-layout; on image load failure set source to null silently) and `ColumnDefinition Width="*"` (name `TextBlock` with `TextOptions.TextFormattingMode="Display"`); `+N more` `TextBlock` row below `ItemsControl` bound to `OverflowCount`, `Visibility` collapsed when zero; `DataContext = OverlayViewModel`; window `Left`/`Top` bound to `AppSettings.WindowLeft`/`WindowTop`; hidden when `IsInChannel = false`
- [x] T025 [P] [US2] Create `src/DiscordChatOverlay/Converters/AvatarUrlConverter.cs` (multi-value `IMultiValueConverter` receiving `[GuildId string?, UserId string, GuildAvatarHash string?, AvatarHash string?]`; returns guild CDN URL `https://cdn.discordapp.com/guilds/{GuildId}/users/{UserId}/avatars/{GuildAvatarHash}.png?size=32` when `GuildId` and `GuildAvatarHash` are non-null; otherwise returns global CDN URL `https://cdn.discordapp.com/avatars/{UserId}/{AvatarHash}.png?size=32` when `AvatarHash` non-null; returns `null` when no hash available — Image control renders blank) and `src/DiscordChatOverlay/Converters/BoolToVisibilityConverter.cs` (IValueConverter: true→Visible, false→Hidden — use Hidden not Collapsed to preserve column width); register both in `App.xaml` resources; WPF `Image` controls in MainWindow MUST set `BitmapImage.CacheOption = BitmapCacheOption.OnLoad` and handle `BitmapImage.DownloadFailed` + `DecodeFailed` events to silently null the source and log via `LogService.Warning()`
- [x] T026 [US2] Create `src/DiscordChatOverlay/Settings/DisplaySettingsCategory.cs` implementing `ISettingsCategory` — display mode toggle (radio buttons / ComboBox: SpeakersOnly / AllMembers); grace period slider label "Fade duration (seconds)" range 0–2 s with value display; debounce threshold slider label "Noise gate (ms, 0 = disabled)" range 0–1000 ms with value display; all controls bound to `AppSettings` instance; `Save()` calls `AppSettings.Save()`; `Load()` reloads from `AppSettings` current values
- [x] T027 [US2] Wire `MainWindow` creation in `App.xaml.cs` `OnStartup` — instantiate `OverlayViewModel` with `VoiceSessionService`; create `MainWindow(overlayViewModel)`; show if `AppSettings.ShowOnStartup == true`; apply `AppSettings.WindowLeft`/`WindowTop` / `Opacity` / `FontSize`; hide when `VoiceSessionService` reports no active channel; subscribe to `OverlayViewModel.PropertyChanged` for `ConnectionState` to update tray icon color (wired further in US4)

### Property Tests for User Story 2

- [x] T028 [P] [US2] Create `tests/DiscordChatOverlay.Tests/ViewModels/OverlayViewModelTests.cs` — FsCheck property test with `#if FAST_TESTS / #else` iteration guards:
  - `// Feature: Discord Chat Overlay, Property 1: speaker cap never exceeds 8 items regardless of event count` — generate arbitrary sequence of `SPEAKING_START` events for N participants (N > 8); feed into `OverlayViewModel` via mock `VoiceSessionService`; assert `ActiveSpeakers.Count <= 8` after every event

**Checkpoint**: US2 complete — overlay shows active speakers in real time, grace period works, connection indicator visible. Independently verifiable without settings window or alias configuration.

---

## Phase 5: User Story 3 — Configure Overlay Appearance and Position (Priority: P3)

**Goal**: Settings panel lets user adjust overlay position, opacity, color theme, and font size with live preview; all values persist across restarts with reset-to-defaults support.

**Independent Test**: Open settings window → drag position control → verify overlay moves live; change opacity → verify overlay opacity updates immediately; select Dark/Light theme → verify overlay theme changes live; save → restart app → verify all values restored exactly. No Discord connection required.

### Implementation for User Story 3

- [x] T029 [US3] Create `src/DiscordChatOverlay/Settings/AppearanceSettingsCategory.cs` implementing `ISettingsCategory` — position inputs: `WindowLeft`/`WindowTop` numeric up-down fields; on value change call `MainWindow.Left = value` / `MainWindow.Top = value` (live move, click-through suspended via `MainWindow.SuspendClickThrough()` while dragging, restored on focus-lost); opacity slider 10–100: on change set `MainWindow.Opacity = value / 100.0` (live preview); theme selector (Dark/Light/System ComboBox): on change call `ThemeService.SetTheme(selection)` + `PaletteHelper` sync per OverlayCore pattern (live preview); font size slider 8–32: on change update `MainWindow` `FontSize` resource live; `Save()`: write all values to `AppSettings` + `AppSettings.Save()`; `Load()`: populate controls from current `AppSettings`
- [x] T030 [US3] Create `src/DiscordChatOverlay/ViewModels/SettingsViewModel.cs` — `INotifyPropertyChanged`; holds references to all `ISettingsCategory` instances (`ConnectionSettingsCategory`, `DisplaySettingsCategory`, `AppearanceSettingsCategory`, `AliasSettingsCategory`, `AboutSettingsCategory`); `SaveAll()`: calls `Save()` on each category; `LoadAll()`: calls `Load()` on each; `ResetToDefaults()`: creates fresh `AppSettings()`, calls `AppSettings.Save()`, then `LoadAll()` to refresh all category controls
- [x] T031 [US3] Wire settings window in `App.xaml.cs` — instantiate `SettingsViewModel` with all five `ISettingsCategory` instances; create `MaterialSettingsWindow` (from OverlayCore) passing `SettingsViewModel`; expose `ShowSettings()` method called from tray menu; on window open: `MainWindow.SuspendClickThrough()` (so user can interact with overlay position drag); on window close: `MainWindow.RestoreClickThrough()`; `ThemeService.PaletteHelper` applied on theme change per WheelOverlay `MaterialDesignBootstrap` pattern

**Checkpoint**: US3 complete — all appearance settings adjustable with live preview, persisted and restored on restart.

---

## Phase 6: User Story 4 — Manage via System Tray (Priority: P4)

**Goal**: App runs silently in the tray; context menu provides Show/Hide Overlay, Settings, and Exit; tray icon color reflects connection state (normal / amber / red).

**Independent Test**: Right-click tray icon → verify context menu shows Show Overlay, Hide Overlay, Settings, Exit → click each and verify expected action; verify tray icon turns amber when app simulates Retrying state and red when Failed state.

### Implementation for User Story 4

- [x] T032 [US4] Create NotifyIcon tray setup in `App.xaml.cs` `OnStartup` — `System.Windows.Forms.NotifyIcon` with `Icon = new Icon("assets/discord-chat-overlay/app.ico")`; `ContextMenuStrip` with items: "Show Overlay" (`MainWindow.Show()`), "Hide Overlay" (`MainWindow.Hide()`), "Settings" (`ShowSettings()`), separator, "Exit" (`Application.Current.Shutdown()`); `NotifyIcon.Visible = true`; tray icon color state machine: Connected → default icon; Retrying → amber-tinted icon (overlay amber icon variant or draw amber dot via `System.Drawing`); Failed → red-tinted icon; subscribe to `OverlayViewModel.PropertyChanged` for `ConnectionState` changes to drive icon swap; dispose `NotifyIcon` in `App.OnExit()`
- [x] T033 [US4] Wire all tray menu item click handlers in `App.xaml.cs` — Show Overlay: `MainWindow.Show(); MainWindow.Activate()`; Hide Overlay: `MainWindow.Hide()`; Settings: `ShowSettings()` (instantiates and shows `MaterialSettingsWindow` if not already open, brings to foreground if already open); Exit: `_notifyIcon.Visible = false; _notifyIcon.Dispose(); Application.Current.Shutdown()`; handle `NotifyIcon.DoubleClick` to toggle overlay visibility as convenience
- [x] T034 [P] [US4] Create `src/DiscordChatOverlay/Settings/AboutSettingsCategory.cs` implementing `ISettingsCategory` — displays app version `v0.1.0` (read from `Assembly.GetExecutingAssembly().GetName().Version`); `TextBlock` links to project GitHub page and Discord approval gate note; read-only (no Save/Load logic needed)

**Checkpoint**: US4 complete — app fully controllable from system tray; tray icon reflects connection state.

---

## Phase 7: User Story 5 — Debounce Noisy Voice Activity (Priority: P5)

**Goal**: Leading-edge debounce threshold prevents noise spikes from flickering names on overlay; configurable 0–1000 ms; 0 = disabled (immediate appearance).

**Independent Test**: Set debounce to 250 ms; simulate a 100 ms `SPEAKING_START`→`SPEAKING_STOP` event pair on a mock participant → verify name does NOT appear; simulate 300 ms of continuous `SPEAKING_START` → verify name appears after 250 ms. No Discord connection required; test via `VoiceSessionService` directly.

### Implementation for User Story 5

- [x] T035 [US5] Verify and wire live debounce threshold updates in `VoiceSessionService.cs` — ensure `VoiceSessionService` reads `AppSettings.DebounceThresholdMs` dynamically per-event (not cached at startup) so that adjusting the slider in `DisplaySettingsCategory` and saving takes effect immediately for the next `SPEAKING_START` event without requiring app restart; add `AppSettings` reference to `VoiceSessionService` constructor; confirm `DisplaySettingsCategory` (T026) debounce slider `Save()` triggers `AppSettings.Save()` and `VoiceSessionService` reads the updated value

### Property Tests for User Story 5

- [x] T036 [P] [US5] Create `tests/DiscordChatOverlay.Tests/Services/VoiceSessionServiceTests.cs` — four FsCheck property tests with `#if FAST_TESTS / #else` iteration guards:
  - `// Feature: Discord Chat Overlay, Property 1: debounce events shorter than threshold produce no state transition` — arbitrary threshold T in [1,1000] ms; simulate `SPEAKING_START` followed by `SPEAKING_STOP` after duration D where D < T; assert participant never reaches `Active` state (remains Idle/Debouncing)
  - `// Feature: Discord Chat Overlay, Property 2: debounce threshold elapsed transitions participant to Active` — arbitrary threshold T in [1,500] ms; simulate continuous `SPEAKING_START` for T+50 ms; assert participant reaches `Active` within 500 ms of debounce timer firing
  - `// Feature: Discord Chat Overlay, Property 3: opacity fade monotone during grace period` — arbitrary grace period G in [0.0,2.0] s; transition participant to `RecentlyActive`; advance the fade timer by N arbitrary ticks (each 33 ms); assert `ActiveSpeaker.Opacity` strictly decreases with each tick (monotone descent, never increases) until it reaches 0.0
  - `// Feature: Discord Chat Overlay, Property 4: grace period resumption always transitions to Active and restores opacity` — arbitrary grace period G in [0.0,2.0] s; transition participant to `RecentlyActive`; advance timer by arbitrary t where t < G (Opacity > 0); send `SPEAKING_START`; assert participant transitions back to `Active`, `Opacity == 1.0`, and fade timer is cancelled

**Checkpoint**: US5 complete — debounce behavior verified by property tests; threshold configurable live from settings.

---

## Phase 8: User Story 6 — Per-Context Member Aliases and Icons (Priority: P6)

**Goal**: App auto-populates ChannelContext + ChannelMember records as the user joins channels and observes members; user can set custom display names (full Unicode) and toggle avatar visibility per member per context; custom names appear on overlay instead of Discord names.

**Independent Test**: Join a voice channel (so ChannelContext + ChannelMember records are auto-created) → open Settings → Aliases → verify the context and its members are listed → enter a custom name for one member → save → that member speaks → verify overlay shows custom name. Change to a different context with no custom names → verify Discord display name shown unmodified.

### Implementation for User Story 6

- [ ] T037 [US6] Create `src/DiscordChatOverlay/Settings/AliasSettingsCategory.cs` implementing `ISettingsCategory` — loads all `ChannelContext` records from `AliasService`; renders a `ListBox` or `ScrollViewer` with one group per `ChannelContext` (header: "GuildName / ChannelName"); within each context: `ItemsControl` over `ChannelMember` list showing `LastKnownName` (read-only label for reference), `CustomDisplayName` `TextBox` (max 100 characters, Unicode-enabled, null/empty = not set), `AvatarVisible` `CheckBox`; "Delete Context" `Button` per context: shows `MessageBox.Show(... MessageBoxButton.YesNo)` confirmation dialog; on confirm calls `AliasService.DeleteChannelContext()` and refreshes list; global "Save" `Button`: iterates all controls, writes updated `CustomDisplayName`/`AvatarVisible` to each `ChannelMember` via `AliasService.UpsertChannelMember()`, then `AliasService.Save()`; `Load()`: refresh control state from `AliasService.GetContext()` data
- [ ] T038 [US6] Verify alias resolution wiring in `VoiceSessionService.BuildActiveSpeaker()` — confirm that `AliasService.Resolve(userId, guildId, channelId)` is called for every `ActiveSpeaker` built (this logic was stubbed or partially wired in T022); ensure `ActiveSpeaker.DisplayName` is `member.CustomDisplayName ?? member.LastKnownName ?? participantSnapshot.DiscordDisplayName`; ensure `ActiveSpeaker.AvatarVisible` is `member?.AvatarVisible ?? true`; verify that after user saves new custom name in `AliasSettingsCategory` and a `SPEAKING_START` event fires, `OverlayViewModel.ActiveSpeakers` reflects the updated custom name

### Property Tests for User Story 6

- [ ] T039 [P] [US6] Create `tests/DiscordChatOverlay.Tests/Models/ChannelMemberTests.cs` — FsCheck property test with `#if FAST_TESTS / #else` iteration guards:
  - `// Feature: Discord Chat Overlay, Property 1: ChannelMember with arbitrary Unicode custom name serializes and deserializes identically` — generate arbitrary Unicode string (including non-Latin scripts, emoji, surrogate pairs) as `CustomDisplayName`; serialize `ChannelMember` to JSON via `System.Text.Json`; deserialize back; assert `CustomDisplayName` bytes-equal to original
- [ ] T040 [P] [US6] Create `tests/DiscordChatOverlay.Tests/Services/AliasServiceTests.cs` — two FsCheck property tests with `#if FAST_TESTS / #else` iteration guards:
  - `// Feature: Discord Chat Overlay, Property 1: AliasService Resolve returns custom name when set, falls back to last-known name, then raw Discord name` — generate arbitrary `ChannelMember` with all combinations of `CustomDisplayName` (null / non-empty) and `LastKnownName` (non-empty); assert `Resolve()` returns custom name when set, last-known name when custom is null, raw Discord name when both are null/missing
  - `// Feature: Discord Chat Overlay, Property 2: AliasService skips and logs malformed entries without throwing` — generate arbitrary JSON arrays with random mix of valid and invalid `ChannelContext`/`ChannelMember` entries (missing fields, non-uint64 IDs, null Members); write to temp file; call `AliasService.Load()`; assert no exception thrown; assert only valid entries are returned; assert `LogService.Error()` was called for each skipped entry (verify via log capture or mock)

**Checkpoint**: US6 complete — alias resolution working end-to-end; AliasSettingsCategory UI functional; Unicode and malformed-entry handling verified by property tests.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: WiX 4 MSI installer, build scripts, documentation, validation

- [ ] T041 Create WiX 4 MSI installer files: `installers/discord-chat-overlay/Package.wxs` (unique `UpgradeCode` GUID; `Name="DiscordChatOverlay"`; `Version="0.1.0"`; component: `DiscordChatOverlay.exe` + all runtime files; install dir `%ProgramFiles%\OpenDash\DiscordChatOverlay`; create `%APPDATA%\DiscordChatOverlay` directory; shortcut in Start Menu; output filename `DiscordChatOverlay-v0.1.0.msi` per FR-015 and project convention) and `installers/discord-chat-overlay/CustomUI.wxs` (standard WiX UI dialog sequence); structure mirrors `installers/wheel-overlay/`
- [ ] T042 [P] Create `scripts/discord-chat-overlay/build_msi.ps1` — `dotnet publish` DiscordChatOverlay in Release to staging dir; run `wix build` with `Package.wxs` + `CustomUI.wxs`; output to `installers/discord-chat-overlay/bin/DiscordChatOverlay-v0.1.0.msi`; mirrors `scripts/wheel-overlay/build_msi.ps1` structure
- [ ] T043 [P] Create `scripts/discord-chat-overlay/build_release.ps1` (full release build: clean, build, test Release, build MSI, sign if cert available) and `scripts/discord-chat-overlay/generate_components.ps1` (helper: enumerate publish output to generate WiX component fragments)
- [ ] T044 [P] Create `docs/discord-chat-overlay/index.md` (overview: what DiscordChatOverlay is, feature summary, version, system requirements), `docs/discord-chat-overlay/getting-started.md` (install from MSI; first-run auth flow step-by-step; quick tour of tray menu and overlay), `docs/discord-chat-overlay/settings.md` (all settings fields explained with defaults and valid ranges: connection, display, appearance, aliases), `docs/discord-chat-overlay/troubleshooting.md` (common errors: Discord not running, auth revoked, overlay off-screen; reconnect behaviour explained; Credential Manager inspection)
- [ ] T045 Update `mkdocs.yml` nav — add `Discord Chat Overlay` entry with sub-pages pointing to `docs/discord-chat-overlay/index.md`, `getting-started.md`, `settings.md`, `troubleshooting.md`; follow hub-and-spoke architecture from `docs/docs-hub` pattern; add app to App Gallery page if one exists
- [ ] T046 [P] Run `powershell -File scripts/Validate-PropertyTests.ps1` from repo root — verify all 10 property tests have correct `// Feature: Discord Chat Overlay, Property N: ...` comment directives and `#if FAST_TESTS / #else` iteration count guards; fix any directive or guard violations before proceeding
- [ ] T047 Build and smoke-test: `dotnet build OpenDash-Overlays.sln`; `dotnet test --configuration FastTests`; verify 0 build errors, 0 test failures, and no regressions in `WheelOverlay.Tests` or `OverlayCore.Tests`
- [ ] T048 [P] Manual performance verification (SC-007): with an active voice session (≥2 participants, one speaking), observe Task Manager / VS Diagnostic Tools for 5 minutes; confirm CPU usage remains below 2% and working set below 100 MB; record results in a comment on the release PR; no automated gate — manual sign-off required before release

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion — **BLOCKS all user story phases**
- **Phase 3 (US1)**: Depends on Phase 2 — can start as soon as Foundational is complete
- **Phase 4 (US2)**: Depends on Phase 2; integrates with Phase 3 (`DiscordIpcClient`, `TokenStorageService`) but US2 is independently testable via mock/stub
- **Phase 5 (US3)**: Depends on Phase 2 + Phase 4 (`MainWindow`, `OverlayViewModel` must exist); `AppSettings` already exists from Phase 2
- **Phase 6 (US4)**: Depends on Phase 2 + Phase 5 (`MaterialSettingsWindow` wired); tray icon color depends on Phase 3/4 `ConnectionState`
- **Phase 7 (US5)**: Depends on Phase 2 + Phase 4 (`VoiceSessionService` debounce already implemented in T022)
- **Phase 8 (US6)**: Depends on Phase 2 (models, `AliasService`) + Phase 4 (`VoiceSessionService` calling `AliasService`)
- **Final Phase**: Depends on all desired user stories complete

### User Story Dependencies

- **US1 (P1)**: No story dependencies — MVP gate. All other stories require a working connection but are independently testable with stubs.
- **US2 (P2)**: No hard story dependency; integrates with US1 `DiscordIpcClient`/`VoiceSessionService` events
- **US3 (P3)**: Requires `MainWindow` from US2 and `AppSettings` from Foundational
- **US4 (P4)**: Requires `MaterialSettingsWindow` wired from US3; `ConnectionState` from US1/US2
- **US5 (P5)**: `VoiceSessionService` debounce logic implemented in US2; US5 adds property tests and verified live-update wiring
- **US6 (P6)**: Requires `AliasService` (Foundational) + `VoiceSessionService` (US2) + `AliasSettingsCategory` UI (US6 itself)

### Parallel Opportunities (within phases)

- **Phase 1**: T002 and T003 can run in parallel after T001; T005, T006, T007 fully independent
- **Phase 2**: T010, T011, T012, T013, T014, T016 all parallelizable after T008/T009 begin; T015 (AliasService) depends on T012 (ChannelContext/ChannelMember models)
- **Phase 3 (US1)**: T017 and T018 fully parallel; T019 and T020 depend on T018; T021 can run parallel to T017–T020
- **Phase 4 (US2)**: T025 (Converters) parallel to T022 (VoiceSessionService); T023 (OverlayViewModel) depends on T022; T024 (MainWindow) depends on T023; T028 (property tests) parallel to implementation
- **Phase 5–8**: Each phase has parallel test tasks; T039 and T040 fully parallel in Phase 8

---

## Parallel Execution Example: User Story 2

```
# Parallel: start together after Phase 2 + T018 complete
Task T022: VoiceSessionService (core state machine)
Task T025: Converters (SpeakerStateToOpacity, BoolToVisibility)

# Sequential: after T022
Task T023: OverlayViewModel (depends on VoiceSessionService)

# Sequential: after T023
Task T024: MainWindow XAML + code-behind (depends on OverlayViewModel)

# Parallel: alongside T022–T024
Task T028: OverlayViewModelTests (property test — can be written in parallel)

# Sequential: after T022–T025
Task T026: DisplaySettingsCategory (depends on AppSettings + VoiceSessionService)
Task T027: Wire MainWindow creation in App.xaml.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (**CRITICAL** — blocks everything)
3. Complete Phase 3: US1 (Connect and Authenticate)
4. **STOP and VALIDATE**: Confirm tray icon shows Connected; re-launch confirms token reuse; revoke confirms re-auth prompt
5. MVP validated — Discord connection working end-to-end

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 (US1) → Auth works → **MVP checkpoint**
3. Phase 4 (US2) → Overlay shows speakers → **Core value delivered**
4. Phase 5 (US3) → Appearance configurable
5. Phase 6 (US4) → Full tray management
6. Phase 7 (US5) → Debounce tunable with verified property tests
7. Phase 8 (US6) → Aliases and custom names
8. Final Phase → Installer, docs, release-ready

### MVP Scope

**Phases 1–4 (T001–T028)** deliver a fully functional overlay: connects to Discord, shows active speakers with grace period, 8-speaker cap, connection indicators, and display mode toggle. This is the minimum shippable value for whitelisted testers.

---

## Notes

- **`[P]`** tasks operate on different files with no blocked dependencies — safe to parallelize
- **`[Story]`** labels map each task to its user story for independent implementation and traceability
- All property tests MUST include `// Feature: Discord Chat Overlay, Property N: <title>` comment and `#if FAST_TESTS / #else` iteration guards (Constitution Principle II)
- `OverlayCore` must **not** have `<Version>` in its `.csproj` (Constitution Principle I)
- All service initialization failures MUST call `LogService.Error()` — never silently swallowed (Constitution Principle V)
- `LogService.Initialize("DiscordChatOverlay")` in `Program.cs` must be the absolute first call (Constitution Principle V)
- Discord `rpc.voice.read` approval is a **distribution gate**, not a build blocker — MSI can be built and tested privately via the 50-slot developer tester whitelist
- The `client_id` constant in `DiscordIpcClient.cs` is a public identifier (not a secret) and is committed to the repo; the OAuth token stored in Windows Credential Manager is the only secret
- Commit after each task or logical group; push to `discord-chat-overlay/v0.1.0` branch per project convention
