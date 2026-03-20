# Wheel Overlay — Usage Guide

This guide explains all layout types, profile management, animation settings, grid and dial configuration, position count, Smart Text Condensing, Test Mode, the Alt+F6 hotkey, and the settings categories in detail.

---

## Layout Types

Wheel Overlay supports five layout types. Select your layout under **Settings → Display → Display Layout**.

### Single Text

Displays one label at a time — the label for the currently active rotary position. This is the most minimal layout, ideal when screen space is tight and you only need to know the current mode name.

**Best for**: Small overlays in a corner, or when your wheel has few positions and you want a clean, distraction-free display.

### Vertical List

All position labels are stacked in a single column, top to bottom. The active position's label is highlighted in the selected text colour; all others appear in the non-selected colour.

**Best for**: Narrow overlays placed along the left or right edge of the screen. Works well when you have many positions and want to see the full list at a glance.

### Horizontal List

All position labels are arranged in a single row, side by side. The active label is highlighted.

**Best for**: Wide, short overlays along the top or bottom of the screen. Best with a low position count (≤ 8) so labels don't get too cramped.

### Grid

Labels are arranged in a rectangular grid of rows and columns. You choose how many rows and columns to use under **Display → Grid Layout Dimensions**. The grid must have enough cells to hold all your positions (rows × columns ≥ position count).

A live grid preview is shown in the settings panel to help you verify the layout before saving.

**Best for**: The default choice for most wheels. Compact and readable even with 8–12 positions.

**Grid Configuration**:
- **Rows**: 1–8
- **Columns**: 1–8
- The grid dimensions are validated automatically. If you set dimensions that cannot fit your position count, the app adjusts to the nearest valid configuration and shows a warning.

### Dial

Labels are arranged in a circle, like positions on a clock face. The active position is highlighted. Two additional controls appear when this layout is selected:

- **Dial Knob Size**: scales the central indicator (range 1–10, in 0.5 steps)
- **Label Gap**: controls the distance between each label and the circle edge (range 10–20%)

**Best for**: Wheels with a traditional rotary knob feel. Visually communicates "you are turning a dial" in a way other layouts don't.

---

## Profile Management

Profiles let you save different label sets and layout configurations for the same device — for example, one profile for GT cars and another for formula cars with different rotary mappings.

Profiles are **per-device**: each wheel device has its own independent set of profiles.

### Create a Profile

1. Open **Settings → Display**.
2. Select your device from the **Wheel Device** dropdown.
3. Click **New** next to the Profile dropdown. A new profile is created as a copy of the current one.
4. Edit the labels and layout as desired, then click **Save** (or switch categories to auto-save).

### Rename a Profile

1. Make sure the profile you want to rename is selected in the Profile dropdown.
2. Click **Rename**.
3. Enter the new name and click **OK**.

### Switch Between Profiles

Open the Profile dropdown and select the desired profile. Settings update immediately.

### Delete a Profile

Select the profile to delete and click **Delete**. A confirmation dialog appears. You cannot delete the last remaining profile for a device.

### Why Use Profiles

- **Car-specific mappings**: Different cars use different rotary encoder functions. A GT car might use positions for TC/ABS/MAP/FUEL while a rally car uses DIFF/BRAKE/POWER/MODE.
- **Event-specific**: Save a "Qualifying" profile with only the settings you need to adjust during qualifying, separate from a "Race" profile.
- **Quick switching**: Switch profiles between sessions without re-typing all your labels.

---

## Animation Settings

When **Enable Animations** is on (found under **Settings → Appearance**), position changes animate smoothly. When off, transitions are instant. Disable animations if you prefer a lower-latency visual response or if you find motion distracting during racing.

---

## Grid Configuration

See the [Grid section](#grid) under Layout Types above. In addition:

- The **Grid Capacity** indicator in the settings panel shows how many cells the current grid dimensions provide vs. how many positions you have configured.
- If your position count exceeds the grid capacity, the app auto-adjusts grid dimensions with a notification.
- You can preview exactly how your labels will be arranged using the mini grid preview in the Display settings panel.

---

## Position Count

Set the number of rotary encoder positions under **Settings → Display → Number of Positions** (range: 2–20).

This controls how many labels are shown and how many position slots the grid, dial, or list displays. When you reduce the position count and labels exist in the removed slots, the app asks for confirmation before discarding them.

---

## Smart Text Condensing

When a position label is too long to fit within its cell, Wheel Overlay automatically condenses the text to fit. Labels are truncated or scaled down to keep them readable without overflowing into adjacent cells. Keep labels short (4–5 characters) for the best result.

---

## Test Mode

Run Wheel Overlay in test mode to simulate rotary encoder input without a physical device connected:

```
WheelOverlay.exe --test-mode
```

In test mode, the overlay cycles through positions automatically every second, letting you verify your labels and layout without needing your wheel plugged in. This is useful for setting up on a secondary PC or when configuring for a new event.

---

## Alt+F6 Hotkey — Repositioning the Overlay

Press **Alt+F6** at any time (including mid-session) to enter **Positioning Mode**:

- The overlay becomes semi-transparent with a grey background, indicating it can be moved.
- Drag the overlay to the desired position on screen.
- Press **Alt+F6** again, or click anywhere outside the overlay, to confirm the position and return to normal mode.

The new position is saved automatically and persists across restarts.

**Fallback if Alt+F6 is unavailable**: If another application has already claimed the Alt+F6 hotkey, registration will fail silently. In that case, use the system tray menu: right-click the tray icon and choose **Configure overlay position** to enter positioning mode.

---

## Settings Categories Overview

The Settings window is divided into four categories accessible from the left-hand navigation panel.

### Display

- Wheel Device selection
- Profile management (create, rename, switch, delete)
- Number of Positions (2–20)
- Grid dimensions and preview (Grid layout only)
- Dial knob size and label gap (Dial layout only)
- Position Labels (one text field per position)
- Display Layout selection

### Appearance

- **Theme**: System (follows Windows dark/light mode), Light, or Dark
- **Selected text colour**: hex colour code for the active position label
- **Non-selected text colour**: hex colour code for inactive labels
- **Font family**: choose from available system fonts
- **Font size**: slider to adjust label text size
- **Item spacing**: gap between layout items

### Advanced

- **Target process**: optionally restrict the overlay to appear only when a specific sim executable is running (leave blank to always show)
- **Overlay opacity**: global transparency level for the overlay window
- **Open settings folder**: opens `%APPDATA%\WheelOverlay\` in File Explorer for direct access to `settings.json` and `logs.txt`
- **Reset to defaults**: restores all settings to factory defaults (confirmation required)

### About

- Displays the current version, product name, and license information.
- Links to the project repository and release notes.

---

## Related

- [Getting Started](getting-started.md) — installation and first configuration
- [Tips](tips.md) — placement advice, font tuning, and power user workflows
- [Troubleshooting](troubleshooting.md) — solutions for common issues
