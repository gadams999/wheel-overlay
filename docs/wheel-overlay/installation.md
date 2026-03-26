# Installation

## Before You Begin

Ensure your system meets the [Requirements](requirements.md) and complete the shared operating system setup in [Common Setup](/common-setup/) before proceeding.

## Install WheelOverlay

1. Download the latest `WheelOverlay-vX.Y.Z.msi` installer from the [Releases page](https://github.com/gadams999/opendash-overlays/releases).
2. Double-click the `.msi` file and follow the installation wizard.
3. Accept the default install location or choose your own.
4. Click **Finish** when the wizard completes.

The installer does not require administrator rights by default. If Windows SmartScreen prompts you, click **More info → Run anyway**.

## First Launch

1. Open the **Start Menu**, search for **Wheel Overlay**, and click to launch it.
2. The app starts silently — there is no main window. Look for the **Wheel Overlay icon** in the **system tray** (bottom-right of your taskbar, near the clock).
    - If you don't see the icon, click the **^** arrow to expand hidden tray icons.
3. The overlay window appears on screen, showing your position labels in the default Grid layout.

!!! note "What to expect on first launch"
    WheelOverlay creates a default profile for the BavarianSimTec Alpha with labels DASH, TC2, MAP, FUEL, BRGT, VOL, BOX, DIFF. If your device is different, open Settings to select the correct device.

## Verify the Overlay is Working

After launching:

1. The overlay window should be visible on your desktop, showing your labels.
2. The currently selected position is highlighted in **white**; all others are shown in **grey**.
3. Rotate your wheel's encoder — the highlighted position should change in real time.

If you cannot see the overlay window, it may be positioned off-screen. Press **Alt+F6** to enter repositioning mode, drag the overlay to the desired location, then press **Alt+F6** again to lock it in place.
