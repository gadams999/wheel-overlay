# Feature Specification: SpeakerSight

**Feature Branch**: `speakersight/v0.1.0`
**Spec Folder**: `specs/004-speakersight/`
**Created**: 2026-03-30
**Status**: Draft
**Input**: User description: "create a new overlay application, Discord Chat. This will be an overlay that connects to a users Discord client, authenticates, then show users in a voice channel that are speaking. The overlay should mimic the wheel-overlay approach using OverlayCore. The version will be v0.1.0 for the application, use that for the branch name also."

## User Scenarios & Testing *(mandatory)*

<!--
  User stories are ordered by priority. Each story is independently testable
  and delivers value on its own as a functional increment.
-->

### User Story 1 - Connect and Authenticate with Discord (Priority: P1)

A user opens the SpeakerSight application and grants it permission to connect to their running Discord client. The application authenticates and begins receiving real-time voice activity data from Discord, without requiring the user to log in again after the initial setup.

**Why this priority**: Without authentication and a live connection to Discord, the overlay has no data source and provides zero value. All other stories depend on this foundation.

**Independent Test**: Can be fully tested by launching the application, completing the authorization flow, and verifying that the connection status indicator shows "Connected" — without any overlay display logic required.

**Acceptance Scenarios**:

1. **Given** a user has Discord running and launches SpeakerSight for the first time, **When** they follow the authorization prompt, **Then** the application connects to Discord, persists the credentials, and shows a "Connected" status in the system tray icon or settings panel.
2. **Given** a user has previously authorized SpeakerSight, **When** they launch the application, **Then** the application reconnects automatically without prompting for authorization again.
3. **Given** the application is running and Discord is closed, **When** Discord is reopened, **Then** the application detects the reconnection and resumes data reception automatically.
4. **Given** the user revokes authorization inside Discord, **When** the application next attempts to retrieve data, **Then** it shows a clear re-authorization prompt and does not silently fail.

---

### User Story 2 - Display Active Speakers in a Voice Channel (Priority: P2)

While the user is in a Discord voice channel, the overlay displays voice channel participants. By default it shows only those who are currently speaking; users may toggle a setting to show all channel members at all times with active speakers visually highlighted. The display updates in real time and collapses to an idle state when no one is speaking and the speakers-only mode is active.

**Why this priority**: This is the core visible value of the application — showing who is speaking at a glance without switching to the Discord window.

**Independent Test**: Can be fully tested by joining a voice channel, having another participant speak, and verifying that their name appears on the overlay; stop speaking and verify the indicator clears — no settings window or tray controls needed.

**Acceptance Scenarios**:

1. **Given** the user is in a voice channel and a participant begins speaking, **When** Discord reports voice activity, **Then** that participant's display name appears on the overlay within 500 ms.
2. **Given** a participant on the overlay stops speaking, **When** voice activity ceases, **Then** their name enters a dim/fade "recently speaking" grace period (default 2 seconds, user-configurable) before being removed (speakers-only) or returned to a plain silent state (all-members).
3. **Given** a participant is in the "recently speaking" grace period, **When** they begin speaking again, **Then** their display immediately returns to full active intensity and the grace period timer resets.
4. **Given** multiple participants are speaking simultaneously, **When** voice activity data is received, **Then** all active speakers are shown on the overlay at the same time, each clearly distinguishable.
5. **Given** the user is not in any voice channel, **When** the overlay is active, **Then** the overlay is hidden or shows an idle state (e.g., blank or "Not in a channel").
6. **Given** the display mode is set to "all members", **When** participants are in the channel, **Then** all members are listed with active speakers visually distinguished from silent ones.
7. **Given** the display mode is set to "speakers only" (default), **When** no one is speaking, **Then** the overlay shows an idle state rather than a list of silent members.

---

### User Story 3 - Configure Overlay Appearance and Position (Priority: P3)

The user opens the settings panel to adjust where the overlay appears on screen, its size, transparency, and color theme (dark/light/system). Changes are previewed live and persisted across application restarts.

**Why this priority**: Customization is important for usability — users run the overlay alongside games or other applications and need precise control over positioning and visual weight. However, a fixed default position delivers the core value without this story.

