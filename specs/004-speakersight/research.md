# Phase 0 Research: SpeakerSight

**Date**: 2026-03-31 | **Branch**: `speakersight/v0.1.0`

All unknowns from the Technical Context have been resolved. This document is the authoritative record of design decisions; see `plan.md` for how they are applied.

---

## R-001: Discord Local IPC/RPC Transport

**Decision**: Use `NamedPipeClientStream` (BCL `System.IO.Pipes`) over the `\\.\pipe\discord-ipc-{N}` named pipe. Probe slots `discord-ipc-0` through `discord-ipc-9` in order; use the first that connects successfully within 500 ms.

**Protocol**: Every IPC frame is a single atomic write:
```
[opcode: uint32 LE][length: uint32 LE][JSON payload: UTF-8]
```
The entire buffer must be written in a single `Write`/`WriteAsync` call — multiple stream writes corrupt the frame.

| Opcode | Name | Direction | Purpose |
|--------|------|-----------|---------|
| 0 | HANDSHAKE | Client → Discord | First packet; `{"v":1,"client_id":"..."}` |
| 1 | FRAME | Bidirectional | All commands and DISPATCH events |
| 2 | CLOSE | Discord → Client | Graceful disconnect |
| 3 | PING | Discord → Client | Keepalive — must reply with PONG (opcode 4) |
| 4 | PONG | Client → Discord | Reply to PING |

**Connection sequence**:
1. `new NamedPipeClientStream(".", "discord-ipc-{N}", PipeDirection.InOut, PipeOptions.Asynchronous)`
2. `await pipe.ConnectAsync(timeout: 500ms, ct)` — fast-fail on absent slots
3. Send opcode-0 HANDSHAKE → Discord replies opcode-1 FRAME / DISPATCH READY
4. All subsequent traffic uses opcode 1

**Rationale**: Named pipe IPC is the only mechanism Discord exposes to unsigned desktop apps. Sub-millisecond latency; works offline; no internet required for the overlay once authenticated.

**Alternatives considered**:
- WebSocket `127.0.0.1:6463–6472` (GameBridge path) — same logical protocol but requires HTTP upgrade; restricted to approved apps only; rejected.
- Discord Social SDK (C++) — voice activity accessible but no .NET native binding; requires game registration; excessive overhead; rejected.

---

## R-002: C# RPC Library

**Decision**: Write a custom `DiscordIpcClient` service in `src/SpeakerSight/Services/`. No NuGet RPC library.

**What `DiscordIpcClient` owns**:
- Pipe connection probe loop (slots 0–9)
- Binary frame encode/decode (`BinaryReader`/`BinaryWriter` + `NamedPipeClientStream`)
- `System.Text.Json` serialization of command/event payloads
- HANDSHAKE → AUTHORIZE → AUTHENTICATE sequence
- SUBSCRIBE / UNSUBSCRIBE command helpers
- Background async read loop dispatching `JsonElement` to typed event handlers
- PING/PONG keepalive response

**Rationale**: No existing .NET NuGet package implements the full AUTHORIZE → AUTHENTICATE → SUBSCRIBE voice event path. `Lachee/discord-rpc-csharp` (`DiscordRichPresence` NuGet v1.6.1.70) is Rich Presence only — it does not expose AUTHORIZE, GET_SELECTED_VOICE_CHANNEL, or voice activity events. Its `ManagedNamedPipeClient.cs` serves as a reference for the read loop.

**Alternatives considered**:
- `Lachee/discord-rpc-csharp` — Rich Presence only; cannot be used; rejected.
- Fork of Lachee's library — architecture does not expose low-level command/event interface; rejected.
- `discordjs/RPC` (JS), `jagrosh/DiscordIPC` (Java) — wrong language; rejected.

---

## R-003: OAuth2 Scopes and Discord Approval Gate

**Decision**: Request scopes `["rpc", "rpc.voice.read", "identify"]`.

| Scope | Freely Available | What It Unlocks |
|-------|-----------------|-----------------|
| `rpc` | **Restricted — Discord approval required** | Core IPC command access |
| `rpc.voice.read` | **Restricted — Discord approval required** | `SPEAKING_START`, `SPEAKING_STOP`, `VOICE_STATE_*`, `GET_SELECTED_VOICE_CHANNEL` |
| `identify` | Freely available | User identity |

