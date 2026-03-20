# Getting Started with Wheel Overlay

This guide walks you through installing Wheel Overlay and configuring it for use with your sim racing wheel — no prior experience required.

---

## Prerequisites

- **Windows 10 or later** (64-bit)
- A **DirectInput-compatible sim racing wheel** connected via USB
  - Verified: BavarianSimTec Alpha
  - Any DirectInput wheel should be detectable; unsupported devices appear in the device list but may have limited position count defaults
- The wheel must be **plugged in before launching** Wheel Overlay

---

## Installation

1. Download the latest `WheelOverlay-vX.Y.Z.msi` installer from the [Releases page](https://github.com/OpenDash/opendash-overlays/releases).
2. Double-click the `.msi` file and follow the installation wizard.
3. Accept the default install location or choose your own.
4. Click **Finish** when the wizard completes.

The installer does not require administrator rights by default. If Windows SmartScreen prompts you, click **More info → Run anyway**.

---

## First Launch

1. Open the **Start Menu** and search for **Wheel Overlay**, then click to launch it.
2. The app starts silently — there is no main window. Look for the **Wheel Overlay icon** in the **system tray** (bottom-right of your taskbar, near the clock).
   - If you don't see the icon, click the **^** arrow to expand hidden tray icons.
3. The overlay window appears on screen, showing your position labels in the default Grid layout.

> **What to expect on first launch**: Wheel Overlay creates a default profile for the BavarianSimTec Alpha with labels DASH, TC2, MAP, FUEL, BRGT, VOL, BOX, DIFF. If your device is different, open Settings to select the correct device (see below).

---

## Initial Configuration

### Step 1 — Open Settings

Right-click the **Wheel Overlay system tray icon** and choose **Settings**.

The Settings window opens with four categories on the left: **Display**, **Appearance**, **Advanced**, and **About**.

### Step 2 — Select Your Device

1. Click **Display** in the left panel.
2. Under **Wheel Device**, open the dropdown and select your wheel from the list.
3. The profile and label list update automatically to match your device.

### Step 3 — Set Your Layout

Still in the **Display** category:

1. Scroll down to **Display Layout** and choose the layout that suits your screen setup:
   - **Single Text** — one label at a time
   - **Vertical List** — labels stacked top to bottom
   - **Horizontal List** — labels side by side
   - **Grid** — labels arranged in rows and columns *(default)*
   - **Dial** — circular arrangement, like a clock face
2. If you chose **Grid**, set the number of **Rows** and **Columns** to match how many positions your wheel has.
3. Set **Number of Positions** to match your wheel's rotary encoder (2–20 positions).

### Step 4 — Edit Your Position Labels

In the **Display** category, the **Position Labels** section lists one text field per position. Type the short label you want displayed for each rotary position (e.g., `TC`, `MAP`, `FUEL`). Labels should be 4–5 characters for best fit.

### Step 5 — Save Settings

Click **Save** (or close the window — settings are saved automatically when you switch categories or close the window).

---

## Verifying the Overlay is Visible

After configuring your device and layout:

1. The overlay window should be visible on your desktop, showing your labels.
2. The currently selected position is highlighted in **white**; all other positions are shown in **grey**.
3. Rotate your wheel's encoder — the highlighted position should change in real time.

If you cannot see the overlay window, it may be positioned off-screen. Press **Alt+F6** to enter repositioning mode, then drag the overlay to the desired location and press **Alt+F6** again (or click elsewhere) to lock it in place.

---

## Your First Sim Session

1. Launch your sim racing game.
2. Wheel Overlay monitors running processes automatically. The overlay stays **always on top** so it remains visible over your sim.
3. Rotate your wheel's rotary encoder — the highlighted label changes to show the current position.
4. To reposition the overlay mid-session without opening Settings, press **Alt+F6**, drag the overlay, and press **Alt+F6** again to confirm.

---

## Next Steps

- [Usage Guide](usage-guide.md) — detailed explanation of all layout types, profile management, and settings categories
- [Tips](tips.md) — overlay placement advice, font tuning, and power user workflows
- [Troubleshooting](troubleshooting.md) — solutions for common issues including device not detected and overlay not visible
