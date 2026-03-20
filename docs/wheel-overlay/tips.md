# Wheel Overlay — Tips

Practical advice for getting the best experience from Wheel Overlay during sim racing.

---

## Optimal Overlay Placement

### Corner Positioning

Place the overlay in a corner of your screen — away from the apex of turns you frequently look toward. Common choices:

- **Bottom-left or bottom-right**: close to the HUD area most sims use for fuel, lap time, and gear; blends naturally with existing on-screen data.
- **Top-right**: often the clearest area if your sim uses a map or radar in the bottom corners.

Avoid positioning the overlay over the road surface or near the horizon line — these are the areas your eyes scan most during cornering and braking.

### Avoiding Apex Sight Lines

During a race, your eyes follow a predictable path: braking point → apex → exit point. Before settling on a position, drive a few laps and notice where your gaze naturally falls at your hardest corners. Move the overlay away from those zones.

Press **Alt+F6** to enter repositioning mode at any time — including mid-session — to adjust without leaving the game.

---

## Font Size for High-Refresh Displays

On high-refresh-rate monitors (144 Hz, 240 Hz), small text can appear to flicker or blur during fast camera movement. A few tips:

- **Use a slightly larger font size** than you think you need — the overlay moves with the window, so labels that look crisp at rest may blur during rapid head movement in VR or triple-screen setups.
- **Bold fonts** (available via font family selection) hold their legibility better at small sizes and high refresh rates.
- **Keep labels short** (4–5 characters). Shorter text allows a larger font size for the same cell width, which is easier to read in motion.
- If using the **Single Text** layout, you can afford a much larger font since only one label is shown at a time.

Adjust font size under **Settings → Appearance → Font size**.

---

## Using Profiles for Different Cars and Games

### One Profile Per Car Class

Create a profile for each car class you race. For example:

| Profile Name | Labels (example) |
|---|---|
| GT3 | TC, ABS, MAP, FUEL, DIFF, BRAKE, RAMP, VOL |
| Formula | BB, MIX, ENG, MGU, TIRE, SC, MODE, PIT |
| Rally | DIFF, BRAKE, POWER, MAP, FAN, INTER, TRAC, STAGE |

Switch profiles between sessions via **Settings → Display → Profile dropdown**.

### One Profile Per Game

If you race multiple sims with the same wheel, their rotary functions may differ even for the same car class. Keep one profile per game/car-class combination and name them clearly (e.g., `iRacing GT3`, `ACC GT3`).

### Copying a Profile as a Starting Point

When you click **New** in the profile panel, the new profile starts as a copy of the currently selected profile. Use this to quickly set up a new car: duplicate the closest existing profile, rename it, and change only the labels that differ.

---

## Power User Tips

### System Tray Quick-Access

Right-click the Wheel Overlay tray icon for fast access to the most common actions without opening the full settings window:

- **Open Settings** — opens the settings window directly
- **Configure overlay position** — enters repositioning mode (same as Alt+F6, useful if the hotkey is claimed by another app)
- **Exit** — closes Wheel Overlay

Keep the tray icon accessible by pinning it to the visible area of your taskbar (drag it out from the overflow menu).

### Hotkey Workflow for Quick Repositioning Mid-Session

When you need to move the overlay without pausing or tabbing out:

1. Press **Alt+F6** — the overlay turns semi-transparent and becomes draggable.
2. Drag it to the new position.
3. Press **Alt+F6** again (or click elsewhere on the screen) to lock it.

The whole operation takes about three seconds and does not interrupt your sim session.

### Test Mode for Setup Without a Wheel

Use `--test-mode` when setting up labels and layout on a PC without your wheel connected, or to preview how the overlay looks in motion:

```
WheelOverlay.exe --test-mode
```

Test mode cycles through all positions automatically every second, so you can see how each label reads in context.

### Checking Your Current Version

Right-click the tray icon → **Settings** → **About** to see the installed version. Compare against the [Releases page](https://github.com/OpenDash/opendash-overlays/releases) to check for updates.

---

## Related

- [Getting Started](getting-started.md) — installation and first configuration
- [Usage Guide](usage-guide.md) — full reference for all layouts, profiles, and settings
- [Troubleshooting](troubleshooting.md) — solutions for common issues
