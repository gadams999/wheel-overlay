---
title: "Troubleshooting"
description: "Common SpeakerSight errors and how to resolve them"
---

# Troubleshooting

## Overlay Does Not Appear

**Symptoms**: App starts (tray icon visible) but no overlay is shown when you join a voice channel.

**Causes and fixes**:

1. **Not in a voice channel** — the overlay only appears while you are actively connected to a Discord voice channel. Join a channel and check again.
2. **Overlay is hidden** — right-click the tray icon and select **Show Overlay**.
3. **Overlay is off-screen** — if you previously moved the overlay to a monitor that is no longer connected, the position resets to `(20, 20)` on the primary monitor. Open **Settings → Appearance** and check/reset the position.
4. **Opacity set too low** — if the window opacity is near `10%`, the overlay may be invisible against your background. Open **Settings → Appearance** and raise the Opacity slider.

---

## Tray Icon is Amber ("Reconnecting")

**Symptoms**: The tray icon is amber and the overlay shows `⟳ Reconnecting…`.

**Cause**: The app lost its connection to the Discord IPC pipe and is trying to reconnect using exponential back-off.

**Fixes**:

1. **Discord not running** — make sure Discord is open and fully loaded (not just the system tray icon in a crashed state). Restart Discord if needed.
2. **Discord update in progress** — Discord briefly closes its IPC pipe during self-updates. Wait 30–60 seconds; the app reconnects automatically.
3. **App ran before Discord started** — the app retries up to the back-off ceiling (64 s). Once Discord is running, the next retry attempt will succeed.

If the app stays in Retrying state for more than a few minutes with Discord running, check the log file for errors:

```
%APPDATA%\SpeakerSight\logs.txt
```

---

## Tray Icon is Red ("Disconnected — Authorization Required")

**Symptoms**: The tray icon is red and the overlay shows `✕ Disconnected`.

**Cause**: The OAuth token was revoked — either by you in Discord's Authorized Apps settings, or by clicking **Disconnect** in the app's Connection settings.

**Fix**:

1. Open **Settings → Connection**
2. Click **Re-authorize**
3. In the Discord dialog that appears, click **Authorize**
4. The tray icon returns to normal and the overlay reconnects

---

## Authorization Prompt Does Not Appear in Discord

**Symptoms**: You clicked Re-authorize but no dialog appeared in Discord.

**Fixes**:

1. Bring Discord to the foreground — the authorization dialog may be behind other windows.
2. Make sure Discord is signed in to an account (not on the login screen).
3. If Discord is in a crashed state, fully restart it (right-click the tray icon → Quit Discord, then relaunch).

---

## "Not Whitelisted" or Discord Rejects the Authorization

**Symptoms**: Discord shows an error like "This application has not been approved" or the authorization fails immediately.

**Cause**: The `rpc.voice.read` scope requires Discord developer whitelisting for public distribution. During the v0.1.0 private testing phase, only accounts on the developer tester allowlist (up to 50 slots) can authorize successfully.

The full set of scopes the app requests:

| Scope | Purpose | Whitelist required? |
|---|---|---|
| `rpc` | Connect to Discord IPC pipe | No |
| `rpc.voice.read` | Read voice channel membership and speaking events | **Yes** |
| `identify` | Read the authorized user's profile | No |
| `guilds` | Read guild (server) names for display in the overlay | No |

**Fix**: Contact the project maintainer to be added to the tester allowlist. This limitation will be resolved once Discord approves the `rpc.voice.read` scope for the application.

---

## Names Are Not Appearing on the Overlay

**Symptoms**: You are in a voice channel, someone is speaking, but their name does not appear.

**Causes and fixes**:

1. **Noise gate threshold too high** — if the **Noise gate** setting is 500 ms or more, short bursts of speech are suppressed. Lower the threshold in **Settings → Display**.
2. **Display mode set to Speakers Only with nobody actively speaking** — if participants are in the channel but silent, they are hidden in Speakers Only mode. Switch to **All Members** mode to see everyone.
3. **IPC subscription dropped** — if the overlay was working and stopped without any connection indicator, try restarting the app. Check `logs.txt` for subscription errors.

---

## Custom Alias Not Showing on Overlay

**Symptoms**: You set a custom name in **Settings → Aliases** and saved, but the overlay still shows the Discord name.

**Fixes**:

1. Confirm you clicked **Save** in the Aliases panel (the save is per-panel, not global).
2. Custom names take effect on the next speaking event — the alias is resolved each time a `SPEAKING_START` event fires. Have the member speak again.
3. Verify the alias was saved by reopening **Settings → Aliases** and confirming the custom name is still populated.

---

## Inspecting Stored Tokens (Windows Credential Manager)

The app stores its OAuth token under the name `SpeakerSight` in Windows Credential Manager.

To inspect or manually delete it:

1. Open **Control Panel → Credential Manager → Windows Credentials**
2. Look for an entry named `SpeakerSight`
3. Expand it to see the stored credential; click **Remove** to delete it

After manual deletion, relaunch the app and follow the authorization flow again.

---

## Checking the Log File

All errors, warnings, and connection events are written to:

```
%APPDATA%\SpeakerSight\logs.txt
```

The log rotates at 1 MB. Entries are prefixed with timestamp and severity (`[INFO]`, `[WARN]`, `[ERROR]`). Share the log file when reporting bugs.

---

## Reconnect Behaviour Explained

The app uses exponential back-off with full jitter when Discord disconnects unexpectedly:

| Attempt | Base delay |
|---|---|
| 1 | Immediate |
| 2 | Up to 2 s |
| 3 | Up to 4 s |
| 4 | Up to 8 s |
| 5 | Up to 16 s |
| 6 | Up to 32 s |
| 7+ | Up to 64 s |

Full jitter means the actual delay is a random value between 0 and the base delay, preventing thundering-herd reconnects if many instances are running. The loop continues indefinitely until either a connection succeeds or Discord revokes the token (which triggers the Failed state and stops retrying).
