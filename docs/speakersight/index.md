---
title: "SpeakerSight"
description: "An always-on-top overlay that shows who is speaking in your Discord voice channel"
deprecated: false
---

# SpeakerSight

SpeakerSight is a lightweight Windows desktop application that displays an always-on-top, click-through overlay showing active speakers in your current Discord voice channel in real time.

## What it does

- **Shows active speakers** — names appear on screen when someone starts talking and fade out after a configurable grace period when they stop
- **Stays out of your way** — the overlay is click-through and fully transparent, so it never blocks your game or other apps
- **Connects to your local Discord client** — no bots, no server-side integration; it reads voice activity directly from the Discord desktop app via its IPC interface
- **Persists your preferences** — position, opacity, font size, theme, and per-channel aliases are all saved and restored automatically

## Version

**v0.1.0** — Initial release. Supports a single active voice channel, Windows 10/11 only.

## System Requirements

| Requirement | Details |
|---|---|
| OS | Windows 10 (1903+) or Windows 11 |
| Discord | Discord desktop app (stable or PTB), running and signed in |
| .NET | .NET 10 runtime (bundled in MSI installer) |
| Architecture | x64 only |

## Features

- Active speaker overlay with configurable grace-period fade (0–2 s)
- Leading-edge noise gate / debounce (0–1000 ms) to suppress brief audio spikes
- Display mode: speakers-only or all channel members
- Up to 8 simultaneous speakers displayed; overflow count shown as `+N more`
- Connection status indicator on overlay (reconnecting / disconnected states)
- OAuth2 PKCE authentication — tokens stored in Windows Credential Manager
- Automatic reconnect with exponential back-off
- Per-channel member aliases and avatar visibility toggles
- Material Design settings window with live preview
- System tray icon with connection state color coding
- WiX 4 MSI installer

## Next Steps

- [Getting Started](getting-started.md) — install, authorize, and start using the overlay
- [Settings Reference](settings.md) — all settings fields explained
- [Troubleshooting](troubleshooting.md) — common issues and how to resolve them
