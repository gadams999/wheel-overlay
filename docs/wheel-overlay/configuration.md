# Configuration

Open Settings by right-clicking the **Wheel Overlay system tray icon** and choosing **Settings**. The Settings window has four categories on the left: **Display**, **Appearance**, **Advanced**, and **About**.

## Display

### Selecting Your Device

Under **Wheel Device**, open the dropdown and select your wheel from the list. The profile and label list update automatically to match your device.

### Profiles

Profiles let you save different label sets and layout configurations for the same device — for example, one profile for GT cars and another for formula cars.

Profiles are **per-device**: each wheel device has its own independent set of profiles.

| Action | How |
|--------|-----|
| Create | Click **New** — the new profile starts as a copy of the current one |
| Rename | Select the profile, then click **Rename** |
| Switch | Open the Profile dropdown and select the desired profile |
| Delete | Select the profile and click **Delete** (confirmation required; cannot delete the last profile) |

### Number of Positions

Set the number of rotary encoder positions under **Number of Positions** (range: 2–20). This controls how many labels are shown and how many slots the layout displays.

### Display Layout

Choose the layout that suits your screen setup under **Display Layout**:

| Layout | Description |
|--------|-------------|
| **Single Text** | One label at a time — the current active position only |
| **Vertical List** | All labels stacked in a column; active label is highlighted |
| **Horizontal List** | All labels in a single row; active label is highlighted |
| **Grid** *(default)* | Labels arranged in rows and columns |
| **Dial** | Labels arranged in a circle, like a clock face |

### Grid Configuration

When **Grid** is selected, set **Rows** and **Columns** under **Grid Layout Dimensions**. The grid must have enough cells to hold all your positions (rows × columns ≥ position count). A live preview in the settings panel shows how your labels will be arranged.

- **Rows**: 1–8
- **Columns**: 1–8

### Dial Configuration

When **Dial** is selected, two additional controls appear:

- **Dial Knob Size**: scales the central indicator (range 1–10, in 0.5 steps)
- **Label Gap**: controls the distance between each label and the circle edge (range 10–20%)

### Position Labels

The **Position Labels** section lists one text field per position. Type the short label for each rotary position (e.g., `TC`, `MAP`, `FUEL`). Labels should be 4–5 characters for best fit.

## Appearance

| Setting | Description |
|---------|-------------|
| **Theme** | System (follows Windows dark/light mode), Light, or Dark |
| **Selected text colour** | Hex colour code for the active position label |
| **Non-selected text colour** | Hex colour code for inactive labels |
| **Font family** | Choose from available system fonts |
| **Font size** | Slider to adjust label text size |
| **Item spacing** | Gap between layout items |
| **Enable Animations** | Smooth transitions on position change; disable for instant response |

## Advanced

| Setting | Description |
|---------|-------------|
| **Target process** | Restrict the overlay to appear only when a specific sim executable is running; leave blank to always show |
| **Overlay opacity** | Global transparency level for the overlay window |
| **Open settings folder** | Opens the WheelOverlay settings folder in File Explorer |
| **Reset to defaults** | Restores all settings to factory defaults (confirmation required) |

## Repositioning the Overlay

Press **Alt+F6** at any time — including mid-session — to enter **Positioning Mode**:

1. The overlay becomes semi-transparent with a grey background.
2. Drag the overlay to the desired position.
3. Press **Alt+F6** again, or click anywhere outside the overlay, to confirm.

The new position is saved automatically and persists across restarts.

If another application has claimed the Alt+F6 hotkey, use the fallback: right-click the tray icon and choose **Configure overlay position**.

## Test Mode

Run WheelOverlay without a physical device connected to preview your labels and layout:

```
WheelOverlay.exe --test-mode
```

In test mode, the overlay cycles through all positions automatically every second.