**Independent Test**: Can be fully tested by opening the settings window, moving the overlay anchor point, changing the opacity, saving, restarting the application, and verifying settings are retained — independently of a Discord connection.

**Acceptance Scenarios**:

1. **Given** the settings window is open, **When** the user drags the position control or enters coordinates, **Then** the overlay moves on screen in real time without requiring a save or restart.
2. **Given** the user adjusts opacity or selects a color theme, **When** the change is made, **Then** the overlay appearance updates immediately as a live preview.
3. **Given** the user saves settings and restarts the application, **When** the overlay is shown again, **Then** all customized values (position, opacity, theme) are restored exactly.
4. **Given** the user resets settings to defaults, **When** defaults are applied, **Then** the overlay returns to its built-in default position and appearance.

---

### User Story 4 - Manage via System Tray (Priority: P4)

The application runs silently in the system tray after launch, with a context menu to show/hide the overlay, open settings, and exit the application — mirroring the WheelOverlay system-tray pattern.

**Why this priority**: Consistent with the OverlayCore pattern; users expect background overlay apps to live in the tray. Provides the minimum control surface without a full UI window.

**Independent Test**: Can be fully tested by right-clicking the tray icon and verifying that Show Overlay, Hide Overlay, Settings, and Exit each perform their expected action.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** the user right-clicks the system tray icon, **Then** a context menu appears with at minimum: Show Overlay, Hide Overlay, Settings, and Exit.
2. **Given** the overlay is visible, **When** the user selects "Hide Overlay" from the tray menu, **Then** the overlay disappears without closing the application.
3. **Given** the user selects "Exit" from the tray menu, **When** confirmed, **Then** the application terminates cleanly and removes itself from the tray.

---

### User Story 5 - Debounce Noisy Voice Activity (Priority: P5)

A user with an open microphone, a mechanical keyboard, or background noise in their environment triggers brief voice-activity events that cause names to flicker on and off the overlay rapidly. The user configures a debounce threshold so that only sustained speech — not transient noise spikes — causes a name to appear.

**Why this priority**: Open mics are common in casual voice channels and the flickering they cause is disruptive to the primary use case (at-a-glance speaker awareness). The grace period (P2) addresses the trailing edge; this story addresses the leading edge. Together they produce a stable, noise-resistant overlay.

**Independent Test**: Can be fully tested without a real Discord connection by simulating rapid on/off voice-activity events and verifying that names only appear after the debounce threshold has elapsed, with no dependency on alias or settings UI stories.

**Acceptance Scenarios**:

1. **Given** the debounce threshold is set to 250 ms and a participant's microphone triggers a 100 ms noise spike, **When** voice activity is reported, **Then** the participant's name does NOT appear on the overlay.
2. **Given** the debounce threshold is set to 250 ms and a participant speaks continuously for 300 ms, **When** voice activity is reported, **Then** the participant's name appears on the overlay after 250 ms of continuous activity.
3. **Given** the debounce threshold is 0 ms (disabled), **When** any voice activity is reported, **Then** the participant's name appears immediately (existing SC-002 behaviour preserved).
4. **Given** a participant passes the debounce threshold and is shown as active, **When** they stop speaking, **Then** the normal trailing-edge grace period applies (debounce only affects activation, not removal).

---

### User Story 6 - Per-Context Member Aliases and Icons (Priority: P6)

A user belongs to multiple Discord servers and voice channels where the same person may be known by different names or where Discord usernames are unclear. Each time the user joins a voice channel, the application automatically records that guild+channel context and adds any observed members to it (identified by snowflake ID, stored with their current Discord display name). The user can then enter a custom name and optional icon for any member within that context. These custom values appear on the overlay instead of the Discord display name.

**Why this priority**: Alias resolution is a quality-of-life feature that makes the overlay more meaningful in communities with generic usernames, inside-joke names, or role-based identities. It does not affect core functionality but significantly improves long-term usability for regular users.

**Independent Test**: Can be fully tested by joining a voice channel (so the context and members are auto-populated), opening settings to enter a custom name for an observed member, then verifying the overlay renders the custom name when that member speaks — without requiring any other stories beyond P1 and P2.

