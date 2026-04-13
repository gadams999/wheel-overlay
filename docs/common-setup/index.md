---
title: "Common Setup"
description: "Shared prerequisites for all OpenDash overlay applications"
---

# Common Setup

This section covers requirements and setup steps that are shared across all OpenDash overlay applications. Complete these steps once and they apply to every overlay you install.

## Operating System

All OpenDash overlay applications require:

- **Windows 10 or Windows 11** (64-bit)

32-bit Windows installations are not supported.

## Windows SmartScreen

When you run an installer downloaded from the internet, Windows SmartScreen may display a warning because the application is not yet widely distributed. This is expected behaviour.

To proceed:

1. Click **More info** in the SmartScreen prompt.
2. Click **Run anyway**.

This step is required once per installer version.

## Game Display Mode Compatibility

All OpenDash overlay applications use a standard Windows always-on-top window. This works correctly with **borderless windowed** mode, which is the default or recommended setting in most modern games and simulators.

**Exclusive fullscreen mode is not supported.** When a game runs in exclusive fullscreen (also called DirectX fullscreen exclusive or fullscreen mode), it takes direct control of the display and Windows cannot render other windows — including overlays — on top of it. The overlay will not be visible.

To use any OpenDash overlay alongside a game:

1. Open your game or simulator's display settings.
2. Set the display mode to **Borderless Windowed** (sometimes labelled "Borderless", "Windowed Borderless", or "Fullscreen Windowed").
3. Restart the game if required.

The overlay will then appear on top of the game as expected.
