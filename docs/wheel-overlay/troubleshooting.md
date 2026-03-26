# Troubleshooting

If you cannot resolve an issue after following these steps, check the log file for detailed error information. Open **Settings → Advanced → Open settings folder** and look for `logs.txt`.

## Device Not Detected

**Symptom**: The device dropdown shows no device, the position label never changes when you rotate the encoder, or your expected device name is missing from the list.

### Check the device is plugged in

1. Open **Device Manager** (right-click Start → Device Manager).
2. Expand **Human Interface Devices** or **USB Input Devices**.
3. Confirm your wheel appears without a warning icon.
4. If the device shows a warning icon, right-click → **Update driver** → **Search automatically**.

### Check that DirectInput drivers are installed

Most sim racing wheels use DirectInput. Ensure the vendor driver is installed:

- For BavarianSimTec wheels: install the device driver from the BavarianSimTec website.
- For other wheels: check your wheel manufacturer's support page for a Windows driver package.

After installing drivers, restart WheelOverlay.

### Try running as administrator

Some USB HID devices require elevated permissions for DirectInput polling:

1. Right-click the WheelOverlay shortcut or `.exe` file.
2. Select **Run as administrator** and confirm the UAC prompt.

If the device appears after running as administrator, you can make this permanent: right-click the shortcut → **Properties** → **Compatibility** → check **Run this program as an administrator**.

### Verify the device name

Open **Settings → Display → Wheel Device** and check whether your device appears under a different name. DirectInput devices report their own name string, which may differ from the name on the box.

---

## Overlay Not Visible

**Symptom**: The app is running (tray icon is present) but you cannot see the overlay window on screen.

### Check the system tray icon

Look for the WheelOverlay icon in the system tray (bottom-right of taskbar). If you don't see it, click the **^** arrow to check the overflow area. If it is not there at all, the app may have crashed — check `logs.txt`.

### Recover an off-screen overlay

Press **Alt+F6** to enter repositioning mode. If the overlay is off-screen, it should move to the nearest visible area. Alternatively:

1. Open the WheelOverlay settings folder (**Settings → Advanced → Open settings folder**).
2. Open `settings.json` in Notepad.
3. Set `windowLeft` and `windowTop` to `100`, save the file, and restart WheelOverlay.

Windows display scaling above 100% can shift window positions unexpectedly — check your scaling percentage under **Display settings** if the issue recurs.

### Check always-on-top behaviour

WheelOverlay stays on top of other windows. Some fullscreen exclusive games override this. Try running your sim in **borderless windowed** mode rather than exclusive fullscreen, which allows overlays to remain visible.

### Overlay placement tips

Place the overlay in a corner away from the areas your eyes scan most during cornering and braking:

- **Bottom-left or bottom-right**: blends naturally with sim HUD elements.
- **Top-right**: often clear if your sim uses a map or radar in the bottom corners.

Avoid placing the overlay over the road surface or near the horizon line. Press **Alt+F6** mid-session to adjust position without leaving the game.

---

## Settings Not Saving

**Symptom**: Changes made in Settings revert after restarting, or you see a save error.

### Check write permissions

WheelOverlay stores settings in its settings folder. Ensure your Windows user account has write access:

1. Open **Settings → Advanced → Open settings folder**.
2. If the folder does not exist, WheelOverlay should create it on first run. If it cannot, you may have a group policy restriction.
3. Right-click the folder → **Properties** → **Security** → confirm your user has **Write** permission.

### Check the log file

Open `logs.txt` and look for lines containing `ERROR`. Common entries:

- `Failed to save settings` — the settings file could not be written (permissions or disk full)
- `Failed to load settings file, using defaults` — the settings file is corrupt; delete it to reset to defaults

### Recover from a corrupt settings file

1. Open **Settings → Advanced → Open settings folder**.
2. Delete or rename `settings.json`.
3. Restart WheelOverlay — it recreates the file with defaults.

---

## Hotkey Not Working

**Symptom**: Pressing Alt+F6 does nothing, or another application responds to it instead.

The Alt+F6 hotkey is registered globally at startup. If another application (such as a voice chat tool, screen capture software, or sim overlay suite) has already registered Alt+F6, WheelOverlay cannot claim it.

**Fallback**: Use the system tray menu:

1. Right-click the WheelOverlay tray icon.
2. Select **Configure overlay position**.
3. Drag the overlay to the desired position and click elsewhere to confirm.

**To reclaim the hotkey**: Identify which application is using Alt+F6 (check your other apps' hotkey settings), change or disable that binding, then restart WheelOverlay.

---

## Performance

**Symptom**: WheelOverlay is consuming more CPU or RAM than expected.

### Expected performance targets

| Metric | Target |
|--------|--------|
| CPU usage | < 2% |
| Private Working Set (RAM) | < 50 MB |

!!! note
    Task Manager's "Memory" column shows Private Working Set. The total Working Set column (~108 MB) includes shared .NET runtime and WPF framework pages mapped by Windows — this memory is not exclusively owned by WheelOverlay.

### Check current usage

Open **PowerShell** and run:

```powershell
Get-Process WheelOverlay | Select-Object CPU, WorkingSet64
```

The `WorkingSet64` value is in bytes; divide by 1,048,576 to get MB. Alternatively, open **Task Manager → Details**, find `WheelOverlay.exe`, and check the CPU and Memory columns.

### Reduce usage

- **Disable animations**: **Settings → Appearance → Enable Animations** off. Animations increase GPU/CPU draw calls on each frame.
- **Check for multiple instances**: WheelOverlay enforces single-instance, but if you see multiple `WheelOverlay.exe` entries in Task Manager, close all of them and relaunch.

---

## Font and Display Tips

### Font size for high-refresh displays

On high-refresh monitors (144 Hz, 240 Hz), small text can appear to flicker or blur during fast camera movement:

- Use a **slightly larger font size** than you think you need — labels that look crisp at rest may blur during rapid head movement in VR or triple-screen setups.
- **Bold fonts** (available via font family selection) hold legibility better at small sizes.
- **Keep labels short** (4–5 characters) — shorter text allows a larger font size for the same cell width.
- The **Single Text** layout allows a much larger font since only one label is shown at a time.

Adjust font size under **Settings → Appearance → Font size**.

---

## Profiles Reference

### One profile per car class

Create a separate profile for each car class you race:

| Profile Name | Labels (example) |
|---|---|
| GT3 | TC, ABS, MAP, FUEL, DIFF, BRAKE, RAMP, VOL |
| Formula | BB, MIX, ENG, MGU, TIRE, SC, MODE, PIT |
| Rally | DIFF, BRAKE, POWER, MAP, FAN, INTER, TRAC, STAGE |

Switch profiles between sessions via **Settings → Display → Profile dropdown**.

### Copy a profile as a starting point

When you click **New**, the new profile starts as a copy of the currently selected profile. Duplicate the closest existing profile, rename it, and change only the labels that differ.

---

## Log File Reference

The log file is in the WheelOverlay settings folder (`logs.txt`). It rotates at 1 MB, keeping one backup.

| Search term | What it indicates |
|---|---|
| `ERROR` | Any error logged by the application |
| `Failed to load settings` | Settings file missing or corrupt |
| `Failed to save settings` | Write permission issue |
| `DirectInput` | Device polling events |
| `Migrating legacy settings` | One-time migration from pre-profile settings format |
| `CRITICAL FAILURE` | A startup crash — full stack trace follows |