**Acceptance Scenarios**:

1. **Given** the user joins a voice channel for the first time, **When** members are present or speak, **Then** the application automatically creates a ChannelContext record for that guild+channel and adds each observed member as a ChannelMember entry with their snowflake ID and current Discord display name.
2. **Given** a ChannelMember entry exists for "John Doe" in server "Foo" / channel "General" and the user sets a custom name of "Johnny Doe", **When** that member speaks while the user is in that channel, **Then** the overlay displays "Johnny Doe" instead of the Discord display name.
3. **Given** the same member has no custom name set in server "Bar" / channel "General", **When** they speak in that context, **Then** the overlay displays their standard Discord display name unmodified.
4. **Given** the avatar toggle is turned off for a ChannelMember entry, **When** that member speaks alongside a member whose avatar is on, **Then** both names start at the same horizontal position — the avatar column for the avatar-off member is visibly empty, preserving alignment.
5. **Given** a custom display name contains inline emoji (e.g., "🎮 Johnny"), **When** that member speaks, **Then** the overlay renders the full custom name string including the emoji as part of the name text.
6. **Given** the user clears the custom name field for a ChannelMember entry, **When** that member next speaks, **Then** the overlay reverts to their Discord display name.
7. **Given** a member's Discord display name changes after their ChannelMember entry was created, **When** they speak in a context where a custom name is set, **Then** the custom name still applies because the entry is keyed to the member's permanent snowflake ID. The stored default name is updated to reflect their new Discord display name.
8. **Given** a ChannelContext record exists with custom member names set, **When** the user deletes that context via the settings panel and confirms the deletion prompt, **Then** the ChannelContext and all its ChannelMember entries are permanently removed and the context is recreated fresh if the user rejoins that channel.

---

### Edge Cases

