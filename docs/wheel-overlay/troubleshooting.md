# Wheel Overlay — Troubleshooting

Solutions for common issues. If you cannot resolve an issue after following these steps, check the log file at `%APPDATA%\WheelOverlay\logs.txt` for detailed error information.

---

## DirectInput Device Not Detected

**Symptom**: The overlay shows no device in the dropdown, the position label never changes when you rotate the encoder, or the device name you expect is missing from the Wheel Device list.

### Check the device is plugged in

1. Open **Device Manager** (right-click Start → Device Manager).
2. Expand **Human Interface Devices** or **USB Input Devices**.
3. Confirm your wheel appears without a warning icon.
4. If the device shows a warning icon, right-click → **Update driver** → **Search automatically**.

### Check that DirectInput drivers are installed

Most sim racing wheels use DirectInput (not XInput). Ensure the vendor driver is installed:

- For BavarianSimTec wheels: install the BavarianSimTec device driver from the vendor's website.
- For other wheels: check your wheel manufacturer's support page for a Windows driver package.

After installing drivers, restart Wheel Overlay.

### Try running as administrator

Some USB HID devices require elevated permissions for DirectInput polling:

1. Right-click the Wheel Overlay shortcut or `.exe` file.
2. Select **Run as administrator**.
3. Confirm the UAC prompt.

If the device appears after running as administrator, you can set this permanently: right-click the shortcut → **Properties** → **Compatibility** → check **Run this program as an administrator**.

### Verify the device name matches

Open **Settings → Display → Wheel Device** and check whether your device appears under a different name. DirectInput devices report their own name string; it may differ from the marketing name on the box. Select the entry that corresponds to your wheel.

---

## Overlay Not Visible

**Symptom**: The app is running (tray icon is present) but you cannot see the overlay window on screen.

### Verify the system tray icon is present

Look for the Wheel Overlay icon in the system tray (bottom-right of taskbar). If you don't see it, click the **^** arrow to check the overflow area. If it's not there at all, the app may have crashed — check `%APPDATA%\WheelOverlay\logs.txt`.

### Check display scaling

Windows display scaling above 100% can shift window positions unexpectedly:

1. Right-click your desktop → **Display settings**.
2. Note your scaling percentage.
3. Open `%APPDATA%\WheelOverlay\settings.json` in Notepad and check the `windowLeft` and `windowTop` values — they may have placed the overlay off-screen on a different monitor layout.
4. Edit `windowLeft` and `windowTop` to `100` and `100`, save, and restart Wheel Overlay.

Alternatively, press **Alt+F6** to enter repositioning mode — if the overlay is off-screen, it should move to the nearest visible area.

### Check always-on-top behaviour

Wheel Overlay is designed to stay on top of other windows. Some fullscreen exclusive games override the always-on-top setting. Try your sim in **borderless windowed** mode rather than exclusive fullscreen — this allows overlays to remain visible over the game.

### Monitor configuration changes

If you disconnected a monitor since your last session, the overlay may have been saved to the position of the now-absent display. Edit `%APPDATA%\WheelOverlay\settings.json` and reset `windowLeft` and `windowTop` to `100`.

---

## Settings Not Saving

**Symptom**: Changes made in Settings revert after restarting, or you see an error related to saving.

### AppData write permissions

Wheel Overlay stores settings at `%APPDATA%\WheelOverlay\settings.json`. Ensure your Windows user account has write access to this folder:

1. Open File Explorer and navigate to `%APPDATA%\WheelOverlay\` (paste this path into the address bar).
2. If the folder does not exist, Wheel Overlay should create it on first run. If it cannot, you may have a group policy restriction.
3. Right-click the `WheelOverlay` folder → **Properties** → **Security** → confirm your user has **Write** permission.

### Check the log file

Open `%APPDATA%\WheelOverlay\logs.txt`. Look for lines containing `ERROR` near the time you experienced the issue. Common entries:

- `Failed to save settings` — the settings file could not be written (permissions issue or disk full)
- `Failed to load settings file, using defaults` — the existing settings file is corrupt; delete it to reset to defaults

### Corrupt settings file

If the settings file contains invalid JSON (e.g., after a crash mid-write), Wheel Overlay falls back to defaults. To recover:

1. Navigate to `%APPDATA%\WheelOverlay\`.
2. Delete or rename `settings.json`.
3. Restart Wheel Overlay — it recreates the file with defaults.

---

## Hotkey Not Working

**Symptom**: Pressing Alt+F6 does nothing, or another application responds to it instead.

### Another app has claimed Alt+F6

The Alt+F6 hotkey is registered globally at startup. If another application (such as a voice chat tool, screen capture software, or sim overlay suite) has already registered Alt+F6, Wheel Overlay cannot claim it and the hotkey will not work.

**Fallback**: Use the system tray menu as an alternative:

1. Right-click the Wheel Overlay tray icon.
2. Select **Configure overlay position**.
3. Drag the overlay to the desired position and click elsewhere to confirm.

**To reclaim the hotkey**: Identify which application is using Alt+F6 (check your other apps' hotkey settings) and either disable that binding or change it to a different key. Then restart Wheel Overlay to register Alt+F6.

---

## Performance

**Symptom**: Wheel Overlay is consuming more CPU or RAM than expected.

### Expected performance targets

Wheel Overlay is designed to stay within these limits while running in overlay mode with no sim process detected:

| Metric | Target | Measured (v0.7.0, idle 60 s) |
|---|---|---|
| CPU usage | < 2% | ~0% |
| Private Working Set (RAM) | < 50 MB | ~32 MB |

> **Note**: Task Manager's "Memory" column shows Private Working Set. The total Working Set column (~108 MB) includes shared .NET runtime and WPF framework pages mapped by Windows — this memory is not exclusively owned by Wheel Overlay.

### How to check

Open **PowerShell** and run:

```powershell
Get-Process WheelOverlay | Select-Object CPU, WorkingSet64
```

The `WorkingSet64` value is in bytes; divide by 1,048,576 to get MB.

Alternatively, open **Task Manager** → **Details** tab, find `WheelOverlay.exe`, and check the CPU and Memory columns.

### If usage is higher than expected

- **Animations**: Disable animations under **Settings → Appearance → Enable Animations**. Animations increase GPU/CPU draw calls on each frame.
- **Multiple instances**: Wheel Overlay enforces single-instance by default, but if you see multiple `WheelOverlay.exe` entries in Task Manager, close all of them and relaunch.
- **Polling interval**: If the DirectInput device is polled too frequently, CPU usage may rise. This is logged in `logs.txt` — check for repeated high-frequency polling warnings.

---

## Log File Reference

The log file is located at `%APPDATA%\WheelOverlay\logs.txt`. It rotates at 1 MB, keeping one backup file.

Useful search terms:

| Search term | What it indicates |
|---|---|
| `ERROR` | Any error logged by the application |
| `Failed to load settings` | Settings file missing or corrupt |
| `Failed to save settings` | Write permission issue |
| `DirectInput` | Device polling events |
| `Migrating legacy settings` | One-time migration from pre-profile settings format |
| `CRITICAL FAILURE` | A startup crash — full stack trace follows |

---

## Related

- [Getting Started](getting-started.md) — installation and first configuration
- [Usage Guide](usage-guide.md) — full reference for all layouts, profiles, and settings
- [Tips](tips.md) — placement advice and power user workflows