**Critical finding**: `rpc` and `rpc.voice.read` are in Discord's private beta. As of 2026-04, only four apps have ever been approved: Overlayed (905987126099836938), Reactive Images (794365445557846066), Discord Streamkit (207646673902501888), and Streamdeck (1267111501219627119). No new approvals have been granted since approximately 2022. Discord has not formally closed the program but has stopped accepting applications without announcement. Full public approval must be obtained before broad distribution. This is a **pre-launch gate**, not a build blocker.

**User consent model**: RPC OAuth consent is an **individual user action** — Discord shows an in-app consent modal when the user first launches the app. The user clicks Authorize. No guild/server administrator involvement at any point. This is distinct from bot invites, which require Manage Server permission.

**50-slot developer tester whitelist**: Available immediately without Discord approval. Located in Developer Portal → App → **App Testers** tab. Add up to 49 other Discord usernames; they receive an email invitation, then must enable **Application Test Mode** in Discord Settings → Advanced and paste the App ID. Covers development and early community testing with zero guild admin friction.

**Strategy**: Build fully against the 50-tester whitelist. Submit the public approval request early via Discord Developer Support ticket (https://support-dev.discord.com/hc/en-us/requests/new) — do not wait until the app is complete. Frame the request around: open-source (AGPL), sim racing niche not served by existing approved apps, no competition with Discord's own products. Reference community discussion at discord/discord-api-docs#4409 for framing precedent.

**Workaround assessment**: No viable workaround exists for real-time `SPEAKING_START`/`SPEAKING_STOP` without `rpc.voice.read`. All alternatives were fully evaluated and rejected:
- **Discord bot (voice WebSocket SPEAKING events)**: Bot must be invited to each guild by someone with Manage Server permission — a non-starter for broader community distribution.
- **Discord Activity (Embedded App SDK)**: Activity iframe CSP blocks connections to `127.0.0.1`; Discord's `patchUrlMappings` proxy routes through Discord's cloud servers which cannot reach local ports. Dead end for local IPC.
- **WASAPI audio metering**: Discord mixes all voice channel participants into a single WASAPI session — per-user speaking detection is not possible.
- **Windows UI Automation**: Discord's Electron app does not expose speaking state in its accessibility tree.
- **Discord Gateway API (bot)**: `VOICE_STATE_UPDATE` events cover join/leave/mute only — real-time speaking events (SPEAKING opcode 5) are only available on the voice WebSocket to connected voice clients, not through the main gateway.
- **Polling `GET_SELECTED_VOICE_CHANNEL`**: Violates SC-002 (500 ms latency); rejected.
- **Implicit IPC auth**: Discord notes some scopes granted implicitly, but `rpc.voice.read` is not among them; rejected.

---

## R-004: OAuth2 Client Secret Handling

**Decision**: Enable `PUBLIC_OAUTH2_CLIENT` flag in the Discord Developer Portal and use PKCE. No `client_secret` is embedded in the binary.

**PKCE flow**:
1. Generate `code_verifier` (32 random bytes, base64url-encoded) on each AUTHORIZE call
2. Compute `code_challenge = base64url(SHA-256(code_verifier))`
3. Send AUTHORIZE IPC command → Discord shows in-app consent dialog → returns `code`
4. POST to `https://discord.com/api/oauth2/token` with `code_verifier` (no `client_secret`)
5. Response: `access_token`, `refresh_token`, `expires_in` (604800 = 7 days), `token_type`
6. Store token bundle in Windows Credential Manager (see R-006)

**`client_id` in binary**: Safe — `client_id` is a public identifier, not a secret.

**Token refresh**: Check `expiry_utc` on every launch. If expired (or within 1 hour of expiry), exchange `refresh_token` for a new pair before attempting AUTHENTICATE. Discord issues a new `refresh_token` on every refresh — old one immediately invalid. Store the new pair atomically.

**Rationale**: PKCE eliminates the embedded `client_secret` risk. The `refresh_token` (the real long-lived credential) never leaves Windows Credential Manager.

**Alternatives considered**:
- Embed `client_secret` (obfuscated) — extractable by any user; open-source project cannot rely on obfuscation; rejected.
- Server-side proxy for token exchange — requires infrastructure; excessive for a desktop overlay; rejected.

---

## R-005: Voice Events and Subscription Sequence

**Decision**: Subscribe to seven events after authentication.

**Global subscriptions** (no `channel_id` arg — subscribe once after AUTHENTICATE):
```jsonc
{ "cmd": "SUBSCRIBE", "evt": "VOICE_CHANNEL_SELECT",   "args": {} }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_CONNECTION_STATUS", "args": {} }
```

**Channel-scoped subscriptions** (re-subscribe each time `VOICE_CHANNEL_SELECT` fires):
```jsonc
{ "cmd": "SUBSCRIBE", "evt": "SPEAKING_START",      "args": { "channel_id": "<id>" } }
{ "cmd": "SUBSCRIBE", "evt": "SPEAKING_STOP",       "args": { "channel_id": "<id>" } }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_STATE_CREATE",  "args": { "channel_id": "<id>" } }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_STATE_UPDATE",  "args": { "channel_id": "<id>" } }
{ "cmd": "SUBSCRIBE", "evt": "VOICE_STATE_DELETE",  "args": { "channel_id": "<id>" } }
```

**Bootstrap** (call once after AUTHENTICATE to seed initial state):
```jsonc
{ "cmd": "GET_SELECTED_VOICE_CHANNEL", "args": {} }
```
Returns full channel + participant list, or `null` if not in a voice channel.

**`SPEAKING_START`/`STOP` DISPATCH payload**:
```json
{ "cmd": "DISPATCH", "evt": "SPEAKING_START", "data": { "user_id": "190320984123768832" } }
```

**`VOICE_STATE_UPDATE` DISPATCH payload** (contains display name):
```json
{
  "cmd": "DISPATCH", "evt": "VOICE_STATE_UPDATE",
  "data": {
    "voice_state": { "mute": false, "self_mute": false, "self_deaf": false },
    "user": { "id": "190320984123768832", "username": "User", "avatar": "hash" },
    "nick": "Display Name in Guild"
  }
}
```

**Rationale**: `SPEAKING_START`/`STOP` provide direct, sub-100 ms speaking notifications. `VOICE_STATE_*` events manage the member roster. `VOICE_CHANNEL_SELECT` handles the user switching channels. `GET_SELECTED_VOICE_CHANNEL` bootstraps initial state on connect.

---

## R-006: Windows Credential Manager Token Storage

**Decision**: `Meziantou.Framework.Win32.CredentialManager` NuGet v1.7.17.

**API usage**:
```csharp
// Write token bundle
CredentialManager.WriteCredential(
    applicationName: "SpeakerSight",
    userName: "oauth2_token_bundle",
    secret: JsonSerializer.Serialize(tokenBundle),   // { access_token, refresh_token, expiry_utc }
    persistence: CredentialPersistence.LocalMachine);

// Read
var cred = CredentialManager.ReadCredential("SpeakerSight");
// cred?.Password → deserialize to token bundle

// Delete (on revocation or Disconnect action)
CredentialManager.DeleteCredential("SpeakerSight");
```

**Stored payload** (JSON in the `secret`/Password field):
```json
{
  "access_token": "...",
  "refresh_token": "...",
  "expiry_utc": "2026-04-07T14:00:00Z"
}
```

**`CredentialPersistence.LocalMachine`**: Survives reboots; current Windows user only; DPAPI-encrypted.

**Rationale**: Meziantou's package is actively maintained with .NET 10 support. Avoids raw P/Invoke boilerplate. Windows-only is acceptable for `net10.0-windows`.

**Alternatives considered**:
- `Windows.Security.Credentials.PasswordVault` (WinRT) — UWP/packaged apps only; not usable in unpackaged WPF; rejected.
- Raw P/Invoke to `advapi32.dll` `CredWrite`/`CredRead` — equivalent result, more boilerplate; rejected.
- `AdysTech/CredentialManager` NuGet — less actively maintained; rejected.
- DPAPI (`ProtectedData`) to file — not visible in Credential Manager UI; harder to audit; rejected.

---

## R-007: Exponential Backoff Reconnect

**Decision**: Custom async retry loop with full-jitter exponential backoff, capped at 64 seconds. No external library.

**Algorithm**:
```
slots = [0s (immediate), 2s, 4s, 8s, 16s, 32s, 64s]
delay  = random(0, slots[min(attempt, 6)] × 1.5)   // full jitter
```

**Behavior**:
- Attempt 0: immediate (satisfies SC-006's 10-second recovery window for common single-drop case)
- Subsequent: full-jitter delay from slot table
- Non-recoverable (error 4006 = auth revoked) → transition to `Failed`, stop loop, surface re-auth prompt
- Each attempt creates a `new NamedPipeClientStream` — closed pipes cannot be reopened
- Each attempt probes all 9 slots (Discord may restart into a different slot)
- Must re-AUTHENTICATE and re-SUBSCRIBE on every successful reconnect — subscriptions are per-connection

**Rate limit compliance**: Discord IPC limit is 2 connections/minute (~30 s minimum). Cap of 64 s with jitter gives ample headroom.

**State transitions**:
```
Connected ──pipe drop──► Retrying ──success──► Connected
Retrying  ──auth revoked──► Failed
Failed    ──user clicks re-authorize──► Connecting ──► Connected
```

**Rationale**: 20 lines of owned code vs. a Polly dependency dragged in for a named-pipe use case. The spec's `Connected/Retrying/Failed` state machine (FR-010a–c) maps directly.

**Alternatives considered**:
- Polly `ResiliencePipeline` — adds NuGet dependency for trivial logic; rejected.
- Fixed 5 s retry — too slow for first attempt per SC-006; rejected.
- No-jitter exponential — thundering-herd if multiple users restart Discord simultaneously; rejected.

---

## R-008: WPF Rendering for Unicode / Emoji Names

**Decision**: Standard WPF `TextBlock` with `TextOptions.TextFormattingMode="Display"` handles all Unicode including emoji and non-Latin scripts without special treatment.

**Fixed-width avatar column**: 32 px `ColumnDefinition Width="32"` in a two-column `Grid`. When avatar is hidden (`Visibility.Hidden`, not `Collapsed`), the column retains its width so names remain horizontally aligned (FR-014b-layout).

**Font**: Segoe UI (Windows default) supports full Unicode BMP and most supplementary characters. System fall-back font handling covers non-BMP emoji.

**Rationale**: WPF's built-in text rendering meets SC-008 (Unicode without garbling). No additional text-shaping library needed.

---

## R-009: Out-of-Bounds Position Correction

**Decision**: On startup, load saved `WindowLeft`/`WindowTop` from `AppSettings`. Call a `ScreenBoundsHelper.Clamp(position, window)` static method that checks all `Screen.AllScreens` (WinForms) and snaps to the nearest monitor's working area if out-of-bounds.

**Algorithm**:
```csharp
bool anyMonitorContains = Screen.AllScreens
    .Any(s => s.WorkingArea.Contains((int)left, (int)top));
if (!anyMonitorContains)
{
    var primary = Screen.PrimaryScreen.WorkingArea;
    return (primary.Left + 20.0, primary.Top + 20.0);
}
```

**Rationale**: Simple, no extra dependency. Matches what WheelOverlay does implicitly. Satisfies FR-012.

---

## Summary Decision Table

| Topic | Decision | Package/API |
|-------|----------|-------------|
| IPC transport | `NamedPipeClientStream`, probe slots 0–9 | `System.IO.Pipes` (BCL) |
| RPC library | Custom `DiscordIpcClient` | `System.Text.Json` (BCL) |
| OAuth2 scopes | `rpc`, `rpc.voice.read`, `identify` | Discord approval gate |
| Client secret | PKCE + `PUBLIC_OAUTH2_CLIENT` | `HttpClient` (BCL) |
| Token storage | Windows Credential Manager | `Meziantou.Framework.Win32.CredentialManager` v1.7.17 |
| Voice events | `SPEAKING_START/STOP` + `VOICE_STATE_*` + `VOICE_CHANNEL_SELECT` | IPC SUBSCRIBE |
| Reconnect | Custom full-jitter backoff, cap 64 s | No external lib |
| Unicode rendering | WPF `TextBlock` | Built-in |
| Position correction | `Screen.AllScreens` clamp | WinForms (BCL) |

## Open Project Gate

**Discord `rpc.voice.read` approval** must be obtained before public distribution. Development and early community testing (up to 50 users) use the developer tester whitelist — no guild admin involvement required, purely individual user consent.

**Approval request**: Submit a developer support ticket at https://support-dev.discord.com/hc/en-us/requests/new as early as possible — do not wait for the app to be complete. Only four apps have ever been approved; approval is not guaranteed. The 50-tester whitelist is the distribution ceiling until approval is granted.

**Tester setup**: Developer Portal → App → App Testers tab. Testers accept an email invite, then enable Application Test Mode in Discord Settings → Advanced and enter the App ID. One-time setup per tester.