- What happens when the user is in a voice channel but all participants are muted or not speaking? (Overlay shows idle state or last known channel name with no speakers listed.)
- What happens if Discord's IPC connection drops mid-session? (Application retries connection silently with exponential backoff and shows a non-intrusive reconnecting indicator.)
- What happens if more than 8 participants are speaking simultaneously? (The overlay shows the first 8 and appends a `+N more` count indicator for the remainder; the overlay never overflows off-screen.)
- What happens when a username contains non-Latin characters or emoji? (Names are rendered as-is; the overlay must handle Unicode display without garbled text.)
- What happens if the overlay is positioned off-screen after a monitor configuration change? (The application detects out-of-bounds on launch and resets position to a safe default.)
- What happens when the system locks or the screen is off? (Overlay rendering pauses and resumes automatically when the session is unlocked.)
- What happens if a participant produces rapid alternating noise spikes shorter than the debounce threshold? (Each spike resets the debounce timer; the name only appears after a continuous uninterrupted period equal to the threshold.)
- What happens if two members in the same channel have the same Discord display name? (ChannelMember entries are keyed to snowflake ID — each member has an independent entry and both resolve correctly even with identical display names.)
- What happens if the Discord avatar for a member cannot be retrieved (network unavailable, avatar URL expired)? (The overlay shows the member's name and emoji (if set) without an avatar image; no error is surfaced to the user beyond a log entry.)
- What happens if `aliases.json` is manually edited with malformed entries? (Invalid entries are skipped and logged; valid entries continue to apply.)
- What happens when a member joins a channel after the user? (The system observes them on join and auto-creates their ChannelMember entry at that point; they appear in settings after their first observation.)
- What happens if the user deletes a ChannelContext and then rejoins that channel? (A new ChannelContext is created from scratch with no custom names — all previously set aliases are permanently gone. The confirmation prompt on deletion is the safeguard.)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST connect to the locally running Discord client using Discord's local IPC mechanism without requiring users to supply developer credentials manually. A pre-registered Discord application `client_id` MUST be bundled in the app binary; no user-side Discord developer registration is required.
- **FR-002**: The system MUST complete a one-time authorization flow within the Discord application and persist the resulting token in the OS-provided encrypted credential store (Windows Credential Manager) so the user is not re-prompted on restart and the token is never stored in plain text.
- **FR-003**: The system MUST display voice channel participant names on an always-on-top overlay window, operating in one of two modes: (a) **speakers-only** (default) — shows only currently speaking participants; (b) **all-members** — shows all channel members at all times with active speakers visually distinguished from silent ones.
- **FR-003a**: The display mode MUST be user-configurable via a toggle in the settings panel and persisted in `settings.json`; the default value MUST be speakers-only.
- **FR-004**: The overlay MUST update speaker presence within 500 ms of Discord reporting a voice-activity change (both activation and deactivation).
- **FR-004a**: When a speaker's voice activity stops, the overlay MUST animate that participant's opacity from 1.0 to 0.0 over the configured grace period duration (default 2 seconds, range 0–2 seconds). In speakers-only mode the row is removed when opacity reaches 0; in all-members mode the row remains visible at 0 opacity (silent state). The opacity animation begins immediately on voice stop — there is no fixed-dim intermediate step.
- **FR-004b**: If a participant in the grace period resumes speaking, the overlay MUST immediately restore full opacity (1.0), cancel the fade animation, and reset the grace period timer.
- **FR-004c**: Active speakers (opacity 1.0) MUST always appear above recently-speaking (fading) participants in the overlay list. Within each group, order is by most-recently-activated. Silent members in all-members mode appear below both groups, sorted alphabetically.
- **FR-005**: The system MUST hide the overlay automatically when the user is not in any voice channel.
- **FR-006**: The system MUST support simultaneous display of up to 8 active speakers without overlap or clipping. If more than 8 speakers are active at once, the overlay MUST show the 8 most-recently-activated speakers and append a `+N more` count indicator for the remainder. This cap is fixed for v0.1.0 and is not user-configurable.
- **FR-007**: The system MUST persist user configuration (position, size, opacity, color theme) to a JSON settings file at `%APPDATA%\SpeakerSight\settings.json`.
- **FR-008**: The application MUST start minimized to the system tray and provide a tray context menu with at minimum: Show Overlay, Hide Overlay, Settings, and Exit.
- **FR-009**: The system MUST log application events and errors to `%APPDATA%\SpeakerSight\logs.txt` with 1 MB rotation, using OverlayCore's LogService.
- **FR-015**: The application MUST ship with a WiX 4 MSI installer, following the same structure and toolchain as WheelOverlay (`installers/speakersight/`). The output MSI filename MUST include the application version number (e.g. `SpeakerSight-v0.1.0.msi`).
- **FR-010**: The application MUST attempt to reconnect to Discord automatically after a connection drop using exponential backoff, without user intervention.
- **FR-010a**: The system MUST reflect connection state in the system tray icon: normal color when connected, **amber** when actively retrying (Retrying state), and **red** when the connection has failed and requires user action (Failed state — e.g., auth revoked or persistent unrecoverable error).
- **FR-010b**: When in Retrying state, the overlay MUST display a light-colored status indicator with the text **"⟳ Reconnecting…"**; when in Failed state it MUST display **"✕ Disconnected"**. Both indicators are removed immediately when the connection returns to Connected.
- **FR-010c**: The Retrying state transitions to Failed when a non-recoverable error occurs (such as authorization revoked by the user). A recoverable IPC drop remains in Retrying indefinitely until reconnection succeeds.
- **FR-011**: The overlay window MUST be click-through (non-interactive) by default so it does not intercept mouse or keyboard input intended for the application behind it. Click-through MUST be suspended only when the user is actively repositioning the overlay via the settings panel, and restored immediately on completion.
- **FR-011a**: The overlay uses the same always-on-top + click-through window pattern as WheelOverlay. Exclusive fullscreen (DirectX fullscreen exclusive) mode is not supported; users must run their game or application in borderless windowed mode for the overlay to be visible.
- **FR-012**: The system MUST detect and correct an out-of-bounds overlay position at startup if the saved position falls outside all connected monitors.
- **FR-013**: The system MUST apply a configurable leading-edge debounce threshold to voice-activity events — a participant's name MUST NOT appear on the overlay until their voice activity has been continuous for at least the threshold duration (default 200 ms, range 0–1000 ms, where 0 disables debounce).
- **FR-013a**: The debounce threshold MUST be user-configurable in the settings panel and persisted in `settings.json`. Setting it to 0 restores immediate appearance (SC-002 behaviour).
- **FR-013b**: The debounce timer MUST reset if voice activity stops before the threshold is reached; each new continuous activity period starts a fresh timer.
- **FR-014**: Each time the user joins a voice channel, the system MUST automatically create or update a ChannelContext record for that guild+channel (identified by guild snowflake ID + channel snowflake ID). As members are observed in that channel, the system MUST automatically add or update a ChannelMember entry for each, storing their snowflake ID and current Discord display name.
- **FR-014a**: ChannelMember entries MUST be keyed to the member's Discord numeric snowflake ID. The stored default display name MUST be updated each time the member is observed with a different name, but any user-set custom name MUST be preserved. The settings panel MUST show the stored default name alongside the custom name field for reference.
- **FR-014b**: Each ChannelMember entry MAY have a user-defined custom display name (supports full Unicode including inline emoji characters) and an avatar visibility toggle (default: on). When resolving a name for display, the system MUST use the custom display name if set; otherwise it MUST use the member's Discord display name. The overlay MUST display the member's Discord avatar — preferring the guild-specific avatar (from the `member.avatar` field in IPC voice state events if present) and falling back to the global Discord avatar (`user.avatar`). The avatar is hidden when the avatar toggle is off for that entry. If the avatar image cannot be loaded (network unavailable, CDN error), the avatar column is left empty with no error surfaced to the user beyond a log entry.
- **FR-014b-layout**: The overlay MUST use a fixed two-column layout for member rows — a fixed-width avatar column on the left and a name column on the right. Name text MUST always start at the same horizontal position regardless of whether the avatar is visible; when avatar is off the avatar column is left empty. This ensures all member names remain vertically aligned.
- **FR-014c**: ChannelContext and ChannelMember data MUST be stored in a dedicated `aliases.json` file under `%APPDATA%\SpeakerSight\`. Malformed or missing entries MUST be skipped and logged without crashing the application. The settings panel is the only supported interface for editing custom names and toggles.
- **FR-014d**: ChannelContext records MUST be retained indefinitely. The settings panel MUST allow the user to manually delete any ChannelContext record, which permanently removes it and all its associated ChannelMember entries. Deletion MUST require a confirmation step to prevent accidental data loss.
- **FR-016**: The configured font size MUST be applied uniformly to all text elements rendered on the overlay — speaker names, the connection status indicator, and the `+N more` overflow count. The overlay window dimensions MUST be computed from the current font size and fixed at those dimensions for the duration of the session; the overlay MUST NOT resize dynamically as speakers are added or removed. The overlay height MUST accommodate exactly 8 speaker rows at the configured font size with consistent inter-row spacing. The overlay width MUST be calculated as: avatar column (fixed width per FR-014b-layout) plus inter-column spacing plus a name column wide enough to display 32 repetitions of the character `W` at the current font size and overlay font family (used as the reference for maximum glyph advance width). Font size changes MUST be persisted to `settings.json` and MUST cause the overlay to recompute and apply new dimensions immediately.
- **FR-016a**: When the settings panel is open, the overlay MUST display a live preview using 8 placeholder speaker rows — named "Speaker 1" through "Speaker 8" — allowing the user to evaluate font size, text color, speaker name background, and opacity without a live Discord session. The first 5 placeholder rows MUST render in the Active state (full opacity) and the last 3 in the RecentlyActive state (fading opacity) so that both visual states are visible in the preview. The live preview MUST reflect the overlay's current on-screen position; position-drag in the settings panel MUST move the overlay in real time during preview as per normal live-preview behavior. The live preview replaces any real speaker data while the settings panel is open and restores live data immediately on close.

### Key Entities *(include if feature involves data)*

- **VoiceSession**: The user's current Discord voice channel state — channel name, guild name, list of participants, connection status.
- **ActiveSpeaker**: A participant producing or recently producing voice activity — display name, guild/global avatar, current opacity (double 0.0–1.0), speaking state (active | recently-active | silent). Active: opacity 1.0, full visual weight. RecentlyActive: opacity animating 1.0→0.0 over the grace period; positioned below all Active speakers. Silent: opacity 0.0, removed from list in speakers-only mode or shown below fading members in all-members mode. Resuming speech at any point during the fade immediately restores opacity to 1.0 and cancels the animation.
- **DiscordConnection**: The authenticated link between SpeakerSight and the local Discord client — connection state (Connected | Retrying | Failed), token (stored encrypted via Windows Credential Manager), retry metadata (attempt count, next retry time). Retrying: IPC dropped, exponential backoff in progress. Failed: auth revoked or persistent disconnect requiring user action.
- **OverlaySettings**: User-persisted preferences — screen position (X, Y), overlay dimensions, opacity (0–100), color theme (Dark/Light), display mode (speakers-only | all-members, default: speakers-only), recently-speaking grace period / fade duration in seconds (default 2, range 0–2), voice-activity debounce threshold in milliseconds (default 200, range 0–1000), show-on-startup flag.
- **ChannelContext**: Auto-created record for each guild+channel the user has visited — guild ID (snowflake), guild display name (cached), channel ID (snowflake), channel display name (cached), list of observed ChannelMember entries.
- **ChannelMember**: Auto-populated record for each member observed in a ChannelContext — member snowflake ID (permanent key), last-known Discord display name (auto-updated on each observation), custom display name (optional, full Unicode including inline emoji; overrides default on overlay), avatar visible toggle (boolean, default: true). A ChannelMember entry is created automatically with defaults and is enriched only when the user explicitly sets values in settings.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The application connects to Discord and reaches an authenticated "Connected" state within 30 seconds of a user completing the first-time authorization flow.
- **SC-002**: Active speaker names appear on the overlay within 500 ms of voice activity being detected, measured from Discord event to visible display update.
- **SC-003**: Within 500 ms of voice activity stopping, the speaker's opacity fade animation begins (1.0→0.0 over the configured grace period, default 2 seconds). After the fade completes the row is removed (speakers-only) or remains at 0 opacity (all-members). Resuming speech at any point during the fade immediately restores opacity to 1.0. Active speakers (opacity 1.0) are always rendered above fading participants.
- **SC-004**: 100% of user-configured overlay settings (position, opacity, theme) are restored correctly after an application restart.
- **SC-005**: The overlay does not capture or block any mouse clicks or keyboard input directed at applications rendered beneath it.
- **SC-006**: Within 500 ms of a connection drop the tray icon changes to amber and the overlay shows a retrying indicator; the application reconnects automatically within 10 seconds of a recoverable IPC drop without any user action. On reconnection the tray icon returns to its normal connected color and the indicator is removed.
- **SC-007**: The application consumes less than 2% CPU and less than 100 MB of memory during steady-state operation with an active voice session.
- **SC-008**: The overlay correctly renders Unicode display names (including non-Latin scripts and emoji) without garbled text or layout breaks.
- **SC-009**: With debounce enabled at its default threshold (200 ms), a voice-activity event shorter than the threshold produces no visible change on the overlay; an event sustained beyond the threshold causes the name to appear within 500 ms of crossing the threshold.
- **SC-010**: When a ChannelMember entry has a custom display name set for the current context, 100% of that member's overlay appearances show the custom name string (including any inline emoji). When the avatar toggle is off the Discord avatar column is empty but the name remains horizontally aligned with all other members. Switching to a context with no custom values reverts to Discord display name and avatar immediately.

## Assumptions

- Discord's local RPC/IPC interface is used for integration; no server-side Discord API calls are required solely for voice activity detection.
- A project-owned Discord application is registered once by the maintainer; its `client_id` is bundled in the binary. The resulting OAuth token (not the `client_id`) is the secret and is protected by Windows Credential Manager per FR-002.
- The application targets Windows 10/11 desktop and follows the same WPF + WinForms (NotifyIcon) pattern established by WheelOverlay.
- The OverlayCore shared library is used for LogService, ThemeService, and other shared infrastructure without modification to OverlayCore.
- Discord avatars are required in v0.1.0. Guild-specific avatar is preferred; global Discord avatar is the fallback. If neither is available or the CDN request fails, the avatar column is left empty.
- Only the single voice channel the user is currently in is shown; multi-guild simultaneous monitoring is out of scope for v0.1.0.
- The Discord client must be running on the same machine; remote or web-app Discord is out of scope.
- ChannelContext and ChannelMember records are managed exclusively through the SpeakerSight settings panel; no import/export or sync mechanism is required in v0.1.0.
- Member visual identifiers use the Discord-provided avatar (fetched via the local IPC connection) and/or a user-set Unicode emoji; no local custom icon files are supported in v0.1.0.

## Clarifications

### Session 2026-03-30

- Q: How should the Discord auth token be persisted between launches? → A: Windows Credential Manager (DPAPI-encrypted); token must never be stored in plain text.
- Q: Should the overlay show only speaking participants or all voice channel members? → A: User-configurable via a settings toggle; default is speakers-only. All-members mode shows every channel participant with active speakers visually distinguished.
- Q: How should the overlay behave when a speaker stops talking — remove immediately or linger? → A: Configurable grace period (default 2s, range 0–2s) with dim/fade animation. Resuming speech during the grace period resets to full intensity immediately.
- Q: How should the overlay handle exclusive fullscreen games? → A: Match WheelOverlay pattern — always-on-top + WS_EX_TRANSPARENT click-through only; exclusive fullscreen (DirectX fullscreen exclusive) is unsupported. Users must run games in borderless windowed mode.
- Q: How should disconnection/retry state be communicated to the user? → A: Tray icon changes to amber (Retrying) or red (Failed/unrecoverable); overlay shows a light-colored status text indicator ("— retrying" / "— failed", wording TBD). Retry is indefinite for recoverable IPC drops; Failed state reserved for non-recoverable errors (e.g., auth revoked).
- Q: What identity key should be used to anchor member aliases? → A: Discord numeric user ID (snowflake) — permanent, survives all renames. Settings UI displays human-readable name alongside the stored ID.
- Q: How does the user create a member alias — manual entry, pick from list, or paste ID? → A: Auto-population model: a ChannelContext record is created each time the user enters a guild+channel; members are added automatically as ChannelMember entries (snowflake ID + default Discord name) as they are observed. Custom name and icon fields are available inline in settings for each observed member.
- Q: What format should custom member icons use? → A: No custom icon files. Discord avatar (via IPC) is used with a per-ChannelMember visibility toggle (default on). Emoji are inline characters within the custom name field, not a separate field.
- Q: Where should emoji and avatar appear relative to the member name? → A: Fixed two-column layout — avatar column (fixed width, left) and name column (right). Names always align regardless of avatar visibility; avatar-off entries show empty space in the avatar column. Emoji are inline within the name text string.
- Q: Should ChannelContext records be pruned automatically or kept indefinitely? → A: Retained indefinitely; settings panel provides manual delete per context (with confirmation prompt); deletion removes the context and all its ChannelMember entries permanently.
- Q: How should the Discord OAuth `client_id` be sourced — bundled, user-supplied, or deferred? → A: Bundle a pre-registered project-owned Discord application `client_id` in the app binary. Users see only the Discord authorization prompt; no developer registration or manual credential entry is required. The OAuth token produced by the flow is the only secret and is stored in Windows Credential Manager.
- Q: How many active speakers should the overlay display before truncating with a `+N more` indicator? → A: 8 speakers. Fixed cap for v0.1.0; not user-configurable.
- Q: Does SpeakerSight v0.1.0 require an installer? → A: Yes — WiX 4 MSI matching the WheelOverlay pattern (`installers/speakersight/`). The output MSI filename MUST include the version number (e.g. `SpeakerSight-v0.1.0.msi`). All new overlays follow this same installer process.
- Q: What should the overlay status indicator text read during Retrying and Failed connection states (FR-010b)? → A: Retrying state: "⟳ Reconnecting…"; Failed state: "✕ Disconnected". Both are removed immediately on reconnection.
