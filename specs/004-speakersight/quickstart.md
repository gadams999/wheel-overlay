# Developer Quickstart: SpeakerSight

**Branch**: `speakersight/v0.1.0`

This guide gets a developer building and running `SpeakerSight` locally. It assumes familiarity with the monorepo and WheelOverlay patterns.

---

## Prerequisites

- .NET 10 SDK (`dotnet --version` ≥ 10.0)
- Visual Studio 2022 17.12+ or Rider with .NET 10 support
- Discord desktop client installed and running locally
- A Discord application registered at the Developer Portal with:
  - `PUBLIC_OAUTH2_CLIENT` flag enabled (no client_secret needed)
  - Your developer account added as a tester (50-slot whitelist)
  - `rpc` and `rpc.voice.read` scopes requested (approval required for public distribution; tester whitelist covers local dev)
- WiX Toolset 4.0.5 (for MSI builds only; not required for development)

---

## 1. Project Setup

The project follows the standard monorepo layout. No new global tooling is required beyond the .NET SDK.

```bash
# From the repo root
dotnet build OpenDash-Overlays.sln
```

This builds `OverlayCore`, `SpeakerSight`, and all test projects. The `SpeakerSight` app is in `src/SpeakerSight/`.

---

## 2. Configuration: Bundle the `client_id`

The Discord `client_id` is a public identifier — it is safe in the binary. It must be set before building.

In `src/SpeakerSight/Services/DiscordIpcClient.cs`, find:

```csharp
private const string ClientId = "YOUR_CLIENT_ID_HERE";
```

Replace with the project-owned Discord application `client_id` from the Developer Portal. This constant is committed — it is not a secret.

---

## 3. First Run: Authorization Flow

1. Build and run `SpeakerSight` (Debug or Release).
2. The app starts minimized to the system tray.
3. If no stored token is found, the app sends an AUTHORIZE command to the local Discord IPC. Discord shows its in-app OAuth2 consent dialog.
4. The user approves. `SpeakerSight` exchanges the code for tokens (PKCE, no client_secret) and stores them in Windows Credential Manager under `"SpeakerSight"`.
5. The tray icon turns to its normal color — the app is connected and authenticated.
6. Join a Discord voice channel. The overlay appears with active speakers.

On subsequent launches, the stored token is used automatically (AUTHENTICATE without AUTHORIZE). Tokens are silently refreshed when within 1 hour of expiry.

---

## 4. Settings

Right-click the tray icon → **Settings** to open the settings panel. Categories:

| Category | Contents |
|----------|----------|
| Connection | Auth status, Re-authorize button, Disconnect |
| Display | Display mode (speakers-only / all-members), grace period, debounce threshold |
| Appearance | Position, opacity, color theme, font size |
| Aliases | Per-channel custom member names and avatar toggles |
| About | Version, links |

Settings are saved to `%APPDATA%\SpeakerSight\settings.json`.
Member aliases are saved to `%APPDATA%\SpeakerSight\aliases.json`.

---

## 5. Running Tests

```bash
# Fast mode (10 PBT iterations — use for development)
dotnet test --configuration FastTests

# Release mode (100 PBT iterations — required before PR)
dotnet test --configuration Release
```

Validate property test directives before committing:
```bash
powershell -File scripts/Validate-PropertyTests.ps1
```

---

## 6. Key Files

| File | Purpose |
|------|---------|
| `src/SpeakerSight/Services/DiscordIpcClient.cs` | Named-pipe transport, IPC framing, HANDSHAKE/AUTH |
| `src/SpeakerSight/Services/VoiceSessionService.cs` | Speaker state machine, debounce, grace period |
| `src/SpeakerSight/Services/AliasService.cs` | aliases.json CRUD, name resolution |
| `src/SpeakerSight/Services/TokenStorageService.cs` | Credential Manager read/write/delete, PKCE helpers |
| `src/SpeakerSight/Models/AppSettings.cs` | settings.json load/save |
| `src/SpeakerSight/ViewModels/OverlayViewModel.cs` | Drives the overlay UI (ActiveSpeakers, OverflowCount, ConnectionIndicator) |
| `src/SpeakerSight/MainWindow.xaml` | Overlay window (always-on-top, click-through, two-column layout) |

---

## 7. Building the MSI

```bash
powershell -File scripts/speakersight/build_msi.ps1
```

Output: `installers/speakersight/bin/SpeakerSight-v0.1.0.msi`

Requires WiX Toolset 4.0.5. The MSI filename includes the version number (required by project convention).

---

## 8. Debugging IPC

The app logs all IPC traffic at Debug level to `%APPDATA%\SpeakerSight\logs.txt`. To enable verbose logging:

- Ensure Discord is running before starting the app
- The app probes pipe slots `discord-ipc-0` through `discord-ipc-9` on each connection attempt
- Slot `0` = Discord stable, `1` = PTB, `2` = Canary (typical slot assignments)
- Connection drops are handled automatically with exponential backoff (first retry immediate, cap 64 s)

To inspect stored credentials: open **Windows Credential Manager** → **Windows Credentials** → look for `"SpeakerSight"`.

---

## 9. Discord Approval Gate

The `rpc.voice.read` scope is in Discord's private beta. Before public distribution:

1. Register the project Discord application in the Developer Portal
2. Apply for `rpc` and `rpc.voice.read` scope approval
3. During development: add developer accounts as testers (50-slot whitelist)

The app builds and runs fully for whitelisted testers without approval. Approval is required only before public MSI distribution.
