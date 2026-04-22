---
title: "Getting Started"
description: "Install SpeakerSight, authorize with Discord, and start using the overlay"
---

# Getting Started

## Prerequisites

- Windows 10 (1903+) or Windows 11, 64-bit
- Discord desktop app installed, running, and signed in to an account

## Installation

1. Download `SpeakerSight-v0.1.0.msi` from the [GitHub Releases](https://github.com/gavincadams/opendash-overlays/releases) page
2. Run the installer — it will install to `%ProgramFiles%\OpenDash\SpeakerSight\`
3. A Start Menu shortcut and desktop shortcut are created automatically

The installer bundles the .NET 10 runtime; no separate runtime installation is required.

## First Launch

1. Launch **SpeakerSight** from the Start Menu or desktop shortcut
2. The app starts minimized to the system tray (look for the overlay icon near the clock)
3. On first launch a Discord authorization dialog appears — **click Authorize** inside Discord to grant access
   - The app requests the `rpc`, `rpc.voice.read`, `identify`, and `guilds` scopes
   - No message access, no server management permissions are requested
4. Once authorized, the tray icon turns to its normal color (connected state)
5. **Join a Discord voice channel** — the overlay appears automatically showing active speakers

## First-Run Authorization Flow

```
App starts
  └─► Connects to Discord IPC pipe
        └─► No stored token found
              └─► Discord shows authorization dialog
                    └─► You click Authorize
                          └─► Token stored in Windows Credential Manager
                                └─► Overlay is live
```

On subsequent launches the stored token is reused — no authorization prompt appears unless you revoke access inside Discord or click **Disconnect** in the app's Connection settings.

## Quick Tour

### System Tray Menu

Right-click the tray icon to access:

| Menu Item | Action |
|---|---|
| Show Overlay | Makes the overlay window visible |
| Hide Overlay | Hides the overlay (app keeps running) |
| Settings | Opens the settings window |
| Exit | Closes the app completely |

Double-click the tray icon to toggle overlay visibility.

### Tray Icon Colors

| Color | Meaning |
|---|---|
| Normal | Connected to Discord |
| Amber | Reconnecting (exponential back-off in progress) |
| Red | Disconnected — authorization required |

### Overlay

The overlay appears in the top-left of your primary monitor by default. It shows:

- **Speaker names** — displayed name (or custom alias if configured) with opacity reflecting speaking state
- **Connection indicator** — shown only when not connected (`⟳ Reconnecting…` or `✕ Disconnected`)
- **Overflow count** — `+N more` when more than 8 speakers are active simultaneously

The overlay is fully transparent and click-through when the settings window is closed, so it never interferes with games or other apps.

### Settings Window

Open Settings from the tray menu. The settings window has five categories:

- **Connection** — connection status and re-authorization controls
- **Display** — display mode, grace period, debounce threshold
- **Appearance** — position, opacity, theme, font size
- **Aliases** — per-channel custom display names and avatar visibility
- **About** — version information and project links

See [Settings Reference](settings.md) for full details on every field.

## Repositioning the Overlay

1. Open **Settings → Appearance**
2. Adjust the **Window Left** and **Window Top** fields — the overlay moves live as you type
3. Click **Save** to persist the new position

The overlay can also be repositioned by dragging it directly while the settings window is open.

## Revoking and Re-authorizing

To disconnect the app from your Discord account:

1. Open **Settings → Connection**
2. Click **Disconnect** — the stored token is deleted and the IPC connection is closed
3. To reconnect, click **Re-authorize** and follow the Discord prompt

Alternatively, revoke access inside Discord (**User Settings → Authorized Apps → SpeakerSight → Deauthorize**) — the app will detect the revocation and update the tray icon to red.
