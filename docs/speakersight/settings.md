---
title: "Settings Reference"
description: "All SpeakerSight settings fields explained with defaults and valid ranges"
---

# Settings Reference

All settings are stored in `%APPDATA%\SpeakerSight\settings.json` and loaded automatically on startup. Changes made in the settings window take effect immediately (live preview) and are written to disk on **Save**.

---

## Connection

Accessible via **Settings → Connection**.

| Field | Description |
|---|---|
| Status | Read-only. Shows current connection state: `Connected`, `Retrying (attempt N)`, or `Disconnected — authorization required` |
| Re-authorize | Triggers a fresh OAuth2 PKCE authorization flow. Opens a Discord prompt; click Authorize inside Discord. |
| Disconnect | Deletes the stored token from Windows Credential Manager and closes the IPC connection. |

The app reconnects automatically using exponential back-off (delays: 0 s, 2 s, 4 s, 8 s, 16 s, 32 s, 64 s with full jitter) when Discord closes or the connection drops. Re-authorization is only required if you explicitly disconnect or Discord revokes the token.

---

## Display

Accessible via **Settings → Display**.

### Display Mode

| Value | Default | Description |
|---|---|---|
| Speakers Only | ✓ | Shows only participants who are currently speaking or recently spoke (Active + RecentlyActive states). Silent members are hidden. |
| All Members | | Shows all voice channel members, including those who are not speaking. Silent members appear below active/fading speakers. |

### Fade Duration (Grace Period)

| Field | Default | Range | Description |
|---|---|---|---|
| Fade duration (seconds) | `2.0` | `0.0` – `2.0` | How long a speaker's name remains visible after they stop talking. At `0.0` names disappear immediately. The opacity fades linearly over this period at ~30 fps. |

### Noise Gate (Debounce Threshold)

| Field | Default | Range | Description |
|---|---|---|---|
| Noise gate (ms, 0 = disabled) | `200` | `0` – `1000` | Leading-edge debounce. A speaking event shorter than this threshold is ignored and the name is never shown. Set to `0` to disable — names appear immediately on any voice activity. Useful for suppressing brief keyboard clicks or mic noise. |

---

## Appearance

Accessible via **Settings → Appearance**. All changes preview live on the overlay before you save.

### Position

| Field | Default | Description |
|---|---|---|
| Window Left | `20.0` | Horizontal position of the overlay window in screen pixels from the left edge. If the stored position is outside all monitor working areas, it resets to `20.0` on the primary monitor. |
| Window Top | `20.0` | Vertical position of the overlay window in screen pixels from the top edge. Same out-of-bounds reset behaviour as Window Left. |

You can also drag the overlay directly while the settings window is open.

### Opacity

| Field | Default | Range | Description |
|---|---|---|---|
| Opacity | `90` | `10` – `100` | Overall opacity of the overlay window as a percentage. At `100` the overlay is fully opaque; at `10` it is nearly invisible. This is the window-level opacity — individual speakers also fade based on their speaking state. |

### Theme

| Value | Default | Description |
|---|---|---|
| System | ✓ | Follows Windows light/dark mode setting. |
| Dark | | Always uses the dark Material Design theme regardless of system setting. |
| Light | | Always uses the light Material Design theme. |

### Font Size

| Field | Default | Range | Description |
|---|---|---|---|
| Font size | `14` | `8` – `32` | Font size in points for speaker names displayed on the overlay. |

---

## Aliases

Accessible via **Settings → Aliases**.

The Aliases panel shows all voice channel contexts (guild + channel combinations) that the app has seen you join. For each context, the members who were present in that channel are listed.

### Per-Member Fields

| Field | Description |
|---|---|
| Discord name (reference) | Read-only. The Discord display name last seen for this member in this context. Updated automatically each time they appear in the channel. |
| Custom display name | Optional. If set, this name is shown on the overlay instead of their Discord display name. Accepts any Unicode text up to 100 characters. Leave blank to use the Discord name. |
| Show avatar | When checked (default), the member's avatar is shown on the overlay. Uncheck to hide the avatar — the name column expands to fill the space. |

### Delete Context

Each context has a **Delete Context** button. This removes the context and all its member records from `aliases.json`. The context will be recreated automatically the next time you join that channel. Custom display names in the deleted context are lost.

### Saving

Click **Save** to write all changes. Alias changes take effect immediately for the next speaking event — no restart required.

---

## About

Accessible via **Settings → About**.

Shows the current application version (read from the assembly) and links to the project GitHub page.

---

## Settings File Location

```
%APPDATA%\SpeakerSight\settings.json
```

Settings are written atomically (write to a temp file, then rename) to prevent corruption on unexpected exit.

## Aliases File Location

```
%APPDATA%\SpeakerSight\aliases.json
```

Same atomic-write pattern as settings. Malformed entries are skipped and logged; the file is never fully rejected due to a single bad record.

## Log File Location

```
%APPDATA%\SpeakerSight\logs.txt
```

Log file rotates at 1 MB. Contains startup events, connection state transitions, IPC errors, and any settings/aliases parse warnings.
